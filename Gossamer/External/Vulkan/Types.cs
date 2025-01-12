#pragma warning disable CS0649

using System.Runtime.InteropServices;

namespace Gossamer.External.Vulkan;

public class VulkanException(string message) : Exception(message)
{
}

readonly struct VkInstance { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkPhysicalDevice { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDevice { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkQueue { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkFence { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkSemaphore { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkSwapChainKhr { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkImageView { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkCommandPool { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkFrameBuffer { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDescriptorPool { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkSampler { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkCommandBuffer { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkPipeline { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkPipelineLayout { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkRenderPass { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDescriptorSet { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDescriptorSetLayout { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDeviceMemory { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkBuffer { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkImage { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkSurfaceKhr { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkPipelineCache { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkShaderModule { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

struct VkPhysicalDevicePushDescriptorPropertiesKHR(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_PUSH_DESCRIPTOR_PROPERTIES_KHR;
    public nint Next = next;

    public uint MaxPushDescriptors;
}

struct VkDescriptorImageInfo
{
    public VkSampler Sampler;
    public VkImageView ImageView;
    public VkImageLayout ImageLayout;
}

struct VkDescriptorBufferInfo
{
    public VkBuffer Buffer;
    public ulong Offset;
    public ulong Range;
}

unsafe struct VkWriteDescriptorSet(nint next)
{
    public VkStructureType SType = VkStructureType.WRITE_DESCRIPTOR_SET;
    public nint Next = next;

    public VkDescriptorSet DestinationSet;
    public uint DestinationBinding;
    public uint DstArrayElement;
    public uint DescriptorCount;
    public VkDescriptorType DescriptorType;
    public VkDescriptorImageInfo* ImageInfo;
    public VkDescriptorBufferInfo* BufferInfo;
    public nint TexelBufferView;
}

unsafe delegate void PFN_vkCmdPushDescriptorSetKHR(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint set, uint descriptorWriteCount, VkWriteDescriptorSet* pDescriptorWrites);

#region VK_EXT_debug_utils

readonly struct VkDebugUtilsMessengerExt { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDebugReportCallbackEXT { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

unsafe delegate uint PFN_vkDebugUtilsMessengerCallbackEXT(
    VkDebugUtilsMessageSeverityExt messageSeverity,
    VkDebugUtilsMessageTypeExt messageTypes,
    VkDebugUtilsMessengerCallbackDataExt* pCallbackData,
    nint pUserData);
unsafe delegate VkResult PFN_vkCreateDebugUtilsMessengerEXT(
    VkInstance instance,
    VkDebugUtilsMessengerCreateInfoExt* pCreateInfo,
    nint pAllocator,
    VkDebugUtilsMessengerExt* pMessenger);
unsafe delegate void PFN_vkDestroyDebugUtilsMessengerEXT(
    VkInstance instance,
    VkDebugUtilsMessengerExt messenger,
    nint pAllocator);
unsafe delegate void PFN_vkSetDebugUtilsObjectNameEXT(
    VkDevice device,
    DebugUtilsObjectNameInfoEXT* pNameInfo);

unsafe struct DebugUtilsObjectNameInfoEXT(nint next)
{
    public const uint DEBUG_UTILS_OBJECT_NAME_INFO_EXT = 1000128000;

    public VkStructureType SType = (VkStructureType)DEBUG_UTILS_OBJECT_NAME_INFO_EXT;
    public nint Next = next;
    public VkObjectType ObjectType;
    public ulong ObjectHandle;
    public nint ObjectName;
}

unsafe struct VkDebugUtilsMessengerCreateInfoExt(nint next)
{
    public const uint VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT = 1000128004;

    public VkStructureType SType = (VkStructureType)VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT;
    public nint Next = next;
    public uint Flags;
    public VkDebugUtilsMessageSeverityExt MessageSeverity;
    public VkDebugUtilsMessageTypeExt MessageType;
    public nint UserCallback;
    public nint pUserData;
}

unsafe struct VkDebugUtilsMessengerCallbackDataExt(nint next = default)
{
    public const uint VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CALLBACK_DATA_EXT = 1000128003;

    public VkStructureType SType = (VkStructureType)VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CALLBACK_DATA_EXT;
    public nint Next = next;
    public uint flags;
    public nint pMessageIdName;
    public int messageIdNumber;
    public nint pMessage;
    public uint queueLabelCount;
    public nint pQueueLabels;
    public uint cmdBufLabelCount;
    public nint pCmdBufLabels;
    public uint objectCount;
    public nint pObjects;
}

#endregion

unsafe struct VkAllocationCallbacks
{
    public nint UserData;
    public nint Allocation;
    public nint Reallocation;
    public nint Free;
    public nint InternalAllocation;
    public nint InternalFree;
}

unsafe struct VkInstanceCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.INSTANCE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public VkApplicationInfo* ApplicationInfo;
    public uint EnabledLayerCount;
    public nint EnabledLayerNames;
    public uint EnabledExtensionCount;
    public nint EnabledExtensionNames;
}

unsafe struct VkApplicationInfo(nint next)
{
    public VkStructureType SType = VkStructureType.APPLICATION_INFO;
    public nint Next = next;
    public nint ApplicationName;
    public uint ApplicationVersion;
    public nint EngineName;
    public uint EngineVersion;
    public uint ApiVersion;
}

unsafe struct VkExtensionProperties
{
    public fixed byte ExtensionName[256];
    public uint SpecVersion;
}

unsafe struct VkLayerProperties
{
    public fixed byte LayerName[256];
    public uint SpecVersion;
    public uint ImplementationVersion;
    public fixed byte Description[256];
}

struct VkPhysicalDeviceLimits
{
    public uint MaxImageDimension1D;
    public uint MaxImageDimension2D;
    public uint MaxImageDimension3D;
    public uint MaxImageDimensionCube;
    public uint MaxImageArrayLayers;
    public uint MaxTexelBufferElements;
    public uint MaxUniformBufferRange;
    public uint MaxStorageBufferRange;
    public uint MaxPushConstantsSize;
    public uint MaxMemoryAllocationCount;
    public uint MaxSamplerAllocationCount;
    public ulong BufferImageGranularity;
    public ulong SparseAddressSpaceSize;
    public uint MaxBoundDescriptorSets;
    public uint MaxPerStageDescriptorSamplers;
    public uint MaxPerStageDescriptorUniformBuffers;
    public uint MaxPerStageDescriptorStorageBuffers;
    public uint MaxPerStageDescriptorSampledImages;
    public uint MaxPerStageDescriptorStorageImages;
    public uint MaxPerStageDescriptorInputAttachments;
    public uint MaxPerStageResources;
    public uint MaxDescriptorSetSamplers;
    public uint MaxDescriptorSetUniformBuffers;
    public uint MaxDescriptorSetUniformBuffersDynamic;
    public uint MaxDescriptorSetStorageBuffers;
    public uint MaxDescriptorSetStorageBuffersDynamic;
    public uint MaxDescriptorSetSampledImages;
    public uint MaxDescriptorSetStorageImages;
    public uint MaxDescriptorSetInputAttachments;
    public uint MaxVertexInputAttributes;
    public uint MaxVertexInputBindings;
    public uint MaxVertexInputAttributeOffset;
    public uint MaxVertexInputBindingStride;
    public uint MaxVertexOutputComponents;
    public uint MaxTessellationGenerationLevel;
    public uint MaxTessellationPatchSize;
    public uint MaxTessellationControlPerVertexInputComponents;
    public uint MaxTessellationControlPerVertexOutputComponents;
    public uint MaxTessellationControlPerPatchOutputComponents;
    public uint MaxTessellationControlTotalOutputComponents;
    public uint MaxTessellationEvaluationInputComponents;
    public uint MaxTessellationEvaluationOutputComponents;
    public uint MaxGeometryShaderInvocations;
    public uint MaxGeometryInputComponents;
    public uint MaxGeometryOutputComponents;
    public uint MaxGeometryOutputVertices;
    public uint MaxGeometryTotalOutputComponents;
    public uint MaxFragmentInputComponents;
    public uint MaxFragmentOutputAttachments;
    public uint MaxFragmentDualSrcAttachments;
    public uint MaxFragmentCombinedOutputResources;
    public uint MaxComputeSharedMemorySize;
    public unsafe fixed uint MaxComputeWorkGroupCount[3];
    public uint MaxComputeWorkGroupInvocations;
    public unsafe fixed uint MaxComputeWorkGroupSize[3];
    public uint SubPixelPrecisionBits;
    public uint SubTexelPrecisionBits;
    public uint MipmapPrecisionBits;
    public uint MaxDrawIndexedIndexValue;
    public uint MaxDrawIndirectCount;
    public float MaxSamplerLodBias;
    public float MaxSamplerAnisotropy;
    public uint MaxViewports;
    public unsafe fixed uint MaxViewportDimensions[2];
    public unsafe fixed float ViewportBoundsRange[2];
    public uint ViewportSubPixelBits;
    public nuint MinMemoryMapAlignment;
    public ulong MinTexelBufferOffsetAlignment;
    public ulong MinUniformBufferOffsetAlignment;
    public ulong MinStorageBufferOffsetAlignment;
    public int MinTexelOffset;
    public uint MaxTexelOffset;
    public int MinTexelGatherOffset;
    public uint MaxTexelGatherOffset;
    public float MinInterpolationOffset;
    public float MaxInterpolationOffset;
    public uint SubPixelInterpolationOffsetBits;
    public uint MaxFramebufferWidth;
    public uint MaxFramebufferHeight;
    public uint MaxFramebufferLayers;
    public VkSampleCount FramebufferColorSampleCounts;
    public VkSampleCount FramebufferDepthSampleCounts;
    public VkSampleCount FramebufferStencilSampleCounts;
    public VkSampleCount FramebufferNoAttachmentsSampleCounts;
    public uint MaxColorAttachments;
    public VkSampleCount SampledImageColorSampleCounts;
    public VkSampleCount SampledImageIntegerSampleCounts;
    public VkSampleCount SampledImageDepthSampleCounts;
    public VkSampleCount SampledImageStencilSampleCounts;
    public VkSampleCount StorageImageSampleCounts;
    public uint MaxSampleMaskWords;
    public uint TimestampComputeAndGraphics;
    public float TimestampPeriod;
    public uint MaxClipDistances;
    public uint MaxCullDistances;
    public uint MaxCombinedClipAndCullDistances;
    public uint DiscreteQueuePriorities;
    public unsafe fixed float PointSizeRange[2];
    public unsafe fixed float LineWidthRange[2];
    public float PointSizeGranularity;
    public float LineWidthGranularity;
    public uint StrictLines;
    public uint StandardSampleLocations;
    public ulong OptimalBufferCopyOffsetAlignment;
    public ulong OptimalBufferCopyRowPitchAlignment;
    public ulong NonCoherentAtomSize;
}

struct VkPhysicalDeviceSparseProperties
{
    public uint ResidencyStandard2DBlockShape;
    public uint ResidencyStandard2DMultisampleBlockShape;
    public uint ResidencyStandard3DBlockShape;
    public uint ResidencyAlignedMipSize;
    public uint ResidencyNonResidentStrict;
}

struct VkPhysicalDeviceProperties
{
    public uint ApiVersion;
    public uint DriverVersion;
    public uint VendorId;
    public uint DeviceId;
    public VkPhysicalDeviceType DeviceType;
    public unsafe fixed byte DeviceName[256];
    public unsafe fixed byte PipelineCacheUuid[16];
    public VkPhysicalDeviceLimits Limits;
    public VkPhysicalDeviceSparseProperties SparseProperties;
}

[Flags]
public enum VkQueueFlags : uint
{
    GRAPHICS_BIT = 0x00000001,
    COMPUTE_BIT = 0x00000002,
    TRANSFER_BIT = 0x00000004,
    SPARSE_BINDING_BIT = 0x00000008,
    PROTECTED_BIT = 0x00000010,
    VIDEO_DECODE_BIT_KHR = 0x00000020,
    VIDEO_ENCODE_BIT_KHR = 0x00000040,
    OPTICAL_FLOW_BIT_NV = 0x00000100,
}

struct VkExtent2D(uint width, uint height)
{
    public uint Width = width;
    public uint Height = height;
}

struct VkExtent3D(uint width, uint height, uint depth = 1)
{
    public uint Width = width;
    public uint Height = height;
    public uint Depth = depth;
}

struct VkOffset2D(int x, int y)
{
    public int X = x;
    public int Y = y;
}

struct VkOffset3D(int x, int y, int z)
{
    public int X = x;
    public int Y = y;
    public int Z = z;
}

struct VkRect2D(VkOffset2D Offset, VkExtent2D Extent)
{
    public VkOffset2D Offset = Offset;
    public VkExtent2D Extent = Extent;
}

struct VkQueueFamilyProperties
{
    public VkQueueFlags QueueFlags;
    public uint QueueCount;
    public uint TimestampValidBits;
    public VkExtent3D MinImageTransferGranularity;
}

struct VkPhysicalDeviceFeatures
{
    public uint RobustBufferAccess;
    public uint FullDrawIndexUint32;
    public uint ImageCubeArray;
    public uint IndependentBlend;
    public uint GeometryShader;
    public uint TessellationShader;
    public uint SampleRateShading;
    public uint DualSrcBlend;
    public uint LogicOp;
    public uint MultiDrawIndirect;
    public uint DrawIndirectFirstInstance;
    public uint DepthClamp;
    public uint DepthBiasClamp;
    public uint FillModeNonSolid;
    public uint DepthBounds;
    public uint WideLines;
    public uint LargePoints;
    public uint AlphaToOne;
    public uint MultiViewport;
    public uint SamplerAnisotropy;
    public uint TextureCompressionEtc2;
    public uint TextureCompressionAstcLdr;
    public uint TextureCompressionBc;
    public uint OcclusionQueryPrecise;
    public uint PipelineStatisticsQuery;
    public uint VertexPipelineStoresAndAtomics;
    public uint FragmentStoresAndAtomics;
    public uint ShaderTessellationAndGeometryPointSize;
    public uint ShaderImageGatherExtended;
    public uint ShaderStorageImageExtendedFormats;
    public uint ShaderStorageImageMultisample;
    public uint ShaderStorageImageReadWithoutFormat;
    public uint ShaderStorageImageWriteWithoutFormat;
    public uint ShaderUniformBufferArrayDynamicIndexing;
    public uint ShaderSampledImageArrayDynamicIndexing;
    public uint ShaderStorageBufferArrayDynamicIndexing;
    public uint ShaderStorageImageArrayDynamicIndexing;
    public uint ShaderClipDistance;
    public uint ShaderCullDistance;
    public uint ShaderFloat64;
    public uint ShaderInt64;
    public uint ShaderInt16;
    public uint ShaderResourceResidency;
    public uint ShaderResourceMinLod;
    public uint SparseBinding;
    public uint SparseResidencyBuffer;
    public uint SparseResidencyImage2D;
    public uint SparseResidencyImage3D;
    public uint SparseResidency2Samples;
    public uint SparseResidency4Samples;
    public uint SparseResidency8Samples;
    public uint SparseResidency16Samples;
    public uint SparseResidencyAliased;
    public uint VariableMultisampleRate;
    public uint InheritedQueries;
}

struct VkPhysicalDeviceFeatures2(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_FEATURES_2;
    public nint Next = next;
    public VkPhysicalDeviceFeatures Features;
}

struct VkPhysicalDeviceDynamicRenderingFeatures(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_DYNAMIC_RENDERING_FEATURES;
    public nint Next = next;
    public uint DynamicRendering;
}

struct VkPhysicalDevice16BitStorageFeatures(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_16BIT_STORAGE_FEATURES;
    public nint Next = next;
    public uint StorageBuffer16BitAccess;
    public uint UniformAndStorageBuffer16BitAccess;
    public uint StoragePushConstant16;
    public uint StorageInputOutput16;
}

struct VkPhysicalDeviceShaderFloat16Int8Features(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_SHADER_FLOAT16_INT8_FEATURES;
    public nint Next = next;
    public uint ShaderFloat16;
    public uint ShaderInt8;
}

struct VkPhysicalDeviceRobustness2FeaturesEXT(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_ROBUSTNESS_2_FEATURES_EXT;
    public nint Next = next;
    public uint RobustBufferAccess2;
    public uint RobustImageAccess2;
    public uint NullDescriptor;
}

struct VkPhysicalDeviceSynchronization2Features(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_SYNCHRONIZATION_2_FEATURES;
    public nint Next = next;
    public uint Synchronization2;
}

struct VkPhysicalDeviceShaderSubgroupExtendedTypesFeatures(nint next)
{
    public VkStructureType SType = VkStructureType.PHYSICAL_DEVICE_SHADER_SUBGROUP_EXTENDED_TYPES_FEATURES;
    public nint Next = next;
    public uint ShaderSubgroupExtendedTypes;
}

unsafe struct VkDeviceCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.DEVICE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint QueueCreateInfoCount;
    public VkDeviceQueueCreateInfo* QueueCreateInfos;
    public uint EnabledLayerCount;
    public nint EnabledLayerNames;
    public uint EnabledExtensionCount;
    public nint EnabledExtensionNames;
    public VkPhysicalDeviceFeatures* EnabledFeatures;
}

unsafe struct VkDeviceQueueCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.DEVICE_QUEUE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint QueueFamilyIndex;
    public uint QueueCount;
    public float* QueuePriorities;
}

struct VkCommandPoolCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.COMMAND_POOL_CREATE_INFO;
    public nint Next = next;
    public VkCommandPoolCreateFlags Flags;
    public uint QueueFamilyIndex;
}

struct VkFormatProperties
{
    public VkFormatFeatureFlags LinearTilingFeatures;
    public VkFormatFeatureFlags OptimalTilingFeatures;
    public VkFormatFeatureFlags BufferFeatures;
}

struct VkCommandBufferAllocateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.COMMAND_BUFFER_ALLOCATE_INFO;
    public nint Next = next;
    public VkCommandPool Pool;
    public VkCommandBufferLevel Level;
    public uint Count;
}

struct VkCommandBufferBeginInfo(nint next)
{
    public VkStructureType SType = VkStructureType.COMMAND_BUFFER_BEGIN_INFO;
    public nint Next = next;
    public VkCommandBufferUsageFlags Flags;
    public nint InheritanceInfo;
}

#region Windows

struct VkWin32SurfaceCreateInfoKhr(nint next)
{
    public VkStructureType SType = VkStructureType.WIN32_SURFACE_CREATE_INFO_KHR;
    public nint Next = next;
    public uint Flags;
    public nint Hinstance;
    public nint Hwnd;
}

#endregion

struct VkSurfaceCapabilitiesKhr
{
    public uint MinImageCount;
    public uint MaxImageCount;
    public VkExtent2D CurrentExtent;
    public VkExtent2D MinImageExtent;
    public VkExtent2D MaxImageExtent;
    public uint MaxImageArrayLayers;
    public SurfaceTransformFlagsKhr SupportedTransforms;
    public SurfaceTransformFlagsKhr CurrentTransform;
    public CompositeAlphaFlagsKhr SupportedCompositeAlpha;
    public VkImageUsage SupportedUsageFlags;
}

struct VkSwapChainCreateInfoKhr(nint next)
{
    public VkStructureType SType = VkStructureType.SWAPCHAIN_CREATE_INFO_KHR;
    public nint Next = next;
    public VkSwapchainCreateFlagsKhr Flags;
    public VkSurfaceKhr Surface;
    public uint MinImageCount;
    public VkFormat ImageFormat;
    public VkColorSpaceKhr ImageColorSpace;
    public VkExtent2D ImageExtent;
    public uint ImageArrayLayers;
    public VkImageUsage ImageUsage;
    public VkSharingMode ImageSharingMode;
    public uint QueueFamilyIndexCount;
    public nint QueueFamilyIndices;
    public SurfaceTransformFlagsKhr PreTransform;
    public CompositeAlphaFlagsKhr CompositeAlpha;
    public VkPresentModeKhr PresentMode;
    /// <summary>
    /// Setting clipped to true allows the implementation to discard rendering outside of the surface area.
    /// </summary>
    public uint Clipped;
    /// <summary>
    /// Setting OldSwapChain to the saved handle of the previous swap chain aids in resource reuse and makes sure that we can still present already acquired images.
    /// </summary>
    public VkSwapChainKhr OldSwapChain;
}

struct VkSurfaceFormatKhr
{
    public VkFormat Format;
    public VkColorSpaceKhr ColorSpace;
}

struct VkComponentMapping
{
    public VkComponentSwizzle R;
    public VkComponentSwizzle G;
    public VkComponentSwizzle B;
    public VkComponentSwizzle A;
}

struct VkImageSubresourceRange
{
    public VkImageAspect AspectMask;
    public uint BaseMipLevel;
    public uint LevelCount;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

struct VkImageCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.IMAGE_CREATE_INFO;
    public nint Next = next;
    public VkImageCreateFlags Flags;
    public VkImageType ImageType;
    public VkFormat Format;
    public VkExtent3D Extent;
    public uint MipLevels;
    public uint ArrayLayers;
    public VkSampleCount Samples;
    public VkImageTiling Tiling;
    public VkImageUsage Usage;
    public VkSharingMode SharingMode;
    public uint QueueFamilyIndexCount;
    public nint QueueFamilyIndices;
    public VkImageLayout InitialLayout;
}

struct VkImageViewCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.IMAGE_VIEW_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public VkImage Image;
    public VkImageViewType ViewType;
    public VkFormat Format;
    public VkComponentMapping Components;
    public VkImageSubresourceRange SubresourceRange;
}

unsafe struct VkBufferCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.BUFFER_CREATE_INFO;
    public nint Next = next;
    public VkBufferCreateFlags Flags;
    public ulong Size;
    public VkBufferUsage Usage;
    public VkSharingMode SharingMode;
    public uint QueueFamilyIndexCount;
    public uint* QueueFamilyIndices;
}

struct VkBufferViewCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.BUFFER_VIEW_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public ulong Buffer;
    public VkFormat Format;
    public ulong Offset;
    public ulong Range;
}

struct VkSubresourceLayout
{
    public ulong Offset;
    public ulong Size;
    public ulong RowPitch;
    public ulong ArrayPitch;
    public ulong DepthPitch;
}

/// <summary>
/// 40 bytes, alignment 8 
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 40, Pack = 8)]
struct VkShaderModuleCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.SHADER_MODULE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public nuint CodeSize;
    public nint Code;
}

struct VkFenceCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.FENCE_CREATE_INFO;
    public nint Next = next;
    public VkFenceCreateFlags Flags;
}

struct VkSemaphoreCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.SEMAPHORE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
}

struct VkEventCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.EVENT_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
}

unsafe struct VkPresentInfoKhr(nint next)
{
    public VkStructureType SType = VkStructureType.PRESENT_INFO_KHR;
    public nint Next = next;
    public uint WaitSemaphoreCount;
    public VkSemaphore* WaitSemaphores;
    public uint SwapchainCount;
    public VkSwapChainKhr* Swapchains;
    public uint* ImageIndices;
    public nint Results;
}

unsafe struct VkSubmitInfo(nint next)
{
    public VkStructureType SType = VkStructureType.SUBMIT_INFO;
    public nint Next = next;
    public uint WaitSemaphoreCount;
    public VkSemaphore* WaitSemaphores;
    public VkPipelineStage* WaitDstStageMask;
    public uint CommandBufferCount;
    public VkCommandBuffer* CommandBuffers;
    public uint SignalSemaphoreCount;
    public VkSemaphore* SignalSemaphores;
}

struct VkMemoryBarrier2(nint next)
{
    public VkStructureType SType = VkStructureType.MEMORY_BARRIER_2;
    public nint Next = next;
    public VkPipelineStage2 SrcStageMask;
    public VkAccessFlags2 SrcAccessMask;
    public VkPipelineStage2 DstStageMask;
    public VkAccessFlags2 DstAccessMask;
}

struct VkBufferMemoryBarrier2(nint next)
{
    public VkStructureType SType = VkStructureType.BUFFER_MEMORY_BARRIER_2;
    public nint Next = next;
    public VkPipelineStage2 SrcStageMask;
    public VkAccessFlags2 SrcAccessMask;
    public VkPipelineStage2 DstStageMask;
    public VkAccessFlags2 DstAccessMask;
    public uint SrcQueueFamilyIndex;
    public uint DstQueueFamilyIndex;
    public VkBuffer Buffer;
    public ulong Offset;
    public ulong Size;
}

struct VkImageMemoryBarrier2(nint next)
{
    public VkStructureType SType = VkStructureType.IMAGE_MEMORY_BARRIER_2;
    public nint Next = next;
    public VkPipelineStage2 SrcStageMask;
    public VkAccessFlags2 SrcAccessMask;
    public VkPipelineStage2 DstStageMask;
    public VkAccessFlags2 DstAccessMask;
    public VkImageLayout OldLayout;
    public VkImageLayout NewLayout;
    public uint SrcQueueFamilyIndex;
    public uint DstQueueFamilyIndex;
    public VkImage Image;
    public VkImageSubresourceRange SubresourceRange;
}

unsafe struct VkDependencyInfo(nint next)
{
    public VkStructureType SType = VkStructureType.DEPENDENCY_INFO;
    public nint Next = next;
    public VkDependencyFlags DependencyFlags;
    public uint MemoryBarrierCount;
    public VkMemoryBarrier2* MemoryBarriers;
    public uint BufferMemoryBarrierCount;
    public VkBufferMemoryBarrier2* BufferMemoryBarriers;
    public uint ImageMemoryBarrierCount;
    public VkImageMemoryBarrier2* ImageMemoryBarriers;
}

struct VkClearRect
{
    public VkRect2D Rect;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct VkClearValue
{
    [FieldOffset(0)] public VkClearColorValue Color;
    [FieldOffset(0)] public VkClearDepthStencilValue Depth;

    public static VkClearValue FromColor(Color color)
    {
        var value = new VkClearValue();
        value.Color.Float32[0] = color.R;
        value.Color.Float32[1] = color.G;
        value.Color.Float32[2] = color.B;
        value.Color.Float32[3] = color.A;
        return value;
    }

    public static VkClearValue FromDepthStencil(float depth, uint stencil)
    {
        var value = new VkClearValue();
        value.Depth.Depth = depth;
        value.Depth.Stencil = stencil;
        return value;
    }
}

[StructLayout(LayoutKind.Explicit)]
unsafe struct VkClearColorValue
{
    [FieldOffset(0)]
    public fixed float Float32[4];
    [FieldOffset(0)]
    public fixed int Int32[4];
    [FieldOffset(0)]
    public fixed uint UInt32[4];

    public static VkClearColorValue FromColor(Color color)
    {
        var value = new VkClearColorValue();
        value.Float32[0] = color.R;
        value.Float32[1] = color.G;
        value.Float32[2] = color.B;
        value.Float32[3] = color.A;
        return value;
    }
}

struct VkClearDepthStencilValue
{
    public float Depth;
    public uint Stencil;
}

struct VkClearAttachment
{
    public VkImageAspect AspectMask;
    public uint ColorAttachment;
    public VkClearValue ClearValue;
}

struct VkImageSubresourceLayers
{
    public VkImageAspect Aspect;
    public uint MipLevel;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

struct VkImageResolve
{
    public VkImageSubresourceLayers Src;
    public VkOffset3D SrcOffset;
    public VkImageSubresourceLayers Dst;
    public VkOffset3D DstOffset;
    public VkExtent3D Extent;
}

/// <summary>
/// 48 bytes, alignment 8 
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 48, Pack = 8)]
unsafe struct VkPipelineShaderStageCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_SHADER_STAGE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public VkShaderStage Stage;
    public VkShaderModule Module;
    public nint Name;
    public nint SpecializationInfo;
}

/// <summary>
/// 32 bytes, alignment 8 
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 32, Pack = 8)]
struct VkPipelineInputAssemblyStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_INPUT_ASSEMBLY_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public VkPrimitiveTopology Topology;
    public uint PrimitiveRestartEnable;
}

/// <summary>
/// 16 bytes, alignment 4
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 16, Pack = 4)]
struct VkVertexInputAttributeDescription
{
    public uint Location;
    public uint Binding;
    public VkFormat Format;
    public uint Offset;
}

/// <summary>
/// 12 bytes, alignment 4
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 12, Pack = 4)]
struct VkVertexInputBindingDescription
{
    public uint Binding;
    public uint Stride;
    public VkVertexInputRate InputRate;
}

/// <summary>
/// 48 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 48, Pack = 8)]
unsafe struct VkPipelineVertexInputStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_VERTEX_INPUT_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint VertexBindingDescriptionCount;
    public VkVertexInputBindingDescription* VertexBindingDescriptions;
    public uint VertexAttributeDescriptionCount;
    public VkVertexInputAttributeDescription* VertexAttributeDescriptions;
}

struct VkPipelineTessellationStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_TESSELLATION_STATE_CREATE_INFO;
    public nint Next = next;
    public uint flags;
    public uint patchControlPoints;
}

struct VkViewport(float x, float y, float width, float height, float minDepth, float maxDepth)
{
    public float X = x;
    public float Y = y;
    public float Width = width;
    public float Height = height;
    public float MinDepth = minDepth;
    public float MaxDepth = maxDepth;
}

/// <summary>
/// 48 bytes, alignment 8 
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 48, Pack = 8)]
unsafe struct VkPipelineViewportStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_VIEWPORT_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint ViewportCount;
    public VkViewport* Viewports;
    public uint ScissorCount;
    public VkRect2D* Scissors;
}

