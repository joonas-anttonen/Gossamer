#pragma warning disable CS0649, IDE1006, SYSLIB1054

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Gossamer.External.Vulkan;

[SuppressUnmanagedCodeSecurity]
unsafe static class Api
{
    public const string BinaryName = "vulkan-1";
    const CallingConvention CallConvention = CallingConvention.StdCall;

    internal static delegate* unmanaged[Stdcall]<VkCommandBuffer, VkPipelineBindPoint, VkPipelineLayout, uint, uint, VkWriteDescriptorSet*, void> _vkCmdPushDescriptorSetKhr;

    public static void vkCmdPushDescriptorSet(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint set, uint descriptorWriteCount, VkWriteDescriptorSet* pDescriptorWrites)
    {
        _vkCmdPushDescriptorSetKhr(commandBuffer, pipelineBindPoint, layout, set, descriptorWriteCount, pDescriptorWrites);
    }

    public static class Constants
    {
        public const uint VK_SUBPASS_EXTERNAL = ~0U;
        public const ulong VK_WHOLE_SIZE = ~0UL;
        public const uint VK_QUEUE_FAMILY_IGNORED = ~0U;
        public const ulong DefaultFenceTimeout = 100000000000;
    }

    /// <summary>
    /// Throws a <see cref="VulkanException"/> if the condition is true.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <exception cref="VulkanException"></exception>
    public static void ThrowVulkanIf(bool condition, string message)
    {
        if (condition)
        {
            throw new VulkanException(message);
        }
    }

    /// <summary>
    /// Throws a <see cref="VulkanException"/> if the result is not <see cref="VkResult.SUCCESS"/>.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="message"></param>
    /// <exception cref="VulkanException"></exception>
    public static void ThrowVulkanIfFailed(VkResult result, [CallerMemberName] string message = "")
    {
        if (result != VkResult.SUCCESS)
        {
            throw new VulkanException($"{result}: {message}");
        }
    }

    /// <summary>
    /// Helper to convert an arbitrary pointer to a <see cref="nint"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="pT"></param>
    /// <returns></returns>
    public static nint Next<T>(T* pT) where T : unmanaged
    {
        return (nint)pT;
    }

    /// <summary>
    /// Helper to create a Vulkan version number. Identical to VK_MAKE_API_VERSION. 
    /// </summary>
    /// <param name="variant"></param>
    /// <param name="major"></param>
    /// <param name="minor"></param>
    /// <param name="patch"></param>
    /// <returns></returns>
    public static uint MakeApiVersion(int variant, int major, int minor, int patch)
        => (uint)(variant << 29 | major << 22 | minor << 12 | patch);

