using UnityEngine;

namespace LastDayLostCameraFix
{
    public class DebugCW
    {
        public static void Log(string message)
        {
            Debug.Log($"[{DateTime.Now}] [{LogType.Log}] [MOD] {message}");
        }
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[{DateTime.Now}] [{LogType.Warning}] [MOD] {message}");
        }
        public static void LogError(string message)
        {
            Debug.LogError($"[{DateTime.Now}] [{LogType.Error}] [MOD] {message}");
        }
        public static void LogException(Exception exception)
        {
            Debug.LogError($"[{DateTime.Now}] [{LogType.Error}] [MOD] Exception: {exception.Message}");
            Debug.LogException(exception);
        }
    }
}
