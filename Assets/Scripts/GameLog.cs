using UnityEngine;

public static class GameLog
{
    public static void Log(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(message);
#endif
    }

    public static void LogWarning(string message)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogWarning(message);
#endif
    }

    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}
