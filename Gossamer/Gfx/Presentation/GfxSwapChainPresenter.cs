using System.Diagnostics;

using Gossamer.External.Vulkan;
using Gossamer.Logging;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx.Presentation;

internal unsafe sealed class GfxSwapChainPresenter : GfxPresenter
{
    readonly Logger logger = Core.GetLogger(nameof(GfxSwapChainPresenter));

    record PerFrame(
        VkCommandPool CommandPool,
        VkCommandBuffer CommandBuffer,
        VkSemaphore SubmitSemaphore,
        VkFence SubmitFence,
        PixelBuffer? OutputImage)
    {
        public static readonly PerFrame Empty = new(default, default, default, default, default);
    }

    readonly VkInstance instance;
    readonly VkPhysicalDevice physicalDevice;
    readonly VkDevice device;
    readonly VkQueue deviceQueue;
    readonly uint deviceQueueIndex;
    readonly Lock deviceQueueLock;

    VkSurfaceKhr surface;
    VkSwapChainKhr swapChain;
    VkExtent2D swapChainExtent = new(0, 0);
    VkFormat swapChainFormat = VkFormat.UNDEFINED;

    PerFrame[] perFrame = [];
    int currentFrameIndex;

    VkFence acquireFence;

    TimeSpan waitingForPreviousFrame = TimeSpan.Zero;
    TimeSpan waitingForNextFrame = TimeSpan.Zero;

    DisplayParameters displayParameters = DisplayParameters.Empty;

    readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public GfxSwapChainPresenter(
        VkInstance instance,
        VkPhysicalDevice physicalDevice,
        VkDevice device,
        VkQueue deviceQueue,
        uint deviceQueueIndex,
        Lock deviceQueueLock,
        VkSurfaceKhr surface)
    {
        this.instance = instance;
        this.physicalDevice = physicalDevice;
        this.device = device;
        this.deviceQueue = deviceQueue;
        this.deviceQueueIndex = deviceQueueIndex;
        this.deviceQueueLock = deviceQueueLock;
        this.surface = surface;

        VkFenceCreateInfo fenceCreateInfo = new(default);
        VkFence pAcquireFence = default;
        ThrowVulkanIfFailed(vkCreateFence(device, &fenceCreateInfo, default, &pAcquireFence));
        acquireFence = pAcquireFence;
    }

    internal override GfxFormat GetFormat()
    {
        return GfxCore.ConvertFormat(swapChainFormat);
    }

    internal override GfxExtent GetExtent()
    {
        return new GfxExtent((int)swapChainExtent.Width, (int)swapChainExtent.Height);
    }

    public override TimeSpan GetPauseDuration()
    {
        return waitingForPreviousFrame + waitingForNextFrame;
    }

