using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.Interfaces;

namespace ImageLibrary.Formats.Encoders
{
    public class LA4 : ImageEncoder
    {
        public override string ToString() => "LA4";
        public uint BitsPerPixel { get; } = 8;

        public LA4()
        {

        }

        public uint CalculateSize(uint width, uint height)
        {
            return width * height;
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            int pixelCount = (int)(width * height);
            byte[] output = new byte[width * height * 4];

            int index = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                byte packed = input[i];
                byte luminance = (byte)((packed >> 4) * 0x11);
                byte alpha = (byte)((packed & 0xF) * 0x11);

                output[index + 0] = luminance;
                output[index + 1] = luminance;
                output[index + 2] = luminance;
                output[index + 3] = alpha;
                index += 4;
            }
            return output;
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            int pixelCount = (int)(width * height);
            byte[] output = new byte[width * height];

            for (int i = 0; i < pixelCount; i++)
            {
                //luminance calculate
                int inputOffset = i * 4;

                byte luminance = L4.CalculateLuminance(input, inputOffset, 15);
                byte a = input[inputOffset + 3];

                // Convert to 4-bit
                byte alpha = (byte)(a >> 4);
                byte packed = (byte)(luminance << 4 | alpha);
                output[i] = packed;
            }
            return output;
        }
    }

    public class L4 : ImageEncoder
    {
        public override string ToString() => "L4";
        public uint BitsPerPixel { get; } = 4;

        public L4()
        {

        }

        public uint CalculateSize(uint width, uint height)
        {
            return width * height / 2;
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            int pixelCount = (int)(width * height);
            byte[] output = new byte[pixelCount * 4];

            for (int i = 0; i < pixelCount; i += 2)
            {
                int byteIndex = i / 2;
                byte packed = input[byteIndex];

                byte luminance1 = (byte)(packed >> 4 & 0xF); //high nibble 
                byte luminance2 = (byte)(packed & 0xF);        //low nibble 

                // Scale 4-bit luminance to 8-bit (0-255)
                output[i * 4 + 0] = (byte)(luminance1 * 0x11);
                output[i * 4 + 1] = (byte)(luminance1 * 0x11);
                output[i * 4 + 2] = (byte)(luminance1 * 0x11);
                output[i * 4 + 3] = 255;

                if ((i + 1) * 4 < output.Length) // Ensure not to go out of bounds
                {
                    byte lum2 = (byte)(luminance2 * 0x11);
                    int idx = (i + 1) * 4;
                    output[idx + 0] = lum2;
                    output[idx + 1] = lum2;
                    output[idx + 2] = lum2;
                    output[idx + 3] = 255;
                }
            }
            return output;
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            int pixelCount = (int)(width * height);
            byte[] output = new byte[width * height];

            for (int i = 0; i < pixelCount; i++)
            {
                // Calculate luminance for the first pixel
                byte luminance1 = CalculateLuminance(input, i * 4, 15);

                byte packed;
                if (i + 1 < pixelCount)
                {
                    // Calculate luminance for the second pixel
                    byte luminance2 = CalculateLuminance(input, i * 4, 15);
                    // Pack both pixels into one byte
                    packed = (byte)(luminance1 << 4 | luminance2);
                }
                else
                {
                    // If there's an odd number of pixels, only pack the first one
                    packed = (byte)(luminance1 << 4);
                }
                output[i / 2] = packed;
            }
            return output;
        }

        public static byte CalculateLuminance(byte[] input, int offset, byte scale)
        {
            float r1 = input[offset + 0] / 255f;
            float g1 = input[offset + 1] / 255f;
            float b1 = input[offset + 2] / 255f;
            return (byte)((0.2126f * r1 + 0.7152f * g1 + 0.0722f * b1) * scale);
        }
    }

    public class LA8 : ImageEncoder
    {
        public override string ToString() => "LA8";
        public uint BitsPerPixel { get; } = 16;

        public uint CalculateSize(uint width, uint height)
        {
            return width * height * 2;
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            byte[] output = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                int inOffset = i * 2;

                int offset = i * 4;
                output[offset + 0] = input[inOffset + 0];
                output[offset + 1] = input[inOffset + 0];
                output[offset + 2] = input[inOffset + 0];
                output[offset + 3] = input[inOffset + 1];
            }
            return output;
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            byte[] output = new byte[width * height * 2];
            for (int i = 0; i < width * height; i++)
            {
                //luminance calculate
                int inputOffset = i * 4;
                int outputOffset = i * 2;

                byte luminance = L4.CalculateLuminance(input, inputOffset, 255);

                output[outputOffset + 0] = luminance;
                output[outputOffset + 1] = input[inputOffset + 3]; //alpha
            }
            return output;
        }
    }

    public class L8 : ImageEncoder
    {
        public override string ToString() => "L8";
        public uint BitsPerPixel { get; } = 8;

        public uint CalculateSize(uint width, uint height)
        {
            return width * height;
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            byte[] output = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                int inOffset = i;

                int offset = i * 4;
                output[offset + 0] = input[inOffset];
                output[offset + 1] = input[inOffset];
                output[offset + 2] = input[inOffset];
                output[offset + 3] = 255;
            }
            return output;
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            byte[] output = new byte[width * height];
            for (int i = 0; i < width * height; i++)
                output[i] = L4.CalculateLuminance(input, i * 4, 255);
            return output;
        }
    }
}
