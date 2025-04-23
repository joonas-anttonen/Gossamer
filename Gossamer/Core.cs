using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Gossamer.Collections;
using Gossamer.Gfx;
using Gossamer.Gfx.Presentation;
using Gossamer.Gui;
using Gossamer.Logging;
using Gossamer.Utilities;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer;

public sealed class Core : SynchronizationContext, IDisposable
{
    public record ApplicationInfo(string Name, Version Version)
    {
        /// <summary>
        /// Creates <see cref="ApplicationInfo"/> using <see cref="System.Reflection.Assembly.GetCallingAssembly"/>.
        /// </summary>
        public static ApplicationInfo FromCallingAssembly()
        {
            var assembly = System.Reflection.Assembly.GetCallingAssembly();
            var name = assembly.GetName();
            return new ApplicationInfo(name.Name!, name.Version!);
        }
    }

    public record Parameters(bool EnableDebugging = false, GuiCore.Platform Platform = GuiCore.Platform.Auto)
    {
        public static Parameters FromArgs(string[] args)
        {
            Dictionary<string, string> argMap = [];
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.StartsWith("--"))
                {
                    string argKey = arg[2..];
                    string argValue = i + 1 < args.Length ? args[i + 1] : string.Empty;
                    if (!string.IsNullOrEmpty(argValue) && !argValue.StartsWith("--"))
                    {
                        i++;
                    }
                    argMap[argKey] = argValue;
                }
            }