    public override void InitializeRendering(DisplayParameters wantedDisplayParameters)
    {
        DisplayParameters currentDisplayParameters = displayParameters;
        displayParameters = wantedDisplayParameters;

        if (currentDisplayParameters.DisplaySizeChanged(wantedDisplayParameters) ||
            currentDisplayParameters.DisplayFormatChanged(wantedDisplayParameters) ||
            currentDisplayParameters.DisplayVerticalSyncChanged(wantedDisplayParameters))
        {
            InitializeSwapChain();
        }
    }

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
            return VkResult.OUT_OF_DATE;
        }

        PerFrame previousFrame = perFrame[currentFrameIndex];

        // Wait for the previous frame to finish
        if (previousFrame.SubmitFence.HasValue)
        {
            TimeSpan beforeWait = stopwatch.Elapsed;

            VkFence localSubmissionFence = previousFrame.SubmitFence;
            ThrowVulkanIfFailed(vkWaitForFences(device, 1, &localSubmissionFence, 1, ulong.MaxValue));

            TimeSpan afterWait = stopwatch.Elapsed;
            TimeSpan waitTime = afterWait - beforeWait;
            waitingForPreviousFrame = waitTime;
        }

        TimeSpan beforeAcquire = stopwatch.Elapsed;

        // Try to acquire the next image
        while (true)
        {
            const ulong nanoSecondsToWait = 1_000_000; // 1 ms

            uint nextFrameIndex = 0;
            VkResult acquireResult = vkAcquireNextImageKhr(device, swapChain, nanoSecondsToWait, default, acquireFence, &nextFrameIndex);
            if (acquireResult == VkResult.SUCCESS || acquireResult == VkResult.SUBOPTIMAL)
            {
                VkFence localAcquireFence = acquireFence;
                ThrowVulkanIfFailed(vkWaitForFences(device, 1, &localAcquireFence, 1, ulong.MaxValue));
                ThrowVulkanIfFailed(vkResetFences(device, 1, &localAcquireFence));

                currentFrameIndex = (int)nextFrameIndex;

                TimeSpan afterAcquire = stopwatch.Elapsed;
                TimeSpan acquireTime = afterAcquire - beforeAcquire;
                waitingForNextFrame = acquireTime;
                break;
            }
            else if (acquireResult == VkResult.TIMEOUT)
            {
                logger.Warning($"{acquireResult}");
                continue;
            }
            else
            {
                // NOTE:  If vkAcquireNextImageKHR does not successfully acquire an image, semaphore and fence are unaffected.
                //        We don't need to worry about them here.
                return acquireResult;
            }
        }

        PerFrame nextFrame = perFrame[currentFrameIndex];

        // Reset the submission fence
        if (nextFrame.SubmitFence.HasValue)
        {
            VkFence localSubmissionFence = nextFrame.SubmitFence;
            ThrowVulkanIfFailed(vkWaitForFences(device, 1, &localSubmissionFence, 1, ulong.MaxValue));
            ThrowVulkanIfFailed(vkResetFences(device, 1, &localSubmissionFence));
        }

        // Reset the command pool
        if (nextFrame.CommandPool.HasValue)
        {
            VkCommandPool localCommandPool = nextFrame.CommandPool;
            ThrowVulkanIfFailed(vkResetCommandPool(device, localCommandPool, 0));
        }

        return VkResult.SUCCESS;
    }

    public override bool BeginFrame()
    {
        VkResult result = AcquireNextImage();
        if (result == VkResult.OUT_OF_DATE || result == VkResult.SUBOPTIMAL)
        {
            bool refreshOK = InitializeSwapChain();
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
        ThrowInvalidOperationIfNull(frame.OutputImage);

        VkCommandBufferBeginInfo commandBufferBeginInfo = new(default) { Flags = VkCommandBufferUsageFlags.ONE_TIME_SUBMIT_BIT };
        VkCommandBuffer commandBuffer = frame.CommandBuffer;
        ThrowVulkanIfFailed(vkBeginCommandBuffer(commandBuffer, &commandBufferBeginInfo));

        TransitionImageLayout(
            pixelBuffer: frame.OutputImage,
            commandBuffer: commandBuffer,
            srcLayout: VkImageLayout.UNDEFINED,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            srcStage: VkPipelineStage2.TOP_OF_PIPE,
            dstStage: VkPipelineStage2.ALL_TRANSFER);

        return true;
    }

    public override void EndFrame()
    {
        PerFrame frame = perFrame[currentFrameIndex];
        ThrowInvalidOperationIfNull(frame.OutputImage);

        uint localCurrentFrameIndex = (uint)currentFrameIndex;
        VkSwapChainKhr localSwapChain = swapChain;
        VkSemaphore localSubmitSemaphore = frame.SubmitSemaphore;
        VkCommandBuffer localCommandBuffer = frame.CommandBuffer;

        TransitionImageLayout(
            pixelBuffer: frame.OutputImage,
            commandBuffer: localCommandBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.PRESENT_SRC_KHR,
            srcStage: VkPipelineStage2.ALL_TRANSFER,
            dstStage: VkPipelineStage2.BOTTOM_OF_PIPE);

        ThrowVulkanIfFailed(vkEndCommandBuffer(localCommandBuffer));

        // We are required to synchronize access to the device queue
        using (deviceQueueLock.EnterScope())
        {
            VkPipelineStage pipelineStages = VkPipelineStage.TOP_OF_PIPE;
            VkSubmitInfo submitInfo = new(default)
            {
                WaitDstStageMask = &pipelineStages,
                CommandBufferCount = 1,
                CommandBuffers = &localCommandBuffer,
                SignalSemaphoreCount = 1,
                SignalSemaphores = &localSubmitSemaphore
            };
            ThrowVulkanIfFailed(vkQueueSubmit(deviceQueue, 1, &submitInfo, frame.SubmitFence));

            VkPresentInfoKhr presentInfo = new(default)
            {
                WaitSemaphoreCount = 1,
                WaitSemaphores = &localSubmitSemaphore,
                SwapchainCount = 1,
                Swapchains = &localSwapChain,
                ImageIndices = &localCurrentFrameIndex
            };

            VkResult result = vkQueuePresentKhr(deviceQueue, &presentInfo);
            if (result == VkResult.OUT_OF_DATE || result == VkResult.SUBOPTIMAL)
            {
                InitializeSwapChain();
            }
            else if (result != VkResult.SUCCESS)
            {
                ThrowVulkanIfFailed(result, nameof(vkQueuePresentKhr));
            }
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

    bool InitializeSwapChain()
    {
        ThrowVulkanIfFailed(vkDeviceWaitIdle(device),
            "Failed to wait for device idle.");

        VkSurfaceCapabilitiesKhr surfaceCapabilities;
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfaceCapabilitiesKhr(physicalDevice, surface, &surfaceCapabilities));

        swapChainExtent = new();
        if (surfaceCapabilities.CurrentExtent.Width == uint.MaxValue)
        {
            // If the surface size is undefined, the size is set to the size of the images requested.
            swapChainExtent.Width = Math.Max(surfaceCapabilities.MinImageExtent.Width,
                                        Math.Min(surfaceCapabilities.MaxImageExtent.Width, (uint)displayParameters.DisplayWidth));
            swapChainExtent.Height = Math.Max(surfaceCapabilities.MinImageExtent.Height,
                                        Math.Min(surfaceCapabilities.MaxImageExtent.Height, (uint)displayParameters.DisplayHeight));
        }
        else if (surfaceCapabilities.CurrentExtent.Width > 0 && surfaceCapabilities.CurrentExtent.Height > 0)
        {
            // If the surface size is defined, the swap chain size must match
            swapChainExtent = surfaceCapabilities.CurrentExtent;
        }
        else
        {
            // Can get here if the window is minimized or not visible. Bail out.
            ReleaseSwapChainIfAny();
            return false;
        }

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

        swapChainFormat = outputSurfaceFormat.Format;

        uint presentModesCount = 0;
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfacePresentModesKhr(physicalDevice, surface, &presentModesCount));

        VkPresentModeKhr* presentModes = stackalloc VkPresentModeKhr[(int)presentModesCount];
        ThrowVulkanIfFailed(vkGetPhysicalDeviceSurfacePresentModesKhr(physicalDevice, surface, &presentModesCount, presentModes));

        VkPresentModeKhr swapChainPresentMode = VkPresentModeKhr.FIFO;
        if (displayParameters.VerticalSync)
        {
            // VK_PRESENT_MODE_FIFO_KHR mode must always be present as per spec, this mode waits for the vertical blank ("v-sync")
            swapChainPresentMode = VkPresentModeKhr.FIFO;
        }
        else
        {
            // If v-sync is not requested, try to find a mailbox mode, it's the lowest latency non-tearing present mode available
            for (int i = 0; i < presentModesCount; i++)
            {
                if (presentModes[i] == VkPresentModeKhr.MAILBOX)
                {
                    swapChainPresentMode = VkPresentModeKhr.MAILBOX;
                    break;
                }

                // We'll take IMMEDIATE mode if that's all that's available but keep looking
                if (presentModes[i] == VkPresentModeKhr.IMMEDIATE)
                {
                    swapChainPresentMode = VkPresentModeKhr.IMMEDIATE;
                }
            }
        }

        // Required number of swap chain images
        uint desiredNumberOfSwapChainImages = 2;
        ThrowNotSupportedIf(desiredNumberOfSwapChainImages < surfaceCapabilities.MinImageCount, "Requested number of swap chain images is too low.");

        // Required swap chain image usage
        VkImageUsage desiredSwapChainImageUsage =
            VkImageUsage.COLOR_ATTACHMENT_BIT |
            VkImageUsage.TRANSFER_SRC_BIT |
            VkImageUsage.TRANSFER_DST_BIT;
        ThrowNotSupportedIf(!surfaceCapabilities.SupportedUsageFlags.HasFlag(desiredSwapChainImageUsage), "Desired swap chain image usage is not supported.");

        VkSwapChainCreateInfoKhr swapChainCreateInfo = new(default)
        {
            Surface = surface,
            MinImageCount = desiredNumberOfSwapChainImages,
            ImageFormat = outputSurfaceFormat.Format,
            ImageColorSpace = outputSurfaceFormat.ColorSpace,
            ImageExtent = swapChainExtent,
            ImageUsage = desiredSwapChainImageUsage,
            ImageArrayLayers = 1,
            ImageSharingMode = VkSharingMode.EXCLUSIVE,
            PreTransform = SurfaceTransformFlagsKhr.IDENTITY_BIT_KHR,
            QueueFamilyIndexCount = 0,
            OldSwapChain = swapChain,
            Clipped = 1,
            PresentMode = swapChainPresentMode,
            CompositeAlpha = CompositeAlphaFlagsKhr.OPAQUE_BIT_KHR
        };
        VkSwapChainKhr localSwapChain = default;
        ThrowVulkanIfFailed(vkCreateSwapchainKhr(device, &swapChainCreateInfo, default, &localSwapChain));

        ReleaseSwapChainIfAny();
        swapChain = localSwapChain;

        // Query swap chain images

        uint swapChainImageCount = 0;
        ThrowVulkanIfFailed(vkGetSwapchainImagesKhr(device, swapChain, &swapChainImageCount));

        VkImage* pSwapChainImages = stackalloc VkImage[(int)swapChainImageCount];
        ThrowVulkanIfFailed(vkGetSwapchainImagesKhr(device, swapChain, &swapChainImageCount, pSwapChainImages));

        Array.Resize(ref perFrame, (int)swapChainImageCount);
        Array.Fill(perFrame, PerFrame.Empty);

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
                width: (int)swapChainExtent.Width,
                height: (int)swapChainExtent.Height,
                image: swapChainImage,
                view: swapChainImageView,
                allocation: default
            );

            VkCommandPoolCreateInfo commandPoolCreateInfo = new(default) { QueueFamilyIndex = deviceQueueIndex, Flags = VkCommandPoolCreateFlags.TRANSIENT };
            VkCommandPool commandPool;
            ThrowVulkanIfFailed(vkCreateCommandPool(device, &commandPoolCreateInfo, default, &commandPool));

            VkCommandBufferAllocateInfo commandBufferAllocateInfo = new(default) { Pool = commandPool, Count = 1 };
            VkCommandBuffer commandBuffer;
            ThrowVulkanIfFailed(vkAllocateCommandBuffers(device, &commandBufferAllocateInfo, &commandBuffer));

            VkFenceCreateInfo submissionFenceCreateInfo = new(default) { Flags = VkFenceCreateFlags.SIGNALED };
            VkFence submissionFence;
            ThrowVulkanIfFailed(vkCreateFence(device, &submissionFenceCreateInfo, default, &submissionFence));

            VkSemaphoreCreateInfo semaphoreCreateInfo = new(default);
            VkSemaphore releaseSemaphore;
            ThrowVulkanIfFailed(vkCreateSemaphore(device, &semaphoreCreateInfo, default, &releaseSemaphore));

            perFrame[i] = new PerFrame(commandPool, commandBuffer, releaseSemaphore, submissionFence, outputImage);
        }

        // Log the swap chain details
        logger.Debug(
            $"{swapChainExtent.Width}x{swapChainExtent.Height}x{swapChainImageCount} " +
            $"[{outputSurfaceFormat.Format}, {outputSurfaceFormat.ColorSpace}] " +
            $"[{swapChainPresentMode}]");

        return true;
    }

    void ReleasePerFrame()
    {
        for (int i = 0; i < perFrame.Length; i++)
        {
            PerFrame frame = perFrame[i];

            if (frame.SubmitSemaphore.HasValue)
            {
                vkDestroySemaphore(device, frame.SubmitSemaphore, default);
            }
            if (frame.SubmitFence.HasValue)
            {
                vkDestroyFence(device, frame.SubmitFence, default);
            }
            if (frame.OutputImage != null && frame.OutputImage.View.HasValue)
            {
                vkDestroyImageView(device, frame.OutputImage.View, default);
            }
            if (frame.CommandBuffer.HasValue)
            {
                VkCommandBuffer commandBuffer = frame.CommandBuffer;
                vkFreeCommandBuffers(device, frame.CommandPool, 1, &commandBuffer);
            }
            if (frame.CommandPool.HasValue)
            {
                vkDestroyCommandPool(device, frame.CommandPool, default);
            }

            perFrame[i] = PerFrame.Empty;
        }
    }

    protected override void Dispose(bool disposing)
    {
        ReleasePerFrame();

        if (acquireFence.HasValue)
        {
            vkDestroyFence(device, acquireFence);
            acquireFence = default;
        }

        if (swapChain.HasValue)
        {
            vkDestroySwapchainKhr(device, swapChain);
            swapChain = default;
        }

        if (surface.HasValue)
        {
            ThrowVulkanIfFailed(vkDestroySurfaceKhr(instance, surface, default));
            surface = default;
        }
    }
}