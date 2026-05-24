using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Net;

namespace ImageLibrary.Test
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM)]
        [DataRow(DDS.DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM)]
        public void TestEncodeBCN(DDS.DXGI_FORMAT format)
        {
            EncodeDDS(Path.Combine("Resources", "grid.png"), format);
        }

        private void EncodeDDS(string filePath, DDS.DXGI_FORMAT format)
        {
            using var image = Image.Load<Rgba32>(filePath);

            Directory.CreateDirectory("batch");

            GenericTextureBase textureBase = new();
            textureBase.ImageFormat = new ImageFormat(format);
            textureBase.Import(image);
            textureBase.ExportDDS(Path.Combine("batch", $"TEST_{format}.dds"));
        }
    }
}
