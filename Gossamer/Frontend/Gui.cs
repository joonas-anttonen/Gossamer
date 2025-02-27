using System.Runtime.InteropServices;

using Gossamer.Backend;
using Gossamer.External.Glfw;
using Gossamer.Logging;
using Gossamer.Utilities;

using static Gossamer.External.Glfw.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Frontend;

public class Gui : IDisposable
{
    public enum Platform
    {
        /// <summary>
        /// Automatically choose the best platform based on the current OS.
        /// </summary>
        Auto,
        /// <summary>
        /// Use the Win32 platform. Only valid on Windows.
        /// </summary>
        Win32,
        /// <summary>
        /// Use the Wayland platform. Only valid on Linux.
        /// </summary>
        Wayland,
        /// <summary>
        /// Use the X11 platform. Only valid on Linux.
        /// </summary>
        X11,
    }

    const float ControlsOffFromFrameSide = 7f;
    const float ControlButtonWidth = 40f;
    const float ControlsButtonSeparation = 2f;

    readonly Platform platform = Platform.Auto;

    readonly Logger logger = Gossamer.GetLogger(nameof(Gui));

    bool isDisposed;
    bool isCreated;

    readonly Gfx gfx;
    readonly BackendMessageQueue messageQueue = new();

    readonly GuiParameters parameters = new();

    GlfwWindow glfwWindow;

    bool isIconified;
    bool isMaximized;
    bool isFullscreen;
    //bool isDamaged;
    readonly bool useFullscreen = true;

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

