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
/// Log listener that writes log events to the console.
/// </summary>
public sealed class ConsoleLogListener : ILogListener
{
    void ILogListener.Append(Log.Event logEvent)
    {
        Console.WriteLine(logEvent.ToShortString());
    }
}

/// <summary>
/// Log listener that writes log events to a file.
/// </summary>
public class FileLogListener : ILogListener
{
    readonly Timer outputTimer;
    readonly StreamWriter outputWriter;
    readonly Lock outputLock = new();

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

        outputTimer = new Timer(SaveToFileTimer, default, Timeout.Infinite, Timeout.Infinite);
        outputWriter = new StreamWriter(Path, true);
    }

    void SaveToFile()
    {
        using (outputLock.EnterScope())
        {
            if (events.IsEmpty)
                return;

            try
            {
                while (!events.IsEmpty)
                {
                    LimitFileSize();
                    if (events.TryDequeue(out Log.Event ev))
                    {
                        outputWriter.WriteLine(ev.ToString());
                    }
                }

                LimitFileSize();
            }
            catch { }
        }

        void LimitFileSize()
        {
            try
            {
                if (outputWriter.BaseStream.Length > MaximumFileSize)
                {
                    File.Copy(Path, Path + ".old", true);
                    outputWriter.BaseStream.SetLength(0);
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
        if (events.Count > 10)
        {
            SaveToFile();
        }
        else
        {
            outputTimer.Change(200, Timeout.Infinite);
        }
    }

    void ILogListener.Flush()
    {
        SaveToFile();
    }
}