/// <summary>
/// 64 bytes, alignment 8 
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 64, Pack = 8)]
struct VkPipelineRasterizationStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_RASTERIZATION_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint DepthClampEnable;
    public uint RasterizerDiscardEnable;
    public VkPolygonMode PolygonMode;
    public VkCullMode CullMode;
    public VkFrontFace FrontFace;
    public uint DepthBiasEnable;
    public float DepthBiasConstantFactor;
    public float DepthBiasClamp;
    public float DepthBiasSlopeFactor;
    public float LineWidth;
}

/// <summary>
/// 48 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 48, Pack = 8)]
unsafe struct VkPipelineMultisampleStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_MULTISAMPLE_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public VkSampleCount RasterizationSamples;
    public uint SampleShadingEnable;
    public float MinSampleShading;
    public nint SampleMask;
    public uint AlphaToCoverageEnable;
    public uint AlphaToOneEnable;
}

/// <summary>
/// 28 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 28, Pack = 4)]
struct VkStencilOpState
{
    public VkStencilOp FailOp;
    public VkStencilOp PassOp;
    public VkStencilOp DepthFailOp;
    public VkCompareOp CompareOp;
    public uint CompareMask;
    public uint WriteMask;
    public uint Reference;
}

/// <summary>
/// 104 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 104, Pack = 8)]
struct VkPipelineDepthStencilStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_DEPTH_STENCIL_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint DepthTestEnable;
    public uint DepthWriteEnable;
    public VkCompareOp DepthCompareOp;
    public uint DepthBoundsTestEnable;
    public uint StencilTestEnable;
    public VkStencilOpState Front;
    public VkStencilOpState Back;
    public float MinDepthBounds;
    public float MaxDepthBounds;
}

