using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Gossamer.External.Vulkan;
using Gossamer.External.Vulkan.Vma;
using Gossamer.Logging;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.External.Vulkan.Vma.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Backend;

record class GfxPipeline(VkPipeline Pipeline, VkPipelineLayout Layout, VkDescriptorSetLayout DescriptorLayout)
{

}

readonly record struct GfxSingleCommand(VkCommandBuffer CommandBuffer, VkFence Fence);

record class GfxPipelineParameters(
    string ShaderProgram,
    VkPushConstantRange[] PushConstants,
    VkDescriptorSetLayoutBinding[] Layout,
    VkPrimitiveTopology InputTopology,
    VkCullMode CullMode,
    VkFrontFace FrontFace,
    VkVertexInputBindingDescription[] InputBindings,
    VkVertexInputAttributeDescription[] InputAttributes,
    PipelineAttachment[] Attachments,
    bool DepthTest,
    bool DepthWrite,
    VkCompareOp DepthCompareOp,
    bool Multisampling
);

record struct PipelineAttachment(VkFormat Format, VkPipelineColorBlendAttachmentState Blend);

public class MemoryBuffer<T>
{
    /// <summary>
    /// The length of the buffer in elements.
    /// </summary>
    public uint Length { get; }

    internal VkBuffer Buffer { get; }
    internal VmaAllocation Allocation { get; }

    internal MemoryBuffer(uint length, VkBuffer buffer, VmaAllocation allocation)
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
    public uint Layers { get; }

    internal VkImageLayout Layout { get; set; }
    internal VkImage Image { get; }
    internal VkImageView View { get; }
    internal VmaAllocation Allocation { get; }

    internal PixelBuffer(GfxFormat format, GfxAspect aspect, GfxSamples samples, uint width, uint height, uint layers, VkImage image, VkImageView view, VmaAllocation allocation)
    {
        Format = format;
        Aspect = aspect;
        Image = image;
        View = view;
        Allocation = allocation;
        Width = width;
        Height = height;
        Samples = samples;
        Layers = layers;
    }
}

public abstract class GfxPresenter : IDisposable
{
    protected bool isDisposed;

    public abstract bool BeginFrame();
    public abstract void EndFrame();

    protected abstract void Dispose(bool disposing);

    internal abstract VkCommandBuffer GetCommandBuffer();
    public abstract PixelBuffer GetPresentationBuffer();

    ~GfxPresenter()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public sealed class GfxHeadlessPresenter : GfxPresenter
{
    public override bool BeginFrame()
    {
        return false;
    }

    public override void EndFrame()
    {
    }

    public override PixelBuffer GetPresentationBuffer()
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {

    }

    internal override VkCommandBuffer GetCommandBuffer()
    {
        throw new NotImplementedException();
    }
}

public sealed class GfxDirectXPresenter : GfxPresenter
{
    public override bool BeginFrame()
    {
        return false;
    }

    public override void EndFrame()
    {
    }

    public override PixelBuffer GetPresentationBuffer()
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {

    }

    internal override VkCommandBuffer GetCommandBuffer()
    {
        throw new NotImplementedException();
    }
}

internal unsafe sealed class GfxSwapChainPresenter(
    VkInstance instance,
    VkPhysicalDevice physicalDevice,
    VkDevice device,
    VkQueue deviceQueue,
    uint deviceQueueIndex,
    VkSurfaceKhr surface) : GfxPresenter
{
    readonly Logger logger = Gossamer.Instance.Log.GetLogger(nameof(GfxSwapChainPresenter));

    class PerFrame(VkCommandPool CommandPool, VkCommandBuffer CommandBuffer, VkSemaphore ReleaseSemaphore, VkFence SubmissionFence, PixelBuffer OutputImage)
    {
        public VkCommandPool CommandPool { get; set; } = CommandPool;
        public VkCommandBuffer CommandBuffer { get; set; } = CommandBuffer;
        public VkFence SubmissionFence { get; set; } = SubmissionFence;
        public PixelBuffer? OutputImage { get; set; } = OutputImage;

        public VkSemaphore ReleaseSemaphore { get; set; } = ReleaseSemaphore;
        public VkSemaphore AcquireSemaphore { get; set; }
    }

    readonly VkInstance instance = instance;
    readonly VkPhysicalDevice physicalDevice = physicalDevice;
    readonly VkDevice device = device;
    readonly VkQueue deviceQueue = deviceQueue;
    readonly uint deviceQueueIndex = deviceQueueIndex;

    VkSurfaceKhr surface = surface;
    VkSwapChainKhr swapChain;

    PerFrame[] perFrame = [];
    int currentFrameIndex;

    readonly Queue<VkSemaphore> semaphores = new();

    readonly Stopwatch stopwatch = Stopwatch.StartNew();

    internal override VkCommandBuffer GetCommandBuffer()
    {
        PerFrame frame = perFrame[currentFrameIndex];
        return frame.CommandBuffer;
    }

    public override PixelBuffer GetPresentationBuffer()
    {
        PerFrame frame = perFrame[currentFrameIndex];
        ThrowInvalidOperationIfNull(frame.OutputImage, "No output image available.");
        return frame.OutputImage;
    }

    VkResult AcquireNextImage()
    {
        if (!swapChain.HasValue)
        {
            return VkResult.OUT_OF_DATE_KHR;
        }

        VkSemaphore acquireSemaphore = default;

        if (semaphores.Count > 0)
        {
            acquireSemaphore = semaphores.Dequeue();
        }
        else
        {
            VkSemaphoreCreateInfo semaphoreCreateInfo = new(default);
            ThrowVulkanIfFailed(vkCreateSemaphore(device, &semaphoreCreateInfo, default, &acquireSemaphore));
        }

        PerFrame? oldFrame = perFrame[currentFrameIndex];
        Assert(oldFrame != null);

        if (oldFrame.SubmissionFence.HasValue)
        {
            TimeSpan beforeWait = stopwatch.Elapsed;

            VkFence fence = oldFrame.SubmissionFence;
            ThrowVulkanIfFailed(vkWaitForFences(device, 1, &fence, 1, ulong.MaxValue));

            TimeSpan afterWait = stopwatch.Elapsed;

            TimeSpan waitTime = afterWait - beforeWait;

            if (waitTime.TotalMilliseconds > 1)
            {
                logger.Debug($"Waited for fence for {waitTime}.");
            }
        }

        uint nextFrameIndex = 0;
        VkResult result = vkAcquireNextImageKhr(device, swapChain, ulong.MaxValue, acquireSemaphore, default, &nextFrameIndex);
        if (result != VkResult.SUCCESS)
        {
            vkDestroySemaphore(device, acquireSemaphore, default);
            return result;
        }

        currentFrameIndex = (int)nextFrameIndex;

        PerFrame? frame = perFrame[currentFrameIndex];
        Assert(frame != null);

        if (frame.SubmissionFence.HasValue)
        {
            VkFence fence = frame.SubmissionFence;
            ThrowVulkanIfFailed(vkWaitForFences(device, 1, &fence, 1, ulong.MaxValue));
            ThrowVulkanIfFailed(vkResetFences(device, 1, &fence));
        }

        if (frame.CommandPool.HasValue)
        {
            VkCommandPool commandPool = frame.CommandPool;
            ThrowVulkanIfFailed(vkResetCommandPool(device, commandPool, 0));
        }

        if (frame.AcquireSemaphore.HasValue)
        {
            semaphores.Enqueue(frame.AcquireSemaphore);
        }
        frame.AcquireSemaphore = acquireSemaphore;

        return result;
    }

    public override bool BeginFrame()
    {
        VkResult result = AcquireNextImage();
        if (result == VkResult.OUT_OF_DATE_KHR || result == VkResult.SUBOPTIMAL_KHR)
        {
            bool refreshOK = Refresh(false);
            if (!refreshOK)
            {
                return false;
            }

            result = AcquireNextImage();
        }
        if (result != VkResult.SUCCESS)
        {
            vkQueueWaitIdle(deviceQueue);
            return false;
        }

        PerFrame frame = perFrame[currentFrameIndex];

        VkCommandBufferBeginInfo commandBufferBeginInfo = new(default)
        {
            Flags = VkCommandBufferUsageFlags.ONE_TIME_SUBMIT_BIT
        };

        VkCommandBuffer commandBuffer = frame.CommandBuffer;
        ThrowVulkanIfFailed(vkBeginCommandBuffer(commandBuffer, &commandBufferBeginInfo));

        AssertNotNull(frame.OutputImage);

        TransitionImageLayout(
            pixelBuffer: frame.OutputImage,
            commandBuffer: frame.CommandBuffer,
            srcLayout: VkImageLayout.UNDEFINED,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            srcStage: VkPipelineStage2.TOP_OF_PIPE,
            dstStage: VkPipelineStage2.ALL_TRANSFER);

        Color clearColor = Color.UnpackRGB(0x1a1c1d);
        VkClearColorValue clearColorValue = new();
        clearColorValue.Float32[0] = clearColor.R;
        clearColorValue.Float32[1] = clearColor.G;
        clearColorValue.Float32[2] = clearColor.B;
        clearColorValue.Float32[3] = clearColor.A;
        VkImageSubresourceRange clearRange = new()
        {
            AspectMask = VkImageAspect.COLOR,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1
        };
        vkCmdClearColorImage(frame.CommandBuffer, frame.OutputImage.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, &clearColorValue, 1, &clearRange);

        return true;
    }

    void TransitionImageLayout(PixelBuffer pixelBuffer, VkCommandBuffer commandBuffer, VkImageLayout srcLayout, VkImageLayout dstLayout, VkPipelineStage2 srcStage, VkPipelineStage2 dstStage)
    {
        VkAccessFlags2 srcAccess;
        VkAccessFlags2 dstAccess;

        switch (srcLayout)
        {
            case VkImageLayout.UNDEFINED:
                srcAccess = VkAccessFlags2.NONE;
                break;
            case VkImageLayout.COLOR_ATTACHMENT_OPTIMAL:
                srcAccess = VkAccessFlags2.COLOR_ATTACHMENT_WRITE_BIT;
                break;
            case VkImageLayout.TRANSFER_DST_OPTIMAL:
                srcAccess = VkAccessFlags2.TRANSFER_WRITE_BIT;
                break;
            default:
                Assert(false);
                srcAccess = VkAccessFlags2.NONE;
                break;
        }

        switch (dstLayout)
        {
            case VkImageLayout.TRANSFER_DST_OPTIMAL:
                dstAccess = VkAccessFlags2.TRANSFER_WRITE_BIT;
                break;
            case VkImageLayout.COLOR_ATTACHMENT_OPTIMAL:
                dstAccess = VkAccessFlags2.COLOR_ATTACHMENT_WRITE_BIT;
                break;
            case VkImageLayout.PRESENT_SRC_KHR:
                dstAccess = VkAccessFlags2.NONE;
                break;
            default:
                Assert(false);
                dstAccess = VkAccessFlags2.NONE;
                break;
        }

        VkImageMemoryBarrier2 imageMemoryBarrier = new(default)
        {
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess,
            SrcStageMask = srcStage,
            DstStageMask = dstStage,
            OldLayout = srcLayout,
            NewLayout = dstLayout,
            SrcQueueFamilyIndex = Constants.VK_QUEUE_FAMILY_IGNORED,
            DstQueueFamilyIndex = Constants.VK_QUEUE_FAMILY_IGNORED,
            Image = pixelBuffer.Image,
            SubresourceRange = new VkImageSubresourceRange
            {
                AspectMask = (VkImageAspect)pixelBuffer.Aspect,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = pixelBuffer.Layers
            }
        };

        VkDependencyInfo dependencyInfo = new(default)
        {
            ImageMemoryBarrierCount = 1,
            ImageMemoryBarriers = &imageMemoryBarrier
        };

        vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);
    }

