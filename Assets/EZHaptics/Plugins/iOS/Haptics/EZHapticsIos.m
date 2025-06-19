#import "EZHapticsIos.h"
#import <AudioToolbox/AudioToolbox.h>
#import <CoreHaptics/CoreHaptics.h>

@implementation EZHapticsIos



+ (void)performHaptic:(int) hapticKey {

    if (hapticKey == 0){ //Soft
        UIImpactFeedbackGenerator *varFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyleSoft)];
        [varFeedback impactOccurred];
        varFeedback = nil;
    } else if (hapticKey == 1){ //
        UIImpactFeedbackGenerator *varFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyleLight)];
        [varFeedback impactOccurred];
        varFeedback = nil;
    } else if (hapticKey == 2){
        UIImpactFeedbackGenerator *varFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyleMedium)];
        [varFeedback impactOccurred];
        varFeedback = nil;
    } else if (hapticKey == 3){
        UIImpactFeedbackGenerator *varFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyleHeavy)];
        [varFeedback impactOccurred];
        varFeedback = nil;
    } else if (hapticKey == 4){
        UIImpactFeedbackGenerator *varFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyleRigid)];
        [varFeedback impactOccurred];
        varFeedback = nil;
    } else if (hapticKey == 5) { 
       UIImpactFeedbackGenerator *varFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyleHeavy)];
       [varFeedback impactOccurred];
       varFeedback = NULL;
       
       dispatch_time_t waitTime = dispatch_time(DISPATCH_TIME_NOW, 0.3 * NSEC_PER_SEC);
       dispatch_after(waitTime, dispatch_get_main_queue(), ^(void){
           [self performHaptic:2];
       });
    } else if (hapticKey == 6) {
            
            UIImpactFeedbackGenerator *varFeedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:(UIImpactFeedbackStyleHeavy)];
            [varFeedback impactOccurred];
            varFeedback = NULL;
            
            dispatch_time_t waitTime = dispatch_time(DISPATCH_TIME_NOW, 0.1 * NSEC_PER_SEC);
            dispatch_after(waitTime, dispatch_get_main_queue(), ^(void){
                [self performHaptic:3];
            });
    }
}

// New method for iOS 13+ custom haptic
+ (void)performTransientHaptic:(float)intensity sharpness:(float)sharpness
{
    // Check iOS version (and device capability):
    if (@available(iOS 13.0, *)) {
        static CHHapticEngine *engine = nil;
        
        // Initialize or reinitialize engine if needed
        if (!engine) {
            NSError *error = nil;
            engine = [[CHHapticEngine alloc] initAndReturnError:&error];
            if (!error) {
                // Setup engine lifecycle handlers
                engine.stoppedHandler = ^(CHHapticEngineStoppedReason reason){
                    NSLog(@"Haptic engine stopped: %ld", (long)reason);
                    engine = nil;  // Reset engine so it will be reinitialized next time
                };
                
                engine.resetHandler = ^{
                    NSLog(@"Haptic engine reset");
                    NSError *startError = nil;
                    [engine startAndReturnError:&startError];
                };
                
                [engine startAndReturnError:&error];
            }
        }
        
        // Create a haptic event with intensity & sharpness
        CHHapticEventParameter *intensityParam =
            [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticIntensity
                                                         value:intensity];
        CHHapticEventParameter *sharpnessParam =
            [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticSharpness
                                                         value:sharpness];

        CHHapticEvent *transientEvent =
        [[CHHapticEvent alloc] initWithEventType:CHHapticEventTypeHapticTransient
                                           parameters:@[intensityParam, sharpnessParam]
                                          relativeTime:0.0];

        NSError *error = nil;
        CHHapticPattern *pattern =
            [[CHHapticPattern alloc] initWithEvents:@[transientEvent] parameters:@[] error:&error];

        // Create a player and start the haptic
        id<CHHapticPatternPlayer> player = [engine createPlayerWithPattern:pattern error:&error];
        [player startAtTime:0 error:&error];
    }
    else {
        // Fallback for older iOS versions
        // e.g., approximate to existing ImpactFeedback styles
        // For instance, map intensity to Light/Medium/Heavy:
        if (intensity < 0.33) {
            UIImpactFeedbackGenerator *lightGen =
              [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            [lightGen impactOccurred];
        } else if (intensity < 0.66) {
            UIImpactFeedbackGenerator *mediumGen =
              [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            [mediumGen impactOccurred];
        } else {
            UIImpactFeedbackGenerator *heavyGen =
              [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            [heavyGen impactOccurred];
        }
    }
}

@end