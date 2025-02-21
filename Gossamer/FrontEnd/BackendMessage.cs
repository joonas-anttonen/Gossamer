using Gossamer.Backend;

namespace Gossamer.Frontend;

using static Utilities.ExceptionUtilities;

enum BackendMessageType
{
    Quit,
    SurfaceDamaged,
    SurfaceLost,
    MouseXY,
    MouseButton,
    MouseWheel,
    KeyboardKey,
    KeyboardChar,
}

class BackendMessage
{
    public BackendMessageType Type { get; private set; }

    int field0;
    int field1;
    int field2;
    int field3;

    public void GetMouseXY(out int x, out int y)
    {
        Assert(Type == BackendMessageType.MouseXY);
        x = field0;
        y = field1;
    }

    public void GetMouseButton(out InputButton button, out InputAction action, out InputMods mods)
    {
        Assert(Type == BackendMessageType.MouseButton);
        button = (InputButton)field0;
        action = (InputAction)field1;
        mods = (InputMods)field2;
    }

    public void GetMouseWheel(out int x, out int y)
    {
        Assert(Type == BackendMessageType.MouseWheel);
        x = field0;
        y = field1;
    }

    public void GetKeyboardKey(out InputKey key, out int scancode, out InputAction action, out InputMods mods)
    {
        Assert(Type == BackendMessageType.KeyboardKey);
        key = (InputKey)field0;
        scancode = field1;
        action = (InputAction)field2;
        mods = (InputMods)field3;
    }

    public void GetKeyboardChar(out int codepoint, out InputMods mods)
    {
        Assert(Type == BackendMessageType.KeyboardChar);
        codepoint = field0;
        mods = (InputMods)field1;
    }

    public void SetQuit()
    {
        Type = BackendMessageType.Quit;
    }

    public void SetSurfaceDamaged()
    {
        Type = BackendMessageType.SurfaceDamaged;
    }

    public void SetSurfaceLost()
    {
        Type = BackendMessageType.SurfaceLost;
    }

    public void SetMouseXY(int x, int y)
    {
        Type = BackendMessageType.MouseXY;
        field0 = x;
        field1 = y;
    }

    public void SetMouseButton(InputButton button, InputAction action, InputMods mods)
    {
        Type = BackendMessageType.MouseButton;
        field0 = (int)button;
        field1 = (int)action;
        field2 = (int)mods;
    }

    public void SetMouseWheel(int x, int y)
    {
        Type = BackendMessageType.MouseWheel;
        field0 = x;
        field1 = y;
    }

    public void SetKeyboardKey(InputKey key, int scancode, InputAction action, InputMods mods)
    {
        Type = BackendMessageType.KeyboardKey;
        field0 = (int)key;
        field1 = scancode;
        field2 = (int)action;
        field3 = (int)mods;
    }

    public void SetKeyboardChar(int codepoint, InputMods mods)
    {
        Type = BackendMessageType.KeyboardChar;
        field0 = codepoint;
        field1 = (int)mods;
    }
}
