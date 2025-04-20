using Gossamer.Gfx;

namespace Gossamer.Gui;

using static Utilities.ExceptionUtilities;

enum GfxMessageType
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

class GfxMessage
{
    public GfxMessageType Type { get; private set; }

    int field0;
    int field1;
    int field2;
    int field3;

    public void GetMouseXY(out int x, out int y)
    {
        ThrowInvalidOperationIfNot(Type == GfxMessageType.MouseXY);
        x = field0;
        y = field1;
    }

    public void GetMouseButton(out InputButton button, out InputAction action, out InputMods mods)
    {
        ThrowInvalidOperationIfNot(Type == GfxMessageType.MouseButton);
        button = (InputButton)field0;
        action = (InputAction)field1;
        mods = (InputMods)field2;
    }

    public void GetMouseWheel(out int x, out int y)
    {
        ThrowInvalidOperationIfNot(Type == GfxMessageType.MouseWheel);
        x = field0;
        y = field1;
    }

    public void GetKeyboardKey(out InputKey key, out int scancode, out InputAction action, out InputMods mods)
    {
        ThrowInvalidOperationIfNot(Type == GfxMessageType.KeyboardKey);
        key = (InputKey)field0;
        scancode = field1;
        action = (InputAction)field2;
        mods = (InputMods)field3;
    }

    public void GetKeyboardChar(out int codepoint, out InputMods mods)
    {
        ThrowInvalidOperationIfNot(Type == GfxMessageType.KeyboardChar);
        codepoint = field0;
        mods = (InputMods)field1;
    }

    public void GetSurfaceLost(out int x, out int y)
    {
        ThrowInvalidOperationIfNot(Type == GfxMessageType.SurfaceLost);
        x = field0;
        y = field1;
    }

    public void SetQuit()
    {
        Type = GfxMessageType.Quit;
    }

    public void SetSurfaceDamaged()
    {
        Type = GfxMessageType.SurfaceDamaged;
    }

    public void SetSurfaceLost(int x, int y)
    {
        Type = GfxMessageType.SurfaceLost;
        field0 = x;
        field1 = y;
    }

    public void SetMouseXY(int x, int y)
    {
        Type = GfxMessageType.MouseXY;
        field0 = x;
        field1 = y;
    }

    public void SetMouseButton(InputButton button, InputAction action, InputMods mods)
    {
        Type = GfxMessageType.MouseButton;
        field0 = (int)button;
        field1 = (int)action;
        field2 = (int)mods;
    }

    public void SetMouseWheel(int x, int y)
    {
        Type = GfxMessageType.MouseWheel;
        field0 = x;
        field1 = y;
    }

    public void SetKeyboardKey(InputKey key, int scancode, InputAction action, InputMods mods)
    {
        Type = GfxMessageType.KeyboardKey;
        field0 = (int)key;
        field1 = scancode;
        field2 = (int)action;
        field3 = (int)mods;
    }

    public void SetKeyboardChar(int codepoint, InputMods mods)
    {
        Type = GfxMessageType.KeyboardChar;
        field0 = codepoint;
        field1 = (int)mods;
    }
}