    /// <summary>
    /// Helper to parse a Vulkan version number to a <see cref="Version"/>.
    /// </summary>
    /// <param name="version"></param>
    /// <returns></returns>
    public static Version ParseVersion(uint version)
        => new((int)((version & 0xFFC00000) >> 22),
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

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkGetDeviceQueue(VkDevice device, uint queueFamilyIndex, uint queueIndex, VkQueue* pQueue);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateCommandPool(VkDevice device, VkCommandPoolCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkCommandPool* pCommandPool);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyDevice(VkDevice device, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyCommandPool(VkDevice device, VkCommandPool commandPool, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkGetPhysicalDeviceFormatProperties(VkPhysicalDevice physicalDevice, VkFormat format, VkFormatProperties* pFormatProperties);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkResetCommandPool(VkDevice device, VkCommandPool commandPool, VkCommandPoolResetFlags flags);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkAllocateCommandBuffers(VkDevice device, VkCommandBufferAllocateInfo* pAllocateInfo, VkCommandBuffer* pCommandBuffers);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkFreeCommandBuffers(VkDevice device, VkCommandPool commandPool, int commandBufferCount, VkCommandBuffer* pCommandBuffers);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkBeginCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferBeginInfo* pBeginInfo);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkEndCommandBuffer(VkCommandBuffer commandBuffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkResetCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferResetFlags flags);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkDeviceWaitIdle(VkDevice device);

    #region Windows

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkCreateWin32SurfaceKHR")]
    public static extern VkResult vkCreateWin32SurfaceKhr(VkInstance instance, VkWin32SurfaceCreateInfoKhr* pCreateInfo, VkAllocationCallbacks* pAllocator, VkSurfaceKhr* pSurface);

    #endregion

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkDestroySurfaceKHR")]
    public static extern VkResult vkDestroySurfaceKhr(VkInstance instance, VkSurfaceKhr surface, VkAllocationCallbacks* pAllocator);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkGetPhysicalDeviceSurfaceSupportKHR")]
    public static extern VkResult vkGetPhysicalDeviceSurfaceSupportKhr(VkPhysicalDevice physicalDevice, uint queueFamilyIndex, VkSurfaceKhr surface, uint* pSupported);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkGetPhysicalDeviceSurfaceCapabilitiesKHR")]
    public static extern VkResult vkGetPhysicalDeviceSurfaceCapabilitiesKhr(VkPhysicalDevice physicalDevice, VkSurfaceKhr surface, VkSurfaceCapabilitiesKhr* pSurfaceCapabilities);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkGetPhysicalDeviceSurfaceFormatsKHR")]
    public static extern VkResult vkGetPhysicalDeviceSurfaceFormatsKhr(VkPhysicalDevice physicalDevice, VkSurfaceKhr surface, uint* pSurfaceFormatCount, VkSurfaceFormatKhr* pSurfaceFormats = null);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkGetPhysicalDeviceSurfacePresentModesKHR")]
    public static extern VkResult vkGetPhysicalDeviceSurfacePresentModesKhr(VkPhysicalDevice physicalDevice, VkSurfaceKhr surface, uint* pPresentModeCount, VkPresentModeKhr* pPresentModes = null);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkCreateSwapchainKHR")]
    public static extern VkResult vkCreateSwapchainKhr(VkDevice device, VkSwapChainCreateInfoKhr* pCreateInfo, VkAllocationCallbacks* pAllocator, VkSwapChainKhr* pSwapchain);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkDestroySwapchainKHR")]
    public static extern void vkDestroySwapchainKhr(VkDevice device, VkSwapChainKhr swapchain, VkAllocationCallbacks* pAllocator = null);

    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkGetSwapchainImagesKHR")]
    public static extern VkResult vkGetSwapchainImagesKhr(VkDevice device, VkSwapChainKhr swapchain, uint* pSwapchainImageCount, VkImage* pSwapchainImages = null);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateBuffer(VkDevice device, VkBufferCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, out VkBuffer pBuffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyBuffer(VkDevice device, VkBuffer buffer, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateBufferView(VkDevice device, VkBufferViewCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, ulong* pView);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyBufferView(VkDevice device, ulong bufferView, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateImage(VkDevice device, VkImageCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkImage* pImage);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyImage(VkDevice device, VkImage image, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkGetImageSubresourceLayout(VkDevice device, VkImage image, VkImageSubresourceRange* pSubresource, VkSubresourceLayout* pLayout);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateImageView(VkDevice device, VkImageViewCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkImageView* pView);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyImageView(VkDevice device, VkImageView imageView, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateShaderModule(VkDevice device, VkShaderModuleCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkShaderModule* pShaderModule);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyShaderModule(VkDevice device, VkShaderModule shaderModule, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateFence(VkDevice device, VkFenceCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkFence* pFence);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyFence(VkDevice device, VkFence fence, VkAllocationCallbacks* pAllocator = null);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkResetFences(VkDevice device, uint fenceCount, VkFence* pFences);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkGetFenceStatus(VkDevice device, VkFence fence);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkWaitForFences(VkDevice device, uint fenceCount, VkFence* pFences, uint waitAll, ulong timeout);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateSemaphore(VkDevice device, VkSemaphoreCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkSemaphore* pSemaphore);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroySemaphore(VkDevice device, VkSemaphore semaphore, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateEvent(VkDevice device, VkEventCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, ulong* pEvent);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyEvent(VkDevice device, ulong @event, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkGetEventStatus(VkDevice device, ulong @event);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkSetEvent(VkDevice device, ulong @event);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkResetEvent(VkDevice device, ulong @event);

    /// <summary>
    /// https://vulkan.lunarg.com/doc/view/1.3.296.0/windows/1.3-extensions/vkspec.html#VUID-vkAcquireNextImageKHR-surface-07783
    /// </summary>
    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkAcquireNextImageKHR")]
    public static extern VkResult vkAcquireNextImageKhr(VkDevice device, VkSwapChainKhr swapchain, ulong timeout, VkSemaphore semaphore, VkFence fence, uint* pImageIndex);

    /// <summary>
    /// https://registry.khronos.org/vulkan/specs/latest/man/html/vkQueuePresentKHR.html
    /// </summary>
    [DllImport(BinaryName, CallingConvention = CallConvention, EntryPoint = "vkQueuePresentKHR")]
    public static extern VkResult vkQueuePresentKhr(VkQueue queue, VkPresentInfoKhr* pPresentInfo);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkQueueSubmit(VkQueue queue, uint submitCount, VkSubmitInfo* pSubmits, VkFence fence);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkQueueWaitIdle(VkQueue queue);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateGraphicsPipelines(VkDevice device, VkPipelineCache pipelineCache, uint createInfoCount, VkGraphicsPipelineCreateInfo* pCreateInfos, VkAllocationCallbacks* pAllocator, VkPipeline* pPipelines);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateComputePipelines(VkDevice device, VkPipelineCache pipelineCache, uint createInfoCount, VkComputePipelineCreateInfo* pCreateInfos, VkAllocationCallbacks* pAllocator, VkPipeline* pPipelines);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyPipeline(VkDevice device, VkPipeline pipeline, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreatePipelineLayout(VkDevice device, VkPipelineLayoutCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkPipelineLayout* pPipelineLayout);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyPipelineLayout(VkDevice device, VkPipelineLayout pipelineLayout, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateSampler(VkDevice device, VkSamplerCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkSampler* pSampler);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroySampler(VkDevice device, VkSampler sampler, VkAllocationCallbacks* pAllocator);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateDescriptorSetLayout(VkDevice device, VkDescriptorSetLayoutCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDescriptorSetLayout* pSetLayout);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyDescriptorSetLayout(VkDevice device, VkDescriptorSetLayout descriptorSetLayout, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateDescriptorPool(VkDevice device, VkDescriptorPoolCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkDescriptorPool* pDescriptorPool);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyDescriptorPool(VkDevice device, VkDescriptorPool descriptorPool, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkResetDescriptorPool(VkDevice device, VkDescriptorPool descriptorPool, uint flags);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkAllocateDescriptorSets(VkDevice device, VkDescriptorSetAllocateInfo* pAllocateInfo, VkDescriptorSet* pDescriptorSets);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkFreeDescriptorSets(VkDevice device, VkDescriptorPool descriptorPool, uint descriptorSetCount, VkDescriptorSet* pDescriptorSets);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreatePipelineCache(VkDevice device, VkPipelineCacheCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkPipelineCache* pPipelineCache);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyPipelineCache(VkDevice device, VkPipelineCache pipelineCache, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdBeginRendering(VkCommandBuffer commandBuffer, VkRenderingInfo* pRenderingInfo);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdEndRendering(VkCommandBuffer commandBuffer);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdPipelineBarrier2(VkCommandBuffer commandBuffer, VkDependencyInfo* pDependencyInfo);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdClearColorImage(VkCommandBuffer commandBuffer, VkImage image, VkImageLayout imageLayout, VkClearColorValue* pColor, uint rangeCount, VkImageSubresourceRange* pRanges);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdClearDepthStencilImage(VkCommandBuffer commandBuffer, VkImage image, VkImageLayout imageLayout, VkClearDepthStencilValue* pDepthStencil, uint rangeCount, VkImageSubresourceRange* pRanges);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdClearAttachments(VkCommandBuffer commandBuffer, uint attachmentCount, VkClearAttachment* pAttachments, uint rectCount, VkClearRect* pRects);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdResolveImage(VkCommandBuffer commandBuffer, VkImage srcImage, VkImageLayout srcImageLayout, VkImage dstImage, VkImageLayout dstImageLayout, uint regionCount, VkImageResolve* pRegions);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdBindPipeline(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipeline pipeline);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdSetViewport(VkCommandBuffer commandBuffer, uint firstViewport, uint viewportCount, VkViewport* pViewports);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdSetScissor(VkCommandBuffer commandBuffer, uint firstScissor, uint scissorCount, VkRect2D* pScissors);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdSetLineWidth(VkCommandBuffer commandBuffer, float lineWidth);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdBindIndexBuffer(VkCommandBuffer commandBuffer, VkBuffer buffer, ulong offset, VkIndexType indexType);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdBindVertexBuffers(VkCommandBuffer commandBuffer, uint firstBinding, uint bindingCount, VkBuffer* pBuffers, ulong* pOffsets);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdDraw(VkCommandBuffer commandBuffer, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdDrawIndexed(VkCommandBuffer commandBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdDrawIndirect(VkCommandBuffer commandBuffer, ulong buffer, ulong offset, uint drawCount, uint stride);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdDrawIndexedIndirect(VkCommandBuffer commandBuffer, VkBuffer buffer, ulong offset, uint drawCount, uint stride);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdDispatch(VkCommandBuffer commandBuffer, uint groupCountX, uint groupCountY, uint groupCountZ);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdDispatchIndirect(VkCommandBuffer commandBuffer, VkBuffer buffer, ulong offset);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdBindDescriptorSets(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint firstSet, uint descriptorSetCount, VkDescriptorSet* pDescriptorSets, uint dynamicOffsetCount = default, uint* pDynamicOffsets = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdPushConstants(VkCommandBuffer commandBuffer, VkPipelineLayout layout, VkShaderStage stageFlags, uint offset, uint size, void* pValues);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdCopyBuffer(VkCommandBuffer commandBuffer, VkBuffer srcBuffer, VkBuffer dstBuffer, uint regionCount, VkBufferCopy* pRegions);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdCopyImage(VkCommandBuffer commandBuffer, VkImage srcImage, VkImageLayout srcImageLayout, VkImage dstImage, VkImageLayout dstImageLayout, uint regionCount, VkImageCopy* pRegions);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdBlitImage(VkCommandBuffer commandBuffer, VkImage srcImage, VkImageLayout srcImageLayout, VkImage dstImage, VkImageLayout dstImageLayout, uint regionCount, VkImageBlit* pRegions, VkFilter filter);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdCopyBufferToImage(VkCommandBuffer commandBuffer, VkBuffer srcBuffer, VkImage dstImage, VkImageLayout dstImageLayout, uint regionCount, VkBufferImageCopy* pRegions);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdCopyImageToBuffer(VkCommandBuffer commandBuffer, ulong srcImage, VkImageLayout srcImageLayout, ulong dstBuffer, uint regionCount, VkBufferImageCopy* pRegions);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkCreateQueryPool(VkDevice device, VkQueryPoolCreateInfo* pCreateInfo, VkAllocationCallbacks* pAllocator, VkQueryPool* pQueryPool);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdResetQueryPool(VkCommandBuffer commandBuffer, VkQueryPool queryPool, uint firstQuery, uint queryCount);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkDestroyQueryPool(VkDevice device, VkQueryPool queryPool, VkAllocationCallbacks* pAllocator = default);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern VkResult vkGetQueryPoolResults(VkDevice device, VkQueryPool queryPool, uint firstQuery, uint queryCount, nuint dataSize, nint pData, ulong stride, VkQueryResultFlags flags);

    [DllImport(BinaryName, CallingConvention = CallConvention)]
    public static extern void vkCmdWriteTimestamp(VkCommandBuffer commandBuffer, VkPipelineStage pipelineStage, VkQueryPool queryPool, uint query);

}