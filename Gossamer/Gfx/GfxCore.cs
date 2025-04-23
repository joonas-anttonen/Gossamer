using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Gossamer.External.Vulkan;
using Gossamer.External.Vulkan.Vma;
using Gossamer.External.Webp;
using Gossamer.Gfx.Presentation;
using Gossamer.Gfx.Shaders;
using Gossamer.Logging;
using Gossamer.Utilities;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.External.Vulkan.Vma.Api;
using static Gossamer.External.Webp.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx;

public unsafe class GfxCore : IDisposable
{
    public readonly record struct Statistics(
        ulong Frame,
        TimeSpan CpuFrameTime,
        TimeSpan GpuFrameTime,
        TimeSpan CpuPauseDuration);

    readonly Logger logger = Core.GetLogger(nameof(GfxCore));

    PFN_vkDebugUtilsMessengerCallbackEXT? VulkanDebugMessengerCallback;
    PFN_vkSetDebugUtilsObjectNameEXT? VulkanDebugSetObjectName;

    PFN_vmaAllocateDeviceMemoryFunction? VmaAllocateDeviceMemoryFunction;
    PFN_vmaFreeDeviceMemoryFunction? VmaFreeDeviceMemoryFunction;

    VkDebugUtilsMessengerExt debugUtilsMessenger;

    VmaAllocator allocator;

    VkInstance instance;
    VkPhysicalDevice physicalDevice;
    VkDevice device;

    VkFormat deviceDepthFormat;
    VkSampleCount deviceSampleCount;

    VkQueue deviceQueue;
    readonly Lock deviceQueueLock = new();
    uint deviceQueueIndex;

    float deviceTimestampPeriodInNanoseconds;

    VkCommandPool deviceCommandPool;
    GfxTimestampPool? timestampPool;

    readonly GfxApiParameters apiParameters;
    GfxParameters? parameters;
    DisplayParameters currentDisplayParameters = DisplayParameters.Empty with
    {
        RenderWidth = 1270,
        RenderHeight = 720,
    };

    GfxCapabilities capabilities = new(
        Debugging: false,
        SwapChain: false,
        Timestamps: false
    );

    GfxPresenter? presenter;
    Gfx2D? gfx2D;
    Gfx3D? gfx3D;

    ulong frameCounter;
    Statistics statistics;

    bool pendingScreenshot = false;

    readonly Dictionary<string, GfxPipelineShader> cachedPipelineShaders = [];

    public Statistics GetStatistics()
    {
        return statistics;
    }

    public DisplayParameters GetDisplayParameters()
    {
        return currentDisplayParameters;
    }

    internal GfxSamples GetMaxSampleCount()
    {
        return (GfxSamples)deviceSampleCount;
    }

    /// <summary>
    /// Returns the 2D renderer.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the 2D renderer is not available.</exception>
    internal Gfx2D Get2D()
    {
        return ThrowInvalidOperationIfNull(gfx2D, "No 2D renderer available.");
    }

    /// <summary>
    /// Returns the 3D renderer.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the 3D renderer is not available.</exception>
    internal Gfx3D Get3D()
    {
        return ThrowInvalidOperationIfNull(gfx3D, "No 3D renderer available.");
    }

    /// <summary>
    /// Returns the current presenter. Can be null if no presenter is currently available.
    /// </summary>
    internal GfxPresenter? GetPresenter()
    {
        if (presenter == null)
        {
            logger.Warning("No presenter available.");
        }
        return presenter;
    }

    public GfxCore(GfxApiParameters apiParameters)
    {
        this.apiParameters = apiParameters;

        CreateInstance();
    }

