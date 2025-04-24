using Gossamer.External.Vulkan;
using Gossamer.Gfx.Presentation;
using Gossamer.Logging;

using static Gossamer.External.Vulkan.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Gfx;

class Gfx3D(GfxCore gfx) : IDisposable
{
    readonly Logger logger = Core.GetLogger(nameof(Gfx3D));

    readonly GfxCore gfx = gfx;

    DisplayParameters displayParameters = DisplayParameters.Empty;

    public void Dispose()
    {
        DestroyRendering();
    }

    public void Create()
    {
        logger.Debug();
    }

    public unsafe void Render(GfxPresenter presenter)
    {
        PixelBuffer presentationBuffer = presenter.GetPresentationBuffer();
        VkCommandBuffer commandBuffer = presenter.GetCommandBuffer();

        gfx.PixelBufferBarrier(
            commandBuffer,
            pixelBuffer: presentationBuffer,
            srcLayout: VkImageLayout.UNDEFINED,
            dstLayout: VkImageLayout.TRANSFER_DST_OPTIMAL);

        VkImageSubresourceRange clearRange = new()
        {
            AspectMask = VkImageAspect.COLOR,
            BaseMipLevel = 0,
            LevelCount = 1,
            BaseArrayLayer = 0,
            LayerCount = 1,
        };
        VkClearColorValue clearColor = VkClearColorValue.FromColor(displayParameters.ClearColor);
        vkCmdClearColorImage(commandBuffer, presentationBuffer.Image, VkImageLayout.TRANSFER_DST_OPTIMAL, &clearColor, 1, &clearRange);
    }

    void DestroyRendering()
    {
    }

    public void InitializeRendering(DisplayParameters displayParameters)
    {
        logger.Debug();
        
        this.displayParameters = displayParameters;

        DestroyRendering();
    }
}