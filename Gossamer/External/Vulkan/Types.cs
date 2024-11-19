#pragma warning disable CS0649

namespace Gossamer.External.Vulkan;

readonly struct VkInstance { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkPhysicalDevice { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDevice { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkQueue { internal readonly nint Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkFence { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkSemaphore { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkSwapChain { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
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
readonly struct VkSurface { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkPipelineCache { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkShaderModule { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

#region VK_EXT_debug_utils

readonly struct VkDebugUtilsMessengerEXT { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }
readonly struct VkDebugReportCallbackEXT { internal readonly ulong Value; public bool HasValue => Value != 0; public override string ToString() => Value.ToString("x"); }

unsafe delegate uint PFN_vkDebugUtilsMessengerCallbackEXT(
    VkDebugUtilsMessageSeverityEXT messageSeverity,
    VkDebugUtilsMessageTypeEXT messageTypes,
    VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
    nint pUserData);
unsafe delegate VkResult PFN_vkCreateDebugUtilsMessengerEXT(
    VkInstance instance,
    VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo,
    nint pAllocator,
    VkDebugUtilsMessengerEXT* pMessenger);
unsafe delegate void PFN_vkDestroyDebugUtilsMessengerEXT(
    VkInstance instance,
    VkDebugUtilsMessengerEXT messenger,
    nint pAllocator);
unsafe delegate void PFN_vkSetDebugUtilsObjectNameEXT(
    VkDevice device,
    ref DebugUtilsObjectNameInfoEXT pNameInfo);

unsafe struct DebugUtilsObjectNameInfoEXT(nint next)
{
    public const uint DEBUG_UTILS_OBJECT_NAME_INFO_EXT = 1000128000;

    public VkStructureType SType = (VkStructureType)DEBUG_UTILS_OBJECT_NAME_INFO_EXT;
    public nint Next = next;
    public VkObjectType ObjectType;
    public ulong ObjectHandle;
    public nint ObjectName;
}

unsafe struct VkDebugUtilsMessengerCreateInfoEXT(nint next)
{
    public const uint VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT = 1000128004;

    public VkStructureType SType = (VkStructureType)VK_STRUCTURE_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT;
    public nint Next = next;
    public uint Flags;
    public VkDebugUtilsMessageSeverityEXT MessageSeverity;
    public VkDebugUtilsMessageTypeEXT MessageType;
    public nint UserCallback;
    public nint pUserData;
}

unsafe struct VkDebugUtilsMessengerCallbackDataEXT(nint next = default)
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