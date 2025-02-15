#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Glfw;

/// <summary>
/// Opaque window object.
/// </summary>
readonly struct GlfwWindow
{
    internal readonly nint Value;

    public bool HasValue => Value != 0; 
    public override string ToString() => Value.ToString("x");
}

/// <summary>
/// Opaque monitor object.
/// </summary>
readonly struct GlfwMonitor
{
    internal readonly nint Value;

    internal GlfwMonitor(nint value) => Value = value;

    public bool HasValue => Value != 0; 
    public override string ToString() => Value.ToString("x");
}

/// <summary>
/// Video mode type. This describes a single video mode.
/// </summary>
struct GlfwVidmode
{
    /// <summary>
    /// The width, in screen coordinates, of the video mode.
    /// </summary>
    internal uint Width;
    /// <summary>
    /// The height, in screen coordinates, of the video mode.
    /// </summary>
    internal uint Height;
    /// <summary>
    /// The bit depth of the red channel of the video mode.
    /// </summary>
    internal uint RedBits;
    /// <summary>
    /// The bit depth of the green channel of the video mode.
    /// </summary>
    internal uint GreenBits;
    /// <summary>
    /// The bit depth of the blue channel of the video mode.
    /// </summary>
    internal uint BlueBits;
    /// <summary>
    /// The refresh rate, in Hz, of the video mode.
    /// </summary>
    internal uint RefreshRate;
}

/// <summary>
/// The function pointer type for error callbacks.
/// </summary>
/// <param name="errorcode"></param>
/// <param name="description"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWerrorfun(int errorcode, [MarshalAs(UnmanagedType.LPUTF8Str)] string description);

/// <summary>
/// The function pointer type for window position callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="posx"></param>
/// <param name="posy"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWwindowposfun(GlfwWindow window, int posx, int posy);

/// <summary>
/// The function pointer type for window size callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="width"></param>
/// <param name="height"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWwindowsizefun(GlfwWindow window, int width, int height);

/// <summary>
/// The function pointer type for window close callbacks.
/// </summary>
/// <param name="window"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWwindowclosefun(GlfwWindow window);

/// <summary>
/// The function pointer type for window refresh callbacks.
/// </summary>
/// <param name="window"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWwindowrefreshfun(GlfwWindow window);

/// <summary>
/// The function pointer type for window focus callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="focused"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWwindowfocusfun(GlfwWindow window, int focused);

/// <summary>
/// The function pointer type for window iconify callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="iconified"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWwindowiconifyfun(GlfwWindow window, int iconified);

/// <summary>
/// The function pointer type for framebuffer size callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="width"></param>
/// <param name="height"></param>
[UnmanagedFunctionPointer(Api.CallConvention), SuppressUnmanagedCodeSecurity]
delegate void GLFWframebuffersizefun(GlfwWindow window, int width, int height);

/// <summary>
/// The function pointer type for mouse button callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="button"></param>
/// <param name="action"></param>
/// <param name="mods"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWmousebuttonfun(GlfwWindow window, int button, int action, int mods);

/// <summary>
/// The function pointer type for cursor position callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="mousex"></param>
/// <param name="mousey"></param>
[UnmanagedFunctionPointer(Api.CallConvention), SuppressUnmanagedCodeSecurity]
delegate void GLFWcursorposfun(GlfwWindow window, double mousex, double mousey);

/// <summary>
/// The function pointer type for cursor enter/leave callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="entered"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWcursorenterfun(GlfwWindow window, int entered);

/// <summary>
/// The function pointer type for scroll callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="xoffset"></param>
/// <param name="yoffset"></param>
[UnmanagedFunctionPointer(Api.CallConvention), SuppressUnmanagedCodeSecurity]
delegate void GLFWscrollfun(GlfwWindow window, double xoffset, double yoffset);

/// <summary>
/// The function pointer type for key callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="key"></param>
/// <param name="scancode"></param>
/// <param name="action"></param>
/// <param name="mods"></param>
[UnmanagedFunctionPointer(Api.CallConvention), SuppressUnmanagedCodeSecurity]
delegate void GLFWkeyfun(GlfwWindow window, int key, int scancode, int action, int mods);

/// <summary>
/// The function pointer type for character callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="codepoint"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWcharfun(GlfwWindow window, uint codepoint);

/// <summary>
/// The function pointer type for character with modifiers callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="codepoint"></param>
/// <param name="mods"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWcharmodsfun(GlfwWindow window, int codepoint, int mods);

/// <summary>
/// The function pointer type for file drop callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="count"></param>
/// <param name="paths"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWdropfun(GlfwWindow window, int count, string[] paths);

/// <summary>
/// The function pointer type for monitor callbacks.
/// </summary>
/// <param name="window"></param>
/// <param name="monitorevent"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
delegate void GLFWmonitorfun(GlfwWindow window, int monitorevent);

/// <summary>
/// The function pointer type for joystick callbacks.
/// </summary>
/// <param name="jid"></param>
/// <param name="ev"></param>
[UnmanagedFunctionPointer(Api.CallConvention)]
public delegate void GLFWjoystickfun(int jid, int ev);