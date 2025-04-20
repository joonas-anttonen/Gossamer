using Gossamer.External.Vulkan;

namespace Gossamer.Gfx.Presentation;

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
