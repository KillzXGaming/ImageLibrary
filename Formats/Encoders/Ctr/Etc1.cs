using ImageLibrary.Interfaces;
using ImageLibrary.PlatformSwizzle.Algorithms.Ctr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders.Ctr
{
    public class Etc1 : ImageEncoder
    {
        public uint BitsPerPixel => HasAlpha ? 8u : 4u;

        public bool HasAlpha { get; }

        public Etc1(bool useAlpha)
        {
            HasAlpha = useAlpha;
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            return ETC1TextureCompression.Decode(input, (int)width, (int)height, HasAlpha);
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            return ETC1TextureCompression.Encode(input, (int)width, (int)height, HasAlpha);
        }

        public uint CalculateSize(uint width, uint height)
        {
            if (HasAlpha)
                return width * height / 2; //4 bits per pixel
            else
                return width * height;
        }
    }
}