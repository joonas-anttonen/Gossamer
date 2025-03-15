/*
    Simple types for the graphics backend. Collected in one file for convenience.
*/

using Gossamer.External.Vulkan;
using Gossamer.External.Vulkan.Vma;
using Gossamer.Utilities;

namespace Gossamer.Backend;

public record GfxPresentation();
public record GfxSwapChainPresentation(Frontend.Gui Gui, bool EnableVerticalSync) : GfxPresentation();
public record GfxDirectXPresentation(nint Handle, GfxFormat Format, uint Width, uint Height) : GfxPresentation();

internal record GfxSwapChainSurface(VkSurfaceKhr Surface, VkExtent2D Extent);

public enum GfxPresentationMode
{
    SwapChain,
    DirectX,
    Headless,
}

public record GfxApiParameters(
    Gossamer.ApplicationInfo AppInfo,
    bool EnableDebugging,
    GfxPresentationMode PresentationMode
);

public record GfxParameters(
    GfxPhysicalDevice PhysicalDevice
);

public record GfxCapabilities(
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
public record GfxPhysicalDevice(
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
    public static readonly DisplayParameters Empty = new(0, 0, 0, 0, 0, 0, 0, AntialiasingMode.None, Color.Palettes.Nord.Nord0_Darkest);

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

readonly record struct GfxSingleCommand(VkCommandBuffer CommandBuffer, VkFence Fence);

record GfxPipeline(VkPipeline Pipeline, VkPipelineLayout Layout, VkDescriptorSetLayout DescriptorLayout);

record GfxPipelineShader(string Name, GfxPipelineShader.Stage[] Stages)
{
    public record Stage(VkShaderStage StageType, SafeNativeString Entrypoint, byte[] Code);
}

record GfxPipelineParameters(
    GfxPipelineShader ShaderProgram,
    VkPushConstantRange[] PushConstants,
    VkDescriptorSetLayoutBinding[] Layout,
    VkPrimitiveTopology InputTopology,
    VkCullMode CullMode,
    VkFrontFace FrontFace,
    VkVertexInputBindingDescription[] InputBindings,
    VkVertexInputAttributeDescription[] InputAttributes,
    GfxPipelineAttachment[] Attachments,
    bool DepthTest,
    bool DepthWrite,
    VkCompareOp DepthCompareOp,
    bool Multisampling
);

record struct GfxPipelineAttachment(VkFormat Format, VkPipelineColorBlendAttachmentState Blend);

public class MemoryBuffer<T>
{
    /// <summary>
    /// The length of the buffer in T's.
    /// </summary>
    public uint Length { get; }

    internal VkBuffer Buffer { get; }
    internal VmaAllocation Allocation { get; }

    internal MemoryBuffer(
        uint length,
        VkBuffer buffer,
        VmaAllocation allocation)
    {
        Length = length;
        Buffer = buffer;
        Allocation = allocation;
    }
}

public class PixelBuffer
{
    public GfxFormat Format { get; }
    public GfxAspect Aspect { get; }
    public GfxSamples Samples { get; }
    public uint Width { get; }
    public uint Height { get; }

    internal VkImage Image { get; }
    internal VkImageView View { get; }
    internal VmaAllocation Allocation { get; }

    internal PixelBuffer(
        GfxFormat format,
        GfxAspect aspect,
        GfxSamples samples,
        uint width,
        uint height,
        VkImage image,
        VkImageView view,
        VmaAllocation allocation)
    {
        Format = format;
        Aspect = aspect;
        Image = image;
        View = view;
        Allocation = allocation;
        Width = width;
        Height = height;
        Samples = samples;
    }
}