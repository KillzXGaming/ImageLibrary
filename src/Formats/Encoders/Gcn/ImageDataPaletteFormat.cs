using ImageLibrary.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ImageLibrary.ImageFormatGcn;

namespace ImageLibrary.Formats.Encoders.Gcn
{
    public class ImageDataPaletteFormat
    {
        public static ImageDataPaletteFormat C4 => new ImageDataPaletteFormat(4, (4, 8));
        public static ImageDataPaletteFormat C8 => new ImageDataPaletteFormat(8, (8, 8));
        public static ImageDataPaletteFormat C14X2 => new ImageDataPaletteFormat(14, (4, 4));

        public int BPP;
        public int BlockX;
        public int BlockY;

        public ImageDataPaletteFormat(int bpp, (int x, int y) blockSize)
        {
            BPP = bpp;
            BlockX = blockSize.x;
            BlockY = blockSize.y;
        }

        public byte[] ConvertFrom(byte[] data, GcnPalette palette, int width, int height)
        {
            var reader = new FileReader(data);
            switch (BPP)
            {
                case 4: return DecodeC4(reader, (uint)width, (uint)height, palette, palette.Format);
                case 8: return DecodeC8(reader, (uint)width, (uint)height, palette, palette.Format);
                case 14: return DecodeC14(reader, (uint)width, (uint)height, palette, palette.Format);
                default:
                    throw new NotImplementedException($"Invalid bits per pixel {BPP}");
            }
        }

        public byte[] ConvertTo(byte[] data, GcnPalette palette, int width, int height)
        {
            Tuple<byte[], ushort[]> palettePair;

            // Keep previous palette colors to merge for mipmaps
            List<ushort> rawColorData = new List<ushort>();
            rawColorData.AddRange(palette.GetUShorts());

            switch (BPP)
            {
                case 4: palettePair = EncodeC4(palette.Format, data, rawColorData, width, height); break;
                case 8: palettePair = EncodeC8(palette.Format, data, rawColorData, width, height); break;
                case 14: palettePair = EncodeC14(palette.Format, data, rawColorData, width, height); break;
                default:
                    throw new NotImplementedException($"Invalid bits per pixel {BPP}");
            }
            // Apply palette
            palette.Load(palettePair.Item2);
            // Return new image
            return palettePair.Item1;
        }

        public static byte[] DecodeC4(FileReader stream, uint width, uint height, GcnPalette imagePalette, GcnPaletteFormats paletteFormat)
        {
            stream.ByteOrder = ByteOrder.BigEndian;

            //4 bpp, 8 block width/height, block size 32 bytes, possible palettes (IA8, RGB565, RGB5A3)
            uint numBlocksW = (width + 7) / 8;
            uint numBlocksH = (height + 7) / 8;

            byte[] decodedData = new byte[width * height * 8];

            //Read the indexes from the file
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //Inner Loop for pixels
                    for (int pY = 0; pY < 8; pY++)
                    {
                        for (int pX = 0; pX < 8; pX += 2)
                        {
                            if (xBlock * 8 + pX >= width || yBlock * 8 + pY >= height)
                            {
                                stream.Seek(1, SeekOrigin.Current);
                                continue;
                            }

                            byte data = stream.ReadByte();
                            byte t = (byte)(data & 0xF0);
                            byte t2 = (byte)(data & 0x0F);

                            decodedData[width * (yBlock * 8 + pY) + xBlock * 8 + pX + 0] = (byte)(t >> 4);
                            decodedData[width * (yBlock * 8 + pY) + xBlock * 8 + pX + 1] = t2;
                        }
                    }
                }
            }

            //Now look them up in the palette and turn them into actual colors.
            byte[] finalDest = new byte[decodedData.Length / 2];

