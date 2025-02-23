/*
    Simple types for the graphics backend. Collected in one file for convenience.
*/

using Gossamer.External.Vulkan;

namespace Gossamer.Backend;

public record class GfxPresentation(Color ClearColor);
public record class GfxSwapChainPresentation(Color ClearColor, Frontend.Gui Gui) : GfxPresentation(ClearColor);
public record class GfxDirectXPresentation(Color ClearColor, nint Handle, GfxFormat Format, uint Width, uint Height) : GfxPresentation(ClearColor);

internal record class GfxSwapChainSurface(VkSurfaceKhr Surface, VkExtent2D Extent);

public enum GfxPresentationMode
{
    SwapChain,
    DirectX,
    Headless,
}

public record class GfxApiParameters(
    Gossamer.ApplicationInfo AppInfo,
    bool EnableDebugging,
    GfxPresentationMode PresentationMode
);

public record class GfxParameters(
    GfxPhysicalDevice PhysicalDevice,
    GfxPresentation Presentation
);

public record class GfxCapabilities(
    bool CanDebug,
    bool CanSwap,
    bool CanTimestamp
);

/// <summary>
/// Represents a physical device.
/// </summary>
/// <param name="Type">Type of the physical device.</param>
/// <param name="Id">Unique identifier of the physical device.</param>
/// <param name="Name">Name of the physical device.</param>
/// <param name="Driver">Vulkan driver version.</param>
/// <param name="Api">Vulkan api version.</param>
public record class GfxPhysicalDevice(
    GfxPhysicalDeviceType Type,
    Guid Id,
    string Name,
    Version Driver,
    Version Api
);

public enum AntialiasingMode
{
    None,
    Fsr,
}

public enum UpscalingMode
{
    None,
    Quality,
    Balanced,
    Performance,
    UltraPerformance,
}

public record DisplayParameters(
    uint RenderWidth,
    uint RenderHeight,
    uint DisplayWidth,
    uint DisplayHeight,
    uint DisplayRefreshRate,
    uint ViewportWidth,
    uint ViewportHeight,
    AntialiasingMode AntialiasingMode,
    Color ClearColor)
{
    public static readonly DisplayParameters Empty = new(0, 0, 0, 0, 0, 0, 0, AntialiasingMode.None, Color.BlackPearl);

    public static float GetRenderScaleFactor(AntialiasingMode antialiasingMode, UpscalingMode upscaleQuality)
    {
        return antialiasingMode switch
        {
            AntialiasingMode.Fsr => upscaleQuality switch
            {
                UpscalingMode.Quality => 1.0f / 1.5f,
                UpscalingMode.Balanced => 1.0f / 1.7f,
                UpscalingMode.Performance => 1.0f / 2.0f,
                UpscalingMode.UltraPerformance => 1.0f / 3.0f,
                _ => 1.0f,
            },
            _ => 1.0f,
        };
    }
}