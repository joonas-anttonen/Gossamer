#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Vulkan;

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    const string VulkanLibrary = "vulkan-1";
    const CallingConvention CallConvention = CallingConvention.Winapi;

    public static class Constants
    {
        public const uint VK_SUBPASS_EXTERNAL = ~0U;
        public const ulong VK_WHOLE_SIZE = ~0UL;
        public const ulong DefaultFenceTimeout = 100000000000;
    }

    public static void ThrowIfFailed(VkResult result, string message)
    {
        if (result != VkResult.SUCCESS)
        {
            throw new GossamerException($"{result}: {message}");
        }
    }

    public static uint MAKE_API_VERSION(int variant, int major, int minor, int patch)
        => (uint)(variant << 29 | major << 22 | minor << 12 | patch);

    public static Version ParseVersion(uint version)
        => new(
            (int)((version & 0xFFC00000) >> 22),
            (int)((version & 0x003FF000) >> 12),
            (int)((version & 0x00000FFF) >> 0));

    [DllImport(VulkanLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumerateInstanceVersion(uint* pApiVersion);
    [DllImport(VulkanLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumerateInstanceLayerProperties(uint* pPropertyCount, VkLayerProperties* pProperties);

    [DllImport(VulkanLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumerateInstanceExtensionProperties([MarshalAs(UnmanagedType.LPUTF8Str)] string? pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties = default);

    [DllImport(VulkanLibrary, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateInstance(VkInstanceCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkInstance* pInstance);

    [DllImport(VulkanLibrary, CallingConvention = CallConvention)]
    public static extern void vkDestroyInstance(VkInstance instance, VkAllocationCallbacks* pAllocator = default);

    [DllImport(VulkanLibrary, CallingConvention = CallConvention)]
    public static extern nint vkGetInstanceProcAddr(VkInstance instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string pName);

    [DllImport(VulkanLibrary, CallingConvention = CallConvention)]
    public static extern nint vkGetDeviceProcAddr(VkDevice device, [MarshalAs(UnmanagedType.LPUTF8Str)] string pName);
}