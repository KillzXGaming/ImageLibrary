using ImageLibrary.Native;
using BCnEncoder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ryujinx.Graphics.Gal.Texture;
using ImageLibrary.Interfaces;
using System.Diagnostics;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

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
            if (File.Exists(astcencPath))
                return DecodeAstcenc(data, width, height);

            return ASTCDecoder.DecodeToRGBA8888(data, (int)BlockWidth, (int)BlockHeight, 1, (int)width, (int)height, 1);

            //Init the texture instance
            var pvrTexture = PvrTexture.Create(data, width, height, 1,
                (PixelFormat)Format, ChannelType.UnsignedByte, ColorSpace.Linear);

            var successful = pvrTexture.Transcode(PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);
            if (!successful)
                throw new InvalidOperationException($"Failed to transcode with PVRTexLib for format {Format}.");

            return pvrTexture.GetData();
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            return EncodeToAstc(data, width, height, $"{BlockWidth}x{BlockHeight}", "fast");

            //Init the texture instance
            var pvrTexture = PvrTexture.Create(data,
                width, height, 1, PixelFormat.RGBA8888, ChannelType.UnsignedByteNorm, ColorSpace.Linear);

            pvrTexture.Transcode((PixelFormat)Format, ChannelType.UnsignedByteNorm, ColorSpace.Linear, CompressionQuality.PVRTCHigh);
            return pvrTexture.GetData();
        }

        static string astcencPath = "astcenc.exe";

        byte[] DecodeAstcenc(byte[] data, uint width, uint height)
        {
            string tempAstc = Path.GetTempFileName() + ".astc";
            string tempOutput = Path.GetTempFileName() + ".tga"; // astcenc supports TGA output
            string tempPng = Path.GetTempFileName() + ".png";

            try
            {
                AstcFile astc = new AstcFile(this, width, height, 1, data);
                astc.Save(tempAstc);

                // Example: 6x6 block size
                string blockSize = $"{BlockWidth}x{BlockHeight}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = astcencPath,
                    Arguments = $"-dl  \"{tempAstc}\" \"{tempOutput}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };


                using var process = Process.Start(startInfo);

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!File.Exists(tempOutput))
                    throw new FileNotFoundException("Decoded ASTC image not found");

                using var image = Image.Load<Rgba32>(tempOutput);
                var raw = image.GetSourceInBytes();
                image.Dispose();

                return raw;
            }
            finally
            {
                // Clean up
                if (File.Exists(tempAstc)) File.Delete(tempAstc);
                if (File.Exists(tempOutput)) File.Delete(tempOutput);
            }
        }

        public byte[] EncodeToAstc(byte[] rgba, uint width, uint height, string blockSize = "6x6", string quality = "fast")
        {
            string tempInput = Path.GetTempFileName() + ".png";
            string tempOutput = Path.GetTempFileName() + ".astc";

            try
            {
                // Save input PNG to disk
                var img = Image.LoadPixelData<Rgba32>(rgba, (int)width, (int)height);
                img.SaveAsPng(tempInput);
                img.Dispose();

                // Build arguments for astcenc CLI
                string args = $"-cl \"{tempInput}\" \"{tempOutput}\" {blockSize} -{quality}";

                var processInfo = new ProcessStartInfo
                {
                    FileName = astcencPath,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(processInfo);

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit();

                Console.WriteLine(stdout);
                Console.WriteLine(stderr);

                if (!File.Exists(tempOutput))
                    throw new FileNotFoundException("ASTC encoding failed");

                AstcFile astc = new AstcFile(tempOutput);
                return astc.DataBlock;
            }
            finally
            {
                // Clean up temp files
                if (File.Exists(tempInput)) File.Delete(tempInput);
                if (File.Exists(tempOutput)) File.Delete(tempOutput);
            }
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
