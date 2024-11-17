using System.Diagnostics;
using System.Runtime.InteropServices;

using Gossamer.BackEnd;
using Gossamer.FrontEnd;

using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer;

public sealed class Gossamer : IDisposable
{
    bool isDisposed;

    readonly FrontToBackMessageQueue frontToBackMessageQueue = new();

    readonly GossamerParameters parameters;

    Thread? backEndThread;

    public Gossamer(GossamerParameters parameters)
    {
        this.parameters = parameters;

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

        // Print the runtime information
        Debug.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.RuntimeIdentifier})");

        // Run the backend in a separate thread
        backEndThread = new(RunBackEnd);
        backEndThread.Start();

        using BackEndGfx backEndGfx = new();
        backEndGfx.Create();

        using FrontEndGui frontEndGui = new(frontToBackMessageQueue);
        RunFrontEnd(frontEndGui);
    }

    public void RunFrontEnd(FrontEndGui frontEndGui)
    {
        frontEndGui.Create();

        while (!frontEndGui.IsClosing)
        {
            frontEndGui.WaitForEvents();
        }

        Debug.WriteLine("FrontEnd is closing.");
    }

    public void RunBackEnd()
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

            Thread.Sleep(10);
        }

        Debug.WriteLine("BackEnd is closing.");
    }
}
