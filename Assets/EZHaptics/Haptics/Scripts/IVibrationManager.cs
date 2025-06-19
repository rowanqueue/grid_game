namespace EZ.Haptics
{
    
    public interface IVibrationManager
    {
        public void PlaySoft();
        public void PlayLight();
        public void PlayMedium();
        public void PlayHeavy();
        public void PlayRigid();
        public void PlayDouble();
        public void PlayError();
        public void PlayTransient(float intensity, float sharpness);
    }


}