using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Gossamer;

public interface ILogListener
{
    void Append(GossamerLog.Event logEvent);
    void Flush();
}

public sealed class ConsoleLogListener : ILogListener
{
    public void Append(GossamerLog.Event logEvent)
    {
        Console.WriteLine(logEvent);
    }

    void ILogListener.Flush()
    {
    }
}

public sealed class DebugLogListener : ILogListener
{
    public void Append(GossamerLog.Event logEvent)
    {
        System.Diagnostics.Debug.WriteLine(logEvent);
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

    readonly ConcurrentQueue<GossamerLog.Event> events = [];

    /// <summary>
    /// Creates a new instance of <see cref="GossamerLog"/> with the specified output path.
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
                    if (events.TryDequeue(out GossamerLog.Event? ev) && ev != null)
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

    void ILogListener.Append(GossamerLog.Event logEvent)
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
public sealed class GossamerLog : IDisposable
{
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
        public override string ToString()
        {
            string timestamp = Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

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

    public GossamerLog()
    {
    }

    public ILogListener AddConsoleListener()
    {
        ILogListener listener = new ConsoleLogListener();
        listeners.Add(listener);
        return listener;
    }

    public ILogListener AddDebugListener()
    {
        ILogListener listener = new DebugLogListener();
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
        foreach (ILogListener listener in listeners)
        {
            listener.Flush();
        }
    }

    public Logger GetLogger(string name, bool enableTypeName, bool enableCallerName)
    {
        if (loggers.TryGetValue(name, out Logger? logger))
        {
            return logger;
        }

        logger = new Logger(this, name, enableTypeName, enableCallerName);
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
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, TypedCallerFormat, nameof(GossamerLog), nameof(Append), e.Message));
        }
    }
}

public class Logger(GossamerLog applicationLog, string name, bool enableTypeName = true, bool enableCallerName = true)
{
    readonly GossamerLog log = applicationLog;
    readonly string name = name;

    public bool EnableName { get; } = enableTypeName;

    public bool EnableCallerName { get; } = enableCallerName;

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Error(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(GossamerLog.Level.Error, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Warning(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(GossamerLog.Level.Warning, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Information(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(GossamerLog.Level.Information, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Debug(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(GossamerLog.Level.Debug, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }
}