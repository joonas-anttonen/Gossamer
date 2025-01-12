/*
    Enums for the graphics backend. Collected in one file for convenience.
*/

namespace Gossamer.Backend;

public enum GfxFormat
{
    Undefined = (int)External.Vulkan.VkFormat.UNDEFINED,
    R32 = (int)External.Vulkan.VkFormat.R32_SFLOAT,
    Rg32 = (int)External.Vulkan.VkFormat.R32G32_SFLOAT,
    Rgb32 = (int)External.Vulkan.VkFormat.R32G32B32_SFLOAT,
    Rgba32 = (int)External.Vulkan.VkFormat.R32G32B32A32_SFLOAT,
    Rgba8 = (int)External.Vulkan.VkFormat.R8G8B8A8_UNORM,
    Bgra8 = (int)External.Vulkan.VkFormat.B8G8R8A8_UNORM,
    D32 = (int)External.Vulkan.VkFormat.D32_SFLOAT,
}

[Flags]
public enum GfxMemoryBufferUsage
{
    None = 0,
    TransferSrc = (int)External.Vulkan.VkBufferUsage.TRANSFER_SRC_BIT,
    TransferDst = (int)External.Vulkan.VkBufferUsage.TRANSFER_DST_BIT,
    UniformTexel = (int)External.Vulkan.VkBufferUsage.UNIFORM_TEXEL_BUFFER_BIT,
    StorageTexel = (int)External.Vulkan.VkBufferUsage.STORAGE_TEXEL_BUFFER_BIT,
    Uniform = (int)External.Vulkan.VkBufferUsage.UNIFORM_BUFFER_BIT,
    Storage = (int)External.Vulkan.VkBufferUsage.STORAGE_BUFFER_BIT,
    Index = (int)External.Vulkan.VkBufferUsage.INDEX_BUFFER_BIT,
    Vertex = (int)External.Vulkan.VkBufferUsage.VERTEX_BUFFER_BIT,
    Indirect = (int)External.Vulkan.VkBufferUsage.INDIRECT_BUFFER_BIT,
}

[Flags]
public enum GfxPixelBufferUsage
{
    None = 0,
    TransferSrc = (int)External.Vulkan.VkImageUsage.TRANSFER_SRC_BIT,
    TransferDst = (int)External.Vulkan.VkImageUsage.TRANSFER_DST_BIT,
    Sampled = (int)External.Vulkan.VkImageUsage.SAMPLED_BIT,
    Storage = (int)External.Vulkan.VkImageUsage.STORAGE_BIT,
    ColorAttachment = (int)External.Vulkan.VkImageUsage.COLOR_ATTACHMENT_BIT,
    DepthStencilAttachment = (int)External.Vulkan.VkImageUsage.DEPTH_STENCIL_ATTACHMENT_BIT,
    InputAttachment = (int)External.Vulkan.VkImageUsage.INPUT_ATTACHMENT_BIT,
}

[Flags]
public enum GfxAspect
{
    Color = (int)External.Vulkan.VkImageAspect.COLOR,
    Depth = (int)External.Vulkan.VkImageAspect.DEPTH,
    Stencil = (int)External.Vulkan.VkImageAspect.STENCIL,
    DepthStencil = (int)External.Vulkan.VkImageAspect.DEPTH | (int)External.Vulkan.VkImageAspect.STENCIL,
}

public enum GfxSamples
{
    X1 = (int)External.Vulkan.VkSampleCount.COUNT_1,
    X2 = (int)External.Vulkan.VkSampleCount.COUNT_2,
    X4 = (int)External.Vulkan.VkSampleCount.COUNT_4,
    X8 = (int)External.Vulkan.VkSampleCount.COUNT_8,
    X16 = (int)External.Vulkan.VkSampleCount.COUNT_16,
    X32 = (int)External.Vulkan.VkSampleCount.COUNT_32,
    X64 = (int)External.Vulkan.VkSampleCount.COUNT_64,
}

public enum GfxPhysicalDeviceType
{
    Discrete,
    Integrated,
    Virtual,
    Cpu,
    Other
}