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

        [TestMethod]
        public void TestMethod1()
        {
        }

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
            using var image = Image.Load<Rgba32>(Path.Combine("Resources", "grid.png"));

            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormat(format);
            textureBase.Import(image);
            textureBase.ExportDDS(Path.Combine(OUTPUT_FOLDER, $"TEST_{format}.dds"));
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
            using var image = Image.Load<Rgba32>(Path.Combine("Resources", "grid.png"));
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
            using var image = Image.Load<Rgba32>(Path.Combine("Resources", "grid.png"));
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
            using var image = Image.Load<Rgba32>(Path.Combine("Resources", "grid.png"));
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
        //[DataRow(ImageFormatDS.NitroTexFormat.CMPR_4x4)]
        public void TestEncodeDS(ImageFormatDS.NitroTexFormat format)
        {
            using var image = Image.Load<Rgba32>(Path.Combine("Resources", "grid.png"));
            Directory.CreateDirectory(OUTPUT_FOLDER);

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormatDS(format);
            textureBase.Import(image);
            textureBase.Export(Path.Combine(OUTPUT_FOLDER, $"NITRO_{format}.png"));
        }
    }
}
