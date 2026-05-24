using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Helpers
{
    public class ImageHelper
    {
        public static byte[] ReplaceChannel(byte[] src, byte[] dst, int channel)
        {
            int index = 0;
            for (int i = 0; i < src.Length / 4; i++)
            {
                src[index + channel] = dst[index];
                index += 4;
            }
            return src;
        }

        public static byte[] ConvertBgraToRgba(byte[] bytes)
        {
            if (bytes == null)
                throw new Exception("Data block returned null.");

            for (int i = 0; i < bytes.Length; i += 4)
            {
                var temp = bytes[i];
                bytes[i] = bytes[i + 2];
                bytes[i + 2] = temp;
            }
            return bytes;
        }
        public static byte[] SwapChannelComponents(byte[] rgba,
            TextureChannelType red,
            TextureChannelType green,
            TextureChannelType blue,
            TextureChannelType alpha)
        {
            byte[] output = new byte[rgba.Length];

            byte GetChannel(TextureChannelType type, int index)
            {
                switch (type)
                {
                    case TextureChannelType.Red: return rgba[index + 0];
                    case TextureChannelType.Green: return rgba[index + 1];
                    case TextureChannelType.Blue: return rgba[index + 2];
                    case TextureChannelType.Alpha: return rgba[index + 3];
                    case TextureChannelType.One: return 255;
                    default:
                        return 0;
                }
            }

            int index = 0;
            for (int i = 0; i < rgba.Length / 4; i++)
            {
                output[index + 0] = GetChannel(red, index);
                output[index + 1] = GetChannel(green, index);
                output[index + 2] = GetChannel(blue, index);
                output[index + 3] = GetChannel(alpha, index);
                index += 4;
            }
            return output;
        }

        public static byte[] GetChannel(byte[] rgba, int channelIdx)
        {
            byte[] output = new byte[rgba.Length];

            int index = 0;
            for (int i = 0; i < rgba.Length / 4; i++)
            {
                output[index + 0] = rgba[index + channelIdx];
                output[index + 1] = rgba[index + channelIdx];
                output[index + 2] = rgba[index + channelIdx];
                output[index + 3] = 255;
                index += 4;
            }
            return output;
        }

        public static byte[] SetGamma(byte[] rgba, int width, int height, float gamma)
        {
            // Precompute the gamma correction lookup table to save computation time
            byte[] gammaLookup = new byte[256];
            for (int i = 0; i < 256; i++)
                gammaLookup[i] = (byte)(Math.Pow(i / 255.0, gamma) * 255);

            byte[] output = new byte[rgba.Length];

            // Process each pixel (assuming 4 bytes per pixel in RGBA format)
            for (int i = 0; i < rgba.Length; i += 4)
            {
                output[i] = gammaLookup[rgba[i]];     // Red
                output[i + 1] = gammaLookup[rgba[i + 1]]; // Green
                output[i + 2] = gammaLookup[rgba[i + 2]]; // Blue
                output[i + 3] = rgba[i + 3];
            }
            return output;
        }

        public static byte[] GetColorOnly(byte[] rgba)
        {
            byte[] output = new byte[rgba.Length];

            int index = 0;
            for (int i = 0; i < rgba.Length / 4; i++)
            {
                output[index + 0] = rgba[index + 0];
                output[index + 1] = rgba[index + 1];
                output[index + 2] = rgba[index + 2];
                output[index + 3] = 255;
                index += 4;
            }
            return output;
        }
    }
}