/// <summary>
/// 32 bytes, alignment 4
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 32, Pack = 4)]
struct VkPipelineColorBlendAttachmentState
{
    public uint BlendEnable;
    public VkBlendFactor SrcColorBlendFactor;
    public VkBlendFactor DstColorBlendFactor;
    public VkBlendOp ColorBlendOp;
    public VkBlendFactor SrcAlphaBlendFactor;
    public VkBlendFactor DstAlphaBlendFactor;
    public VkBlendOp AlphaBlendOp;
    public VkColorComponent ColorWriteMask;
}

/// <summary>
/// 56 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 56, Pack = 8)]
unsafe struct VkPipelineColorBlendStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_COLOR_BLEND_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint LogicOpEnable;
    public VkLogicOp LogicOp;
    public uint AttachmentCount;
    public VkPipelineColorBlendAttachmentState* Attachments;
    public unsafe fixed float BlendConstants[4];
}

/// <summary>
/// 32 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 32, Pack = 8)]
unsafe struct VkPipelineDynamicStateCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_DYNAMIC_STATE_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint DynamicStateCount;
    public VkDynamicState* DynamicStates;
}

/// <summary>
/// 144 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 144, Pack = 8)]
unsafe struct VkGraphicsPipelineCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.GRAPHICS_PIPELINE_CREATE_INFO;
    public nint Next = next;
    public VkPipelineCreateFlags Flags;
    public uint StageCount;
    public VkPipelineShaderStageCreateInfo* Stages;
    public VkPipelineVertexInputStateCreateInfo* VertexInputState;
    public VkPipelineInputAssemblyStateCreateInfo* InputAssemblyState;
    public nint TessellationState;
    public VkPipelineViewportStateCreateInfo* ViewportState;
    public VkPipelineRasterizationStateCreateInfo* RasterizationState;
    public VkPipelineMultisampleStateCreateInfo* MultisampleState;
    public VkPipelineDepthStencilStateCreateInfo* DepthStencilState;
    public VkPipelineColorBlendStateCreateInfo* ColorBlendState;
    public VkPipelineDynamicStateCreateInfo* DynamicState;
    public VkPipelineLayout Layout;
    public ulong RenderPass;
    public uint SubPass;
    public ulong BasePipelineHandle;
    public int BasePipelineIndex;
}

