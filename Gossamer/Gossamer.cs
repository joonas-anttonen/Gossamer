using System.Diagnostics;
using System.Runtime.InteropServices;

using Gossamer.Backend;
using Gossamer.Frontend;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer;

public sealed class Gossamer : IDisposable
{
    readonly GossamerLog log;
    readonly Logger logger;

    bool isDisposed;

    readonly FrontToBackMessageQueue frontToBackMessageQueue = new();

    readonly GossamerParameters parameters;

    Thread? backEndThread;

    static Gossamer? instance;

    /// <summary>
    /// The singleton instance of <see cref="Gossamer"/>. Safe to use only after an instance has been created.
    /// </summary>
    public static Gossamer Instance
    {
        get => instance ?? throw new InvalidOperationException("Gossamer has not been initialized.");
    }

    /// <summary>
    /// The <see cref="GossamerLog"/> instance used by the <see cref="Gossamer"/> instance.
    /// </summary>
    public GossamerLog Log
    {
        get => log;
    }

    public Gossamer(GossamerLog log, GossamerParameters parameters)
    {
        if (instance != null)
        {
            throw new InvalidOperationException("Gossamer has already been initialized.");
        }
        instance = this;

        this.parameters = parameters;
        this.log = log;

        logger = log.GetLogger("Gossamer", false, false);

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

        frontToBackMessageQueue.PostQuit();
        backEndThread?.Join();
    }

    public void Run()
    {
        // Set the current directory to the directory of the executable
        Directory.SetCurrentDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location!)!);

        logger.Debug($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.RuntimeIdentifier})");
        logger.Debug($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})");
        logger.Debug($"Working directory: {Directory.GetCurrentDirectory()}");

        // Run the backend in a separate thread
        backEndThread = new(RunBackend);
        backEndThread.Start();

        using Gfx gfx = new();
        gfx.Create(new GfxParameters(
            EnableValidation: true,
            EnableDebugging: true,
            EnableSwapchain: true
        ));

        using Gui gui = new(frontToBackMessageQueue);
        RunFrontend(gui);
    }

    public void RunFrontend(Gui gui)
    {
        gui.Create();

        while (!gui.IsClosing)
        {
            gui.WaitForEvents();
        }

        logger.Debug("Frontend is closing.");
    }

    public void RunBackend()
    {
        bool keepRunning = true;
        while (keepRunning)
        {
            bool keepDequeueing = true;
            while (keepDequeueing)
            {
                keepDequeueing = frontToBackMessageQueue.TryDequeue(out FrontToBackMessage? message);
                if (keepDequeueing)
                {
                    AssertNotNull(message);

                    Debug.WriteLine($"Processing message of type {message.Type}.");

                    //ProcessMessage(message);
                    frontToBackMessageQueue.Return(message);

                    if (message.Type == FrontToBackMessageType.Quit)
                    {
                        keepDequeueing = false;
                        keepRunning = false;
                    }
                }
            }

            // FIXME: This is a temporary solution to prevent the backend from spinning too fast
            Thread.Sleep(10);
        }

        logger.Debug("Backend is closing.");
    }
}
