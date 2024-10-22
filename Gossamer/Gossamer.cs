using System.Diagnostics;
using System.Runtime.InteropServices;

using Gossamer.BackEnd;
using Gossamer.FrontEnd;

namespace Gossamer;

using static Utilities.ExceptionUtilities;

public sealed class Gossamer : IDisposable
{
    bool isDisposed;

    readonly FrontToBackMessageQueue frontToBackMessageQueue = new();

    readonly GossamerParameters parameters;

    Thread? backEndThread;

    public Gossamer(GossamerParameters parameters)
    {
        this.parameters = parameters;

        //NativeLibrary.SetDllImportResolver(typeof(Gossamer).Assembly, (libraryName, assembly, searchPath) =>
        //{
        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        //    {
        //        return NativeLibrary.Load($"{libraryName}.dll");
        //    }
        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //    {
        //        return NativeLibrary.Load($"{libraryName}.so");
        //    }
//
        //    return IntPtr.Zero;
        //});
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
