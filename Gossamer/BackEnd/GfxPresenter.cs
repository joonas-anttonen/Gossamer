using System.Diagnostics;

using Gossamer.External.Vulkan;
using Gossamer.Logging;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Backend;

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
    readonly Logger logger = Gossamer.GetLogger(nameof(GfxSwapChainPresenter));

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
