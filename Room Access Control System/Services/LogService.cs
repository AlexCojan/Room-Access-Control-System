using System;
using System.IO;
using System.Linq;

namespace Room_Access_Control_System.Services
{
    public static class LogService
    {
        // Logs in: Documents\RoomAccessLogs
        private static readonly string LogRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "RoomAccessLogs");

        public static string GetLogFilePath(DateTime simNow)
        {
            Directory.CreateDirectory(LogRoot);
            var fileName = $"room_access_log_{simNow:yyyy-MM-dd}.txt";
            return Path.Combine(LogRoot, fileName);
        }

        // --- Public API -------------------------------------------------------

        public static void Append(DateTime simNow, string message)
        {
            var path = GetLogFilePath(simNow);
            EnsureDailyHeader(simNow, path);

            var line = $"{simNow:yyyy-MM-dd HH:mm:ss} {message}";
            File.AppendAllText(path, line + Environment.NewLine);
        }

        public static void LogModeChange(DateTime simNow, string modeText)
        {
            var path = GetLogFilePath(simNow);
            EnsureDailyHeader(simNow, path);

            var timeLine = $"[{simNow:HH:mm:ss dd/MM/yyyy}]";
            var msgLine = $"{modeText} initiated";

            var block = BuildBox(timeLine, msgLine);
            File.AppendAllText(path, block + Environment.NewLine);
        }

        // --- Helpers ----------------------------------------------------------

        private static void EnsureDailyHeader(DateTime simNow, string path)
        {
            if (File.Exists(path)) return;

            // Make sure parent directory exists (C# 7.3 friendly)
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var dateText = simNow.ToString("dd.MM.yyyy");
            // Simple date banner:
            // ===================
            // ==  29.08.2025  ==
            // ===================
            string middle = $"==  {dateText}  ==";
            string border = new string('=', middle.Length);

            var header = border + Environment.NewLine
                       + middle + Environment.NewLine
                       + border + Environment.NewLine + Environment.NewLine;

            File.WriteAllText(path, header);
        }

        private static string BuildBox(params string[] innerLines)
        {
            // Determine width by the longest inner line
            int maxInner = innerLines.Max(s => s.Length);

            string Border(int w) { return new string('=', w + 6); }          // "== " + w + " =="
            string Pad(string s) { return "== " + s.PadRight(maxInner) + " =="; }

            var top = Border(maxInner);
            var lines = string.Join(Environment.NewLine, innerLines.Select(Pad));
            var bottom = Border(maxInner);

            return top + Environment.NewLine + lines + Environment.NewLine + bottom + Environment.NewLine;
        }
    }
}
