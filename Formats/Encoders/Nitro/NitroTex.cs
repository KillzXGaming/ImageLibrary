using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders.Nitro
{
    //All ported from
    //https://github.com/magcius/noclip.website/blob/e5c302ff52ad72429e5d0dc64062420546010831/src/SuperMario64DS/nitro_tex.ts
    public class NitroTex
    {
        public enum NitroTexFormat
        {
            None = 0x00,
            A3I5 = 0x01,
            Palette4 = 0x02,
            Palette16 = 0x03,
            Palette256 = 0x04,
            CMPR_4x4 = 0x05,
            A5I3 = 0x06,
            Direct = 0x07,
        }

        static int s3tcblend(int a, int b)
        {
            return (a << 1) + a + (b << 2) + b >> 3;
        }

        static byte expand3to8(int n)
        {
            return (byte)(n << 8 - 3 | n << 8 - 6 | n >> 9 - 8);
        }

        static byte expand5to8(int n)
        {
            return (byte)(n << 8 - 5 | n >> 10 - 8);
        }

        public static void bgr5(byte[] pixels, int dstOffs, int p)
        {
            pixels[dstOffs + 0] = expand5to8(p & 0x1F);
            pixels[dstOffs + 1] = expand5to8(p >> 5 & 0x1F);
            pixels[dstOffs + 2] = expand5to8(p >> 10 & 0x1F);
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
                    var dstOffs = 4 * (y * width + x);
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
                        var palIdx = texBlock & 0x03;
                        var p = palette.GetUshort(palIdx * 2, true);
                        var dstOffs = 4 * (y * width + xx + x);
                        bgr5(output, dstOffs, p);
                        output[dstOffs + 3] = (byte)(palIdx == 0 ? color0 ? 0x00 : 0xFF : 0xFF);
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
                        var palIdx = texBlock & 0x0F;
                        var p = palette.GetUshort(palIdx * 2, true);
                        var dstOffs = 4 * (y * width + xx + x);
                        bgr5(output, dstOffs, p);
                        output[dstOffs + 3] = (byte)(palIdx == 0 ? color0 ? 0x00 : 0xFF : 0xFF);
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
                    var dstOffs = 4 * (y * width + xx);
                    bgr5(output, dstOffs, p);
                    output[dstOffs + 3] = (byte)(palIdx == 0 ? color0 ? 0x00 : 0xFF : 0xFF);
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
                    var dstOffs = 4 * (y * width + x);
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
                    var dstOffs = 4 * (y * width + x);
                    bgr5(output, dstOffs, p);
                    output[dstOffs + 3] = 0xFF;
                    srcOffs += 2;
                }
            }
            return output;
        }

        static byte[] Decode_CMPR_4x4(int width, int height,
            Span<byte> data, Span<byte> paletteIdx, byte[] palette)
        {
            byte[] BuildColorTable(ushort palBlock)
            {
                int palMode = palBlock >> 14;
                int palOffs = (palBlock & 0x3FFF) << 2;

                var colorTable = new byte[16]; // 4 colors × RGBA

                ushort p0 = GetPal16(palOffs + 0x00, palette);
                Bgr5(colorTable, 0, p0);
                colorTable[3] = 0xFF;

                ushort p1 = GetPal16(palOffs + 0x02, palette);
                Bgr5(colorTable, 4, p1);
                colorTable[7] = 0xFF;

                if (palMode == 0)
                {
                    // PTY=0, A=0
                    ushort p2 = GetPal16(palOffs + 0x04, palette);
                    Bgr5(colorTable, 8, p2);
                    colorTable[11] = 0xFF;
                    // Color4 transparent black
                }
                else if (palMode == 1)
                {
                    // PTY=1, A=0
                    colorTable[8] = (byte)((colorTable[0] + colorTable[4]) >> 1);
                    colorTable[9] = (byte)((colorTable[1] + colorTable[5]) >> 1);
                    colorTable[10] = (byte)((colorTable[2] + colorTable[6]) >> 1);
                    colorTable[11] = 0xFF;
                }
                else if (palMode == 2)
                {
                    // PTY=0, A=1
                    ushort p2 = GetPal16(palOffs + 0x04, palette);
                    Bgr5(colorTable, 8, p2);
                    colorTable[11] = 0xFF;

                    ushort p3 = GetPal16(palOffs + 0x06, palette);
                    Bgr5(colorTable, 12, p3);
                    colorTable[15] = 0xFF;
                }
                else
                {
                    colorTable[8] = S3tcBlend(colorTable[4], colorTable[0]);
                    colorTable[9] = S3tcBlend(colorTable[5], colorTable[1]);
                    colorTable[10] = S3tcBlend(colorTable[6], colorTable[2]);
                    colorTable[11] = 0xFF;

                    colorTable[12] = S3tcBlend(colorTable[0], colorTable[4]);
                    colorTable[13] = S3tcBlend(colorTable[1], colorTable[5]);
                    colorTable[14] = S3tcBlend(colorTable[2], colorTable[6]);
                    colorTable[15] = 0xFF;
                }

                return colorTable;
            }

            var pixels = new byte[width * height * 4];
            int srcOffs = 0;

            for (int yy = 0; yy < height; yy += 4)
            {
                for (int xx = 0; xx < width; xx += 4)
                {
                    uint texBlock = BitConverter.ToUInt32(data.Slice(srcOffs * 4, 4));
                    ushort palBlock = BitConverter.ToUInt16(paletteIdx.Slice(srcOffs * 2, 2));

                    var colorTable = BuildColorTable(palBlock);

                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int colorIdx = (int)(texBlock & 0x03);
                            int dstOffs = 4 * (((yy + y) * width) + xx + x);

                            pixels[dstOffs + 0] = colorTable[colorIdx * 4 + 0];
                            pixels[dstOffs + 1] = colorTable[colorIdx * 4 + 1];
                            pixels[dstOffs + 2] = colorTable[colorIdx * 4 + 2];
                            pixels[dstOffs + 3] = colorTable[colorIdx * 4 + 3];

                            texBlock >>= 2;
                        }
                    }

                    srcOffs++;
                }
            }

            return pixels;
        }
        private static ushort GetPal16(int offs, Span<byte> palette)
        {
            if (offs < palette.Length)
                return BitConverter.ToUInt16(palette.Slice(offs, 2));
            return 0;
        }

        private static void Bgr5(byte[] pixels, int dstOffs, ushort p)
        {
            pixels[dstOffs + 0] = Expand5To8(p & 0x1F);        // R
            pixels[dstOffs + 1] = Expand5To8((p >> 5) & 0x1F);  // G
            pixels[dstOffs + 2] = Expand5To8((p >> 10) & 0x1F); // B
        }

        private static byte Expand5To8(int v)
        {
            return (byte)((v << 3) | (v >> 2));
        }
        private static byte S3tcBlend(byte a, byte b)
        {
            return (byte)((((a << 1) + a) + ((b << 2) + b)) >> 3);
        }

        public static byte[] DecodeTexture(int width, int height,
            NitroTexFormat format, byte[] data, byte[] paletteIdx, byte[] palette, bool color0 = true)
        {
            byte[] output = new byte[width * height * 4];

            switch (format)
            {
                case NitroTexFormat.A3I5:
                    return Decode_A3I5(width, height, data, palette);
                case NitroTexFormat.A5I3:
                    return Decode_A5I3(width, height, data, palette);
                case NitroTexFormat.Palette4:
                    return Decode_Palette4(width, height, data, palette, color0);
                case NitroTexFormat.Palette16:
                    return Decode_Palette16(width, height, data, palette, color0);
                case NitroTexFormat.Palette256:
                    return Decode_Palette256(width, height, data, palette, color0);
                case NitroTexFormat.Direct:
                    return Decode_Direct(width, height, data, palette);
                case NitroTexFormat.CMPR_4x4:
                    return Decode_CMPR_4x4(width, height, data, paletteIdx, palette);
            }
            return output;
        }
    }
}