#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Vulkan;

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "vulkan-1";
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

    public static nint Next<T>(T* pT) where T : unmanaged
    {
        return (nint)pT;
    }

    public static uint MakeApiVersion(int variant, int major, int minor, int patch)
        => (uint)(variant << 29 | major << 22 | minor << 12 | patch);

    public static Version ParseVersion(uint version)
        => new(
            (int)((version & 0xFFC00000) >> 22),
            (int)((version & 0x003FF000) >> 12),
            (int)((version & 0x00000FFF) >> 0));

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumerateInstanceVersion(uint* pApiVersion);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumerateInstanceLayerProperties(uint* pPropertyCount, VkLayerProperties* pProperties);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumerateInstanceExtensionProperties([MarshalAs(UnmanagedType.LPUTF8Str)] string? pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumerateDeviceExtensionProperties(VkPhysicalDevice physicalDevice, [MarshalAs(UnmanagedType.LPUTF8Str)] string? pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkEnumeratePhysicalDevices(VkInstance instance, uint* pPhysicalDeviceCount, VkPhysicalDevice* pPhysicalDevices = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkGetPhysicalDeviceProperties(VkPhysicalDevice physicalDevice, VkPhysicalDeviceProperties* pProperties);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkGetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice, uint* pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkGetPhysicalDeviceFeatures2(VkPhysicalDevice physicalDevice, VkPhysicalDeviceFeatures2* pFeatures);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateInstance(VkInstanceCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkInstance* pInstance);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyInstance(VkInstance instance, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint vkGetInstanceProcAddr(VkInstance instance, [MarshalAs(UnmanagedType.LPUTF8Str)] string pName);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern nint vkGetDeviceProcAddr(VkDevice device, [MarshalAs(UnmanagedType.LPUTF8Str)] string pName);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateDevice(VkPhysicalDevice physicalDevice, VkDeviceCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDevice* pDevice);
}