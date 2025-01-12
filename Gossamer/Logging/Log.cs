using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Gossamer.Logging;

public interface ILogListener
{
    void Append(Log.Event logEvent);
    void Flush();
}

public sealed class ConsoleLogListener : ILogListener
{
    public void Append(Log.Event logEvent)
    {
        Console.WriteLine(logEvent.ToShortString());
    }

    void ILogListener.Flush()
    {
    }
}

public class FileLogListener : ILogListener
{
    readonly Timer saveToFileTimer;

    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024 /* 10 MB */;

    public string Path { get; } = string.Empty;

    void ILogListener.Flush()
    {
        SaveToFile();
    }

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

    public void SaveToFile()
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
                    if (events.TryDequeue(out Log.Event? ev) && ev != null)
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
}

/// <summary>
/// Logging system for Gossamer.
/// </summary>
public sealed class Log : IDisposable
{
    bool isDisposed;

    /// <summary>
    /// Log levels.
    /// </summary>
    public enum Level
    {
        Error,
        Warning,
        Information,
        Debug
    }

    /// <summary>
    /// Log event.
    /// </summary>
    /// <param name="Level">Log level.</param>
    /// <param name="Message">Log message.</param>
    /// <param name="Timestamp">Log timestamp.</param>
    public record class Event(Level Level, string Message, DateTime Timestamp)
    {
        public string ToShortString()
        {
            return Message;
        }

        public override string ToString()
        {
            // ISO 8601
            string timestamp = Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);

            return $"[{timestamp}] [{LevelToString(Level)}] {Message}";
        }

        static string LevelToString(Level level)
        {
            return level switch
            {
                Level.Error => "ERROR",
                Level.Warning => "WARNING",
                Level.Information => "INFO",
                Level.Debug => "DEBUG",
                _ => throw new GossamerException(),
            };
        }
    }

    /// <summary>
    /// Log event handler delegate.
    /// </summary>
    /// <param name="logEvent">Log event.</param>
    public delegate void LogEventHandler(Event logEvent);

    readonly ConcurrentBag<ILogListener> listeners = [];

    readonly ConcurrentDictionary<string, Logger> loggers = [];

    public IReadOnlyDictionary<string, Logger> Loggers => loggers;

    public Log()
    {
    }

    public ILogListener AddConsoleListener()
    {
        ILogListener listener = new ConsoleLogListener();
        listeners.Add(listener);
        return listener;
    }

    public ILogListener AddFileListener(string path)
    {
        ILogListener listener = new FileLogListener(path);
        listeners.Add(listener);
        return listener;
    }

    public void AddListener(ILogListener listener)
    {
        listeners.Add(listener);
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        isDisposed = true;

        foreach (ILogListener listener in listeners)
        {
            listener.Flush();
        }
    }

    public Logger GetLogger(string name)
    {
        if (loggers.TryGetValue(name, out Logger? logger))
        {
            return logger;
        }

        logger = new Logger(this, name);
        loggers.TryAdd(name, logger);
        return logger;
    }

    /// <summary>
    /// Logs a message to the global application log.
    /// Use <see cref="GetLogger(string, bool, bool)"/> to get a instance of <see cref="Logger"/> which can be easier to use.
    /// </summary>
    /// <param name="message">Message to log.</param>
    /// <param name="typeName">Name of the type that the message originated from.</param>
    /// <param name="callerName">Name of the caller that the message originated from.</param>
    public void Append(Level level, string message, DateTime timestamp, string typeName = "", [CallerMemberName] string callerName = "")
    {
        const string TypedCallerFormat = "{0}::{1} {2}";
        const string TypedFormat = "{0} {1}";
        const string PlainFormat = "{0}";

        string logMsg;
        Event logEvent;

        if (string.IsNullOrEmpty(typeName))
        {
            if (string.IsNullOrEmpty(callerName))
            {
                logMsg = string.Format(CultureInfo.InvariantCulture, PlainFormat, message);
            }
            else
            {
                logMsg = string.Format(CultureInfo.InvariantCulture, TypedFormat, callerName, message);
            }
        }
        else if (string.IsNullOrEmpty(callerName))
        {
            logMsg = string.Format(CultureInfo.InvariantCulture, TypedFormat, typeName, message);
        }
        else
        {
            logMsg = string.Format(CultureInfo.InvariantCulture, TypedCallerFormat, typeName, callerName, message);
        }

        logEvent = new Event(level, logMsg, timestamp);

        try
        {
            foreach (ILogListener listener in listeners)
            {
                listener.Append(logEvent);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, TypedCallerFormat, nameof(Log), nameof(Append), e.Message));
        }
    }
}

public class Logger(Log applicationLog, string name)
{
    readonly Log log = applicationLog;
    readonly string name = name;

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Error(string message = "")
    {
        log.Append(Log.Level.Error, message, DateTime.Now, string.Empty, string.Empty);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Warning(string message = "")
    {
        log.Append(Log.Level.Warning, message, DateTime.Now, string.Empty, string.Empty);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Information(string message = "")
    {
        log.Append(Log.Level.Information, message, DateTime.Now, string.Empty, string.Empty);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Debug(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Level.Debug, message, DateTime.Now, name, callerName);
    }
}