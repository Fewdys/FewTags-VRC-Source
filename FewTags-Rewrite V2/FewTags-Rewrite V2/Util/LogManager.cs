namespace FewTags.FewTags
{
    internal class LogManager
    {
        /// <summary>
        /// Logs Message To Console.
        /// </summary>
        internal static void LogToConsole(string message)
        {
            BepInExExample.Log.LogInfo($"[FewTags] {message}");
        }

        internal static void LogToConsole(ConsoleColor color, string message)
        {
            //Log.Msg(color, $"[FewTags] {message}"); // however you log to ml
        }

        /// <summary>
        /// Logs Error To Console.
        /// </summary>
        internal static void LogErrorToConsole(string message)
        {
            BepInExExample.Log.LogError($"[FewTags] {message}");
        }

        /// <summary>
        /// Logs Warning To Console.
        /// </summary>
        internal static void LogWarningToConsole(string message)
        {
            BepInExExample.Log.LogWarning($"[FewTags] {message}");
        }
    }
}
