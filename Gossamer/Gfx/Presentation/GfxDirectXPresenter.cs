using Gossamer.External.Vulkan;

namespace Gossamer.Gfx.Presentation;

public sealed class GfxDirectXPresenter : GfxPresenter
{
    public override bool BeginFrame()
    {
        return false;
    }

    public override void EndFrame()
    {
    }

    internal override VkFormat GetFormat()
    {
        return VkFormat.UNDEFINED;
    }

    internal override VkExtent2D GetExtent()
    {
        throw new NotImplementedException();
    }

    public override PixelBuffer GetPresentationBuffer()
    {
        throw new NotImplementedException();
    }

    public override void Invalidate(uint width, uint height)
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
