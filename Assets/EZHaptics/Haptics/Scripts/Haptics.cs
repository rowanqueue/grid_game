using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace EZ.Haptics
{
    
    public static class Haptics
    {
        private static bool _isInitialized;
        private static bool _hapticsEnabled = true;
        private static IVibrationManager _vibrationManager;

        private static bool _useNumbers;
        public static int HapticNumber { get; private set; }
        
        /// <summary>
        /// If true, Haptics can range in strength from 0-4, with 3 being default.
        /// </summary>
        /// <param name="useNumbers"></param>
        public static void EnableHapticStrengthChange(bool useNumbers)
        {
            _useNumbers = useNumbers;
        }
        

        /// <summary>
        /// 3 is default, 4 is max, 0 is off.
        /// </summary>
        public static void SetHapticStrength(int num)
        {
            HapticNumber = num;
            if (num == 0) _hapticsEnabled = false;
            else _hapticsEnabled = true;
        }
        
        /// <summary>
        /// Ensures that the VibrationManager is created before calling haptic functions.
        /// </summary>
        private static void CheckForInitialization()
        {
            if (_isInitialized) return;
            
            #if UNITY_IOS && !UNITY_EDITOR
                _vibrationManager = new IOSVibrationManager(); //iOS
            #elif UNITY_ANDROID && !UNITY_EDITOR
                _vibrationManager = new AndroidVibrationManager(); //Android
            #else
                _vibrationManager = new DefaultVibrationManager(); //PC/Mac
            #endif

            
            _isInitialized = true;
        }

        public static void SetEnabled(bool hapticsEnabled)
        {
            _hapticsEnabled = hapticsEnabled;
        }

        public static void PlayHaptic(HapticType hapticType)
        {
            if (!_hapticsEnabled || (_useNumbers && HapticNumber == 0)) return;
            
            if (HapticNumber < 2 && hapticType == HapticType.Medium) hapticType = HapticType.Light;
            if (HapticNumber < 3 && hapticType == HapticType.Heavy) hapticType = HapticType.Medium;
            if (HapticNumber < 4 && hapticType == HapticType.Rigid) hapticType = HapticType.Heavy;
            if (HapticNumber > 3 && hapticType == HapticType.Medium) hapticType = HapticType.Rigid;
            
#if UNITY_IOS && !UNITY_EDITOR
            Version currentVersion = new Version(Device.systemVersion);
            Version ios13 = new Version("13.0");
            // Haptics cause crashes on iOS 12
            if(currentVersion < ios13) return;
#endif
            
            CheckForInitialization();
            
            switch (hapticType)
            {
                case HapticType.Soft:
                    _vibrationManager.PlaySoft();
                    break;
                case HapticType.Light:
                    _vibrationManager.PlayLight();
                    break;
                case HapticType.Medium:
                    _vibrationManager.PlayMedium();
                    break;
                case HapticType.Heavy:
                    _vibrationManager.PlayHeavy();
                    break;
                case HapticType.Rigid:
                    _vibrationManager.PlayRigid();
                    break;
                case HapticType.Double:
                    _vibrationManager.PlayDouble();
                    break;
                case HapticType.Error:
                    _vibrationManager.PlayError();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hapticType), hapticType, null);
            }
        }
        
        
        public static void PlayTransient(float intensity, float sharpness)
        {
            if (!_hapticsEnabled || (_useNumbers && HapticNumber == 0)) return;

            float intensityMult = 1;
            if (HapticNumber == 1) intensityMult = .33f;
            if (HapticNumber == 2) intensityMult = .66f;
            if (HapticNumber == 4) intensityMult = 1.5f;
            
            intensity *= intensityMult;
            

#if UNITY_IOS && !UNITY_EDITOR
            // On iOS <= 12, we skip haptics (same logic as existing code).
            Version currentVersion = new Version(UnityEngine.iOS.Device.systemVersion);
            Version ios13 = new Version("13.0");
            if (currentVersion < ios13) return;
#endif
            
            CheckForInitialization();
            _vibrationManager.PlayTransient(intensity, sharpness);
        }
    }

    public enum HapticType
    {
        Soft,
        Light,
        Medium,
        Heavy,
        Rigid,
        Double,
        Error
    }

}
