using ImageLibrary.Formats;
using ImageLibrary.Formats.Encoders;
using ImageLibrary.Formats.Encoders.Nitro;
using ImageLibrary.Helpers;
using ImageLibrary.Interfaces;
using ImageLibrary.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    /// <summary>
    /// Represents an image format that can encode and decode image data.
    /// </summary>
    public class ImageFormatDS : IImageFormat
    {
        /// <summary>
        /// The palette used for palette based formats
        /// </summary>
        public byte[] Palette = new byte[0];

        /// <summary>
        /// 
        /// </summary>
        public byte[] PaletteIdx = new byte[0];

        /// <summary>
        /// The Nitro image format.
        /// </summary>
        public NitroTexFormat Format = NitroTexFormat.CMPR_4x4;

        /// <summary>
        /// 
        /// </summary>
        public bool Color0 = false;

        /// <summary>
        /// Gets the raw encoder. Always rgba8.
        /// </summary>
        /// <returns></returns>
        public ImageEncoder GetEncoder() => new Rgba(8, 8, 8, 8);

        public ImageFormatDS(NitroTexFormat format)
        {
            Format = format;
        }

        public ImageFormatDS(NitroTexFormat format, byte[] palette, byte[] paletteIdx)
        {
            Format = format;
            Palette = palette;
            PaletteIdx = paletteIdx;
        }

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
        /// String to display for user interface
        /// </summary>
        public override string ToString() => Format.ToString();

        public virtual DecoderOutput Decode(byte[] data, uint width, uint height)
        {
            return new DecoderOutput()
            {          
                // Decode either palette or block based format
                Data = NitroTexDecoder.DecodeTexture((int)width, (int)height,
                    this.Format, data, Palette, PaletteIdx, Color0),
                Width = width,
                Height = height
            };
        }

        public virtual byte[] Encode(byte[] data, uint width, uint height)
        {
            var colorCount = 256;
            switch (this.Format)
            {
                case NitroTexFormat.Palette256: colorCount = 256; break;
                case NitroTexFormat.Palette16: colorCount = 16; break;
                case NitroTexFormat.Palette4: colorCount = 4; break;
                case NitroTexFormat.A3I5: colorCount = 32; break;
                case NitroTexFormat.A5I3: colorCount = 8; break;
            }

            if (this.Format != NitroTexFormat.Direct && this.Format != NitroTexFormat.CMPR_4x4)
            {
                var hasAlpha = this.Format == NitroTexFormat.A3I5 || this.Format == NitroTexFormat.A5I3;
                var palFormat = MedianCut.PaletteFormat.BGR555;
                var img = MedianCut.Quantize(Image.LoadPixelData<Rgba32>(data, (int)width, (int)height),
                    colorCount, palFormat, hasAlpha, null);

                data = img.GetSourceInBytes();
                img.Dispose();
            }

            var output = NitroTexEncoder.Encode(this.Format, data, (int)width, (int)height, Color0);
            Palette = output.palette;
            PaletteIdx = output.paletteIdx;
            return output.texData;
        }

        public virtual uint GetSize(uint width, uint height) => 0;

        /// <summary>
        /// The DXGI DDS format will be RGBA8 
        /// All raw rgba formats are palette or tile based and not directly supported
        /// </summary>
        /// <returns></returns>
        public DDS.DXGI_FORMAT GetDDSFormat() => DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

        public static List<ImageFormatDS> GetSupportedFormats()
        {
            List<ImageFormatDS> imageFormats = new List<ImageFormatDS>();
            foreach (NitroTexFormat format in Enum.GetValues(typeof(NitroTexFormat)))
                imageFormats.Add(new ImageFormatDS(format));
            return imageFormats;
        }

        public enum NitroTexFormat
        {
            None = 0x00,
            A3I5 = 0x01,
            Palette4 = 0x02,
            Palette16 = 0x03,
            Palette256 = 0x04,
            CMPR_4x4 = 0x05,
            A5I3 = 0x06,
            Direct = 0x07,
        }
    }
}
