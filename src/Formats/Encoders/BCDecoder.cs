using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders
{
    public class BCDecoder
    {
        public static byte[] DecodeBC1(byte[] input, int width, int height)
        {
            int blockWidth = (width + 3) / 4;
            int blockHeight = (height + 3) / 4;
            byte[] output = new byte[width * height * 4]; // RGBA output

            using (MemoryStream stream = new MemoryStream(input))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                for (int by = 0; by < blockHeight; by++)
                {
                    for (int bx = 0; bx < blockWidth; bx++)
                    {
                        ushort color0 = reader.ReadUInt16();
                        ushort color1 = reader.ReadUInt16();
                        uint indices = reader.ReadUInt32();

                        byte[] colorPalette = new byte[4 * 4];
                        byte[] rgbColor0 = RGB565ToRGB888(color0);
                        byte[] rgbColor1 = RGB565ToRGB888(color1);

                        Array.Copy(rgbColor0, 0, colorPalette, 0, 3);
                        Array.Copy(rgbColor1, 0, colorPalette, 4, 3);

                        if (color0 > color1)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                colorPalette[8 + i] = (byte)((2 * rgbColor0[i] + rgbColor1[i]) / 3);
                                colorPalette[12 + i] = (byte)((rgbColor0[i] + 2 * rgbColor1[i]) / 3);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                colorPalette[8 + i] = (byte)((rgbColor0[i] + rgbColor1[i]) / 2);
                                colorPalette[12 + i] = 0;
                            }
                        }

                        for (int y = 0; y < 4; y++)
                        {
                            for (int x = 0; x < 4; x++)
                            {
                                int pixelIndex = ((by * 4 + y) * width + bx * 4 + x) * 4;
                                if (pixelIndex < output.Length)
                                {
                                    int index = (int)(indices & 0x03) * 4;
                                    output[pixelIndex] = colorPalette[index];     // R
                                    output[pixelIndex + 1] = colorPalette[index + 1]; // G
                                    output[pixelIndex + 2] = colorPalette[index + 2]; // B
                                    output[pixelIndex + 3] = 255; // A (fully opaque)
                                    indices >>= 2;
                                }
                            }
                        }
                    }
                }
            }

            return output;
        }

        public static byte[] DecodeBC2(byte[] input, int width, int height)
        {
            int blockWidth = (width + 3) / 4;
            int blockHeight = (height + 3) / 4;
            byte[] output = new byte[width * height * 4]; // RGBA output

            using (MemoryStream stream = new MemoryStream(input))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                for (int by = 0; by < blockHeight; by++)
                {
                    for (int bx = 0; bx < blockWidth; bx++)
                    {
                        // Decode alpha block
                        byte[] alphaValues = new byte[16];
                        for (int i = 0; i < 4; i++)
                        {
                            ushort alphaRow = reader.ReadUInt16();
                            for (int j = 0; j < 4; j++)
                            {
                                alphaValues[i * 4 + j] = (byte)(alphaRow >> j * 4 & 0x0F);
                                alphaValues[i * 4 + j] = (byte)(alphaValues[i * 4 + j] | alphaValues[i * 4 + j] << 4); // Expand 4-bit to 8-bit
                            }
                        }

                        // Decode color block
                        ushort color0 = reader.ReadUInt16();
                        ushort color1 = reader.ReadUInt16();
                        uint indices = reader.ReadUInt32();

                        byte[] colorPalette = new byte[4 * 4];
                        byte[] rgbColor0 = RGB565ToRGB888(color0);
                        byte[] rgbColor1 = RGB565ToRGB888(color1);

                        Array.Copy(rgbColor0, 0, colorPalette, 0, 3);
                        Array.Copy(rgbColor1, 0, colorPalette, 4, 3);

                        if (color0 > color1)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                colorPalette[8 + i] = (byte)((2 * rgbColor0[i] + rgbColor1[i]) / 3);
                                colorPalette[12 + i] = (byte)((rgbColor0[i] + 2 * rgbColor1[i]) / 3);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                colorPalette[8 + i] = (byte)((rgbColor0[i] + rgbColor1[i]) / 2);
                                colorPalette[12 + i] = 0;
                            }
                        }

                        // Combine alpha and color data
                        for (int y = 0; y < 4; y++)
                        {
                            for (int x = 0; x < 4; x++)
                            {
                                int pixelIndex = ((by * 4 + y) * width + bx * 4 + x) * 4;
                                if (pixelIndex < output.Length)
                                {
                                    int colorIndex = (int)(indices & 0x03) * 4;
                                    output[pixelIndex] = colorPalette[colorIndex];     // R
                                    output[pixelIndex + 1] = colorPalette[colorIndex + 1]; // G
                                    output[pixelIndex + 2] = colorPalette[colorIndex + 2]; // B
                                    output[pixelIndex + 3] = alphaValues[y * 4 + x]; // A
                                    indices >>= 2;
                                }
                            }
                        }
                    }
                }
            }

            return output;
        }

        public static byte[] DecodeBC3(byte[] input, int width, int height)
        {
            int blockWidth = (width + 3) / 4;
            int blockHeight = (height + 3) / 4;
            byte[] output = new byte[width * height * 4]; // RGBA output

            using (MemoryStream stream = new MemoryStream(input))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                for (int by = 0; by < blockHeight; by++)
                {
                    for (int bx = 0; bx < blockWidth; bx++)
                    {
                        // Decode alpha block
                        byte[] alphaPalette = new byte[8];
                        byte minAlpha = reader.ReadByte();
                        byte maxAlpha = reader.ReadByte();
                        ulong alphaIndices = reader.ReadUInt32();
                        alphaIndices |= (ulong)reader.ReadUInt16() << 32;

                        alphaPalette[0] = minAlpha;
                        alphaPalette[1] = maxAlpha;

                        if (minAlpha > maxAlpha)
                        {
                            for (int i = 1; i < 7; i++)
                            {
                                alphaPalette[i + 1] = (byte)(((7 - i) * minAlpha + i * maxAlpha) / 7);
                            }
                        }
                        else
                        {
                            for (int i = 1; i < 5; i++)
                            {
                                alphaPalette[i + 1] = (byte)(((5 - i) * minAlpha + i * maxAlpha) / 5);
                            }
                            alphaPalette[6] = 0;
                            alphaPalette[7] = 255;
                        }

                        // Decode color block
                        ushort color0 = reader.ReadUInt16();
                        ushort color1 = reader.ReadUInt16();
                        uint colorIndices = reader.ReadUInt32();

                        byte[] colorPalette = new byte[4 * 4];
                        byte[] rgbColor0 = RGB565ToRGB888(color0);
                        byte[] rgbColor1 = RGB565ToRGB888(color1);

                        Array.Copy(rgbColor0, 0, colorPalette, 0, 3);
                        Array.Copy(rgbColor1, 0, colorPalette, 4, 3);

                        if (color0 > color1)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                colorPalette[8 + i] = (byte)((2 * rgbColor0[i] + rgbColor1[i]) / 3);
                                colorPalette[12 + i] = (byte)((rgbColor0[i] + 2 * rgbColor1[i]) / 3);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                colorPalette[8 + i] = (byte)((rgbColor0[i] + rgbColor1[i]) / 2);
                                colorPalette[12 + i] = 0;
                            }
                        }

                        // Combine alpha and color data
                        for (int y = 0; y < 4; y++)
                        {
                            for (int x = 0; x < 4; x++)
                            {
                                int pixelIndex = ((by * 4 + y) * width + bx * 4 + x) * 4;
                                if (pixelIndex < output.Length)
                                {
                                    int colorIndex = (int)(colorIndices & 0x03) * 4;
                                    int alphaIndex = (int)(alphaIndices & 0x07);

                                    output[pixelIndex] = colorPalette[colorIndex];     // R
                                    output[pixelIndex + 1] = colorPalette[colorIndex + 1]; // G
                                    output[pixelIndex + 2] = colorPalette[colorIndex + 2]; // B
                                    output[pixelIndex + 3] = alphaPalette[alphaIndex]; // A

                                    colorIndices >>= 2;
                                    alphaIndices >>= 3;
                                }
                            }
                        }
                    }
                }
            }

            return output;
        }

        public static byte[] DecodeBC4(byte[] input, int width, int height, bool snorm = false, bool IsAlpha = false)
        {
            int blockWidth = (width + 3) / 4;
            int blockHeight = (height + 3) / 4;
            byte[] output = new byte[width * height * 4];

            using (MemoryStream stream = new MemoryStream(input))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                for (int by = 0; by < blockHeight; by++)
                {
                    for (int bx = 0; bx < blockWidth; bx++)
                    {
                        byte min = reader.ReadByte();
                        byte max = reader.ReadByte();
                        ulong indices = reader.ReadUInt32();
                        indices |= (ulong)reader.ReadUInt16() << 32;

                        byte[] palette = new byte[8];
                        palette[0] = min;
                        palette[1] = max;

                        if (min > max)
                        {
                            for (int i = 1; i < 7; i++)
                            {
                                palette[i + 1] = (byte)(((7 - i) * min + i * max) / 7);
                            }
                        }
                        else
                        {
                            for (int i = 1; i < 5; i++)
                            {
                                palette[i + 1] = (byte)(((5 - i) * min + i * max) / 5);
                            }
                            palette[6] = 0;
                            palette[7] = 255;
                        }

                        for (int y = 0; y < 4; y++)
                        {
                            for (int x = 0; x < 4; x++)
                            {
                                int pixelIndex = ((by * 4 + y) * width + bx * 4 + x) * 4;
                                if (pixelIndex < output.Length)
                                {
                                    int index = (int)(indices & 0x07);
                                    byte value = palette[index];
                                    if (IsAlpha)
                                    {
                                        output[pixelIndex] = 255; // R
                                        output[pixelIndex + 1] = 255; // G
                                        output[pixelIndex + 2] = 255; // B
                                        output[pixelIndex + 3] = value;
                                    }
                                    else
                                    {
                                        output[pixelIndex] = value; // R
                                        output[pixelIndex + 1] = value; // G
                                        output[pixelIndex + 2] = value; // B
                                        output[pixelIndex + 3] = 255;
                                    }

                                    indices >>= 3;
                                }
                            }
                        }
                    }
                }
            }

            return output;
        }

        public static byte[] DecodeBC5(byte[] input, int width, int height, bool IsSNORM = false, bool IsAlpha = false)
        {
            int W = (width + 3) / 4;
            int H = (height + 3) / 4;

            byte[] output = new byte[width * height * 4];

            for (int Y = 0; Y < H; Y++)
            {
                for (int X = 0; X < W; X++)
                {
                    int IOffs = (Y * W + X) * 16;
                    byte[] Red = new byte[8];
                    byte[] Green = new byte[8];

                    Red[0] = input[IOffs + 0];
                    Red[1] = input[IOffs + 1];

                    Green[0] = input[IOffs + 8];
                    Green[1] = input[IOffs + 9];

                    if (IsSNORM == true)
                    {
                        CalculateBC3AlphaS(Red);
                        CalculateBC3AlphaS(Green);
                    }
                    else
                    {
                        CalculateBC3Alpha(Red);
                        CalculateBC3Alpha(Green);
                    }

                    int RedLow = Get32(input, IOffs + 2);
                    int RedHigh = Get16(input, IOffs + 6);

                    int GreenLow = Get32(input, IOffs + 10);
                    int GreenHigh = Get16(input, IOffs + 14);

                    ulong RedCh = (uint)RedLow | (ulong)RedHigh << 32;
                    ulong GreenCh = (uint)GreenLow | (ulong)GreenHigh << 32;

                    int TW = Math.Min(width - X * 4, 4);
                    int TH = Math.Min(height - Y * 4, 4);

                    if (IsSNORM == true)
                    {
                        for (int TY = 0; TY < TH; TY++)
                        {
                            for (int TX = 0; TX < TW; TX++)
                            {

                                int Shift = TY * 12 + TX * 3;
                                int OOffset = ((Y * 4 + TY) * width + X * 4 + TX) * 4;

                                byte RedPx = Red[RedCh >> Shift & 7];
                                byte GreenPx = Green[GreenCh >> Shift & 7];

                                if (IsSNORM == true)
                                {
                                    RedPx += 0x80;
                                    GreenPx += 0x80;
                                }

                                float NX = RedPx / 255f * 2 - 1;
                                float NY = GreenPx / 255f * 2 - 1;
                                float NZ = (float)Math.Sqrt(1 - (NX * NX + NY * NY));

                                if (IsAlpha)
                                {
                                    output[OOffset + 0] = Clamp((NX + 1) * 0.5f);
                                    output[OOffset + 1] = Clamp((NX + 1) * 0.5f);
                                    output[OOffset + 2] = Clamp((NX + 1) * 0.5f);
                                    output[OOffset + 3] = Clamp((NY + 1) * 0.5f);
                                }
                                else
                                {
                                    output[OOffset + 0] = Clamp((NX + 1) * 0.5f);
                                    output[OOffset + 1] = Clamp((NY + 1) * 0.5f);
                                    output[OOffset + 2] = Clamp((NZ + 1) * 0.5f);
                                    output[OOffset + 3] = 0xff;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int TY = 0; TY < TH; TY++)
                        {
                            for (int TX = 0; TX < TW; TX++)
                            {

                                int Shift = TY * 12 + TX * 3;
                                int OOffset = ((Y * 4 + TY) * width + X * 4 + TX) * 4;

                                byte RedPx = Red[RedCh >> Shift & 7];
                                byte GreenPx = Green[GreenCh >> Shift & 7];

                                if (IsAlpha)
                                {
                                    output[OOffset + 0] = RedPx;
                                    output[OOffset + 1] = RedPx;
                                    output[OOffset + 2] = RedPx;
                                    output[OOffset + 3] = GreenPx;
                                }
                                else
                                {
                                    output[OOffset + 0] = RedPx;
                                    output[OOffset + 1] = GreenPx;
                                    output[OOffset + 2] = 255;
                                    output[OOffset + 3] = 255;
                                }

                            }
                        }
                    }
                }
            }
            return output;
        }

        private static byte Clamp(float Value)
        {
            if (Value > 1) return 0xff;
            else if (Value < 0) return 0;
            else return (byte)(Value * 0xff);
        }


        private static void CalculateBC3Alpha(byte[] Alpha)
        {
            if (Alpha[0] > Alpha[1])
            {
                for (int i = 2; i < 8; i++)
                    Alpha[i] = (byte)(Alpha[0] + (Alpha[1] - Alpha[0]) * (i - 1) / 7);
            }
            else
            {
                for (int i = 2; i < 6; i++)
                    Alpha[i] = (byte)(Alpha[0] + (Alpha[1] - Alpha[0]) * (i - 1) / 5);
                Alpha[6] = 0;
                Alpha[7] = 255;
            }
        }
        private static void CalculateBC3AlphaS(byte[] Alpha)
        {
            if ((sbyte)Alpha[0] > (sbyte)Alpha[1])
            {
                for (int i = 2; i < 8; i++)
                    Alpha[i] = (byte)(Alpha[0] + ((sbyte)Alpha[1] - (sbyte)Alpha[0]) * (i - 1) / 7);
            }
            else
            {
                for (int i = 2; i < 6; i++)
                    Alpha[i] = (byte)(Alpha[0] + ((sbyte)Alpha[1] - (sbyte)Alpha[0]) * (i - 1) / 5);
                Alpha[6] = 0x80;
                Alpha[7] = 0x7f;
            }
        }

        private static byte[] RGB565ToRGB888(ushort color)
        {
            byte[] rgb = new byte[3];
            rgb[0] = (byte)(color >> 11 & 0x1F); // R
            rgb[1] = (byte)(color >> 5 & 0x3F);  // G
            rgb[2] = (byte)(color & 0x1F);        // B

            // Convert to 8-bit per channel
            rgb[0] = (byte)(rgb[0] << 3 | rgb[0] >> 2);
            rgb[1] = (byte)(rgb[1] << 2 | rgb[1] >> 4);
            rgb[2] = (byte)(rgb[2] << 3 | rgb[2] >> 2);

            return rgb;
        }

        public static int Get16(byte[] Data, int Address)
        {
            return
                Data[Address + 0] << 0 |
                Data[Address + 1] << 8;
        }

        public static int Get32(byte[] Data, int Address)
        {
            return
                Data[Address + 0] << 0 |
                Data[Address + 1] << 8 |
                Data[Address + 2] << 16 |
                Data[Address + 3] << 24;
        }
    }
}
