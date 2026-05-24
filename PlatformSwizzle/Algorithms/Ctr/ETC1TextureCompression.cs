using ImageLibrary.Utils;
using System;
using System.Drawing;
using System.IO;

namespace ImageLibrary.PlatformSwizzle.Algorithms.Ctr
{
    public static class ETC1TextureCompression
    {
        private static byte[] XT = { 0, 4, 0, 4 };
        private static byte[] YT = { 0, 0, 4, 4 };

        public static byte[] Decode(byte[] Input, int Width, int Height, bool Alpha)
        {
            byte[] Output = new byte[Width * Height * 4];

            using (MemoryStream MS = new MemoryStream(Input))
            {
                BinaryReader Reader = new BinaryReader(MS);

                for (int TY = 0; TY < Height; TY += 8)
                {
                    for (int TX = 0; TX < Width; TX += 8)
                    {
                        for (int T = 0; T < 4; T++)
                        {
                            ulong AlphaBlock = 0xfffffffffffffffful;

                            if (Alpha) AlphaBlock = Reader.ReadUInt64();

                            ulong ColorBlock = BitUtils.Swap64(Reader.ReadUInt64());

                            byte[] Tile = ETC1Tile(ColorBlock);

                            int TileOffset = 0;

                            for (int PY = YT[T]; PY < 4 + YT[T]; PY++)
                            {
                                for (int PX = XT[T]; PX < 4 + XT[T]; PX++)
                                {
                                    int OOffs = ((Height - 1 - (TY + PY)) * Width + TX + PX) * 4;

                                    Buffer.BlockCopy(Tile, TileOffset, Output, OOffs, 3);

                                    int AlphaShift = (PX & 3) * 4 + (PY & 3) << 2;

                                    byte A = (byte)(AlphaBlock >> AlphaShift & 0xf);

                                    Output[OOffs + 3] = (byte)(A << 4 | A);

                                    TileOffset += 4;
                                }
                            }
                        }
                    }
                }

                return Output;
            }
        }

        private static byte[] ETC1Tile(ulong Block)
        {
            uint BlockLow = (uint)(Block >> 32);
            uint BlockHigh = (uint)(Block >> 0);

            bool Flip = (BlockHigh & 0x1000000) != 0;
            bool Diff = (BlockHigh & 0x2000000) != 0;

            uint R1, G1, B1;
            uint R2, G2, B2;

            if (Diff)
            {
                B1 = (BlockHigh & 0x0000f8) >> 0;
                G1 = (BlockHigh & 0x00f800) >> 8;
                R1 = (BlockHigh & 0xf80000) >> 16;

                B2 = (uint)((sbyte)(B1 >> 3) + ((sbyte)((BlockHigh & 0x000007) << 5) >> 5));
                G2 = (uint)((sbyte)(G1 >> 3) + ((sbyte)((BlockHigh & 0x000700) >> 3) >> 5));
                R2 = (uint)((sbyte)(R1 >> 3) + ((sbyte)((BlockHigh & 0x070000) >> 11) >> 5));

                B1 |= B1 >> 5;
                G1 |= G1 >> 5;
                R1 |= R1 >> 5;

                B2 = B2 << 3 | B2 >> 2;
                G2 = G2 << 3 | G2 >> 2;
                R2 = R2 << 3 | R2 >> 2;
            }
            else
            {
                B1 = (BlockHigh & 0x0000f0) >> 0;
                G1 = (BlockHigh & 0x00f000) >> 8;
                R1 = (BlockHigh & 0xf00000) >> 16;

                B2 = (BlockHigh & 0x00000f) << 4;
                G2 = (BlockHigh & 0x000f00) >> 4;
                R2 = (BlockHigh & 0x0f0000) >> 12;

                B1 |= B1 >> 4;
                G1 |= G1 >> 4;
                R1 |= R1 >> 4;

                B2 |= B2 >> 4;
                G2 |= G2 >> 4;
                R2 |= R2 >> 4;
            }

            uint Table1 = BlockHigh >> 29 & 7;
            uint Table2 = BlockHigh >> 26 & 7;

            byte[] Output = new byte[4 * 4 * 4];

            if (!Flip)
            {
                for (int Y = 0; Y < 4; Y++)
                {
                    for (int X = 0; X < 2; X++)
                    {
                        Color Color1 = ETC1Pixel(R1, G1, B1, X + 0, Y, BlockLow, Table1);
                        Color Color2 = ETC1Pixel(R2, G2, B2, X + 2, Y, BlockLow, Table2);

                        int Offset1 = (Y * 4 + X) * 4;

                        Output[Offset1 + 0] = Color1.B;
                        Output[Offset1 + 1] = Color1.G;
                        Output[Offset1 + 2] = Color1.R;

                        int Offset2 = (Y * 4 + X + 2) * 4;

                        Output[Offset2 + 0] = Color2.B;
                        Output[Offset2 + 1] = Color2.G;
                        Output[Offset2 + 2] = Color2.R;
                    }
                }
            }
            else
            {
                for (int Y = 0; Y < 2; Y++)
                {
                    for (int X = 0; X < 4; X++)
                    {
                        Color Color1 = ETC1Pixel(R1, G1, B1, X, Y + 0, BlockLow, Table1);
                        Color Color2 = ETC1Pixel(R2, G2, B2, X, Y + 2, BlockLow, Table2);

                        int Offset1 = (Y * 4 + X) * 4;

                        Output[Offset1 + 0] = Color1.B;
                        Output[Offset1 + 1] = Color1.G;
                        Output[Offset1 + 2] = Color1.R;

                        int Offset2 = ((Y + 2) * 4 + X) * 4;

                        Output[Offset2 + 0] = Color2.B;
                        Output[Offset2 + 1] = Color2.G;
                        Output[Offset2 + 2] = Color2.R;
                    }
                }
            }

            return Output;
        }

