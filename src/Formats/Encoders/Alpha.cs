using ImageLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders
{
    public class A8 : ImageEncoder
    {
        public uint BitsPerPixel { get; } = 8;

        public uint CalculateSize(uint width, uint height)
        {
            return width * height * 1;
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            byte[] output = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                int inOffset = i * 1;

                int offset = i * 4;
                output[offset + 0] = input[inOffset];
                output[offset + 1] = input[inOffset];
                output[offset + 2] = input[inOffset];
                output[offset + 3] = input[inOffset];
            }
            return output;
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            byte[] output = new byte[width * height * 1];
            for (int i = 0; i < width * height; i++)
            {
                //luminance calculate
                int inputOffset = i * 4;
                output[i] = input[inputOffset + 3]; //alpha
            }
            return output;
        }
    }

    public class A4 : ImageEncoder
    {
        public uint BitsPerPixel { get; } = 4;

        public A4()
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

                output[i * 4 + 0] = 255;
                output[i * 4 + 1] = 255;
                output[i * 4 + 2] = 255;
                output[i * 4 + 3] = (byte)(luminance1 * 0x11);

                if ((i + 1) * 4 < output.Length) // Ensure not to go out of bounds
                {
                    byte lum2 = (byte)(luminance2 * 0x11);

                    int idx = (i + 1) * 4;
                    output[idx + 0] = 255;
                    output[idx + 1] = 255;
                    output[idx + 2] = 255;
                    output[idx + 3] = lum2;
                }
            }
            return output;
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            int pixelCount = (int)(width * height);
            byte[] output = new byte[(pixelCount + 1) / 2];

            for (int i = 0; i < pixelCount; i++)
            {
                // Calculate luminance for the first pixel
                byte alpha1 = (byte)(input[i * 4 + 3] >> 4);

                byte packed;
                if (i + 1 < pixelCount)
                {
                    // Calculate luminance for the second pixel
                    byte alpha2 = (byte)(input[(i + 1) * 4 + 3] >> 4); // low nibble
                                                                       // Pack both pixels into one byte
                    packed = (byte)((alpha1 << 4) | alpha2);
                }
                else
                {
                    // If there's an odd number of pixels, only pack the first one
                    packed = (byte)(alpha1 << 4);
                }
                output[i / 2] = packed;
            }
            return output;
        }

        public static byte CalculateLuminance(byte[] input, int offset, byte scale)
        {
            float r1 = input[offset + 3] / 255f;
            float g1 = input[offset + 3] / 255f;
            float b1 = input[offset + 3] / 255f;
            return (byte)((0.2126f * r1 + 0.7152f * g1 + 0.0722f * b1) * scale);
        }
    }
}
