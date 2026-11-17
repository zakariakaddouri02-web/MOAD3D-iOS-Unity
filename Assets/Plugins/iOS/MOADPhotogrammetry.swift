import Foundation
import ModelIO
import RealityKit

@available(iOS 17.0, *)
@objc public class MOADPhotogrammetryBridge: NSObject {
    @objc public static func isSupported() -> Bool {
        PhotogrammetrySession.isSupported
    }

    @objc public static func start(
        inputPath: String,
        usdzPath: String,
        objPath: String,
        callback: @escaping @convention(block) (Float, NSString?) -> Void
    ) {
        guard PhotogrammetrySession.isSupported else {
            callback(0, "This iPhone does not support on-device object reconstruction.")
            return
        }

        Task { @MainActor in
            let inputURL = URL(fileURLWithPath: inputPath, isDirectory: true)
            let stagingURL = inputURL.appendingPathComponent("_moad_reconstruction_input", isDirectory: true)
            let usdzURL = URL(fileURLWithPath: usdzPath)
            let objURL = URL(fileURLWithPath: objPath)
            do {
                let images = try FileManager.default.contentsOfDirectory(
                    at: inputURL,
                    includingPropertiesForKeys: nil
                ).filter { ["jpg", "jpeg"].contains($0.pathExtension.lowercased()) }
                guard images.count >= 20 else {
                    callback(0, "Capture at least 20 photos with overlapping views before reconstruction.")
                    return
                }

                try? FileManager.default.removeItem(at: stagingURL)
                try FileManager.default.createDirectory(
                    at: stagingURL,
                    withIntermediateDirectories: true
                )
                for imageURL in images {
                    try FileManager.default.copyItem(
                        at: imageURL,
                        to: stagingURL.appendingPathComponent(imageURL.lastPathComponent)
                    )
                }
                try FileManager.default.createDirectory(
                    at: usdzURL.deletingLastPathComponent(),
                    withIntermediateDirectories: true
                )

                let session = try PhotogrammetrySession(input: stagingURL)
                let request = PhotogrammetrySession.Request.modelFile(
                    url: usdzURL,
                    detail: .reduced
                )

                let monitor = Task { @MainActor in
                    do {
                        for try await output in session.outputs {
                            switch output {
                            case .requestProgress(_, let fractionComplete):
                                callback(Float(fractionComplete), "Building your 3D model…")
                            case .requestComplete(_, let result):
                                if case .modelFile(let modelURL) = result {
                                    do {
                                        guard MDLAsset.canExportFileExtension("obj") else {
                                            callback(0, "This iPhone cannot export the model as OBJ.")
                                            continue
                                        }
                                        let asset = MDLAsset(url: modelURL)
                                        if FileManager.default.fileExists(atPath: objURL.path) {
                                            try FileManager.default.removeItem(at: objURL)
                                        }
                                        try asset.export(to: objURL)
                                        callback(1, "Model ready: \(objURL.path)" as NSString)
                                    } catch {
                                        callback(0, "Model created but OBJ export failed: \(error.localizedDescription)" as NSString)
                                    }
                                }
                            case .requestError(_, let error):
                                callback(0, "Reconstruction failed: \(error.localizedDescription)" as NSString)
                            case .invalidSample(_, let reason):
                                callback(0, "A photo could not be used: \(reason)" as NSString)
                            case .processingCancelled:
                                callback(0, "Reconstruction was cancelled.")
                            case .processingComplete:
                                return
                            default:
                                break
                            }
                        }
                    } catch {
                        callback(0, "Reconstruction failed: \(error.localizedDescription)" as NSString)
                    }
                }

                session.process(requests: [request])
                _ = await monitor.result
                try? FileManager.default.removeItem(at: stagingURL)
            } catch {
                try? FileManager.default.removeItem(at: stagingURL)
                callback(0, "Could not start reconstruction: \(error.localizedDescription)" as NSString)
            }
        }
    }
}
