using ImageLibrary.Formats.Encoders;
using ImageLibrary.Formats.Encoders.Gcn;
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
    public class ImageFormatGcn : IImageFormat
    {
        /// <summary>
        /// The palette used for palette based formats (C4, C8, and C14X2)
        /// </summary>
        public GcnPalette Palette = new GcnPalette(GcnPaletteFormats.RGB5A3);

        /// <summary>
        /// The GCN image format.
        /// </summary>
        public GcnTextureFormats Format = GcnTextureFormats.RGBA32;

        /// <summary>
        /// Returns true if the format is a palette type or not.
        /// </summary>
        public bool IsFormatPalette => PaletteFormats.ContainsKey(Format);

        /// <summary>
        /// Gets the raw encoder. Always rgba8.
        /// </summary>
        /// <returns></returns>
        public ImageEncoder GetEncoder() => new Rgba(8, 8, 8, 8);

        public ImageFormatGcn(GcnTextureFormats format)
        {
            Format = format;
        }

        public ImageFormatGcn(GcnTextureFormats format, GcnPalette palette)
        {
            Format = format;
            Palette = palette;
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
            if (PaletteFormats.ContainsKey(Format))
                return PaletteFormats[this.Format].ConvertFrom(data, Palette, (int)width, (int)height);
            else
                return BlockFormats[this.Format].ConvertFrom(data, (int)width, (int)height);
        }

        public virtual byte[] Encode(byte[] data, uint width, uint height)
        {
            data = ImageHelper.ConvertBgraToRgba(data);

            // Encode either palette or block based format
            if (PaletteFormats.ContainsKey(Format))
                return PaletteFormats[this.Format].ConvertTo(data, Palette, (int)width, (int)height);
            else
                return BlockFormats[this.Format].ConvertTo(data, (int)width, (int)height, null);
        }        

        public byte[] EncodeWithPalette(byte[] data, uint width, uint height, int colorCount = 256)
        {
            data = ImageHelper.ConvertBgraToRgba(data);

            var palFormat = MedianCut.PaletteFormat.RGB5A3;
            if (Palette.Format == GcnPaletteFormats.IA8)
                palFormat = MedianCut.PaletteFormat.IA8;
            if (Palette.Format == GcnPaletteFormats.RGB565)
                palFormat = MedianCut.PaletteFormat.RGB565;

            var img = MedianCut.Quantize(Image.LoadPixelData<Rgba32>(data, (int)width, (int)height),
                colorCount, palFormat, null);

            var quantData = img.GetSourceInBytes();
            img.Dispose();

            return PaletteFormats[this.Format].ConvertTo(quantData, Palette, (int)width, (int)height);
        }

        public virtual uint GetSize(uint width, uint height)
            => (uint)GetDataSize(Format, (int)width, (int)height);

        /// <summary>
        /// The DXGI DDS format will be RGBA8 
        /// All raw rgba formats are block based and not directly supported
        /// </summary>
        /// <returns></returns>
        public DDS.DXGI_FORMAT GetDDSFormat() => DDS.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;

        static Dictionary<GcnTextureFormats, ImageDataBlockFormat> BlockFormats = new Dictionary<GcnTextureFormats, ImageDataBlockFormat>()
        {
            { GcnTextureFormats.RGBA32, ImageDataBlockFormat.Rgba32 },
            { GcnTextureFormats.RGB565, ImageDataBlockFormat.RGB565 },
            { GcnTextureFormats.RGB5A3, ImageDataBlockFormat.RGB5A3 },
            { GcnTextureFormats.CMPR, ImageDataBlockFormat.Cmpr },
            { GcnTextureFormats.I4, ImageDataBlockFormat.I4 },
            { GcnTextureFormats.I8, ImageDataBlockFormat.I8 },
            { GcnTextureFormats.IA4, ImageDataBlockFormat.IA4 },
            { GcnTextureFormats.IA8, ImageDataBlockFormat.IA8 },
        };

        static Dictionary<GcnTextureFormats, ImageDataPaletteFormat> PaletteFormats = new Dictionary<GcnTextureFormats, ImageDataPaletteFormat>()
        {
            { GcnTextureFormats.C4, ImageDataPaletteFormat.C4 },
            { GcnTextureFormats.C8, ImageDataPaletteFormat.C8 },
            { GcnTextureFormats.C14X2, ImageDataPaletteFormat.C14X2 },
        };

        public static List<IImageFormat> GetSupportedFormats()
        {
            List<IImageFormat> imageFormats = new List<IImageFormat>();
            foreach (GcnTextureFormats format in Enum.GetValues(typeof(GcnTextureFormats)))
                imageFormats.Add(new ImageFormatGcn(format));
            return imageFormats;
        }

        #region Size Calculations

        private static readonly int[] Bpp = { 4, 8, 8, 16, 16, 16, 32, 0, 4, 8, 16, 0, 0, 0, 4 };

        public static int GetBpp(GcnTextureFormats Format) { return Bpp[(uint)Format]; }

        private static readonly int[] TileSizeW = { 8, 8, 8, 4, 4, 4, 4, 0, 8, 8, 4, 0, 0, 0, 8 };
        private static readonly int[] TileSizeH = { 8, 4, 4, 4, 4, 4, 4, 0, 8, 4, 4, 0, 0, 0, 8 };

        public static int GetDataSize(GcnTextureFormats Format, int Width, int Height, bool adjjustByTileSize = true)
        {
            if (adjjustByTileSize)
            {
                while (Width % TileSizeW[(uint)Format] != 0) Width++;
                while (Height % TileSizeH[(uint)Format] != 0) Height++;
            }
            return Width * Height * GetBpp(Format) / 8;
        }

        public static int GetDataSizeWithMips(GcnTextureFormats format, uint Width, uint Height, uint MipCount) {
            return GetDataSizeWithMips((uint)format, Width, Height, MipCount);
        }

        public static int GetDataSizeWithMips(uint format, uint Width, uint Height, uint MipCount)
        {
            if (MipCount == 0)
                MipCount = 1;

            int size = 0;
            for (int m = 0; m < MipCount; m++)
            {
                uint width = (uint)Math.Max(1, Width >> m);
                uint height = (uint)Math.Max(1, Height >> m);
                size += GetDataSize((GcnTextureFormats)format, (int)width, (int)height);
            }

            return size;
        }

        public enum GcnTextureFormats : ushort
        {
            //Bits per Pixel | Block Width | Block Height | Block Size | Type / Description
            I4 = 0x00,      //  4 | 8 | 8 | 32 | grey
            I8 = 0x01,      //  8 | 8 | 8 | 32 | grey
            IA4 = 0x02,     //  8 | 8 | 4 | 32 | grey + alpha
            IA8 = 0x03,     // 16 | 4 | 4 | 32 | grey + alpha
            RGB565 = 0x04,  // 16 | 4 | 4 | 32 | color
            RGB5A3 = 0x05,  // 16 | 4 | 4 | 32 | color + alpha
            RGBA32 = 0x06,  // 32 | 4 | 4 | 64 | color + alpha
            C4 = 0x08,      //  4 | 8 | 8 | 32 | palette choices (IA8, RGB565, RGB5A3)
            C8 = 0x09,      //  8 | 8 | 4 | 32 | palette choices (IA8, RGB565, RGB5A3)
            C14X2 = 0x0a,   // 16 | 4 | 4 | 32 | palette (IA8, RGB565, RGB5A3) NOTE: only 14 bits are used per pixel
            CMPR = 0x0e,    //  4 | 8 | 8 | 32 | mini palettes in each block, RGB565 or transparent.
        }


        #endregion

        /// <summary>
        /// PaletteFormat specifies how the data within the palette is stored. An
        /// image uses a single palette (except CMPR which defines its own
        /// mini-palettes within the Image data). Only C4, C8, and C14X2 use
        /// palettes. For all other formats the type and count is zero.
        /// </summary>
        public enum GcnPaletteFormats
        {
            IA8 = 0x00,
            RGB565 = 0x01,
            RGB5A3 = 0x02,
        }
    }
}
