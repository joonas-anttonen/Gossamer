using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Gossamer;

public interface ILogListener
{
    void Append(Log.Event logEvent);
}

/// <summary>
/// Logging system for Gossamer.
/// </summary>
public sealed class Log : IDisposable
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
            return $"[{Timestamp}] [{LevelToString(Level)}] {Message}";
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

    bool isDisposed;

    readonly object eventsSyncRoot = new();
    readonly Dictionary<string, Logger> loggers = [];
    readonly ConcurrentQueue<Event> events = [];
    readonly Timer saveToFileTimer;

    readonly bool enableFileOutput = true;
    readonly bool enableConsoleOutput = true;
    readonly bool enableDebugOutput = true;

    public event LogEventHandler? Appended;

    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024 /* 10 MB */;

    public IReadOnlyDictionary<string, Logger> Loggers => loggers;

    public string Path { get; } = string.Empty;

    /// <summary>
    /// Creates a new instance of <see cref="Log"/> with the specified output path.
    /// If <paramref name="path"/> points to a directory that does not exist, it will be created.
    /// If <paramref name="path"/> has no extension, .log will be appended to it.
    /// </summary>
    /// <param name="path">The output path.</param>
    /// <exception cref="ArgumentException"></exception>
    public Log(string path, bool enableFileOutput = true, bool enableConsoleOutput = true, bool enableDebugOutput = false)
    {
        this.enableFileOutput = enableFileOutput;
        this.enableConsoleOutput = enableConsoleOutput;
        this.enableDebugOutput = enableDebugOutput;

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
        if(string.IsNullOrEmpty(pathFileName))
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

    public void Dispose()
    {
        if (!isDisposed)
        {
            SaveToFile();
            isDisposed = true;
        }
    }

    void SaveToFileTimer(object? state)
    {
        SaveToFile();
    }

    public Logger GetLogger(string name, bool enableTypeName, bool enableCallerName)
    {
        if (loggers.TryGetValue(name, out Logger? logger))
        {
            return logger;
        }

        logger = new Logger(this, name, enableTypeName, enableCallerName);
        loggers.Add(name, logger);
        return logger;
    }

    public void SaveToFile()
    {
        lock (eventsSyncRoot)
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
                    if (events.TryDequeue(out Event? ev) && ev != null)
                    {
                        file.WriteLine(ev);
                    }
                }
            }
            catch { }
        }
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

        Console.WriteLine(logMsg);

        lock (eventsSyncRoot)
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

        try
        {
            Appended?.Invoke(logEvent);
        }
        catch (Exception e)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, TypedCallerFormat, nameof(Log), nameof(Append), e.Message));
        }
    }
}

public class Logger(Log applicationLog, string name, bool enableTypeName = true, bool enableCallerName = true)
{
    readonly Log log = applicationLog;
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
        log.Append(Log.Level.Error, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Warning(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Level.Warning, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Information(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Level.Information, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="callerName"></param>
    public void Debug(string message = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Level.Debug, message, DateTime.Now, EnableName ? name : string.Empty, EnableCallerName ? callerName : string.Empty);
    }
}