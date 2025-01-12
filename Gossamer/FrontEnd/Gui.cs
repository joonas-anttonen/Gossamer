using System.Runtime.InteropServices;
using System.Security;

using Gossamer.Backend;
using Gossamer.Collections;
using Gossamer.External.Glfw;
using Gossamer.Logging;

using static Gossamer.External.Glfw.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Frontend;

public class Gui : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    public delegate long WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

    readonly Logger logger = Gossamer.Instance.Log.GetLogger(nameof(Gui));

    bool isDisposed;
    bool isCreated;

    readonly BackendMessageQueue messageQueue = new();

    GLFWwindow glfwWindow;
    readonly WndProcDelegate glfwCallbackWindowProc;
    readonly GLFWwindowrefreshfun glfwCallbackWindowRefresh;
    readonly GLFWcursorenterfun glfwCallbackMouseEnter;
    readonly GLFWcursorposfun glfwCallbackMouseMove;
    readonly GLFWmousebuttonfun glfwCallbackMouseButton;
    readonly GLFWwindowsizefun glfwCallbackWindowSize;
    readonly GLFWscrollfun glfwCallbackMouseScroll;
    readonly GLFWkeyfun glfwCallbackKeyboardKey;
    readonly GLFWcharfun glfwCallbackKeyboardChar;
    readonly GLFWwindowiconifyfun glfwCallbackWindowIconify;
    readonly GLFWwindowclosefun glfwCallbackWindowClose;

    public bool IsClosing
    {
        get => glfwWindowShouldClose(glfwWindow) == 1;
    }

    internal Gui(BackendMessageQueue messageQueue)
    {
        this.messageQueue = messageQueue;

        var glfwResult = glfwInit();
        ThrowIf(glfwResult != 1, "Failed to initialize GLFW.");

        glfwCallbackWindowProc = Callback_WindowProc;
        glfwCallbackWindowRefresh = Callback_WindowRefresh;
        glfwCallbackMouseEnter = Callback_MouseEnter;
        glfwCallbackMouseMove = Callback_MouseMove;
        glfwCallbackMouseButton = Callback_MouseButton;
        glfwCallbackWindowSize = Callback_WindowSize;
        glfwCallbackMouseScroll = Callback_MouseScroll;
        glfwCallbackKeyboardKey = Callback_KeyboardKey;
        glfwCallbackKeyboardChar = Callback_KeyboardChar;
        glfwCallbackWindowIconify = Callback_WindowIconify;
        glfwCallbackWindowClose = Callback_WindowClose;
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        isDisposed = true;

        glfwTerminate();
    }

    internal unsafe External.Vulkan.VkSurfaceKhr CreateSurface(External.Vulkan.VkInstance instance)
    {
        Assert(isCreated);

        External.Vulkan.VkSurfaceKhr surface = default;
        External.Vulkan.Api.ThrowVulkanIfFailed(glfwCreateWindowSurface(instance, glfwWindow, null, &surface),
            "Failed to create window surface.");

        return surface;
    }

    public void Create()
    {
        Assert(!isCreated);

        // Disable OpenGL
        glfwWindowHint(Constants.GLFW_CLIENT_API, 0);
        // Disable default window frame
        //glfwWindowHint(Constants.GLFW_DECORATED, 0);
        // Disable automatic iconification when focus gets lost in fullscreen mode
        glfwWindowHint(Constants.GLFW_AUTO_ICONIFY, 0);

        glfwWindow = glfwCreateWindow(1920, 1080, "Gossamer");
        ThrowIf(!HasValue(glfwWindow), "Failed to create GLFW window.");

        glfwSetWindowRefreshCallback(glfwWindow, glfwCallbackWindowRefresh);
        glfwSetCursorEnterCallback(glfwWindow, glfwCallbackMouseEnter);
        glfwSetCursorPosCallback(glfwWindow, glfwCallbackMouseMove);
        glfwSetMouseButtonCallback(glfwWindow, glfwCallbackMouseButton);
        glfwSetWindowSizeCallback(glfwWindow, glfwCallbackWindowSize);
        glfwSetScrollCallback(glfwWindow, glfwCallbackMouseScroll);
        glfwSetKeyCallback(glfwWindow, glfwCallbackKeyboardKey);
        glfwSetCharCallback(glfwWindow, glfwCallbackKeyboardChar);
        glfwSetWindowIconifyCallback(glfwWindow, glfwCallbackWindowIconify);
        glfwSetWindowCloseCallback(glfwWindow, glfwCallbackWindowClose);

        isCreated = true;
    }

    public static void PostEmptyEvent()
    {
        glfwPostEmptyEvent();
    }

    public static void WaitForEvents()
    {
        glfwWaitEvents();
    }

    long Callback_WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        return 0;
    }

    void Callback_WindowRefresh(GLFWwindow window)
    {
        messageQueue.PostSurfaceDamaged();
    }

    void Callback_WindowSize(GLFWwindow window, int w, int h)
    {
        messageQueue.PostSurfaceLost();
    }

    void Callback_WindowIconify(GLFWwindow window, int iconified)
    {
    }

    void Callback_WindowClose(GLFWwindow window)
    {
        messageQueue.PostQuit();
    }

    void Callback_MouseEnter(GLFWwindow window, int contained)
    {
    }

    void Callback_MouseMove(GLFWwindow window, double x, double y)
    {
        messageQueue.PostMouseXY((int)(x * 1000), (int)(y * 1000));
    }

    void Callback_MouseButton(GLFWwindow window, int button, int action, int mods)
    {
        messageQueue.PostMouseButton(GetInputButton(button), GetInputAction(action), GetInputMod(mods));
    }

    void Callback_MouseScroll(GLFWwindow window, double x, double y)
    {
        messageQueue.PostMouseWheel((int)(x * 1000), (int)(y * 1000));
    }

    void Callback_KeyboardKey(GLFWwindow window, int key, int code, int action, int mods)
    {
        messageQueue.PostKeyboardKey(GetInputKey(key), code, GetInputAction(action), GetInputMod(mods));
    }

    void Callback_KeyboardChar(GLFWwindow window, uint c)
    {
        messageQueue.PostKeyboardChar((int)c, 0);
    }

    static InputButton GetInputButton(int button)
    {
        return button switch
        {
            Constants.GLFW_MOUSE_BUTTON_LEFT => InputButton.Left,
            Constants.GLFW_MOUSE_BUTTON_RIGHT => InputButton.Right,
            Constants.GLFW_MOUSE_BUTTON_MIDDLE => InputButton.Middle,
            _ => InputButton.Unknown,
        };
    }

    static InputAction GetInputAction(int action)
    {
        return action switch
        {
            Constants.GLFW_PRESS => InputAction.Press,
            Constants.GLFW_RELEASE => InputAction.Release,
            Constants.GLFW_REPEAT => InputAction.Repeat,
            _ => InputAction.Unknown,
        };
    }

    static InputMod GetInputMod(int mods)
    {
        InputMod result = InputMod.None;

        if ((mods & Constants.GLFW_MOD_SHIFT) != 0)
        {
            result |= InputMod.Shift;
        }

        if ((mods & Constants.GLFW_MOD_CONTROL) != 0)
        {
            result |= InputMod.Control;
        }

        if ((mods & Constants.GLFW_MOD_ALT) != 0)
        {
            result |= InputMod.Alt;
        }

        if ((mods & Constants.GLFW_MOD_SUPER) != 0)
        {
            result |= InputMod.Super;
        }

        return result;
    }

    static InputKey GetInputKey(int key)
    {
        return key switch
        {
            Constants.GLFW_KEY_SPACE => InputKey.SPACE,
            Constants.GLFW_KEY_APOSTROPHE => InputKey.APOSTROPHE,
            Constants.GLFW_KEY_COMMA => InputKey.COMMA,
            Constants.GLFW_KEY_MINUS => InputKey.MINUS,
            Constants.GLFW_KEY_PERIOD => InputKey.PERIOD,
            Constants.GLFW_KEY_SLASH => InputKey.SLASH,
            Constants.GLFW_KEY_0 => InputKey.N_0,
            Constants.GLFW_KEY_1 => InputKey.N_1,
            Constants.GLFW_KEY_2 => InputKey.N_2,
            Constants.GLFW_KEY_3 => InputKey.N_3,
            Constants.GLFW_KEY_4 => InputKey.N_4,
            Constants.GLFW_KEY_5 => InputKey.N_5,
            Constants.GLFW_KEY_6 => InputKey.N_6,
            Constants.GLFW_KEY_7 => InputKey.N_7,
            Constants.GLFW_KEY_8 => InputKey.N_8,
            Constants.GLFW_KEY_9 => InputKey.N_9,
            Constants.GLFW_KEY_SEMICOLON => InputKey.SEMICOLON,
            Constants.GLFW_KEY_EQUAL => InputKey.EQUAL,
            Constants.GLFW_KEY_A => InputKey.A,
            Constants.GLFW_KEY_B => InputKey.B,
            Constants.GLFW_KEY_C => InputKey.C,
            Constants.GLFW_KEY_D => InputKey.D,
            Constants.GLFW_KEY_E => InputKey.E,
            Constants.GLFW_KEY_F => InputKey.F,
            Constants.GLFW_KEY_G => InputKey.G,
            Constants.GLFW_KEY_H => InputKey.H,
            Constants.GLFW_KEY_I => InputKey.I,
            Constants.GLFW_KEY_J => InputKey.J,
            Constants.GLFW_KEY_K => InputKey.K,
            Constants.GLFW_KEY_L => InputKey.L,
            Constants.GLFW_KEY_M => InputKey.M,
            Constants.GLFW_KEY_N => InputKey.N,
            Constants.GLFW_KEY_O => InputKey.O,
            Constants.GLFW_KEY_P => InputKey.P,
            Constants.GLFW_KEY_Q => InputKey.Q,
            Constants.GLFW_KEY_R => InputKey.R,
            Constants.GLFW_KEY_S => InputKey.S,
            Constants.GLFW_KEY_T => InputKey.T,
            Constants.GLFW_KEY_U => InputKey.U,
            Constants.GLFW_KEY_V => InputKey.V,
            Constants.GLFW_KEY_W => InputKey.W,
            Constants.GLFW_KEY_X => InputKey.X,
            Constants.GLFW_KEY_Y => InputKey.Y,
            Constants.GLFW_KEY_Z => InputKey.Z,
            Constants.GLFW_KEY_UP => InputKey.UP,
            Constants.GLFW_KEY_DOWN => InputKey.DOWN,
            Constants.GLFW_KEY_LEFT => InputKey.LEFT,
            Constants.GLFW_KEY_RIGHT => InputKey.RIGHT,
            Constants.GLFW_KEY_LEFT_SHIFT => InputKey.LEFT_SHIFT,
            Constants.GLFW_KEY_RIGHT_SHIFT => InputKey.RIGHT_SHIFT,
            Constants.GLFW_KEY_LEFT_CONTROL => InputKey.LEFT_CONTROL,
            Constants.GLFW_KEY_RIGHT_CONTROL => InputKey.RIGHT_CONTROL,
            Constants.GLFW_KEY_LEFT_ALT => InputKey.LEFT_ALT,
            Constants.GLFW_KEY_RIGHT_ALT => InputKey.RIGHT_ALT,
            Constants.GLFW_KEY_LEFT_SUPER => InputKey.LEFT_SUPER,
            Constants.GLFW_KEY_RIGHT_SUPER => InputKey.RIGHT_SUPER,
            Constants.GLFW_KEY_MENU => InputKey.MENU,
            Constants.GLFW_KEY_ESCAPE => InputKey.ESCAPE,
            Constants.GLFW_KEY_ENTER => InputKey.ENTER,
            Constants.GLFW_KEY_TAB => InputKey.TAB,
            Constants.GLFW_KEY_BACKSPACE => InputKey.BACKSPACE,
            Constants.GLFW_KEY_INSERT => InputKey.INSERT,
            Constants.GLFW_KEY_DELETE => InputKey.DELETE,
            Constants.GLFW_KEY_PAGE_UP => InputKey.PAGE_UP,
            Constants.GLFW_KEY_PAGE_DOWN => InputKey.PAGE_DOWN,
            Constants.GLFW_KEY_HOME => InputKey.HOME,
            Constants.GLFW_KEY_END => InputKey.END,
            Constants.GLFW_KEY_CAPS_LOCK => InputKey.CAPS_LOCK,
            Constants.GLFW_KEY_SCROLL_LOCK => InputKey.SCROLL_LOCK,
            Constants.GLFW_KEY_NUM_LOCK => InputKey.NUM_LOCK,
            Constants.GLFW_KEY_F1 => InputKey.F1,
            Constants.GLFW_KEY_F2 => InputKey.F2,
            Constants.GLFW_KEY_F3 => InputKey.F3,
            Constants.GLFW_KEY_F4 => InputKey.F4,
            Constants.GLFW_KEY_F5 => InputKey.F5,
            Constants.GLFW_KEY_F6 => InputKey.F6,
            Constants.GLFW_KEY_F7 => InputKey.F7,
            Constants.GLFW_KEY_F8 => InputKey.F8,
            Constants.GLFW_KEY_F9 => InputKey.F9,
            Constants.GLFW_KEY_F10 => InputKey.F10,
            Constants.GLFW_KEY_F11 => InputKey.F11,
            Constants.GLFW_KEY_F12 => InputKey.F12,
            Constants.GLFW_KEY_KP_0 => InputKey.KP_0,
            Constants.GLFW_KEY_KP_1 => InputKey.KP_1,
            Constants.GLFW_KEY_KP_2 => InputKey.KP_2,
            Constants.GLFW_KEY_KP_3 => InputKey.KP_3,
            Constants.GLFW_KEY_KP_4 => InputKey.KP_4,
            Constants.GLFW_KEY_KP_5 => InputKey.KP_5,
            Constants.GLFW_KEY_KP_6 => InputKey.KP_6,
            Constants.GLFW_KEY_KP_7 => InputKey.KP_7,
            Constants.GLFW_KEY_KP_8 => InputKey.KP_8,
            Constants.GLFW_KEY_KP_9 => InputKey.KP_9,
            Constants.GLFW_KEY_KP_DECIMAL => InputKey.KP_DECIMAL,
            Constants.GLFW_KEY_KP_DIVIDE => InputKey.KP_DIVIDE,
            Constants.GLFW_KEY_KP_MULTIPLY => InputKey.KP_MULTIPLY,
            Constants.GLFW_KEY_KP_SUBTRACT => InputKey.KP_SUBTRACT,
            Constants.GLFW_KEY_KP_ADD => InputKey.KP_ADD,
            Constants.GLFW_KEY_KP_ENTER => InputKey.KP_ENTER,
            Constants.GLFW_KEY_KP_EQUAL => InputKey.KP_EQUAL,
            _ => InputKey.UNKNOWN,
        };
    }
}