/// <summary>
/// 40 bytes, alignment 8
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 40, Pack = 8)]
unsafe struct VkPipelineRenderingCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_RENDERING_CREATE_INFO;
    public nint Next = next;
    public uint ViewMask;
    public uint ColorAttachmentCount;
    public VkFormat* ColorAttachmentFormats;
    public VkFormat DepthAttachmentFormat;
    public VkFormat StencilAttachmentFormat;
}

struct VkDescriptorPoolSize
{
    public VkDescriptorType Type;
    public uint Count;
}

struct VkPushConstantRange
{
    public VkShaderStage StageFlags;
    public uint Offset;
    public uint Size;
}

unsafe struct VkPipelineLayoutCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_LAYOUT_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public uint SetLayoutCount;
    public VkDescriptorSetLayout* SetLayouts;
    public uint PushConstantRangeCount;
    public VkPushConstantRange* PushConstantRanges;
}

struct VkComputePipelineCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.COMPUTE_PIPELINE_CREATE_INFO;
    public nint Next = next;
    public VkPipelineCreateFlags Flags;
    public VkPipelineShaderStageCreateInfo Stage;
    public VkPipelineLayout Layout;
    public ulong BasePipelineHandle;
    public int BasePipelineIndex;
}

struct VkSamplerCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.SAMPLER_CREATE_INFO;
    public nint Next = next;
    public uint Flags;
    public VkFilter MagFilter;
    public VkFilter MinFilter;
    public VkSamplerMipmapMode MipmapMode;
    public VkSamplerAddressMode AddressModeU;
    public VkSamplerAddressMode AddressModeV;
    public VkSamplerAddressMode AddressModeW;
    public float MipLodBias;
    public uint AnisotropyEnable;
    public float MaxAnisotropy;
    public uint CompareEnable;
    public VkCompareOp CompareOp;
    public float MinLod;
    public float MaxLod;
    public VkBorderColor BorderColor;
    public uint UnNormalizedCoordinates;
}

