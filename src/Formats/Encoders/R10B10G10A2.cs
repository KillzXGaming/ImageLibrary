using ImageLibrary.Interfaces;
using ImageLibrary.Utils;
using System.Runtime.InteropServices;

namespace ImageLibrary.Formats.Encoders
{
    public class R10B10G10A2 : ImageEncoder
    {
        public uint BitsPerPixel => 32;
        public override string ToString() => "R10B10G10A2_FLOAT";

        public uint CalculateSize(uint width, uint height)
        {
            return width * height * 4u;
        }

        public byte[] Decode(byte[] data, uint width, uint height)
        {
            return ByteUtil.ConvertFloatToBytes(DecodeFloats(data, width, height));
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            return EncodeFloats(ByteUtil.ConvertBytesToFloat(data), width, height);
        }

        public float[] DecodeFloats(byte[] data, uint width, uint height)
        {
            uint pixelCount = width * height;
            float[] output = new float[pixelCount * 4];

            for (int i = 0; i < pixelCount; i++)
            {
                // 32 bpp
                uint packed = BitConverter.ToUInt32(data, (int)(i * 4));

                uint r10 = (packed >> 0) & 0x3FF;
                uint g10 = (packed >> 10) & 0x3FF;
                uint b10 = (packed >> 20) & 0x3FF;
                uint a2 = (packed >> 30) & 0x3;

                byte r8 = (byte)((r10 * 255u) / 1023u);
                byte g8 = (byte)((g10 * 255u) / 1023u);
                byte b8 = (byte)((b10 * 255u) / 1023u);
                byte a8 = (byte)((a2 * 255u) / 3u);

                int baseIdx = i * 4;

                output[baseIdx + 0] = r10 / 1023f;
                output[baseIdx + 1] = g10 / 1023f;
                output[baseIdx + 2] = b10 / 1023f;
                output[baseIdx + 3] = a2 / 3f;
            }
            return output;
        }

        public byte[] EncodeFloats(float[] data, uint width, uint height)
        {
            uint pixelCount = width * height;
            byte[] output = new byte[CalculateSize(width, height)];
            var dst = MemoryMarshal.Cast<byte, uint>(output);

            for (int i = 0; i < pixelCount; i++)
            {
                int baseIdx = i * 4;

                float r = Math.Clamp(data[baseIdx + 0], 0f, 1f);
                float g = Math.Clamp(data[baseIdx + 1], 0f, 1f);
                float b = Math.Clamp(data[baseIdx + 2], 0f, 1f);
                float a = Math.Clamp(data[baseIdx + 3], 0f, 1f);

                uint r10 = (uint)MathF.Round(r * 1023f);
                uint g10 = (uint)MathF.Round(g * 1023f);
                uint b10 = (uint)MathF.Round(b * 1023f);
                uint a2 = (uint)MathF.Round(a * 3f);

                uint packed = (r10 & 0x3FFu) |
                             ((g10 & 0x3FFu) << 10) |
                             ((b10 & 0x3FFu) << 20) |
                             ((a2 & 0x03u) << 30);

                dst[i] = packed;
            }
            return output;
        }
    }
}
