
using ImageLibrary;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using ImageLibrary.PlatformSwizzle;

using var image = Image.Load<Rgba32>("grid.png");

GenericTextureBase textureBase = new();
textureBase.ImageFormat = new ImageFormat(TextureFormat.ASTC_4x4_UNORM);
textureBase.PlatformSwizzle = new PlatformSwizzleSwitch();
textureBase.Import(image);
textureBase.Export(Path.Combine($"gridSwitch.png"));