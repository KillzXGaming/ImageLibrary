using ImageLibrary.IO;
using ImageLibrary.Pixels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ImageLibrary.ImageFormatGcn;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageLibrary.Formats.Encoders.Gcn
{
    public class GcnPalette
    {
        public GcnPaletteFormats Format = GcnPaletteFormats.RGB565;

        public bool HasData => _paletteData?.Length > 0;

        private byte[] _paletteData;

        public GcnPalette() { }
        public GcnPalette(GcnPaletteFormats format)
        {
            Format = format;
        }
        public GcnPalette(GcnPaletteFormats format, byte[] data)
        {
            Format = format;
            _paletteData = data;
        }
        public GcnPalette(GcnPaletteFormats format, ushort[] data)
        {
            Format = format;
            Load(data);
        }

        public void Load(byte[] paletteData)
        {
            _paletteData = paletteData;
        }

        public void Load(ushort[] paletteData)
        {
            var mem = new MemoryStream();
            using (var writer = new FileWriter(mem))
            {
                writer.ByteOrder = ByteOrder.BigEndian;
                for (int i = 0; i < paletteData.Length; i++)
                    writer.Write(paletteData[i]);
            }

            _paletteData = mem.ToArray();
        }

        public void Load(BinaryReader reader, uint paletteEntryCount)
        {
            //Files that don't have palettes have an entry count of zero.
            if (paletteEntryCount == 0)
            {
                _paletteData = new byte[0];
                return;
            }
            //All palette formats are 2 bytes per entry.
            _paletteData = reader.ReadBytes((int)paletteEntryCount * 2);
        }

        public byte[] GetBytes()
        {
            return _paletteData == null ? new byte[0] : _paletteData;
        }
        public ushort[] GetUShorts()
        {
            if (_paletteData == null) return new ushort[0];

            ushort[] indices = new ushort[_paletteData.Length / 2];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = (ushort)(Buffer.GetByte(_paletteData, 2 * i) << 8 |
                                        Buffer.GetByte(_paletteData, 2 * i + 1));
            }
            return indices;
        }

        public void SetPaletteColor(RgbaColor color, int index)
        {

        }

        public RgbaColor[] GetPaletteColors()
        {
            var data = this.GetBytes();

            RgbaColor[] colors = new RgbaColor[data.Length / 2];
            for (int i = 0; i < colors.Length; i++)
            {
                ushort palettePixelData = (ushort)(Buffer.GetByte(data, 2 * i) << 8 | Buffer.GetByte(data, 2 * i + 1));
                switch (this.Format)
                {
                    case GcnPaletteFormats.IA8:
                        colors[i] = IA8ToRGBA8(palettePixelData);
                        break;
                    case GcnPaletteFormats.RGB565:
                        colors[i] = RGB565ToRGBA8(palettePixelData);
                        break;
                    case GcnPaletteFormats.RGB5A3:
                        colors[i] = RGB5A3ToRGBA8(palettePixelData);
                        break;
                }
            }
            return colors;
        }

        /// <summary>
        /// Convert a IA8 encoded pixel (two bytes in length) to a RGBA (4 byte in length)
        /// </summary>
        /// <param name="sourcePixel"></param>
        /// <param name="sourcePixel">IA8 encoded pixel.</param>
        private static RgbaColor IA8ToRGBA8(ushort sourcePixel)
        {
            byte low_byte = (byte)(sourcePixel & 0xFF);
            byte high_byte = (byte)((sourcePixel >> 8) & 0xFF);
            return new RgbaColor(low_byte, low_byte, low_byte, high_byte);
        }

        /// <summary>
        /// Convert a RGB565 encoded pixel (two bytes in length) to a RGBA (4 byte in length)
        /// pixel.
        /// </summary>
        /// <param name="sourcePixel">RGB565 encoded pixel.</param>
        private static RgbaColor RGB565ToRGBA8(ushort sourcePixel)
        {
            //This repo fixes some decoding bugs SuperBMD had
            //https://github.com/RenolY2/SuperBMD/tree/master/SuperBMDLib/source

            byte r, g, b;
            r = (byte)((sourcePixel & 0xF100) >> 11);
            g = (byte)((sourcePixel & 0x7E0) >> 5);
            b = (byte)(sourcePixel & 0x1F);

            r = (byte)(r << 8 - 5 | r >> 10 - 8);
            g = (byte)(g << 8 - 6 | g >> 12 - 8);
            b = (byte)(b << 8 - 5 | b >> 10 - 8);

            return new RgbaColor(r, g, b, 0xFF);
        }

        /// <summary>
        /// Convert a RGB5A3 encoded pixel (two bytes in length) to an RGBA (4 byte in length)
        /// pixel.
        /// </summary>
        /// <param name="sourcePixel">RGB5A3 encoded pixel.</param>
        /// <param name="dest">Destination array for RGBA pixel.</param>
        /// <param name="destOffset">Offset into destination array to write RGBA pixel.</param>
        private static RgbaColor RGB5A3ToRGBA8(ushort sourcePixel)
        {
            byte r, g, b, a;

            //No alpha bits
            if ((sourcePixel & 0x8000) == 0x8000)
            {
                a = 0xFF;
                r = (byte)((sourcePixel & 0x7C00) >> 10);
                g = (byte)((sourcePixel & 0x3E0) >> 5);
                b = (byte)(sourcePixel & 0x1F);

                r = (byte)(r << 8 - 5 | r >> 10 - 8);
                g = (byte)(g << 8 - 5 | g >> 10 - 8);
                b = (byte)(b << 8 - 5 | b >> 10 - 8);
            }
            //Alpha bits
            else
            {
                a = (byte)((sourcePixel & 0x7000) >> 12);
                r = (byte)((sourcePixel & 0xF00) >> 8);
                g = (byte)((sourcePixel & 0xF0) >> 4);
                b = (byte)(sourcePixel & 0xF);

                a = (byte)(a << 8 - 3 | a << 8 - 6 | a >> 9 - 8);
                r = (byte)(r << 8 - 4 | r);
                g = (byte)(g << 8 - 4 | g);
                b = (byte)(b << 8 - 4 | b);
            }

            return new RgbaColor(r, g, b, a);
        }
    }
}
