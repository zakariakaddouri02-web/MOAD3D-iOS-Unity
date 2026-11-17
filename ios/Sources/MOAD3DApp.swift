import ARKit
import AVFoundation
import CoreImage
import Foundation
import QuickLook
import RealityKit
import simd
import SwiftUI

@main
struct MOAD3DApp: App {
    @StateObject private var scanner = ScanViewModel()

    var body: some SwiftUI.Scene {
        WindowGroup {
            ScanScreen(scanner: scanner)
        }
    }
}

@MainActor
final class ScanViewModel: ObservableObject {
    @Published private(set) var photoCount = 0
    @Published private(set) var isCameraReady = false
    @Published private(set) var isScanning = false
    @Published private(set) var isReconstructing = false
    @Published private(set) var progress = 0.0
    @Published private(set) var status = "Move slowly around a textured object."
    @Published var modelURL: URL?
    @Published private(set) var torchEnabled = false

    private let captureEngine = ScanFrameCapture()
    private var arSession: ARSession?
    private var scanFolder: URL?

    init() {
        captureEngine.onPhotoCount = { [weak self] count in
            self?.photoCount = count
            if count >= 150, self?.isScanning == true {
                self?.stopScan()
            }
        }
        captureEngine.onError = { [weak self] message in self?.status = message }
    }

    func attach(session: ARSession) {
        arSession = session
        captureEngine.attach(to: session)
        isCameraReady = true
    }

    func startScan() {
        guard isCameraReady, let arSession else {
            status = "Waiting for camera access…"
            return
        }
        guard ARWorldTrackingConfiguration.isSupported else {
            status = "This iPhone does not support AR camera tracking."
            return
        }

        guard let folder = captureEngine.startScan() else {
            status = "Could not create a scan folder. Check free iPhone storage."
            return
        }
        scanFolder = folder

        photoCount = 0
        isScanning = true
        modelURL = nil
        status = "Scanning with the rear camera. Keep moving slowly around the object."
        let configuration = ARWorldTrackingConfiguration()
        arSession.run(configuration, options: [.resetTracking, .removeExistingAnchors])
    }

    func stopScan() {
        let (folder, count) = captureEngine.stopScan()
        scanFolder = folder
        isScanning = false
        photoCount = count
        if count >= 20, let folder {
            scanFolder = folder
            status = "Saved \(count) photos. Build the 3D model when ready."
        } else {
            status = "Saved \(count) photos. Capture at least 20 to build a 3D model."
        }
    }

    func setTorchEnabled(_ enabled: Bool) {
        guard let device = AVCaptureDevice.default(.builtInWideAngleCamera,
                                                   for: .video,
                                                   position: .back), device.hasTorch else {
            status = "The rear camera flash is not available."
            return
        }
        do {
            try device.lockForConfiguration()
            defer { device.unlockForConfiguration() }
            if enabled {
                try device.setTorchModeOn(level: 1.0)
            } else {
                device.torchMode = .off
            }
            torchEnabled = enabled
        } catch {
            status = "Could not change the flash: \(error.localizedDescription)"
        }
    }

    func buildModel() {
        guard !isScanning, !isReconstructing, photoCount >= 20, let scanFolder else {
            status = "Capture at least 20 photos, then stop the scan before building the model."
            return
        }
        guard #available(iOS 17.0, *), PhotogrammetrySession.isSupported else {
            status = "Apple on-device reconstruction is not supported on this iPhone."
            return
        }

        isReconstructing = true
        progress = 0
        status = "Building the 3D model on iPhone. Keep the app open."
        Task { await reconstruct(from: scanFolder) }
    }

    @available(iOS 17.0, *)
    private func reconstruct(from inputFolder: URL) async {
        let outputFolder = documentsURL().appendingPathComponent("MOAD3D/Models", isDirectory: true)
        let stagingFolder = inputFolder.appendingPathComponent("_moad_reconstruction_input", isDirectory: true)
        let outputURL = outputFolder.appendingPathComponent(
            "scan_\(DateFormatter.scanTimestamp.string(from: Date())).usdz"
        )

        do {
            let imageURLs = try FileManager.default.contentsOfDirectory(
                at: inputFolder,
                includingPropertiesForKeys: nil
            ).filter { ["jpg", "jpeg"].contains($0.pathExtension.lowercased()) }
            guard imageURLs.count >= 20 else {
                throw ScanError.tooFewPhotos
            }
            try? FileManager.default.removeItem(at: stagingFolder)
            try FileManager.default.createDirectory(at: stagingFolder, withIntermediateDirectories: true)
            for imageURL in imageURLs {
                try FileManager.default.copyItem(
                    at: imageURL,
                    to: stagingFolder.appendingPathComponent(imageURL.lastPathComponent)
                )
            }
            try FileManager.default.createDirectory(at: outputFolder, withIntermediateDirectories: true)

            let session = try PhotogrammetrySession(input: stagingFolder)
            let request = PhotogrammetrySession.Request.modelFile(url: outputURL, detail: .reduced)
            try session.process(requests: [request])

            var completedURL: URL?
            outputLoop: for try await output in session.outputs {
                switch output {
                case .requestProgress(_, let fractionComplete):
                    progress = Double(fractionComplete)
                    status = "Building the 3D model… \(Int(fractionComplete * 100))%"
                case .requestComplete(_, let result):
                    if case .modelFile(let url) = result {
                        completedURL = url
                    }
                case .requestError(_, let error):
                    throw error
                case .invalidSample(_, let reason):
                    status = "Skipping an unusable photo: \(reason)"
                case .processingCancelled:
                    throw ScanError.cancelled
                case .processingComplete:
                    break outputLoop
                default:
                    break
                }
            }

            guard let completedURL else {
                throw ScanError.modelMissing
            }
            modelURL = completedURL
            progress = 1
            status = "3D model ready. Tap Preview 3D to inspect it."
        } catch {
            status = "3D reconstruction failed: \(error.localizedDescription)"
        }
        try? FileManager.default.removeItem(at: stagingFolder)
        isReconstructing = false
    }

    private func documentsURL() -> URL {
        FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
    }
}