    public void Render()
    {
        ThrowInvalidOperationIfNull(gfx2D);
        ThrowInvalidOperationIfNull(gfx3D);

        if (presenter == null)
        {
            logger.Warning("No presenter available.");
            return;
        }

        bool canRender = presenter.BeginFrame();
        if (!canRender)
        {
            logger.Warning("Failed to begin frame.");
            return;
        }

        VkFormat displayFormat = presenter.GetFormat();
        VkExtent2D displayExtent = presenter.GetExtent();
        if (currentDisplayParameters.DisplaySizeChanged(displayExtent) ||
            currentDisplayParameters.DisplayFormatChanged(displayFormat))
        {
            InitializeRendering(currentDisplayParameters with
            {
                DisplayWidth = (int)displayExtent.Width,
                DisplayHeight = (int)displayExtent.Height,
                DisplayFormat = (GfxFormat)displayFormat,
            });
        }

        TimeSpan cpuFrameTime = TimeSpan.Zero;
        TimeSpan gpuFrameTime = TimeSpan.Zero;
        if (timestampPool != null)
        {
            timestampPool.Reset(device, presenter.GetCommandBuffer());

            cpuFrameTime = timestampPool.GetCpuDuration(0, 1);
            gpuFrameTime = timestampPool.GetGpuDuration(0, 1);

            timestampPool.BeginCpuTimestamp();
            timestampPool.BeginGpuTimestamp(presenter.GetCommandBuffer());
        }

        gfx3D.Render(presenter);
        gfx2D.Render(presenter);

        if (timestampPool != null)
        {
            timestampPool.EndGpuTimestamp(presenter.GetCommandBuffer());
            timestampPool.EndCpuTimestamp();
        }

        if (!pendingScreenshot)
        {
            presenter.EndFrame();
        }
        else
        {
            pendingScreenshot = false;

            int screenshotDataLength = currentDisplayParameters.DisplayWidth * currentDisplayParameters.DisplayHeight * 4;
            MemoryBuffer<byte> screenshotBuffer = CreateMemoryBuffer<byte>(
                screenshotDataLength,
                GfxMemoryUsage.TransferDst,
                GfxMemoryAccess.Read);

            VkCommandBuffer commandBuffer = presenter.GetCommandBuffer();
            PixelBuffer presentBuffer = presenter.GetPresentationBuffer();

            FullBarrier(commandBuffer);

            PixelBufferBarrier(
                commandBuffer,
                presentBuffer,
                srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
                dstLayout: VkImageLayout.TRANSFER_SRC_OPTIMAL);

            VkBufferImageCopy bufferImageCopy = new()
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new()
                {
                    Aspect = VkImageAspect.COLOR,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
                ImageOffset = new(0, 0, 0),
                ImageExtent = new((uint)currentDisplayParameters.DisplayWidth, (uint)currentDisplayParameters.DisplayHeight, 1),
            };
            vkCmdCopyImageToBuffer(commandBuffer, presentBuffer.Image, VkImageLayout.TRANSFER_SRC_OPTIMAL, screenshotBuffer.Buffer, 1, &bufferImageCopy);

            PixelBufferBarrier(
                commandBuffer,
                presentBuffer,
                srcLayout: VkImageLayout.TRANSFER_SRC_OPTIMAL,
                dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);

            FullBarrier(commandBuffer);

            presenter.EndFrame();
            vkQueueWaitIdle(deviceQueue);

            byte[] screenshotData = ReadMemoryBuffer(screenshotBuffer, screenshotDataLength);
            DestroyMemoryBuffer(screenshotBuffer);

            fixed (byte* p_screenshotData = screenshotData)
            {
                byte* webpData = null;
                int webpDataLength = 0;
                WebPStatus webpStatus = webpEncode(
                    p_screenshotData,
                    screenshotDataLength,
                    webpConvertFormat(currentDisplayParameters.DisplayFormat),
                    currentDisplayParameters.DisplayWidth,
                    currentDisplayParameters.DisplayHeight,
                    currentDisplayParameters.DisplayWidth * 4,
                    &webpData,
                    &webpDataLength);
                ThrowInvalidDataIf(webpStatus != WebPStatus.OK, $"{nameof(webpEncode)}: {webpStatus}");

                File.WriteAllBytes("c:/users/jant/desktop/screenshot.webp", new ReadOnlySpan<byte>(webpData, webpDataLength));

                webpStatus = webpFree(webpData);
                ThrowInvalidDataIf(webpStatus != WebPStatus.OK, $"{nameof(webpFree)}: {webpStatus}");
            }
        }

        statistics = new(
            frameCounter++,
            cpuFrameTime,
            gpuFrameTime,
            presenter.GetPauseDuration());
    }

    public void Create(GfxParameters parameters)
    {
        this.parameters = parameters;

        CreateDevice();
        CreateDeviceCommandPool();
        CreateMemoryAllocator();

        LoadShaders(ReflectionUtilities.LoadEmbeddedResourceAsStream("Gossamer.Gfx.Shaders.built-in.shaders"));

        gfx3D = new Gfx3D(this);
        gfx3D.Create();

        gfx2D = new Gfx2D(this);
        gfx2D.Create();
    }

