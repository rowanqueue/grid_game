using UnityEngine;

namespace EZ.Haptics
{

    public class DefaultVibrationManager : IVibrationManager
    {
        private static bool _enableLogs;

        public static void EnableLogs(bool enable)
        {
            _enableLogs = enable;
        }
        
        public DefaultVibrationManager()
        {
            Debug.Log("Creating Default [PC/Mac] Vibration Manager");
        }

        public void PlaySoft()
        {
            if (_enableLogs) Debug.Log("[PC/Mac] Soft Haptic");
        }

        public void PlayLight()
        {
            if (_enableLogs) Debug.Log("[PC/Mac] Light Haptic");
        }

        public void PlayMedium()
        {
            if (_enableLogs) Debug.Log("[PC/Mac] Medium Haptic");
        }

        public void PlayHeavy()
        {
            if (_enableLogs) Debug.Log("[PC/Mac] Heavy Haptic");
        }

        public void PlayRigid()
        {
            if (_enableLogs) Debug.Log("[PC/Mac] Rigid Haptic");
        }

        public void PlayDouble()
        {
            if (_enableLogs) Debug.Log("[PC/Mac] Double Haptic");
        }

        public void PlayError()
        {
            if (_enableLogs) Debug.Log("[PC/Mac] Error Haptic");
        }

        public void PlayTransient(float intensity, float sharpness)
        {
            if (_enableLogs) Debug.Log($"[PC/Mac] Transient Haptic with Intensity: {intensity}, Sharpness: {sharpness}");
        }
    }

}