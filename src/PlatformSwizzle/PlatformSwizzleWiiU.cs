using ImageLibrary.Formats.Encoders;
using ImageLibrary.Interfaces;
using ImageLibrary.WiiU;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ImageLibrary.Formats.Encoders.ImageDds;

namespace ImageLibrary.PlatformSwizzle
{
    /// <summary>
    /// A swizzle platform base for the Wii U GX2 format.
    /// </summary>
    public class PlatformSwizzleWiiU : PlatformSwizzleBase
    {
        public GX2.GX2AAMode AAMode { get; set; }
        public GX2.GX2TileMode TileMode { get; set; }
        public GX2.GX2RResourceFlags ResourceFlags { get; set; }
        public GX2.GX2SurfaceDimension SurfaceDimension { get; set; } = GX2.GX2SurfaceDimension.DIM_2D;
        public GX2.GX2SurfaceUse SurfaceUse { get; set; }
        public GX2.GX2SurfaceFormat GX2Format { get; set; }
        public uint Pitch { get; set; }
        public uint Alignment { get; set; }
        public uint SwizzleValue { get; set; }
        public uint[] Regs { get; set; } = new uint[4];
        public uint[] MipOffsets { get; set; }
        public byte[] MipData { get; set; }

        public PlatformSwizzleWiiU(ImageEncoder imageFormat)
        {
            UpdateFormat(imageFormat);
        }
        public PlatformSwizzleWiiU(GX2.GX2SurfaceFormat imageFormat)
        {
            GX2Format = imageFormat;
        }
        public PlatformSwizzleWiiU(IImageFormat imageFormat)
        {
            UpdateFormat(imageFormat);

            AAMode = GX2.GX2AAMode.GX2_AA_MODE_1X;
            TileMode = GX2.GX2TileMode.MODE_2D_TILED_THIN1;
            ResourceFlags = GX2.GX2RResourceFlags.GX2R_BIND_TEXTURE;
            SurfaceDimension = GX2.GX2SurfaceDimension.DIM_2D;
            SurfaceUse = GX2.GX2SurfaceUse.USE_TEXTURE;
            Alignment = 0;
            Pitch = 0;
        }

        public void UpdateFormat(ImageEncoder imageFormat)
        {
            if (imageFormat is Bcn bcn)
            {
                switch (bcn.Format)
                {
                    case BcnFormats.BC1: this.GX2Format = GX2.GX2SurfaceFormat.T_BC1_UNORM; break;
                    case BcnFormats.BC2: this.GX2Format = GX2.GX2SurfaceFormat.T_BC2_UNORM; break;
                    case BcnFormats.BC3: this.GX2Format = GX2.GX2SurfaceFormat.T_BC3_UNORM; break;
                    case BcnFormats.BC4: this.GX2Format = GX2.GX2SurfaceFormat.T_BC4_UNORM; break;
                    case BcnFormats.BC4S: this.GX2Format = GX2.GX2SurfaceFormat.T_BC4_UNORM; break;
                    case BcnFormats.BC5: this.GX2Format = GX2.GX2SurfaceFormat.T_BC5_SNORM; break;
                    case BcnFormats.BC5S: this.GX2Format = GX2.GX2SurfaceFormat.T_BC5_SNORM; break;
                    default:
                        throw new Exception($"Unsupported format {bcn.Format}");
                }
            }
            else if (imageFormat is Rgba rgba)
            {
                switch ((rgba.R, rgba.G, rgba.B, rgba.A))
                {
                    case (8, 8, 8, 8): this.GX2Format = GX2.GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNORM; break;
                    case (5, 6, 5, 0): this.GX2Format = GX2.GX2SurfaceFormat.TCS_R5_G6_B5_UNORM; break;
                    case (5, 5, 5, 1): this.GX2Format = GX2.GX2SurfaceFormat.TC_R5_G5_B5_A1_UNORM; break;
                    case (4, 4, 4, 4): this.GX2Format = GX2.GX2SurfaceFormat.TC_R4_G4_B4_A4_UNORM; break;
                    case (4, 4, 0, 0): this.GX2Format = GX2.GX2SurfaceFormat.T_R4_G4_UNORM; break;
                    case (8, 8, 0, 0): this.GX2Format = GX2.GX2SurfaceFormat.TC_R8_G8_SNORM; break;
                    case (8, 0, 0, 0): this.GX2Format = GX2.GX2SurfaceFormat.TC_R8_SNORM; break;
                    case (4, 0, 0, 0): this.GX2Format = GX2.GX2SurfaceFormat.T_R4_G4_UNORM; break;
                    case (16, 0, 0, 0): this.GX2Format = GX2.GX2SurfaceFormat.TCD_R16_UNORM; break;
                    case (32, 0, 0, 0): this.GX2Format = GX2.GX2SurfaceFormat.TCD_R32_FLOAT; break;
                    case (32, 32, 32, 32): this.GX2Format = GX2.GX2SurfaceFormat.TC_R32_G32_B32_A32_FLOAT; break;
                    case (16, 16, 0, 0): this.GX2Format = GX2.GX2SurfaceFormat.TC_R16_G16_FLOAT; break;
                    case (11, 11, 10, 0): this.GX2Format = GX2.GX2SurfaceFormat.TC_R11_G11_B10_FLOAT; break;
                    case (10, 10, 10, 2): this.GX2Format = GX2.GX2SurfaceFormat.TCS_A2_B10_G10_R10_UNORM; break;
                    case (16, 16, 16, 16): this.GX2Format = GX2.GX2SurfaceFormat.TC_R16_G16_B16_A16_UNORM; break;
                    default:
                        throw new Exception($"Unsupported format {rgba}");
                }
            }
            else if (imageFormat is LA8)
                this.GX2Format = GX2.GX2SurfaceFormat.TC_R8_G8_UNORM;
            else if (imageFormat is L8)
                this.GX2Format = GX2.GX2SurfaceFormat.TC_R8_UNORM;
            else if (imageFormat is L4)
                this.GX2Format = GX2.GX2SurfaceFormat.T_R4_G4_UNORM;
            else
                throw new Exception($"Unsupported format {imageFormat}");
        }

