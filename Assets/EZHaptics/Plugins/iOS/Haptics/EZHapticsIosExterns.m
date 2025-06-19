#import "EZHapticsIos.h"

void performHaptic(int hapticKey)
{
    [EZHapticsIos performHaptic:hapticKey];
}

void performTransientHaptic(float intensity, float sharpness)
{
    [EZHapticsIos performTransientHaptic:intensity
                                      sharpness:sharpness];
}
