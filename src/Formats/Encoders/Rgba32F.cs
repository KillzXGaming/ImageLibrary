using ImageLibrary.Interfaces;
using ImageLibrary.Utils;
using System.Runtime.InteropServices;

namespace ImageLibrary.Formats.Encoders
{
    /// <summary>
    /// An rgba encoder/decoder.
    /// </summary>
    public class Rgba32F : ImageEncoder, IImageEncoderFloat
    {
        public uint BitsPerPixel => BytesPerPixel * 8;
        public uint BlockWidth => 1;
        public uint BlockHeight => 1;
        public uint BlockDepth => 1;
        public uint BytesPerPixel => GetChannelCount(Channels) * 4;
        public ChannelLayout Channels { get; }

        public Rgba32F(ChannelLayout channels)
        {
            this.Channels = channels;
        }

        public uint CalculateSize(uint width, uint height)
            => width * height * BytesPerPixel;

        public byte[] Decode(byte[] data, uint width, uint height) 
        {
            float[] pixels = DecodeFloats(data, width, height);
            return pixels.Select(x => ByteUtil.ConvertToByte(x)).ToArray();
        }

        public byte[] Encode(byte[] data, uint width, uint height) 
        {
            return EncodeFloats(data.Select(x => (float)x / 255.0f).ToArray(), width, height);
        }

        public float[] DecodeFloats(byte[] data, uint width, uint height)
        {
            if (data == null || data.Length == 0)
                return new float[width * height * 4];

            var channelCount = (int)GetChannelCount(this.Channels);
            float[] output = new float[width * height * 4];

            var srcFloats = MemoryMarshal.Cast<byte, float>(data);
            int srcIndex = 0;
            int dstIndex = 0;

            for (int i = 0; i < width * height; i++)
            {
                output[dstIndex + 0] = channelCount >= 1 ? (float)srcFloats[srcIndex] : 0f;
                output[dstIndex + 1] = channelCount >= 2 ? (float)srcFloats[srcIndex + 1] : 0f;
                output[dstIndex + 2] = channelCount >= 3 ? (float)srcFloats[srcIndex + 2] : 0f;
                output[dstIndex + 3] = channelCount >= 4 ? (float)srcFloats[srcIndex + 3] : 1f;

                srcIndex += (int)channelCount;
                dstIndex += 4;
            }

            return output;
        }

        public byte[] EncodeFloats(float[] data, uint width, uint height)
        {
            byte[] output = new byte[CalculateSize(width, height)];
            var dstFloats = MemoryMarshal.Cast<byte, float>(output);

            var channelCount = (int)GetChannelCount(this.Channels);

            int srcIndex = 0;
            int dstIndex = 0;
            for (int i = 0; i < width * height; i++)
            {
                if (channelCount >= 1) dstFloats[dstIndex + 0] = data[srcIndex + 0];
                if (channelCount >= 2) dstFloats[dstIndex + 1] = data[srcIndex + 1];
                if (channelCount >= 3) dstFloats[dstIndex + 2] = data[srcIndex + 2];
                if (channelCount >= 4) dstFloats[dstIndex + 3] = data[srcIndex + 3];

                srcIndex += 4;
                dstIndex += (int)channelCount;
            }

            return output;
        }

        private static uint GetChannelCount(ChannelLayout layout) => layout switch
        {
            ChannelLayout.R => 1u,
            ChannelLayout.RG => 2u,
            ChannelLayout.RGB => 3u,
            ChannelLayout.RGBA => 4u,
            _ => throw new NotSupportedException($"Unsupported channel layout {layout}")
        };
    }
}