            return new Parameters(
                EnableDebugging: argMap.ContainsKey("debug"),
                Platform: argMap.TryGetValue("platform", out string? platformArg) ? Enum.Parse<GuiCore.Platform>(platformArg, ignoreCase: true) : GuiCore.Platform.Auto
            );
        }
    }

    readonly Log log;
    readonly Logger logger;

    bool isDisposed;

    readonly GfxMessageQueue gfxMessageQueue = new(initialCapacity: 16);

    readonly int guiThreadId;
    readonly int gfxThreadId;
    readonly Thread gfxThread;

    int guiSyncOperationCount;
    readonly ConcurrentObjectPool<SyncEntry> guiSyncEntryPool = new(16);
    readonly ConcurrentQueue<SyncEntry> guiSyncQueue = [];

    readonly ManualResetEventSlim guiSyncEvent = new(initialState: false);

    GfxCore? gfx;
    GuiCore? gui;

    readonly Stopwatch stopwatch = Stopwatch.StartNew();

    readonly Parameters coreParameters;
    readonly ApplicationInfo appInfo;

    static Core? instance;

    public static TimeSpan GetTime()
    {
        return Instance.stopwatch.Elapsed;
    }

    /// <summary>
    /// Gets a <see cref="Logger"/> from the <see cref="Log"/> of the <see cref="Core"/> singleton instance. 
    /// </summary>
    /// <param name="name"></param>
    public static Logger GetLogger(string name)
    {
        return Instance.log.GetLogger(name);
    }

    /// <summary>
    /// The singleton instance of <see cref="Core"/>. Safe to use only after an instance has been created.
    /// </summary>
    public static Core Instance
    {
        get => ThrowInvalidOperationIfNull(instance, $"{nameof(Core)} has not been initialized.");
    }

    static int Main(string[] args)
    {
        var parameters = Parameters.FromArgs(args);
        using var gossamer = new Core(parameters);

        return gossamer.Run();
    }

    public Core(Parameters parameters, ApplicationInfo? appInfo = default)
    {
        ThrowInvalidOperationIf(instance != null, $"{nameof(Core)} has already been initialized.");
        instance = this;

        this.coreParameters = parameters;
        this.appInfo = appInfo ?? ApplicationInfo.FromCallingAssembly();

        guiThreadId = Environment.CurrentManagedThreadId;
        gfxThread = new(RunGfx);
        gfxThreadId = gfxThread.ManagedThreadId;

        log = new Log();
        logger = log.GetLogger(nameof(Core));

        if (parameters.EnableDebugging)
        {
            log.AddConsoleListener();
        }

        SetSynchronizationContext(this);

        NativeLibrary.SetDllImportResolver(typeof(Core).Assembly, NativeImportResolver);
    }

    /// <summary>
    /// Custom DllImportResolver to load external libraries from the correct location based on the OS.
    /// </summary>
    static nint NativeImportResolver(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        static nint Load(string name, System.Reflection.Assembly assembly)
        {
            string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dll" : "so";

            return NativeLibrary.Load($"{name}.{extension}", assembly, DllImportSearchPath.AssemblyDirectory);
        }

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
                return nint.Zero;
            case External.Glfw.Api.BinaryName:
                return Load(External.Glfw.Api.BinaryName, assembly);
            case External.HarfBuzz.Api.BinaryName:
                return Load(External.HarfBuzz.Api.BinaryName, assembly);
            case External.FreeType.Api.BinaryName:
                return Load(External.FreeType.Api.BinaryName, assembly);
            case External.Vulkan.Vma.Api.BinaryName:
                return Load(External.Vulkan.Vma.Api.BinaryName, assembly);
            case External.Webp.Api.BinaryName:
                return Load(External.Webp.Api.BinaryName, assembly);
            default:
                return nint.Zero;
        }
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
    /// Runs Gossamer. This method will block until the user interface is closed.
    /// </summary>
    public int Run()
    {
        GfxCore? gfx = null;
        GuiCore? gui = null;

        try
        {
            if (coreParameters.EnableDebugging)
            {
                logger.Debug($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})");
                logger.Debug($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.RuntimeIdentifier})");
                logger.Debug($"Directory: {Directory.GetCurrentDirectory()}");
            }

            // 1. Create graphics
            gfx = new(new GfxApiParameters(
               appInfo,
               EnableDebugging: coreParameters.EnableDebugging,
               PresentationMode: GfxPresentationMode.SwapChain
           ));

            // 2. Initialize graphics
            GfxParameters gfxParameters = new(
                PhysicalDevice: gfx.SelectOptimalDevice(gfx.EnumeratePhysicalDevices())
            );
            if (coreParameters.EnableDebugging)
            {
                logger.Debug($"GPU: {gfxParameters.PhysicalDevice}");
            }
            gfx.Create(gfxParameters);

            // 3. Create user interface
            gui = new(coreParameters, gfx, gfxMessageQueue);

            // 4. Initialize user interface
            GuiParameters guiParameters = new(
                name: appInfo.Name
            );
            gui.Create(guiParameters);

            // 5. Create graphics swap chain presenter (depends on user interface)
            gfx.CreatePresenter(new GfxSwapChainPresentation(gui, EnableVerticalSync: false));

            RunGfx(gfx);
            RunGui(gui);

            gfxMessageQueue.PostQuit();
            gfxThread?.Join();

            return 0;
        }
        catch (Exception ex)
        {
            logger.Error(ex.ToString());
        }
        finally
        {
            gfx?.Dispose();
            gui?.Dispose();
        }

        return 1;
    }

    /// <summary>
    /// Runs the graphics on a separate thread. This method will return immediately.
    /// </summary>
    /// <param name="gfx"></param>
    public void RunGfx(GfxCore gfx)
    {
        ThrowInvalidOperationIfNull(gfx, "Gfx is null.");
        this.gfx = gfx;

        gfxThread.Start();
    }

    /// <summary>
    /// Runs the user interface on the current thread. This method will block until the user interface is closed.
    /// </summary>
    /// <param name="gui"></param>
    public void RunGui(GuiCore gui)
    {
        ThrowInvalidOperationIfNull(gui, "Gui is null.");
        this.gui = gui;

        while (true)
        {
            GuiDispatchSyncQueue();
            GuiFrame();

            if (gui.IsClosing)
            {
                break;
            }
        }

        logger.Debug("Exit");
    }

    void GuiDispatchSyncQueue()
    {
        while (guiSyncQueue.TryDequeue(out SyncEntry? entry))
        {
            logger.Debug($"FrontendDispatchSyncQueue on thread = {Environment.CurrentManagedThreadId}");

            entry.Execute();
            // TODO: Handle exceptions
            entry.Complete();

            guiSyncEntryPool.Return(entry);
        }
    }

    void GuiFrame()
    {
        ThrowInvalidOperationIfNull(gui);

        gui.WaitForEvents(0.1);
        gui.Render();
    }

    void GuiWakeUp()
    {
        ThrowInvalidOperationIfNull(gui);

        gui.PostEmptyEvent();
    }

    void RunGfx()
    {
        GfxCore localGfx = ThrowInvalidOperationIfNull(gfx);

        bool keepRunning = true;
        while (keepRunning)
        {
            bool keepDequeueing = true;
            while (keepDequeueing)
            {
                if (!gfxMessageQueue.TryDequeue(out GfxMessage? message))
                {
                    break;
                }

                switch (message.Type)
                {
                    case GfxMessageType.Quit:
                        {
                            keepDequeueing = false;
                            keepRunning = false;
                            break;
                        }
                    case GfxMessageType.SurfaceLost:
                        {
                            message.GetSurfaceLost(out int w, out int h);

                            GfxPresenter? presenter = localGfx.GetPresenter();
                            presenter?.Invalidate((uint)w, (uint)h);
                            break;
                        }
                }

                gfxMessageQueue.Return(message);
            }

            localGfx.Render();

            gfxMessageQueue.WaitForMessage(1);
        }

        logger.Debug("Exit");
    }

    public override SynchronizationContext CreateCopy()
    {
        ThrowNotSupportedIf(true, "CreateCopy is not supported.");
        return this;
    }

    public override void OperationStarted()
    {
        Interlocked.Increment(ref guiSyncOperationCount);
    }

    public override void OperationCompleted()
    {
        Interlocked.Decrement(ref guiSyncOperationCount);
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
        var entry = guiSyncEntryPool.Rent();
        entry.Initialize(synchronous, d, state);

        guiSyncQueue.Enqueue(entry);

        if (Environment.CurrentManagedThreadId == guiThreadId)
        {
            GuiDispatchSyncQueue();
        }
        else
        {
            GuiWakeUp();
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