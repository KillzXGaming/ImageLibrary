using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Utils
{
    public class ImageUtil
    {
        /// <summary>
        /// Decodes all surfaces to a raw rgba8 image with combined mipmaps and surfaces.
        /// </summary>
        /// <param name="imageInfo"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] DecodeAllSurfaces(GenericTextureBase imageInfo, byte[] data)
        {
            List<byte[]> surfaces = new List<byte[]>();
            var span = data.AsSpan();

            int ofs = 0;
            for (int a = 0; a < imageInfo.ArrayCount; a++)
            {
                for (int m = 0; m < imageInfo.MipCount; m++)
                {
                    uint w = Math.Max(1, imageInfo.Width >> m);
                    uint h = Math.Max(1, imageInfo.Height >> m);
                    var size = imageInfo.ImageFormat.GetSize(w, h);
                    if (size == 0) size = (uint)data.Length;

                    // Slice and decode each layer and mipmap
                    var sliced = span.Slice(ofs, (int)size).ToArray();
                    surfaces.Add(imageInfo.ImageFormat.Decode(sliced, w, h).Data);

                    ofs += (int)size;
                }
            }
            return ByteUtil.CombineByteArray(surfaces.ToArray());
        }

        /// <summary>
        /// Gets the rgba output with component channels applied from the texture.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="rgba"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static byte[] ProcessComponentChannels(GenericTextureBase texture, byte[] rgba, uint width, uint height)
        {
            return ProcessComponentChannels(rgba, width, height,
                texture.RedComponent, texture.GreenComponent, texture.BlueComponent, texture.AlphaComponent);
        }

        /// <summary>
        /// Gets the rgba output with component channels applied from the texture.
        /// </summary>
        /// <param name="rgba"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="redComponent"></param>
        /// <param name="greenComponent"></param>
        /// <param name="blueComponent"></param>
        /// <param name="alphaComponent"></param>
        /// <returns></returns>
        public static byte[] ProcessComponentChannels(byte[] rgba, uint width, uint height,
            TextureChannelType redComponent, 
            TextureChannelType greenComponent,
            TextureChannelType blueComponent, 
            TextureChannelType alphaComponent)
        {
            if (rgba?.Length != width * height * 4) //invalid image, skip
                return rgba;

            //Check if need to swap component channels
            if (redComponent   == TextureChannelType.Red &&
                greenComponent == TextureChannelType.Green &&
                blueComponent  == TextureChannelType.Blue &&
                alphaComponent == TextureChannelType.Alpha)
            {
                return rgba;
            }

            byte GetComponent(TextureChannelType channel, int offset)
            {
                switch (channel)
                {
                    case TextureChannelType.Red: return rgba[offset + 0];
                    case TextureChannelType.Green: return rgba[offset + 1];
                    case TextureChannelType.Blue: return rgba[offset + 2];
                    case TextureChannelType.Alpha: return rgba[offset + 3];
                    case TextureChannelType.One: return 255;
                    case TextureChannelType.Zero: return 0;
                }
                return 1;
            }

            byte[] output = new byte[width * height * 4];

            int pixelIndex = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    output[pixelIndex + 0] = GetComponent(redComponent, pixelIndex);
                    output[pixelIndex + 1] = GetComponent(greenComponent, pixelIndex);
                    output[pixelIndex + 2] = GetComponent(blueComponent, pixelIndex);
                    // Skip zero alpha and fall back to 1.0 so texture is visible
                    output[pixelIndex + 3] = GetComponent(alphaComponent == TextureChannelType.Zero ?
                        TextureChannelType.One : alphaComponent, pixelIndex);
                    pixelIndex += 4;
                }
            }
            return output;
        }

        public static byte[] EncodeMipmaps(GenericTextureBase imageInfo, TextureSurface[] surfaces)
        {
            List<byte[]> mipmaps = new List<byte[]>();
            for (int arrayLevel = 0; arrayLevel < surfaces.Length; arrayLevel++)
            {
                for (int mipLevel = 0; mipLevel < surfaces[arrayLevel].Mipmaps.Count; mipLevel++)
                {
                    uint mipWidth = Math.Max(1, imageInfo.Width >> mipLevel);
                    uint mipHeight = Math.Max(1, imageInfo.Height >> mipLevel);
                    var mipMap = surfaces[arrayLevel].Mipmaps[mipLevel];

                    var mip_encoded = imageInfo.ImageFormat.Encode(mipMap, mipWidth, mipHeight);
                    mipmaps.Add(mip_encoded);
                }
            }
            return ByteUtil.CombineByteArray(mipmaps.ToArray());
        }

        public static byte[] EncodeMipmaps(GenericTextureBase imageInfo, List<byte[]> input)
        {
            List<byte[]> mipmaps = new List<byte[]>();
            for (int mipLevel = 0; mipLevel < input.Count; mipLevel++)
            {
                uint mipWidth = Math.Max(1, imageInfo.Width >> mipLevel);
                uint mipHeight = Math.Max(1, imageInfo.Height >> mipLevel);

                var mip_encoded = imageInfo.ImageFormat.Encode(input[mipLevel], mipWidth, mipHeight);
                mipmaps.Add(mip_encoded);
            }
            return ByteUtil.CombineByteArray(mipmaps.ToArray());
        }
    }
}
