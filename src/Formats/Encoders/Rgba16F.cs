using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.Interfaces;

namespace ImageLibrary.Formats.Encoders
{
    /// <summary>
    /// An rgba encoder/decoder.
    /// </summary>
    public class Rgba16F : ImageEncoder
    {
        public uint BitsPerPixel => 64;
        public uint BlockWidth { get; } = 1;
        public uint BlockHeight { get; } = 1;
        public uint BlockDepth { get; } = 1;

        public uint CalculateSize(uint width, uint height)
        {
            return width * height * 16;
        }

        public byte[] Decode(byte[] data, uint width, uint height)
        {
            if (data.Length == 0) return new byte[width * height * 4];

            byte[] output = new byte[width * height * 4];
            Decode<byte>(data, output, DataType.Float, DataFormat.Bit16, ChannelLayout.RGBA);
            return output;
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            int bitsPerPixel = 16 * 4;
            int bytesPerPixel = (bitsPerPixel + 7) / 8;

            byte[] output = new byte[width * height * bytesPerPixel];
            Encode<byte>(data, output, DataType.Float, DataFormat.Bit16, ChannelLayout.RGBA);
            return output;
        }

        public static void Decode<T>(ReadOnlySpan<byte> src, Span<T> dst, 
            DataType dataType,
            DataFormat format,
            ChannelLayout layout) where T : unmanaged
        {
            int channels = GetChannelCount(layout);
            int bytesPerChannel = (format == DataFormat.Bit16) ? 2 : 4;
            int pixels = src.Length / (bytesPerChannel * channels);

           // if (dst.Length < pixels * channels)
            //    throw new ArgumentException("Destination buffer too small.");

            var srcU16 = MemoryMarshal.Cast<byte, ushort>(src);

            if (format == DataFormat.Bit16)
            {
                for (int i = 0; i < pixels * channels; i++)
                    dst[i] = DataConverterHelper.Convert16<T>(srcU16[i], dataType);
            }
            else
            {
                for (int i = 0; i < pixels * channels; i++)
                    dst[i] = DataConverterHelper.Convert32<T>(srcU16[i], dataType);
            }
        }

        public static void Encode<T>(ReadOnlySpan<byte> src, Span<byte> dst,
                DataType dataType,
                DataFormat format,
                ChannelLayout layout) where T : unmanaged
        {
            int channels = GetChannelCount(layout);
            int pixels = src.Length / channels;
            int bytesPerChannel = (format == DataFormat.Bit16) ? 2 : 4;

            if (dst.Length < pixels * channels * bytesPerChannel)
                throw new ArgumentException("Destination buffer too small.");


            if (format == DataFormat.Bit16)
            {
                var dstU16 = MemoryMarshal.Cast<byte, ushort>(dst);
                for (int i = 0; i < src.Length; i++)
                    dstU16[i] = DataConverterHelper.Encode16(src[i], dataType);
            }
            else
            {
                var dstU32 = MemoryMarshal.Cast<byte, uint>(dst);
                for (int i = 0; i < pixels * channels; i++)
                    dstU32[i] = DataConverterHelper.Encode32(src[i], dataType);
            }
        }

        private static int GetChannelCount(ChannelLayout format)
        {
            switch (format)
            {
                case ChannelLayout.R:    return 1;
                case ChannelLayout.RG:   return 2;
                case ChannelLayout.RGBA: return 4;
                default:
                    throw new NotSupportedException(format.ToString());
            }       
        }
    }
}