    public void CreatePresenter(GfxPresentation presentation)
    {
        switch (presentation)
        {
            case GfxSwapChainPresentation swapChainPresentation:
                {
                    var swapChainSurface = swapChainPresentation.Gui.CreateSurface(instance);
                    var swapChainPresenter = new GfxSwapChainPresenter(
                        instance,
                        physicalDevice,
                        device,
                        deviceQueue,
                        deviceQueueIndex,
                        deviceQueueLock,
                        swapChainSurface.Surface,
                        swapChainSurface.Extent);
                    presenter = swapChainPresenter;
                    break;
                }

            default:
                throw new NotImplementedException();
        }
    }

    public void InitializeRendering(DisplayParameters displayParameters)
    {
        ThrowInvalidOperationIfNull(gfx2D);
        ThrowInvalidOperationIfNull(gfx3D);

        currentDisplayParameters = displayParameters;

        // Log new display parameters.
        logger.Debug($"{displayParameters.DisplayWidth}x{displayParameters.DisplayHeight} [{displayParameters.DisplayFormat}]");

        gfx3D.InitializeRendering(displayParameters);
        gfx2D.InitializeRendering(displayParameters);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (device.HasValue)
        {
            vkDeviceWaitIdle(device);
        }

        if (deviceCommandPool.HasValue)
        {
            vkDestroyCommandPool(device, deviceCommandPool, default);
            deviceCommandPool = default;
        }

        if (timestampPool != null)
        {
            DestroyTimestampPool(timestampPool);
            timestampPool = null;
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

    [Conditional("DEBUG")]
    internal void AssingName(PixelBuffer pixelBuffer, string name)
    {
        VulkanSetObjectName(VkObjectType.IMAGE, pixelBuffer.Image.Value, name);
    }

    uint VulkanDebugMessageCallback(VkDebugUtilsMessageSeverityExt severity, VkDebugUtilsMessageTypeExt type, VkDebugUtilsMessengerCallbackDataExt* pCallbackData, nint pUserData)
    {
        string msg = Utf8StringMarshaller.ConvertToManaged((byte*)pCallbackData->pMessage) ?? string.Empty;

        if (severity == VkDebugUtilsMessageSeverityExt.ERROR)
        {
            logger.Error(msg, "Vulkan", "Validation");
        }
        else if (severity == VkDebugUtilsMessageSeverityExt.WARNING)
        {
            logger.Warning(msg, "Vulkan", "Validation");
        }
        else if (severity == VkDebugUtilsMessageSeverityExt.INFO)
        {
            logger.Information(msg, "Vulkan", "Validation");
        }
        else if (severity == VkDebugUtilsMessageSeverityExt.VERBOSE)
        {
            logger.Debug(msg, "Vulkan", "Validation");
        }
        return 0;
    }

    void VmaAllocateDeviceMemory(VmaAllocator allocator, uint memoryType, VkDeviceMemory memory, ulong size, nint pUserData)
    {
        logger.Debug($"{StringUtilities.ByteSizeShortIEC(size)}", "Vma", "Allocate");
    }

    void VmaFreeDeviceMemory(VmaAllocator allocator, uint memoryType, VkDeviceMemory memory, ulong size, nint pUserData)
    {
        logger.Debug($"{StringUtilities.ByteSizeShortIEC(size)}", "Vma", "Free");
    }

    internal GfxTimestampPool CreateTimestampPool(int capacity)
    {
        ThrowNotSupportedIf(!capabilities.Timestamps, "Timestamps are not supported.");

        VkQueryPoolCreateInfo queryPoolCreateInfo = new(default)
        {
            QueryType = VkQueryType.TIMESTAMP,
            QueryCount = (uint)capacity
        };

        VkQueryPool queryPool;
        ThrowVulkanIfFailed(vkCreateQueryPool(device, &queryPoolCreateInfo, default, &queryPool));

        return new GfxTimestampPool(queryPool, capacity, deviceTimestampPeriodInNanoseconds);
    }

    internal void DestroyTimestampPool(GfxTimestampPool? timestampPool)
    {
        if (timestampPool == null)
        {
            return;
        }

        // FIXME: Implement proper resource management so we don't have to wait for idle.
        vkDeviceWaitIdle(device);

        vkDestroyQueryPool(device, timestampPool.queryPool, default);
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

        // We are required to synchronize access to the device queue
        using (deviceQueueLock.EnterScope())
        {
            ThrowVulkanIfFailed(vkQueueSubmit(deviceQueue, 1, &submitInfo, fence));
        }
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

    internal void FullBarrier(VkCommandBuffer commandBuffer)
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
                LayerCount = 1
            }
        };

        VkDependencyInfo dependencyInfo = new(default)
        {
            ImageMemoryBarrierCount = 1,
            ImageMemoryBarriers = &imageMemoryBarrier
        };

        vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);
    }

    byte[] ReadMemoryBuffer(MemoryBuffer<byte> memoryBuffer, int size)
    {
        byte[] data = new byte[size];

        void* pDst = null;
        ThrowVulkanIfFailed(vmaMapMemory(allocator, memoryBuffer.Allocation, &pDst));

        fixed (byte* pData = data)
        {
            Unsafe.CopyBlock(pData, pDst, (uint)size);
        }

        vmaUnmapMemory(allocator, memoryBuffer.Allocation);
        return data;
    }

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

    /// <summary>
    /// Updates a dynamic memory buffer with the specified data.
    /// <para>Safe to call with an empty span.</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="memoryBuffer"></param>
    /// <param name="data"></param>
    internal void UpdateDynamicBuffer<T>(MemoryBuffer<T> memoryBuffer, ReadOnlySpan<T> data) where T : unmanaged
    {
        if (data.IsEmpty) return;

        fixed (void* pData = data)
        {
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

        // FIXME: Implement proper resource management so we don't have to wait for idle.
        vkDeviceWaitIdle(device);

        vmaDestroyBuffer(allocator, memoryBuffer.Buffer, memoryBuffer.Allocation);
    }

    internal MemoryBuffer<T> CreateMemoryBuffer<T>(int length, GfxMemoryUsage usage, GfxMemoryAccess access)
    {
        VkBufferCreateInfo bufferCreateInfo = new(default)
        {
            Size = (ulong)(Marshal.SizeOf<T>() * length),
            Usage = (VkBufferUsage)usage,
        };

        VmaAllocationCreateInfo allocationCreateInfo = new()
        {
            Usage = VmaMemoryUsage.AUTO,
        };

        if (access.HasFlag(GfxMemoryAccess.Write))
        {
            allocationCreateInfo.Flags |= VmaAllocationCreateFlags.HOST_ACCESS_SEQUENTIAL_WRITE;
        }

        if (access.HasFlag(GfxMemoryAccess.Read))
        {
            allocationCreateInfo.Flags |= VmaAllocationCreateFlags.HOST_ACCESS_RANDOM;
        }

        VkBuffer buffer;
        VmaAllocation allocation;
        VmaAllocationInfo allocationInfo;
        ThrowVulkanIfFailed(vmaCreateBuffer(allocator, &bufferCreateInfo, &allocationCreateInfo, &buffer, &allocation, &allocationInfo));

        VkMemoryProperty memoryProperties;
        vmaGetMemoryTypeProperties(allocator, allocationInfo.MemoryType, &memoryProperties);

        logger.Debug($"{usage} [{StringUtilities.ByteSizeShortIEC(allocationInfo.Size)}] [{memoryProperties}]");

        return new MemoryBuffer<T>(length: (uint)length, buffer, allocation);
    }

    internal void DestroySampler(VkSampler sampler)
    {
        if (!sampler.HasValue)
        {
            return;
        }

        // FIXME: Implement proper resource management so we don't have to wait for idle.
        vkDeviceWaitIdle(device);

        vkDestroySampler(device, sampler, default);
    }

    internal VkSampler CreateSampler(VkSamplerCreateInfo samplerCreateInfo)
    {
        VkSampler sampler;
        ThrowVulkanIfFailed(vkCreateSampler(device, &samplerCreateInfo, default, &sampler));
        return sampler;
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

        // FIXME: Implement proper resource management so we don't have to wait for idle.
        vkDeviceWaitIdle(device);

        vkDestroyImageView(device, pixelBuffer.View, default);
        vmaDestroyImage(allocator, pixelBuffer.Image, pixelBuffer.Allocation);
    }

    internal PixelBuffer CreatePixelBuffer(
        int width,
        int height,
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
                Width = (uint)width,
                Height = (uint)height,
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

        logger.Debug($"{width}x{height} {format} [{StringUtilities.ByteSizeShortIEC(allocationInfo.Size)}] [{memoryProperties}]");

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

        return new PixelBuffer(format, aspect, samples, width, height, image, view, allocation);
    }

    internal PixelBuffer CreatePixelBufferFromFile(string path, GfxPixelBufferUsage usage)
    {
        byte[] encodedData = File.ReadAllBytes(path);
        int encodedDataLength = encodedData.Length;
        int decodedDataLength = 0;

        int width = 0;
        int height = 0;
        int hasAlpha = 0;
        fixed (byte* p_encodedData = encodedData)
        {
            WebPStatus webpStatus = webpVerify(p_encodedData, encodedDataLength, &width, &height, &hasAlpha);
            ThrowInvalidDataIf(webpStatus != WebPStatus.OK, $"{nameof(webpVerify)}: {webpStatus}");

            decodedDataLength = width * height * 4;
        }

        MemoryBuffer<byte> stagingBuffer = CreateMemoryBuffer<byte>(length: decodedDataLength, GfxMemoryUsage.TransferSrc, GfxMemoryAccess.Write);

        fixed (byte* p_encodedData = encodedData)
        {
            void* p_decodedData = null;
            ThrowVulkanIfFailed(vmaMapMemory(allocator, stagingBuffer.Allocation, &p_decodedData));

            WebPStatus webpStatus = webpDecodeInto(p_encodedData, encodedDataLength, WebPFormat.RGBA, width * 4, (byte*)p_decodedData, decodedDataLength);

            vmaUnmapMemory(allocator, stagingBuffer.Allocation);
            ThrowVulkanIfFailed(vmaFlushAllocation(allocator, stagingBuffer.Allocation, 0, (ulong)decodedDataLength));

            ThrowInvalidDataIf(webpStatus != WebPStatus.OK, $"{nameof(webpDecodeInto)}: {webpStatus}");
        }

        PixelBuffer pixelBuffer = CreatePixelBuffer(
            width: width,
            height: height,
            format: GfxFormat.Rgba8,
            usage: usage | GfxPixelBufferUsage.TransferDst,
            aspect: GfxAspect.Color,
            samples: GfxSamples.X1);

        GfxSingleCommand stagingCommand = BeginSingleCommand();

        PixelBufferBarrier(
            stagingCommand.CommandBuffer,
            pixelBuffer: pixelBuffer,
            srcLayout: VkImageLayout.UNDEFINED,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);

        VkBufferImageCopy bufferImageCopy = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new()
            {
                Aspect = VkImageAspect.COLOR,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new(0, 0, 0),
            ImageExtent = new((uint)width, (uint)height, 1),
        };

        vkCmdCopyBufferToImage(stagingCommand.CommandBuffer, stagingBuffer.Buffer, pixelBuffer.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, 1, &bufferImageCopy);

        PixelBufferBarrier(
            stagingCommand.CommandBuffer,
            pixelBuffer: pixelBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.SHADER_READ_ONLY_OPTIMAL);

        SubmitSingleCommand(stagingCommand);
        EndSingleCommand(stagingCommand);

        DestroyMemoryBuffer(stagingBuffer);

        return pixelBuffer;
    }

    internal PixelBuffer CreatePixelBuffer(byte[] data, int width, int height, GfxFormat format, GfxPixelBufferUsage usage)
    {
        PixelBuffer pixelBuffer = CreatePixelBuffer(
            width: width,
            height: height,
            format: format,
            usage: usage | GfxPixelBufferUsage.TransferDst,
            aspect: GfxAspect.Color,
            samples: GfxSamples.X1);

        MemoryBuffer<byte> stagingBuffer = CreateMemoryBuffer<byte>(length: data.Length, GfxMemoryUsage.TransferSrc, GfxMemoryAccess.Write);
        UpdateDynamicBuffer(stagingBuffer, data);

        GfxSingleCommand stagingCommand = BeginSingleCommand();

        PixelBufferBarrier(
            stagingCommand.CommandBuffer,
            pixelBuffer: pixelBuffer,
            srcLayout: VkImageLayout.UNDEFINED,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);

        VkBufferImageCopy bufferImageCopy = new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new()
            {
                Aspect = VkImageAspect.COLOR,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new(0, 0, 0),
            ImageExtent = new((uint)width, (uint)height, 1),
        };

        vkCmdCopyBufferToImage(stagingCommand.CommandBuffer, stagingBuffer.Buffer, pixelBuffer.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, 1, &bufferImageCopy);

        PixelBufferBarrier(
            stagingCommand.CommandBuffer,
            pixelBuffer: pixelBuffer,
            srcLayout: VkImageLayout.TRANSFER_DST_OPTIMAL,
            dstLayout: VkImageLayout.SHADER_READ_ONLY_OPTIMAL);

        SubmitSingleCommand(stagingCommand);
        EndSingleCommand(stagingCommand);

        DestroyMemoryBuffer(stagingBuffer);

        return pixelBuffer;
    }

    internal VkDescriptorSetLayout CreateDescriptorLayout(VkDescriptorSetLayoutBinding[] bindings)
    {
        fixed (VkDescriptorSetLayoutBinding* pBindings = bindings)
        {
            // Some asserts to make sure the bindings are valid.
            // Hardly any error checking on these in the validation layers.
            ThrowInvalidOperationIfNot(bindings.Length > 0);
            for (int i = 0; i < bindings.Length; i++)
            {
                ThrowInvalidOperationIfNot(bindings[i].DescriptorCount > 0);
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

    internal void LoadShaders(Stream stream)
    {
        foreach (var shaderProgram in GfxShaderPackage.Deserialize(stream))
        {
            cachedPipelineShaders[shaderProgram.Key] = shaderProgram.Value;
        }
    }

    internal GfxPipelineShader GetShaderProgram(string name)
    {
        _ = cachedPipelineShaders.TryGetValue(name, out GfxPipelineShader? shaderProgram);
        ThrowInvalidOperationIfNull(shaderProgram, $"Shader program '{name}' not found.");
        return shaderProgram;
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
            ThrowInvalidOperationIfNot(parameters.InputBindings[i].Stride > 0);
            vertexInputBindings[i] = parameters.InputBindings[i];
        }

        int vertexInputAttibutesCount = parameters.InputAttributes.Length;
        VkVertexInputAttributeDescription* vertexInputAttributes = stackalloc VkVertexInputAttributeDescription[vertexInputAttibutesCount];
        for (uint i = 0; i < vertexInputAttibutesCount; i++)
        {
            ThrowInvalidOperationIfNot(parameters.InputAttributes[i].Format != VkFormat.UNDEFINED);
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
            DepthAttachmentFormat = parameters.DepthTest || parameters.DepthWrite ? deviceDepthFormat : VkFormat.UNDEFINED
        };

        VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new(default)
        {
            AttachmentCount = (uint)attachmentCount,
            Attachments = colorAttachmentBlends
        };

        int shaderStageCount = parameters.ShaderProgram.Stages.Length;
        VkPipelineShaderStageCreateInfo* shaderStageCreateInfos = stackalloc VkPipelineShaderStageCreateInfo[shaderStageCount];
        for (int i = 0; i < shaderStageCount; i++)
        {
            GfxPipelineShader.Stage stage = parameters.ShaderProgram.Stages[i];

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
                Stage = stage.StageType,
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

    void CreateInstance()
    {
        HashSet<string> availableInstanceLayers = [];
        HashSet<string> availableInstanceExtensions = [];

        uint availableApiVersionRaw = 0;
        ThrowVulkanIfFailed(vkEnumerateInstanceVersion(&availableApiVersionRaw),
            "Failed to get instance version.");

        Version availableApiVersion = ParseVersion(availableApiVersionRaw);
        Version requiredApiVersion = new(1, 3, 0);
        ThrowNotSupportedIf(availableApiVersion < requiredApiVersion,
            $"Required API version {requiredApiVersion} not available.");

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
                        capabilities = capabilities with { SwapChain = true };
                    }
                    else
                    {
                        logger.Warning($"Swapchain is enabled but {VK_KHR_win32_surface} is not available.");
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    const string VK_KHR_wayland_surface = "VK_KHR_wayland_surface";
                    const string VK_KHR_xcb_surface = "VK_KHR_xcb_surface";
                    const string VK_KHR_xlib_surface = "VK_KHR_xlib_surface";

                    if (availableInstanceExtensions.Contains(VK_KHR_wayland_surface))
                    {
                        enabledExtensionNames.Add(VK_KHR_wayland_surface);
                        capabilities = capabilities with { SwapChain = true };
                    }
                    if (availableInstanceExtensions.Contains(VK_KHR_xcb_surface))
                    {
                        enabledExtensionNames.Add(VK_KHR_xcb_surface);
                        capabilities = capabilities with { SwapChain = true };
                    }
                    if (availableInstanceExtensions.Contains(VK_KHR_xlib_surface))
                    {
                        enabledExtensionNames.Add(VK_KHR_xlib_surface);
                        capabilities = capabilities with { SwapChain = true };
                    }

                    if (!capabilities.SwapChain)
                    {
                        logger.Warning($"Swapchain is enabled but {VK_KHR_wayland_surface} or {VK_KHR_xcb_surface} or {VK_KHR_xlib_surface} is not available.");
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
                    capabilities = capabilities with { Debugging = true };
                }
                else
                {
                    logger.Warning($"Validation is enabled but {VK_LAYER_KHRONOS_validation} is not available.");
                }

                const string VK_EXT_debug_utils = "VK_EXT_debug_utils";
                if (availableInstanceExtensions.Contains(VK_EXT_debug_utils))
                {
                    enabledExtensionNames.Add(VK_EXT_debug_utils);
                    capabilities = capabilities with { Debugging = true };
                }
                else
                {
                    logger.Warning($"Debugging is enabled but {VK_EXT_debug_utils} is not available.");
                }
            }
        }

        Core.ApplicationInfo engineInfo = Core.ApplicationInfo.FromCallingAssembly();

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

        if (apiParameters.EnableDebugging && capabilities.Debugging)
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
                capabilities = capabilities with { Debugging = false };
            }
        }
    }

    void CreateDevice()
    {
        ThrowInvalidOperationIfNull(parameters);

        VkPhysicalDevice physicalDevice = GetVulkanPhysicalDevice(parameters.PhysicalDevice);
        this.physicalDevice = physicalDevice;

        VkPhysicalDeviceProperties physicalDeviceProperties;
        vkGetPhysicalDeviceProperties(physicalDevice, &physicalDeviceProperties);

        deviceTimestampPeriodInNanoseconds = physicalDeviceProperties.Limits.TimestampPeriod;
        if (deviceTimestampPeriodInNanoseconds > 0)
        {
            capabilities = capabilities with { Timestamps = true };
        }

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
        VkPhysicalDeviceVulkan14Features physicalDeviceVulkan14Features = new(next: default);
        VkPhysicalDeviceSynchronization2Features physicalDeviceSynchronization2Features = new(next: Next(&physicalDeviceVulkan14Features));
        VkPhysicalDeviceRobustness2FeaturesEXT physicalDeviceRobustness2Features = new(next: Next(&physicalDeviceSynchronization2Features));
        VkPhysicalDeviceShaderSubgroupExtendedTypesFeatures physicalDeviceSubgroupExtendedTypesFeatures = new(next: Next(&physicalDeviceRobustness2Features));
        VkPhysicalDevice16BitStorageFeatures physicalDeviceFloat16StorageFeatures = new(next: Next(&physicalDeviceSubgroupExtendedTypesFeatures));
        VkPhysicalDeviceShaderFloat16Int8Features physicalDeviceFloat16ShaderFeatures = new(next: Next(&physicalDeviceFloat16StorageFeatures));
        VkPhysicalDeviceDynamicRenderingFeatures physicalDeviceDynamicRenderingFeatures = new(next: Next(&physicalDeviceFloat16ShaderFeatures));

        VkPhysicalDeviceFeatures2 physicalDeviceFeatures2 = new(next: Next(&physicalDeviceDynamicRenderingFeatures));
        vkGetPhysicalDeviceFeatures2(physicalDevice, &physicalDeviceFeatures2);

        VkPhysicalDeviceVulkan14Properties physicalDeviceVulkan14Properties = new(next: default);
        VkPhysicalDeviceProperties2 physicalDeviceProperties2 = new(next: Next(&physicalDeviceVulkan14Properties));
        vkGetPhysicalDeviceProperties2(physicalDevice, &physicalDeviceProperties2);

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
            //ThrowNotSupportedIf(physicalDevicePushDescriptorProperties.MaxPushDescriptors == 0, "Required feature push descriptor is not supported.");
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

        uint deviceQueueIndex = uint.MaxValue;

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
                    deviceQueueIndex = i;
                    break;
                }
            }

            ThrowVulkanIf(deviceQueueIndex == uint.MaxValue, "Failed to find a queue family.");
        }

        float queuePriority = 0.5f;
        VkDeviceQueueCreateInfo generalQueueCreateInfo = new(default)
        {
            QueueCount = 1,
            QueuePriorities = &queuePriority,
            QueueFamilyIndex = deviceQueueIndex
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
        ThrowVulkanIfFailed(vkCreateDevice(physicalDevice, &deviceCreateInfo, default, &device));
        this.device = device;

        // Get the device queue
        VkQueue deviceQueue = default;
        vkGetDeviceQueue(device, deviceQueueIndex, 0, &deviceQueue);
        this.deviceQueue = deviceQueue;
        this.deviceQueueIndex = deviceQueueIndex;

        // Get device function pointers
        const string STR_vkCmdPushDescriptorSetKHR = "vkCmdPushDescriptorSetKHR";
        _vkCmdPushDescriptorSetKhr = (delegate* unmanaged[Stdcall]<VkCommandBuffer, VkPipelineBindPoint, VkPipelineLayout, uint, uint, VkWriteDescriptorSet*, void>)vkGetDeviceProcAddr(device, STR_vkCmdPushDescriptorSetKHR);
        ThrowVulkanIf(_vkCmdPushDescriptorSetKhr == null, $"{nameof(vkGetDeviceProcAddr)} [{STR_vkCmdPushDescriptorSetKHR}]");

        if (capabilities.Debugging && capabilities.Timestamps)
        {
            timestampPool = CreateTimestampPool(8);
        }

        ResolveSampleCount();
        ResolveDepthFormat();
    }

    void CreateMemoryAllocator()
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
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, default));

        var physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices));

        for (int i = 0; i < physicalDeviceCount; i++)
        {
            GfxPhysicalDevice physicalDevice = GetPhysicalDevice(physicalDevices[i]);

            if (gfxPhysicalDevice.Id == physicalDevice.Id)
            {
                return physicalDevices[i];
            }
        }

        throw new InvalidOperationException($"Not Found: {gfxPhysicalDevice}");
    }

    static GfxPhysicalDevice GetPhysicalDevice(VkPhysicalDevice physicalDevice)
    {
        VkPhysicalDeviceProperties properties;
        vkGetPhysicalDeviceProperties(physicalDevice, &properties);

        GfxPhysicalDeviceType type = properties.DeviceType switch
        {
            VkPhysicalDeviceType.INTEGRATED_GPU => GfxPhysicalDeviceType.Integrated,
            VkPhysicalDeviceType.DISCRETE_GPU => GfxPhysicalDeviceType.Discrete,
            VkPhysicalDeviceType.VIRTUAL_GPU => GfxPhysicalDeviceType.Virtual,
            VkPhysicalDeviceType.CPU => GfxPhysicalDeviceType.Cpu,
            _ => GfxPhysicalDeviceType.Other
        };

        return new GfxPhysicalDevice(
            Name: new string((sbyte*)properties.DeviceName),
            Vulkan: ParseVersion(properties.ApiVersion),
            Driver: ParseVersion(properties.DriverVersion),
            Type: type,
            Id: new Guid(new ReadOnlySpan<byte>(properties.PipelineCacheUuid, 16))
        );
    }

    /// <summary>
    /// Enumerates the available physical devices.
    /// </summary>
    /// <returns></returns>
    public GfxPhysicalDevice[] EnumeratePhysicalDevices()
    {
        uint physicalDeviceCount = 0;
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, default));

        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        ThrowVulkanIfFailed(vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices));

        GfxPhysicalDevice[] devices = new GfxPhysicalDevice[(int)physicalDeviceCount];
        for (int i = 0; i < physicalDeviceCount; i++)
        {
            devices[i] = GetPhysicalDevice(physicalDevices[i]);
        }

        return devices;
    }

    /// <summary>
    /// Selects the optimal device from the given array of devices. The optimal device is a discrete GPU if available, otherwise an integrated GPU.
    /// </summary>
    /// <param name="devices"></param>
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
            logger.Warning("No discrete or integrated GPU available. Selecting the first device.");
            selectedDevice = devices[0];
        }

        return selectedDevice;
    }
}