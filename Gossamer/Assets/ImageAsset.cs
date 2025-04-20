namespace Gossamer.Assets;

public record class ImageAsset(byte[] Data, Gfx.GfxFormat Format, uint Width, uint Height);