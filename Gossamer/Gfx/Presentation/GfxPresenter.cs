using Gossamer.External.Vulkan;

using static Gossamer.External.Vulkan.Api;

namespace Gossamer.Gfx.Presentation;

public abstract class GfxPresenter : IDisposable
{
    public abstract bool BeginFrame();
    public abstract void EndFrame();

    protected abstract void Dispose(bool disposing);

    internal abstract VkCommandBuffer GetCommandBuffer();
    public abstract PixelBuffer GetPresentationBuffer();

    public virtual TimeSpan GetPauseDuration()
    {
        return TimeSpan.Zero;
    }

    internal abstract VkFormat GetFormat();
    internal abstract VkExtent2D GetExtent();

    /// <summary>
    /// Invalidates the presentation surface.
    /// </summary>
    /// <param name="width"> The new width of the surface. </param>
    /// <param name="height"> The new height of the surface. </param>
    public abstract void Invalidate(uint width, uint height);

    ~GfxPresenter()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    internal unsafe static void TransitionImageLayout(PixelBuffer pixelBuffer, VkCommandBuffer commandBuffer, VkImageLayout srcLayout, VkImageLayout dstLayout, VkPipelineStage2 srcStage, VkPipelineStage2 dstStage)
    {
        VkAccessFlags2 srcAccess = srcLayout switch
        {
            VkImageLayout.UNDEFINED => VkAccessFlags2.NONE,
            VkImageLayout.COLOR_ATTACHMENT_OPTIMAL => VkAccessFlags2.COLOR_ATTACHMENT_WRITE_BIT,
            VkImageLayout.TRANSFER_DST_OPTIMAL => VkAccessFlags2.TRANSFER_WRITE_BIT,
            _ => throw new NotSupportedException("Unsupported source layout."),
        };
        VkAccessFlags2 dstAccess = dstLayout switch
        {
            VkImageLayout.TRANSFER_DST_OPTIMAL => VkAccessFlags2.TRANSFER_WRITE_BIT,
            VkImageLayout.COLOR_ATTACHMENT_OPTIMAL => VkAccessFlags2.COLOR_ATTACHMENT_WRITE_BIT,
            VkImageLayout.PRESENT_SRC_KHR => VkAccessFlags2.NONE,
            _ => throw new NotSupportedException("Unsupported destination layout."),
        };

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
}