    public override void EndFrame()
    {
        PerFrame frame = perFrame[currentFrameIndex];
        Assert(frame.OutputImage != null);

        TransitionImageLayout(
            pixelBuffer: frame.OutputImage,
            commandBuffer: frame.CommandBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.PRESENT_SRC_KHR,
            srcStage: VkPipelineStage2.ALL_TRANSFER,
            dstStage: VkPipelineStage2.BOTTOM_OF_PIPE);

        ThrowVulkanIfFailed(vkEndCommandBuffer(frame.CommandBuffer));

        VkPipelineStage pipelineStages = VkPipelineStage.TOP_OF_PIPE;
        VkSemaphore acquireSemaphore = frame.AcquireSemaphore;
        VkSemaphore releaseSemaphore = frame.ReleaseSemaphore;
        VkCommandBuffer commandBuffer = frame.CommandBuffer;

        VkSubmitInfo submitInfo = new(default)
        {
            WaitSemaphoreCount = 1,
            WaitSemaphores = &acquireSemaphore,
            WaitDstStageMask = &pipelineStages,
            CommandBufferCount = 1,
            CommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            SignalSemaphores = &releaseSemaphore
        };

        ThrowVulkanIfFailed(vkQueueSubmit(deviceQueue, 1, &submitInfo, frame.SubmissionFence));

        VkSwapChainKhr swapChain = this.swapChain;
        uint presentFrameIndex = (uint)currentFrameIndex;
        VkPresentInfoKhr presentInfo = new(default)
        {
            WaitSemaphoreCount = 1,
            WaitSemaphores = &releaseSemaphore,
            SwapchainCount = 1,
            Swapchains = &swapChain,
            ImageIndices = &presentFrameIndex
        };

        VkResult result = vkQueuePresentKhr(deviceQueue, &presentInfo);
        if (result == VkResult.OUT_OF_DATE_KHR || result == VkResult.SUBOPTIMAL_KHR)
        {
            Refresh(false);
        }
        else if (result != VkResult.SUCCESS)
        {
            ThrowVulkanIfFailed(result, "Failed to end frame.");
        }
    }

    void ReleaseSwapChainIfAny()
    {
        if (swapChain.HasValue)
        {
            ReleasePerFrame();

            vkDestroySwapchainKhr(device, swapChain);
            swapChain = default;
        }
    }

    public bool Refresh(bool enableVerticalSync)
    {
        ThrowVulkanIfFailed(vkDeviceWaitIdle(device),
            "Failed to wait for device idle.");

        VkSurfaceFormatKhr outputSurfaceFormat = default;

        var surfaceFormatCount = 0u;
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfaceFormatsKhr(physicalDevice, surface, &surfaceFormatCount),
            "Failed to get surface format count.");
        ThrowIf(surfaceFormatCount == 0, "No surface formats found.");

        var surfaceFormats = stackalloc VkSurfaceFormatKhr[(int)surfaceFormatCount];
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfaceFormatsKhr(physicalDevice, surface, &surfaceFormatCount, surfaceFormats),
            "Failed to get surface formats.");

        // If the surface format list only includes one entry with Undefined, there is no preferred format, so we assume B8G8R8A8
        if (surfaceFormatCount == 1 && surfaceFormats[0].Format == VkFormat.UNDEFINED)
        {
            outputSurfaceFormat.Format = VkFormat.B8G8R8A8_UNORM;
            outputSurfaceFormat.ColorSpace = surfaceFormats[0].ColorSpace;
        }
        else
        {
            // Iterate over the list of available surface format and check for the presence of B8G8R8A8
            bool foundB8G8R8A8UNorm = false;
            for (int i = 0; i < surfaceFormatCount; i++)
            {
                VkSurfaceFormatKhr surfaceFormat = surfaceFormats[i];

                if (surfaceFormat.Format == VkFormat.B8G8R8A8_UNORM)
                {
                    outputSurfaceFormat.Format = surfaceFormat.Format;
                    outputSurfaceFormat.ColorSpace = surfaceFormat.ColorSpace;
                    foundB8G8R8A8UNorm = true;
                    break;
                }
            }

            // In case B8G8R8A8 is not available select the first available color format
            if (!foundB8G8R8A8UNorm)
            {
                outputSurfaceFormat.Format = surfaceFormats[0].Format;
                outputSurfaceFormat.ColorSpace = surfaceFormats[0].ColorSpace;
            }
        }

        // Get surface capabilities and present modes

        VkSurfaceCapabilitiesKhr surfaceCapabilities;
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfaceCapabilitiesKhr(physicalDevice, surface, &surfaceCapabilities));

        uint presentModesCount = 0;
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfacePresentModesKhr(physicalDevice, surface, &presentModesCount));

        VkPresentModeKhr* presentModes = stackalloc VkPresentModeKhr[(int)presentModesCount];
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfacePresentModesKhr(physicalDevice, surface, &presentModesCount, presentModes));

        bool swapChainExtentOK;

        VkExtent2D swapChainExtent = new();
        if (surfaceCapabilities.CurrentExtent.Width == uint.MaxValue)
        {
            swapChainExtentOK = false;
        }
        else if (surfaceCapabilities.CurrentExtent.Width == 0 || surfaceCapabilities.CurrentExtent.Height == 0)
        {
            swapChainExtentOK = false;
        }
        else
        {
            swapChainExtentOK = true;
            // If the surface size is defined, the swap chain size must match
            swapChainExtent = surfaceCapabilities.CurrentExtent;
        }

        if (!swapChainExtentOK)
        {
            ReleaseSwapChainIfAny();
            return false;
        }

        // Select a present mode for the swap chain, the VK_PRESENT_MODE_FIFO_KHR mode must always be present as per spec, this mode waits for the vertical blank ("v-sync")
        VkPresentModeKhr swapChainPresentMode = VkPresentModeKhr.FIFO_KHR;

        if (!enableVerticalSync)
        {
            // If v-sync is not requested, try to find a mailbox mode, it's the lowest latency non-tearing present mode available
            for (int i = 0; i < presentModesCount; i++)
            {
                if (presentModes[i] == VkPresentModeKhr.MAILBOX_KHR)
                {
                    swapChainPresentMode = VkPresentModeKhr.MAILBOX_KHR;
                    break;
                }

                if (presentModes[i] == VkPresentModeKhr.IMMEDIATE_KHR)
                {
                    swapChainPresentMode = VkPresentModeKhr.IMMEDIATE_KHR;
                }
            }
        }

        // Determine the number of images
        uint desiredNumberOfSwapChainImages = Math.Min(surfaceCapabilities.MinImageCount + 1, surfaceCapabilities.MaxImageCount);

        // Determine the image usage
        VkImageUsage swapChainImageUsage = VkImageUsage.COLOR_ATTACHMENT_BIT;

        if (surfaceCapabilities.SupportedUsageFlags.HasFlag(VkImageUsage.TRANSFER_SRC_BIT))
        {
            swapChainImageUsage |= VkImageUsage.TRANSFER_SRC_BIT;
        }

        if (surfaceCapabilities.SupportedUsageFlags.HasFlag(VkImageUsage.TRANSFER_DST_BIT))
        {
            swapChainImageUsage |= VkImageUsage.TRANSFER_DST_BIT;
        }

        CompositeAlphaFlagsKhr compositeAlpha = CompositeAlphaFlagsKhr.OPAQUE_BIT_KHR;
        if (surfaceCapabilities.SupportedCompositeAlpha.HasFlag(CompositeAlphaFlagsKhr.INHERIT_BIT_KHR))
        {
            compositeAlpha = CompositeAlphaFlagsKhr.INHERIT_BIT_KHR;
        }

        VkSwapChainCreateInfoKhr swapChainCreateInfo = new(default)
        {
            Surface = surface,
            MinImageCount = desiredNumberOfSwapChainImages,
            ImageFormat = outputSurfaceFormat.Format,
            ImageColorSpace = outputSurfaceFormat.ColorSpace,
            ImageExtent = swapChainExtent,
            ImageUsage = swapChainImageUsage,
            ImageArrayLayers = 1,
            ImageSharingMode = VkSharingMode.EXCLUSIVE,
            PreTransform = SurfaceTransformFlagsKhr.IDENTITY_BIT_KHR,
            QueueFamilyIndexCount = 0,
            OldSwapChain = swapChain,
            Clipped = 1,
            PresentMode = swapChainPresentMode,
            CompositeAlpha = compositeAlpha
        };

        // Create swap chain

        VkSwapChainKhr pSwapChain = default;
        ThrowVulkanIfFailed(vkCreateSwapchainKhr(device, &swapChainCreateInfo, default, &pSwapChain));

        ReleaseSwapChainIfAny();
        swapChain = pSwapChain;

        // Query swap chain images

        uint swapChainImageCount = 0;
        ThrowVulkanIfFailed(vkGetSwapchainImagesKhr(device, swapChain, &swapChainImageCount));

        VkImage* pSwapChainImages = stackalloc VkImage[(int)swapChainImageCount];
        ThrowVulkanIfFailed(vkGetSwapchainImagesKhr(device, swapChain, &swapChainImageCount, pSwapChainImages));

        Array.Resize(ref perFrame, (int)swapChainImageCount);

        // Create image views for swap chain images

        for (int i = 0; i < perFrame.Length; i++)
        {
            VkImage swapChainImage = pSwapChainImages[i];
            VkImageView swapChainImageView;

            VkImageViewCreateInfo colorAttachmentView = new(default)
            {
                ViewType = VkImageViewType.TYPE_2D,
                Image = swapChainImage,
                Format = outputSurfaceFormat.Format,
                Components = new VkComponentMapping
                {
                    R = VkComponentSwizzle.R,
                    G = VkComponentSwizzle.G,
                    B = VkComponentSwizzle.B,
                    A = VkComponentSwizzle.A
                },
                SubresourceRange = new VkImageSubresourceRange
                {
                    AspectMask = VkImageAspect.COLOR,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };

            ThrowVulkanIfFailed(vkCreateImageView(device, &colorAttachmentView, default, &swapChainImageView));

            PixelBuffer outputImage = new(
                format: (GfxFormat)outputSurfaceFormat.Format,
                aspect: GfxAspect.Color,
                samples: GfxSamples.X1,
                width: swapChainExtent.Width,
                height: swapChainExtent.Height,
                layers: 1,
                image: swapChainImage,
                view: swapChainImageView,
                allocation: default
            );

            VkFenceCreateInfo fenceCreateInfo = new(default)
            {
                Flags = VkFenceCreateFlags.SIGNALED
            };

            VkFence submissionFence;
            ThrowVulkanIfFailed(vkCreateFence(device, &fenceCreateInfo, default, &submissionFence));

            VkCommandPoolCreateInfo commandPoolCreateInfo = new(default)
            {
                QueueFamilyIndex = deviceQueueIndex,
                Flags = VkCommandPoolCreateFlags.TRANSIENT
            };

            VkCommandPool commandPool;
            ThrowVulkanIfFailed(vkCreateCommandPool(device, &commandPoolCreateInfo, default, &commandPool));

            VkCommandBufferAllocateInfo commandBufferAllocateInfo = new(default)
            {
                Level = VkCommandBufferLevel.PRIMARY,
                Pool = commandPool,
                Count = 1
            };

            VkCommandBuffer commandBuffer;
            ThrowVulkanIfFailed(vkAllocateCommandBuffers(device, &commandBufferAllocateInfo, &commandBuffer));

            VkSemaphoreCreateInfo semaphoreCreateInfo = new(default);

            VkSemaphore releaseSemaphore;
            ThrowVulkanIfFailed(vkCreateSemaphore(device, &semaphoreCreateInfo, default, &releaseSemaphore));

            perFrame[i] = new PerFrame(commandPool, commandBuffer, releaseSemaphore, submissionFence, outputImage);
        }

        return true;
    }

    void ReleasePerFrame()
    {
        for (int i = 0; i < perFrame.Length; i++)
        {
            PerFrame? frame = perFrame[i];
            if (frame == null)
            {
                continue;
            }

            if (frame.ReleaseSemaphore.HasValue)
            {
                vkDestroySemaphore(device, frame.ReleaseSemaphore, default);
                frame.ReleaseSemaphore = default;
            }
            if (frame.AcquireSemaphore.HasValue)
            {
                vkDestroySemaphore(device, frame.AcquireSemaphore, default);
                frame.AcquireSemaphore = default;
            }
            if (frame.SubmissionFence.HasValue)
            {
                vkDestroyFence(device, frame.SubmissionFence, default);
                frame.SubmissionFence = default;
            }
            if (frame.OutputImage != null && frame.OutputImage.View.HasValue)
            {
                vkDestroyImageView(device, frame.OutputImage.View, default);
                frame.OutputImage = default;
            }
            if (frame.CommandBuffer.HasValue)
            {
                VkCommandBuffer commandBuffer = frame.CommandBuffer;
                vkFreeCommandBuffers(device, frame.CommandPool, 1, &commandBuffer);
                frame.CommandBuffer = default;
            }
            if (frame.CommandPool.HasValue)
            {
                vkDestroyCommandPool(device, frame.CommandPool, default);
                frame.CommandPool = default;
            }
        }

        while (semaphores.Count > 0)
        {
            VkSemaphore semaphore = semaphores.Dequeue();
            vkDestroySemaphore(device, semaphore, default);
        }
    }

    protected unsafe override void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            isDisposed = true;

            ReleasePerFrame();

            if (swapChain.HasValue)
            {
                vkDestroySwapchainKhr(device, swapChain);
                swapChain = default;
            }

            if (surface.HasValue)
            {
                ThrowVulkanIfFailed(vkDestroySurfaceKhr(instance, surface, default),
                    "Failed to destroy surface.");
                surface = default;
            }
        }
    }
}

