using BCnEncoder.Shared;
using ImageLibrary.Formats.Encoders.Ctr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.PlatformSwizzle.Algorithms.Ctr
{
    public static class Swizzle3DS
    {
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


        static byte[] DecodeTile(int width, int height, int bpp, Action<byte[], int> decodePixels)
        {
            byte[] pixels = new byte[width * height * bpp];
            int morton7(int n)
            {
                // 0a0b0c => 000abc
                return ((n >>> 2) & 0x04) | ((n >>> 1) & 0x02) | (n & 0x01);
            }

            for (int TY = 0; TY < height; TY += 8)
            {
                for (int TX = 0; TX < width; TX += 8)
                {
                    for (int Px = 0; Px < 64; Px++)
                    {
                        var X = morton7(Px);
                        var Y = morton7(Px >>> 1);
                        var OOffs = ((TY + Y) * width + TX + X) * bpp;
                        decodePixels?.Invoke(pixels, OOffs);
                    }
                }
            }
            return pixels;
        }


        internal static int gcm(int n, int m)
        {
            return ((n + m - 1) / m) * m;
        }

        internal static int nlpo2(int x)
        {
            x--; // comment out to always take the next biggest power of two, even if x is already a power of two
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return (x + 1);
        }

        //Direct method to only deswizzle 3DS content
        public static byte[] Deswizzle(byte[] Input, int Width, int Height,
            IImageFormat Format, PICASwizzleTransformation mode)
        {
            var bitsPerPixel = Format.GetBitsPerPixel();

            if (Format.GetEncoder() is Etc1)
            {
                return Input; //For now we use 3ds specific ETC1 decoder/encoder
            }
            else
            {
                return Input; //For now we use 3ds specific ETC1 decoder/encoder

                int bp = Math.Max((int)Format.GetBytesPerPixel(), 1);


                var output = new byte[Width * Height * bp];

                int w = nlpo2(gcm((int)Width, 8));
                int h = nlpo2(gcm((int)Height, 8));

                int src = 0;
                var decoded = DecodeTile(w, h, bp, (pixels, dst) =>
                {
                    for (int i = 0; i < bp; i++)
                        pixels[src + i] = Input[dst + i];
                    src += bp;
                });

                for (int y = 0; y < Height; y++)
                {
                    int srcOffset = (y * w) * bp;
                    int dstOffset = (y * (int)Width) * bp;
                    Buffer.BlockCopy(decoded, srcOffset, output, dstOffset, (int)Width * bp);
                }

                return output; //For now we use 3ds specific ETC1 decoder/encoder

                int BPP = Math.Max((int)Format.GetBytesPerPixel(), 1);
                byte[] Output = new byte[Width * Height * 4];

                int IOffs = 0;
                for (int TY = 0; TY < Height; TY += 8)
                {
                    for (int TX = 0; TX < Width; TX += 8)
                    {
                        for (int Px = 0; Px < 64; Px++)
                        {
                            int X = SwizzleLUT[Px] & 7;
                            int Y = SwizzleLUT[Px] - X >> 3;

                            int OOffs = (TX + X + (TY + Y) * Width) * BPP;

                            switch (mode)
                            {
                                case PICASwizzleTransformation.Transposed:
                                    //Swap X/Y
                                    int transposedX = TY + Y;
                                    int transposedY = TX + X;
                                    OOffs = (transposedX + transposedY * Height) * BPP;
                                    break;
                                case PICASwizzleTransformation.FlipY:
                                    //Flip Y
                                    int flippedY = Height - 1 - (TY + Y);
                                    OOffs = (TX + X + flippedY * Width) * BPP;
                                    break;
                                case PICASwizzleTransformation.Rotate90:
                                    //Rotate 90
                                    int rotatedX = TY + Y;
                                    int rotatedY = Width - 1 - (TX + X);
                                    OOffs = (rotatedY + rotatedX * Width) * BPP;
                                    break;
                            }


                            if (bitsPerPixel == 4)
                            {
                                int byteIndex = IOffs / 2;
                                bool isHighNibble = (IOffs % 2) == 0;

                                byte b = Input[byteIndex];
                                byte pixelValue = isHighNibble ? (byte)(b >> 4) : (byte)(b & 0x0F);

                                if ((OOffs % 2) == 0)
                                    Output[OOffs / 2] = (byte)(pixelValue << 4);
                                else
                                    Output[OOffs / 2] |= pixelValue;
                            }
                            else
                            {
                                //order swap ABGR
                                for (int i = 0; i < BPP; i++)
                                    Output[OOffs + i] = Input[IOffs + i];
                            }
                            IOffs += BPP;
                        }
                    }
                }

                return Output;
            }
        }

        public static byte[] Swizzle(byte[] Input, int Width, int Height,
            IImageFormat Format, PICASwizzleTransformation mode)
        {
            if (Format.GetEncoder() is Etc1)
                return Input;

            var bitsPerPixel = Format.GetBitsPerPixel();
            int BPP = Math.Max((int)bitsPerPixel / 8, 1);
            byte[] Output = new byte[CalculateLength(Width, Height, Format)];

            Console.WriteLine(Format + " BPP " + BPP);
            Console.WriteLine(Format + " bitsPerPixel " + bitsPerPixel);

            int OOffs = 0;

            for (int TY = 0; TY < Height; TY += 8)
            {
                for (int TX = 0; TX < Width; TX += 8)
                {
                    for (int Px = 0; Px < 64; Px++)
                    {
                        int X = SwizzleLUT[Px] & 7;
                        int Y = SwizzleLUT[Px] - X >> 3;

                        int IOffs = (TX + X + (TY + Y) * Width) * BPP;

                        switch (mode)
                        {
                            case PICASwizzleTransformation.Transposed:
                                //Swap X/Y
                                int transposedX = TY + Y;
                                int transposedY = TX + X;
                                IOffs = (transposedX + transposedY * Height) * BPP;
                                break;
                            case PICASwizzleTransformation.FlipY:
                                //Flip Y
                                int flippedY = Height - 1 - (TY + Y);
                                IOffs = (TX + X + flippedY * Width) * BPP;
                                break;
                            case PICASwizzleTransformation.Rotate90:
                                //Rotate 90
                                int rotatedX = TY + Y;
                                int rotatedY = Width - 1 - (TX + X);
                                IOffs = (rotatedX + rotatedY * Height) * BPP;
                                break;
                        }


                        if (bitsPerPixel == 4)
                        {
                            int byteIndex = IOffs / 2;
                            bool isHighNibble = (IOffs % 2) == 0;

                            byte b = Input[byteIndex];
                            byte pixelValue = isHighNibble ? (byte)(b >> 4) : (byte)(b & 0x0F);

                            if ((OOffs % 2) == 0)
                                Output[OOffs / 2] = (byte)(pixelValue << 4);
                            else
                                Output[OOffs / 2] |= pixelValue;
                        }
                        else
                        {
                            //order swap ABGR
                            for (int i = 0; i < BPP; i++)
                                Output[OOffs + i] = Input[IOffs + i];

                            // for (int i = 0; i < BPP; i++)
                            //    Output[OOffs + i] = Input[IOffs + i];
                        }
                        OOffs += BPP;
                    }
                }
            }

            return Output;
        }

        public static int CalculateLength(int Width, int Height, IImageFormat format)
        {
            int Length = Width * Height * (int)format.GetBitsPerPixel() / 8;
            if ((Length & 0x7f) != 0)
                Length = (Length & ~0x7f) + 0x80;

            return Length;
        }
    }

    public enum PICASwizzleTransformation : byte
    {
        None,
        FlipY = 2,
        Rotate90 = 4,
        Transposed = 8,
    }
}
