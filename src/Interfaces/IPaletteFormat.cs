using ImageLibrary.Pixels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Interfaces
{
    public interface IPaletteFormat
    {
        bool IsFormatPalette { get; }
        int MaxPaletteColorCount { get; }
        void ClearPalette();
        RgbaColor[] GetPaletteColors();
        void SetPaletteColor(RgbaColor color, int index);
    }
}