public unsafe class Gfx : IDisposable
{
    readonly Logger logger = Gossamer.Instance.Log.GetLogger(nameof(Gfx));

    bool isDisposed;

    PFN_vkDebugUtilsMessengerCallbackEXT? VulkanDebugMessengerCallback;
    PFN_vkSetDebugUtilsObjectNameEXT? VulkanDebugSetObjectName;

    PFN_vmaAllocateDeviceMemoryFunction? VmaAllocateDeviceMemoryFunction;
    PFN_vmaFreeDeviceMemoryFunction? VmaFreeDeviceMemoryFunction;

    VkDebugUtilsMessengerExt debugUtilsMessenger;

    VmaAllocator allocator;

    VkInstance instance;
    VkPhysicalDevice physicalDevice;
    VkDevice device;

    VkQueue deviceQueue;
    uint deviceQueueIndex;

    VkCommandPool deviceCommandPool;

    VkFormat deviceDepthFormat;
    VkSampleCount deviceSampleCount;

    readonly GfxApiParameters apiParameters;
    GfxParameters? parameters;

    GfxCapabilities capabilities = new(
        CanDebug: false,
        CanSwap: false
    );

    GfxPresenter? presenter;

    Gfx2D? gfx2D;

    readonly Dictionary<string, PipelineShaderProgram> cachedPipelineShaders = [];

    internal GfxSamples GetDeviceMaxSampleCount()
    {
        return (GfxSamples)deviceSampleCount;
    }

    internal GfxPresenter GetPresenter()
    {
        ThrowInvalidOperationIfNull(presenter, "No presenter available.");
        return presenter;
    }

    public Gfx(GfxApiParameters apiParameters)
    {
        this.apiParameters = apiParameters;

        CreateVulkanInstance();
    }

    readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public void Render()
    {
        AssertNotNull(presenter);
        AssertNotNull(gfx2D);

        bool canRender = presenter.BeginFrame();
        if (canRender)
        {
            gfx2D.BeginFrame();

            gfx2D.BeginBatch();
            //gfx2D.DrawRectangle(new(10, 10), new(100, 100), new Color(Color.MintyGreen, 1.0f));
            //gfx2D.DrawCircle(new(200, 200), 50, new Color(Color.White, 1.0f), 2);

            gfx2D.DrawRectangle(new(5, 0), new(5, 20), new Color(Color.MintyGreen, 1.0f));
            gfx2D.DrawRectangle(new(0, 5), new(20, 5), new Color(Color.MintyGreen, 1.0f));
            gfx2D.DrawText(stopwatch.ToString(), new(5, 5), new Color(Color.White, 1.0f), Color.UnpackRGB(0x000000));

            Vector2 textPosition = new(5, 100);
            Vector2 textAvailableSize = new(400, 200);

            {
                var textToTest = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
                //textToTest = "Word1 woORd2 longerword3 andword4 maybeevenlongerword5 word6";
                var textLayout = gfx2D.CreateTextLayout(textToTest,
                    textAvailableSize,
                    wordWrap: true);

                gfx2D.DrawText(textLayout, textPosition, new Color(Color.White, 1.0f), Color.UnpackRGB(0x000000));
                gfx2D.DrawRectangle(textPosition, textPosition + textAvailableSize, new Color(Color.HighlighterRed, 1.0f));
                gfx2D.DrawRectangle(textPosition, textPosition + textLayout.Size, new Color(Color.MintyGreen, 1.0f));

                gfx2D.DestroyTextLayout(textLayout);
            }

            //gfx2D.DrawText("t", new(300, 300), new Color(Color.White, 1.0f), Color.UnpackRGB(0x000000));
            gfx2D.EndBatch();

            gfx2D.EndFrame();

            presenter?.EndFrame();
        }
        else
        {
            //logger.Warning("Failed to render frame.");
        }
    }

    public void Create(GfxParameters parameters)
    {
        this.parameters = parameters;

        CreateVulkanDevice();
        CreateVulkanMemoryAllocator();
        CreateDeviceCommandPool();

        if (parameters.Presentation is GfxSwapChainPresentation swapChainPresentation)
        {
            var swapChainPresenter = new GfxSwapChainPresenter(instance, physicalDevice, device, deviceQueue, deviceQueueIndex, swapChainPresentation.Gui.CreateSurface(instance));
            presenter = swapChainPresenter;
            swapChainPresenter.Refresh(false);
        }

        LoadShaders("Jangine.shaders");

        gfx2D = new Gfx2D(this);
        gfx2D.Create();
        gfx2D.InitializeRendering(DisplayParameters.Empty);
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        GC.SuppressFinalize(this);
        isDisposed = true;

        if (device.HasValue)
        {
            vkDeviceWaitIdle(device);
        }

        if (deviceCommandPool.HasValue)
        {
            vkDestroyCommandPool(device, deviceCommandPool, default);
            deviceCommandPool = default;
        }

        if (gfx2D != null)
        {
            gfx2D.Dispose();
            gfx2D = null;
        }

        if (presenter != null)
        {
            presenter.Dispose();
            presenter = null;
        }

        if (allocator.HasValue)
        {
            vmaDestroyAllocator(allocator);
            allocator = default;
        }

        if (device.HasValue)
        {
            deviceQueue = default;

            vkDestroyDevice(device, default);
            device = default;
        }

        if (debugUtilsMessenger.HasValue)
        {
            var vkDestroyDebugUtilsMessengerEXT = (delegate*<VkInstance, VkDebugUtilsMessengerExt, nint, void>)vkGetInstanceProcAddr(instance, "vkDestroyDebugUtilsMessengerEXT");

            vkDestroyDebugUtilsMessengerEXT(instance, debugUtilsMessenger, default);
            debugUtilsMessenger = default;
        }

        if (instance.HasValue)
        {
            vkDestroyInstance(instance, default);
            instance = default;
        }
    }

