/*
    Enums for the graphics backend. Collected in one file for convenience.
*/

namespace Gossamer.Gfx;

public enum GfxFormat
{
    UNDEFINED = (int)External.Vulkan.VkFormat.UNDEFINED,
    R32 = (int)External.Vulkan.VkFormat.R32_SFLOAT,
    RG32 = (int)External.Vulkan.VkFormat.R32G32_SFLOAT,
    RGB32 = (int)External.Vulkan.VkFormat.R32G32B32_SFLOAT,
    RGBA32 = (int)External.Vulkan.VkFormat.R32G32B32A32_SFLOAT,
    RGBA8 = (int)External.Vulkan.VkFormat.R8G8B8A8_UNORM,
    BGRA8 = (int)External.Vulkan.VkFormat.B8G8R8A8_UNORM,
    D32 = (int)External.Vulkan.VkFormat.D32_SFLOAT,
}

[Flags]
public enum GfxMemoryUsage
{
    NONE = 0,
    TRANSFER_SRC = (int)External.Vulkan.VkBufferUsage.TRANSFER_SRC_BIT,
    TRANSFER_DST = (int)External.Vulkan.VkBufferUsage.TRANSFER_DST_BIT,
    UniformTexel = (int)External.Vulkan.VkBufferUsage.UNIFORM_TEXEL_BUFFER_BIT,
    StorageTexel = (int)External.Vulkan.VkBufferUsage.STORAGE_TEXEL_BUFFER_BIT,
    UNIFORM = (int)External.Vulkan.VkBufferUsage.UNIFORM_BUFFER_BIT,
    STORAGE = (int)External.Vulkan.VkBufferUsage.STORAGE_BUFFER_BIT,
    INDEX = (int)External.Vulkan.VkBufferUsage.INDEX_BUFFER_BIT,
    VERTEX = (int)External.Vulkan.VkBufferUsage.VERTEX_BUFFER_BIT,
    Indirect = (int)External.Vulkan.VkBufferUsage.INDIRECT_BUFFER_BIT,
}

[Flags]
public enum GfxMemoryAccess
{
    None = 0,
    Write = 1,
    Read = 2,
    ReadWrite = Write | Read,
}

[Flags]
public enum GfxPixelBufferUsage
{
    NONE = 0,
    TRANSFER_SRC = (int)External.Vulkan.VkImageUsage.TRANSFER_SRC_BIT,
    TRANSFER_DST = (int)External.Vulkan.VkImageUsage.TRANSFER_DST_BIT,
    SAMPLED = (int)External.Vulkan.VkImageUsage.SAMPLED_BIT,
    STORAGE = (int)External.Vulkan.VkImageUsage.STORAGE_BIT,
    COLOR_ATTACHMENT = (int)External.Vulkan.VkImageUsage.COLOR_ATTACHMENT_BIT,
    DEPTH_ATTACHMENT = (int)External.Vulkan.VkImageUsage.DEPTH_STENCIL_ATTACHMENT_BIT,
    INPUT_ATTACHMENT = (int)External.Vulkan.VkImageUsage.INPUT_ATTACHMENT_BIT,
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