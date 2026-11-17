#import <AVFoundation/AVFoundation.h>

extern "C" void MOADSetTorch(int enabled) {
    AVCaptureDevice *device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    if (device == nil || !device.hasTorch) return;

    NSError *error = nil;
    if (![device lockForConfiguration:&error]) return;
    device.torchMode = enabled ? AVCaptureTorchModeOn : AVCaptureTorchModeOff;
    [device unlockForConfiguration];
}
