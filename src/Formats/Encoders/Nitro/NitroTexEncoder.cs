using BCnEncoder.Shared;
using ImageLibrary.Formats.Encoders.Gcn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders.Nitro
{
    internal class NitroTexEncoder
    {
        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode(ImageFormatDS.NitroTexFormat format, 
            byte[] rgba, int width, int height, bool color0Transparent = false)
        {
            switch (format)
            {
                case ImageFormatDS.NitroTexFormat.Palette256:
                    return Encode_Palette256(rgba, width, height, color0Transparent);
                case ImageFormatDS.NitroTexFormat.Palette16:
                    return Encode_Palette16(rgba, width, height, color0Transparent);
                case ImageFormatDS.NitroTexFormat.Palette4:
                    return Encode_Palette4(rgba, width, height, color0Transparent);
                case ImageFormatDS.NitroTexFormat.Direct:
                    return Encode_Direct(rgba, width, height);
                case ImageFormatDS.NitroTexFormat.A3I5:
                    return Encode_A3I5(rgba, width, height);
                case ImageFormatDS.NitroTexFormat.A5I3:
                    return Encode_A5I3(rgba, width, height);
                case ImageFormatDS.NitroTexFormat.CMPR_4x4:
                    return NitroCMPR_4x4.Encode(rgba, width, height);
                default:
                    throw new NotSupportedException($"{format} not supported for encoding!");
            }
        }

        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode_Direct(byte[] rgba, int width, int height)
        {
            byte[] output = new byte[width * height * 2];
            int dstOffset = 0;

            for (int i = 0; i < rgba.Length; i += 4)
            {
                byte a = rgba[i + 3];
                byte r = rgba[i + 2];
                byte g = rgba[i + 1];
                byte b = rgba[i + 0];

                ushort bgr5551 = RgbaToBgr5551(r, g, b, a);

                SetUshort(output, dstOffset, bgr5551); 
                dstOffset += 2;
            }

            return (output, new byte[0], new byte[0]);
        }

        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode_Palette256(byte[] rgba, int width, int height, bool color0Transparent = false)
        {
            var (indices, rawPalette) = BuildPaletteAndIndices(rgba, 256, color0Transparent);

            byte[] output = new byte[width * height];
            for (int i = 0; i < width * height; i++)
                output[i] = indices[i];

            return (output, rawPalette, new byte[0]);
        }

        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode_Palette16(byte[] rgba, int width, int height, bool color0Transparent)
        {
            var (indices, rawPalette) = BuildPaletteAndIndices(rgba, 16, color0Transparent);

            uint numBlocksW = (uint)((width + 3) / 4);
            byte[] output = new byte[numBlocksW * height * 2];

            int srcIdx = 0;
            int dstIdx = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    ushort block = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        if (x + i < width)
                            block |= (ushort)((indices[srcIdx++] & 0x0F) << (i * 4));
                        else
                            srcIdx++; // padding
                    }
                    SetUshort(output, dstIdx, block);
                    dstIdx += 2;
                }
            }

            return (output, rawPalette, new byte[0]);
        }

        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode_Palette4(byte[] rgba, int width, int height, bool color0Transparent)
        {
            var (indices, rawPalette) = BuildPaletteAndIndices(rgba, 4, color0Transparent);

            uint numBlocksW = (uint)((width + 7) / 8);
            byte[] output = new byte[numBlocksW * height * 2];

            int srcIdx = 0;
            int dstIdx = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += 8)
                {
                    ushort block = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (x + i < width)
                            block |= (ushort)((indices[srcIdx++] & 0x03) << (i * 2));
                        else
                            srcIdx++;
                    }
                    SetUshort(output, dstIdx, block);
                    dstIdx += 2;
                }
            }
            return (output, rawPalette, new byte[0]);
        }

        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode_A3I5(byte[] rgbaData, int width, int height, bool color0Transparent = true)
        {
            var (indices, rawPalette) = BuildPaletteAndIndices(rgbaData, 32, color0Transparent);
            byte[] texData = new byte[width * height];

            for (int i = 0; i < rgbaData.Length; i += 4)
            {
                int pixelIdx = i / 4;
                byte palIndex = indices[pixelIdx]; // 0-31
                byte alpha = rgbaData[i + 3];
                // 3-bit alpha
                byte alpha3 = (byte)(alpha >> 5);
                texData[pixelIdx] = (byte)((alpha3 << 5) | (palIndex & 0x1F));
            }
            return (texData, rawPalette, new byte[0]);
        }

        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode_A5I3(byte[] rgbaData, int width, int height, bool color0Transparent = true)
        {
            // 3 bits = 8 colors
            var (indices, rawPalette) = BuildPaletteAndIndices(rgbaData, 8, color0Transparent);
            byte[] texData = new byte[width * height];

            for (int i = 0; i < rgbaData.Length; i += 4)
            {
                int pixelIdx = i / 4;
                byte palIndex = indices[pixelIdx];
                byte alpha = rgbaData[i + 3];
                // 5-bit alpha
                byte alpha5 = (byte)(alpha >> 3);
                texData[pixelIdx] = (byte)((alpha5 << 3) | (palIndex & 0x03));
            }

            return (texData, rawPalette, new byte[0]);
        }

        internal static (byte[] indices, byte[] rawPalette) BuildPaletteAndIndices(
                byte[] rgbaData, int maxColors, bool color0Transparent)
        {
            List<Color32> uniqueColors = new List<Color32>();
            Dictionary<Color32, byte> colorToIndex = new Dictionary<Color32, byte>();
            for (int i = 0; i < rgbaData.Length; i += 4)
            {
                var col = new Color32(
                    rgbaData[i + 2],
                    rgbaData[i + 1],
                    rgbaData[i + 0],
                    rgbaData[i + 3]
                );

                if (!colorToIndex.ContainsKey(col) && uniqueColors.Count < maxColors)
                {
                    colorToIndex[col] = (byte)uniqueColors.Count;
                    uniqueColors.Add(col);
                }
            }

            byte[] palette = new byte[maxColors * 2];
            for (int i = 0; i < uniqueColors.Count; i++)
            {
                var c = uniqueColors[i];
                ushort bgr555 = RgbaToBgr5551(c.R, c.G, c.B);

                if (color0Transparent && i == 0)
                    bgr555 = 0; // transparent black

                SetUshort(palette, i * 2, bgr555);
            }

            // Build index map
            byte[] indices = new byte[rgbaData.Length / 4];
            for (int i = 0; i < indices.Length; i++)
            {
                var col = new Color32(
                    rgbaData[i * 4 + 2],
                    rgbaData[i * 4 + 1],
                    rgbaData[i * 4 + 0],
                    rgbaData[i * 4 + 3]
                );
                // if failed to index color, the image has too many colors
                indices[i] = colorToIndex.ContainsKey(col) ? colorToIndex[col] : (byte)0;
            }

            return (indices, palette);
        }

        internal static ushort RgbaToBgr5551(byte r, byte g, byte b, byte a = 255)
        {
            ushort alpha = (ushort)(a >= 128 ? 1 : 0);
            return (ushort)(
                (alpha << 15) |
                ((r >> 3) << 10) |
                ((g >> 3) << 5) |
                (b >> 3));
        }

        internal static void SetUshort(byte[] arr, int offset, ushort value)
        {
            arr[offset] = (byte)value;
            arr[offset + 1] = (byte)(value >> 8);
        }
        internal static void SetUint(byte[] arr, int offset, uint value)
        {
            arr[offset] = (byte)(value >> 24);
            arr[offset + 1] = (byte)(value >> 16);
            arr[offset + 2] = (byte)(value >> 8);
            arr[offset + 3] = (byte)value;
        }
    }
}
