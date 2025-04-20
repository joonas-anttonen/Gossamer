using System.Diagnostics;

using Gossamer.External.Vulkan;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx;

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
        ThrowInvalidOperationIfNot(cpuCount < capacity);

        cpuTimestamps[cpuCount++] = cpuStopwatch.Elapsed;
        return cpuCount - 1;
    }

    internal uint EndCpuTimestamp()
    {
        ThrowInvalidOperationIfNot(cpuCount < capacity);

        cpuTimestamps[cpuCount++] = cpuStopwatch.Elapsed;
        return cpuCount - 1;
    }

    internal uint BeginGpuTimestamp(VkCommandBuffer commandBuffer)
    {
        ThrowInvalidOperationIfNot(gpuCount < capacity);

        vkCmdWriteTimestamp(commandBuffer, VkPipelineStage.TOP_OF_PIPE, queryPool, gpuCount);
        gpuCount++;
        return gpuCount - 1;
    }

    internal uint EndGpuTimestamp(VkCommandBuffer commandBuffer)
    {
        ThrowInvalidOperationIfNot(gpuCount < capacity);

        vkCmdWriteTimestamp(commandBuffer, VkPipelineStage.BOTTOM_OF_PIPE, queryPool, gpuCount);
        gpuCount++;
        return gpuCount - 1;
    }

    internal TimeSpan GetGpuDuration(uint start, uint end)
    {
        ThrowInvalidOperationIfNot(start < capacity);
        ThrowInvalidOperationIfNot(end < capacity);

        ulong startTimestamp = gpuTimestamps[start];
        ulong endTimestamp = gpuTimestamps[end];

        return TimeSpan.FromSeconds((endTimestamp - startTimestamp) * deviceTimestampPeriodInSeconds);
    }

    internal TimeSpan GetCpuDuration(uint start, uint end)
    {
        ThrowInvalidOperationIfNot(start < capacity);
        ThrowInvalidOperationIfNot(end < capacity);

        return cpuTimestamps[end] - cpuTimestamps[start];
    }
}