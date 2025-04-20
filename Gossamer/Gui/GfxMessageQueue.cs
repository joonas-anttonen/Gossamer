using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Gossamer.Gfx;
using Gossamer.Collections;

namespace Gossamer.Gui;

class GfxMessageQueue(int initialCapacity = 4)
{
    readonly ConcurrentObjectPool<GfxMessage> messagePool = new(initialCapacity: initialCapacity);
    readonly ConcurrentQueue<GfxMessage> messageQueue = new();

    public bool TryDequeue([NotNullWhen(true)] out GfxMessage? message)
    {
        return messageQueue.TryDequeue(out message);
    }

    public void Return(GfxMessage message)
    {
        messagePool.Return(message);
    }

    public void PostQuit()
    {
        var message = messagePool.Rent();
        message.SetQuit();
        messageQueue.Enqueue(message);
    }

    public void PostSurfaceDamaged()
    {
        var message = messagePool.Rent();
        message.SetSurfaceDamaged();
        messageQueue.Enqueue(message);
    }

    public void PostSurfaceLost(int x, int y)
    {
        var message = messagePool.Rent();
        message.SetSurfaceLost(x, y);
        messageQueue.Enqueue(message);
    }

    public void PostMouseXY(int x, int y)
    {
        var message = messagePool.Rent();
        message.SetMouseXY(x, y);
        messageQueue.Enqueue(message);
    }

    public void PostMouseButton(InputButton button, InputAction action, InputMods mods)
    {
        var message = messagePool.Rent();
        message.SetMouseButton(button, action, mods);
        messageQueue.Enqueue(message);
    }

    public void PostMouseWheel(int x, int y)
    {
        var message = messagePool.Rent();
        message.SetMouseWheel(x, y);
        messageQueue.Enqueue(message);
    }

    public void PostKeyboardKey(InputKey key, int scancode, InputAction action, InputMods mods)
    {
        var message = messagePool.Rent();
        message.SetKeyboardKey(key, scancode, action, mods);
        messageQueue.Enqueue(message);
    }

    public void PostKeyboardChar(int codepoint, InputMods mods)
    {
        var message = messagePool.Rent();
        message.SetKeyboardChar(codepoint, mods);
        messageQueue.Enqueue(message);
    }
}