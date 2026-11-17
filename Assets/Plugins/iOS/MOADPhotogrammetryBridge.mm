#import <Foundation/Foundation.h>
#import "UnityInterface.h"
#import "UnityFramework-Swift.h"

static void MOADSendReconstructionProgress(float progress, NSString *message) {
    NSString *safeMessage = [message stringByReplacingOccurrencesOfString:@"|" withString:@" "];
    NSString *payload = [NSString stringWithFormat:@"%.4f|%@", progress, safeMessage];
    UnitySendMessage("Reconstruction Controller", "OnNativeProgress", [payload UTF8String]);
}

extern "C" int MOADPhotogrammetryIsSupported(void) {
    if (@available(iOS 17.0, *)) {
        return [MOADPhotogrammetryBridge isSupported] ? 1 : 0;
    }
    return 0;
}

extern "C" void MOADPhotogrammetryStart(const char *inputPath,
                                         const char *usdzPath,
                                         const char *objPath) {
    if (@available(iOS 17.0, *)) {
        NSString *input = [NSString stringWithUTF8String:inputPath ?: ""];
        NSString *usdz = [NSString stringWithUTF8String:usdzPath ?: ""];
        NSString *obj = [NSString stringWithUTF8String:objPath ?: ""];
        [MOADPhotogrammetryBridge startWithInputPath:input
                                            usdzPath:usdz
                                             objPath:obj
                                            callback:^(float progress, NSString *message) {
            MOADSendReconstructionProgress(progress, message ?: @"Reconstruction update");
        }];
    } else {
        MOADSendReconstructionProgress(0, @"iOS 17 or later is required for reconstruction.");
    }
}
