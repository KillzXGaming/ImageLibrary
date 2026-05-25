# ImageLibrary
WIP image processing library. Codebase likely to make changes during development.

## Libraries used
- [tegra_swizzle](https://github.com/KillzXGaming/tegra_swizzle) [MIT license](https://github.com/ScanMountGoat/tegra_swizzle/blob/main/LICENSE)
- [ImageSharp](https://github.com/SixLabors/ImageSharp)
- [image_ddsT](https://github.com/ScanMountGoat/image_dds) [MIT license](https://github.com/ScanMountGoat/image_dds/blob/main/LICENSE)
- [BCnEncoder.Net](https://github.com/Nominom/BCnEncoder.NET)
- [PVRTexLib used for ASTC](https://github.com/YingFengTingYu/PVRTexLib.NET)

## Usage

Make GenericTextureBase instance and set texture parameters. The "Data" must be compressed/encoded data.
```cs

GenericTextureBase texture = new();
texture.Width = width;
texture.Height = height;
texture.MipCount = mipCount;
texture.Data = data;

```

Set the format. Make sure the enum is set to the format you need to decode/encode.

PC
```cs
texture.ImageFormat = new ImageFormat(TextureFormat.BC1_UNORM);
```

Switch
```cs
texture.ImageFormat = new ImageFormat(TextureFormat.BC1_UNORM);
texture.PlatformSwizzle = new PlatformSwizzleSwitch();
```

Wii U
```cs
texture.ImageFormat = new ImageFormat(TextureFormat.BC1_UNORM);
texture.PlatformSwizzle = new PlatformSwizzleWiiU(texture.ImageFormat);
```

GCN
```cs
texture.ImageFormat = new ImageFormatGcn(GcnTextureFormats.RGBA32);
```

GCN with palette
```cs
GcnPalette paletteFormat = new GcnPalette(GcnPaletteFormats.RGB565, paletteBytes);
texture.ImageFormat = new ImageFormatGcn(GcnTextureFormats.C8, paletteFormat);
```

3DS
```cs
texture.ImageFormat = new ImageFormat3DS(PICATextureFormat.ETC1A4);
```

Finally export or turn into rgba.

Export. Supports .dds, .png, .astc
```cs
texture.ExportDDS("test.dds");
```

Image sharp rgba image.
```cs
var image = texture.ToImage();
```

Raw with rgba, width, height. 
```cs
var output = texture.GetRgba();
```
