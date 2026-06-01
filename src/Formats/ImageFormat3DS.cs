using BCnEncoder.Encoder;
using FontLibrary.Textures;
using ImageLibrary.Formats;
using ImageLibrary.Formats.Encoders;
using ImageLibrary.Formats.Encoders.Ctr;
using ImageLibrary.Helpers;
using ImageLibrary.Interfaces;
using ImageLibrary.PlatformSwizzle.Algorithms.Ctr;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    /// <summary>
    /// Represents an image format that can encode and decode 3DS image data.
    /// </summary>
    public class ImageFormat3DS : IImageFormat
    {
        /// <summary>
        /// The 3DS pica texture format to decode/encode data.
        /// </summary>
        public PICATextureFormat Format { get; set; }

        public PICASwizzleTransformation SwizzleTransformation { get; set; } 

        /// <summary>
        /// The raw texture encoder/decoder
        /// </summary>
        private ImageEncoder _encoder;

        private static int[] FmtBPP = new int[] { 32, 24, 16, 16, 16, 16, 16, 8, 8, 8, 4, 4, 4, 8 };

        private static int[] SwizzleLUT =
        {
             0,  1,  8,  9,  2,  3, 10, 11,
            16, 17, 24, 25, 18, 19, 26, 27,
             4,  5, 12, 13,  6,  7, 14, 15,
            20, 21, 28, 29, 22, 23, 30, 31,
            32, 33, 40, 41, 34, 35, 42, 43,
            48, 49, 56, 57, 50, 51, 58, 59,
            36, 37, 44, 45, 38, 39, 46, 47,
            52, 53, 60, 61, 54, 55, 62, 63
        };

        public ImageFormat3DS(PICATextureFormat format) {
            Format = format;
            _encoder = Encoders[format];
        }

        public override string ToString() => Format.ToString();

        /// <summary>
        /// Decodes the raw data to rgba8 byte[]
        /// </summary>
        /// <param name="data"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public DecoderOutput Decode(byte[] data, uint width, uint height) {
            var rgba = TextureConverter3DS.DecodeBuffer(data, (int)width, (int)height, this.Format);
            if (SwizzleTransformation == PICASwizzleTransformation.FlipY)
                return new DecoderOutput()
                {
                    Data = rgba,
                    Width = width,
                    Height = height
                };

            return new DecoderOutput()
            {
                Data = FlipImage(rgba, (int)width, (int)height),
                Width = width,
                Height = height
            };
        }

        static byte[] FlipImage(byte[] data, int width, int height)
        {
            byte[] Output = new byte[data.Length];

            var Stride = width * 4;
            for (int Y = 0; Y < height; Y++)
            {
                int IOffs = Stride * Y;
                int OOffs = Stride * (height - 1 - Y);

                for (int X = 0; X < width; X++)
                {
                    Output[OOffs + 0] = data[IOffs + 0];
                    Output[OOffs + 1] = data[IOffs + 1];
                    Output[OOffs + 2] = data[IOffs + 2];
                    Output[OOffs + 3] = data[IOffs + 3];

                    IOffs += 4;
                    OOffs += 4;
                }
            }
            return Output;
        }

        public static byte[] ConvertAbgraToRgba(byte[] bytes)
        {
            if (bytes == null)
                throw new Exception("Data block returned null.");

            for (int i = 0; i < bytes.Length; i += 4)
            {
                var temp = new byte[]
                {
                    bytes[i],bytes[i + 1],bytes[i + 2],bytes[i + 3]
                };
                bytes[i + 3] = temp[0];
                bytes[i + 2] = temp[1];
                bytes[i + 1] = temp[2];
                bytes[i + 0] = temp[3];
            }
            return bytes;
        }

        /// <summary>
        /// Encodes the raw data to rgba8 byte[]
        /// </summary>
        /// <param name="data"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public byte[] Encode(byte[] data, uint width, uint height) {
            var img = Image.LoadPixelData<Rgba32>(data, (int)width, (int)height);
            return TextureConverter3DS.Encode(img, this.Format);
        }

        /// <summary>
        /// Gets total bits per pixel.
        /// </summary>
        /// <returns></returns>
        public uint GetBitsPerPixel() => _encoder.BitsPerPixel;

        /// <summary>
        /// Gets total bytes per pixel.
        /// </summary>
        /// <returns></returns>
        public uint GetBytesPerPixel() => _encoder.BytesPerPixel;

        /// <summary>
        /// Gets the DXGI format used for DDS exporting.
        /// If RGBA8, the format will be decoded to match.
        /// </summary>
        /// <returns></returns>
        public DDS.DXGI_FORMAT GetDDSFormat()
        {
            return DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

            switch (this.Format)
            {
                case PICATextureFormat.L8:
                    return DDS.DXGI_FORMAT.DXGI_FORMAT_R8_UNORM;
                case PICATextureFormat.RGBA8:
                case PICATextureFormat.RGB8:
                    return DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
                case PICATextureFormat.RGB565:
                    return DDS.DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM;
                case PICATextureFormat.RGBA5551:
                    return DDS.DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM;
                case PICATextureFormat.LA8:
                    return DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM;
                default:
                    return DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
            }
        }

        /// <summary>
        /// Calculates the total possible mip count of the current width, height and format.
        /// ETC1 type is limited to 16 pixels on width/height due to tiling and will use less mips.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public uint CalculateMipCount(uint width, uint height)
        {
            int MipmapNum = 0;
            int num = Math.Max((int)width, (int)height);

            uint Pow2RoundDown(uint Value)
            {
                return IsPow2(Value) ? Value : Pow2RoundUp(Value) >> 1;
            }

            bool IsPow2(uint Value)
            {
                return Value != 0 && (Value & (Value - 1)) == 0;
            }

            uint Pow2RoundUp(uint Value)
            {
                Value--;

                Value |= (Value >> 1);
                Value |= (Value >> 2);
                Value |= (Value >> 4);
                Value |= (Value >> 8);
                Value |= (Value >> 16);

                return ++Value;
            }

            while (true)
            {
                num >>= 1;

                width = width / 2;
                height = height / 2;

                width = Pow2RoundDown(width);
                height = Pow2RoundDown(height);

                if (Format == PICATextureFormat.ETC1)
                {
                    if (width < 16 || height < 16)
                        break;
                }
                else if (width < 8 || height < 8)
                    break;

                if (num > 0)
                    ++MipmapNum;
                else
                    break;
            }
            return (uint)MipmapNum;
        }

        public static uint Pow2RoundUp(uint Value)
        {
            Value--;

            Value |= (Value >> 1);
            Value |= (Value >> 2);
            Value |= (Value >> 4);
            Value |= (Value >> 8);
            Value |= (Value >> 16);

            return ++Value;
        }

        public static uint Pow2RoundDown(uint Value)
        {
            return IsPow2(Value) ? Value : Pow2RoundUp(Value) >> 1;
        }

        public static bool IsPow2(uint Value)
        {
            return Value != 0 && (Value & (Value - 1)) == 0;
        }

        public int CalculateSize(uint Width, uint Height) {
            return TextureConverter3DS.CalculateLength((int)Width, (int)Height, this.Format);
        }

        public static IEnumerable<PICATextureFormat> GetFormatList() 
            => Encoders.Keys;

        /// <summary>
        /// The raw image encoder
        /// </summary>
        /// <returns></returns>
        public ImageEncoder GetEncoder() => _encoder;

        static Dictionary<PICATextureFormat, ImageEncoder> Encoders = new Dictionary<PICATextureFormat, ImageEncoder>()
        {
            { PICATextureFormat.RGBA8, new Rgba(8, 8, 8, 8) },
            { PICATextureFormat.RGB8, new Rgba(8, 8, 8) },
            { PICATextureFormat.RGB565, new Rgba(5, 6, 5) },
            { PICATextureFormat.RGBA5551, new Rgba(5, 5, 5, 1) },
            { PICATextureFormat.RGBA4, new Rgba(4, 4, 4, 4) },
            { PICATextureFormat.A8, new ImageLibrary.Formats.Encoders.A8() },
            { PICATextureFormat.A4, new A4() },
            { PICATextureFormat.L8, new  ImageLibrary.Formats.Encoders.L8() },
            { PICATextureFormat.L4, new L4() },
            { PICATextureFormat.LA8, new LA8() },
            { PICATextureFormat.LA4, new LA4() },
            { PICATextureFormat.HiLo8, new Rgba(8, 8) },
            { PICATextureFormat.ETC1, new Etc1(false) },
            { PICATextureFormat.ETC1A4, new Etc1(true) },
        };
    }

    public enum PICATextureFormat : ushort
    {
        RGBA8,
        RGB8,
        RGBA5551,
        RGB565,
        RGBA4,
        LA8,
        HiLo8,
        L8,
        A8,
        LA4,
        L4,
        A4,
        ETC1,
        ETC1A4,
    }
}

