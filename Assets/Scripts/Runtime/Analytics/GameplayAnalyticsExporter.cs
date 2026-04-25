using System.IO;
using UnityEngine;

public static class GameplayAnalyticsExporter
{
    public static string LogsDirectory => Path.Combine(Application.persistentDataPath, "Logs");

    public static string BuildPath(string fileName)
    {
        Directory.CreateDirectory(LogsDirectory);
        return Path.Combine(LogsDirectory, fileName);
    }

    public static void WriteText(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, contents ?? string.Empty);
    }
}
