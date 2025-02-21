namespace Gossamer.Backend;

public enum MouseMode
{
    Normal,
    Hidden,
    Disabled,
}

[Flags]
public enum InputMods
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Super = 8,
}

public enum InputButton
{
    Unknown,
    X1,
    X2,
    X3,
    X4,
    X5,
    X6,
    X7,
    X8,
    Left,
    Right,
    Middle,
}

public enum InputAction
{
    Unknown,
    Release,
    Press,
    Repeat,
}

public enum InputKey
{
    UNKNOWN,
    SPACE,
    APOSTROPHE,
    COMMA,
    MINUS,
    PERIOD,
    SLASH,
    N_0, N_1, N_2, N_3, N_4, N_5, N_6, N_7, N_8, N_9,
    SEMICOLON,
    EQUAL,
    A, B, C, D, E, F, G, H, I, J,
    K, L, M, N, O, P, Q, R, S, T,
    U, V, W, X, Y, Z,
    LEFT_BRACKET,
    BACKSLASH,
    RIGHT_BRACKET,
    GRAVE_ACCENT,
    WORLD_1, WORLD_2,
    ESCAPE,
    ENTER,
    TAB,
    BACKSPACE,
    INSERT,
    DELETE,
    RIGHT, LEFT, DOWN, UP,
    PAGE_UP, PAGE_DOWN,
    HOME, END,
    CAPS_LOCK, SCROLL_LOCK, NUM_LOCK,
    PRINT_SCREEN,
    PAUSE,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10,
    F11, F12, F13, F14, F15, F16, F17, F18, F19, F20,
    F21, F22, F23, F24, F25,
    KP_0, KP_1, KP_2, KP_3, KP_4, KP_5, KP_6, KP_7, KP_8, KP_9,
    KP_DECIMAL,
    KP_DIVIDE,
    KP_MULTIPLY,
    KP_SUBTRACT,
    KP_ADD,
    KP_ENTER,
    KP_EQUAL,
    LEFT_SHIFT, LEFT_CONTROL, LEFT_ALT, LEFT_SUPER,
    RIGHT_SHIFT, RIGHT_CONTROL, RIGHT_ALT, RIGHT_SUPER,
    MENU,
}