            int destOffset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    UnpackPixelFromPalette(decodedData[y * width + x], ref finalDest, destOffset, imagePalette.GetBytes(), paletteFormat);
                    destOffset += 4;
                }
            }

            return finalDest;
        }

        public static byte[] DecodeC8(FileReader stream, uint width, uint height, GcnPalette imagePalette, GcnPaletteFormats paletteFormat)
        {
            stream.ByteOrder = ByteOrder.BigEndian;

            //4 bpp, 8 block width/4 block height, block size 32 bytes, possible palettes (IA8, RGB565, RGB5A3)
            uint numBlocksW = (width + 7) / 8;
            uint numBlocksH = (height + 3) / 4;

            byte[] decodedData = new byte[width * height * 8];

            //Read the indexes from the file
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    //Inner Loop for pixels
                    for (int pY = 0; pY < 4; pY++)
                    {
                        for (int pX = 0; pX < 8; pX++)
                        {
                            if (xBlock * 8 + pX >= width || yBlock * 4 + pY >= height)
                            {
                                stream.Seek(1, SeekOrigin.Current);
                                continue;
                            }

                            byte data = stream.ReadByte();
                            decodedData[width * (yBlock * 4 + pY) + xBlock * 8 + pX] = data;
                        }
                    }
                }
            }

            //Now look them up in the palette and turn them into actual colors.
            byte[] finalDest = new byte[decodedData.Length / 2];

            int destOffset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    UnpackPixelFromPalette(decodedData[y * width + x], ref finalDest, destOffset, imagePalette.GetBytes(), paletteFormat);
                    destOffset += 4;
                }
            }

            return finalDest;
        }

        public static byte[] DecodeC14(FileReader stream, uint width, uint height, GcnPalette imagePalette, GcnPaletteFormats paletteFormat)
        {
            stream.ByteOrder = ByteOrder.BigEndian;

            uint numBlocksW = (width + 3u) / 4u;
            uint numBlocksH = (height + 3u) / 4u;

            ushort[] indices = new ushort[width * height];

            //Read the indexes from the file
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        int srcY = yBlock * 4 + pY;

                        for (int pX = 0; pX < 4; pX++)
                        {
                            int srcX = xBlock * 4 + pX;

                            if (srcY < height && srcX < width)
                            {
                                byte low = stream.ReadByte();
                                byte high = stream.ReadByte();

                                ushort index = (ushort)(low | ((high & 0x3F) << 8));
                                indices[srcY * width + srcX] = index;
                            }
                            else
                            {
                                // Skip padding bytes
                                stream.Seek(2, SeekOrigin.Current);
                            }
                        }
                    }
                }
            }

            //Now look them up in the palette and turn them into actual colors.
            int bytesPerPixel = (paletteFormat == GcnPaletteFormats.IA8) ? 2 : 4;
            byte[] finalDest = new byte[width * height * bytesPerPixel];

            int pixelSize = paletteFormat == GcnPaletteFormats.IA8 ? 2 : 4;
            int destOffset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ushort palIndex = indices[y * width + x];
                    UnpackPixelFromPalette(palIndex, ref finalDest, destOffset, imagePalette.GetBytes(), paletteFormat);
                    destOffset += bytesPerPixel;
                }
            }

            return finalDest;
        }

        public static Tuple<byte[], ushort[]> EncodeC4(GcnPaletteFormats PaletteFormat, byte[] m_rgbaImageData, List<ushort> rawColorData, int Width, int Height)
        {
            uint numBlocksW = (uint)((Width + 7) / 8);
            uint numBlocksH = (uint)((Height + 7) / 8);

            int paddedWidth = (int)(numBlocksW * 8);
            int paddedHeight = (int)(numBlocksH * 8);

            byte[] pixIndices = new byte[numBlocksH * numBlocksW * 32];

            List<Color32> palColors = new List<Color32>(Width * Height);
            for (int i = 0; i < Width * Height * 4; i += 4)
            {
                palColors.Add(new Color32(
                    m_rgbaImageData[i + 0],
                    m_rgbaImageData[i + 1],
                    m_rgbaImageData[i + 2],
                    m_rgbaImageData[i + 3]));
            }

            Dictionary<Color32, byte> pixelColorIndexes = new Dictionary<Color32, byte>();
            foreach (Color32 col in palColors)
            {
                if (!pixelColorIndexes.ContainsKey(col))
                    EncodeColor(PaletteFormat, col, rawColorData, pixelColorIndexes);
            }

            int pixIndex = 0;

            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 8; pY++)
                    {
                        int srcY = yBlock * 8 + pY;

                        for (int pX = 0; pX < 8; pX += 2)
                        {
                            int srcX1 = xBlock * 8 + pX;
                            int srcX2 = srcX1 + 1;

                            byte idx1, idx2;

                            if (srcY < Height && srcX1 < Width)
                            {
                                int srcIdx = srcY * Width + srcX1;
                                idx1 = (byte)(pixelColorIndexes[palColors[srcIdx]] & 0xF);
                            }
                            else
                                idx1 = 0; // padding

                            if (srcY < Height && srcX2 < Width)
                            {
                                int srcIdx = srcY * Width + srcX2;
                                idx2 = (byte)(pixelColorIndexes[palColors[srcIdx]] & 0xF);
                            }
                            else
                                idx2 = 0; // padding

                            pixIndices[pixIndex++] = (byte)((idx1 << 4) | idx2);
                        }
                    }
                }
            }
            return new Tuple<byte[], ushort[]>(pixIndices, rawColorData.ToArray());
        }

        public static Tuple<byte[], ushort[]> EncodeC8(GcnPaletteFormats PaletteFormat, byte[] m_rgbaImageData, List<ushort> rawColorData, int Width, int Height)
        {
            List<Color32> palColors = new List<Color32>();

            uint numBlocksW = (uint)(Width + 7) / 8;
            uint numBlocksH = (uint)(Height + 3) / 4;

            int paddedWidth = (int)(numBlocksW * 8);
            int paddedHeight = (int)(numBlocksH * 4);

            byte[] pixIndices = new byte[paddedWidth * paddedHeight];

            for (int i = 0; i < (Width * Height) * 4; i += 4)
                palColors.Add(new Color32(m_rgbaImageData[i + 0], m_rgbaImageData[i + 1], m_rgbaImageData[i + 2], m_rgbaImageData[i + 3]));

            Dictionary<Color32, byte> pixelColorIndexes = new Dictionary<Color32, byte>();
            foreach (Color32 col in palColors)
            {
                EncodeColor(PaletteFormat, col, rawColorData, pixelColorIndexes);
            }

            int pixIndex = 0;
            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        int srcY = yBlock * 4 + pY; 
                        bool yInside = srcY < Height;

                        for (int pX = 0; pX < 8; pX++)
                        {
                            int srcX = xBlock * 8 + pX;

                            byte paletteIdx;
                            if (srcY < Height && srcX < Width)
                            {
                                int srcIdx = srcY * Width + srcX;
                                paletteIdx = pixelColorIndexes[palColors[srcIdx]];
                            }
                            else
                            {
                                paletteIdx = 0;
                            }

                            pixIndices[pixIndex++] = paletteIdx;
                        }
                    }
                }
            }

            return new Tuple<byte[], ushort[]>(pixIndices, rawColorData.ToArray());
        }

        public static Tuple<byte[], ushort[]> EncodeC14(GcnPaletteFormats PaletteFormat, byte[] m_rgbaImageData, List<ushort> rawColorData, int Width, int Height)
        {
            uint numBlocksW = (uint)((Width + 3) / 4);
            uint numBlocksH = (uint)((Height + 3) / 4);

            byte[] pixIndices = new byte[numBlocksH * numBlocksW * 32];

            List<Color32> palColors = new List<Color32>(Width * Height);
            for (int i = 0; i < Width * Height * 4; i += 4)
            {
                palColors.Add(new Color32(
                    m_rgbaImageData[i + 0],
                    m_rgbaImageData[i + 1],
                    m_rgbaImageData[i + 2],
                    m_rgbaImageData[i + 3]));
            }

            Dictionary<Color32, ushort> pixelColorIndexes = new Dictionary<Color32, ushort>();
            foreach (Color32 col in palColors)
            {
                if (!pixelColorIndexes.ContainsKey(col))
                    EncodeColor(PaletteFormat, col, rawColorData, pixelColorIndexes);
            }

            int pixIndex = 0;

            for (int yBlock = 0; yBlock < numBlocksH; yBlock++)
            {
                for (int xBlock = 0; xBlock < numBlocksW; xBlock++)
                {
                    for (int pY = 0; pY < 4; pY++)
                    {
                        int srcY = yBlock * 4 + pY;

                        for (int pX = 0; pX < 4; pX++)
                        {
                            int srcX = xBlock * 4 + pX;

                            ushort index;
                            if (srcY < Height && srcX < Width)
                            {
                                int srcIdx = srcY * Width + srcX;
                                index = pixelColorIndexes[palColors[srcIdx]];
                            }
                            else
                                index = 0;

                            // 14-bit index: low byte + 6 bits from high byte
                            pixIndices[pixIndex++] = (byte)(index & 0xFF);           // Low 8 bits
                            pixIndices[pixIndex++] = (byte)((index >> 8) & 0x3F);   // High 6 bits
                        }
                    }
                }
            }

            return new Tuple<byte[], ushort[]>(pixIndices, rawColorData.ToArray());
        }

        private static void UnpackPixelFromPalette(int paletteIndex, ref byte[] dest, int offset, byte[] paletteData, GcnPaletteFormats format)
        {
            if (paletteIndex == 255) return;

            switch (format)
            {
                case GcnPaletteFormats.IA8:
                    byte gray = paletteData[2 * paletteIndex + 0];
                    byte alpha = paletteData[2 * paletteIndex + 1];
                    dest[offset + 0] = gray; 
                    dest[offset + 1] = gray;
                    dest[offset + 2] = gray;
                    dest[offset + 3] = alpha;
                    break;
                case GcnPaletteFormats.RGB565:
                    {
                        ushort palettePixelData = (ushort)(Buffer.GetByte(paletteData, 2 * paletteIndex) << 8 | Buffer.GetByte(paletteData, 2 * paletteIndex + 1));
                        RGB565ToRGBA8(palettePixelData, ref dest, offset);
                    }
                    break;
                case GcnPaletteFormats.RGB5A3:
                    {
                        ushort palettePixelData = (ushort)(Buffer.GetByte(paletteData, 2 * paletteIndex) << 8 | Buffer.GetByte(paletteData, 2 * paletteIndex + 1));
                        RGB5A3ToRGBA8(palettePixelData, ref dest, offset);
                    }
                    break;
            }
        }

        // Packs palette format color
        private static void EncodeColor(GcnPaletteFormats PaletteFormat, Color32 col, List<ushort> rawColorData, Dictionary<Color32, byte> pixelColorIndexes)
        {
            switch (PaletteFormat)
            {
                case GcnPaletteFormats.IA8:
                    byte i = (byte)(col.R * 0.2126 + col.G * 0.7152 + col.B * 0.0722);

                    ushort fullIA8 = (ushort)(i << 8 | col.A);
                    if (!rawColorData.Contains(fullIA8))
                        rawColorData.Add(fullIA8);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (byte)rawColorData.IndexOf(fullIA8));
                    break;
                case GcnPaletteFormats.RGB565:
                    ushort r_565 = (ushort)(col.R >> 3);
                    ushort g_565 = (ushort)(col.G >> 2);
                    ushort b_565 = (ushort)(col.B >> 3);

                    ushort fullColor565 = 0;
                    fullColor565 |= b_565;
                    fullColor565 |= (ushort)(g_565 << 5);
                    fullColor565 |= (ushort)(r_565 << 11);

                    if (!rawColorData.Contains(fullColor565))
                        rawColorData.Add(fullColor565);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (byte)rawColorData.IndexOf(fullColor565));
                    break;
                case GcnPaletteFormats.RGB5A3:
                    ushort r_53 = (ushort)(col.R >> 4);
                    ushort g_53 = (ushort)(col.G >> 4);
                    ushort b_53 = (ushort)(col.B >> 4);
                    ushort a_53 = (ushort)(col.A >> 5);

                    ushort fullColor53 = 0;
                    fullColor53 |= b_53;
                    fullColor53 |= (ushort)(g_53 << 4);
                    fullColor53 |= (ushort)(r_53 << 8);
                    fullColor53 |= (ushort)(a_53 << 12);

                    if (!rawColorData.Contains(fullColor53))
                        rawColorData.Add(fullColor53);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (byte)rawColorData.IndexOf(fullColor53));
                    break;
            }
        }
        private static void EncodeColor(GcnPaletteFormats PaletteFormat, Color32 col, List<ushort> rawColorData, Dictionary<Color32, ushort> pixelColorIndexes)
        {
            switch (PaletteFormat)
            {
                case GcnPaletteFormats.IA8:
                    byte i = (byte)(col.R * 0.2126 + col.G * 0.7152 + col.B * 0.0722);

                    ushort fullIA8 = (ushort)(i << 8 | col.A);
                    if (!rawColorData.Contains(fullIA8))
                        rawColorData.Add(fullIA8);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (ushort)rawColorData.IndexOf(fullIA8));
                    break;
                case GcnPaletteFormats.RGB565:
                    ushort r_565 = (ushort)(col.R >> 3);
                    ushort g_565 = (ushort)(col.G >> 2);
                    ushort b_565 = (ushort)(col.B >> 3);

                    ushort fullColor565 = 0;
                    fullColor565 |= b_565;
                    fullColor565 |= (ushort)(g_565 << 5);
                    fullColor565 |= (ushort)(r_565 << 11);

                    if (!rawColorData.Contains(fullColor565))
                        rawColorData.Add(fullColor565);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (ushort)rawColorData.IndexOf(fullColor565));
                    break;
                case GcnPaletteFormats.RGB5A3:
                    ushort r_53 = (ushort)(col.R >> 4);
                    ushort g_53 = (ushort)(col.G >> 4);
                    ushort b_53 = (ushort)(col.B >> 4);
                    ushort a_53 = (ushort)(col.A >> 5);

                    ushort fullColor53 = 0;
                    fullColor53 |= b_53;
                    fullColor53 |= (ushort)(g_53 << 4);
                    fullColor53 |= (ushort)(r_53 << 8);
                    fullColor53 |= (ushort)(a_53 << 12);

                    if (!rawColorData.Contains(fullColor53))
                        rawColorData.Add(fullColor53);
                    if (!pixelColorIndexes.ContainsKey(col))
                        pixelColorIndexes.Add(col, (ushort)rawColorData.IndexOf(fullColor53));
                    break;
            }
        }

        /// <summary>
        /// Convert a RGB565 encoded pixel (two bytes in length) to a RGBA (4 byte in length)
        /// pixel.
        /// </summary>
        /// <param name="sourcePixel">RGB565 encoded pixel.</param>
        /// <param name="dest">Destination array for RGBA pixel.</param>
        /// <param name="destOffset">Offset into destination array to write RGBA pixel.</param>
        private static void RGB565ToRGBA8(ushort sourcePixel, ref byte[] dest, int destOffset)
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

            dest[destOffset] = r;
            dest[destOffset + 1] = g;
            dest[destOffset + 2] = b;
            dest[destOffset + 3] = 0xFF; //Set alpha to 1
        }

        /// <summary>
        /// Convert a RGB5A3 encoded pixel (two bytes in length) to an RGBA (4 byte in length)
        /// pixel.
        /// </summary>
        /// <param name="sourcePixel">RGB5A3 encoded pixel.</param>
        /// <param name="dest">Destination array for RGBA pixel.</param>
        /// <param name="destOffset">Offset into destination array to write RGBA pixel.</param>
        private static void RGB5A3ToRGBA8(ushort sourcePixel, ref byte[] dest, int destOffset)
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

            dest[destOffset + 0] = r;
            dest[destOffset + 1] = g;
            dest[destOffset + 2] = b;
            dest[destOffset + 3] = a;
        }
    }
}
