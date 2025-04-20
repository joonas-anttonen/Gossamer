using System.Runtime.InteropServices;

using Gossamer.External.Webp;

using static Gossamer.External.Webp.Api;
using static Gossamer.Utilities.ExceptionUtilities;

namespace Gossamer.Assets;

public class AssetManager
{
    public static ImageAsset ReadFromFile(string path)
    {
        byte[] encodedData = File.ReadAllBytes(path);

        unsafe
        {
            WebPStatus status = WebPStatus.OK;

            fixed (byte* p_encodedData = encodedData)
            {
                int width = 0;
                int height = 0;
                int hasAlpha = 0;
                status = Analyze(p_encodedData, (ulong)encodedData.Length, &width, &height, &hasAlpha);
                ThrowInvalidDataIf(status != WebPStatus.OK, $"Failed to analyze image: {status}");

                byte* decoded_data = null;
                ulong decoded_data_size = 0;
                status = Decode(p_encodedData, (ulong)encodedData.Length, WebPFormat.RGBA, &decoded_data, &decoded_data_size);
                ThrowInvalidDataIf(status != WebPStatus.OK, $"Failed to decode image: {status}");

                byte[] data = new byte[decoded_data_size];
                Marshal.Copy((IntPtr)decoded_data, data, 0, (int)decoded_data_size);
                Free(decoded_data);

                return new ImageAsset(data, Gfx.GfxFormat.Rgba8, (uint)width, (uint)height);
            }
        }
    }
}