    internal Gui(Gossamer.Parameters parameters, Gfx gfx, BackendMessageQueue messageQueue)
    {
        this.gfx = gfx;
        this.messageQueue = messageQueue;

        var wantedPlatform = parameters.Platform;

        // If platform is not specified, choose the best one for the current OS
        if (wantedPlatform == Platform.Auto)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                wantedPlatform = Platform.Win32;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                wantedPlatform = Platform.Wayland;
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported platform.");
            }
        }

        // Set appropriate platform hint for GLFW
        switch (wantedPlatform)
        {
            case Platform.Win32:
                ThrowInvalidOperationIf(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Win32 GUI platform is only supported on Windows.");
                platform = Platform.Win32;
                glfwInitHint(Constants.GLFW_PLATFORM, Constants.GLFW_PLATFORM_WIN32);
                break;
            case Platform.Wayland:
                ThrowInvalidOperationIf(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "Wayland GUI platform is only supported on Linux.");
                platform = Platform.Wayland;
                glfwInitHint(Constants.GLFW_PLATFORM, Constants.GLFW_PLATFORM_WAYLAND);
                break;
            case Platform.X11:
                ThrowInvalidOperationIf(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux), "X11 GUI platform is only supported on Linux.");
                platform = Platform.X11;
                glfwInitHint(Constants.GLFW_PLATFORM, Constants.GLFW_PLATFORM_X11);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(parameters.Platform));
        }

        ThrowIf(glfwInit() != 1, "Failed to initialize GLFW.");

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

        // Wayland does not allow application to move its own window
        isWindowPositionable = platform != Platform.Wayland;
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

    internal unsafe GfxSwapChainSurface CreateSurface(External.Vulkan.VkInstance instance)
    {
        Assert(isCreated);
        Assert(glfwWindow.HasValue);

        External.Vulkan.VkSurfaceKhr surface = default;
        External.Vulkan.Api.ThrowVulkanIfFailed(glfwCreateWindowSurface(instance, glfwWindow, null, &surface));

        glfwGetWindowSize(glfwWindow, out int ww, out int wh);

        return new(surface, new((uint)ww, (uint)wh));
    }

    public void Create()
    {
        Assert(!isCreated);

        // Disable OpenGL
        glfwWindowHint(Constants.GLFW_CLIENT_API, 0);
        // Disable default window frame
        glfwWindowHint(Constants.GLFW_DECORATED, 0);
        // Disable automatic iconification when focus gets lost in fullscreen mode
        glfwWindowHint(Constants.GLFW_AUTO_ICONIFY, 0);

        // Retrieve monitor info for the monitor we are going to be starting in
        GlfwMonitor monitor = glfwGetMonitor(MathUtilities.Clamp(parameters.StartupMonitor, 0, glfwGetMonitorCount() - 1));

        glfwGetMonitorWorkarea(monitor, out int mx, out int my, out int mw, out int mh);
        glfwGetMonitorPhysicalSize(monitor, out int mpw, out int mph);
        GlfwVideoMode monitorVideoMode = glfwGetVideoMode(monitor);

        Vector2 windowSize = parameters.Size ?? new Vector2(Math.Min(2560, (int)(monitorVideoMode.Width * 0.75f)), Math.Min(1440, (int)(monitorVideoMode.Height * 0.75f)));
        Vector2 windowPosition = parameters.Position ?? new Vector2(mx + (int)((mw - windowSize.X) / 2.0f), my + (int)((mh - windowSize.Y) / 2.0f));

        glfwWindow = glfwCreateWindow((int)windowSize.X, (int)windowSize.Y, "Gossamer");
        ThrowIf(!glfwWindow.HasValue, "Failed to create GLFW window.");

        glfwSetWindowSizeLimits(glfwWindow, 256, 144, -1, -1);
        if (isWindowPositionable)
        {
            glfwSetWindowPos(glfwWindow, (int)windowPosition.X, (int)windowPosition.Y);
        }

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

    void UpdateParameters()
    {
        isIconified = glfwGetWindowAttrib(glfwWindow, Constants.GLFW_ICONIFIED) != 0;
        isMaximized = glfwGetWindowAttrib(glfwWindow, Constants.GLFW_MAXIMIZED) != 0;

        glfwGetWindowPos(glfwWindow, out int wx, out int wy);
        glfwGetWindowSize(glfwWindow, out int ww, out int wh);
        parameters.Position = new Vector2(wx, wy);
        parameters.Size = new Vector2(ww, wh);
    }

    bool layoutRequested = true;

    bool mouseOnClose;
    bool mouseOnMaximize;
    bool mouseOnMinimize;
    bool mouseOnFrameControls;

    readonly bool isWindowPositionable = true;
    bool isFrameDragging;
    Vector2 frameDraggingStartPosition;

    Rectangle controlsCloseRect;
    Rectangle controlsCloseIconRect;
    Rectangle controlsMaximizeRect;
    Rectangle controlsMaximizeIconRect;
    Rectangle controlsMinimizeRect;
    Rectangle controlsMinimizeIconRect;

    FrameMode mode = FrameMode.Full;

    public enum FrameMode
    {
        None = 0,
        Title = 1,
        Full = 2,
    }

    public void Render()
    {
        Assert(isCreated);

        if (layoutRequested)
        {
            layoutRequested = false;
            //Layout();
        }

        var gfx2D = gfx.Get2D();

        var gfxStats = gfx.GetStatistics();
        var gfx2DStats = gfx2D.GetStatistics();

        var cmdBuffer = gfx2D.BeginCommandBuffer();
        {
            cmdBuffer.BeginBatch();

            UpdateParameters();
            glfwGetWindowSize(glfwWindow, out int ww, out int wh);

            Vector4 sizeOfFrame = parameters.SizeOfFrame;

            controlsCloseRect = new Rectangle(ww - ControlButtonWidth - ControlsOffFromFrameSide, 0.0f, ww - ControlsOffFromFrameSide, sizeOfFrame.Y);
            controlsCloseIconRect = new Rectangle(0, 0, 8, 8).CenterOn(controlsCloseRect.Center);
            controlsMaximizeRect = new Rectangle(controlsCloseRect.Left - ControlsButtonSeparation - ControlButtonWidth, 0.0f, controlsCloseRect.Left - ControlsButtonSeparation, sizeOfFrame.Y);
            controlsMaximizeIconRect = new Rectangle(0, 0, 8, 8).CenterOn(controlsMaximizeRect.Center);
            controlsMinimizeRect = new Rectangle(controlsMaximizeRect.Left - ControlsButtonSeparation - ControlButtonWidth, 0.0f, controlsMaximizeRect.Left - ControlsButtonSeparation, sizeOfFrame.Y);
            controlsMinimizeIconRect = new Rectangle(0, 0, 8, 8).CenterOn(controlsMinimizeRect.Center);

            cmdBuffer.FillRectangle(new(0, 0), new(ww, wh), parameters.ColorOfBackground);

            Color colorOfFrame = parameters.ColorOfFrame;

            if (mode != FrameMode.None)
            {
                if (mode == FrameMode.Full)
                {
                    cmdBuffer.FillRectangle(new Vector2(0, 0), new Vector2(sizeOfFrame.X, wh), colorOfFrame);
                    cmdBuffer.FillRectangle(new Vector2(ww - sizeOfFrame.Z, 0), new Vector2(ww, wh), colorOfFrame);
                    cmdBuffer.FillRectangle(new Vector2(0, wh - sizeOfFrame.W), new Vector2(ww, wh), colorOfFrame);
                }

                cmdBuffer.FillRectangle(new Vector2(0, 0), new Vector2(ww, sizeOfFrame.Y), colorOfFrame);

                cmdBuffer.FillRectangle(controlsCloseRect, mouseOnClose ? new Color(Color.SizzlingRed, 0.75f) : colorOfFrame);
                cmdBuffer.FillRectangle(controlsMaximizeRect, mouseOnMaximize ? new Color(Color.White, 0.5f) : colorOfFrame);
                cmdBuffer.FillRectangle(controlsMinimizeRect, mouseOnMinimize ? new Color(Color.White, 0.5f) : colorOfFrame);

                cmdBuffer.FillRectangle(controlsCloseIconRect, Color.White);
                cmdBuffer.FillRectangle(controlsMaximizeIconRect, Color.White);
                cmdBuffer.FillRectangle(controlsMinimizeIconRect, Color.White);

                //cmdBuffer.DrawText("Gossamer", new Vector2(10, 5), Color.White, Color.UnpackRGB(0x1f1e25), gfx2D.GetBuiltInFont());
            }

            var font = gfx2D.GetBuiltInFont();
            var statsText = $"GC: {StringUtilities.TimeShort(GC.GetTotalPauseDuration())}\nCPU: {StringUtilities.TimeShort(gfxStats.CpuFrameTime)}\nGPU: {StringUtilities.TimeShort(gfxStats.GpuFrameTime)}\n2D Draws: {gfx2DStats.DrawCalls} ({gfx2DStats.Vertices}v {gfx2DStats.Indices}i)";
            cmdBuffer.DrawText(statsText, new(5, sizeOfFrame.Y), Color.White, parameters.ColorOfBackground, font);

            Vector2 textAvailableSize = new(ww, wh);

            {
                var textToTest =
"""
Gossamer::Run OS: Ubuntu 24.10 (X64)
Gossamer::Run Runtime: .NET 9.0.2 (linux-x64)
Gossamer::Run Working directory: /home/jant/projects/Gossamer/Gossamer.Benchmark/bin/x64/Debug/net9.0
Gossamer::Run Frontend thread = 1
Gossamer::Run Backend thread = 4
libdecor-gtk-WARNING: Failed to initialize GTK
Failed to load plugin 'libdecor-gtk.so': failed to init
No plugins found, falling back on no decorations
Gfx::EnumeratePhysicalDevices Available physical device: GfxPhysicalDevice { Type = Discrete, Id = 222e6fd6-92e8-664c-7e98-55367237f806, Name = NVIDIA GeForce RTX 4060 Ti, Driver = 570.344.1024, Api = 1.4.303 }
Gfx::EnumeratePhysicalDevices Available physical device: GfxPhysicalDevice { Type = Discrete, Id = 222e6fd6-92e8-664c-7e98-55367237f806, Name = NVIDIA GeForce RTX 4060 Ti, Driver = 570.344.1024, Api = 1.4.303 }
Gfx::EnumeratePhysicalDevices Available physical device: GfxPhysicalDevice { Type = Cpu, Id = 322e3432-382e-312d-7562-756e7475317e, Name = llvmpipe (LLVM 19.1.1, 256 bits), Driver = 0.0.1, Api = 1.3.289 }
Gfx::CreateVulkanDevice Selected physical device: GfxPhysicalDevice { Type = Discrete, Id = 222e6fd6-92e8-664c-7e98-55367237f806, Name = NVIDIA GeForce RTX 4060 Ti, Driver = 570.344.1024, Api = 1.4.303 }
Gui::CreateSurface GLFW error: [1000C] Wayland: The platform does not support setting the window opacity
Gui::CreateSurface Required instance extensions: 2
Gui::CreateSurface Required instance extension: VK_KHR_surface
Gui::CreateSurface Required instance extension: VK_KHR_wayland_surface
Vma::Allocate 7.69 MiB
Gfx::CreateDynamicMemoryBuffer Vertex, [2.00 MiB] [DEVICE_LOCAL, HOST_VISIBLE, HOST_COHERENT]
Gfx::CreateDynamicMemoryBuffer Index, [128.00 KiB] [DEVICE_LOCAL, HOST_VISIBLE, HOST_COHERENT]
Gfx::CreateDynamicMemoryBuffer Uniform, [64 B] [DEVICE_LOCAL, HOST_VISIBLE, HOST_COHERENT]
Vma::Allocate 32.00 MiB
Gfx::CreatePixelBuffer Rgba8 512x512 [1.00 MiB] [DEVICE_LOCAL]
Vma::Allocate 32.00 MiB
Gfx::CreateDynamicMemoryBuffer TransferSrc, [1.00 MiB] [HOST_VISIBLE, HOST_COHERENT]
Vma::Allocate 15.00 MiB
Gfx::CreatePixelBuffer Bgra8 2560x1440 [15.00 MiB] [DEVICE_LOCAL]
""";
                //textToTest = "Word1 woORd2 longerword3 andword4 maybeevenlongerword5 word6";
                var textLayout = gfx2D.ComputeTextLayout(
                    textToTest,
                    gfx2D.GetFontOrBuiltIn("CascadiaCode", 16),
                    textAvailableSize,
                    wordWrap: true);

                Rectangle layoutRect = new(0, 0, textLayout.Size.X, textLayout.Size.Y);
                Rectangle windowRect = new(0, 0, ww, wh);
                Rectangle layoutCentered = layoutRect.CenterOn(windowRect.Center);
                cmdBuffer.DrawText(textLayout, Vector2.Round(layoutCentered.Position), Color.UnpackRGB(0xbde5fb).WithAlpha(0.95f), parameters.ColorOfBackground);
                cmdBuffer.DrawRectangle(layoutCentered.Position, layoutCentered.Position + textLayout.Size, Color.MintyGreen);
                //cmdBuffer.DrawRectangle(windowRect.Position, windowRect.Position + textAvailableSize, Color.HighlighterRed);

                gfx2D.ReleaseTextLayout(textLayout);
            }

            cmdBuffer.EndBatch();
        }
        gfx2D.EndCommandBuffer(cmdBuffer);
    }

    public void PostEmptyEvent()
    {
        glfwPostEmptyEvent();
    }

    public void WaitForEvents()
    {
        glfwWaitEvents();
    }

    public void WaitForEvents(double timeout)
    {
        glfwWaitEventsTimeout(timeout);
    }

    void Callback_WindowRefresh(GlfwWindow window)
    {
        messageQueue.PostSurfaceDamaged();
    }

    void Callback_WindowSize(GlfwWindow window, int w, int h)
    {
        messageQueue.PostSurfaceLost();

        if (platform != Platform.Win32)
        {
            gfx.GetPresenter().Invalidate((uint)w, (uint)h);
        }
    }

    void Callback_WindowIconify(GlfwWindow window, int iconified)
    {
    }

    void Callback_WindowClose(GlfwWindow window)
    {
        messageQueue.PostQuit();
    }

    void Callback_MouseEnter(GlfwWindow window, int contained)
    {
    }

    void Callback_MouseMove(GlfwWindow window, double x, double y)
    {
        messageQueue.PostMouseXY((int)(x * 1000), (int)(y * 1000));

        Vector4 sizeOfFrame = parameters.SizeOfFrame;

        Vector2 mouseOnWindow = new((int)x, (int)y);

        if (isWindowPositionable && isFrameDragging)
        {
            glfwGetWindowPos(glfwWindow, out int wx, out int wy);
            Vector2 windowPosition = new(wx, wy);

            windowPosition += mouseOnWindow - frameDraggingStartPosition;
            glfwSetWindowPos(glfwWindow, (int)windowPosition.X, (int)windowPosition.Y);
        }

        if (mode != FrameMode.None && mouseOnWindow.Y <= sizeOfFrame.Y)
        {
            mouseOnClose = controlsCloseRect.Contains(mouseOnWindow);
            mouseOnMaximize = controlsMaximizeRect.Contains(mouseOnWindow);
            mouseOnMinimize = controlsMinimizeRect.Contains(mouseOnWindow);
            mouseOnFrameControls = mouseOnClose || mouseOnMaximize || mouseOnMinimize;
        }
        else
        {
            mouseOnClose = mouseOnMaximize = mouseOnMinimize = mouseOnFrameControls = false;
        }

        if (mouseOnFrameControls)
        {
            return;
        }
    }

    bool IsPositionOnFrame(Vector2 position)
    {
        Vector4 sizeOfFrame = parameters.SizeOfFrame;

        glfwGetWindowSize(glfwWindow, out int ww, out int wh);

        return mode == FrameMode.Full && (position.X <= sizeOfFrame.X || position.X >= (ww - sizeOfFrame.Z) || position.Y <= sizeOfFrame.Y || position.Y >= (wh - sizeOfFrame.W));
    }

    void Callback_MouseButton(GlfwWindow window, int button, int action, int mods)
    {
        InputButton iButton = GetInputButton(button);
        InputAction iAction = GetInputAction(action);
        InputMods iMods = GetInputMods(mods);

        messageQueue.PostMouseButton(iButton, iAction, iMods);

        if (iButton == InputButton.Left)
        {
            if (iAction == InputAction.Press)
            {
                if (isWindowPositionable)
                {
                    glfwGetCursorPos(glfwWindow, out double x, out double y);
                    frameDraggingStartPosition = new Vector2((int)x, (int)y);
                    isFrameDragging = IsPositionOnFrame(frameDraggingStartPosition);
                }
            }
            else if (iAction == InputAction.Release)
            {
                isFrameDragging = false;

                if (mouseOnClose)
                {
                    glfwSetWindowShouldClose(window, true);
                }
                else if (mouseOnMaximize)
                {
                    CommitMaximize();
                }
                else if (mouseOnMinimize)
                {
                    glfwIconifyWindow(glfwWindow);
                }
            }
        }

    }

    void Callback_MouseScroll(GlfwWindow window, double x, double y)
    {
        messageQueue.PostMouseWheel((int)(x * 1000), (int)(y * 1000));
    }

    void Callback_KeyboardKey(GlfwWindow window, int key, int code, int action, int mods)
    {
        messageQueue.PostKeyboardKey(GetInputKey(key), code, GetInputAction(action), GetInputMods(mods));
    }

    void Callback_KeyboardChar(GlfwWindow window, uint c)
    {
        messageQueue.PostKeyboardChar((int)c, 0);
    }

    int normalWindowX = 0;
    int normalWindowY = 0;
    int normalWindowW = 0;
    int normalWindowH = 0;

    void CommitMaximize()
    {
        if (!useFullscreen && !isMaximized || useFullscreen && !isFullscreen)
        {
            mode = FrameMode.Title;

            if (useFullscreen)
            {
                isFullscreen = true;

                glfwGetWindowPos(glfwWindow, out normalWindowX, out normalWindowY);
                glfwGetWindowSize(glfwWindow, out normalWindowW, out normalWindowH);

                GlfwMonitor primaryMonitor = glfwGetPrimaryMonitor();
                GlfwVideoMode videoMode = glfwGetVideoMode(primaryMonitor);
                glfwSetWindowMonitor(glfwWindow, primaryMonitor, 0, 0, (int)videoMode.Width, (int)videoMode.Height, (int)videoMode.RefreshRate);
            }
            else
            {
                glfwMaximizeWindow(glfwWindow);
            }
        }
        else
        {
            mode = FrameMode.Full;

            if (useFullscreen)
            {
                isFullscreen = false;
                glfwSetWindowMonitor(glfwWindow, default, normalWindowX, normalWindowY, normalWindowW, normalWindowH, default);

                // Window size callback is not called on Wayland when restoring from fullscreen - call it manually
                if (platform == Platform.Wayland)
                {
                    Callback_WindowSize(glfwWindow, normalWindowW, normalWindowH);
                }
            }
            else
            {
                glfwRestoreWindow(glfwWindow);
            }
        }

        ScheduleLayout();
    }

    void ScheduleLayout()
    {
        layoutRequested = true;
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

    static InputMods GetInputMods(int mods)
    {
        InputMods result = InputMods.None;

        if ((mods & Constants.GLFW_MOD_SHIFT) != 0)
        {
            result |= InputMods.Shift;
        }

        if ((mods & Constants.GLFW_MOD_CONTROL) != 0)
        {
            result |= InputMods.Control;
        }

        if ((mods & Constants.GLFW_MOD_ALT) != 0)
        {
            result |= InputMods.Alt;
        }

        if ((mods & Constants.GLFW_MOD_SUPER) != 0)
        {
            result |= InputMods.Super;
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