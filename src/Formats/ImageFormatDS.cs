using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.Formats.Encoders;
using ImageLibrary.Formats.Encoders.Gcn;
using ImageLibrary.Helpers;
using ImageLibrary.Interfaces;
using ImageLibrary.Utils;
using ImageLibrary.PlatformSwizzle.Algorithms.Nitro;

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

        public virtual byte[] Decode(byte[] data, uint width, uint height)
        {
            // Decode either palette or block based format
            return NitroTex.DecodeTexture((int)width, (int)height,
                this.Format, data, Palette, PaletteIdx, Color0);
        }

        public virtual byte[] Encode(byte[] data, uint width, uint height)
        {
            throw new NotImplementedException();
        }

        public virtual uint GetSize(uint width, uint height) => 0;

        /// <summary>
        /// The DXGI DDS format will be RGBA8 
        /// All raw rgba formats are block based and not directly supported
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
