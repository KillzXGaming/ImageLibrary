using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders.Ctr
{
    public class PicaFormats
    {

        static byte[] DecodeRGB565(byte[] Input, int width, int height)
        {
            var srcOffs = 0;
            return DecodeTile(width, height, (pixels, dstOffs) =>
            {
                var value = GetUShort(Input, srcOffs);
                pixels[dstOffs + 1] = Expand6to8((byte)((value >> 5) & 0x3F));
                pixels[dstOffs + 0] = Expand5to8((byte)((value >> 11) & 0x1F));
                pixels[dstOffs + 2] = Expand5to8((byte)((value) & 0x1F));
                pixels[dstOffs + 3] = 255;
                srcOffs += 2;
            });
        }

        static byte Expand6to8(byte n)
        {
            return (byte)((n << (8 - 6)) | (n >>> (12 - 8)));
        }
        static byte Expand5to8(byte n)
        {
            return (byte)((n << (8 - 5)) | (n >>> (10 - 8)));
        }

        static ushort GetUShort(byte[] Buffer, int Address)
        {
            return (ushort)(
                Buffer[Address + 0] << 0 |
                Buffer[Address + 1] << 8);
        }

        static byte[] DecodeTile(int width, int height, Action<byte[], int> decodePixels)
        {
            byte[] pixels = new byte[width * height * 4];
            int morton7(int n)
            {
                // 0a0b0c => 000abc
                return ((n >>> 2) & 0x04) | ((n >>> 1) & 0x02) | (n & 0x01);
            }

            for (int TY = 0; TY < height; TY += 8)
            {
                for (int TX = 0; TX < width; TX += 8)
                {
                    for (int Px = 0; Px < 64; Px++)
                    {
                        var X = morton7(Px);
                        var Y = morton7(Px >>> 1);
                        var OOffs = ((TY + Y) * width + TX + X) * 4;
                        decodePixels?.Invoke(pixels, OOffs);
                    }
                }
            }
            return pixels;
        }
    }
}
