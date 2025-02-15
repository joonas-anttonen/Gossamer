using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Gossamer.Logging;

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
    /// <param name="Timestamp">Log timestamp.</param>
    /// <param name="Message">Log message.</param>
    public readonly record struct Event(Level Level, DateTime Timestamp, string Message)
    {
        public readonly string ToShortString()
        {
            return Message;
        }

        public override readonly string ToString()
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

    bool isDisposed;

    readonly ConcurrentBag<ILogListener> listeners = [];

    readonly ConcurrentDictionary<string, Logger> loggers = [];

    /// <summary>
    /// Adds a <see cref="ConsoleLogListener"/> to the collection of listeners.
    /// </summary>
    /// <returns></returns>
    public ILogListener AddConsoleListener()
    {
        ILogListener listener = new ConsoleLogListener();
        listeners.Add(listener);
        return listener;
    }

    /// <summary>
    /// Adds a <see cref="FileLogListener"/> to the collection of listeners.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public ILogListener AddFileListener(string path)
    {
        ILogListener listener = new FileLogListener(path);
        listeners.Add(listener);
        return listener;
    }

    /// <summary>
    /// Adds a <see cref="ILogListener"/> to the collection of listeners.
    /// </summary>
    /// <param name="listener"></param>
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

    /// <summary>
    /// Gets a <see cref="Logger"/> instance with the specified name.
    /// </summary>
    /// <param name="name"></param>
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
    /// Appends a message to the log.
    /// </summary>
    /// <param name="level">Severity of the event.</param>
    /// <param name="timestamp">Timestamp of the event.</param>
    /// <param name="message">Message describing the event.</param>
    /// <param name="typeName">Name of the type that the message originated from.</param>
    /// <param name="callerName">Name of the caller that the message originated from.</param>
    public void Append(Level level, DateTime timestamp, string message, string typeName, string callerName)
    {
        const string TypedCallerFormat = "{0}::{1} {2}";
        const string TypedFormat = "{0} {1}";
        const string PlainFormat = "{0}";

        bool haveTypeName = !string.IsNullOrEmpty(typeName);
        bool haveCallerName = !string.IsNullOrEmpty(callerName);

        string logMsg = haveTypeName
            ? haveCallerName
                ? string.Format(CultureInfo.InvariantCulture, PlainFormat, message)
                : string.Format(CultureInfo.InvariantCulture, TypedFormat, callerName, message)
            : haveCallerName
                ? string.Format(CultureInfo.InvariantCulture, TypedFormat, typeName, message)
                : string.Format(CultureInfo.InvariantCulture, TypedCallerFormat, typeName, callerName, message);

        Event logEvent = new(level, timestamp, logMsg);

        foreach (ILogListener listener in listeners)
        {
            listener.Append(logEvent);
        }
    }
}

/// <summary>
/// Helper for logging messages.
/// </summary>
/// <param name="log">The log.</param>
/// <param name="name">The name of the logger instance.</param>
public class Logger(Log log, string name)
{
    readonly Log log = log;
    readonly string name = name;

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message"></param>
    public void Error(string message)
    {
        log.Append(Log.Level.Error, DateTime.Now, message, string.Empty, string.Empty);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message"></param>
    public void Warning(string message)
    {
        log.Append(Log.Level.Warning, DateTime.Now, message, string.Empty, string.Empty);
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="message"></param>
    public void Information(string message)
    {
        log.Append(Log.Level.Information, DateTime.Now, message, string.Empty, string.Empty);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="typeName"></param>
    /// <param name="callerName"></param>
    public void Debug(string message = "", string typeName = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Level.Debug, DateTime.Now, message, string.IsNullOrEmpty(typeName) ? name : typeName, callerName);
    }
}