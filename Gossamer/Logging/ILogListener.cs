using System.Collections.Concurrent;

namespace Gossamer.Logging;

/// <summary>
/// Log listener interface.
/// </summary>
public interface ILogListener
{
    void Append(Log.Event logEvent);
    void Flush() { }
}

/// <summary>
/// Console log listener.
/// </summary>
public sealed class ConsoleLogListener : ILogListener
{
    void ILogListener.Append(Log.Event logEvent)
    {
        Console.WriteLine(logEvent.ToShortString());
    }
}

/// <summary>
/// File log listener.
/// </summary>
public class FileLogListener : ILogListener
{
    readonly Timer saveToFileTimer;

    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024 /* 10 MB */;

    public string Path { get; } = string.Empty;

    readonly ConcurrentQueue<Log.Event> events = [];

    /// <summary>
    /// Creates a new instance of <see cref="Log"/> with the specified output path.
    /// If <paramref name="path"/> points to a directory that does not exist, it will be created.
    /// If <paramref name="path"/> has no extension, .log will be appended to it.
    /// </summary>
    /// <param name="path">The output path.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public FileLogListener(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        Path = path;
        Path = System.IO.Path.GetFullPath(Path);

        string pathExtension = System.IO.Path.GetExtension(Path) ?? string.Empty;
        string pathFileName = System.IO.Path.GetFileName(Path) ?? string.Empty;
        string pathDirectory = System.IO.Path.GetDirectoryName(Path) ?? string.Empty;

        // Ensure the file name is not empty
        if (string.IsNullOrEmpty(pathFileName))
        {
            throw new ArgumentException("Path must contain a file name.", nameof(path));
        }

        // Ensure the path has an extension
        if (string.IsNullOrEmpty(pathExtension))
        {
            Path += ".log";
        }

        // Ensure the directory exists
        if (!Directory.Exists(pathDirectory))
        {
            Directory.CreateDirectory(pathDirectory);
        }

        saveToFileTimer = new Timer(SaveToFileTimer, default, Timeout.Infinite, Timeout.Infinite);
    }

    void SaveToFile()
    {
        lock (events)
        {
            if (events.IsEmpty)
                return;

            try
            {
                try
                {
                    // Limit log file size
                    if (File.Exists(Path) && new FileInfo(Path).Length > MaximumFileSize)
                    {
                        File.Copy(Path, Path + ".old", true);
                        File.Delete(Path);
                    }
                }
                catch { }

                using StreamWriter? file = new(Path, true);

                while (!events.IsEmpty)
                {
                    if (events.TryDequeue(out Log.Event ev))
                    {
                        file.WriteLine(ev);
                    }
                }
            }
            catch { }
        }
    }

    void SaveToFileTimer(object? state)
    {
        SaveToFile();
    }

    void ILogListener.Append(Log.Event logEvent)
    {
        events.Enqueue(logEvent);

        // Since every log event restarts the save timer, log might never be saved to disk
        // Force an immediate save if event count exceeds some constant
        if (events.Count <= 10)
        {
            saveToFileTimer.Change(200, Timeout.Infinite);
        }
        else
        {
            SaveToFile();
        }
    }

    void ILogListener.Flush()
    {
        SaveToFile();
    }
}