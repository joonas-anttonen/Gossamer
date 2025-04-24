using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Gossamer.Utilities;

namespace Gossamer.Logging;

/// <summary>
/// A simple logging system.
/// <para> All operations are thread-safe. </para>
/// </summary>
public sealed class Log : IDisposable
{
    /// <summary>
    /// Log event severity.
    /// </summary>
    public enum Severity { Error, Warning, Information, Debug }

    /// <summary>
    /// Log event.
    /// </summary>
    /// <param name="Severity">Event severity.</param>
    /// <param name="Timestamp">Event timestamp.</param>
    /// <param name="Message">Event message.</param>
    /// <param name="Type">Event origin type.</param>
    /// <param name="Method">Event origin method.</param>
    public readonly record struct Event(Severity Severity, DateTime Timestamp, string Message, string Type, string Method, int Thread)
    {
        /// <summary>
        /// Returns a string with the event origin and message.
        /// </summary>
        public readonly string ToShortString()
        {
            return $"[{Thread}] {OriginString(Type, Method)} {Message}";
        }

        /// <summary>
        /// Returns a string that represents the event.
        /// </summary>
        public override readonly string ToString()
        {
            return $"[{Thread}] [{StringUtilities.DateTimeISO8601(Timestamp)}] [{SeverityString(Severity)}] {OriginString(Type, Method)} {Message}";
        }

        static string OriginString(string type, string method) => $"{type}::{method}";

        static string SeverityString(Severity level) => level switch
        {
            Severity.Error => "ERROR",
            Severity.Warning => "WARNING",
            Severity.Information => "INFO",
            Severity.Debug => "DEBUG",
            _ => throw new NotImplementedException(),
        };
    }

    ILogListener[] listeners = [];

    readonly Lock listenersIteratorLock = new();

    readonly ConcurrentDictionary<string, Logger> loggers = [];

    /// <summary>
    /// Adds a <see cref="ConsoleLogListener"/> to the collection of listeners.
    /// <para> This is thread-safe. </para>
    /// </summary>
    public ILogListener AddConsoleListener()
    {
        using (listenersIteratorLock.EnterScope())
        {
            ILogListener listener = new ConsoleLogListener();
            ArrayUtilities.Append(ref listeners, listener);
            return listener;
        }
    }

    /// <summary>
    /// Adds a <see cref="FileLogListener"/> to the collection of listeners.
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="path"></param>
    public ILogListener AddFileListener(string path)
    {
        using (listenersIteratorLock.EnterScope())
        {
            ILogListener listener = new FileLogListener(path);
            ArrayUtilities.Append(ref listeners, listener);
            return listener;
        }
    }

    /// <summary>
    /// Adds a <see cref="ILogListener"/> to the collection of listeners.
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="listener"></param>
    public void AddListener(ILogListener listener)
    {
        using (listenersIteratorLock.EnterScope())
        {
            ArrayUtilities.Append(ref listeners, listener);
        }
    }

    /// <summary>
    /// Returns a copy of the listeners.
    /// <para> This is thread-safe. </para>
    /// </summary>
    public ILogListener[] GetListeners()
    {
        using (listenersIteratorLock.EnterScope())
        {
            return listeners.ToArray();
        }
    }

    /// <summary>
    /// Flushes all listeners.
    /// <para> This is thread-safe. </para>
    /// </summary>
    public void Dispose()
    {
        using (listenersIteratorLock.EnterScope())
        {
            foreach (ILogListener listener in listeners)
            {
                listener.Flush();
            }
        }
    }

    /// <summary>
    /// Gets a <see cref="Logger"/> instance with the specified name. 
    /// <para> Instances are cached so that only one instance is created per name. </para>
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="name"> The name of the logger instance. </param>
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
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="severity">Severity of the event.</param>
    /// <param name="message">Message describing the event.</param>
    /// <param name="typeName">Name of the type that the message originated from.</param>
    /// <param name="methodName">Name of the method that the message originated from.</param>
    public void Append(Severity severity, string message, string typeName, string methodName)
    {
        Event logEvent = new(severity, DateTime.Now, message, typeName, methodName, Environment.CurrentManagedThreadId);

        using (listenersIteratorLock.EnterScope())
        {
            foreach (ILogListener listener in listeners)
            {
                listener.Append(logEvent);
            }
        }
    }
}

/// <summary>
/// Helper for logging messages.
/// </summary>
/// <param name="log">The log.</param>
/// <param name="name">The name of the logger instance.</param>
public record Logger(Log log, string name)
{
    readonly Log log = log;

    public string Name { get; } = name;

    string ResolveTypeName(string typeName) => string.IsNullOrEmpty(typeName) ? Name : typeName;

    /// <summary>
    /// Logs an error message.
    /// <para> If <paramref name="typeName"/> is null or empty, the logger's name is used. </para>
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="typeName"></param>
    /// <param name="callerName"></param>
    public void Error(string message, string typeName = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Severity.Error, message, ResolveTypeName(typeName), callerName);
    }

    /// <summary>
    /// Logs a warning message.
    /// <para> If <paramref name="typeName"/> is null or empty, the logger's name is used. </para>
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="typeName"></param>
    /// <param name="callerName"></param>
    public void Warning(string message, string typeName = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Severity.Warning, message, ResolveTypeName(typeName), callerName);
    }

    /// <summary>
    /// Logs an information message.
    /// <para> If <paramref name="typeName"/> is null or empty, the logger's name is used. </para>
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="typeName"></param>
    /// <param name="callerName"></param>
    public void Information(string message, string typeName = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Severity.Information, message, ResolveTypeName(typeName), callerName);
    }

    /// <summary>
    /// Logs a debug message.
    /// <para> If <paramref name="typeName"/> is null or empty, the logger's name is used. </para>
    /// <para> This is thread-safe. </para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="typeName"></param>
    /// <param name="callerName"></param>
    public void Debug(string message = "", string typeName = "", [CallerMemberName] string callerName = "")
    {
        log.Append(Log.Severity.Debug, message, ResolveTypeName(typeName), callerName);
    }
}