    [Conditional("DEBUG")]
    internal void VulkanSetObjectName(VkObjectType type, ulong handle, string? name)
    {
        if (VulkanDebugSetObjectName == null || string.IsNullOrEmpty(name))
            return;

        nint objectNamePtr = Marshal.StringToHGlobalAnsi(name);

        DebugUtilsObjectNameInfoEXT debugUtilsObjectNameInfoEXT = new(default)
        {
            ObjectType = type,
            ObjectHandle = handle,
            ObjectName = objectNamePtr
        };

        VulkanDebugSetObjectName(device, &debugUtilsObjectNameInfoEXT);

        Marshal.FreeHGlobal(objectNamePtr);
    }

    uint VulkanDebugMessageCallback(VkDebugUtilsMessageSeverityExt severity, VkDebugUtilsMessageTypeExt type, VkDebugUtilsMessengerCallbackDataExt* pCallbackData, nint pUserData)
    {
        Log.Level level = severity switch
        {
            VkDebugUtilsMessageSeverityExt.VERBOSE => Log.Level.Debug,
            VkDebugUtilsMessageSeverityExt.INFO => Log.Level.Information,
            VkDebugUtilsMessageSeverityExt.WARNING => Log.Level.Warning,
            VkDebugUtilsMessageSeverityExt.ERROR => Log.Level.Error,
            _ => Log.Level.Debug
        };

        Gossamer.Instance.Log.Append(level, Utf8StringMarshaller.ConvertToManaged((byte*)pCallbackData->pMessage) ?? "", DateTime.Now, "Vulkan", "Validation");
        return 0;
    }

    void VmaAllocateDeviceMemory(VmaAllocator allocator, uint memoryType, VkDeviceMemory memory, ulong size, nint pUserData)
    {
        Gossamer.Instance.Log.Append(Log.Level.Debug, $"{StringUtilities.ByteSizeShortIEC(size)}", DateTime.Now, "Vma", "Allocate");
    }

    void VmaFreeDeviceMemory(VmaAllocator allocator, uint memoryType, VkDeviceMemory memory, ulong size, nint pUserData)
    {
        Gossamer.Instance.Log.Append(Log.Level.Debug, $"{StringUtilities.ByteSizeShortIEC(size)}", DateTime.Now, "Vma", "Free");
    }

    internal GfxSingleCommand BeginSingleCommand()
    {
        VkCommandBufferAllocateInfo commandBufferAllocateInfo = new(default)
        {
            Level = VkCommandBufferLevel.PRIMARY,
            Pool = deviceCommandPool,
            Count = 1
        };

        VkCommandBuffer commandBuffer;
        ThrowVulkanIfFailed(vkAllocateCommandBuffers(device, &commandBufferAllocateInfo, &commandBuffer));

        VkCommandBufferBeginInfo commandBufferBeginInfo = new(default)
        {
            Flags = VkCommandBufferUsageFlags.ONE_TIME_SUBMIT_BIT
        };

        ThrowVulkanIfFailed(vkBeginCommandBuffer(commandBuffer, &commandBufferBeginInfo));

        VkFenceCreateInfo fenceCreateInfo = new(default);
        VkFence fence;
        ThrowVulkanIfFailed(vkCreateFence(device, &fenceCreateInfo, default, &fence));

        return new GfxSingleCommand(commandBuffer, fence);
    }

    internal void SubmitSingleCommand(GfxSingleCommand singleCommand)
    {
        VkFence fence = singleCommand.Fence;
        VkCommandBuffer commandBuffer = singleCommand.CommandBuffer;

        ThrowVulkanIfFailed(vkEndCommandBuffer(commandBuffer));

        VkSubmitInfo submitInfo = new(default)
        {
            CommandBufferCount = 1,
            CommandBuffers = &commandBuffer,
        };

        ThrowVulkanIfFailed(vkQueueSubmit(deviceQueue, 1, &submitInfo, fence));
    }

    internal void EndSingleCommand(GfxSingleCommand singleCommand)
    {
        VkFence fence = singleCommand.Fence;
        VkCommandBuffer commandBuffer = singleCommand.CommandBuffer;

        ThrowVulkanIfFailed(vkWaitForFences(device, 1, &fence, 1, ulong.MaxValue));
        vkFreeCommandBuffers(device, deviceCommandPool, 1, &commandBuffer);
        vkDestroyFence(device, fence, default);
    }

