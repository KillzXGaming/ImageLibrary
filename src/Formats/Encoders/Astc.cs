using BCnEncoder.Shared;
using ImageLibrary.Helpers;
using ImageLibrary.Interfaces;
using ImageLibrary.Native;
using PVRTexLib;
using Ryujinx.Graphics.Gal.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders
{
    public class Astc : ImageEncoder, ImageBlockFormat
    {
        public AstcFormat Format { get; }
        public uint BitsPerPixel => BytesPerPixel * 8;
        public uint BlockWidth { get; }
        public uint BlockHeight { get; }
        public uint BlockDepth { get; } = 1;
        public uint BytesPerPixel => 16;

        public bool IsSRGB;

        public Astc(uint x, uint y, bool isSrgb = false)
        {
            Format = Enum.Parse<AstcFormat>($"ASTC_{x}x{y}");
            BlockWidth = x;
            BlockHeight = y;
            IsSRGB = isSrgb;
        }

        public Astc(AstcFormat format, bool isSrgb = false)
        {
            Format = format;

            // Use Regex to find block width/height/depth by format name
            MatchCollection matches = Regex.Matches(format.ToString(), @"\d+");

            BlockWidth = uint.Parse(matches[0].Value);
            BlockHeight = uint.Parse(matches[1].Value);
            if (matches.Count == 3)
                BlockDepth = uint.Parse(matches[2].Value);
        }

        public uint CalculateSize(uint width, uint height)
        {
            int blocksWidth = ((int)width + (int)BlockWidth - 1) / (int)BlockWidth;
            int blocksHeight = ((int)height + (int)BlockHeight - 1) / (int)BlockHeight;

            int numBlocks = blocksWidth * blocksHeight;
            return (uint)(numBlocks * BitsPerPixel);
        }

        public byte[] Decode(byte[] data, uint width, uint height)
        {
            PVRTexture texture = PvrTextureHelper.CreateTexture(data, (PVRTexLibPixelFormat)Format, width, height)
                     ?? throw new InvalidOperationException("Failed to create astc texture with PVRTexLib.");

            bool successful = texture.Transcode(PvrTextureHelper.Rgba8888, 
                PVRTexLibVariableType.UnsignedByteNorm, 
                PVRTexLibColourSpace.Linear,
                PVRTexLibCompressorQuality.PVRTCHigh);
            if (!successful)
                throw new InvalidOperationException("Transcoding with PVRTexLib was not successful.");

            return PvrTextureHelper.GetData(texture);
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            PVRTexture texture = PvrTextureHelper.CreateTexture(data, PvrTextureHelper.Rgba8888, width, height)
                     ?? throw new InvalidOperationException("Failed to create astc texture with PVRTexLib.");

            texture.Transcode((ulong)Format,
                PVRTexLibVariableType.UnsignedByteNorm,
                PVRTexLibColourSpace.Linear,
                PVRTexLibCompressorQuality.PVRTCHigh);

            return PvrTextureHelper.GetData(texture);
        }

        public enum AstcFormat
        {
            ASTC_4x4 = 27,
            ASTC_5x4,
            ASTC_5x5,
            ASTC_6x5,
            ASTC_6x6,
            ASTC_8x5,
            ASTC_8x6,
            ASTC_8x8,
            ASTC_10x5,
            ASTC_10x6,
            ASTC_10x8,
            ASTC_10x10,
            ASTC_12x10,
            ASTC_12x12,

            ASTC_3x3x3,
            ASTC_4x3x3,
            ASTC_4x4x3,
            ASTC_4x4x4,
            ASTC_5x4x4,
            ASTC_5x5x4,
            ASTC_5x5x5,
            ASTC_6x5x5,
            ASTC_6x6x5,
            ASTC_6x6x6,
        }
    }
}
