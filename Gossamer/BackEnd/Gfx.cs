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

class GfxTimestampPool
{
    internal VkQueryPool queryPool;

    readonly float deviceTimestampPeriodInSeconds;
    readonly int capacity;
    readonly ulong[] gpuTimestamps;
    readonly TimeSpan[] cpuTimestamps;
    readonly Stopwatch cpuStopwatch = Stopwatch.StartNew();
    uint gpuCount;
    uint cpuCount;

    internal GfxTimestampPool(VkQueryPool queryPool, int capacity, float deviceTimestampPeriodInNanoseconds)
    {
        this.queryPool = queryPool;
        this.capacity = capacity;
        gpuTimestamps = new ulong[capacity];
        cpuTimestamps = new TimeSpan[capacity];
        deviceTimestampPeriodInSeconds = deviceTimestampPeriodInNanoseconds / 1e9f;
    }

    internal unsafe void Reset(VkDevice device, VkCommandBuffer commandBuffer)
    {
        cpuStopwatch.Restart();

        if (gpuCount > 0)
        {
            fixed (ulong* previousTimestamps = gpuTimestamps)
            {
                ThrowVulkanIfFailed(vkGetQueryPoolResults(
                    device,
                    queryPool,
                    0,
                    gpuCount,
                    gpuCount * sizeof(ulong),
                    (nint)previousTimestamps,
                    sizeof(ulong),
                    VkQueryResultFlags.RESULT_64 | VkQueryResultFlags.RESULT_WAIT));
            }
        }

        gpuCount = 0;
        cpuCount = 0;

        vkCmdResetQueryPool(commandBuffer, queryPool, 0, (uint)capacity);
    }

    internal uint BeginCpuTimestamp()
    {
        Assert(cpuCount < capacity);

        cpuTimestamps[cpuCount++] = cpuStopwatch.Elapsed;
        return cpuCount - 1;
    }

    internal uint EndCpuTimestamp()
    {
        Assert(cpuCount < capacity);

        cpuTimestamps[cpuCount++] = cpuStopwatch.Elapsed;
        return cpuCount - 1;
    }

    internal uint BeginGpuTimestamp(VkCommandBuffer commandBuffer)
    {
        Assert(gpuCount < capacity);

        vkCmdWriteTimestamp(commandBuffer, VkPipelineStage.TOP_OF_PIPE, queryPool, gpuCount);
        gpuCount++;
        return gpuCount - 1;
    }

    internal uint EndGpuTimestamp(VkCommandBuffer commandBuffer)
    {
        Assert(gpuCount < capacity);

        vkCmdWriteTimestamp(commandBuffer, VkPipelineStage.BOTTOM_OF_PIPE, queryPool, gpuCount);
        gpuCount++;
        return gpuCount - 1;
    }

    internal TimeSpan GetGpuDuration(uint start, uint end)
    {
        Assert(start < capacity);
        Assert(end < capacity);

        ulong startTimestamp = gpuTimestamps[start];
        ulong endTimestamp = gpuTimestamps[end];

        return TimeSpan.FromSeconds((endTimestamp - startTimestamp) * deviceTimestampPeriodInSeconds);
    }

    internal TimeSpan GetCpuDuration(uint start, uint end)
    {
        Assert(start < capacity);
        Assert(end < capacity);

        return cpuTimestamps[end] - cpuTimestamps[start];
    }
}

readonly record struct GfxSingleCommand(VkCommandBuffer CommandBuffer, VkFence Fence);

record class GfxPipeline(VkPipeline Pipeline, VkPipelineLayout Layout, VkDescriptorSetLayout DescriptorLayout);

class GfxPipelineShader(string name, GfxPipelineShader.Stage[] stages)
{
    public record class Stage(VkShaderStage StageType, SafeNativeString Entrypoint, byte[] Code);

    public string Name { get; } = name;
    public Stage[] Stages { get; } = stages;
}

