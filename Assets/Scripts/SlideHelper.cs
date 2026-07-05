using System.Collections;
using UnityEngine;

public static class SlideHelper
{
    public enum EaseType
    {
        Linear,
        EaseOutQuad,
        EaseInOutQuad
    }

    public static float ApplyEase(float t, EaseType ease)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case EaseType.EaseOutQuad:
                return 1f - (1f - t) * (1f - t);
            case EaseType.EaseInOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            default:
                return t;
        }
    }

    public static IEnumerator SlideLocalX(Transform target, float toX, float duration, EaseType ease = EaseType.EaseOutQuad)
    {
        if (target == null)
            yield break;

        Vector3 start = target.localPosition;
        Vector3 end = new Vector3(toX, start.y, start.z);
        if (duration <= 0f)
        {
            target.localPosition = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ApplyEase(elapsed / duration, ease);
            target.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }

        target.localPosition = end;
    }

    public static IEnumerator SlideLocalY(Transform target, float toY, float duration, EaseType ease = EaseType.EaseOutQuad)
    {
        if (target == null)
            yield break;

        Vector3 start = target.localPosition;
        Vector3 end = new Vector3(start.x, toY, start.z);
        if (duration <= 0f)
        {
            target.localPosition = end;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ApplyEase(elapsed / duration, ease);
            target.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }

        target.localPosition = end;
    }

    public static IEnumerator SlideWorldPosition(Transform target, Vector3 toPosition, float duration, EaseType ease = EaseType.EaseOutQuad)
    {
        if (target == null)
            yield break;

        Vector3 start = target.position;
        if (duration <= 0f)
        {
            target.position = toPosition;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ApplyEase(elapsed / duration, ease);
            target.position = Vector3.Lerp(start, toPosition, t);
            yield return null;
        }

        target.position = toPosition;
    }
}