        private static int[,] ETC1LUT =
        {
            {    2,   8,    -2,   -8 },
            {    5,   17,   -5,  -17 },
            {    9,   29,   -9,  -29 },
            {   13,   42,  -13,  -42 },
            {   18,   60,  -18,  -60 },
            {   24,   80,  -24,  -80 },
            {   33,  106,  -33, -106 },
            {   47,  183,  -47, -183 }
        };

        private static Color ETC1Pixel(uint R, uint G, uint B, int X, int Y, uint Block, uint Table)
        {
            int Index = X * 4 + Y;
            uint MSB = Block << 1;

            int Pixel = Index < 8
                ? ETC1LUT[Table, (Block >> Index + 24 & 1) + (MSB >> Index + 8 & 2)]
                : ETC1LUT[Table, (Block >> Index + 8 & 1) + (MSB >> Index - 8 & 2)];

            R = Saturate((int)(R + Pixel));
            G = Saturate((int)(G + Pixel));
            B = Saturate((int)(B + Pixel));

            return Color.FromArgb((int)R, (int)G, (int)B);
        }

        private static byte Saturate(int Value)
        {
            if (Value > byte.MaxValue) return byte.MaxValue;
            if (Value < byte.MinValue) return byte.MinValue;

            return (byte)Value;
        }


        #region ETC1 Encoding

