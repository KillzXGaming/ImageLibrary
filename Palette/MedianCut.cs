using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;

interface IProgressTracker
{

}

// From https://github.com/soopercool101/BrawlCrate/blob/8fd21ef7ea4c10073fea83b9236517b6c3cb45c3/BrawlLib/Imaging/MedianCut.cs#L10
internal class MedianCut : IDisposable
{
    public enum PaletteFormat
    {
        RGB565,
        RGB5A3,
        IA8,
    }

    private Image<Rgba32> _srcImage;
    private readonly int _width, _height;

    private ColorBox[] _boxes = new ColorBox[256];
    private int _boxCount;

    private ColorEntry?[] _groupTable = new ColorEntry[65536];

    private readonly Func<Rgba32, ushort> _idFunc;
    private readonly Func<ushort, Rgba32> _idConv;

    private const int R_SCALE = 13;
    private const int G_SCALE = 24;
    private const int B_SCALE = 26;
    private const int A_SCALE = 28;

    #region Handlers/Converters

    private static ushort ToIA8(Rgba32 p)
    {
        byte i = (byte)((p.R * 77 + p.G * 150 + p.B * 29) >> 8);
        return (ushort)((p.A << 8) | i);
    }

    private static Rgba32 FromIA8(ushort id)
    {
        byte i = (byte)(id & 0xFF);
        byte a = (byte)(id >> 8);
        return new Rgba32(i, i, i, a);
    }

    private static ushort ToRGB565(Rgba32 p)
    {
        return (ushort)(((p.R >> 3) << 11) | ((p.G >> 2) << 5) | (p.B >> 3));
    }

    private static Rgba32 FromRGB565(ushort id)
    {
        byte r = (byte)(((id >> 11) & 0x1F) * 255 / 31);
        byte g = (byte)(((id >> 5) & 0x3F) * 255 / 63);
        byte b = (byte)((id & 0x1F) * 255 / 31);
        return new Rgba32(r, g, b, 255);
    }

    private static ushort ToRGB5A3(Rgba32 p)
    {
        if (p.A >= 224)
        {
            return (ushort)(0x8000 | ((p.R >> 3) << 10) | ((p.G >> 3) << 5) | (p.B >> 3));
        }
        else
        {
            return (ushort)(((p.A >> 5) << 12) | ((p.R >> 4) << 8) | ((p.G >> 4) << 4) | (p.B >> 4));
        }
    }

    private static Rgba32 FromRGB5A3(ushort id)
    {
        if ((id & 0x8000) != 0)
        {
            byte r = (byte)(((id >> 10) & 0x1F) * 255 / 31);
            byte g = (byte)(((id >> 5) & 0x1F) * 255 / 31);
            byte b = (byte)((id & 0x1F) * 255 / 31);
            return new Rgba32(r, g, b, 255);
        }
        else
        {
            byte a = (byte)(((id >> 12) & 0x07) * 255 / 7);
            byte r = (byte)(((id >> 8) & 0x0F) * 255 / 15);
            byte g = (byte)(((id >> 4) & 0x0F) * 255 / 15);
            byte b = (byte)((id & 0x0F) * 255 / 15);
            return new Rgba32(r, g, b, a);
        }
    }

    #endregion

    private MedianCut(Image<Rgba32> image, PaletteFormat palFormat)
    {
        if (palFormat == PaletteFormat.IA8)
        {
            _idFunc = ToIA8;
            _idConv = FromIA8;
        }
        else if (palFormat == PaletteFormat.RGB565)
        {
            _idFunc = ToRGB565;
            _idConv = FromRGB565;
        }
        else
        {
            _idFunc = ToRGB5A3;
            _idConv = FromRGB5A3;
        }

        _srcImage = image;
        _width = image.Width;
        _height = image.Height;

        for (int i = 0; i < 256; i++)
            _boxes[i] = new ColorBox();
    }

    public void Dispose()
    {
        _srcImage?.Dispose();
        _srcImage = null;
        GC.SuppressFinalize(this);
    }