unsafe struct VkDescriptorSetLayoutBinding
{
    public uint Binding;
    public VkDescriptorType DescriptorType;
    public uint DescriptorCount;
    public VkShaderStage Stages;
    public VkSampler* ImmutableSamplers;
}

unsafe struct VkDescriptorSetLayoutCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.DESCRIPTOR_SET_LAYOUT_CREATE_INFO;
    public nint Next = next;
    public VkDescriptorSetLayoutCreateFlags Flags;
    public uint BindingCount;
    public VkDescriptorSetLayoutBinding* Bindings;
}

unsafe struct VkDescriptorPoolCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.DESCRIPTOR_POOL_CREATE_INFO;
    public nint Next = next;
    public VkDescriptorPoolCreateFlags Flags;
    public uint MaxSets;
    public uint PoolSizeCount;
    public VkDescriptorPoolSize* PoolSizes;
}

unsafe struct VkDescriptorSetAllocateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.DESCRIPTOR_SET_ALLOCATE_INFO;
    public nint Next = next;
    public VkDescriptorPool DescriptorPool;
    public uint DescriptorSetCount;
    public VkDescriptorSetLayout* SetLayouts;
}

struct VkPipelineCacheCreateInfo(nint next)
{
    public VkStructureType SType = VkStructureType.PIPELINE_CACHE_CREATE_INFO;
    public nint Next = next;
    internal uint Flags;
    internal nuint InitialDataSize;
    internal nint InitialData;
}

