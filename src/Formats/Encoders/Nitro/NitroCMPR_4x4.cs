using ImageLibrary.Formats.Encoders.Gcn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders.Nitro
{
    public class NitroCMPR_4x4
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TxTileData
        {
            public Color32[] Rgb;           // 16 pixels
            public Color32[] Palette32;     // 4 colors
            public ushort Mode;             // mode + palette index
            public ushort InitMode;
            public ushort PaletteIndex;
            public bool Used;
            public bool Duplicate;
            public int NTransparent;
            public uint Texel;
        }

        public static (byte[] texData, byte[] palette, byte[] paletteIdx) Encode(
            byte[] rgbaData, int width, int height, float threshold = 50f)
        {
            if (width % 4 != 0 || height % 4 != 0)
                throw new ArgumentException("CMPR_4x4 requires dimensions divisible by 4.");

            int tilesX = width / 4;
            int tilesY = height / 4;
            int nTiles = tilesX * tilesY;

            var tiles = new TxTileData[nTiles];
            int tileIndex = 0;

            byte[] paletteData = new byte[0];

            // TODO

            // Pack output
            byte[] texData = new byte[nTiles * 4];
            byte[] pidxData = new byte[nTiles * 2];
            return (texData, paletteData, pidxData);
        }
    }
}
