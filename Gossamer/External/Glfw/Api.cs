#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Glfw;

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "External/libgossamer-glfw";
    public const CallingConvention CallConvention = CallingConvention.Cdecl;

    public static class Constants
    {
        public const int GLFW_RELEASE = 0;
        public const int GLFW_PRESS = 1;
        public const int GLFW_REPEAT = 2;
        public const int GLFW_KEY_UNKNOWN = -1;
        public const int GLFW_KEY_SPACE = 32;
        public const int GLFW_KEY_APOSTROPHE = 39;
        public const int GLFW_KEY_COMMA = 44;
        public const int GLFW_KEY_MINUS = 45;
        public const int GLFW_KEY_PERIOD = 46;
        public const int GLFW_KEY_SLASH = 47;
        public const int GLFW_KEY_0 = 48;
        public const int GLFW_KEY_1 = 49;
        public const int GLFW_KEY_2 = 50;
        public const int GLFW_KEY_3 = 51;
        public const int GLFW_KEY_4 = 52;
        public const int GLFW_KEY_5 = 53;
        public const int GLFW_KEY_6 = 54;
        public const int GLFW_KEY_7 = 55;
        public const int GLFW_KEY_8 = 56;
        public const int GLFW_KEY_9 = 57;
        public const int GLFW_KEY_SEMICOLON = 59;
        public const int GLFW_KEY_EQUAL = 61;
        public const int GLFW_KEY_A = 65;
        public const int GLFW_KEY_B = 66;
        public const int GLFW_KEY_C = 67;
        public const int GLFW_KEY_D = 68;
        public const int GLFW_KEY_E = 69;
        public const int GLFW_KEY_F = 70;
        public const int GLFW_KEY_G = 71;
        public const int GLFW_KEY_H = 72;
        public const int GLFW_KEY_I = 73;
        public const int GLFW_KEY_J = 74;
        public const int GLFW_KEY_K = 75;
        public const int GLFW_KEY_L = 76;
        public const int GLFW_KEY_M = 77;
        public const int GLFW_KEY_N = 78;
        public const int GLFW_KEY_O = 79;
        public const int GLFW_KEY_P = 80;
        public const int GLFW_KEY_Q = 81;
        public const int GLFW_KEY_R = 82;
        public const int GLFW_KEY_S = 83;
        public const int GLFW_KEY_T = 84;
        public const int GLFW_KEY_U = 85;
        public const int GLFW_KEY_V = 86;
        public const int GLFW_KEY_W = 87;
        public const int GLFW_KEY_X = 88;
        public const int GLFW_KEY_Y = 89;
        public const int GLFW_KEY_Z = 90;
        public const int GLFW_KEY_LEFT_BRACKET = 91;
        public const int GLFW_KEY_BACKSLASH = 92;
        public const int GLFW_KEY_RIGHT_BRACKET = 93;
        public const int GLFW_KEY_GRAVE_ACCENT = 96;
        public const int GLFW_KEY_WORLD_1 = 161;
        public const int GLFW_KEY_WORLD_2 = 162;
        public const int GLFW_KEY_ESCAPE = 256;
        public const int GLFW_KEY_ENTER = 257;
        public const int GLFW_KEY_TAB = 258;
        public const int GLFW_KEY_BACKSPACE = 259;
        public const int GLFW_KEY_INSERT = 260;
        public const int GLFW_KEY_DELETE = 261;
        public const int GLFW_KEY_RIGHT = 262;
        public const int GLFW_KEY_LEFT = 263;
        public const int GLFW_KEY_DOWN = 264;
        public const int GLFW_KEY_UP = 265;
        public const int GLFW_KEY_PAGE_UP = 266;
        public const int GLFW_KEY_PAGE_DOWN = 267;
        public const int GLFW_KEY_HOME = 268;
        public const int GLFW_KEY_END = 269;
        public const int GLFW_KEY_CAPS_LOCK = 280;
        public const int GLFW_KEY_SCROLL_LOCK = 281;
        public const int GLFW_KEY_NUM_LOCK = 282;
        public const int GLFW_KEY_PRINT_SCREEN = 283;
        public const int GLFW_KEY_PAUSE = 284;
        public const int GLFW_KEY_F1 = 290;
        public const int GLFW_KEY_F2 = 291;
        public const int GLFW_KEY_F3 = 292;
        public const int GLFW_KEY_F4 = 293;
        public const int GLFW_KEY_F5 = 294;
        public const int GLFW_KEY_F6 = 295;
        public const int GLFW_KEY_F7 = 296;
        public const int GLFW_KEY_F8 = 297;
        public const int GLFW_KEY_F9 = 298;
        public const int GLFW_KEY_F10 = 299;
        public const int GLFW_KEY_F11 = 300;
        public const int GLFW_KEY_F12 = 301;
        public const int GLFW_KEY_F13 = 302;
        public const int GLFW_KEY_F14 = 303;
        public const int GLFW_KEY_F15 = 304;
        public const int GLFW_KEY_F16 = 305;
        public const int GLFW_KEY_F17 = 306;
        public const int GLFW_KEY_F18 = 307;
        public const int GLFW_KEY_F19 = 308;
        public const int GLFW_KEY_F20 = 309;
        public const int GLFW_KEY_F21 = 310;
        public const int GLFW_KEY_F22 = 311;
        public const int GLFW_KEY_F23 = 312;
        public const int GLFW_KEY_F24 = 313;
        public const int GLFW_KEY_F25 = 314;
        public const int GLFW_KEY_KP_0 = 320;
        public const int GLFW_KEY_KP_1 = 321;
        public const int GLFW_KEY_KP_2 = 322;
        public const int GLFW_KEY_KP_3 = 323;
        public const int GLFW_KEY_KP_4 = 324;
        public const int GLFW_KEY_KP_5 = 325;
        public const int GLFW_KEY_KP_6 = 326;
        public const int GLFW_KEY_KP_7 = 327;
        public const int GLFW_KEY_KP_8 = 328;
        public const int GLFW_KEY_KP_9 = 329;
        public const int GLFW_KEY_KP_DECIMAL = 330;
        public const int GLFW_KEY_KP_DIVIDE = 331;
        public const int GLFW_KEY_KP_MULTIPLY = 332;
        public const int GLFW_KEY_KP_SUBTRACT = 333;
        public const int GLFW_KEY_KP_ADD = 334;
        public const int GLFW_KEY_KP_ENTER = 335;
        public const int GLFW_KEY_KP_EQUAL = 336;
        public const int GLFW_KEY_LEFT_SHIFT = 340;
        public const int GLFW_KEY_LEFT_CONTROL = 341;
        public const int GLFW_KEY_LEFT_ALT = 342;
        public const int GLFW_KEY_LEFT_SUPER = 343;
        public const int GLFW_KEY_RIGHT_SHIFT = 344;
        public const int GLFW_KEY_RIGHT_CONTROL = 345;
        public const int GLFW_KEY_RIGHT_ALT = 346;
        public const int GLFW_KEY_RIGHT_SUPER = 347;
        public const int GLFW_KEY_MENU = 348;
        public const int GLFW_KEY_LAST = GLFW_KEY_MENU;
        public const int GLFW_MOD_SHIFT = 0x0001;
        public const int GLFW_MOD_CONTROL = 0x0002;
        public const int GLFW_MOD_ALT = 0x0004;
        public const int GLFW_MOD_SUPER = 0x0008;
        public const int GLFW_MOUSE_BUTTON_1 = 0;
        public const int GLFW_MOUSE_BUTTON_2 = 1;
        public const int GLFW_MOUSE_BUTTON_3 = 2;
        public const int GLFW_MOUSE_BUTTON_4 = 3;
        public const int GLFW_MOUSE_BUTTON_5 = 4;
        public const int GLFW_MOUSE_BUTTON_6 = 5;
        public const int GLFW_MOUSE_BUTTON_7 = 6;
        public const int GLFW_MOUSE_BUTTON_8 = 7;
        public const int GLFW_MOUSE_BUTTON_LAST = GLFW_MOUSE_BUTTON_8;
        public const int GLFW_MOUSE_BUTTON_LEFT = GLFW_MOUSE_BUTTON_1;
        public const int GLFW_MOUSE_BUTTON_RIGHT = GLFW_MOUSE_BUTTON_2;
        public const int GLFW_MOUSE_BUTTON_MIDDLE = GLFW_MOUSE_BUTTON_3;
        public const int GLFW_JOYSTICK_1 = 0;
        public const int GLFW_JOYSTICK_2 = 1;
        public const int GLFW_JOYSTICK_3 = 2;
        public const int GLFW_JOYSTICK_4 = 3;
        public const int GLFW_JOYSTICK_5 = 4;
        public const int GLFW_JOYSTICK_6 = 5;
        public const int GLFW_JOYSTICK_7 = 6;
        public const int GLFW_JOYSTICK_8 = 7;
        public const int GLFW_JOYSTICK_9 = 8;
        public const int GLFW_JOYSTICK_10 = 9;
        public const int GLFW_JOYSTICK_11 = 10;
        public const int GLFW_JOYSTICK_12 = 11;
        public const int GLFW_JOYSTICK_13 = 12;
        public const int GLFW_JOYSTICK_14 = 13;
        public const int GLFW_JOYSTICK_15 = 14;
        public const int GLFW_JOYSTICK_16 = 15;
        public const int GLFW_JOYSTICK_LAST = GLFW_JOYSTICK_16;
        public const int GLFW_TRUE = 1;
        public const int GLFW_FALSE = 0;
        public const int GLFW_MAXIMIZED = 0x00020008;
        public const int GLFW_NOT_INITIALIZED = 0x00010001;
        public const int GLFW_NO_CURRENT_CONTEXT = 0x00010002;
        public const int GLFW_INVALID_ENUM = 0x00010003;
        public const int GLFW_INVALID_VALUE = 0x00010004;
        public const int GLFW_OUT_OF_MEMORY = 0x00010005;
        public const int GLFW_API_UNAVAILABLE = 0x00010006;
        public const int GLFW_VERSION_UNAVAILABLE = 0x00010007;
        public const int GLFW_PLATFORM_ERROR = 0x00010008;
        public const int GLFW_FORMAT_UNAVAILABLE = 0x00010009;
        public const int GLFW_FOCUSED = 0x00020001;
        public const int GLFW_ICONIFIED = 0x00020002;
        public const int GLFW_RESIZABLE = 0x00020003;
        public const int GLFW_VISIBLE = 0x00020004;
        public const int GLFW_DECORATED = 0x00020005;
        public const int GLFW_AUTO_ICONIFY = 0x00020006;
        public const int GLFW_FLOATING = 0x00020007;
        public const int GLFW_RED_BITS = 0x00021001;
        public const int GLFW_GREEN_BITS = 0x00021002;
        public const int GLFW_BLUE_BITS = 0x00021003;
        public const int GLFW_ALPHA_BITS = 0x00021004;
        public const int GLFW_DEPTH_BITS = 0x00021005;
        public const int GLFW_STENCIL_BITS = 0x00021006;
        public const int GLFW_ACCUM_RED_BITS = 0x00021007;
        public const int GLFW_ACCUM_GREEN_BITS = 0x00021008;
        public const int GLFW_ACCUM_BLUE_BITS = 0x00021009;
        public const int GLFW_ACCUM_ALPHA_BITS = 0x0002100A;
        public const int GLFW_AUX_BUFFERS = 0x0002100B;
        public const int GLFW_STEREO = 0x0002100C;
        public const int GLFW_SAMPLES = 0x0002100D;
        public const int GLFW_SRGB_CAPABLE = 0x0002100E;
        public const int GLFW_REFRESH_RATE = 0x0002100F;
        public const int GLFW_DOUBLEBUFFER = 0x00021010;
        public const int GLFW_CLIENT_API = 0x00022001;
        public const int GLFW_CONTEXT_VERSION_MAJOR = 0x00022002;
        public const int GLFW_CONTEXT_VERSION_MINOR = 0x00022003;
        public const int GLFW_CONTEXT_REVISION = 0x00022004;
        public const int GLFW_CONTEXT_ROBUSTNESS = 0x00022005;
        public const int GLFW_OPENGL_FORWARD_COMPAT = 0x00022006;
        public const int GLFW_OPENGL_DEBUG_CONTEXT = 0x00022007;
        public const int GLFW_OPENGL_PROFILE = 0x00022008;
        public const int GLFW_CONTEXT_RELEASE_BEHAVIOR = 0x00022009;
        public const int GLFW_NO_API = 0;
        public const int GLFW_OPENGL_API = 0x00030001;
        public const int GLFW_OPENGL_ES_API = 0x00030002;
        public const int GLFW_NO_ROBUSTNESS = 0;
        public const int GLFW_NO_RESET_NOTIFICATION = 0x00031001;
        public const int GLFW_LOSE_CONTEXT_ON_RESET = 0x00031002;
        public const int GLFW_OPENGL_ANY_PROFILE = 0;
        public const int GLFW_OPENGL_CORE_PROFILE = 0x00032001;
        public const int GLFW_OPENGL_COMPAT_PROFILE = 0x00032002;
        public const int GLFW_CURSOR = 0x00033001;
        public const int GLFW_STICKY_KEYS = 0x00033002;
        public const int GLFW_STICKY_MOUSE_BUTTONS = 0x00033003;
        public const int GLFW_CURSOR_NORMAL = 0x00034001;
        public const int GLFW_CURSOR_HIDDEN = 0x00034002;
        public const int GLFW_CURSOR_DISABLED = 0x00034003;
        public const int GLFW_ANY_RELEASE_BEHAVIOR = 0;
        public const int GLFW_RELEASE_BEHAVIOR_FLUSH = 0x00035001;
        public const int GLFW_RELEASE_BEHAVIOR_NONE = 0x00035002;
        public const int GLFW_ARROW_CURSOR = 0x00036001;
        public const int GLFW_IBEAM_CURSOR = 0x00036002;
        public const int GLFW_CROSSHAIR_CURSOR = 0x00036003;
        public const int GLFW_HAND_CURSOR = 0x00036004;
        public const int GLFW_HRESIZE_CURSOR = 0x00036005;
        public const int GLFW_VRESIZE_CURSOR = 0x00036006;
        public const int GLFW_CONNECTED = 0x00040001;
        public const int GLFW_DISCONNECTED = 0x00040002;

        public const int GLFW_WAYLAND_LIBDECOR = 0x00053001;
        public const int GLFW_X11_XCB_VULKAN_SURFACE = 0x00052001;

        public const int GLFW_PLATFORM = 0x00050003;
        public const int GLFW_ANY_PLATFORM = 0x00060000;
        public const int GLFW_PLATFORM_WIN32 = 0x00060001;
        public const int GLFW_PLATFORM_COCOA = 0x00060002;
        public const int GLFW_PLATFORM_WAYLAND = 0x00060003;
        public const int GLFW_PLATFORM_X11 = 0x00060004;
        public const int GLFW_PLATFORM_NULL = 0x00060005;
    }

    /// <summary>
    /// Initializes the GLFW library.
    /// <para/>
    /// This function initializes the GLFW library.  Before most GLFW functions can
    /// be used, GLFW must be initialized, and before an application terminates GLFW
    /// should be terminated in order to free any resources allocated during or
    /// after initialization.
    /// </summary>
    /// <returns>`GLFW_TRUE` if successful, or `GLFW_FALSE` if an error occurred</returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int glfwInit();

    /// <summary>
    /// Terminates the GLFW library.
    /// <para/>
    /// This function destroys all remaining windows and cursors, restores any
    /// modified gamma ramps and frees any other allocated resources.  Once this
    /// function is called, you must again call @ref glfwInit successfully before
    /// you will be able to use most GLFW functions.
    ///
    /// If GLFW has been successfully initialized, this function should be called
    /// before the application exits.  If initialization fails, there is no need to
    /// call this function, as it is called by @ref glfwInit before it returns
    /// failure.
    /// </summary>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwTerminate();

    /// <summary>
    /// Sets the specified init hint to the desired value.
    /// <para/>
    /// This function sets hints for the next initialization of GLFW.
    ///
    /// The values you set hints to are never reset by GLFW, but they only take
    /// effect during initialization.  Once GLFW has been initialized, any values
    /// you set will be ignored until the library is terminated and initialized
    /// again.
    ///
    /// Some hints are platform specific.  These may be set on any platform but they
    /// will only affect their specific platform.  Other platforms will ignore them.
    /// Setting these hints requires no platform specific headers or functions.
    /// </summary>
    /// <param name="hint"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwInitHint(int hint, int value);

    /// <summary>
    /// Retrieves the version of the GLFW library.
    /// </summary>
    /// <param name="major"></param>
    /// <param name="minor"></param>
    /// <param name="rev"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetVersion(int* major, int* minor, int* rev);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int glfwGetPlatform();

    /// <summary>
    /// Returns a string describing the compile-time configuration.
    /// </summary>
    /// <returns></returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    public static extern string glfwGetVersionString();

    /// <summary>
    /// Waits until events are queued and processes them.
    /// </summary>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwWaitEvents();

    /// <summary>
    /// This function puts the calling thread to sleep until at least one event is available in the event queue, or until the specified timeout is reached. 
    /// If one or more events are available, it behaves exactly like glfwPollEvents, i.e. the events in the queue are processed and the function then returns immediately. 
    /// Processing events will cause the window and input callbacks associated with those events to be called.
    /// </summary>
    /// <param name="timeout"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwWaitEventsTimeout(double timeout);

    /// <summary>
    /// Posts an empty event to the event queue.
    /// </summary>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwPostEmptyEvent();

    /// <summary>
    /// Sets the specified window hint to the desired value.
    /// </summary>
    /// <param name="hint">The window hint to set.</param>
    /// <param name="value">The new value of the window hint.</param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwWindowHint(int hint, int value);

    /// <summary>
    /// Creates a window and its associated context.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="title"></param>
    /// <param name="monitor"></param>
    /// <param name="share"></param>
    /// <returns>The handle of the created window, or `NULL` if an error occured.</returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GlfwWindow glfwCreateWindow(int width, int height, [MarshalAs(UnmanagedType.LPUTF8Str)] string title, GlfwMonitor monitor = default, GlfwWindow share = default);

    /// <summary>
    /// Destroys the specified window and its context.
    /// </summary>
    /// <param name="window">The window to destroy.</param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwDestroyWindow(GlfwWindow window);

    /// <summary>
    /// This function returns the opacity of the window, including any decorations.
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern float glfwGetWindowOpacity(GlfwWindow window);

    /// <summary>
    /// This function sets the opacity of the window, including any decorations.
    /// The opacity (or alpha) value is a positive finite number between zero and one, where zero is fully transparent and one is fully opaque.
    /// The initial opacity value for newly created windows is one.
    /// A window created with framebuffer transparency may not use whole window transparency. The results of doing this are undefined.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="opacity"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwSetWindowOpacity(GlfwWindow window, float opacity);

    /// <summary>
    /// This function returns the value of an attribute of the specified window
    /// </summary>
    /// <param name="window"></param>
    /// <param name="attrib"></param>
    /// <returns></returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int glfwGetWindowAttrib(GlfwWindow window, int attrib);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="window"></param>
    /// <param name="value"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwSetWindowShouldClose(GlfwWindow window, bool value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="window"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwIconifyWindow(GlfwWindow window);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="window"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwRestoreWindow(GlfwWindow window);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="window"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwSetWindowSize(GlfwWindow window, int width, int height);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="window"></param>
    /// <param name="xpos"></param>
    /// <param name="ypos"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetCursorPos(GlfwWindow window, out double xpos, out double ypos);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="window"></param>
    /// <param name="monitor"></param>
    /// <param name="xpos"></param>
    /// <param name="ypos"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="refreshRate"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwSetWindowMonitor(GlfwWindow window, GlfwMonitor monitor, int xpos, int ypos, int width, int height, int refreshRate);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="window"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwMaximizeWindow(GlfwWindow window);

    /// <summary>
    /// Checks the close flag of the specified window.
    /// </summary>
    /// <param name="window">The window to query.</param>
    /// <returns>The value of the close flag.</returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern int glfwWindowShouldClose(GlfwWindow window);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWerrorfun glfwSetErrorCallback(GLFWerrorfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWmonitorfun glfwSetMonitorCallback(GLFWmonitorfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWwindowposfun glfwSetWindowPosCallback(GlfwWindow window, GLFWwindowposfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWwindowsizefun glfwSetWindowSizeCallback(GlfwWindow window, GLFWwindowsizefun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWwindowclosefun glfwSetWindowCloseCallback(GlfwWindow window, GLFWwindowclosefun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWwindowrefreshfun glfwSetWindowRefreshCallback(GlfwWindow window, GLFWwindowrefreshfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWwindowfocusfun glfwSetWindowFocusCallback(GlfwWindow window, GLFWwindowfocusfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWwindowiconifyfun glfwSetWindowIconifyCallback(GlfwWindow window, GLFWwindowiconifyfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWframebuffersizefun glfwSetFramebufferSizeCallback(GlfwWindow window, GLFWframebuffersizefun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWkeyfun glfwSetKeyCallback(GlfwWindow window, GLFWkeyfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWcharfun glfwSetCharCallback(GlfwWindow window, GLFWcharfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWcharmodsfun glfwSetCharModsCallback(GlfwWindow window, GLFWcharmodsfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWmousebuttonfun glfwSetMouseButtonCallback(GlfwWindow window, GLFWmousebuttonfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWcursorposfun glfwSetCursorPosCallback(GlfwWindow window, GLFWcursorposfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWcursorenterfun glfwSetCursorEnterCallback(GlfwWindow window, GLFWcursorenterfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWscrollfun glfwSetScrollCallback(GlfwWindow window, GLFWscrollfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWdropfun glfwSetDropCallback(GlfwWindow window, GLFWdropfun cbfun);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GLFWjoystickfun glfwSetJoystickCallback(GLFWjoystickfun cbfun);

    /// <summary>
    /// Creates a Vulkan surface for the specified window. 
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="window"></param>
    /// <param name="allocator"></param>
    /// <param name="surface"></param>
    /// <returns></returns>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern Vulkan.VkResult glfwCreateWindowSurface(Vulkan.VkInstance instance, GlfwWindow window, Vulkan.VkAllocationCallbacks* allocator, Vulkan.VkSurfaceKhr* surface);

    [DllImport(BinaryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte** glfwGetRequiredInstanceExtensions(uint* count);

    [DllImport(BinaryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int glfwGetError(byte** description);

    [DllImport(BinaryName, CallingConvention = CallingConvention.Cdecl)]
    static extern string glfwGetClipboardString(IntPtr window);

    public static string glfwGetClipboardString() => glfwGetClipboardString(IntPtr.Zero);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern long[] glfwGetVideoModes(nint monitor, out int count);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern GlfwMonitor glfwGetPrimaryMonitor();

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetMonitorWorkarea(GlfwMonitor monitor, out int xpos, out int ypos, out int width, out int height);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwSetWindowSizeLimits(GlfwWindow window, int minWidth, int minHeight, int maxWidth, int maxHeight);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    static extern void** glfwGetMonitors(int* count);

    public static int glfwGetMonitorCount()
    {
        int count = 0;
        glfwGetMonitors(&count);
        return count;
    }

    public static GlfwMonitor glfwGetMonitor(int index)
    {
        int count = 0;
        void** pMonitors = glfwGetMonitors(&count);
        return new GlfwMonitor((nint)pMonitors[index]);
    }

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwShowWindow(GlfwWindow window);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwHideWindow(GlfwWindow window);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint glfwRequestWindowAttention(GlfwWindow window);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint glfwGetWindowUserPointer(GlfwWindow window);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetVersion(out int major, out int minor, out int rev);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetMonitorPos(GlfwMonitor monitor, out int xpos, out int ypos);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetMonitorPhysicalSize(GlfwMonitor monitor, out int widthMM, out int heightMM);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetWindowPos(GlfwWindow window, out int xpos, out int ypos);

    /// <summary>
    /// Retrieves the size of the client area of the specified window.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetWindowSize(GlfwWindow window, out int width, out int height);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetFramebufferSize(GlfwWindow window, out int width, out int height);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwGetWindowFrameSize(GlfwWindow window, out int left, out int top, out int right, out int bottom);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "glfwGetVideoMode")]
    static extern nint _glfwGetVideoMode(GlfwMonitor monitor);

    [StructLayout(LayoutKind.Sequential)]
    public struct GlfwVideoMode
    {
        public uint Width;
        public uint Height;
        public uint RedBits;
        public uint GreenBits;
        public uint BlueBits;
        public uint RefreshRate;
    }

    public static GlfwVideoMode glfwGetVideoMode(GlfwMonitor monitor)
    {
        nint pVideoMode = _glfwGetVideoMode(monitor);
        return Marshal.PtrToStructure<GlfwVideoMode>(pVideoMode);
    }

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwSetWindowTitle(GlfwWindow window, string title);

    /// <summary>
    /// Sets the position of the client area of the specified window.
    /// Wayland: There is no way for an application to set the global position of its windows.
    /// </summary>
    /// <param name="window"></param>
    /// <param name="xpos"></param>
    /// <param name="ypos"></param>
    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void glfwSetWindowPos(GlfwWindow window, int xpos, int ypos);
}