struct VkRenderingAttachmentInfo(nint next)
{
    public VkStructureType SType = VkStructureType.RENDERING_ATTACHMENT_INFO;
    public nint Next = next;
    public VkImageView ImageView;
    public VkImageLayout ImageLayout;
    public VkResolveMode resolveMode;
    public VkImageView resolveImageView;
    public VkImageLayout resolveImageLayout;
    public VkAttachmentLoadOp LoadOp;
    public VkAttachmentStoreOp StoreOp;
    public VkClearValue ClearValue;
}

unsafe struct VkRenderingInfo(nint next)
{
    public VkStructureType SType = VkStructureType.RENDERING_INFO;
    public nint Next = next;
    public uint Flags;
    public VkRect2D RenderArea;
    public uint LayerCount;
    public uint ViewMask;
    public uint ColorAttachmentCount;
    public VkRenderingAttachmentInfo* ColorAttachments;
    public VkRenderingAttachmentInfo* DepthAttachment;
    public VkRenderingAttachmentInfo* StencilAttachment;
}

struct VkImageCopy
{
    public VkImageSubresourceLayers SrcSubresource;
    public VkOffset3D SrcOffset;
    public VkImageSubresourceLayers DstSubresource;
    public VkOffset3D DstOffset;
    public VkExtent3D Extent;
}

struct VkBufferCopy
{
    public ulong SrcOffset;
    public ulong DstOffset;
    public ulong Size;
}

struct VkImageBlit
{
    internal VkImageSubresourceLayers Src;
    internal VkOffset3D SrcOffsets0;
    internal VkOffset3D SrcOffsets1;
    internal VkImageSubresourceLayers Dst;
    internal VkOffset3D DstOffsets0;
    internal VkOffset3D DstOffsets1;
}

struct VkBufferImageCopy
{
    public ulong BufferOffset;
    public uint BufferRowLength;
    public uint BufferImageHeight;
    public VkImageSubresourceLayers ImageSubresource;
    public VkOffset3D ImageOffset;
    public VkExtent3D ImageExtent;
}