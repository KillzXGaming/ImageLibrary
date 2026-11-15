using ImageLibrary.Formats;
using ImageLibrary.Formats.Encoders;
using ImageLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public class ImageEncoders
    {
        public static ImageEncoder Rgb10a2(EncoderType type = EncoderType.Unorm) => new Rgba(type, 10, 10, 10, 2);
        public static ImageEncoder R11g11b10()
            => new R11G11B10();

        public static ImageEncoder Rgba32(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 32, 32, 32, 32);

        public static ImageEncoder Rgba16(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 16, 16, 16, 16);

        public static ImageEncoder R32(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 32)
            {
                ChannelOrder = "RRR1",
                IsLuminance = true
            };

        public static ImageEncoder Rg32(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 32, 32);

        public static ImageEncoder R16(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 16);

        public static ImageEncoder Rg16(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 16, 16);

        public static ImageEncoder Rgba8(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 8, 8, 8, 8);

        public static ImageEncoder Rgb8(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 8, 8, 8);

        public static ImageEncoder Rg8(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 8, 8);

        public static ImageEncoder R8Signed()
            => new Rgba(EncoderType.Snorm, 8);

        public static ImageEncoder Rg8Signed()
            => new Rgba(EncoderType.Snorm, 8, 8);

        public static ImageEncoder Rgba8Signed()
            => new Rgba(EncoderType.Snorm, 8, 8, 8, 8);

        public static ImageEncoder R8()
            => new Rgba(EncoderType.Unorm, 8) { ChannelOrder = "RRR1" };

        public static ImageEncoder A8()
            => new A8();

        public static ImageEncoder A4()
            => new A4();

        public static ImageEncoder R4()
            => new Rgba(EncoderType.Unorm, 4);

        public static ImageEncoder Rgba4()
            => new Rgba(EncoderType.Unorm, 4, 4, 4, 4);

        public static ImageEncoder Rg4()
            => new Rgba(EncoderType.Unorm, 4, 4);

        public static ImageEncoder Rgba565(EncoderType type = EncoderType.Unorm)
            => new Rgba(type, 5, 6, 5);

        public static ImageEncoder Rgba5551()
            => new Rgba(EncoderType.Unorm, 5, 5, 5, 1);

        public static ImageEncoder La4() => new LA4();
        public static ImageEncoder L4() => new L4();
        public static ImageEncoder La8() => new LA8();
        public static ImageEncoder L8() => new L8();

        public static ImageEncoder Bc1(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC1, type);

        public static ImageEncoder Bc2(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC2, type);

        public static ImageEncoder Bc3(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC3, type);

        public static ImageEncoder Bc4(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC4, type);

        public static ImageEncoder Bc4A(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC4, type) { IsAlpha = true };

        public static ImageEncoder Bc5(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC5, type);

        public static ImageEncoder Bc5A(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC5, type) { IsAlpha = true };

        public static ImageEncoder Bc6(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC6, type);

        public static ImageEncoder Bc7(EncoderType type = EncoderType.Unorm)
            => new Bcn(BcnFormats.BC7, type);

        public static ImageEncoder ETC1() => new L8();
        public static ImageEncoder ETC1A4() => new L8();

        public static ImageEncoder Astc4x4(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_4x4, type);
        public static ImageEncoder Astc5x4(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_5x4, type);

        public static ImageEncoder Astc5x5(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_5x5, type);

        public static ImageEncoder Astc6x5(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_6x5, type);

        public static ImageEncoder Astc6x6(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_6x6, type);

        public static ImageEncoder Astc8x5(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_8x5, type);

        public static ImageEncoder Astc8x6(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_8x6, type);

        public static ImageEncoder Astc8x8(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_8x8, type);

        public static ImageEncoder Astc10x5(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_10x5, type);

        public static ImageEncoder Astc10x6(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_10x6, type);

        public static ImageEncoder Astc10x8(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_10x8, type);

        public static ImageEncoder Astc10x10(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_10x10, type);

        public static ImageEncoder Astc12x10(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_12x10, type);

        public static ImageEncoder Astc12x12(EncoderType type = EncoderType.Unorm)
            => new Astc(Astc.AstcFormat.ASTC_12x12, type);

        public static uint GetBlockWidth(ImageEncoder format)
        {
            if (format is ImageBlockFormat blockFormat) return blockFormat.BlockWidth;
            return 1;
        }

        public static uint GetBlockHeight(ImageEncoder format)
        {
            if (format is ImageBlockFormat blockFormat) return blockFormat.BlockHeight;
            return 1;
        }

        public static uint GetBlockDepth(ImageEncoder format)
        {
            if (format is ImageBlockFormat blockFormat) return blockFormat.BlockDepth;
            return 1;
        }
    }
}
