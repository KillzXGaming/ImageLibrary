using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.Formats;
using ImageLibrary.Formats.Encoders;
using ImageLibrary.Interfaces;
using ImageLibrary.Utils;

namespace ImageLibrary
{
    /// <summary>
    /// Represents an image format that can encode and decode image data.
    /// </summary>
    public class ImageFormat : IImageFormat
    {
        /// <summary>
        /// The image format 
        /// </summary>
        private TextureFormat _format = TextureFormat.RGBA8_UNORM;
        /// <summary>
        /// The raw image encoder instance for decoding/encoding
        /// </summary>
        private ImageEncoder Encoder;
        /// <summary>
        /// The preview name.
        /// </summary>
        private string _name = "";

        public ImageEncoder GetEncoder() => this.Encoder;

        public ImageFormat(TextureFormat format)
        {
            _format = format;
            if (!Encoders.ContainsKey(format))
                throw new Exception($"Format {format} not supported!");

            Encoder = Encoders[format];
            _name = _format.ToString();
        }

        public ImageFormat(ImageEncoder encoder)
        {
            // Custom encoder
            Encoder = encoder;
            _name = encoder.ToString();
        }

        public ImageFormat(DdsFile.DXGI_FORMAT dxgiFormat)
        {
            _format = (TextureFormat)dxgiFormat;
            if (!Encoders.ContainsKey(_format))
                throw new Exception($"Format {_format} not supported!");

            Encoder = Encoders[_format];
            _name = _format.ToString();
        }

        public override string ToString() => _name;

        /// <summary>
        /// Calculates total amount of mips possible for the image based on width/height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public uint CalculateMipCount(uint width, uint height)
        {
            return 1 + (uint)Math.Floor(Math.Log(Math.Max(width, height), 2));
        }

        /// <summary>
        /// Returns true if the dxgi format can be decoded and encoded
        /// </summary>
        /// <param name="dxgiFormat"></param>
        /// <returns></returns>
        public static bool IsEncoderSupported(DdsFile.DXGI_FORMAT dxgiFormat) {
            return Encoders.ContainsKey((TextureFormat)dxgiFormat);
        }

        /// <summary>
        /// Returns true if the format can be decoded and encoded
        /// </summary>
        /// <param name="dxgiFormat"></param>
        /// <returns></returns>
        public static bool IsEncoderSupported(TextureFormat format) {
            return Encoders.ContainsKey(format);
        }

        /// <summary>
        /// Decodes the raw data into rgba
        /// </summary>
        /// <param name="data"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public virtual DecoderOutput Decode(byte[] data, uint width, uint height) {
            return new DecoderOutput() {
                Data = Encoder.Decode(data, width, height),
                Width = width,
                Height = height
            };
        }

