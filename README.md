# MOAD 3D Scan for iOS

Unity iOS app for object scanning from the rear RGB camera. It captures photos and reconstructs a 3D object on supported iPhones, then opens the result in the in-app viewer.

## Current state

- Codemagic workflow generates an iOS Xcode project and packages an unsigned IPA for sideloading.
- Runtime-created starter UI has Start scan, Stop, Flash, photo count, progress, and movement guidance controls.
- Capture saves up to 150 JPEG frames and a `camera_poses.jsonl` record under `Application.persistentDataPath/MOAD3D/Captures/`.
- The scanner uses ARKit world tracking for camera pose estimates; LiDAR is not enabled or required by this project.
- **Build 3D Model** runs Apple's on-device RealityKit photogrammetry from the captured photos. It writes a USDZ model, then Model I/O exports an OBJ copy for the in-app viewer.
- The 3D Model button opens an interactive OBJ viewer with drag-to-rotate and pinch/scroll-to-zoom controls.
- On iOS, the viewer's **Import OBJ** button opens Files and copies the selected model into `Application.persistentDataPath/MOAD3D/Models/model.obj`.
- Reconstruction requires at least 20 photos; capture overlapping, sharp views around the entire object for better results.
- The app does not capture stereo depth or export GLB. It creates an image-based photogrammetry mesh.

## Image-based reconstruction on iPhone

On an iPhone 12 Pro Max, reconstruction uses the rear RGB images and RealityKit photogrammetry, not a LiDAR depth stream. Apple's API is restricted to select LiDAR-capable iPhones; the app checks `PhotogrammetrySession.isSupported` before starting. Whether this API succeeds on a particular phone with a failed LiDAR sensor still needs confirmation from a device run.

The viewer parses OBJ vertex and face data and uses a simple shaded material. It does not import OBJ materials or texture files. The source USDZ model is also saved under `Application.persistentDataPath/MOAD3D/Models/`.

## Build with Codemagic

1. Add this GitHub repository to Codemagic.
2. In Codemagic, create the `unity_credentials` secret group with `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL` for a valid Unity Plus or Pro license. Codemagic's current Unity build guide requires Plus or Pro for cloud builds.
3. Run the `ios-build` workflow from `codemagic.yaml`.
4. Download `MOAD3D-unsigned.ipa` from build artifacts and sign/install it with your iOS sideloading workflow. An unsigned IPA is not an App Store or TestFlight release.

The workflow uses Unity `2022.3.11f1` and produces an iOS device archive with code signing disabled. It has not yet been verified by a successful Codemagic run. To publish to TestFlight or the App Store, add App Store Connect signing credentials and a signed IPA workflow.

If Unity activation stops with HTTP `401`, Codemagic has rejected the email/password credentials before it can generate the Xcode project. Check the Unity ID email/password and confirm the serial belongs to that account in the Codemagic secret group. The AltServer install step only applies after a successful IPA build.

## Local project

Open the repository root in Unity `2022.3.11f1` with the iOS Build Support module installed. Unity resolves packages from `Packages/manifest.json`. The build script creates a minimal capture scene when it does not exist.

## Device limitations

- Requires an ARKit-capable iPhone and a device where RealityKit Object Capture reports support for on-device reconstruction.
- The user still needs to move around the object and capture overlapping, sharp views with useful texture and lighting.
- Flash availability depends on the physical iPhone model. The torch is controlled through a small AVFoundation bridge.
- Reflective, transparent, textureless, moving, or poorly lit subjects are difficult for photogrammetry.
- Reconstruction can be slow and may fail on photos with too little overlap, blur, reflections, transparency, low texture, or changing lighting.

## References

- [Apple: Creating 3D objects from photographs](https://developer.apple.com/documentation/realitykit/creating-3d-objects-from-photographs)
- [Apple: Object Capture availability and workflow](https://developer.apple.com/documentation/realitykit/realitykit-object-capture)
- [Apple: Capturing photographs for RealityKit Object Capture](https://developer.apple.com/documentation/realitykit/capturing-photographs-for-realitykit-object-capture/)
- [OpenMVG](https://github.com/openMVG/openMVG) — Structure from Motion library, MPL-2.0
- [OpenMVS](https://github.com/cdcseacave/openMVS) — dense reconstruction and mesh library, AGPL-3.0
- [Codemagic: Unity apps](https://docs.codemagic.io/yaml-quick-start/building-a-unity-app/)
