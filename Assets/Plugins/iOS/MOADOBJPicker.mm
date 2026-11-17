#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import "UnityInterface.h"

@interface MOADOBJPickerDelegate : NSObject <UIDocumentPickerDelegate>
@property(nonatomic, copy) NSString *destinationFolder;
@end

@implementation MOADOBJPickerDelegate
- (void)documentPicker:(UIDocumentPickerViewController *)controller
didPickDocumentsAtURLs:(NSArray<NSURL *> *)urls {
    NSURL *source = urls.firstObject;
    if (source == nil) {
        UnitySendMessage("MOAD App", "OnOBJImportResult", "No file selected.");
        return;
    }
    if (![source.pathExtension.lowercaseString isEqualToString:@"obj"]) {
        UnitySendMessage("MOAD App", "OnOBJImportResult", "Select a .obj file.");
        return;
    }

    BOOL accessing = [source startAccessingSecurityScopedResource];
    NSError *error = nil;
    NSString *destination = [self.destinationFolder stringByAppendingPathComponent:@"model.obj"];
    [[NSFileManager defaultManager] createDirectoryAtPath:self.destinationFolder
                              withIntermediateDirectories:YES attributes:nil error:&error];
    if (error == nil && [[NSFileManager defaultManager] fileExistsAtPath:destination])
        [[NSFileManager defaultManager] removeItemAtPath:destination error:&error];
    if (error == nil)
        [[NSFileManager defaultManager] copyItemAtURL:source toURL:[NSURL fileURLWithPath:destination] error:&error];
    if (accessing) [source stopAccessingSecurityScopedResource];

    if (error == nil) {
        UnitySendMessage("MOAD App", "OnOBJImportResult", "OK");
    } else {
        const char *message = error.localizedDescription.UTF8String;
        UnitySendMessage("MOAD App", "OnOBJImportResult", message ?: "Could not copy the selected model.");
    }
}

- (void)documentPickerWasCancelled:(UIDocumentPickerViewController *)controller {
    UnitySendMessage("MOAD App", "OnOBJImportResult", "Selection cancelled.");
}
@end

static MOADOBJPickerDelegate *MOADPickerDelegate;

extern "C" void MOADPickOBJ(const char *destinationFolder) {
    NSString *folder = [NSString stringWithUTF8String:destinationFolder ?: ""];
    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController *presenter = UnityGetGLViewController();
        if (presenter == nil) {
            UnitySendMessage("MOAD App", "OnOBJImportResult", "Could not open Files.");
            return;
        }
        MOADPickerDelegate = [MOADOBJPickerDelegate new];
        MOADPickerDelegate.destinationFolder = folder;
        UIDocumentPickerViewController *picker = [[UIDocumentPickerViewController alloc]
            initWithDocumentTypes:@[@"public.data"] inMode:UIDocumentPickerModeImport];
        picker.delegate = MOADPickerDelegate;
        picker.modalPresentationStyle = UIModalPresentationFormSheet;
        [presenter presentViewController:picker animated:YES completion:nil];
    });
}
