# MOAD 3D Scan for iOS

Unity iOS starter project for camera-based scanning. It captures camera frames and ARKit camera poses and stores them locally. The app now includes a basic interactive 3D viewer for local Wavefront OBJ meshes.

## Current state

- Codemagic workflow generates an iOS Xcode project and packages an unsigned IPA for sideloading.
- Runtime-created starter UI has Start scan, Stop, Flash, photo count, progress, and movement guidance controls.
- Capture saves up to 150 JPEG frames and a `camera_poses.jsonl` record under `Application.persistentDataPath/MOAD3D/Captures/`.
- The scanner uses ARKit world tracking for camera pose estimates; LiDAR is not enabled or required by this project.
- The 3D Model button opens an interactive OBJ viewer with drag-to-rotate and pinch/scroll-to-zoom controls.
- On iOS, the viewer's **Import OBJ** button opens Files and copies the selected model into `Application.persistentDataPath/MOAD3D/Models/model.obj`.
- The capture pipeline does not yet generate a mesh, capture stereo depth, or export GLB. Viewing an OBJ is separate from scanning/reconstruction.

## Mesh reconstruction without LiDAR

The intended workflow is on-device photogrammetry: match visual features across saved photos, estimate camera poses, build a dense point cloud, then reconstruct and texture a mesh. The iOS capture app does not contain that pipeline yet.

On an iPhone 12 Pro Max, AVFoundation can provide relative depth from the rear dual-camera system in supported capture formats; this is stereo disparity, not LiDAR depth. Apple documents that dual-camera depth has relative accuracy. This project has not yet integrated that capture path, so the current scanner still saves RGB frames and ARKit poses only. A compatible RGB-D or photogrammetry reconstruction engine still needs to be integrated and tested on the phone before the app can claim end-to-end offline scanning.

The viewer currently parses OBJ vertex and face data and uses a simple shaded material. It does not import OBJ materials or texture files. The scan pipeline does not yet create the OBJ file; import an existing OBJ from Files to use the viewer.

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
- [Apple: Capturing photos with depth](https://developer.apple.com/documentation/avfoundation/capturing-photos-with-depth)
- [OpenMVG](https://github.com/openMVG/openMVG) — Structure from Motion library, MPL-2.0
- [OpenMVS](https://github.com/cdcseacave/openMVS) — dense reconstruction and mesh library, AGPL-3.0
- [Codemagic: Unity apps](https://docs.codemagic.io/yaml-quick-start/building-a-unity-app/)