record class GfxPipelineParameters(
    GfxPipelineShader ShaderProgram,
    VkPushConstantRange[] PushConstants,
    VkDescriptorSetLayoutBinding[] Layout,
    VkPrimitiveTopology InputTopology,
    VkCullMode CullMode,
    VkFrontFace FrontFace,
    VkVertexInputBindingDescription[] InputBindings,
    VkVertexInputAttributeDescription[] InputAttributes,
    GfxPipelineAttachment[] Attachments,
    bool DepthTest,
    bool DepthWrite,
    VkCompareOp DepthCompareOp,
    bool Multisampling
);

record struct GfxPipelineAttachment(VkFormat Format, VkPipelineColorBlendAttachmentState Blend);

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

public unsafe class Gfx : IDisposable
{
    readonly Logger logger = Gossamer.GetLogger(nameof(Gfx));

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
    float deviceTimestampPeriodInNanoseconds;

    VkCommandPool deviceCommandPool;
    GfxTimestampPool? timestampPool;
    readonly Stopwatch globalRenderStopwatch = Stopwatch.StartNew();
    TimeSpan globalRenderTimestamp;

    VkFormat deviceDepthFormat;
    VkSampleCount deviceSampleCount;

    readonly GfxApiParameters apiParameters;
    GfxParameters? parameters;

    GfxCapabilities capabilities = new(
        CanDebug: false,
        CanSwap: false,
        CanTimestamp: false
    );

    GfxPresenter? presenter;

    Gfx2D? gfx2D;

    readonly Dictionary<string, GfxPipelineShader> cachedPipelineShaders = [];

    internal GfxSamples GetDeviceMaxSampleCount()
    {
        return (GfxSamples)deviceSampleCount;
    }