    internal void Barrier(VkCommandBuffer commandBuffer, VkPipelineStage2 srcStage, VkPipelineStage2 dstStage)
    {
        VkMemoryBarrier2 memoryBarrier = new(default)
        {
            SrcAccessMask = VkAccessFlags2.NONE,
            DstAccessMask = VkAccessFlags2.NONE,
            SrcStageMask = srcStage,
            DstStageMask = dstStage
        };

        VkDependencyInfo dependencyInfo = new(default)
        {
            MemoryBarrierCount = 1,
            MemoryBarriers = &memoryBarrier
        };

        vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);
    }

    internal void FIXME_OmegaBarrier(VkCommandBuffer commandBuffer)
    {
        VkMemoryBarrier2 memoryBarrier = new(default)
        {
            SrcAccessMask = VkAccessFlags2.NONE,
            DstAccessMask = VkAccessFlags2.NONE,
            SrcStageMask = VkPipelineStage2.ALL_COMMANDS_BIT,
            DstStageMask = VkPipelineStage2.ALL_COMMANDS_BIT,
        };

        VkDependencyInfo dependencyInfo = new(default)
        {
            MemoryBarrierCount = 1,
            MemoryBarriers = &memoryBarrier,
        };

        vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);
    }

    internal void PixelBufferBarrier(VkCommandBuffer commandBuffer, PixelBuffer pixelBuffer, VkImageLayout srcLayout, VkImageLayout dstLayout)
    {
        static VkAccessFlags2 GetAccessFlags(VkImageLayout layout)
        {
            switch (layout)
            {
                case VkImageLayout.UNDEFINED:
                case VkImageLayout.PRESENT_SRC_KHR:
                    return 0;
                case VkImageLayout.PREINITIALIZED:
                    return VkAccessFlags2.HOST_WRITE_BIT;
                case VkImageLayout.COLOR_ATTACHMENT_OPTIMAL:
                    return VkAccessFlags2.COLOR_ATTACHMENT_READ_BIT | VkAccessFlags2.COLOR_ATTACHMENT_WRITE_BIT;
                case VkImageLayout.DEPTH_ATTACHMENT_OPTIMAL:
                    return VkAccessFlags2.DEPTH_STENCIL_ATTACHMENT_READ_BIT | VkAccessFlags2.DEPTH_STENCIL_ATTACHMENT_WRITE_BIT;
                case VkImageLayout.SHADER_READ_ONLY_OPTIMAL:
                    return VkAccessFlags2.SHADER_READ_BIT | VkAccessFlags2.INPUT_ATTACHMENT_READ_BIT;
                case VkImageLayout.TRANSFER_SRC_OPTIMAL:
                    return VkAccessFlags2.TRANSFER_READ_BIT;
                case VkImageLayout.TRANSFER_DST_OPTIMAL:
                    return VkAccessFlags2.TRANSFER_WRITE_BIT;
                default:
                    throw new InvalidOperationException($"Unsupported image layout: {layout}");
            }
        }

        static VkPipelineStage2 GetPipelineStageFlags(VkImageLayout layout)
        {
            switch (layout)
            {
                case VkImageLayout.UNDEFINED:
                    return VkPipelineStage2.TOP_OF_PIPE;
                case VkImageLayout.PREINITIALIZED:
                    return VkPipelineStage2.HOST_BIT;
                case VkImageLayout.TRANSFER_DST_OPTIMAL:
                case VkImageLayout.TRANSFER_SRC_OPTIMAL:
                    return VkPipelineStage2.ALL_TRANSFER;
                case VkImageLayout.COLOR_ATTACHMENT_OPTIMAL:
                    return VkPipelineStage2.COLOR_ATTACHMENT_OUTPUT;
                case VkImageLayout.DEPTH_ATTACHMENT_OPTIMAL:
                    return VkPipelineStage2.EARLY_FRAGMENT_TESTS_BIT | VkPipelineStage2.LATE_FRAGMENT_TESTS_BIT;
                case VkImageLayout.SHADER_READ_ONLY_OPTIMAL:
                    return VkPipelineStage2.VERTEX_SHADER_BIT | VkPipelineStage2.FRAGMENT_SHADER;
                case VkImageLayout.PRESENT_SRC_KHR:
                    return VkPipelineStage2.BOTTOM_OF_PIPE;
                default:
                    throw new InvalidOperationException($"Unsupported image layout: {layout}");
            }
        }

        VkImageMemoryBarrier2 imageMemoryBarrier = new(default)
        {
            SrcAccessMask = GetAccessFlags(srcLayout),
            DstAccessMask = GetAccessFlags(dstLayout),
            SrcStageMask = GetPipelineStageFlags(srcLayout),
            DstStageMask = GetPipelineStageFlags(dstLayout),
            OldLayout = srcLayout,
            NewLayout = dstLayout,
            SrcQueueFamilyIndex = Constants.VK_QUEUE_FAMILY_IGNORED,
            DstQueueFamilyIndex = Constants.VK_QUEUE_FAMILY_IGNORED,
            Image = pixelBuffer.Image,
            SubresourceRange = new VkImageSubresourceRange
            {
                AspectMask = (VkImageAspect)pixelBuffer.Aspect,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = pixelBuffer.Layers
            }
        };

        VkDependencyInfo dependencyInfo = new(default)
        {
            ImageMemoryBarrierCount = 1,
            ImageMemoryBarriers = &imageMemoryBarrier
        };

        vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);
    }

    record class PipelineShaderProgramStage(VkShaderStage Stage, SafeNativeString Entrypoint, byte[] Code);

    class PipelineShaderProgram(string name, PipelineShaderProgramStage[] stages)
    {
        public string Name { get; } = name;
        public PipelineShaderProgramStage[] Stages { get; } = stages;
    }

    record class ShaderStageDefinition(uint Stage, string EntryPoint, long Offset, long Size);
    record class ShaderProgramDefinition(string Name, ShaderStageDefinition[] Stages);
    record class ShaderPackageDefinition(Dictionary<string, ShaderProgramDefinition> Pipelines);

    void UpdateDynamicBuffer<T>(MemoryBuffer<T> memoryBuffer, void* pSrc, int srcSize) where T : unmanaged
    {
        int dstSize = (int)memoryBuffer.Length * sizeof(T);

        ThrowInvalidOperationIf(srcSize > dstSize, "Data size exceeds buffer size.");

        void* pDst = null;
        ThrowVulkanIfFailed(vmaMapMemory(allocator, memoryBuffer.Allocation, &pDst));

        Unsafe.CopyBlock(pDst, pSrc, (uint)srcSize);

        vmaUnmapMemory(allocator, memoryBuffer.Allocation);
        ThrowVulkanIfFailed(vmaFlushAllocation(allocator, memoryBuffer.Allocation, 0, (ulong)srcSize));
    }

    internal void UpdateDynamicBuffer<T>(MemoryBuffer<T> memoryBuffer, T data) where T : unmanaged
    {
        UpdateDynamicBuffer(memoryBuffer, &data, sizeof(T));
    }

    internal void UpdateDynamicBuffer<T>(MemoryBuffer<T> memoryBuffer, T[] data, int dataCount = -1) where T : unmanaged
    {
        fixed (void* pData = data)
        {
            dataCount = dataCount < 0 ? data.Length : dataCount;
            int srcSize = data.Length * sizeof(T);
            UpdateDynamicBuffer(memoryBuffer, pData, srcSize);
        }
    }

    /// <summary>
    /// Destroys a memory buffer. Safe to call with null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="memoryBuffer"></param>
    internal void DestroyMemoryBuffer<T>(MemoryBuffer<T>? memoryBuffer)
    {
        if (memoryBuffer == null)
        {
            return;
        }

        ThrowVulkanIfFailed(vmaDestroyBuffer(allocator, memoryBuffer.Buffer, memoryBuffer.Allocation));
    }

    internal MemoryBuffer<T> CreateDynamicMemoryBuffer<T>(int length, GfxMemoryBufferUsage usage)
    {
        VkBufferCreateInfo bufferCreateInfo = new(default)
        {
            Size = (ulong)(Marshal.SizeOf<T>() * length),
            Usage = (VkBufferUsage)usage,
        };

        VmaAllocationCreateInfo allocationCreateInfo = new()
        {
            Usage = VmaMemoryUsage.AUTO,
            Flags = VmaAllocationCreateFlags.HOST_ACCESS_SEQUENTIAL_WRITE,
        };

        VkBuffer buffer;
        VmaAllocation allocation;
        VmaAllocationInfo allocationInfo;
        ThrowVulkanIfFailed(vmaCreateBuffer(allocator, &bufferCreateInfo, &allocationCreateInfo, &buffer, &allocation, &allocationInfo));

        VkMemoryProperty memoryProperties;
        vmaGetMemoryTypeProperties(allocator, allocationInfo.MemoryType, &memoryProperties);

        logger.Debug($"{usage}, [{StringUtilities.ByteSizeShortIEC(allocationInfo.Size)}] [{memoryProperties}]");

        return new MemoryBuffer<T>(length: (uint)length, buffer, allocation);
    }

    internal void DestroySampler(VkSampler sampler)
    {
        if (sampler.HasValue)
        {
            vkDestroySampler(device, sampler, default);
        }
    }

    internal VkSampler CreateSampler(VkSamplerCreateInfo samplerCreateInfo)
    {
        VkSampler sampler;
        ThrowVulkanIfFailed(vkCreateSampler(device, &samplerCreateInfo, default, &sampler));
        return sampler;
    }

    [Conditional("DEBUG")]
    internal void AssingName(PixelBuffer pixelBuffer, string name)
    {
        VulkanSetObjectName(VkObjectType.IMAGE, pixelBuffer.Image.Value, name);
    }

    /// <summary>
    /// Destroys a pixel buffer. Safe to call with null.
    /// </summary>
    /// <param name="pixelBuffer"></param>
    internal void DestroyPixelBuffer(PixelBuffer? pixelBuffer)
    {
        if (pixelBuffer == null)
        {
            return;
        }

        vkDestroyImageView(device, pixelBuffer.View, default);
        ThrowVulkanIfFailed(vmaDestroyImage(allocator, pixelBuffer.Image, pixelBuffer.Allocation));
    }

    internal PixelBuffer CreatePixelBuffer(
        uint width,
        uint height,
        GfxFormat format,
        GfxPixelBufferUsage usage,
        GfxAspect aspect,
        GfxSamples samples)
    {
        VkImageCreateInfo imageCreateInfo = new(default)
        {
            ImageType = VkImageType.TYPE_2D,
            Format = (VkFormat)format,
            Extent = new VkExtent3D
            {
                Width = width,
                Height = height,
                Depth = 1
            },
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = (VkSampleCount)samples,
            Tiling = VkImageTiling.OPTIMAL,
            Usage = (VkImageUsage)usage,
        };

        VmaAllocationCreateInfo allocationCreateInfo = new()
        {
            Usage = VmaMemoryUsage.AUTO,
        };

        VkImage image;
        VmaAllocation allocation;
        VmaAllocationInfo allocationInfo;
        ThrowVulkanIfFailed(vmaCreateImage(allocator, &imageCreateInfo, &allocationCreateInfo, &image, &allocation, &allocationInfo));

        VkMemoryProperty memoryProperties;
        vmaGetMemoryTypeProperties(allocator, allocationInfo.MemoryType, &memoryProperties);

        logger.Debug($"{format} {width}x{height} [{StringUtilities.ByteSizeShortIEC(allocationInfo.Size)}] [{memoryProperties}]");

        VkImageViewCreateInfo imageViewCreateInfo = new(default)
        {
            ViewType = VkImageViewType.TYPE_2D,
            Image = image,
            Format = (VkFormat)format,
            SubresourceRange = new VkImageSubresourceRange
            {
                LevelCount = 1,
                BaseMipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
                AspectMask = (VkImageAspect)aspect
            }
        };

        VkImageView view;
        ThrowVulkanIfFailed(vkCreateImageView(device, &imageViewCreateInfo, default, &view));

        return new PixelBuffer(format, aspect, samples, width, height, 1, image, view, allocation);
    }

    internal VkDescriptorSetLayout CreateDescriptorLayout(VkDescriptorSetLayoutBinding[] bindings)
    {
        fixed (VkDescriptorSetLayoutBinding* pBindings = bindings)
        {
            // Some asserts to make sure the bindings are valid.
            // Hardly any error checking on these in the validation layers.
            Assert(bindings.Length > 0);
            for (int i = 0; i < bindings.Length; i++)
            {
                Assert(bindings[i].DescriptorCount > 0);
            }

            VkDescriptorSetLayoutCreateInfo layoutInfo = new(default)
            {
                Flags = VkDescriptorSetLayoutCreateFlags.PUSH_DESCRIPTOR_BIT_KHR,
                BindingCount = (uint)bindings.Length,
                Bindings = pBindings
            };

            VkDescriptorSetLayout pDescriptorSetLayout = default;
            ThrowVulkanIfFailed(vkCreateDescriptorSetLayout(device, &layoutInfo, default, &pDescriptorSetLayout),
                "Failed to create descriptor set layout.");

            return pDescriptorSetLayout;
        }
    }

    internal VkPipelineLayout CreatePipelineLayout(VkDescriptorSetLayout[] layouts, VkPushConstantRange[] pushConstantRanges)
    {
        fixed (VkDescriptorSetLayout* pLayouts = layouts)
        fixed (VkPushConstantRange* pPushConstantRanges = pushConstantRanges)
        {
            VkPipelineLayoutCreateInfo pipelineLayoutCreateInfo = new(default)
            {
                SetLayoutCount = (uint)layouts.Length,
                SetLayouts = pLayouts,
                PushConstantRangeCount = (uint)pushConstantRanges.Length,
                PushConstantRanges = pPushConstantRanges
            };

            VkPipelineLayout pPipelineLayout = default;
            ThrowVulkanIfFailed(vkCreatePipelineLayout(device, &pipelineLayoutCreateInfo, default, &pPipelineLayout),
                "Failed to create pipeline layout.");

            return pPipelineLayout;
        }
    }

    internal void LoadShaders(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream);

        // Json chunk
        uint jsonChunkLength = reader.ReadUInt32();
        uint jsonChunkType = reader.ReadUInt32();
        ThrowInvalidDataIf(jsonChunkType != 1, "Invalid json chunk type.");

        byte[] jsonChunkData = reader.ReadBytes((int)jsonChunkLength);

        // Bytecode chunk
        uint bytecodeChunkLength = reader.ReadUInt32();
        uint bytecodeChunkType = reader.ReadUInt32();
        ThrowInvalidOperationIf(bytecodeChunkType != 2, "Invalid bytecode chunk type.");

        ShaderPackageDefinition packageDefinition = System.Text.Json.JsonSerializer.Deserialize<ShaderPackageDefinition>(System.Text.Encoding.UTF8.GetString(jsonChunkData)) ?? throw new InvalidDataException();
        byte[] packageBytecode = reader.ReadBytes((int)bytecodeChunkLength);

        foreach (var (shaderProgramName, shaderProgramDefinition) in packageDefinition.Pipelines)
        {
            PipelineShaderProgramStage[] stages = new PipelineShaderProgramStage[shaderProgramDefinition.Stages.Length];

            for (int i = 0; i < shaderProgramDefinition.Stages.Length; i++)
            {
                ShaderStageDefinition stageDefinition = shaderProgramDefinition.Stages[i];
                byte[] stageBytecode = new byte[stageDefinition.Size];
                Array.Copy(packageBytecode, stageDefinition.Offset, stageBytecode, 0, stageDefinition.Size);

                stages[i] = new PipelineShaderProgramStage((VkShaderStage)stageDefinition.Stage, new SafeNativeString(stageDefinition.EntryPoint), stageBytecode);
            }

            cachedPipelineShaders[shaderProgramName] = new PipelineShaderProgram(shaderProgramName, stages);
        }
    }

    /// <summary>
    /// Destroys a pipeline. Safe to call with null.
    /// </summary>
    /// <param name="pipeline"></param>
    internal void DestroyPipeline(GfxPipeline? pipeline)
    {
        if (pipeline == null)
        {
            return;
        }

        if (pipeline.Pipeline.HasValue)
        {
            vkDestroyPipeline(device, pipeline.Pipeline, default);
        }
        if (pipeline.Layout.HasValue)
        {
            vkDestroyPipelineLayout(device, pipeline.Layout, default);
        }
        if (pipeline.DescriptorLayout.HasValue)
        {
            vkDestroyDescriptorSetLayout(device, pipeline.DescriptorLayout, default);
        }
    }

    internal GfxPipeline CreatePipeline(GfxPipelineParameters parameters)
    {
        _ = cachedPipelineShaders.TryGetValue(parameters.ShaderProgram, out PipelineShaderProgram? pipelineShaderProgram);
        ThrowInvalidOperationIfNull(pipelineShaderProgram, $"Shaders program '{parameters.ShaderProgram}' not found.");

        VkDescriptorSetLayout descriptorSetLayout = CreateDescriptorLayout(parameters.Layout);
        VkPipelineLayout pipelineLayout = CreatePipelineLayout([descriptorSetLayout], parameters.PushConstants);

        VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new(default)
        {
            Topology = parameters.InputTopology,
        };
        VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new(default)
        {
            PolygonMode = VkPolygonMode.FILL,
            CullMode = parameters.CullMode,
            FrontFace = parameters.FrontFace,
            LineWidth = 1.0f
        };
        VkPipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new(default)
        {
            DepthTestEnable = parameters.DepthTest ? 1u : 0u,
            DepthWriteEnable = parameters.DepthWrite ? 1u : 0u,
            DepthCompareOp = parameters.DepthCompareOp,
            DepthBoundsTestEnable = 0,
            StencilTestEnable = 0
        };
        VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new(default)
        {
            ViewportCount = 1,
            ScissorCount = 1
        };
        VkDynamicState* dynamicStates = stackalloc VkDynamicState[2]
        {
            VkDynamicState.VIEWPORT,
            VkDynamicState.SCISSOR
        };
        VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = new(default)
        {
            DynamicStateCount = 2,
            DynamicStates = dynamicStates
        };

        VkPipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new(default)
        {
            RasterizationSamples = VkSampleCount.COUNT_1
        };

        if (parameters.Multisampling)
        {
            multisampleStateCreateInfo.RasterizationSamples = deviceSampleCount;
            multisampleStateCreateInfo.SampleShadingEnable = 1;
            multisampleStateCreateInfo.MinSampleShading = 1.0f;
        }

        int vertexInputBindingsCount = parameters.InputBindings.Length;
        VkVertexInputBindingDescription* vertexInputBindings = stackalloc VkVertexInputBindingDescription[vertexInputBindingsCount];
        for (uint i = 0; i < vertexInputBindingsCount; i++)
        {
            Assert(parameters.InputBindings[i].Stride > 0);
            vertexInputBindings[i] = parameters.InputBindings[i];
        }

        int vertexInputAttibutesCount = parameters.InputAttributes.Length;
        VkVertexInputAttributeDescription* vertexInputAttributes = stackalloc VkVertexInputAttributeDescription[vertexInputAttibutesCount];
        for (uint i = 0; i < vertexInputAttibutesCount; i++)
        {
            Assert(parameters.InputAttributes[i].Format != VkFormat.UNDEFINED);
            vertexInputAttributes[i] = parameters.InputAttributes[i];
        }

        VkPipelineVertexInputStateCreateInfo vertexInputInfo = new(default)
        {
            VertexBindingDescriptionCount = (uint)vertexInputBindingsCount,
            VertexAttributeDescriptionCount = (uint)vertexInputAttibutesCount,
            VertexBindingDescriptions = vertexInputBindings,
            VertexAttributeDescriptions = vertexInputAttributes
        };

        int attachmentCount = parameters.Attachments.Length;
        VkFormat* colorAttachmentFormats = stackalloc VkFormat[attachmentCount];
        VkPipelineColorBlendAttachmentState* colorAttachmentBlends = stackalloc VkPipelineColorBlendAttachmentState[attachmentCount];
        for (int i = 0; i < attachmentCount; i++)
        {
            colorAttachmentFormats[i] = parameters.Attachments[i].Format;
            colorAttachmentBlends[i] = parameters.Attachments[i].Blend;
        }

        VkPipelineRenderingCreateInfo pipelineRendering = new(default)
        {
            ColorAttachmentCount = (uint)attachmentCount,
            ColorAttachmentFormats = colorAttachmentFormats,
            DepthAttachmentFormat = (parameters.DepthTest || parameters.DepthWrite) ? deviceDepthFormat : VkFormat.UNDEFINED
        };

        VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new(default)
        {
            AttachmentCount = (uint)attachmentCount,
            Attachments = colorAttachmentBlends
        };

        int shaderStageCount = pipelineShaderProgram.Stages.Length;
        VkPipelineShaderStageCreateInfo* shaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[shaderStageCount];
        for (int i = 0; i < shaderStageCount; i++)
        {
            PipelineShaderProgramStage stage = pipelineShaderProgram.Stages[i];

            VkShaderModule shaderModule;
            fixed (void* pCode = stage.Code)
            {
                VkShaderModuleCreateInfo shaderModuleCreateInfo = new(default)
                {
                    CodeSize = (nuint)stage.Code.Length,
                    Code = (nint)pCode,
                };

                ThrowVulkanIfFailed(vkCreateShaderModule(device, &shaderModuleCreateInfo, default, &shaderModule));
            }

            shaderStageCreateInfos[i] = new(default)
            {
                Stage = stage.Stage,
                Module = shaderModule,
                Name = stage.Entrypoint.DangerousGetHandle()
            };
        }

        VkGraphicsPipelineCreateInfo pipelineInfo = new(next: Next(&pipelineRendering))
        {
            Layout = pipelineLayout,
            StageCount = (uint)shaderStageCount,
            Stages = shaderStageCreateInfos,
            VertexInputState = &vertexInputInfo,
            InputAssemblyState = &inputAssemblyStateCreateInfo,
            ViewportState = &viewportStateCreateInfo,
            RasterizationState = &rasterizationStateCreateInfo,
            MultisampleState = &multisampleStateCreateInfo,
            DepthStencilState = &depthStencilStateCreateInfo,
            ColorBlendState = &colorBlendStateCreateInfo,
            DynamicState = &dynamicStateCreateInfo,
        };

        VkPipeline pPipeline = default;
        VkResult result = vkCreateGraphicsPipelines(device, default, 1, &pipelineInfo, default, &pPipeline);

        // Shader modules are no longer needed after the pipeline has been created
        for (int i = 0; i < shaderStageCount; i++)
        {
            vkDestroyShaderModule(device, shaderStageCreateInfos[i].Module, default);
        }

        ThrowVulkanIfFailed(result, "Failed to create graphics pipeline.");

        return new GfxPipeline(pPipeline, pipelineLayout, descriptorSetLayout);
    }

    void CreateVulkanInstance()
    {
        HashSet<string> availableInstanceLayers = [];
        HashSet<string> availableInstanceExtensions = [];

        uint availableApiVersionRaw = 0;
        ThrowVulkanIfFailed(vkEnumerateInstanceVersion(&availableApiVersionRaw),
            "Failed to get instance version.");

        Version availableApiVersion = ParseVersion(availableApiVersionRaw);

        // Check if the available version is compatible with the required version
        {
            Version requiredApiVersion = new(1, 3, 0);
            ThrowNotSupportedIf(availableApiVersion < requiredApiVersion,
                $"Required API version {requiredApiVersion} not available.");
        }

        // Get available instance layers
        {
            var instanceLayerCount = 0u;
            ThrowVulkanIfFailed(vkEnumerateInstanceLayerProperties(&instanceLayerCount, default),
                "Failed to get instance layer count.");

            var layerProperties = stackalloc VkLayerProperties[(int)instanceLayerCount];
            ThrowVulkanIfFailed(vkEnumerateInstanceLayerProperties(&instanceLayerCount, layerProperties),
                "Failed to get instance layers.");

            for (int i = 0; i < instanceLayerCount; i++)
            {
                availableInstanceLayers.Add(new string((sbyte*)layerProperties[i].LayerName));
            }
        }

        // Get available instance extensions
        {
            var instanceExtensionCount = 0u;
            ThrowVulkanIfFailed(vkEnumerateInstanceExtensionProperties(default, &instanceExtensionCount),
                "Failed to get instance extension count.");

            var extensionProperties = stackalloc VkExtensionProperties[(int)instanceExtensionCount];
            ThrowVulkanIfFailed(vkEnumerateInstanceExtensionProperties(default, &instanceExtensionCount, extensionProperties),
                "Failed to get instance extensions.");

            for (int i = 0; i < instanceExtensionCount; i++)
            {
                availableInstanceExtensions.Add(new string((sbyte*)extensionProperties[i].ExtensionName));
            }
        }

        SafeNativeStringArray enabledLayerNames = new(capacity: 8);
        SafeNativeStringArray enabledExtensionNames = new(capacity: 8);

        // Enable required instance layers and extensions
        {
            // VK_KHR_surface should always be available but let's check anyway
            const string VK_KHR_surface = "VK_KHR_surface";
            ThrowNotSupportedIf(!availableInstanceExtensions.Contains(VK_KHR_surface),
                $"Required feature {VK_KHR_surface} not available.");

            enabledExtensionNames.Add(VK_KHR_surface);

            if (apiParameters.PresentationMode == GfxPresentationMode.SwapChain)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    const string VK_KHR_win32_surface = "VK_KHR_win32_surface";

                    if (availableInstanceExtensions.Contains(VK_KHR_win32_surface))
                    {
                        enabledExtensionNames.Add(VK_KHR_win32_surface);
                        capabilities = capabilities with { CanSwap = true };
                    }
                    else
                    {
                        logger.Warning($"Swapchain is enabled but {VK_KHR_win32_surface} is not available.");
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    const string VK_KHR_xcb_surface = "VK_KHR_xcb_surface";

                    if (availableInstanceExtensions.Contains(VK_KHR_xcb_surface))
                    {
                        enabledExtensionNames.Add(VK_KHR_xcb_surface);
                        capabilities = capabilities with { CanSwap = true };
                    }
                    else
                    {
                        logger.Warning($"Swapchain is enabled but {VK_KHR_xcb_surface} is not available.");
                    }
                }
                else
                {
                    logger.Warning("Swapchain is enabled but platform is not supported for it.");
                }
            }

            if (apiParameters.EnableDebugging)
            {
                const string VK_LAYER_KHRONOS_validation = "VK_LAYER_KHRONOS_validation";
                if (availableInstanceLayers.Contains(VK_LAYER_KHRONOS_validation))
                {
                    enabledLayerNames.Add(VK_LAYER_KHRONOS_validation);
                    capabilities = capabilities with { CanDebug = true };
                }
                else
                {
                    logger.Warning($"Validation is enabled but {VK_LAYER_KHRONOS_validation} is not available.");
                }

                const string VK_EXT_debug_utils = "VK_EXT_debug_utils";
                if (availableInstanceExtensions.Contains(VK_EXT_debug_utils))
                {
                    enabledExtensionNames.Add(VK_EXT_debug_utils);
                    capabilities = capabilities with { CanDebug = true };
                }
                else
                {
                    logger.Warning($"Debugging is enabled but {VK_EXT_debug_utils} is not available.");
                }
            }
        }

        Gossamer.ApplicationInfo engineInfo = Gossamer.ApplicationInfo.FromCallingAssembly();

        using SafeNativeString applicationName = new(apiParameters.AppInfo.Name);
        using SafeNativeString engineName = new(engineInfo.Name);

        VkApplicationInfo applicationInfo = new(default)
        {
            ApiVersion = availableApiVersionRaw,

            ApplicationName = applicationName.DangerousGetHandle(),
            ApplicationVersion = MakeApiVersion(0, apiParameters.AppInfo.Version.Major, apiParameters.AppInfo.Version.Minor, apiParameters.AppInfo.Version.Build),

            EngineName = engineName.DangerousGetHandle(),
            EngineVersion = MakeApiVersion(0, engineInfo.Version.Major, engineInfo.Version.Minor, engineInfo.Version.Build),
        };

        VkInstanceCreateInfo instanceCreateInfo = new(default)
        {
            ApplicationInfo = &applicationInfo,

            EnabledExtensionCount = (uint)enabledExtensionNames.Count,
            EnabledExtensionNames = enabledExtensionNames.DangerousGetHandle(),

            EnabledLayerCount = (uint)enabledLayerNames.Count,
            EnabledLayerNames = enabledLayerNames.DangerousGetHandle(),
        };

        VkInstance instance = default;
        ThrowVulkanIfFailed(vkCreateInstance(&instanceCreateInfo, default, &instance), "Failed to create instance.");
        this.instance = instance;

        if (apiParameters.EnableDebugging && capabilities.CanDebug)
        {
            const string STR_vkCreateDebugUtilsMessengerEXT = "vkCreateDebugUtilsMessengerEXT";
            const string STR_vkDestroyDebugUtilsMessengerEXT = "vkDestroyDebugUtilsMessengerEXT";
            const string STR_vkSetDebugUtilsObjectNameEXT = "vkSetDebugUtilsObjectNameEXT";

            nint debugCreateUtilsMessengerPfn = vkGetInstanceProcAddr(instance, STR_vkCreateDebugUtilsMessengerEXT);
            nint debugDestroyUtilsMessengerPfn = vkGetInstanceProcAddr(instance, STR_vkDestroyDebugUtilsMessengerEXT);
            nint debugUtilsObjectNamePfn = vkGetInstanceProcAddr(instance, STR_vkSetDebugUtilsObjectNameEXT);

            bool debugFunctionsOk = debugCreateUtilsMessengerPfn != nint.Zero && debugDestroyUtilsMessengerPfn != nint.Zero && debugUtilsObjectNamePfn != nint.Zero;
            if (debugFunctionsOk)
            {
                VulkanDebugMessengerCallback = VulkanDebugMessageCallback;
                VulkanDebugSetObjectName = Marshal.GetDelegateForFunctionPointer<PFN_vkSetDebugUtilsObjectNameEXT>(debugUtilsObjectNamePfn);

                VkDebugUtilsMessengerCreateInfoExt debugUtilsMessengerCreateInfo = new(default)
                {
                    MessageSeverity = VkDebugUtilsMessageSeverityExt.ERROR | VkDebugUtilsMessageSeverityExt.WARNING,
                    MessageType = VkDebugUtilsMessageTypeExt.GENERAL | VkDebugUtilsMessageTypeExt.VALIDATION,
                    UserCallback = Marshal.GetFunctionPointerForDelegate(VulkanDebugMessengerCallback),
                };

                var vkCreateDebugUtilsMessengerEXT = (delegate*<VkInstance, VkDebugUtilsMessengerCreateInfoExt*, nint, VkDebugUtilsMessengerExt*, VkResult>)debugCreateUtilsMessengerPfn;

                VkDebugUtilsMessengerExt debugUtilsMessenger = default;
                ThrowVulkanIfFailed(vkCreateDebugUtilsMessengerEXT(instance, &debugUtilsMessengerCreateInfo, default, &debugUtilsMessenger), "Failed to create debug utils messenger.");
                this.debugUtilsMessenger = debugUtilsMessenger;
            }
            else
            {
                logger.Warning("Debugging is enabled but some required functions are not available.");
                capabilities = capabilities with { CanDebug = false };
            }
        }
    }

    void CreateVulkanDevice()
    {
        AssertNotNull(parameters);

        VkPhysicalDevice physicalDevice = GetVulkanPhysicalDevice(parameters.PhysicalDevice);
        this.physicalDevice = physicalDevice;

        VkPhysicalDeviceProperties physicalDeviceProperties;
        vkGetPhysicalDeviceProperties(physicalDevice, &physicalDeviceProperties);

        HashSet<string> availableDeviceExtensions = [];

        // Get available device extensions
        {
            var deviceExtensionCount = 0u;
            ThrowVulkanIfFailed(vkEnumerateDeviceExtensionProperties(physicalDevice, default, &deviceExtensionCount, default),
                "Failed to get device extension count.");

            var extensionProperties = stackalloc VkExtensionProperties[(int)deviceExtensionCount];
            ThrowVulkanIfFailed(vkEnumerateDeviceExtensionProperties(physicalDevice, default, &deviceExtensionCount, extensionProperties),
                "Failed to get device extensions.");

            for (int i = 0; i < deviceExtensionCount; i++)
            {
                availableDeviceExtensions.Add(new string((sbyte*)extensionProperties[i].ExtensionName));
            }
        }

        SafeNativeStringArray enabledDeviceExtensionNames = new(capacity: 16);

        // Enable required device extensions
        {
            const string VK_KHR_swapchain = "VK_KHR_swapchain";
            //const string VK_KHR_dynamic_rendering = "VK_KHR_dynamic_rendering";
            const string VK_KHR_external_memory_win32 = "VK_KHR_external_memory_win32";
            const string VK_EXT_memory_budget = "VK_EXT_memory_budget";
            //const string VK_EXT_subgroup_size_control = "VK_EXT_subgroup_size_control";
            const string VK_EXT_robustness2 = "VK_EXT_robustness2";
            const string VK_KHR_push_descriptor = "VK_KHR_push_descriptor";

            void AddExtension(string extension)
            {
                if (availableDeviceExtensions.Contains(extension))
                {
                    enabledDeviceExtensionNames.Add(extension);
                }
                else
                {
                    logger.Warning($"Required extension {extension} is not available.");
                }
            }

            AddExtension(VK_KHR_swapchain);
            //AddExtension(VK_KHR_dynamic_rendering);
            AddExtension(VK_EXT_memory_budget);
            //AddExtension(VK_EXT_subgroup_size_control);
            AddExtension(VK_EXT_robustness2);
            AddExtension(VK_KHR_push_descriptor);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AddExtension(VK_KHR_external_memory_win32);
            }
        }

        // Probe for required features
        VkPhysicalDeviceSynchronization2Features physicalDeviceSynchronization2Features = new(next: default);
        VkPhysicalDeviceRobustness2FeaturesEXT physicalDeviceRobustness2Features = new(next: Next(&physicalDeviceSynchronization2Features));
        VkPhysicalDeviceShaderSubgroupExtendedTypesFeatures physicalDeviceSubgroupExtendedTypesFeatures = new(next: Next(&physicalDeviceRobustness2Features));
        VkPhysicalDevice16BitStorageFeatures physicalDeviceFloat16StorageFeatures = new(next: Next(&physicalDeviceSubgroupExtendedTypesFeatures));
        VkPhysicalDeviceShaderFloat16Int8Features physicalDeviceFloat16ShaderFeatures = new(next: Next(&physicalDeviceFloat16StorageFeatures));
        VkPhysicalDeviceDynamicRenderingFeatures physicalDeviceDynamicRenderingFeatures = new(next: Next(&physicalDeviceFloat16ShaderFeatures));

        VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new(next: Next(&physicalDeviceDynamicRenderingFeatures));
        vkGetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

        // Fail if required features are not supported
        {
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.SamplerAnisotropy == 0, "Required feature anisotropic filtering is not supported.");
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.GeometryShader == 0, "Required feature geometry shader is not supported.");
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.DrawIndirectFirstInstance == 0, "Required feature draw indirect first instance is not supported.");
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.FragmentStoresAndAtomics == 0, "Required feature fragment stores and atomics is not supported.");
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.VertexPipelineStoresAndAtomics == 0, "Required feature vertex pipeline stores and atomics is not supported.");
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.ShaderStorageImageWriteWithoutFormat == 0, "Required feature shader storage image write without format is not supported.");
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.ShaderImageGatherExtended == 0, "Required feature shader image gather extended is not supported.");
            ThrowNotSupportedIf(physicalDeviceFeatures2.Features.IndependentBlend == 0, "Required feature independent blend is not supported.");
            ThrowNotSupportedIf(physicalDeviceDynamicRenderingFeatures.DynamicRendering == 0, "Required feature dynamic rendering is not supported.");
            ThrowNotSupportedIf(physicalDeviceRobustness2Features.NullDescriptor == 0, "Required feature null descriptor is not supported.");
            ThrowNotSupportedIf(physicalDeviceSynchronization2Features.Synchronization2 == 0, "Required feature synchronization2 is not supported.");
        }

        // Enable required features
        {
            bool enableFp16 = physicalDeviceFloat16ShaderFeatures.ShaderFloat16 > 0 && physicalDeviceFloat16StorageFeatures.StorageBuffer16BitAccess > 0;

            // We don't want to enable unnecessary features, so we construct a clean feature set here rather than using the probed features.
            physicalDeviceFeatures2.Features = new()
            {
                SamplerAnisotropy = 1,
                SampleRateShading = 1,
                GeometryShader = 1,
                DrawIndirectFirstInstance = 1,
                FragmentStoresAndAtomics = 1,
                VertexPipelineStoresAndAtomics = 1,
                ShaderStorageImageWriteWithoutFormat = 1,
                ShaderImageGatherExtended = 1,
                IndependentBlend = 1,
                ShaderInt16 = enableFp16 ? 1u : 0u
            };

            // Don't enable unnecessary bounds checking features, we only care about null descriptor.
            physicalDeviceRobustness2Features.RobustBufferAccess2 = 0;
            physicalDeviceRobustness2Features.RobustImageAccess2 = 0;
        }

        uint generalQueueFamilyIndex = uint.MaxValue;

        // Find suitable queue family
        {
            var physicalDeviceQueueFamilyPropertyCount = 0u;
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &physicalDeviceQueueFamilyPropertyCount);

            var physicalDeviceQueueFamilyProperties = stackalloc VkQueueFamilyProperties[(int)physicalDeviceQueueFamilyPropertyCount];
            vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &physicalDeviceQueueFamilyPropertyCount, physicalDeviceQueueFamilyProperties);

            for (uint i = 0; i < physicalDeviceQueueFamilyPropertyCount; i++)
            {
                var queueFlags = physicalDeviceQueueFamilyProperties[i].QueueFlags;
                if (queueFlags.HasFlag(VkQueueFlags.GRAPHICS_BIT) &&
                    queueFlags.HasFlag(VkQueueFlags.COMPUTE_BIT) &&
                    queueFlags.HasFlag(VkQueueFlags.TRANSFER_BIT))
                {
                    generalQueueFamilyIndex = i;
                    break;
                }
            }

            ThrowVulkanIf(generalQueueFamilyIndex == uint.MaxValue, "Failed to find a queue family.");
        }

        float queuePriority = 0.5f;
        VkDeviceQueueCreateInfo generalQueueCreateInfo = new(default)
        {
            QueueCount = 1,
            QueuePriorities = &queuePriority,
            QueueFamilyIndex = generalQueueFamilyIndex
        };

        VkDeviceCreateInfo deviceCreateInfo = new(next: Next(&physicalDeviceFeatures2))
        {
            EnabledFeatures = default, // Must be null when using VkPhysicalDeviceFeatures2
            EnabledExtensionCount = (uint)enabledDeviceExtensionNames.Count,
            EnabledExtensionNames = enabledDeviceExtensionNames.DangerousGetHandle(),
            QueueCreateInfoCount = 1,
            QueueCreateInfos = &generalQueueCreateInfo,
        };

        // Create the device
        VkDevice device = default;
        ThrowVulkanIfFailed(vkCreateDevice(physicalDevice, &deviceCreateInfo, default, &device),
            "Failed to create device.");
        this.device = device;

        // Get the device queue
        VkQueue deviceQueue = default;
        vkGetDeviceQueue(device, generalQueueFamilyIndex, 0, &deviceQueue);
        ThrowVulkanIf(!deviceQueue.HasValue, "Failed to get device queue.");
        this.deviceQueue = deviceQueue;
        this.deviceQueueIndex = generalQueueFamilyIndex;

        // Get device function pointers
        const string STR_vkCmdPushDescriptorSetKHR = "vkCmdPushDescriptorSetKHR";
        _vkCmdPushDescriptorSetKhr = (delegate* unmanaged[Stdcall]<VkCommandBuffer, VkPipelineBindPoint, VkPipelineLayout, uint, uint, VkWriteDescriptorSet*, void>)vkGetDeviceProcAddr(device, STR_vkCmdPushDescriptorSetKHR);
        ThrowVulkanIf(_vkCmdPushDescriptorSetKhr == null, $"Failed to get {STR_vkCmdPushDescriptorSetKHR} function pointer.");

        ResolveSampleCount();
        ResolveDepthFormat();
    }

    void CreateVulkanMemoryAllocator()
    {
        uint availableApiVersionRaw = 0;
        ThrowVulkanIfFailed(vkEnumerateInstanceVersion(&availableApiVersionRaw));

        VmaAllocateDeviceMemoryFunction = VmaAllocateDeviceMemory;
        VmaFreeDeviceMemoryFunction = VmaFreeDeviceMemory;

        VmaDeviceMemoryCallbacks vmaDeviceMemoryCallbacks = new()
        {
            pfnAllocate = Marshal.GetFunctionPointerForDelegate(VmaAllocateDeviceMemoryFunction),
            pfnFree = Marshal.GetFunctionPointerForDelegate(VmaFreeDeviceMemoryFunction)
        };

        VmaAllocatorCreateInfo vmaAllocatorCreateInfo = new()
        {
            VulkanApiVersion = availableApiVersionRaw,
            Instance = instance,
            PhysicalDevice = physicalDevice,
            Device = device,
            DeviceMemoryCallbacks = &vmaDeviceMemoryCallbacks
        };

        VmaAllocator pVmaAllocator;
        ThrowVulkanIfFailed(vmaCreateAllocator(&vmaAllocatorCreateInfo, &pVmaAllocator));
        allocator = pVmaAllocator;
    }

    void CreateDeviceCommandPool()
    {
        VkCommandPoolCreateInfo commandPoolCreateInfo = new(default)
        {
            QueueFamilyIndex = deviceQueueIndex,
            Flags = VkCommandPoolCreateFlags.RESET_COMMAND_BUFFER
        };

        VkCommandPool commandPool = default;
        ThrowVulkanIfFailed(vkCreateCommandPool(device, &commandPoolCreateInfo, default, &commandPool));
        deviceCommandPool = commandPool;
    }

    void ResolveSampleCount()
    {
        VkPhysicalDeviceProperties physicalDeviceProperties;
        vkGetPhysicalDeviceProperties(physicalDevice, &physicalDeviceProperties);

        VkSampleCount colorMaxSampleCount = VkSampleCount.COUNT_1;

        VkSampleCount maximumCombinedSampleCount = (VkSampleCount)Math.Min((int)physicalDeviceProperties.Limits.FramebufferColorSampleCounts, (int)physicalDeviceProperties.Limits.FramebufferDepthSampleCounts);
        if (maximumCombinedSampleCount.HasFlag(VkSampleCount.COUNT_8)) colorMaxSampleCount = VkSampleCount.COUNT_8;
        else if (maximumCombinedSampleCount.HasFlag(VkSampleCount.COUNT_4)) colorMaxSampleCount = VkSampleCount.COUNT_4;
        else if (maximumCombinedSampleCount.HasFlag(VkSampleCount.COUNT_2)) colorMaxSampleCount = VkSampleCount.COUNT_2;

        deviceSampleCount = colorMaxSampleCount;
    }

    void ResolveDepthFormat()
    {
        Span<VkFormat> depthFormats = [VkFormat.D32_SFLOAT, VkFormat.D16_UNORM];
        for (int i = 0; i < depthFormats.Length; i++)
        {
            VkFormatProperties formatProps;
            vkGetPhysicalDeviceFormatProperties(physicalDevice, depthFormats[i], &formatProps);

            // Format must support depth stencil attachment for optimal tiling
            if (formatProps.OptimalTilingFeatures.HasFlag(VkFormatFeatureFlags.DepthStencilAttachment))
            {
                deviceDepthFormat = depthFormats[i];
                break;
            }
        }
    }

    /// <summary>
    /// Gets the Vulkan physical device corresponding to the given <see cref="GfxPhysicalDevice"/>.
    /// Throws an <see cref="InvalidDataException"/> if the device is not found.
    /// </summary>
    /// <param name="gfxPhysicalDevice"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    VkPhysicalDevice GetVulkanPhysicalDevice(GfxPhysicalDevice gfxPhysicalDevice)
    {
        var physicalDeviceCount = 0u;
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, default),
            "Failed to get physical device count.");

        var physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices),
            "Failed to get physical devices.");

        GfxPhysicalDevice[] devices = new GfxPhysicalDevice[(int)physicalDeviceCount];
        for (int i = 0; i < physicalDeviceCount; i++)
        {
            VkPhysicalDeviceProperties properties;
            vkGetPhysicalDeviceProperties(physicalDevices[i], &properties);

            if (gfxPhysicalDevice.Id == new Guid(new ReadOnlySpan<byte>(properties.PipelineCacheUuid, 16)))
            {
                return physicalDevices[i];
            }
        }

        throw new InvalidOperationException($"Physical device {gfxPhysicalDevice} not found.");
    }

    /// <summary>
    /// Enumerates the available physical devices.
    /// </summary>
    /// <returns></returns>
    public GfxPhysicalDevice[] EnumeratePhysicalDevices()
    {
        uint physicalDeviceCount = 0;
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, default), "Failed to get physical device count.");

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices), "Failed to get physical devices.");

        GfxPhysicalDevice[] devices = new GfxPhysicalDevice[(int)physicalDeviceCount];
        for (int i = 0; i < physicalDeviceCount; i++)
        {
            VkPhysicalDeviceProperties properties;
            vkGetPhysicalDeviceProperties(physicalDevices[i], &properties);

            GfxPhysicalDeviceType type = properties.DeviceType switch
            {
                VkPhysicalDeviceType.INTEGRATED_GPU => GfxPhysicalDeviceType.Integrated,
                VkPhysicalDeviceType.DISCRETE_GPU => GfxPhysicalDeviceType.Discrete,
                VkPhysicalDeviceType.VIRTUAL_GPU => GfxPhysicalDeviceType.Virtual,
                VkPhysicalDeviceType.CPU => GfxPhysicalDeviceType.Cpu,
                _ => GfxPhysicalDeviceType.Other
            };

            devices[i] = new GfxPhysicalDevice(
                Type: type,
                Id: new Guid(new ReadOnlySpan<byte>(properties.PipelineCacheUuid, 16)),
                Name: new string((sbyte*)properties.DeviceName),
                Driver: ParseVersion(properties.DriverVersion),
                Api: ParseVersion(properties.ApiVersion)
            );

            logger.Debug($"Available physical device: {devices[i]}");
        }

        return devices;
    }

    /// <summary>
    /// Selects the optimal device from the given array of devices. The optimal device is a discrete GPU if available, otherwise an integrated GPU.
    /// </summary>
    /// <param name="devices"></param>
    /// <returns></returns>
    public GfxPhysicalDevice SelectOptimalDevice(GfxPhysicalDevice[] devices)
    {
        ThrowInvalidDataIf(devices.Length == 0, "No devices available.");

        GfxPhysicalDevice? selectedDevice = null;
        foreach (GfxPhysicalDevice device in devices)
        {
            if (device.Type == GfxPhysicalDeviceType.Discrete)
            {
                selectedDevice = device;
                break;
            }
            else if (device.Type == GfxPhysicalDeviceType.Integrated && selectedDevice == null)
            {
                selectedDevice = device;
            }
        }

        if (selectedDevice == null)
        {
            logger.Warning("No optimal device found, selecting the first available device.");
            selectedDevice = devices[0];
        }

        return selectedDevice;
    }
}
