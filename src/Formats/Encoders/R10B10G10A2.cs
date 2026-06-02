using ImageLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            uint pixelCount = width * height;
            byte[] output = new byte[pixelCount * 4];

            for (uint i = 0; i < pixelCount; i++)
            {
                // 32 bpp
                uint packed = BitConverter.ToUInt32(data, (int)(i * 4));

                uint r10 = (packed >> 0) & 0x3FF;
                uint g10 = (packed >> 10) & 0x3FF;
                uint b10 = (packed >> 20) & 0x3FF;
                uint a2 = (packed >> 30) & 0x3;

                // Scale from 1023 to 255
                // Todo float formats need a way to decode as raw float
                byte r8 = (byte)((r10 * 255u) / 1023u);
                byte g8 = (byte)((g10 * 255u) / 1023u);
                byte b8 = (byte)((b10 * 255u) / 1023u);
                byte a8 = (byte)((a2 * 255u) / 3u);

                output[i * 4 + 0] = r8;
                output[i * 4 + 1] = g8;
                output[i * 4 + 2] = b8;
                output[i * 4 + 3] = a8;
            }
            return output;
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            uint pixelCount = width * height;
            byte[] output = new byte[pixelCount * 4];

            for (uint i = 0; i < pixelCount; i++)
            {
                byte r8 = data[i * 4 + 0];
                byte g8 = data[i * 4 + 1];
                byte b8 = data[i * 4 + 2];
                byte a8 = data[i * 4 + 3];

                uint r10 = (uint)((r8 * 1023u) / 255u);
                uint g10 = (uint)((g8 * 1023u) / 255u);
                uint b10 = (uint)((b8 * 1023u) / 255u);
                uint a2 = (uint)((a8 * 3u) / 255u);

                // Pack to 32 bits
                uint packed = (r10 & 0x3FF) |
                             ((g10 & 0x3FF) << 10) |
                             ((b10 & 0x3FF) << 20) |
                             ((a2 & 0x3) << 30);

                byte[] bytes = BitConverter.GetBytes(packed);
                output[i * 4 + 0] = bytes[0];
                output[i * 4 + 1] = bytes[1];
                output[i * 4 + 2] = bytes[2];
                output[i * 4 + 3] = bytes[3];
            }
            return output;
        }
    }
}
