using AstcEncoder;
using BCnEncoder.Shared;
using ImageLibrary.Helpers;
using ImageLibrary.Interfaces;
using ImageLibrary.Native;
using Ryujinx.Graphics.Gal.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
            // Do one time check
            if (!AstcEncHelper.IsValid)
                return ASTCDecoder.DecodeToRGBA8888(data,
                  (int)BlockWidth, (int)BlockHeight, (int)BlockDepth, (int)width, (int)height, 1);

            AstcencSwizzle swizzle = new AstcencSwizzle()
            {
                r = AstcencSwz.AstcencSwzR,
                g = AstcencSwz.AstcencSwzG,
                b = AstcencSwz.AstcencSwzB,
                a = AstcencSwz.AstcencSwzA
            };
            AstcencError status = Astcenc.AstcencConfigInit(AstcencProfile.AstcencPrfLdr, 
                BlockWidth, BlockHeight, BlockDepth, Astcenc.AstcencPreMedium, 0, out AstcencConfig config);
            
            AstcencContext context;
            status = Astcenc.AstcencContextAlloc(ref config, 1, out context);

            byte[] output = new byte[width * height * 4];

            AstcencImage outImage;
            outImage.dimX = width;
            outImage.dimY = height;
            outImage.dimZ = 1;
            outImage.dataType = AstcencType.AstcencTypeU8;
            outImage.data = output;
            status = Astcenc.AstcencDecompressImage(context, data, ref outImage, swizzle, 0);

            Astcenc.AstcencContextFree(context);

            return output;
        }

        public byte[] Encode(byte[] data, uint width, uint height)
        {
            uint thread_count = 1;

            AstcencSwizzle swizzle = new AstcencSwizzle()
            {
                r = AstcencSwz.AstcencSwzR,
                g = AstcencSwz.AstcencSwzG,
                b = AstcencSwz.AstcencSwzB,
                a = AstcencSwz.AstcencSwzA
            };

            AstcencError status = Astcenc.AstcencConfigInit(AstcencProfile.AstcencPrfLdr,
                BlockWidth, BlockHeight, BlockDepth, Astcenc.AstcencPreMedium, 0, out AstcencConfig config);

            AstcencContext context;
            status = Astcenc.AstcencContextAlloc(ref config, thread_count, out context);

            uint block_count_x = ((uint)width + BlockWidth - 1) / BlockWidth;
            uint block_count_y = ((uint)height + BlockHeight - 1) / BlockHeight;

            uint compLen = block_count_x * block_count_y * 16;
            byte[] comp_data = new byte[compLen];

            AstcencImage outImage;
            outImage.dimX = width;
            outImage.dimY = height;
            outImage.dimZ = 1;
            outImage.dataType = AstcencType.AstcencTypeU8;
            outImage.data = data;
            status = Astcenc.AstcencCompressImage(context, ref outImage, swizzle, comp_data, 0);

            Astcenc.AstcencContextFree(context);

            return comp_data;
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

    // Used for a single time check if astc env is present.
    static class AstcEncHelper
    {
        public static bool IsValid = false;

        static AstcEncHelper()
        {
            IsValid = IsAstcEncValid();
        }

        // Checks for valid dll paths to use astc enc
        static bool IsAstcEncValid()
        {
            if (OperatingSystem.IsWindows())
            {
                string dir_x64 = Path.Combine("runtimes", "win-x64", "native");
                string dir_arm = Path.Combine("runtimes", "win-arm64", "native");

                return RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => File.Exists(Path.Combine(dir_x64, "astcenc-avx2-shared.dll")),
                    Architecture.Arm64 => File.Exists(Path.Combine(dir_arm, "astcenc-neon-shared.dll")),
                    _ => false,
                };
            }
            if (OperatingSystem.IsLinux())
            {
                string dir_x64 = Path.Combine("runtimes", "linux-x64", "native");
                string dir_arm = Path.Combine("runtimes", "linux-x64", "native");

                return RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => File.Exists(Path.Combine(dir_x64, "libastcenc-avx2-shared.so")),
                    Architecture.Arm64 => File.Exists(Path.Combine(dir_arm, "libastcenc-neon-shared.so")),
                    _ => throw new PlatformNotSupportedException(),
                };
            }
            if (OperatingSystem.IsMacOS())
            {
                string dir = Path.Combine("runtimes", "osx", "native");
                return File.Exists(Path.Combine(dir, "libastcenc-shared.dylib"));
            }

            return false;
        }
    }
}
