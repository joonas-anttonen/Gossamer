#pragma warning disable CS0649

using System.Diagnostics;

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