    internal Gfx2D Get2D()
    {
        ThrowInvalidOperationIfNull(gfx2D, "No 2D renderer available.");
        return gfx2D;
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

    public void Render()
    {
        AssertNotNull(presenter);
        AssertNotNull(gfx2D);

        TimeSpan globalRenderElapsed = globalRenderStopwatch.Elapsed - globalRenderTimestamp;
        globalRenderTimestamp = globalRenderStopwatch.Elapsed;

        bool canRender = presenter.BeginFrame();
        if (!canRender)
        {
            //logger.Warning("Failed to render frame.");
            return;
        }

        timestampPool?.Reset(device, presenter.GetCommandBuffer());

        TimeSpan cpuFrameTime = TimeSpan.Zero;
        TimeSpan gpuFrameTime = TimeSpan.Zero;
        if (timestampPool != null)
        {
            cpuFrameTime = timestampPool.GetCpuDuration(0, 1);
            gpuFrameTime = timestampPool.GetGpuDuration(0, 1);
        }

        timestampPool?.BeginCpuTimestamp();
        timestampPool?.BeginGpuTimestamp(presenter.GetCommandBuffer());

        Gfx2D.Statistics gfx2DStatistics = gfx2D.GetStatistics();

        gfx2D.BeginFrame();
        {
            var cmdBuffer = gfx2D.BeginCommandBuffer();
            {
                cmdBuffer.BeginBatch();
                //gfx2D.DrawRectangle(new(10, 10), new(100, 100), new Color(Color.MintyGreen, 1.0f));
                //gfx2D.DrawCircle(new(200, 200), 50, new Color(Color.White, 1.0f), 2);

                cmdBuffer.DrawRectangle(new(5, 0), new(5, 20), new Color(Color.MintyGreen, 1.0f));
                cmdBuffer.DrawRectangle(new(0, 5), new(20, 5), new Color(Color.MintyGreen, 1.0f));

                var font = gfx2D.GetFont("Arial", 12);
                cmdBuffer.DrawText($"{globalRenderTimestamp:hh\\:mm\\:ss\\.fff} GC: {GC.GetTotalPauseDuration():mm\\:ss\\.ffffff} {globalRenderElapsed:ss\\.ffffff}\nCPU: {cpuFrameTime:ss\\.ffffff}\nGPU: {gpuFrameTime:ss\\.ffffff}\n2D Draws: {gfx2DStatistics.DrawCalls} ({gfx2DStatistics.Vertices} vtx {gfx2DStatistics.Indices} idx)", new(5, 5), new Color(Color.White, 1.0f), Color.UnpackRGB(0x000000), font);

                Vector2 textPosition = new(5, 200);
                Vector2 textAvailableSize = new(400, 200);

                {
                    var textToTest = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
                    //textToTest = "Word1 woORd2 longerword3 andword4 maybeevenlongerword5 word6";
                    var textLayout = gfx2D.CreateTextLayout(textToTest,
                        textAvailableSize,
                        wordWrap: true);

                    cmdBuffer.DrawText(textLayout, textPosition, new Color(Color.White, 1.0f), Color.UnpackRGB(0x000000));
                    cmdBuffer.DrawRectangle(textPosition, textPosition + textAvailableSize, new Color(Color.HighlighterRed, 1.0f));
                    cmdBuffer.DrawRectangle(textPosition, textPosition + textLayout.Size, new Color(Color.MintyGreen, 1.0f));

                    gfx2D.DestroyTextLayout(textLayout);
                }

                //gfx2D.DrawText("t", new(300, 300), new Color(Color.White, 1.0f), Color.UnpackRGB(0x000000));
                cmdBuffer.EndBatch();

            }
            gfx2D.EndCommandBuffer(cmdBuffer);
        }
        gfx2D.EndFrame();

        timestampPool?.EndGpuTimestamp(presenter.GetCommandBuffer());
        timestampPool?.EndCpuTimestamp();

        presenter?.EndFrame();
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

    uint VulkanDebugMessageCallback(VkDebugUtilsMessageSeverityExt severity, VkDebugUtilsMessageTypeExt type, VkDebugUtilsMessengerCallbackDataExt* pCallbackData, nint pUserData)
    {
        logger.Debug($"{severity} {Utf8StringMarshaller.ConvertToManaged((byte*)pCallbackData->pMessage)}", "Vulkan", "Validation");
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
        ThrowNotSupportedIf(!capabilities.CanTimestamp, "Timestamps are not supported.");

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

    internal void UpdateDynamicBuffer<T>(MemoryBuffer<T> memoryBuffer, ReadOnlySpan<T> data) where T : unmanaged
    {
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
            GfxPipelineShader.Stage[] stages = new GfxPipelineShader.Stage[shaderProgramDefinition.Stages.Length];

            for (int i = 0; i < shaderProgramDefinition.Stages.Length; i++)
            {
                ShaderStageDefinition stageDefinition = shaderProgramDefinition.Stages[i];
                byte[] stageBytecode = new byte[stageDefinition.Size];
                Array.Copy(packageBytecode, stageDefinition.Offset, stageBytecode, 0, stageDefinition.Size);

                stages[i] = new GfxPipelineShader.Stage((VkShaderStage)stageDefinition.Stage, new SafeNativeString(stageDefinition.EntryPoint), stageBytecode);
            }

            cachedPipelineShaders[shaderProgramName] = new GfxPipelineShader(shaderProgramName, stages);
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

        deviceTimestampPeriodInNanoseconds = physicalDeviceProperties.Limits.TimestampPeriod;
        if (deviceTimestampPeriodInNanoseconds > 0)
        {
            capabilities = capabilities with { CanTimestamp = true };
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
        ThrowVulkanIf(_vkCmdPushDescriptorSetKhr == null, $"Failed to get {STR_vkCmdPushDescriptorSetKHR} function pointer.");

        if (capabilities.CanDebug && capabilities.CanTimestamp)
        {
            timestampPool = CreateTimestampPool(8);
        }

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
