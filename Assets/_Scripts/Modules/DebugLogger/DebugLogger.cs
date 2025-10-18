using System.IO;
using UnityEngine;
using Utilities.Extensions;
using System.Runtime.CompilerServices;
using System;

namespace Modules.Logger
{
    public static class DebugLogger
    {
        public static void Log(string message, [CallerFilePath] string filePath = "")
        {
            string header = GetHeaderFromFilePath(filePath);
            Debug.Log($"<color=cyan>[{header}]</color> -> {message}");
        }

        public static void LogWarning(string message, [CallerFilePath] string filePath = "")
        {
            string header = GetHeaderFromFilePath(filePath);
            Debug.LogWarning($"<color=yellow>[{header}]</color> -> {message}");
        }

        public static void LogError(string message, [CallerFilePath] string filePath = "")
        {
            string header = GetHeaderFromFilePath(filePath);
            Debug.LogError($"<color=red>[{header}]</color> -> {message}");
        }

        public static void LogException(Exception exception, [CallerFilePath] string filePath = "")
        {
            string header = GetHeaderFromFilePath(filePath);
            Debug.LogError($"<color=red>[{header}]</color> -> {exception}");
        }

        public static void LogFormat(string format, params object[] args)
        {
            string message = string.Format(format, args);
            Log(message);
        }

        private static string GetHeaderFromFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "UNKNOWN";

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName.ToEmptySeperated().ToUpper();
        }
    }
}

