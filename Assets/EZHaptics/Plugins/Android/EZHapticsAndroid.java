import android.app.Activity;
import android.content.Context;
import android.os.Build;
import android.os.Handler;
import android.os.VibrationEffect;
import android.os.Vibrator;
import android.util.Log;
import android.view.HapticFeedbackConstants;
import android.view.View;

class EZHapticsAndroid {

    public Context ctx;
    

    private void playHapticSoft() {
        View rootView = ((Activity)ctx).findViewById(android.R.id.content);
        rootView.performHapticFeedback(HapticFeedbackConstants.CLOCK_TICK);
    }

    private void playHapticLight() {
        View rootView = ((Activity)ctx).findViewById(android.R.id.content);
        rootView.performHapticFeedback(HapticFeedbackConstants.CONTEXT_CLICK);
    }

    private void playHapticMedium() {
        View rootView = ((Activity)ctx).findViewById(android.R.id.content);
        rootView.performHapticFeedback(HapticFeedbackConstants.VIRTUAL_KEY);
    }

    private void playHapticHeavy() {
        View rootView = ((Activity)ctx).findViewById(android.R.id.content);
        rootView.performHapticFeedback(HapticFeedbackConstants.VIRTUAL_KEY);
    }

    private void playHapticRigid() {
        View rootView = ((Activity)ctx).findViewById(android.R.id.content);
        rootView.performHapticFeedback(HapticFeedbackConstants.CONFIRM);
    }

    private void playHapticDouble() {
        playHapticMedium();
        
        final Handler handler = new Handler();
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                playHapticMedium();
            }
        }, 300); //ms
    }

    private void playHapticError() {
        View rootView = ((Activity)ctx).findViewById(android.R.id.content);
        rootView.performHapticFeedback(HapticFeedbackConstants.REJECT);
    }
    
   public void playHapticTransient(float intensity, float sharpness) {
        // 1) Get Vibrator service from the Android context
        Vibrator vibrator = (Vibrator) ctx.getSystemService(Context.VIBRATOR_SERVICE);
        if (vibrator == null) {
            Log.e("EZHapticsAndroid", "Vibrator service not found on device!");
            return;
        }

        // 2) Convert intensity [0..1] → amplitude [1..255].
        //    If intensity is out of bounds, clamp it to [0..1].
        //    Then map to [1..255] for amplitude-based vibrations on Android.
        if (intensity < 0) intensity = 0;
        if (intensity > 1) intensity = 1;
        int amplitude = (int) (intensity * 255f);
        if (amplitude < 1) amplitude = 1;  // 0 amplitude = silent

        // 3) Approximate "sharpness" as a short vs. longer duration.
        //    For example: 30 ms + up to 70 ms for sharper "punch".
        //    You can tweak this as you wish (e.g. 10..150 ms).
        if (sharpness < 0) sharpness = 0;
        if (sharpness > 1) sharpness = 1;
        long durationMs = (long) (30 + 70 * sharpness);  // range ~30..100 ms

        // 4) If API >= 26, use VibrationEffect (which supports amplitude).
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            VibrationEffect effect = VibrationEffect.createOneShot(durationMs, amplitude);
            vibrator.vibrate(effect);
        } else {
            // Fallback for older devices that do not support amplitude control.
            // This still vibrates for the correct duration, but ignores amplitude.
            vibrator.vibrate(durationMs);
        }
    }

}