        /// <summary>
        /// Encodes the raw data from rgba
        /// </summary>
        /// <param name="data"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public virtual byte[] Encode(byte[] data, uint width, uint height) {
            return Encoder.Encode(data, width, height);
        }

        /// <summary>
        /// The bytes used per pixel.
        /// </summary>
        /// <returns></returns>
        public uint GetBytesPerPixel() => Encoder.BytesPerPixel;

        /// <summary>
        /// The bits used per pixel.
        /// </summary>
        /// <returns></returns>
        public uint GetBitsPerPixel() => Encoder.BitsPerPixel;

        /// <summary>
        /// The total image size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public uint GetSize(uint width, uint height) => Encoder.CalculateSize(width, height);

        /// <summary>
        /// Gets the DXGI format used for DDS exporting.
        /// If RGBA8, the format will be decoded to match.
        /// </summary>
        /// <returns></returns>
        public DdsFile.DXGI_FORMAT GetDDSFormat() => (DdsFile.DXGI_FORMAT)_format; 
        /// <summary>
        /// The format enum value used.
        /// </summary>
        /// <returns></returns>
        public TextureFormat GetTextureFormat() => _format;

        /// <summary>
        /// Gets the block width of the encoder.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static uint GetBlockWidth(IImageFormat format)
        {
            if (format.GetEncoder() is ImageBlockFormat blockFormat) return blockFormat.BlockWidth;
            return 1;
        }

        /// <summary>
        /// Gets the block height of the encoder.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static uint GetBlockHeight(IImageFormat format)
        {
            if (format.GetEncoder() is ImageBlockFormat blockFormat) return blockFormat.BlockHeight;
            return 1;
        }

        /// <summary>
        /// Gets the block depth of the encoder.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static uint GetBlockDepth(IImageFormat format)
        {
            if (format.GetEncoder() is ImageBlockFormat blockFormat) return blockFormat.BlockDepth;
            return 1;
        }


        static Dictionary<TextureFormat, ImageEncoder> Encoders = new Dictionary<TextureFormat, ImageEncoder>()
        {
            { TextureFormat.RGB9E5_SHAREDEXP, new RGB9E5() },
            
            { TextureFormat.RGBA8_UNORM, new Rgba(8, 8, 8, 8) },
            { TextureFormat.RGBA8_SRGB,  new Rgba(8, 8, 8, 8) },
            { TextureFormat.RG8_UNORM,   new Rgba(8, 8) },
            { TextureFormat.A8_UNORM,    new A8() },
            { TextureFormat.R8_UNORM,    new L8() },

            { TextureFormat.RGBA4_UNORM,   new Rgba(4, 4, 4, 4) },
            { TextureFormat.R4_UNORM,      new Rgba(4) },
            { TextureFormat.RG4_UNORM,      new Rgba(4, 4) },

            { TextureFormat.RGB565_UNORM,  new Rgba(5, 6, 5) },
            { TextureFormat.RGB5A1_UNORM,  new Rgba(5, 5, 5, 1) },

            { TextureFormat.BGR565_UNORM,  new Rgba(5, 6, 5) { ChannelOrder = "BGR" } },
            { TextureFormat.BGR5A1_UNORM,  new Rgba(5, 5, 5, 1) { ChannelOrder = "BGRA" } },
            { TextureFormat.BGRA4_UNORM,   new Rgba(4, 4, 4, 4) { ChannelOrder = "BGRA" } },
            { TextureFormat.BGRA8_UNORM,   new Rgba(8, 8, 8, 8) { ChannelOrder = "BGRA" } },
            { TextureFormat.BGRA8_SRGB,    new Rgba(8, 8, 8, 8) { ChannelOrder = "BGRA" } },

            { TextureFormat.RGBB10A2_UNORM,    new R10B10G10A2() },

            { TextureFormat.D16_UNORM,  new Rgba(16) },

            // Signed formats
            { TextureFormat.R8_SNORM,       new Rgba(8) { IsSigned = true } },
            { TextureFormat.RG8_SNORM,      new Rgba(8, 8) { IsSigned = true } },
            { TextureFormat.RGBA8_SNORM,    new Rgba(8, 8, 8, 8) { IsSigned = true } },

            { TextureFormat.R16_SNORM,       new Rgba(16) { IsSigned = true } },
            { TextureFormat.RG16_SNORM,      new Rgba(16) { IsSigned = true } },
            { TextureFormat.RGBA16_SNORM,    new Rgba(16) { IsSigned = true } },

            // Float formats
            { TextureFormat.RG11B10_FLOAT,  new R11G11B10() },

            { TextureFormat.R32_FLOAT,     new Rgba(32) { IsFloat = true } },
            { TextureFormat.R16_FLOAT,     new Rgba(16) { IsFloat = true } },
            { TextureFormat.RG16_FLOAT,     new Rgba(16, 16) { IsFloat = true } },
            { TextureFormat.RGBA32_FLOAT,     new Rgba(32,32,32,32) { IsFloat = true } },
            { TextureFormat.R16_UNORM,     new Rgba(16) { ChannelOrder = "RRR" } },
            { TextureFormat.RG16_UNORM,      new Rgba(16,16) },
            { TextureFormat.RGBA16_UNORM,      new Rgba(16,16,16,16) },

            { TextureFormat.RGBA16_FLOAT,     new Rgba16F()},

            { TextureFormat.D32_FLOAT,  new Rgba(32) },

            // Compressed
            { TextureFormat.BC1_UNORM,  new Bcn(BcnFormats.BC1) },
            { TextureFormat.BC1_SRGB,  new Bcn(BcnFormats.BC1) },
            { TextureFormat.BC2_UNORM,  new Bcn(BcnFormats.BC2) },
            { TextureFormat.BC2_SRGB,  new Bcn(BcnFormats.BC2) },
            { TextureFormat.BC3_UNORM,  new Bcn(BcnFormats.BC3) },
            { TextureFormat.BC3_SRGB,  new Bcn(BcnFormats.BC3) },
            { TextureFormat.BC4_UNORM,  new Bcn(BcnFormats.BC4) },
            { TextureFormat.BC4_SNORM,  new Bcn(BcnFormats.BC4S) },
            { TextureFormat.BC5_UNORM,  new Bcn(BcnFormats.BC5) },
            { TextureFormat.BC5_SNORM,  new Bcn(BcnFormats.BC5S) },
            { TextureFormat.BC6H_UF16,  new Bcn(BcnFormats.BC6) },
            { TextureFormat.BC6H_SF16,  new Bcn(BcnFormats.BC6S) },
            { TextureFormat.BC7_UNORM,  new Bcn(BcnFormats.BC7) },
            { TextureFormat.BC7_SRGB,  new Bcn(BcnFormats.BC7) },

            { TextureFormat.ASTC_4x4_UNORM,     new Astc(Astc.AstcFormat.ASTC_4x4) },
            { TextureFormat.ASTC_5x4_UNORM,     new Astc(Astc.AstcFormat.ASTC_5x4) },
            { TextureFormat.ASTC_5x5_UNORM,     new Astc(Astc.AstcFormat.ASTC_5x5) },
            { TextureFormat.ASTC_6x5_UNORM,     new Astc(Astc.AstcFormat.ASTC_6x5) },
            { TextureFormat.ASTC_6x6_UNORM,     new Astc(Astc.AstcFormat.ASTC_6x6) },
            { TextureFormat.ASTC_8x5_UNORM,     new Astc(Astc.AstcFormat.ASTC_8x5) },
            { TextureFormat.ASTC_8x6_UNORM,     new Astc(Astc.AstcFormat.ASTC_8x6) },
            { TextureFormat.ASTC_8x8_UNORM,     new Astc(Astc.AstcFormat.ASTC_8x8) },
            { TextureFormat.ASTC_10x5_UNORM,    new Astc(Astc.AstcFormat.ASTC_10x5) },
            { TextureFormat.ASTC_10x6_UNORM,    new Astc(Astc.AstcFormat.ASTC_10x6) },
            { TextureFormat.ASTC_10x8_UNORM,    new Astc(Astc.AstcFormat.ASTC_10x8) },
            { TextureFormat.ASTC_10x10_UNORM,   new Astc(Astc.AstcFormat.ASTC_10x10) },
            { TextureFormat.ASTC_12x10_UNORM,   new Astc(Astc.AstcFormat.ASTC_12x10) },
            { TextureFormat.ASTC_12x12_UNORM,   new Astc(Astc.AstcFormat.ASTC_12x12) },

            { TextureFormat.ASTC_4x4_SRGB,     new Astc(Astc.AstcFormat.ASTC_4x4) },
            { TextureFormat.ASTC_5x4_SRGB,     new Astc(Astc.AstcFormat.ASTC_5x4) },
            { TextureFormat.ASTC_5x5_SRGB,     new Astc(Astc.AstcFormat.ASTC_5x5) },
            { TextureFormat.ASTC_6x5_SRGB,     new Astc(Astc.AstcFormat.ASTC_6x5) },
            { TextureFormat.ASTC_6x6_SRGB,     new Astc(Astc.AstcFormat.ASTC_6x6) },
            { TextureFormat.ASTC_8x5_SRGB,     new Astc(Astc.AstcFormat.ASTC_8x5) },
            { TextureFormat.ASTC_8x6_SRGB,     new Astc(Astc.AstcFormat.ASTC_8x6) },
            { TextureFormat.ASTC_8x8_SRGB,     new Astc(Astc.AstcFormat.ASTC_8x8) },
            { TextureFormat.ASTC_10x5_SRGB,    new Astc(Astc.AstcFormat.ASTC_10x5) },
            { TextureFormat.ASTC_10x6_SRGB,    new Astc(Astc.AstcFormat.ASTC_10x6) },
            { TextureFormat.ASTC_10x8_SRGB,    new Astc(Astc.AstcFormat.ASTC_10x8) },
            { TextureFormat.ASTC_10x10_SRGB,   new Astc(Astc.AstcFormat.ASTC_10x10) },
            { TextureFormat.ASTC_12x10_SRGB,   new Astc(Astc.AstcFormat.ASTC_12x10) },
            { TextureFormat.ASTC_12x12_SRGB,   new Astc(Astc.AstcFormat.ASTC_12x12) },
        };
    }
}
