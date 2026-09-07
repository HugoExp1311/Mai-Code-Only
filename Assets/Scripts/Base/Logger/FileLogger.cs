using UnityEngine;
using System.IO;
using System;

namespace Base.Logger
{
    public static class FileLogger
    {
        private static string logFilePath;
        private static StreamWriter streamWriter;

        public static void Initialize(string fileNamePrefix = "DialogueImportLog")
        {
            string logsDirectory = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(logsDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(logsDirectory, $"{fileNamePrefix}_{timestamp}.txt");

            try
            {
                streamWriter = new StreamWriter(logFilePath, false);
                streamWriter.AutoFlush = true;
            }
            catch (Exception)
            {
                streamWriter = null;
            }
        }

        public static void Log(string message)
        {
            if (streamWriter != null)
            {
                string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                streamWriter.WriteLine(logEntry);
            }
        }

        public static void Close()
        {
            if (streamWriter != null)
            {
                streamWriter.Close();
                streamWriter.Dispose();
                streamWriter = null;
            }
        }
    }
}