private final class ScanFrameCapture: NSObject, ARSessionDelegate {
    var onPhotoCount: (@MainActor (Int) -> Void)?
    var onError: (@MainActor (String) -> Void)?

    private let stateLock = NSLock()
    private let delegateQueue = DispatchQueue(label: "app.moad.scan.frames", qos: .userInitiated)
    private let writerQueue = DispatchQueue(label: "app.moad.scan.writer", qos: .utility)
    private let activeCaptureWork = DispatchGroup()
    private let ciContext = CIContext()
    private var scanning = false
    private var currentCount = 0
    private var lastTimestamp: TimeInterval = 0
    private var lastPosition: SIMD3<Float>?
    private var lastRotation: simd_quatf?
    private var scanFolder: URL?

    func attach(to session: ARSession) {
        session.delegateQueue = delegateQueue
        session.delegate = self
    }

    func startScan() -> URL? {
        writerQueue.sync {}
        let capturesURL = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("MOAD3D/Captures", isDirectory: true)
        let folder = capturesURL.appendingPathComponent(
            "scan_\(DateFormatter.scanTimestamp.string(from: Date()))",
            isDirectory: true
        )
        do {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            FileManager.default.createFile(atPath: folder.appendingPathComponent("camera_poses.jsonl").path,
                                           contents: nil)
        } catch {
            return nil
        }

        stateLock.lock()
        scanning = true
        currentCount = 0
        lastTimestamp = 0
        lastPosition = nil
        lastRotation = nil
        scanFolder = folder
        stateLock.unlock()
        return folder
    }

    func stopScan() -> (URL?, Int) {
        stateLock.lock()
        scanning = false
        let folder = scanFolder
        stateLock.unlock()

        activeCaptureWork.wait()
        writerQueue.sync {}
        stateLock.lock()
        let count = currentCount
        stateLock.unlock()
        return (folder, count)
    }

    func session(_ session: ARSession, didUpdate frame: ARFrame) {
        guard case .normal = frame.camera.trackingState else { return }
        let matrix = frame.camera.transform
        let position = SIMD3<Float>(matrix.columns.3.x, matrix.columns.3.y, matrix.columns.3.z)
        let rotation = simd_quatf(matrix)

        stateLock.lock()
        guard scanning, currentCount < 150, frame.timestamp - lastTimestamp >= 0.55 else {
            stateLock.unlock()
            return
        }
        if let previousPosition = lastPosition, let previousRotation = lastRotation {
            let moved = simd_distance(previousPosition, position)
            let dot = min(1, abs(simd_dot(previousRotation, rotation)))
            let rotatedDegrees = 2 * acos(dot) * 180 / Float.pi
            if moved < 0.02 && rotatedDegrees < 3 {
                stateLock.unlock()
                return
            }
        }
        lastTimestamp = frame.timestamp
        lastPosition = position
        lastRotation = rotation
        activeCaptureWork.enter()
        stateLock.unlock()

        defer { activeCaptureWork.leave() }
        let ciImage = CIImage(cvPixelBuffer: frame.capturedImage)
        guard let jpeg = ciContext.jpegRepresentation(of: ciImage,
                                                     colorSpace: CGColorSpaceCreateDeviceRGB(),
                                                     options: [:]) else { return }

        stateLock.lock()
        currentCount += 1
        let index = currentCount
        if currentCount >= 150 { scanning = false }
        let folder = scanFolder
        stateLock.unlock()
        guard let folder else { return }

        let imageName = String(format: "frame_%04d.jpg", index)
        let imageURL = folder.appendingPathComponent(imageName)
        let poseURL = folder.appendingPathComponent("camera_poses.jsonl")
        let record = PoseRecord(image: imageName,
                                capturedAt: Date().ISO8601Format(),
                                position: [position.x, position.y, position.z],
                                rotation: [rotation.vector.x, rotation.vector.y,
                                           rotation.vector.z, rotation.vector.w])
        Task { @MainActor [weak self] in self?.onPhotoCount?(index) }
        writerQueue.async { [weak self] in
            do {
                try jpeg.write(to: imageURL, options: Data.WritingOptions.atomic)
                var line = try JSONEncoder().encode(record)
                line.append(0x0A)
                let handle = try FileHandle(forWritingTo: poseURL)
                defer { try? handle.close() }
                try handle.seekToEnd()
                try handle.write(contentsOf: line)
            } catch {
                Task { @MainActor [weak self] in
                    self?.onError?("Could not save a scan photo: \(error.localizedDescription)")
                }
            }
        }
    }
}

