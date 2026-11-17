# Install the unsigned iOS development build

Codemagic's `ios-build` workflow creates `MOAD3D-unsigned.ipa`. This is an unsigned development build for sideloading; it is not ready for TestFlight or the App Store. AltStore, Sideloadly, or another signing workflow may re-sign it with your own Apple development identity. Follow the current instructions from the signing tool you choose.

## Build artifact

1. Run the `ios-build` workflow in Codemagic.
2. Download `MOAD3D-unsigned.ipa` from the build artifacts.
3. Sign and install it using your own Apple ID/development signing setup.

The workflow requires Unity license secrets configured in Codemagic under the environment group `unity_credentials`: `UNITY_EMAIL`, `UNITY_PASSWORD`, and `UNITY_SERIAL`. Never put these values in this repository.

## Current app behavior

The current build is a camera-only capture foundation. It can capture and locally save photos and camera poses. It does not yet reconstruct a mesh, preview a 3D model, or export GLB. The on-device photogrammetry engine is still to be integrated and verified on a physical iPhone without LiDAR.

## App Store / TestFlight

For a signed App Store or TestFlight build, configure App Store Connect signing credentials and provisioning in Codemagic, then add a dedicated signed workflow. The current unsigned IPA workflow does not upload or publish the app.