        public static byte[] Encode(byte[] Data, int Width, int Height, bool hasAlpha)
        {
            byte[] Out_Data = null;

            // Os tiles com compressão ETC1 no 3DS estão embaralhados
            byte[] Out = new byte[(Width * Height * 4)];
            int[] Tile_Scramble = Get_ETC1_Scramble(Width, Height);

            int i = 0;
            for (int Tile_Y = 0; Tile_Y <= (Height / 4) - 1; Tile_Y++)
            {
                for (int Tile_X = 0; Tile_X <= (Width / 4) - 1; Tile_X++)
                {
                    int TX = Tile_Scramble[i] % (Width / 4);
                    int TY = (Tile_Scramble[i] - TX) / (Width / 4);
                    for (int Y = 0; Y <= 3; Y++)
                    {
                        for (int X = 0; X <= 3; X++)
                        {
                            int Out_Offset = ((TX * 4) + X + ((((TY * 4) + Y)) * Width)) * 4;
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;

                            Out[Out_Offset] = Data[Image_Offset + 0];
                            Out[Out_Offset + 1] = Data[Image_Offset + 1];
                            Out[Out_Offset + 2] = Data[Image_Offset + 2];
                            if (hasAlpha)
                                Out[Out_Offset + 3] = Data[Image_Offset + 3];
                            else
                                Out[Out_Offset + 3] = 0xFF;
                        }
                    }
                    i += 1;
                }
            }

            Out_Data = new byte[((Width * Height) / (!hasAlpha ? 2 : 1))];
            int Out_Data_Offset = 0;

            for (int Tile_Y = 0; Tile_Y <= (Height / 4) - 1; Tile_Y++)
            {
                for (int Tile_X = 0; Tile_X <= (Width / 4) - 1; Tile_X++)
                {
                    bool Flip = false;
                    bool Difference = false;
                    int Block_Top = 0;
                    int Block_Bottom = 0;

                    // Teste do Difference Bit
                    int Diff_Match_V = 0;
                    int Diff_Match_H = 0;
                    for (int Y = 0; Y <= 3; Y++)
                    {
                        for (int X = 0; X <= 1; X++)
                        {
                            int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + Y) * Width)) * 4;

                            byte Bits_R1 = Convert.ToByte(Out[Image_Offset_1] & 0xF8);
                            byte Bits_G1 = Convert.ToByte(Out[Image_Offset_1 + 1] & 0xF8);
                            byte Bits_B1 = Convert.ToByte(Out[Image_Offset_1 + 2] & 0xF8);

                            byte Bits_R2 = Convert.ToByte(Out[Image_Offset_2] & 0xF8);
                            byte Bits_G2 = Convert.ToByte(Out[Image_Offset_2 + 1] & 0xF8);
                            byte Bits_B2 = Convert.ToByte(Out[Image_Offset_2 + 2] & 0xF8);

                            if ((Bits_R1 == Bits_R2) & (Bits_G1 == Bits_G2) & (Bits_B1 == Bits_B2))
                                Diff_Match_V += 1;
                        }
                    }
                    for (int Y = 0; Y <= 1; Y++)
                    {
                        for (int X = 0; X <= 3; X++)
                        {
                            int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Image_Offset_2 = ((Tile_X * 4) + X + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                            byte Bits_R1 = Convert.ToByte(Out[Image_Offset_1] & 0xF8);
                            byte Bits_G1 = Convert.ToByte(Out[Image_Offset_1 + 1] & 0xF8);
                            byte Bits_B1 = Convert.ToByte(Out[Image_Offset_1 + 2] & 0xF8);

                            byte Bits_R2 = Convert.ToByte(Out[Image_Offset_2] & 0xF8);
                            byte Bits_G2 = Convert.ToByte(Out[Image_Offset_2 + 1] & 0xF8);
                            byte Bits_B2 = Convert.ToByte(Out[Image_Offset_2 + 2] & 0xF8);

                            if ((Bits_R1 == Bits_R2) & (Bits_G1 == Bits_G2) & (Bits_B1 == Bits_B2))
                                Diff_Match_H += 1;
                        }
                    }
                    if (Diff_Match_H == 8)
                    {
                        Difference = true;
                        Flip = true;
                    }
                    else if (Diff_Match_V == 8)
                        Difference = true;
                    else
                    {
                        int Test_R1 = 0;
                        int Test_G1 = 0;
                        int Test_B1 = 0;
                        int Test_R2 = 0;
                        int Test_G2 = 0;
                        int Test_B2 = 0;
                        for (int Y = 0; Y <= 1; Y++)
                        {
                            for (int X = 0; X <= 1; X++)
                            {
                                int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                                Test_R1 += Out[Image_Offset_1];
                                Test_G1 += Out[Image_Offset_1 + 1];
                                Test_B1 += Out[Image_Offset_1 + 2];

                                Test_R2 += Out[Image_Offset_2];
                                Test_G2 += Out[Image_Offset_2 + 1];
                                Test_B2 += Out[Image_Offset_2 + 2];
                            }
                        }

                        Test_R1 /= 8;
                        Test_G1 /= 8;
                        Test_B1 /= 8;

                        Test_R2 /= 8;
                        Test_G2 /= 8;
                        Test_B2 /= 8;

                        int Test_Luma_1 = Convert.ToInt32(0.299F * Test_R1 + 0.587F * Test_G1 + 0.114F * Test_B1);
                        int Test_Luma_2 = Convert.ToInt32(0.299F * Test_R2 + 0.587F * Test_G2 + 0.114F * Test_B2);
                        int Test_Flip_Diff = Math.Abs(Test_Luma_1 - Test_Luma_2);
                        if (Test_Flip_Diff > 48)
                            Flip = true;
                    }

                    int Avg_R1 = 0;
                    int Avg_G1 = 0;
                    int Avg_B1 = 0;
                    int Avg_R2 = 0;
                    int Avg_G2 = 0;
                    int Avg_B2 = 0;

                    // Primeiro, cálcula a média de cores de cada bloco
                    if (Flip)
                    {
                        for (int Y = 0; Y <= 1; Y++)
                        {
                            for (int X = 0; X <= 3; X++)
                            {
                                int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                int Image_Offset_2 = ((Tile_X * 4) + X + (((Tile_Y * 4) + (2 + Y)) * Width)) * 4;

                                Avg_R1 += Out[Image_Offset_1];
                                Avg_G1 += Out[Image_Offset_1 + 1];
                                Avg_B1 += Out[Image_Offset_1 + 2];

                                Avg_R2 += Out[Image_Offset_2];
                                Avg_G2 += Out[Image_Offset_2 + 1];
                                Avg_B2 += Out[Image_Offset_2 + 2];
                            }
                        }
                    }
                    else
                        for (int Y = 0; Y <= 3; Y++)
                        {
                            for (int X = 0; X <= 1; X++)
                            {
                                int Image_Offset_1 = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                                int Image_Offset_2 = ((Tile_X * 4) + (2 + X) + (((Tile_Y * 4) + Y) * Width)) * 4;

                                Avg_R1 += Out[Image_Offset_1];
                                Avg_G1 += Out[Image_Offset_1 + 1];
                                Avg_B1 += Out[Image_Offset_1 + 2];

                                Avg_R2 += Out[Image_Offset_2];
                                Avg_G2 += Out[Image_Offset_2 + 1];
                                Avg_B2 += Out[Image_Offset_2 + 2];
                            }
                        }

                    Avg_R1 /= 8;
                    Avg_G1 /= 8;
                    Avg_B1 /= 8;

                    Avg_R2 /= 8;
                    Avg_G2 /= 8;
                    Avg_B2 /= 8;

                    if (Difference)
                    {
                        // +============+
                        // | Difference |
                        // +============+
                        if ((Avg_R1 & 7) > 3)
                        {
                            Avg_R1 = Clip(Avg_R1 + 8); Avg_R2 = Clip(Avg_R2 + 8);
                        }
                        if ((Avg_G1 & 7) > 3)
                        {
                            Avg_G1 = Clip(Avg_G1 + 8); Avg_G2 = Clip(Avg_G2 + 8);
                        }
                        if ((Avg_B1 & 7) > 3)
                        {
                            Avg_B1 = Clip(Avg_B1 + 8); Avg_B2 = Clip(Avg_B2 + 8);
                        }

                        Block_Top = (Avg_R1 & 0xF8) | (((Avg_R2 - Avg_R1) / 8) & 7);
                        Block_Top = Block_Top | (((Avg_G1 & 0xF8) << 8) | ((((Avg_G2 - Avg_G1) / 8) & 7) << 8));
                        Block_Top = Block_Top | (((Avg_B1 & 0xF8) << 16) | ((((Avg_B2 - Avg_B1) / 8) & 7) << 16));

                        // Vamos ter certeza de que os mesmos valores obtidos pelo descompressor serão usados na comparação (modo Difference)
                        Avg_R1 = Block_Top & 0xF8;
                        Avg_G1 = (Block_Top & 0xF800) >> 8;
                        Avg_B1 = (Block_Top & 0xF80000) >> 16;

                        int R = Signed_Byte(Convert.ToByte(Avg_R1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 7) << 5)) >> 5);
                        int G = Signed_Byte(Convert.ToByte(Avg_G1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x700) >> 3)) >> 5);
                        int B = Signed_Byte(Convert.ToByte(Avg_B1 >> 3)) + (Signed_Byte(Convert.ToByte((Block_Top & 0x70000) >> 11)) >> 5);

                        Avg_R2 = R;
                        Avg_G2 = G;
                        Avg_B2 = B;

                        Avg_R1 = Avg_R1 + (Avg_R1 >> 5);
                        Avg_G1 = Avg_G1 + (Avg_G1 >> 5);
                        Avg_B1 = Avg_B1 + (Avg_B1 >> 5);

                        Avg_R2 = (Avg_R2 << 3) + (Avg_R2 >> 2);
                        Avg_G2 = (Avg_G2 << 3) + (Avg_G2 >> 2);
                        Avg_B2 = (Avg_B2 << 3) + (Avg_B2 >> 2);
                    }
                    else
                    {
                        // +============+
                        // | Individual |
                        // +============+
                        if ((Avg_R1 & 0xF) > 7)
                            Avg_R1 = Clip(Avg_R1 + 0x10);
                        if ((Avg_G1 & 0xF) > 7)
                            Avg_G1 = Clip(Avg_G1 + 0x10);
                        if ((Avg_B1 & 0xF) > 7)
                            Avg_B1 = Clip(Avg_B1 + 0x10);
                        if ((Avg_R2 & 0xF) > 7)
                            Avg_R2 = Clip(Avg_R2 + 0x10);
                        if ((Avg_G2 & 0xF) > 7)
                            Avg_G2 = Clip(Avg_G2 + 0x10);
                        if ((Avg_B2 & 0xF) > 7)
                            Avg_B2 = Clip(Avg_B2 + 0x10);

                        Block_Top = ((Avg_R2 & 0xF0) >> 4) | (Avg_R1 & 0xF0);
                        Block_Top = Block_Top | (((Avg_G2 & 0xF0) << 4) | ((Avg_G1 & 0xF0) << 8));
                        Block_Top = Block_Top | (((Avg_B2 & 0xF0) << 12) | ((Avg_B1 & 0xF0) << 16));

                        // Vamos ter certeza de que os mesmos valores obtidos pelo descompressor serão usados na comparação (modo Individual)
                        Avg_R1 = (Avg_R1 & 0xF0) + ((Avg_R1 & 0xF0) >> 4);
                        Avg_G1 = (Avg_G1 & 0xF0) + ((Avg_G1 & 0xF0) >> 4);
                        Avg_B1 = (Avg_B1 & 0xF0) + ((Avg_B1 & 0xF0) >> 4);

                        Avg_R2 = (Avg_R2 & 0xF0) + ((Avg_R2 & 0xF0) >> 4);
                        Avg_G2 = (Avg_G2 & 0xF0) + ((Avg_G2 & 0xF0) >> 4);
                        Avg_B2 = (Avg_B2 & 0xF0) + ((Avg_B2 & 0xF0) >> 4);
                    }

                    if (Flip)
                        Block_Top = Block_Top | 0x1000000;
                    if (Difference)
                        Block_Top = Block_Top | 0x2000000;

                    // Seleciona a melhor tabela para ser usada nos blocos
                    int Mod_Table_1 = 0;
                    int[] Min_Diff_1 = new int[8];
                    for (int a = 0; a <= 7; a++)
                        Min_Diff_1[a] = 0;
                    for (int Y = 0; Y <= (Flip ? 1 : 3); Y++)
                    {
                        for (int X = 0; X <= (Flip ? 3 : 1); X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            for (int a = 0; a <= 7; a++)
                            {
                                int Optimal_Diff = 255 * 4;
                                for (int b = 0; b <= 3; b++)
                                {
                                    int CR = Clip(Avg_R1 + Modulation_Table[a, b]);
                                    int CG = Clip(Avg_G1 + Modulation_Table[a, b]);
                                    int CB = Clip(Avg_B1 + Modulation_Table[a, b]);

                                    int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                    int Diff = Math.Abs(Luma - Test_Luma);
                                    if (Diff < Optimal_Diff)
                                        Optimal_Diff = Diff;
                                }
                                Min_Diff_1[a] += Optimal_Diff;
                            }
                        }
                    }

                    int Temp_1 = 255 * 8;
                    for (int a = 0; a <= 7; a++)
                    {
                        if (Min_Diff_1[a] < Temp_1)
                        {
                            Temp_1 = Min_Diff_1[a];
                            Mod_Table_1 = a;
                        }
                    }

                    int Mod_Table_2 = 0;
                    int[] Min_Diff_2 = new int[8];
                    for (int a = 0; a <= 7; a++)
                        Min_Diff_2[a] = 0;
                    for (int Y = Flip ? 2 : 0; Y <= 3; Y++)
                    {
                        for (int X = Flip ? 0 : 2; X <= 3; X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            for (int a = 0; a <= 7; a++)
                            {
                                int Optimal_Diff = 255 * 4;
                                for (int b = 0; b <= 3; b++)
                                {
                                    int CR = Clip(Avg_R2 + Modulation_Table[a, b]);
                                    int CG = Clip(Avg_G2 + Modulation_Table[a, b]);
                                    int CB = Clip(Avg_B2 + Modulation_Table[a, b]);

                                    int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                    int Diff = Math.Abs(Luma - Test_Luma);
                                    if (Diff < Optimal_Diff)
                                        Optimal_Diff = Diff;
                                }
                                Min_Diff_2[a] += Optimal_Diff;
                            }
                        }
                    }

                    int Temp_2 = 255 * 8;
                    for (int a = 0; a <= 7; a++)
                    {
                        if (Min_Diff_2[a] < Temp_2)
                        {
                            Temp_2 = Min_Diff_2[a];
                            Mod_Table_2 = a;
                        }
                    }

                    Block_Top = Block_Top | (Mod_Table_1 << 29);
                    Block_Top = Block_Top | (Mod_Table_2 << 26);

                    // Seleciona o melhor valor da tabela que mais se aproxima com a cor original
                    for (int Y = 0; Y <= (Flip ? 1 : 3); Y++)
                    {
                        for (int X = 0; X <= (Flip ? 3 : 1); X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            int Col_Diff = 255;
                            int Pix_Table_Index = 0;
                            for (int b = 0; b <= 3; b++)
                            {
                                int CR = Clip(Avg_R1 + Modulation_Table[Mod_Table_1, b]);
                                int CG = Clip(Avg_G1 + Modulation_Table[Mod_Table_1, b]);
                                int CB = Clip(Avg_B1 + Modulation_Table[Mod_Table_1, b]);

                                int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                int Diff = Math.Abs(Luma - Test_Luma);
                                if (Diff < Col_Diff)
                                {
                                    Col_Diff = Diff;
                                    Pix_Table_Index = b;
                                }
                            }

                            int Index = X * 4 + Y;
                            if (Index < 8)
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index + 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 24));
                            }
                            else
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index - 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 8));
                            }
                        }
                    }

                    for (int Y = Flip ? 2 : 0; Y <= 3; Y++)
                    {
                        for (int X = Flip ? 0 : 2; X <= 3; X++)
                        {
                            int Image_Offset = ((Tile_X * 4) + X + (((Tile_Y * 4) + Y) * Width)) * 4;
                            int Luma = Convert.ToInt32(0.299F * Out[Image_Offset] + 0.587F * Out[Image_Offset + 1] + 0.114F * Out[Image_Offset + 2]);

                            int Col_Diff = 255;
                            int Pix_Table_Index = 0;
                            for (int b = 0; b <= 3; b++)
                            {
                                int CR = Clip(Avg_R2 + Modulation_Table[Mod_Table_2, b]);
                                int CG = Clip(Avg_G2 + Modulation_Table[Mod_Table_2, b]);
                                int CB = Clip(Avg_B2 + Modulation_Table[Mod_Table_2, b]);

                                int Test_Luma = Convert.ToInt32(0.299F * CR + 0.587F * CG + 0.114F * CB);
                                int Diff = Math.Abs(Luma - Test_Luma);
                                if (Diff < Col_Diff)
                                {
                                    Col_Diff = Diff;
                                    Pix_Table_Index = b;
                                }
                            }

                            int Index = X * 4 + Y;
                            if (Index < 8)
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index + 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 24));
                            }
                            else
                            {
                                Block_Bottom = Block_Bottom | (((Pix_Table_Index & 2) >> 1) << (Index - 8));
                                Block_Bottom = Block_Bottom | ((Pix_Table_Index & 1) << (Index + 8));
                            }
                        }
                    }

                    // Copia dados para a saída
                    byte[] Block = new byte[8];
                    Buffer.BlockCopy(BitConverter.GetBytes(Block_Top), 0, Block, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(Block_Bottom), 0, Block, 4, 4);
                    byte[] New_Block = new byte[8];
                    for (int j = 0; j <= 7; j++)
                        New_Block[7 - j] = Block[j];
                    if (hasAlpha)
                    {
                        byte[] Alphas = new byte[8];
                        int Alpha_Offset = 0;
                        for (int TX = 0; TX <= 3; TX++)
                        {
                            for (int TY = 0; TY <= 3; TY += 2)
                            {
                                int Img_Offset_1 = (Tile_X * 4 + TX + ((Tile_Y * 4 + TY) * Width)) * 4;
                                int Img_Offset_2 = (Tile_X * 4 + TX + ((Tile_Y * 4 + TY + 1) * Width)) * 4;

                                byte Alpha_1 = (byte)(Out[Img_Offset_1 + 3] >> 4);
                                byte Alpha_2 = (byte)(Out[Img_Offset_2 + 3] >> 4);

                                Alphas[Alpha_Offset] = (byte)(Alpha_1 | (Alpha_2 << 4));

                                Alpha_Offset += 1;
                            }
                        }

                        Buffer.BlockCopy(Alphas, 0, Out_Data, Out_Data_Offset, 8);
                        Buffer.BlockCopy(New_Block, 0, Out_Data, Out_Data_Offset + 8, 8);
                        Out_Data_Offset += 16;
                    }
                    else 
                    {
                        Buffer.BlockCopy(New_Block, 0, Out_Data, Out_Data_Offset, 8);
                        Out_Data_Offset += 8;
                    }
                }
            }

            return Out_Data;
        }

        private static int[] Get_ETC1_Scramble(int Width, int Height)
        {
            int[] Tile_Scramble = new int[((Width / 4) * (Height / 4)) - 1 + 1];
            int Base_Accumulator = 0;
            int Line_Accumulator = 0;
            int Base_Number = 0;
            int Line_Number = 0;

            for (int Tile = 0; Tile <= Tile_Scramble.Length - 1; Tile++)
            {
                if ((Tile % (Width / 4) == 0) & Tile > 0)
                {
                    if (Line_Accumulator < 1)
                    {
                        Line_Accumulator += 1;
                        Line_Number += 2;
                        Base_Number = Line_Number;
                    }
                    else
                    {
                        Line_Accumulator = 0;
                        Base_Number -= 2;
                        Line_Number = Base_Number;
                    }
                }

                Tile_Scramble[Tile] = Base_Number;

                if (Base_Accumulator < 1)
                {
                    Base_Accumulator += 1;
                    Base_Number += 1;
                }
                else
                {
                    Base_Accumulator = 0;
                    Base_Number += 3;
                }
            }

            return Tile_Scramble;
        }

        private static sbyte Signed_Byte(byte Byte_To_Convert)
        {
            if ((Byte_To_Convert < 0x80))
                return Convert.ToSByte(Byte_To_Convert);
            return Convert.ToSByte(Byte_To_Convert - 0x100);
        }

        private static byte Clip(int Value)
        {
            if (Value > 0xFF)
                return 0xFF;
            else if (Value < 0)
                return 0;
            else
                return Convert.ToByte(Value & 0xFF);
        }

        private static int[,] Modulation_Table = new[,] {
            { 2, 8, -2, -8 },
            { 5, 17, -5, -17 },
            { 9, 29, -9, -29 },
            { 13, 42, -13, -42 },
            { 18, 60, -18, -60 },
            { 24, 80, -24, -80 },
            { 33, 106, -33, -106 },
            { 47, 183, -47, -183 }
        };

        #endregion
    }
}