private struct PoseRecord: Encodable {
    let image: String
    let capturedAt: String
    let position: [Float]
    let rotation: [Float]
}

private enum ScanError: LocalizedError {
    case tooFewPhotos
    case modelMissing
    case cancelled

    var errorDescription: String? {
        switch self {
        case .tooFewPhotos: return "Capture at least 20 photos with overlapping views."
        case .modelMissing: return "The reconstruction finished without an output model."
        case .cancelled: return "The reconstruction was cancelled."
        }
    }
}

private extension DateFormatter {
    static let scanTimestamp: DateFormatter = {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "yyyyMMdd_HHmmss"
        return formatter
    }()
}

struct CameraPreview: UIViewRepresentable {
    @ObservedObject var scanner: ScanViewModel

    func makeUIView(context: Context) -> ARView {
        let view = ARView(frame: .zero)
        view.automaticallyConfigureSession = false
        let configuration = ARWorldTrackingConfiguration()
        view.session.run(configuration, options: [.resetTracking, .removeExistingAnchors])
        scanner.attach(session: view.session)
        return view
    }

    func updateUIView(_ uiView: ARView, context: Context) {}

    static func dismantleUIView(_ uiView: ARView, coordinator: ()) {
        uiView.session.pause()
    }
}

struct ScanScreen: View {
    @ObservedObject var scanner: ScanViewModel
    @State private var previewURL: URL?

    var body: some View {
        ZStack {
            CameraPreview(scanner: scanner).ignoresSafeArea()
            LinearGradient(colors: [.black.opacity(0.58), .clear, .black.opacity(0.78)],
                           startPoint: .top, endPoint: .bottom)
                .ignoresSafeArea()

            VStack(spacing: 18) {
                HStack {
                    VStack(alignment: .leading, spacing: 3) {
                        Text("MOAD 3D SCAN").font(.headline).bold()
                        Text("Offline · rear camera").font(.caption).opacity(0.8)
                    }
                    Spacer()
                    Button {
                        scanner.setTorchEnabled(!scanner.torchEnabled)
                    } label: {
                        Image(systemName: scanner.torchEnabled ? "flashlight.on.fill" : "flashlight.off.fill")
                            .font(.title3).frame(width: 44, height: 44)
                            .background(.ultraThinMaterial, in: Circle())
                    }
                }

                Spacer()

                VStack(spacing: 9) {
                    Text(scanner.status).font(.subheadline).multilineTextAlignment(.center)
                    if scanner.isScanning {
                        Text("\(scanner.photoCount) / 150 photos")
                            .font(.title2).bold().monospacedDigit()
                        ProgressView(value: Double(scanner.photoCount), total: 150)
                            .tint(.mint)
                        Text("Move slowly. Keep each view overlapping and sharp.")
                            .font(.caption).opacity(0.8)
                    } else if scanner.isReconstructing {
                        ProgressView(value: scanner.progress)
                            .tint(.mint)
                    }
                }
                .padding(18)
                .frame(maxWidth: .infinity)
                .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 20))

                HStack(spacing: 12) {
                    Button {
                        scanner.isScanning ? scanner.stopScan() : scanner.startScan()
                    } label: {
                        Label(scanner.isScanning ? "Stop" : "Start scan",
                              systemImage: scanner.isScanning ? "stop.fill" : "viewfinder")
                            .frame(maxWidth: .infinity).frame(height: 54)
                    }
                    .buttonStyle(.borderedProminent)
                    .tint(scanner.isScanning ? .red : .mint)
                    .disabled(!scanner.isCameraReady || scanner.isReconstructing)

                    if scanner.photoCount >= 20 && !scanner.isScanning {
                        Button {
                            scanner.buildModel()
                        } label: {
                            Label("Build 3D", systemImage: "cube.transparent")
                                .frame(maxWidth: .infinity).frame(height: 54)
                        }
                        .buttonStyle(.borderedProminent)
                        .disabled(scanner.isReconstructing)
                    }
                }

                if scanner.modelURL != nil {
                    Button {
                        previewURL = scanner.modelURL
                    } label: {
                        Label("Preview 3D model", systemImage: "arkit")
                            .frame(maxWidth: .infinity).frame(height: 50)
                    }
                    .buttonStyle(.bordered)
                }
            }
            .padding(.horizontal, 20)
            .padding(.top, 12)
            .padding(.bottom, 18)
            .foregroundStyle(.white)
        }
        .preferredColorScheme(.dark)
        .quickLookPreview($previewURL)
        .onDisappear {
            if scanner.isScanning { scanner.stopScan() }
            if scanner.torchEnabled { scanner.setTorchEnabled(false) }
        }
    }
}
