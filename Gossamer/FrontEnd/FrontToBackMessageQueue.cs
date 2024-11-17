using System.Collections.Concurrent;

using Gossamer.Backend;
using Gossamer.Collections;

namespace Gossamer.Frontend;

class FrontToBackMessageQueue(int initialCapacity = 4)
{
    readonly ConcurrentObjectPool<FrontToBackMessage> messagePool = new(initialCapacity: initialCapacity);
    readonly ConcurrentQueue<FrontToBackMessage> messageQueue = new();

    public bool TryDequeue(out FrontToBackMessage? message)
    {
        return messageQueue.TryDequeue(out message);
    }

    public void Return(FrontToBackMessage message)
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

    public void PostSurfaceLost()
    {
        var message = messagePool.Rent();
        message.SetSurfaceLost();
        messageQueue.Enqueue(message);
    }

    public void PostMouseXY(int x, int y)
    {
        var message = messagePool.Rent();
        message.SetMouseXY(x, y);
        messageQueue.Enqueue(message);
    }

    public void PostMouseButton(InputButton button, InputAction action, InputMod mods)
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

    public void PostKeyboardKey(InputKey key, int scancode, InputAction action, InputMod mods)
    {
        var message = messagePool.Rent();
        message.SetKeyboardKey(key, scancode, action, mods);
        messageQueue.Enqueue(message);
    }

    public void PostKeyboardChar(int codepoint, InputMod mods)
    {
        var message = messagePool.Rent();
        message.SetKeyboardChar(codepoint, mods);
        messageQueue.Enqueue(message);
    }
}
