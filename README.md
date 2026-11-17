# MOAD 3D Scan for iOS

Native SwiftUI iPhone app. The Codemagic workflow builds the Xcode project directly; it does not install or activate Unity.

## Scan flow

1. Open MOAD 3D Scan and allow camera access.
2. Tap **Start scan** and move slowly around a static, textured object. Keep views sharp with substantial overlap.
3. Tap **Stop** after at least 20 photos; capture can collect up to 150.
4. Tap **Build 3D**. RealityKit photogrammetry processes the saved rear-camera photos on the iPhone and saves a USDZ model.
5. Tap **Preview 3D model** to inspect and rotate the USDZ model with Apple's Quick Look viewer.

Photos and camera poses are stored locally under `Documents/MOAD3D/Captures/`. Generated USDZ files are stored under `Documents/MOAD3D/Models/`. Reconstruction does not upload scans or use network services.

## LiDAR and device support

Capture uses the rear RGB camera and ARKit camera tracking. It does not request LiDAR depth or rear stereo depth. RealityKit's on-device Object Capture API is available only on select LiDAR-capable Apple devices, and this app checks `PhotogrammetrySession.isSupported` before reconstruction. Apple's reconstruction request uses the captured photographs, but the API's behavior on an iPhone whose LiDAR hardware is faulty has not yet been verified on that device. If the runtime reports unsupported or reconstruction fails, capture still saves the photo set locally.

The iPhone 12 Pro Max uses an A14 chip and includes LiDAR hardware, so it is a model worth testing with this image-based workflow. The camera can still track with ARKit without using LiDAR. Final confirmation requires a device build and a complete scan on the phone.

Photogrammetry works best with a static object that has visible texture, diffuse lighting, sharp frames, and at least 70% overlap between adjacent views. Reflective, transparent, plain, moving, or poorly lit objects may fail or produce holes. On iOS, RealityKit generates reduced-detail models for mobile viewing.

## Build with Codemagic

The `ios-build` workflow builds `MOAD3D.xcodeproj` using Xcode and packages `MOAD3D-unsigned.ipa`. It requires no Unity account or Unity license. The IPA is unsigned for AltServer sideloading and is not ready for TestFlight or the App Store. This native workflow has not yet been run successfully in Codemagic or on a physical iPhone.

1. Add this GitHub repository to Codemagic.
2. Run the `ios-build` workflow.
3. Download `MOAD3D-unsigned.ipa` from the build artifacts.
4. On Windows, connect the iPhone and hold **Shift** while clicking AltServer; choose **Sideload .ipa…** and select the IPA. On Mac, hold **Option** while clicking AltServer. Sign with your Apple ID.

Free Apple ID sideloads need periodic refresh. See [AltServer's guide](https://faq.altstore.io/altstore-classic/altserver) and the [AltStore Windows](https://faq.altstore.io/altstore-classic/how-to-install-altstore-windows) or [macOS](https://faq.altstore.io/altstore-classic/how-to-install-altstore-macos) installation guide.

To distribute through TestFlight or the App Store, configure Apple signing and provisioning in Codemagic, then use a signed archive workflow.

## Build locally

Open `MOAD3D.xcodeproj` in Xcode 16 or later and build for an iPhone running iOS 17 or later. The app uses only Apple frameworks: SwiftUI, ARKit, RealityKit, AVFoundation, and Quick Look.

## References

- [Apple: Creating 3D objects from photographs](https://developer.apple.com/documentation/realitykit/creating-3d-objects-from-photographs)
- [Apple: Object Capture workflow](https://developer.apple.com/documentation/realitykit/realitykit-object-capture)
- [Codemagic: Xcode projects](https://docs.codemagic.io/yaml-quick-start/building-a-native-ios-app/)