    private bool SelectColors(int targetColors)
    {
        ColorBox initialBox = _boxes[0];
        initialBox.Initialize();
        _boxCount = 1;

        Array.Fill(_groupTable, null);

        _srcImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < _height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                for (int x = 0; x < _width; x++)
                {
                    Rgba32 pixel = row[x];
                    ushort id = _idFunc(pixel);
                    ref ColorEntry? entryRef = ref _groupTable[id];
                    if (entryRef == null)
                    {
                        entryRef = new ColorEntry
                        {
                            Color = _idConv(id),
                            Weight = 1,
                            Box = initialBox
                        };
                        initialBox.Entries.Add(entryRef);
                    }
                    else
                    {
                        entryRef.Weight++;
                    }
                }
            }

        });


        if (initialBox.Colors <= targetColors)
        {
            return false;
        }

        initialBox.Update(targetColors - 1);

        while (_boxCount < targetColors)
        {
            int splitAxis;
            ColorBox splitBox = ColorBox.FindSplit(_boxes, _boxCount, targetColors, out splitAxis);

            ColorBox newBox = _boxes[_boxCount];
            newBox.Initialize();

            splitBox.Split(newBox, splitAxis);

            _boxCount++;
            splitBox.Update(targetColors - _boxCount);
            newBox.Update(targetColors - _boxCount);
        }

        return true;
    }

    private void SortBoxes()
    {
        List<ColorBox> colorList = new List<ColorBox>();
        for (int i = 0; i < _boxCount; i++)
        {
            _boxes[i].Luminance = GetLuminance(_boxes[i].Color);
            colorList.Add(_boxes[i]);
        }

        colorList.Sort((a, b) => a.Luminance.CompareTo(b.Luminance));

        for (int i = 0; i < _boxCount; i++)
        {
            ColorBox box = colorList[i];
            box.Index = i;
            ushort id = _idFunc(box.Color);
            box.Color = _idConv(id);
        }
    }

    private static float GetLuminance(Rgba32 c) => c.R * 0.299f + c.G * 0.587f + c.B * 0.114f;

    private void ClearBoxes()
    {
        for (int i = 0; i < _boxCount; i++)
        {
            _boxes[i].Destroy();
        }
    }

    private void SpreadColors(int total)
    {
        ColorBox initialBox = _boxes[0];
        List<ColorEntry> entries = initialBox.Entries;
        int count = entries.Count;

        for (int index = 0; index < count; index++)
        {
            ColorEntry entry = entries[index];
            ColorBox box = _boxes[index];
            box.Color = entry.Color;
            entry.Box = box;
        }

        _boxCount = count;
    }

    private Image<Rgba32> Quantize(int targetColors, IProgressTracker progress)
    {
        Array.Fill(_groupTable, null);

        if (!SelectColors(targetColors))
            SpreadColors(targetColors);

        SortBoxes();

        Rgba32[] pal = new Rgba32[_boxCount];
        for (int i = 0; i < _boxCount; i++)
        {
            pal[_boxes[i].Index] = _boxes[i].Color;
        }

        Rgba32[][] srcPixels = new Rgba32[_height][];
        _srcImage.ProcessPixelRows(srcAccessor =>
        {
            for (int y = 0; y < _height; y++)
            {
                Span<Rgba32> srcRow = srcAccessor.GetRowSpan(y);
                srcPixels[y] = srcRow.ToArray(); // copy each row
            }
        });
        Image<Rgba32> bmp = new Image<Rgba32>(_width, _height);
        bmp.ProcessPixelRows(destAccessor =>
        {
            for (int y = 0; y < _height; y++)
            {
                Span<Rgba32> destRow = destAccessor.GetRowSpan(y);
                Span<Rgba32> srcRow = srcPixels[y]; 
                for (int x = 0; x < _width; x++)
                {
                    ushort id = _idFunc(srcRow[x]);
                    byte index = (byte)_groupTable[id]!.Box.Index;
                    destRow[x] = pal[index];
                }
            }
        });

        ClearBoxes();

        return bmp;
    }

    public static Image<Rgba32> Quantize(Image<Rgba32> bmp, int colors, PaletteFormat palFormat,
                                          IProgressTracker progress)
    {
        using (MedianCut mc = new MedianCut(bmp, palFormat))
        {
            return mc.Quantize(colors, progress);
        }
    }

    private class ColorEntry
    {
        public Rgba32 Color;
        public uint Weight;
        public ColorBox Box = null!;
    }

    private class ColorBox
    {
        public byte[] ComponentMin = new byte[4];
        public byte[] ComponentMax = new byte[4];
        public byte[] HalfError = new byte[4];
        public uint Volume;
        public ulong[] Error = new ulong[4];

        public Rgba32 Color;
        public float Luminance;

        public List<ColorEntry> Entries = new List<ColorEntry>();
        public uint Colors => (uint)Entries.Count;
        public uint Weight;
        public int Index;

        public void Initialize()
        {
            Entries.Clear();
        }

        public void Destroy()
        {
            Entries.Clear();
        }

        public void Split(ColorBox newBox, int axis)
        {
            byte limit = HalfError[axis];

            List<ColorEntry> toMove = new List<ColorEntry>();
            foreach (ColorEntry current in Entries)
            {
                byte val = GetComponent(current.Color, axis);
                if (val > limit)
                {
                    toMove.Add(current);
                }
            }

            foreach (ColorEntry m in toMove)
            {
                Entries.Remove(m);
                newBox.Entries.Add(m);
                m.Box = newBox;
            }
        }

        private static byte GetComponent(Rgba32 c, int axis)
        {
            return axis switch
            {
                0 => c.B,
                1 => c.G,
                2 => c.R,
                3 => c.A,
                _ => 0
            };
        }

        public void Update(int remaining)
        {
            Weight = 0;
            ulong[] sum = new ulong[4];
            int[] size = new int[4];

            for (int i = 0; i < 4; i++)
            {
                ComponentMin[i] = 255;
                ComponentMax[i] = 0;
                sum[i] = 0;
            }

            foreach (ColorEntry current in Entries)
            {
                uint weight = current.Weight;
                Weight += weight;

                byte b = current.Color.B;
                byte g = current.Color.G;
                byte r = current.Color.R;
                byte a = current.Color.A;

                ComponentMin[0] = Math.Min(ComponentMin[0], b);
                ComponentMax[0] = Math.Max(ComponentMax[0], b);
                sum[0] += (ulong)b * weight;

                ComponentMin[1] = Math.Min(ComponentMin[1], g);
                ComponentMax[1] = Math.Max(ComponentMax[1], g);
                sum[1] += (ulong)g * weight;

                ComponentMin[2] = Math.Min(ComponentMin[2], r);
                ComponentMax[2] = Math.Max(ComponentMax[2], r);
                sum[2] += (ulong)r * weight;

                ComponentMin[3] = Math.Min(ComponentMin[3], a);
                ComponentMax[3] = Math.Max(ComponentMax[3], a);
                sum[3] += (ulong)a * weight;
            }

            byte avgB = (byte)(sum[0] / Weight);
            byte avgG = (byte)(sum[1] / Weight);
            byte avgR = (byte)(sum[2] / Weight);
            byte avgA = (byte)(sum[3] / Weight);
            Color = new Rgba32(avgR, avgG, avgB, avgA);

            Volume = 1;
            for (int i = 0; i < 4; i++)
            {
                int diff = ComponentMax[i] - ComponentMin[i] + 1;
                Volume *= (uint)diff;
                size[i] = diff;
            }

            if (Volume == 0)
            {
                Volume = 0xFFFFFFFF;
            }

            for (int i = 0; i < 4; i++)
            {
                Error[i] = 0;
            }

            foreach (ColorEntry current in Entries)
            {
                uint weight = current.Weight;
                int diffB = current.Color.B - avgB;
                Error[0] += (ulong)(weight * diffB * diffB);
                int diffG = current.Color.G - avgG;
                Error[1] += (ulong)(weight * diffG * diffG);
                int diffR = current.Color.R - avgR;
                Error[2] += (ulong)(weight * diffR * diffR);
                int diffA = current.Color.A - avgA;
                Error[3] += (ulong)(weight * diffA * diffA);
            }

            for (int i = 0; i < 4; i++)
            {
                HalfError[i] = (byte)(ComponentMin[i] + size[i] / 2);
            }

            if (Volume > 1)
            {
                int axis1 = -1, axis2 = -1;
                int len1 = 0, len2 = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (size[i] > len1)
                    {
                        len2 = len1;
                        axis2 = axis1;
                        len1 = size[i];
                        axis1 = i;
                    }
                    else if (size[i] > len2)
                    {
                        len2 = size[i];
                        axis2 = i;
                    }
                }

                if (len2 == 0)
                {
                    len2 = 1;
                }

                int ratio = (len1 + len2 / 2) / len2;

                if (ratio > remaining + 1)
                {
                    ratio = remaining + 1;
                }

                if (ratio > 2 && axis1 >= 0)
                {
                    int diff = ComponentMin[axis1] + (ComponentMax[axis1] - ComponentMin[axis1]) + ratio / 2;
                    if (diff < ComponentMax[axis1])
                    {
                        HalfError[axis1] = (byte)diff;
                    }
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (HalfError[i] == ComponentMax[i])
                {
                    HalfError[i] = ComponentMin[i];
                }
            }
        }

        public static ColorBox FindSplit(ColorBox[] boxes, int boxCount, int maxColors, out int axis)
        {
            ColorBox outBox = null!;
            double lBias = 1.0, maxC = 0.0;

            if (maxColors <= 16 && boxCount <= 2)
            {
                lBias = (3.0 - boxCount) / (2.0 / 2.66);
            }

            axis = -1;

            for (int i = 0; i < boxCount; i++)
            {
                ColorBox box = boxes[i];
                if (box.Volume <= 1)
                {
                    continue;
                }

                double rpe = box.Error[2] * R_SCALE * R_SCALE;
                double gpe = box.Error[1] * G_SCALE * G_SCALE;
                double bpe = box.Error[0] * B_SCALE * B_SCALE;
                double ape = box.Error[3] * A_SCALE * A_SCALE;

                if (lBias * rpe > maxC && box.ComponentMin[2] < box.ComponentMax[2])
                {
                    outBox = box;
                    maxC = lBias * rpe;
                    axis = 2;
                }

                if (gpe > maxC && box.ComponentMin[1] < box.ComponentMax[1])
                {
                    outBox = box;
                    maxC = gpe;
                    axis = 1;
                }

                if (bpe > maxC && box.ComponentMin[0] < box.ComponentMax[0])
                {
                    outBox = box;
                    maxC = bpe;
                    axis = 0;
                }

                if (ape > maxC && box.ComponentMin[3] < box.ComponentMax[3])
                {
                    outBox = box;
                    maxC = ape;
                    axis = 3;
                }
            }

            return outBox;
        }
    }
}