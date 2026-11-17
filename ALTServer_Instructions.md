# Build and sideload MOAD 3D Scan

## Build the iOS app

Run the `ios-build` Codemagic workflow. It compiles the native Swift/Xcode project and produces `MOAD3D-unsigned.ipa`. No Unity Editor, Unity account, or Unity license secrets are used by this workflow.

## Install with AltServer

1. Download the IPA from Codemagic build artifacts.
2. Install and open AltServer on Windows or Mac.
3. Connect the iPhone and trust the computer if prompted.
4. Hold **Shift** on Windows or **Option** on Mac while clicking the AltServer icon.
5. Choose **Sideload .ipa…**, select `MOAD3D-unsigned.ipa`, and follow the prompts to sign and install with your Apple ID.

See the official [AltServer guide](https://faq.altstore.io/altstore-classic/altserver) and [Windows](https://faq.altstore.io/altstore-classic/how-to-install-altstore-windows) or [macOS](https://faq.altstore.io/altstore-classic/how-to-install-altstore-macos) setup instructions. Free Apple ID sideloading requires periodic refresh.

## Scan and preview

Open MOAD 3D Scan, start scanning, and move slowly around a static object with visible texture. Stop after 20 or more overlapping photos, tap **Build 3D**, then tap **Preview 3D model**. The image-based reconstruction runs locally when Apple's RealityKit reports the device as supported. Behavior with faulty LiDAR hardware still needs confirmation on the iPhone 12 Pro Max.

For TestFlight or App Store distribution, configure Apple signing and provisioning in Codemagic and create a signed workflow. The unsigned AltServer IPA is for development sideloading only.