        public void UpdateFormat(IImageFormat imageFormat)
        {
            var format = ((ImageFormat)imageFormat).GetTextureFormat();

            GX2Format = GX2FormatList.FirstOrDefault(x => x.Value == format).Key;
            // Swap some bgra
            if (format == TextureFormat.BGRA4_UNORM) GX2Format = GX2.GX2SurfaceFormat.TC_R4_G4_B4_A4_UNORM;
            if (format == TextureFormat.BGRA8_UNORM) GX2Format = GX2.GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNORM;

            if (GX2Format == 0)
                throw new Exception($"Failed to find GX2Format for encoder {imageFormat}");
        }

        public override byte[] Deswizzle(GenericTextureBase texture, int arrayLevel, int mipLevel)
        {
            uint bpp = texture.ImageFormat.GetBytesPerPixel();

            GX2.GX2Surface surf = new GX2.GX2Surface();
            surf.bpp = bpp;
            surf.height = texture.Height;
            surf.width = texture.Width;
            surf.depth = texture.Depth;
            surf.alignment = Alignment;
            surf.aa = (uint)AAMode;
            surf.dim = (uint)SurfaceDimension;
            surf.format = (uint)GX2Format;
            surf.use = (uint)SurfaceUse;
            surf.pitch = Pitch;
            surf.data = texture.Data.ToArray();
            surf.mipData = MipData != null ? MipData : texture.Data.ToArray();
            surf.mipOffset = MipOffsets != null ? MipOffsets : new uint[0];
            surf.numMips = texture.MipCount;
            surf.numArray = texture.ArrayCount;
            surf.tileMode = (uint)TileMode;
            surf.swizzle = SwizzleValue;

            return GX2.Decode(surf, arrayLevel, mipLevel);
        }

        public GX2.GX2Surface ToGx2(GenericTextureBase texture)
        {
            uint bpp = texture.ImageFormat.GetBytesPerPixel();

            GX2.GX2Surface surf = new GX2.GX2Surface();
            surf.bpp = bpp;
            surf.height = texture.Height;
            surf.width = texture.Width;
            surf.depth = texture.Depth;
            surf.numArray = texture.ArrayCount;
            surf.alignment = Alignment;
            surf.aa = (uint)AAMode;
            surf.dim = (uint)SurfaceDimension;
            surf.format = (uint)GX2Format;
            surf.use = (uint)SurfaceUse;
            surf.pitch = Pitch;
            surf.data = texture.Data.ToArray();
            surf.mipData = MipData != null ? MipData : texture.Data.ToArray();
            surf.mipOffset = MipOffsets != null ? MipOffsets : new uint[0];
            surf.numMips = texture.MipCount;
            surf.numArray = texture.ArrayCount;
            surf.tileMode = (uint)TileMode;
            surf.swizzle = SwizzleValue;
            surf.compSel = new byte[] { 0, 1, 2, 3 };
            return surf;
        }

        public override byte[] DeswizzleAllSurfaces(GenericTextureBase texture)
        {
            uint bpp = texture.ImageFormat.GetBytesPerPixel();

            GX2.GX2Surface surf = new GX2.GX2Surface();
            surf.bpp = bpp;
            surf.height = texture.Height;
            surf.width = texture.Width;
            surf.depth = texture.ArrayCount;
            surf.alignment = Alignment;
            surf.aa = (uint)AAMode;
            surf.dim = (uint)SurfaceDimension;
            surf.format = (uint)GX2Format;
            surf.use = (uint)SurfaceUse;
            surf.pitch = Pitch;
            surf.data = texture.Data.ToArray();
            surf.mipData = MipData != null ? MipData : texture.Data.ToArray();
            surf.mipOffset = MipOffsets != null ? MipOffsets : new uint[0];
            surf.numMips = texture.MipCount;
            surf.numArray = texture.ArrayCount;
            surf.tileMode = (uint)TileMode;
            surf.swizzle = SwizzleValue;

            return GX2.Decode(surf, -1, -1);
        }

