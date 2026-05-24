using ImageLibrary.Interfaces;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders
{
    internal class RGB9E5 : ImageEncoder
    {
        public uint BitsPerPixel => 32;

        public uint CalculateSize(uint width, uint height) {
            return width * height * 4; 
        }

        public byte[] Decode(byte[] data, uint width, uint height)
        {
            var pixelCount = width * height;
            var output = new byte[pixelCount * 4];

            int pixelIndex = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                uint packed = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(i * 4));
                ushort r = (ushort)(packed & 0x1FF);
                ushort g = (ushort)((packed >> 9) & 0x1FF);
                ushort b = (ushort)((packed >> 18) & 0x1FF);
                byte e = (byte)((packed >> 27) & 0x1F);

                float scale = MathF.Pow(2f, e - 24);

                float rf = r * scale;
                float gf = g * scale;
                float bf = b * scale;

                output[pixelIndex + 0] = (byte)(Math.Clamp(rf, 0, 1) * 255);
                output[pixelIndex + 1] = (byte)(Math.Clamp(gf, 0, 1) * 255);
                output[pixelIndex + 2] = (byte)(Math.Clamp(bf, 0, 1) * 255);
                output[pixelIndex + 3] = 255;

                pixelIndex += 4;
            }
            return output;
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            var pixelCount = width * height;
            var result = new byte[pixelCount * 4];

            for (int i = 0; i < pixelCount; i++)
            {
                float r = BitConverter.ToSingle(data, i * 12 + 0);
                float g = BitConverter.ToSingle(data, i * 12 + 4);
                float b = BitConverter.ToSingle(data, i * 12 + 8);

                float maxRGB = MathF.Max(r, MathF.Max(g, b));
                if (maxRGB < 1e-6f)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(i * 4), 0);
                    continue;
                }

                int e;
                float scale = Frexp(maxRGB, out e); // maxRGB = scale * 2^e, scale in [0.5, 1)
                e = e + 24;
                if (e < 0) e = 0;
                if (e > 31) e = 31;

                float denom = MathF.Pow(2f, e - 24);
                ushort ri = (ushort)MathF.Round(r / denom);
                ushort gi = (ushort)MathF.Round(g / denom);
                ushort bi = (ushort)MathF.Round(b / denom);

                ri = (ushort)Math.Min(ri, (ushort)0x1FF);
                gi = (ushort)Math.Min(gi, (ushort)0x1FF);
                bi = (ushort)Math.Min(bi, (ushort)0x1FF);

                uint packed = (uint)(ri | (gi << 9) | (bi << 18) | (e << 27));
                BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(i * 4), packed);
            }

            return result;
        }

        public static float Frexp(float value, out int exponent)
        {
            if (value == 0.0f)
            {
                exponent = 0;
                return 0.0f;
            }

            int bits = BitConverter.SingleToInt32Bits(value);
            exponent = ((bits >> 23) & 0xFF) - 127 + 1;
            bits &= ~(0xFF << 23);
            bits |= (127 - 1) << 23;
            return BitConverter.Int32BitsToSingle(bits);
        }
    }
}
