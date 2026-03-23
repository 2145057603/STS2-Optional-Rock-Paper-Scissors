using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Context;
using Rock.Core;
using System.Text;
using System.Diagnostics;

namespace Rock.Infrastructure;

internal static class RockLog
{
    private static readonly object Sync = new();
    private static readonly string LogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SlayTheSpire2", "logs");

    private static readonly string LogPath = Path.Combine(LogDirectory, "Rock.log");
    private static bool _sessionBannerWritten;

    public static void Info(string message) => Log.Info(Prefix(message));

    public static void Warn(string message) => Log.Warn(Prefix(message));

    public static void Error(string message) => Log.Error(Prefix(message));

    public static void Debug(string message) => Log.Debug(Prefix(message));

    public static void Exception(string context, Exception ex)
    {
        Error($"{context}: {ex}");
    }

    public static void Trace(string area, string message)
    {
        string formatted = $"[{RockModInfo.ModName}]{GetContextPrefix()}[{area}] {message}";
        Log.Info(formatted);
        AppendToFile("TRACE", $"{area} | {message}");
    }

    private static string Prefix(string message)
    {
        AppendToFile("INFO", message);
        return $"[{RockModInfo.ModName}]{GetContextPrefix()} {message}";
    }

    private static string GetContextPrefix()
    {
        string localId = LocalContext.NetId?.ToString() ?? "null";
        int pid = Process.GetCurrentProcess().Id;
        return $"[pid={pid}][local={localId}]";
    }

    private static void AppendToFile(string level, string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);

                using StreamWriter writer = new(LogPath, append: true, Encoding.UTF8);
                if (!_sessionBannerWritten)
                {
                    writer.WriteLine($"===== Rock session {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} =====");
                    _sessionBannerWritten = true;
                }

                writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}");
            }
        }
        catch
        {
            // Logging must never break gameplay.
        }
    }
}
