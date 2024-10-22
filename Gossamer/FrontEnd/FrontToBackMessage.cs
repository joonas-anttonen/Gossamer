using Gossamer.BackEnd;

namespace Gossamer.FrontEnd;

using static Utilities.ExceptionUtilities;

enum FrontToBackMessageType
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

class FrontToBackMessage
{
    public FrontToBackMessageType Type { get; private set; }

    int field0;
    int field1;
    int field2;
    int field3;

    public void GetMouseXY(out int x, out int y)
    {
        Assert(Type == FrontToBackMessageType.MouseXY);
        x = field0;
        y = field1;
    }

    public void GetMouseButton(out InputButton button, out InputAction action, out InputMod mods)
    {
        Assert(Type == FrontToBackMessageType.MouseButton);
        button = (InputButton)field0;
        action = (InputAction)field1;
        mods = (InputMod)field2;
    }

    public void GetMouseWheel(out int x, out int y)
    {
        Assert(Type == FrontToBackMessageType.MouseWheel);
        x = field0;
        y = field1;
    }

    public void GetKeyboardKey(out InputKey key, out int scancode, out InputAction action, out InputMod mods)
    {
        Assert(Type == FrontToBackMessageType.KeyboardKey);
        key = (InputKey)field0;
        scancode = field1;
        action = (InputAction)field2;
        mods = (InputMod)field3;
    }

    public void GetKeyboardChar(out int codepoint, out InputMod mods)
    {
        Assert(Type == FrontToBackMessageType.KeyboardChar);
        codepoint = field0;
        mods = (InputMod)field1;
    }

    public void SetQuit()
    {
        Type = FrontToBackMessageType.Quit;
    }

    public void SetSurfaceDamaged()
    {
        Type = FrontToBackMessageType.SurfaceDamaged;
    }

    public void SetSurfaceLost()
    {
        Type = FrontToBackMessageType.SurfaceLost;
    }

    public void SetMouseXY(int x, int y)
    {
        Type = FrontToBackMessageType.MouseXY;
        field0 = x;
        field1 = y;
    }

    public void SetMouseButton(InputButton button, InputAction action, InputMod mods)
    {
        Type = FrontToBackMessageType.MouseButton;
        field0 = (int)button;
        field1 = (int)action;
        field2 = (int)mods;
    }

    public void SetMouseWheel(int x, int y)
    {
        Type = FrontToBackMessageType.MouseWheel;
        field0 = x;
        field1 = y;
    }

    public void SetKeyboardKey(InputKey key, int scancode, InputAction action, InputMod mods)
    {
        Type = FrontToBackMessageType.KeyboardKey;
        field0 = (int)key;
        field1 = scancode;
        field2 = (int)action;
        field3 = (int)mods;
    }

    public void SetKeyboardChar(int codepoint, InputMod mods)
    {
        Type = FrontToBackMessageType.KeyboardChar;
        field0 = codepoint;
        field1 = (int)mods;
    }
}
