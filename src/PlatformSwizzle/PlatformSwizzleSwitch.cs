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
        public long[] MipOffsets;

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

        public byte[] DeswizzleAllSurfaces(IImageFormat format, byte[] imageData, uint width, uint height,
            uint mipCount = 1, uint arrayCount = 1, uint depth = 1)
        {
            var tileMode = TileMode;

            uint bpp = format.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(format);
            uint bh = ImageFormat.GetBlockHeight(format);
            uint bd = ImageFormat.GetBlockDepth(format);

            var blk_sizes = (bw, bh, bd);

            return TextureConverter.Deswizzle(width, height, depth, arrayCount,
                 mipCount, blk_sizes, bpp, tileMode, imageData, (int)Target, IsOrin);
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

            return TextureConverter.DeswizzleSurface(texture.Width, texture.Height, texture.Depth, texture.ArrayCount,
                 texture.MipCount, blk_sizes, bpp, (uint)arrayLevel, (uint)mipLevel, image_data.ToArray(), (int)Target, IsOrin);
        }

        public override byte[] Swizzle(GenericTextureBase texture, byte[] imageData, int arrayLevel, int mipLevel)
        {
            return base.Swizzle(texture, imageData, mipLevel, arrayLevel);
        }

        public override byte[] SwizzleAllSurfaces(GenericTextureBase texture, byte[] imageData)
        {
            var tileMode = TileMode;

            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);

            Console.WriteLine($"{texture.ImageFormat} {bpp}");

            var blk_sizes = (bw, bh, bd);

            return TextureConverter.Swizzle(texture.Width, texture.Height, texture.Depth, texture.ArrayCount,
                 texture.MipCount, blk_sizes, bpp, tileMode, imageData, (int)Target, IsOrin);
        }

        public byte[] SwizzleAllSurfaces(IImageFormat format, byte[] imageData, uint width, uint height, 
            uint mipCount = 1, uint arrayCount = 1, uint depth = 1)
        {
            var tileMode = TileMode;

            uint bpp = format.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(format);
            uint bh = ImageFormat.GetBlockHeight(format);
            uint bd = ImageFormat.GetBlockDepth(format);

            Console.WriteLine($"{format} {bpp}");

            var blk_sizes = (bw, bh, bd);

            return TextureConverter.Swizzle(width, height, depth, arrayCount,
                 mipCount, blk_sizes, bpp, tileMode, imageData, (int)Target, IsOrin);
        }

        // Computes mipmap offsets
        public List<long> GetMipOffsets(GenericTextureBase texture)
        {
            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);

            var blk_sizes = (bw, bh, bd);

            List<long> mip_offsets = new List<long>();

            uint offset = 0;
            for (int mip = 0; mip < texture.MipCount; mip++)
            {
                var size = TextureConverter.GetSwizzleSurfaceSizeMip(mip, texture.Width,
                    texture.Height, texture.Depth, blk_sizes, bpp);

                mip_offsets.Add(offset);

                offset += size;
            }

            return mip_offsets;
        }

        // Computes mipmap sizes
        public List<long> GetMipSizes(GenericTextureBase texture)
        {
            uint bpp = texture.ImageFormat.GetBytesPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);

            var blk_sizes = (bw, bh, bd);

            List<long> mip_sizes = new List<long>();
            for (int mip = 0; mip < texture.MipCount; mip++)
            {
                var size = TextureConverter.GetSwizzleSurfaceSizeMip(mip, texture.Width,
                    texture.Height, texture.Depth, blk_sizes, bpp);

                mip_sizes.Add(size);
            }
            return mip_sizes;
        }

        // Creates a full texture list of both surfaces and mipmaps
        public List<List<byte[]>> CreateSurfaceList(GenericTextureBase texture)
        {
            List<List<byte[]>> surfaces = new List<List<byte[]>>();

            uint bpp = texture.ImageFormat.GetBitsPerPixel();
            uint bw = ImageFormat.GetBlockWidth(texture.ImageFormat);
            uint bh = ImageFormat.GetBlockHeight(texture.ImageFormat);
            uint bd = ImageFormat.GetBlockDepth(texture.ImageFormat);

            var block_height_mip0 = TextureConverter.GetBlockHeight(TextureConverter.DIV_ROUND_UP(texture.Height, bh));

            uint offset = 0;
            for (int a = 0; a < texture.ArrayCount; a++)
            {
                List<byte[]> mipmaps = new List<byte[]>();

                for (int mip = 0; mip < texture.MipCount; mip++)
                {
                    var mip_width = Math.Max(TextureConverter.DIV_ROUND_UP(texture.Width >> mip, bw), 1);
                    var mip_height = Math.Max(TextureConverter.DIV_ROUND_UP(texture.Height >> mip, bh), 1);
                    var mip_depth = Math.Max(TextureConverter.DIV_ROUND_UP(texture.Depth >> mip, bd), 1);

                    var mip_block_height = tegra_swizzle_native_x64.MipBlockHeight(mip_height, block_height_mip0);

                    var size = tegra_swizzle_native_x64.SwizzleSurfaceMipSize(
                        mip_width, mip_height, mip_depth, mip_block_height, bpp);

                    if (texture.Data.Length < (long)(offset + size))
                        throw new Exception($"Invalid data length! {texture.Data.Length} for mip {mip} {mip_width}x{mip_height}");

                    mipmaps.Add(texture.Data.Slice((int)offset, (int)size).ToArray());

                    offset += (uint)size;
                }
                mipmaps[0] = ByteUtil.CombineByteArray(mipmaps.ToArray());
                surfaces.Add(mipmaps);

                //align between layers
                offset = TextureConverter.AlignLayerSize(offset, texture.Height, texture.Depth, block_height_mip0, 1);
            }

            return surfaces;
        }
    }
}