        public override byte[] SwizzleAllSurfaces(GenericTextureBase texture, byte[] imageData)
        {
            SwizzleValue = 0;
            //Swizzle and create surface
            var NewSurface = GX2.CreateGx2Texture(imageData, texture.Name,
                (uint)TileMode,
                (uint)AAMode,
                texture.Width,
                texture.Height,
                texture.ArrayCount * texture.Depth,
                (uint)GX2Format,
                SwizzleValue,
                (uint)SurfaceDimension,
                texture.MipCount);

            Pitch = NewSurface.pitch;
            Alignment = NewSurface.alignment;
            TileMode = (GX2.GX2TileMode)NewSurface.tileMode;
            MipData = NewSurface.mipData;
            return NewSurface.data;
        }

        public static Dictionary<GX2.GX2SurfaceFormat, TextureFormat> GX2FormatList = new Dictionary<GX2.GX2SurfaceFormat, TextureFormat>()
        {
            { GX2.GX2SurfaceFormat.TC_R8_UNORM, TextureFormat.R8_UNORM },
            { GX2.GX2SurfaceFormat.TC_R8_G8_UNORM, TextureFormat.RG8_UNORM },
            { GX2.GX2SurfaceFormat.TCS_R8_G8_B8_A8_UNORM, TextureFormat.RGBA8_UNORM },
            { GX2.GX2SurfaceFormat.TCS_R8_G8_B8_A8_SRGB, TextureFormat.RGBA8_SRGB },

            { GX2.GX2SurfaceFormat.TC_R8_SNORM, TextureFormat.R8_SNORM },
            { GX2.GX2SurfaceFormat.TC_R8_G8_SNORM, TextureFormat.RG8_SNORM },
            { GX2.GX2SurfaceFormat.TC_R8_G8_B8_A8_SNORM, TextureFormat.RGBA8_SNORM },

            { GX2.GX2SurfaceFormat.TC_R4_G4_B4_A4_UNORM, TextureFormat.RGBA4_UNORM },
            { GX2.GX2SurfaceFormat.T_R4_G4_UNORM, TextureFormat.RG4_UNORM },
            { GX2.GX2SurfaceFormat.TC_R5_G5_B5_A1_UNORM, TextureFormat.RGB5A1_UNORM },
            { GX2.GX2SurfaceFormat.TCS_R5_G6_B5_UNORM, TextureFormat.BGR565_UNORM },
            { GX2.GX2SurfaceFormat.TC_A1_B5_G5_R5_UNORM, TextureFormat.BGR5A1_UNORM },
            { GX2.GX2SurfaceFormat.TCS_A2_B10_G10_R10_UNORM, TextureFormat.RGBB10A2_UNORM },

            { GX2.GX2SurfaceFormat.TCD_R16_UNORM, TextureFormat.R16_UNORM },
            { GX2.GX2SurfaceFormat.TC_R16_G16_UNORM, TextureFormat.RG16_UNORM },
            { GX2.GX2SurfaceFormat.TC_R11_G11_B10_FLOAT, TextureFormat.RG11B10_FLOAT },

            { GX2.GX2SurfaceFormat.T_BC1_UNORM, TextureFormat.BC1_UNORM },
            { GX2.GX2SurfaceFormat.T_BC1_SRGB,  TextureFormat.BC1_SRGB },
            { GX2.GX2SurfaceFormat.T_BC2_UNORM, TextureFormat.BC2_UNORM },
            { GX2.GX2SurfaceFormat.T_BC2_SRGB,  TextureFormat.BC2_SRGB },
            { GX2.GX2SurfaceFormat.T_BC3_UNORM, TextureFormat.BC3_UNORM },
            { GX2.GX2SurfaceFormat.T_BC3_SRGB,  TextureFormat.BC3_SRGB },
            { GX2.GX2SurfaceFormat.T_BC4_UNORM, TextureFormat.BC4_UNORM },
            { GX2.GX2SurfaceFormat.T_BC4_SNORM, TextureFormat.BC4_SNORM },
            { GX2.GX2SurfaceFormat.T_BC5_UNORM, TextureFormat.BC5_UNORM },
            { GX2.GX2SurfaceFormat.T_BC5_SNORM, TextureFormat.BC5_SNORM },
        };

        public static List<IImageFormat> GetSupportedFormats()
        {
            List<IImageFormat> formats = new();
            foreach (var v in GX2FormatList.Values)
                formats.Add(new ImageFormat(v));
            return formats;
        }
    }
}
