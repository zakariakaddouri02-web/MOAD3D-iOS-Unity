# MOAD 3D SCAN – iOS (Unity) Project

This Unity project implements the same core features as the Android version **without LiDAR**:
- **Flash toggle** (torch) using `AVCaptureDevice`.
- **150‑photo capture mode** with auto‑stop and HUD.
- **GLB export** of the reconstructed mesh (uses `UnityGLTF` library).
- **UI**: simple overlay with three buttons (Flash, 150 Photos, Export GLB).
- **No external network** – all data stays on‑device.

## Project structure (generated)
```
MOAD3D_iOS_Unity/
├─ Assets/
│  └─ Scripts/
│     ├─ CaptureManager.cs      # Handles ARKit session, photo capture, counter
│     ├─ FlashToggle.cs         # Torch control
│     └─ GLBExporter.cs         # Converts Unity mesh to GLB and saves to Documents/MOAD3D
├─ Packages/
│  └─ manifest.json            # Unity package manifest (includes UnityGLTF)
├─ ProjectSettings/
│  └─ ProjectSettings.asset    # Minimal Unity project settings (iOS platform)
└─ README.md                    # This file
```

## How to build for iOS from Windows
1. **Upload the folder** to a cloud‑build service that supports Unity iOS builds (e.g., Unity Cloud Build, GameCI, or a CI service with a macOS runner).
2. Configure the build target:
   - Platform: **iOS**
   - Architecture: **ARM64**
   - Scripting Backend: **IL2CPP** (default for iOS)
   - **Apple Developer** credentials must be supplied to the service for signing.
3. Trigger the build – the service will produce an `.ipa` file.
4. Download the `.ipa` and install it on your iPhone via **TestFlight**, **Diawi**, or **Apple Configurator**.

## Usage on the device
- Launch the app – it immediately requests Camera permission.
- **⚡ Flash** button toggles the torch.
- **🎯 150 Photos** starts the capture mode; a HUD shows count and a progress bar. Capturing stops automatically at 150 images.
- **💾 Export GLB** creates `scan_YYYYMMDD_HHMMSS.glb` in the app’s `Documents/MOAD3D/` folder and opens the iOS share sheet.

## Dependencies
- **ARFoundation** (v5.2+) – provides ARKit session without LiDAR.
- **UnityGLTF** (latest) – for GLB generation.
- No external network libraries are used.

## Extending / customizing
- To change the capture interval, edit `CaptureManager.MIN_PHOTO_INTERVAL_MS`.
- To add a UI slider for speed guidance, modify the HUD in `CaptureManager`.

---
*All source files are located under the artifact directory; you can download the whole project as a zip (see `MOAD3D_iOS_Unity.zip`).*