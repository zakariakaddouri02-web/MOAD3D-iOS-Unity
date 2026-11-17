# Install a MOAD iOS build with AltServer

## 1. Build the IPA in Codemagic

Run the `ios-build` workflow and download `MOAD3D-unsigned.ipa` from its artifacts.

The Unity activation step must finish before Codemagic can generate the Xcode project. Codemagic's Unity CI documentation requires a Unity Plus or Pro license. Add the account email, password, and matching license serial as secret variables `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL` in the `unity_credentials` group.

If the log says `UnityConnectLoginRequest: Failed to login` or returns HTTP `401`, the Unity credentials were rejected. Check the Unity ID email/password and the serial assigned to that account in Codemagic's secret group. Do not paste secrets into GitHub or the build log. AltServer cannot fix this build-stage error because no IPA exists yet.

## 2. Sign and install with AltServer

The workflow creates an unsigned development IPA. On Windows or Mac, install and open AltServer, connect the iPhone, and trust the computer if prompted. Hold **Shift** on Windows or **Option** on Mac while clicking the AltServer icon, choose **Sideload .ipa…**, select `MOAD3D-unsigned.ipa`, then follow the prompts to sign and install it with your Apple ID. With a free Apple ID, the app needs to be refreshed about every 7 days. ([AltServer guide](https://faq.altstore.io/altstore-classic/altserver)) The unsigned IPA is not ready for TestFlight or the App Store.

Use the official [AltStore Windows installation guide](https://faq.altstore.io/altstore-classic/how-to-install-altstore-windows) or [AltStore macOS installation guide](https://faq.altstore.io/altstore-classic/how-to-install-altstore-macos).

## App flow

After installing, open MOAD 3D Scan, start a scan, move slowly around a textured object, then stop after at least 20 photos or let it reach 150. Tap **Build 3D Model** to reconstruct locally, then **3D Model** to view the result. Image-based reconstruction runs only when RealityKit reports support on the iPhone; this workflow still needs a successful device build and scan to confirm behavior with a faulty LiDAR sensor.

## Apple distribution

For TestFlight or the App Store, configure Apple signing and provisioning in Codemagic and create a separate signed workflow. AltServer sideloading does not publish the app.
