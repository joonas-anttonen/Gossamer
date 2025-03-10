using System.Collections.Concurrent;

using Gossamer.Utilities;

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
        string msg = logEvent.ToShortString();

        Console.ForegroundColor = logEvent.Severity switch
        {
            Log.Severity.Debug => ConsoleColor.Blue,
            Log.Severity.Information => ConsoleColor.White,
            Log.Severity.Warning => ConsoleColor.Yellow,
            Log.Severity.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        Console.WriteLine(msg);
        Console.ResetColor();
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
    readonly ConcurrentQueue<Log.Event> events = [];

    /// <summary>
    /// The output file path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// The maximum output file size in bytes.
    /// </summary>
    public long OutputMaximumSize { get; }

    /// <summary>
    /// Creates a new instance of <see cref="Log"/> with the specified output path.
    /// If <paramref name="path"/> points to a directory that does not exist, it will be created.
    /// </summary>
    /// <param name="path"> The output file path. </param>
    /// <param name="maximumFileSize"> The maximum output file size in bytes. </param>
    /// <exception cref="ArgumentException"></exception>
    public FileLogListener(string path, long maximumFileSize = 10 * 1024 * 1024)
    {
        ExceptionUtilities.ThrowArgumentIfNullOrEmpty(path);
        ExceptionUtilities.ThrowArgumentIfNullOrEmpty(Path.GetFileName(path));

        OutputPath = path;
        OutputPath = Path.GetFullPath(OutputPath);
        OutputMaximumSize = maximumFileSize;

        // Ensure the directory exists
        string pathDirectory = Path.GetDirectoryName(OutputPath) ?? string.Empty;

        if (!Directory.Exists(pathDirectory))
        {
            Directory.CreateDirectory(pathDirectory);
        }

        outputTimer = new Timer(SaveToFileTimer, default, Timeout.Infinite, Timeout.Infinite);
        outputWriter = new StreamWriter(OutputPath, true);
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
                if (outputWriter.BaseStream.Length > OutputMaximumSize)
                {
                    File.Copy(OutputPath, OutputPath + ".old", true);
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