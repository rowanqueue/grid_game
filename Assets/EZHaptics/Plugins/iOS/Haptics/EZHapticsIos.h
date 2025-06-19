

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

NS_ASSUME_NONNULL_BEGIN

@interface EZHapticsIos : NSObject

+ (void)performHaptic:(int) hapticKey;
+ (void)performTransientHaptic:(float)intensity sharpness:(float)sharpness;

@end

NS_ASSUME_NONNULL_END
