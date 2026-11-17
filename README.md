# MOAD 3D Scan for iOS

Unity iOS starter project for camera-only scanning. The first build target is an iPhone app that captures real camera frames and ARKit camera poses without requiring LiDAR, and writes them locally. It does **not** yet reconstruct those images into a mesh or export GLB.

## Current state

- Codemagic workflow generates an iOS Xcode project and packages an unsigned IPA for sideloading.
- Runtime-created starter UI has Start scan, Stop, Flash, photo count, progress, and movement guidance controls.
- Capture saves up to 150 JPEG frames and a `camera_poses.jsonl` record under `Application.persistentDataPath/MOAD3D/Captures/`.
- The scanner uses ARKit world tracking for camera pose estimates; LiDAR is not enabled or required by this project.
- The processing stage is not implemented yet. A saved photo set is not a mesh, and the app does not currently offer GLB export or a model viewer.

## Mesh reconstruction without LiDAR

The intended workflow is on-device photogrammetry: match visual features across the saved photos, estimate camera poses, build a dense point cloud, then reconstruct and texture a mesh. The iOS starter captures camera poses but does not contain that pipeline yet.

Apple's RealityKit Object Capture reconstruction is available only on select iOS devices with LiDAR, so it is not a general solution for camera-only iPhones. OpenMVG can provide Structure from Motion components and is MPL-2.0, but it does not alone provide the complete dense textured mesh pipeline. OpenMVS provides dense reconstruction and mesh stages but is AGPL-3.0 and has no ready-to-use iOS build in this project. A compatible engine still needs to be ported, integrated, and verified on physical non-LiDAR iPhones before this app can claim end-to-end offline scanning.

## Build with Codemagic

1. Add this GitHub repository to Codemagic.
2. In the Codemagic app's environment variable groups, create `unity_credentials` and add secret variables `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL` for a Unity license that can activate on a CI machine.
3. Run the `ios-build` workflow from `codemagic.yaml`.
4. Download `MOAD3D-unsigned.ipa` from build artifacts and sign/install it with your iOS sideloading workflow. An unsigned IPA is not an App Store or TestFlight release.

The workflow uses Unity `2022.3.11f1` and produces an iOS device archive with code signing disabled. It has not yet been verified by a successful Codemagic run. To publish to TestFlight or the App Store, add App Store Connect signing credentials and a signed IPA workflow.

## Local project

Open the repository root in Unity `2022.3.11f1` with the iOS Build Support module installed. Unity resolves packages from `Packages/manifest.json`. The build script creates a minimal capture scene when it does not exist.

## Device limitations

- Requires an ARKit-capable iPhone for camera pose tracking; this is not LiDAR mesh reconstruction.
- The user still needs to move around the object and capture overlapping, sharp views with useful texture and lighting.
- Flash availability depends on the physical iPhone model. The torch is controlled through a small AVFoundation bridge.
- Reflective, transparent, textureless, moving, or poorly lit subjects are difficult for photogrammetry.
- Until an on-device reconstruction engine is added, this project is a capture/build foundation, not a complete 3D scanner.

## References

- [Apple: Creating 3D objects from photographs](https://developer.apple.com/documentation/realitykit/creating-3d-objects-from-photographs)
- [OpenMVG](https://github.com/openMVG/openMVG) — Structure from Motion library, MPL-2.0
- [OpenMVS](https://github.com/cdcseacave/openMVS) — dense reconstruction and mesh library, AGPL-3.0
- [Codemagic: Unity apps](https://docs.codemagic.io/yaml-quick-start/building-a-unity-app/)
