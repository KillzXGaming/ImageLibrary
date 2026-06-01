using BCnEncoder.Shared;
using ImageLibrary.Formats.Encoders.Gcn;
using Microsoft.CodeCoverage.Core.Reports.Coverage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;

namespace ImageLibrary.Test
{
    [TestClass]
    public sealed class Test1
    {
        const string OUTPUT_FOLDER = "OUTPUT";

        static string IMG_FILE => Path.Combine("Resources", "grid.png");
        static string CUBEMAP_FILE => Path.Combine("Resources", "cubemap.png");

        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod]
        public void EncodeCubemap()
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormat(TextureFormat.RGBA8_UNORM);
            textureBase.PlatformSwizzle = new PlatformSwizzle.PlatformSwizzleSwitch();
            textureBase.Import(CUBEMAP_FILE, new ImportSettings()
            {
                CrossCubemap = true, MipCount = 2,
            });
            // Slice inject test
            textureBase.ImportSlices(IMG_FILE, array:0, depth:0, mip:1);
            textureBase.ExportDDS(Path.Combine(OUTPUT_FOLDER, $"CUBEMAP_switch.dds"));
        }
        /*
        [TestMethod]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM)]
        public void TestEncodeBCN(DDS.DXGI_FORMAT format)
        {
            using var image = Image.Load<Rgba32>(IMG_FILE);

            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormat(format);
            textureBase.Import(image);
            textureBase.ExportDDS(Path.Combine(OUTPUT_FOLDER, $"TEST_{format}.dds"));
        }

        [TestMethod]
        [DataRow(TextureFormat.ASTC_4x4_UNORM)]
        [DataRow(TextureFormat.ASTC_5x4_UNORM)]
        [DataRow(TextureFormat.ASTC_5x5_UNORM)]
        [DataRow(TextureFormat.ASTC_6x5_UNORM)]
        [DataRow(TextureFormat.ASTC_6x6_UNORM)]
        [DataRow(TextureFormat.ASTC_8x5_UNORM)]
        [DataRow(TextureFormat.ASTC_8x6_UNORM)]
        [DataRow(TextureFormat.ASTC_8x8_UNORM)]
        [DataRow(TextureFormat.ASTC_10x5_UNORM)]
        [DataRow(TextureFormat.ASTC_10x6_UNORM)]
        [DataRow(TextureFormat.ASTC_10x8_UNORM)]
        [DataRow(TextureFormat.ASTC_10x10_UNORM)]
        [DataRow(TextureFormat.ASTC_12x10_UNORM)]
        [DataRow(TextureFormat.ASTC_12x12_UNORM)]
        public void TestEncodeASTC(TextureFormat format)
        {
            using var image = Image.Load<Rgba32>(IMG_FILE);

            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormat(format);
            textureBase.Import(image);
            textureBase.Export(Path.Combine(OUTPUT_FOLDER, $"TEST_{format}.png"));
        }

        [TestMethod]
        [DataRow(ImageFormatGcn.GcnTextureFormats.CMPR)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.IA4)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.IA8)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.I8)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.I4)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.RGB565)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.RGBA32)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.RGB5A3)]
        public void TestEncodeGCN(ImageFormatGcn.GcnTextureFormats format)
        {
            using var image = Image.Load<Rgba32>(IMG_FILE);
            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormatGcn(format);
            textureBase.Import(image);
            textureBase.Export(Path.Combine(OUTPUT_FOLDER, $"GCN_{format}.png"));
        }

        [TestMethod]
        [DataRow(ImageFormatGcn.GcnTextureFormats.C8, ImageFormatGcn.GcnPaletteFormats.RGB565)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.C8, ImageFormatGcn.GcnPaletteFormats.RGB5A3)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.C8, ImageFormatGcn.GcnPaletteFormats.IA8)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.C4, ImageFormatGcn.GcnPaletteFormats.RGB565)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.C4, ImageFormatGcn.GcnPaletteFormats.RGB5A3)]
        [DataRow(ImageFormatGcn.GcnTextureFormats.C4, ImageFormatGcn.GcnPaletteFormats.IA8)]
        public void TestEncodeGCNPalette(
            ImageFormatGcn.GcnTextureFormats format, 
            ImageFormatGcn.GcnPaletteFormats paletteFormat)
        {
            using var image = Image.Load<Rgba32>(IMG_FILE);
            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormatGcn(format, new GcnPalette(paletteFormat));
            textureBase.Import(image);
            textureBase.Export(Path.Combine(OUTPUT_FOLDER, $"GCN_{format}_{paletteFormat}.png"));
        }

        [TestMethod]
        [DataRow(PICATextureFormat.ETC1)]
        [DataRow(PICATextureFormat.ETC1A4)]
        [DataRow(PICATextureFormat.LA8)]
        [DataRow(PICATextureFormat.LA4)]
        [DataRow(PICATextureFormat.L8)]
        [DataRow(PICATextureFormat.L4)]
        [DataRow(PICATextureFormat.RGB8)]
        [DataRow(PICATextureFormat.RGB565)]
        [DataRow(PICATextureFormat.RGBA4)]
        [DataRow(PICATextureFormat.RGBA5551)]
        [DataRow(PICATextureFormat.RGBA8)]
        [DataRow(PICATextureFormat.HiLo8)]
        [DataRow(PICATextureFormat.A8)]
        [DataRow(PICATextureFormat.A4)]
        public void TestEncode3DS(PICATextureFormat format)
        {
            using var image = Image.Load<Rgba32>(IMG_FILE);
            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormat3DS(format);
            textureBase.Import(image);
            textureBase.Export(Path.Combine(OUTPUT_FOLDER, $"CTR_{format}.png"));
        }

        [TestMethod]
        [DataRow(ImageFormatDS.NitroTexFormat.Palette4)]
        [DataRow(ImageFormatDS.NitroTexFormat.Palette16)]
        [DataRow(ImageFormatDS.NitroTexFormat.Palette256)]
        [DataRow(ImageFormatDS.NitroTexFormat.Direct)]
        [DataRow(ImageFormatDS.NitroTexFormat.A3I5)]
        [DataRow(ImageFormatDS.NitroTexFormat.A5I3)]
        [DataRow(ImageFormatDS.NitroTexFormat.CMPR_4x4)]
        public void TestEncodeDS(ImageFormatDS.NitroTexFormat format)
        {
            using var image = Image.Load<Rgba32>(IMG_FILE);
            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormatDS(format);
            textureBase.Import(image);
            textureBase.Export(Path.Combine(OUTPUT_FOLDER, $"NITRO_{format}.png"));
        }*/
    }
}
