using CommunityToolkit.HighPerformance;
using ImageLibrary.PlatformSwizzle.Algorithms.Switch;
using ImageLibrary.Utils;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.PlatformSwizzle
{
    /// <summary>
    /// A swizzle platform base for the Switch tegra X1 format.
    /// </summary>
    public class PlatformSwizzleSwitch : PlatformSwizzleBase
    {
        //Required settings
        public uint BlockHeightLog2;
        public uint Alignment = 512;
        public uint TileMode;
        public uint Target = 1; //Platform PC (0) or NX (1)

        //Adjusted on encode
        public uint ReadTextureLayout;
        public uint ImageSize;

        public long[] MipOffsets = new long[0];
        public long[] MipSizes = new long[0];

        //Quick check for linear tiling
        public bool LinearMode => TileMode == 1;
        public bool IsOrin = false;

        public override byte[] DeswizzleAllSurfaces(GenericTextureBase texture)
        {
            var image_data = texture.Data;
            var tileMode = TileMode;

            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);

            var blk_sizes = (bw, bh, bd);
            return TextureConverter.Deswizzle(texture.Width, texture.Height, texture.Depth, texture.ArrayCount,
                 texture.MipCount, blk_sizes, bpp, tileMode, image_data.ToArray(), (int)Target, IsOrin);
        }

        public override byte[] Deswizzle(GenericTextureBase texture, int arrayLevel, int mipLevel)
        {
            var image_data = texture.Data;
            var tileMode = TileMode;

            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);

            var blk_sizes = (bw, bh, bd);

            return TextureConverter.DeswizzleSurfaceSlice(texture.Width, texture.Height, texture.Depth, texture.ArrayCount,
                 texture.MipCount, blk_sizes, bpp, (uint)arrayLevel, (uint)mipLevel, image_data.ToArray(), (int)Target, IsOrin);
        }

        public override byte[] SwizzleSlices(GenericTextureBase texture,
            List<GenericTextureBase.ImportSlice> surfaces)
        {
            var tileMode = TileMode;

            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);
            // Inject a single swizzled slice
            var blk_sizes = (bw, bh, bd);
            TextureConverter.SwizzleSliceSurfaces(surfaces, texture.Data, texture.Width, texture.Height, texture.Depth, texture.ArrayCount,
                 texture.MipCount, blk_sizes, bpp);
            return texture.Data.ToArray();
        }

        public override byte[] SwizzleAllSurfaces(GenericTextureBase texture, byte[] imageData)
        {
            // Compute mip meta data for offset/size info which may get used for file formats
            CalculateMipInfo(texture);

            var tileMode = TileMode;
            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);
            var blk_sizes = (bw, bh, bd);

            return TextureConverter.Swizzle(texture.Width, texture.Height, texture.Depth, texture.ArrayCount,
                 texture.MipCount, blk_sizes, bpp, tileMode, imageData, (int)Target, IsOrin);
        }


        // Computes mipmap offsets/sizes
        private void CalculateMipInfo(GenericTextureBase texture)
        {
            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);
            var blk_sizes = (bw, bh, bd);

            List<long> mip_offsets = new List<long>();
            List<long> mip_sizes = new List<long>();

            uint offset = 0;
            for (int mip = 0; mip < texture.MipCount; mip++)
            {
                var size = TextureConverter.GetSwizzleSurfaceSizeMip(mip, texture.Width,
                    texture.Height, texture.Depth, blk_sizes, bpp);

                mip_sizes.Add(size);
                mip_offsets.Add(offset);
                offset += size;
            }

            this.MipOffsets = mip_offsets.ToArray();
            this.MipSizes = mip_sizes.ToArray();
        }
    }
}
