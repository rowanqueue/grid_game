using UnityEngine;

namespace EZ.Haptics
{

    public class AndroidVibrationManager : IVibrationManager
    {
        private readonly AndroidJavaObject _contextObject;

        public AndroidVibrationManager()
        {
            
            //Create class and set context.
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _contextObject = new AndroidJavaObject("EZHapticsAndroid");
            _contextObject.Set("ctx", currentActivity);
        }


        public void PlaySoft()
        {
            _contextObject.Call("playHapticSoft");
        }

        public void PlayLight()
        {
            _contextObject.Call("playHapticLight");
        }

        public void PlayMedium()
        {
            _contextObject.Call("playHapticMedium");
        }

        public void PlayHeavy()
        {
            _contextObject.Call("playHapticHeavy");
        }

        public void PlayRigid()
        {
            _contextObject.Call("playHapticRigid");
        }

        public void PlayDouble()
        {
            _contextObject.Call("playHapticDouble");
        }
        
        public void PlayError()
        {
            _contextObject.Call("playHapticError");
        }
        
        public void PlayTransient(float intensity, float sharpness)
        {
            _contextObject.Call("playHapticTransient", intensity, sharpness);
        }
    }
}
