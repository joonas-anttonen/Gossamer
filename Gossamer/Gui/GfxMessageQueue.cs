using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Gossamer.Collections;
using Gossamer.Gfx;

namespace Gossamer.Gui;

class GfxMessageQueue(int initialCapacity = 4)
{
    readonly ConcurrentObjectPool<GfxMessage> messagePool = new(initialCapacity: initialCapacity);
    readonly ConcurrentQueue<GfxMessage> messageQueue = new();
    readonly ManualResetEventSlim messageAvailable = new(initialState: false);

    public bool WaitForMessage(int millisecondsTimeout)
    {
        if (messageAvailable.Wait(millisecondsTimeout))
        {
            messageAvailable.Reset();
            return true;
        }
        return false;
    }

    public bool TryDequeue([NotNullWhen(true)] out GfxMessage? message)
    {
        return messageQueue.TryDequeue(out message);
    }

    public void Return(GfxMessage message)
    {
        messagePool.Return(message);
    }

    void Enqueue(GfxMessage message)
    {
        messageQueue.Enqueue(message);
        messageAvailable.Set();
    }

    public void PostQuit()
    {
        var message = messagePool.Rent();
        message.SetQuit();
        Enqueue(message);
    }

    public void PostSurfaceDamaged()
    {
        var message = messagePool.Rent();
        message.SetSurfaceDamaged();
        Enqueue(message);
    }

    public void PostMouseXY(int x, int y)
    {
        var message = messagePool.Rent();
        message.SetMouseXY(x, y);
        Enqueue(message);
    }

    public void PostMouseButton(InputButton button, InputAction action, InputMods mods)
    {
        var message = messagePool.Rent();
        message.SetMouseButton(button, action, mods);
        Enqueue(message);
    }

    public void PostMouseWheel(int x, int y)
    {
        var message = messagePool.Rent();
        message.SetMouseWheel(x, y);
        Enqueue(message);
    }

    public void PostKeyboardKey(InputKey key, int scancode, InputAction action, InputMods mods)
    {
        var message = messagePool.Rent();
        message.SetKeyboardKey(key, scancode, action, mods);
        Enqueue(message);
    }

    public void PostKeyboardChar(int codepoint, InputMods mods)
    {
        var message = messagePool.Rent();
        message.SetKeyboardChar(codepoint, mods);
        Enqueue(message);
    }
}