using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Gossamer.Backend;
using Gossamer.Collections;
using Gossamer.Frontend;
using Gossamer.Logging;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer;

public sealed class Gossamer : SynchronizationContext, IDisposable
{
    public record class ApplicationInfo(string Name, Version Version)
    {
        /// <summary>
        /// Creates <see cref="ApplicationInfo"/> using <see cref="System.Reflection.Assembly.GetCallingAssembly"/>.
        /// </summary>
        /// <returns></returns>
        public static ApplicationInfo FromCallingAssembly()
        {
            var assembly = System.Reflection.Assembly.GetCallingAssembly();
            var name = assembly.GetName();
            return new ApplicationInfo(name.Name!, name.Version!);
        }
    }

    public record class Parameters(bool EnableDebugging = false)
    {
        public static Parameters FromArgs(string[] args)
        {
            Dictionary<string, string> argMap = [];
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("--"))
                {
                    string key = arg[2..];
                    string value = i + 1 < args.Length ? args[i + 1] : string.Empty;
                    if (!string.IsNullOrEmpty(value) && !value.StartsWith("--"))
                    {
                        i++;
                    }
                    argMap[key] = value;
                }
            }

            return new Parameters(
                EnableDebugging: argMap.ContainsKey("debug")
            );
        }
    }

    readonly Log log;
    readonly Logger logger;

    bool isDisposed;

    readonly BackendMessageQueue backendMessageQueue = new(initialCapacity: 16);

    readonly int frontendThreadId;
    readonly int backendThreadId;
    readonly Thread backendThread;
    int syncOperationCount;
    readonly ConcurrentObjectPool<SyncEntry> syncEntryPool = new(16);
    readonly ConcurrentQueue<SyncEntry> frontendSyncQueue = [];
    readonly ConcurrentQueue<SyncEntry> backendSyncQueue = [];

    Gfx? gfx;
    Gui? gui;

    readonly Parameters parameters;
    readonly ApplicationInfo appInfo;

    static Gossamer? instance;

    /// <inheritdoc cref="Log.GetLogger(string)"/>
    public static Logger GetLogger(string name)
    {
        return Instance.log.GetLogger(name);
    }

    /// <summary>
    /// The singleton instance of <see cref="Gossamer"/>. Safe to use only after an instance has been created.
    /// </summary>
    public static Gossamer Instance
    {
        get => instance ?? throw new InvalidOperationException("Gossamer has not been initialized.");
    }

    public Gossamer(Parameters parameters, ApplicationInfo? appInfo = default)
    {
        if (instance != null)
        {
            throw new InvalidOperationException("Gossamer has already been initialized.");
        }
        instance = this;

        this.parameters = parameters;
        this.appInfo = appInfo ?? ApplicationInfo.FromCallingAssembly();

        frontendThreadId = Environment.CurrentManagedThreadId;
        backendThread = new(BackendThread);
        backendThreadId = backendThread.ManagedThreadId;

        log = new Log();
        logger = log.GetLogger(nameof(Gossamer));

        if (parameters.EnableDebugging)
        {
            log.AddConsoleListener();
        }

        SetSynchronizationContext(this);

        NativeLibrary.SetDllImportResolver(typeof(Gossamer).Assembly, DllImportResolver);
    }

    /// <summary>
    /// Custom DllImportResolver to load external libraries from the correct location based on the OS.
    /// </summary>
    static nint DllImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        switch (libraryName)
        {
            case External.Vulkan.Api.BinaryName:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return NativeLibrary.Load("vulkan-1.dll");
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return NativeLibrary.Load("libvulkan.so.1");
                }
                break;

            case External.Glfw.Api.BinaryName:
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return NativeLibrary.Load($"Gossamer.glfw.dll", assembly, DllImportSearchPath.AssemblyDirectory);
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return NativeLibrary.Load($"Gossamer.glfw.so", assembly, DllImportSearchPath.AssemblyDirectory);
                }
                break;
        }

        return nint.Zero;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        isDisposed = true;

        log.Dispose();
    }

    /// <summary>
    /// Runs <see cref="Gossamer"/> in full application mode. This method will block until the frontend is closed.
    /// </summary>
    public int Run()
    {
        try
        {
            // Set the current directory to the directory of the executable
            Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location!)!);

            logger.Debug($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})");
            logger.Debug($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.RuntimeIdentifier})");
            logger.Debug($"Working directory: {Directory.GetCurrentDirectory()}");

            // Debug log threading information
            logger.Debug("Frontend thread = " + frontendThreadId);
            logger.Debug("Backend thread = " + backendThreadId);

            // Frontend
            using Gui gui = new(backendMessageQueue);
            gui.Create();

            // Backend
            using Gfx gfx = new(new GfxApiParameters(
                appInfo,
                EnableDebugging: parameters.EnableDebugging,
                PresentationMode: GfxPresentationMode.SwapChain
            ));
            gfx.Create(new GfxParameters(
                PhysicalDevice: gfx.SelectOptimalDevice(gfx.EnumeratePhysicalDevices()),
                Presentation: new GfxSwapChainPresentation(Color.DarkPeriwinkle, gui)
            ));

            RunBackend(gfx);
            RunFrontend(gui);

            backendMessageQueue.PostQuit();
            backendThread?.Join();

            return 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex.ToString());
        }

        return 1;
    }

    /// <summary>
    /// Runs the backend on a separate thread. This method will return immediately.
    /// </summary>
    /// <param name="gfx"></param>
    public void RunBackend(Gfx gfx)
    {
        ThrowInvalidOperationIfNull(gfx, "Gfx is null.");
        this.gfx = gfx;

        backendThread.Start();
    }

    /// <summary>
    /// Runs the frontend on the current thread. This method will block until the frontend is closed.
    /// </summary>
    /// <param name="gui"></param>
    public void RunFrontend(Gui gui)
    {
        ThrowInvalidOperationIfNull(gui, "Gui is null.");
        this.gui = gui;

        while (true)
        {
            FrontendDispatchSyncQueue();
            FrontendFrame();

            if (gui.IsClosing)
            {
                break;
            }
        }

        logger.Debug("Frontend Exit");
    }

    void FrontendDispatchSyncQueue()
    {
        while (frontendSyncQueue.TryDequeue(out SyncEntry? entry))
        {
            logger.Debug($"FrontendDispatchSyncQueue on thread = {Environment.CurrentManagedThreadId}");

            entry.Execute();
            // TODO: Handle exceptions
            entry.Complete();

            syncEntryPool.Return(entry);
        }
    }

    void FrontendFrame()
    {
        Gui.WaitForEvents();
    }

    void FrontendWakeUp()
    {
        Gui.PostEmptyEvent();
    }

    void BackendThread()
    {
        bool keepRunning = true;
        while (keepRunning)
        {
            bool keepDequeueing = true;
            while (keepDequeueing)
            {
                keepDequeueing = backendMessageQueue.TryDequeue(out BackendMessage? message);
                if (keepDequeueing)
                {
                    AssertNotNull(message);

                    //Debug.WriteLine($"Processing message of type {message.Type}.");

                    //ProcessMessage(message);
                    backendMessageQueue.Return(message);

                    if (message.Type == BackendMessageType.Quit)
                    {
                        keepDequeueing = false;
                        keepRunning = false;
                    }
                }
            }

            gfx?.Render();

            // FIXME: This is a temporary solution to prevent the backend from spinning too fast
            Thread.Sleep(1);
        }

        logger.Debug("Backend Exit");
    }

    public override SynchronizationContext CreateCopy()
    {
        Assert(false);
        return this;
    }

    public override void OperationStarted()
    {
        Interlocked.Increment(ref syncOperationCount);
    }

    public override void OperationCompleted()
    {
        Interlocked.Decrement(ref syncOperationCount);
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        EnqueueSync(d, state, synchronous: false);
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        SyncEntry syncEntry = EnqueueSync(d, state, synchronous: true);

        // Wait for the operation to complete if needed
        if (!syncEntry.IsCompleted)
        {
            syncEntry.AsyncWaitHandle.WaitOne(Timeout.Infinite, false);
        }
    }

    SyncEntry EnqueueSync(SendOrPostCallback d, object? state, bool synchronous)
    {
        var entry = syncEntryPool.Rent();
        entry.Initialize(synchronous, d, state);

        frontendSyncQueue.Enqueue(entry);

        if (Environment.CurrentManagedThreadId == frontendThreadId)
        {
            FrontendDispatchSyncQueue();
        }
        else
        {
            FrontendWakeUp();
        }

        return entry;
    }

    /// <summary>
    /// Represents a delegate invocation to be performed at a later time.
    /// </summary>
    sealed class SyncEntry : IAsyncResult, IDisposable
    {
        readonly ManualResetEventSlim resetEvent = new(initialState: false);

        bool isDisposed;

        public bool Synchronous { get; set; }
        public object? ReturnValue { get; set; }
        public object? AsyncState { get; set; }
        public object?[]? Arguments { get; set; }
        public Delegate? Method { get; set; }
        public Exception? Exception { get; set; }
        public bool IsCompleted { get; set; }
        public bool CompletedSynchronously => IsCompleted && Synchronous;
        public bool HasException => Exception != null;

        public WaitHandle AsyncWaitHandle => resetEvent.WaitHandle;

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                resetEvent.Dispose();
            }
        }

        /// <summary>
        /// Initializes the <see cref="SyncEntry"/>.
        /// </summary>
        /// <param name="synchronous"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        public void Initialize(bool synchronous, Delegate method, params object?[]? args)
        {
            ThrowInvalidOperationIf(isDisposed, "SyncEntry has already been disposed.");
            ThrowInvalidOperationIf(!IsCompleted, "SyncEntry has not been completed.");

            Method = method;
            Arguments = args;
            Synchronous = synchronous;
            IsCompleted = false;
            resetEvent.Reset();
        }

        /// <summary>
        /// Executes the <see cref="SyncEntry"/>. Exceptions are caught and stored in the <see cref="Exception"/> property.
        /// </summary>
        public void Execute()
        {
            ThrowInvalidOperationIf(isDisposed, "SyncEntry has already been disposed.");
            ThrowInvalidOperationIf(IsCompleted, "SyncEntry has already been completed.");
            ThrowInvalidOperationIfNull(Method, "Method is null.");

            try
            {
                ReturnValue = Method.DynamicInvoke(Arguments);
            }
            catch (Exception ex)
            {
                Exception = ex;
            }
        }

        /// <summary>
        /// Completes the <see cref="SyncEntry"/>. All references are cleared.
        /// </summary>
        public void Complete()
        {
            // Since sync entries have the same lifetime as the application, clear all references when completing.
            Method = null;
            Arguments = null;
            Exception = null;

            IsCompleted = true;
            resetEvent.Set();
        }
    }
}
