using BCnEncoder.Decoder;
using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using ImageLibrary.Interfaces;
using SixLabors.ImageSharp.Formats.Bmp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders
{
    public class Bcn : ImageEncoder, ImageBlockFormat
    {
        public static int QualityLevel = 1;

        public BcnFormats Format { get; } = BcnFormats.BC1;
        public uint BitsPerPixel => BytesPerPixel * 8;
        public uint BytesPerPixel => IsSingleBlock ? 8u : 16u;
        public uint BlockWidth { get; } = 4;
        public uint BlockHeight { get; } = 4;
        public uint BlockDepth { get; } = 1;

        public bool IsSRGB = false;
        public bool IsAlpha = false;

        public bool UseBc1Alpha = true;
        public bool IsSnorm = false;

        private bool IsSingleBlock =>
            Format == BcnFormats.BC1 ||
            Format == BcnFormats.BC4 ||
            Format == BcnFormats.BC4S;

        public Bcn(BcnFormats format, bool isSrgb = false)
        {
            Format = format;
            IsSRGB = isSrgb;
        }

        public uint CalculateSize(uint width, uint height)
        {
            int blocksWidth = ((int)width + 3) / 4;
            int blocksHeight = ((int)height + 3) / 4;

            int numBlocks = blocksWidth * blocksHeight;
            return (uint)(numBlocks * BytesPerPixel);
        }

        public byte[] Decode(byte[] input, uint width, uint height)
        {
            BcDecoder decoder = new BcDecoder();
            decoder.InputOptions.DdsBc1ExpectAlpha = UseBc1Alpha;
            CompressionFormat compressionFormat = CompressionFormat.Bc1;

            switch (Format)
            {
                case BcnFormats.BC1: compressionFormat = CompressionFormat.Bc1WithAlpha; break;
                case BcnFormats.BC2: compressionFormat = CompressionFormat.Bc2; break;
                case BcnFormats.BC3: compressionFormat = CompressionFormat.Bc3; break;
                case BcnFormats.BC4: compressionFormat = CompressionFormat.Bc4; break;
                case BcnFormats.BC5: compressionFormat = CompressionFormat.Bc5; break;
               // Snorm is not supported so decode it using our own software decoder
                case BcnFormats.BC4S: return BCDecoder.DecodeBC4(input, (int)width, (int)height, true, IsAlpha);
                case BcnFormats.BC5S: return BCDecoder.DecodeBC5(input, (int)width, (int)height, true, IsAlpha);
                case BcnFormats.BC6: compressionFormat = CompressionFormat.Bc6U; break;
                case BcnFormats.BC6S: compressionFormat = CompressionFormat.Bc6S; break;
                case BcnFormats.BC7: compressionFormat = CompressionFormat.Bc7; break;
            }

            // BC6 Decoding HDR
            if (Format == BcnFormats.BC6 || Format == BcnFormats.BC6S)
            {
                // Here we scale the HDR data down to sdr
                var colors = decoder.DecodeRawHdr(new MemoryStream(input), (int)width, (int)height, compressionFormat);

                byte[] output = new byte[colors.Length * 4];
                for (int i = 0; i < colors.Length; i++)
                {
                    int offset = i * 4;

                    byte r = (byte)(Math.Clamp(colors[i].r, 0f, 1f) * 255);
                    byte g = (byte)(Math.Clamp(colors[i].g, 0f, 1f) * 255);
                    byte b = (byte)(Math.Clamp(colors[i].b, 0f, 1f) * 255);

                    output[offset + 0] = r;
                    output[offset + 1] = g;
                    output[offset + 2] = b;
                    output[offset + 3] = 255;
                }
                return output;
            }
            else
            {
                var colors = decoder.DecodeRaw(new MemoryStream(input), (int)width, (int)height, compressionFormat);

                byte[] output = new byte[colors.Length * 4];
                for (int i = 0; i < colors.Length; i++)
                {
                    int offset = i * 4;
                    // BC4A
                    if (IsAlpha && Format == BcnFormats.BC4)
                    {
                        output[offset + 0] = 255;
                        output[offset + 1] = 255;
                        output[offset + 2] = 255;
                        output[offset + 3] = colors[i].r;
                    }       
                    // BC5A
                    else if (IsAlpha && Format == BcnFormats.BC5)
                    {
                        output[offset + 0] = colors[i].r;
                        output[offset + 1] = colors[i].r;
                        output[offset + 2] = colors[i].r;
                        output[offset + 3] = colors[i].g;
                    }
                    else
                    {
                        output[offset + 0] = colors[i].r;
                        output[offset + 1] = colors[i].g;
                        output[offset + 2] = colors[i].b;
                        output[offset + 3] = colors[i].a;
                    }
                }
                return output;
            }
        }

        public byte[] Encode(byte[] input, uint width, uint height)
        {
            // Image dds library encoders faster, but only supported on selected platforms
            if (ImageDds.IsSupported())
            {
                var quality = (ImageDds.Quality)QualityLevel;
                ImageDds.ImageFormat format = ImageDds.ImageFormat.Rgba8Unorm;
                switch (Format)
                {
                    case BcnFormats.BC1: format = ImageDds.ImageFormat.BC1RgbaUnorm; break;
                    case BcnFormats.BC3: format = ImageDds.ImageFormat.BC3RgbaUnorm; break;
                    case BcnFormats.BC4: format = ImageDds.ImageFormat.BC4RUnorm; break;
                    case BcnFormats.BC4S: format = ImageDds.ImageFormat.BC4RSnorm; break;
                    case BcnFormats.BC5: format = ImageDds.ImageFormat.BC5RgUnorm; break;
                    case BcnFormats.BC6: format = ImageDds.ImageFormat.BC6hRgbUfloat; break;
                    case BcnFormats.BC6S: format = ImageDds.ImageFormat.BC6hRgbSfloat; break;
                    case BcnFormats.BC7: format = ImageDds.ImageFormat.BC7RgbaUnorm; break;
                }
                if (format != ImageDds.ImageFormat.Rgba8Unorm)
                    return ImageDds.Encode(input, width, height, format, quality);
            }

            var size = CalculateSize(width, height);

            var encoder = new BcEncoder();
            encoder.OutputOptions.GenerateMipMaps = false;
            encoder.OutputOptions.Format = CompressionFormat.Bc1;
            encoder.OutputOptions.Quality = CompressionQuality.Fast;

            //BC5 RRRG
            if (Format == BcnFormats.BC5 && IsAlpha)
            {
                encoder.InputOptions.Bc5Component1 = ColorComponent.Luminance;
                encoder.InputOptions.Bc5Component2 = ColorComponent.A;

            } //BC4 AAAA
            else if (Format == BcnFormats.BC4 && IsAlpha)
            {
                encoder.InputOptions.Bc4Component = ColorComponent.A;
            }

            switch (Format)
            {
                case BcnFormats.BC1: encoder.OutputOptions.Format = CompressionFormat.Bc1; break;
                case BcnFormats.BC2: encoder.OutputOptions.Format = CompressionFormat.Bc2; break;
                case BcnFormats.BC3: encoder.OutputOptions.Format = CompressionFormat.Bc3; break;
                case BcnFormats.BC4: encoder.OutputOptions.Format = CompressionFormat.Bc4; break;
                case BcnFormats.BC4S: encoder.OutputOptions.Format = CompressionFormat.Bc4S; break;
                case BcnFormats.BC5: encoder.OutputOptions.Format = CompressionFormat.Bc5; break;
                case BcnFormats.BC5S: encoder.OutputOptions.Format = CompressionFormat.Bc5S; break;
                case BcnFormats.BC6: encoder.OutputOptions.Format = CompressionFormat.Bc6S; break;
                case BcnFormats.BC7: encoder.OutputOptions.Format = CompressionFormat.Bc7; break;
            }

            var colors = encoder.EncodeToRawBytes(input, (int)width, (int)height, PixelFormat.Rgba32);
            return colors[0];
        }

        public override string ToString() => this.Format.ToString();
    }

    public enum BcnFormats
    {
        BC1, BC2, BC3, BC4, BC4S, BC5, BC5S, BC6, BC6S, BC7,
    }
}
