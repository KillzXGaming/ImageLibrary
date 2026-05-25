
using ImageLibrary;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

using var image = Image.Load<Rgba32>("grid.png");

GenericTextureBase textureBase = new();
textureBase.ImageFormat = new ImageFormat3DS(PICATextureFormat.ETC1A4);
textureBase.Import(image);
textureBase.Export(Path.Combine($"grid{PICATextureFormat.ETC1A4}.png"));