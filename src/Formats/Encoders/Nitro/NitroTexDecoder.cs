using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders.Nitro
{
    //All ported from
    //https://github.com/magcius/noclip.website/blob/e5c302ff52ad72429e5d0dc64062420546010831/src/SuperMario64DS/nitro_tex.ts
    public class NitroTexDecoder
    {
        static int s3tcblend(int a, int b)
        {
            return (((a << 1) + a) + ((b << 2) + b)) >> 3;
        }

        static byte expand3to8(int n)
        {
            return (byte)((n << (8 - 3)) | (n << (8 - 6)) | (n >> (9 - 8)));
        }

        static byte expand5to8(int n)
        {
            return (byte)((n << (8 - 5)) | (n >> (10 - 8)));
        }

        public static void bgr5(byte[] pixels, int dstOffs, int p)
        {
            pixels[dstOffs + 0] = expand5to8(p & 0x1F);
            pixels[dstOffs + 1] = expand5to8((p >> 5) & 0x1F);
            pixels[dstOffs + 2] = expand5to8((p >> 10) & 0x1F);
        }

        static byte[] Decode_A3I5(int width, int height, byte[] data, byte[] palette)
        {
            byte[] output = new byte[width * height * 4];
            int srcOffs = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte texBlock = data[srcOffs++];
                    var palIdx = (texBlock & 0x1F) << 1;
                    var alpha = texBlock >> 5;
                    var dstOffs = 4 * ((y * width) + x);
                    var p = palette.GetUshort(palIdx, true);
                    bgr5(output, dstOffs, p);
                    output[dstOffs + 3] = expand3to8(alpha);
                }
            }
            return output;
        }

        static byte[] Decode_Palette4(int width, int height, byte[] data, byte[] palette, bool color0 = false)
        {
            byte[] output = new byte[width * height * 4];
            int srcOffs = 0;
            for (int y = 0; y < height; y++)
            {
                for (int xx = 0; xx < width; xx += 8)
                {
                    ushort texBlock = data.GetUshort(srcOffs, true);
                    srcOffs += 2;
                    for (int x = 0; x < 8; x++)
                    {
                        var palIdx = (texBlock & 0x03);
                        var p = palette.GetUshort(palIdx * 2, true);
                        var dstOffs = 4 * ((y * width) + xx + x);
                        bgr5(output, dstOffs, p);
                        output[dstOffs + 3] = (byte)(palIdx == 0 ? (color0 ? 0x00 : 0xFF) : 0xFF);
                        texBlock >>= 2;
                    }
                }
            }
            return output;
        }

        static byte[] Decode_Palette16(int width, int height, byte[] data, byte[] palette, bool color0 = false)
        {
            byte[] output = new byte[width * height * 4];
            int srcOffs = 0;
            for (int y = 0; y < height; y++)
            {
                for (int xx = 0; xx < width; xx += 4)
                {
                    ushort texBlock = data.GetUshort(srcOffs, true);
                    srcOffs += 2;
                    for (int x = 0; x < 4; x++)
                    {
                        var palIdx = (texBlock & 0x0F);
                        var p = palette.GetUshort(palIdx * 2, true);
                        var dstOffs = 4 * ((y * width) + xx + x);
                        bgr5(output, dstOffs, p);
                        output[dstOffs + 3] = (byte)(palIdx == 0 ? (color0 ? 0x00 : 0xFF) : 0xFF);
                        texBlock >>= 4;
                    }
                }
            }
            return output;
        }

        static byte[] Decode_Palette256(int width, int height, byte[] data, byte[] palette, bool color0 = false)
        {
            byte[] output = new byte[width * height * 4];
            int srcOffs = 0;
            for (int y = 0; y < height; y++)
            {
                for (int xx = 0; xx < width; xx++)
                {
                    byte palIdx = data[srcOffs++];
                    var p = palette.GetUshort(palIdx * 2, true);
                    var dstOffs = 4 * ((y * width) + xx);
                    bgr5(output, dstOffs, p);
                    output[dstOffs + 3] = (byte)(palIdx == 0 ? (color0 ? 0x00 : 0xFF) : 0xFF);
                }
            }
            return output;
        }

        static byte[] Decode_A5I3(int width, int height, byte[] data, byte[] palette)
        {
            byte[] output = new byte[width * height * 4];
            int srcOffs = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte texBlock = data[srcOffs++];
                    var palIdx = (texBlock & 0x03) << 1;
                    var alpha = texBlock >> 3;
                    var p = palette.GetUshort(palIdx, true);
                    var dstOffs = 4 * ((y * width) + x);
                    bgr5(output, dstOffs, p);
                    output[dstOffs + 3] = expand5to8(alpha);
                }
            }
            return output;
        }

        static byte[] Decode_Direct(int width, int height, byte[] data, byte[] palette)
        {
            byte[] output = new byte[width * height * 4];
            int srcOffs = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = data.GetUshort(srcOffs, true);
                    var dstOffs = 4 * ((y * width) + x);
                    bgr5(output, dstOffs, p);
                    output[dstOffs + 3] = 0xFF;
                    srcOffs += 2;
                }
            }
            return output;
        }

        static byte[] Decode_CMPR_4x4(int width, int height, byte[] data, 
            byte[] palette, byte[] paletteIdx)
        {
            byte[] output = new byte[width * height * 4];

            byte[] buildColorTable(ushort palBlock)
            {
                var palMode = palBlock >> 14;
                var palOffs = (palBlock & 0x3FFF) << 2;

                byte[] colorTable = new byte[16];

                var p0 = palette.GetUshort(palOffs + 0x00, true);
                bgr5(colorTable, 0, p0);
                colorTable[3] = 0xFF;

                var p1 = palette.GetUshort(palOffs + 0x02, true);
                bgr5(colorTable, 4, p1);
                colorTable[7] = 0xFF;
                if (palMode == 0)
                {
                    var p2 = palette.GetUshort(palOffs + 0x04, true);
                    bgr5(colorTable, 8, p2);
                    colorTable[11] = 0xFF;
                }
                else if (palMode == 1)
                {
                    colorTable[8] = (byte)((colorTable[0] + colorTable[4]) >> 1);
                    colorTable[9] = (byte)((colorTable[1] + colorTable[5]) >> 1);
                    colorTable[10] = (byte)((colorTable[2] + colorTable[6]) >> 1);
                    colorTable[11] = 0xFF;
                }
                else if (palMode == 2)
                {
                    var p2 = palette.GetUshort(palOffs + 0x04, true);
                    bgr5(colorTable, 8, p2);
                    colorTable[11] = 0xFF;

                    var p3 = palette.GetUshort(palOffs + 0x06, true);
                    bgr5(colorTable, 12, p3);
                    colorTable[15] = 0xFF;
                }
                else
                {
                    colorTable[8] = (byte)s3tcblend(colorTable[4], colorTable[0]);
                    colorTable[9] = (byte)s3tcblend(colorTable[5], colorTable[1]);
                    colorTable[10] = (byte)s3tcblend(colorTable[6], colorTable[2]);
                    colorTable[11] = 0xFF;

                    colorTable[12] = (byte)s3tcblend(colorTable[0], colorTable[4]);
                    colorTable[13] = (byte)s3tcblend(colorTable[1], colorTable[5]);
                    colorTable[14] = (byte)s3tcblend(colorTable[2], colorTable[6]);
                    colorTable[15] = 0xFF;
                }
                    return colorTable;
            }

            uint srcOffs = 0;
            for (int yy = 0; yy < height; yy += 4)
            {
                for (int xx = 0; xx < width; xx += 4)
                {
                    var texBlock = data.GetUint(srcOffs * 0x04, false);
                    var palBlock = paletteIdx.GetUshort((int)srcOffs * 0x02, true);
                    var colorTable = buildColorTable(palBlock);

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int colorIdx = (int)texBlock & 0x03;
                            int dstOffs = 4 * (((yy + y) * width) + xx + x);
                            output[dstOffs + 0] = colorTable[colorIdx * 4 + 0];
                            output[dstOffs + 1] = colorTable[colorIdx * 4 + 1];
                            output[dstOffs + 2] = colorTable[colorIdx * 4 + 2];
                            output[dstOffs + 3] = colorTable[colorIdx * 4 + 3];
                            texBlock >>= 2;
                        }
                    }
                    srcOffs++;
                }
            }
            return output;
        }

        public static byte[] DecodeTexture(int width, int height,
            ImageFormatDS.NitroTexFormat format, byte[] data, 
            byte[] palette, byte[] paletteIdx, bool color0 = true)
        {
            byte[] output = new byte[width * height * 4];

            switch (format)
            {
                case ImageFormatDS.NitroTexFormat.A3I5:
                    return Decode_A3I5(width, height, data, palette);
                case ImageFormatDS.NitroTexFormat.A5I3:
                    return Decode_A5I3(width, height, data, palette);
                case ImageFormatDS.NitroTexFormat.Palette4:
                    return Decode_Palette4(width, height, data, palette, color0);
                case ImageFormatDS.NitroTexFormat.Palette16:
                    return Decode_Palette16(width, height, data, palette, color0);
                case ImageFormatDS.NitroTexFormat.Palette256:
                    return Decode_Palette256(width, height, data, palette, color0);
                case ImageFormatDS.NitroTexFormat.Direct:
                    return Decode_Direct(width, height, data, palette);
                case ImageFormatDS.NitroTexFormat.CMPR_4x4:
                    return Decode_CMPR_4x4(width, height, data, palette, paletteIdx);
            }
            return output;
        }

        public static byte[] DecodePalette(byte[] palette)
        {
            int colorCount = palette.Length / 2; // 2 bytes per color
            byte[] rgba = new byte[colorCount * 4];

            for (int i = 0; i < colorCount; i++)
            {
                ushort color = palette.GetUshort(i * 2, true);
                bgr5(rgba, i * 4, color);
                rgba[i * 4 + 3] = 0xFF; // no alpha needed
            }
            return rgba;
        }
    }
}