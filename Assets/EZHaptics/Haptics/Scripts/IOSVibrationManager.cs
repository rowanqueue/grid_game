namespace EZ.Haptics
{
    public class IOSVibrationManager : IVibrationManager
    {
        public void PlaySoft()
        {
            IOSFrameworkBridge.PlayHaptic(0);
        }

        public void PlayLight()
        {
            IOSFrameworkBridge.PlayHaptic(1);
        }

        public void PlayMedium()
        {
            IOSFrameworkBridge.PlayHaptic(2);
        }

        public void PlayHeavy()
        {
            IOSFrameworkBridge.PlayHaptic(3);
        }

        public void PlayRigid()
        {
            IOSFrameworkBridge.PlayHaptic(4);
        }

        public void PlayDouble()
        {
            IOSFrameworkBridge.PlayHaptic(5);
        }
        
        public void PlayError()
        {
            IOSFrameworkBridge.PlayHaptic(6);
        }
        
        public void PlayTransient(float intensity, float sharpness)
        {
            IOSFrameworkBridge.PlayTransientHaptic(intensity, sharpness);
        }
    }
}