using BCnEncoder.Shared;
using ImageLibrary.Utils;
using ImageLibrary.Formats.Encoders;
using ImageLibrary.Interfaces;
using ImageLibrary.PlatformSwizzle;
using ImageLibrary.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using static ImageLibrary.WiiU.GX2;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageLibrary
{
    public class GenericTextureBase
    {
        /// <summary>
        /// The image name. Used for user interface displaying.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        public uint Width { get; set; }

        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        public uint Height { get; set; }

        private uint _depth = 1;
        /// <summary>
        /// The depth of the image in pixels.
        /// </summary>
        public uint Depth
        {
            get => _depth;
            set => _depth = Math.Max(value, 1);
        }

        private uint _mipCount = 1;

        /// <summary>
        /// The mip count of the image. 
        /// </summary>
        public uint MipCount
        {
            get => _mipCount;
            set => _mipCount = Math.Max(value, 1);
        }
        private uint _arrayCount = 1;

        /// <summary>
        /// The amount of array levels of the image.
        /// </summary>
        public uint ArrayCount
        {
            get => _arrayCount;
            set => _arrayCount = Math.Max(value, 1);
        }

        /// <summary>
        /// The red channel component which determines the channel output.
        /// </summary>
        public TextureChannelType RedComponent { get; set; } = TextureChannelType.Red;

        /// <summary>
        /// The green channel component which determines the channel output.
        /// </summary>
        public TextureChannelType GreenComponent { get; set; } = TextureChannelType.Green;

        /// <summary>
        /// The blue channel component which determines the channel output.
        /// </summary>
        public TextureChannelType BlueComponent { get; set; } = TextureChannelType.Blue;

        /// <summary>
        /// The alpha channel component which determines the channel output.
        /// </summary>
        public TextureChannelType AlphaComponent { get; set; } = TextureChannelType.Alpha;

        /// <summary>
        /// The texture dim type.
        /// </summary>
        public TextureType Type { get; set; } = TextureType.Texture2D;

        /// <summary>
        /// Returns true if type of texture is cubemap.
        /// </summary>
        public bool IsCubemap => Type == TextureType.TextureCube || Type == TextureType.TextureCubeArray;

        /// <summary>
        /// The raw image data.
        /// </summary>
        public Memory<byte> Data { get; set; } = new byte[0];

        /// <summary>
        /// The image format.
        /// </summary>
        public IImageFormat ImageFormat { get; set; } = new ImageFormat(TextureFormat.RGBA8_UNORM);

        /// <summary>
        /// The platform swizzle base.
        /// </summary>
        public PlatformSwizzleBase PlatformSwizzle { get; set; } = new PlatformSwizzleBase();

        /// <summary>
        /// Full format list on what can be supported for re encoding back for the image dialog UI
        /// </summary>
        public List<IImageFormat> SupportedFormats { get; set; } = new List<IImageFormat>();

        /// <summary>
        /// The properties instance.
        /// </summary>
        public object PropertiesObject { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultExportExtension { get; set; } = ".png";

        /// <summary>
        /// Event for when PropertiesObject has been edited in a user interface.
        /// </summary>
        public EventHandler<PropertyChangedEventArgs> OnPropertyChanged;

        /// <summary>
        /// Event for when the texture has been edited.
        /// </summary>
        public EventHandler OnEdited;

        /// <summary>
        /// Event for when the texture has been renamed.
        /// </summary>
        public EventHandler OnRenamed;

        /// <summary>
        /// 
        /// </summary>
        public EventHandler OnRequestEditorUpdate;

        /// <summary>
        /// 
        /// </summary>
        public EventHandler OnRemoved;

        /// <summary>
        /// Object instance for render attachment
        /// </summary>
        public object RenderHandle;

        // Editor functions

        /// <summary>
        /// Determines if the texture is renamable if set in a GUI.
        /// </summary>
        public bool CanRename = true;

        /// <summary>
        /// Determines if the texture is replacable if set in a GUI.
        /// </summary>
        public bool CanReplace = true;

        public GenericTextureBase()
        {
            this.PropertiesObject = new PropertyDisplay(this); 
        }

        public void ReloadEditor()
        {
            OnRequestEditorUpdate?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets the supported format list with the TextureFormat enum.
        /// </summary>
        /// <param name="formats"></param>
        public void SetSupportedFormats(IEnumerable<TextureFormat> formats)
        {
            SupportedFormats.Clear();
            foreach (var format in formats)
                if (ImageLibrary.ImageFormat.IsEncoderSupported(format))
                    SupportedFormats.Add(new ImageFormat(format));
        }

        /// <summary>
        /// Sets the supported format list with an array of image encoders.
        /// </summary>
        /// <param name="formats"></param>
        public void SetSupportedFormats(IEnumerable<ImageEncoder> encoders)
        {
            SupportedFormats.Clear();
            foreach (var encoder in encoders)
                SupportedFormats.Add(new ImageFormat(encoder));
        }

        /// <summary>
        /// Sets the supported format list with an array of 3ds pica formats.
        /// </summary>
        /// <param name="formats"></param>
        public void SetSupportedFormats(IEnumerable<PICATextureFormat> formats)
        {
            SupportedFormats.Clear();
            foreach (var encoder in formats)
                SupportedFormats.Add(new ImageFormat3DS(encoder));
        }

        /// <summary>
        /// Gets the total image size as a string.
        /// </summary>
        /// <returns></returns>
        public string GetDataSize()
        {
            return SizeUtil.GetFileSize(this.Data.Length);
        }

        public virtual bool IsCustomImport(string path)
            => path.EndsWith(".dds") || path.EndsWith(".dds2") || path.EndsWith(".astc");

        /// <summary>
        /// Imports a file based on the file extension.
        /// </summary>
        /// <param name="filePath"></param>
        public virtual void Import(string filePath, ImportSettings settings = null)
        {
            if (filePath.EndsWith(".dds"))
                Import(new DdsFile(filePath));
            else if (filePath.EndsWith(".astc"))
                Import(new AstcFile(filePath));
            else
                Import(Image.Load<Rgba32>(filePath), settings);
        }

        /// <summary>
        /// Imports an image and generates mipmaps if specificed.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="mipCount"></param>
        /// <returns></returns>
        public virtual bool Import(Image<Rgba32> image, ImportSettings settings = null)
        {
            settings ??= new ImportSettings();

            this.Width = (uint)image.Width;
            this.Height = (uint)image.Height;
            this.Depth = 1;
            this.ArrayCount = 1;
            this.MipCount = (uint)settings.MipCount;

            if (settings.FlipVertical)
                image.Mutate(x => x.Flip(FlipMode.Vertical));

            if (settings.CrossCubemap)
                this.Type = TextureType.TextureCube;

            // Expect cubemaps to be cross image if not fully square
            if (IsCubemap && (uint)image.Width / 4 == (uint)image.Height / 3)
            {
                this.ArrayCount = 6;
                this.Width = (uint)image.Width / 4;
                this.Height = (uint)image.Height / 3;

                TextureSurface[] surfaces = new TextureSurface[this.ArrayCount];
                for (int i = 0; i < surfaces.Length; i++)
                    surfaces[i] = new TextureSurface();

                var mipmaps = ImageSharpTextureHelper.GenerateMipmaps(image, this.MipCount);
                foreach (var mipmap in mipmaps)
                {
                    var faces = CrossCubemapConverter.FromCrossImage(mipmap);
                    for (int i = 0; i < faces.Count; i++)
                        surfaces[i].Mipmaps.Add(faces[i]);
                }

                var encoded = ImageUtil.EncodeMipmaps(this, surfaces);
                var swizzle = this.PlatformSwizzle.SwizzleAllSurfaces(this, encoded);
                this.Data = swizzle;
            }
            else
            {
                List<byte[]> mipmaps = ImageSharpTextureHelper.GenerateMipmaps(image, this.MipCount)
                    .Select(x => x.GetSourceInBytes()).ToList();

                var encoded = ImageUtil.EncodeMipmaps(this, mipmaps);
                var swizzle = this.PlatformSwizzle.SwizzleAllSurfaces(this, encoded);
                this.Data = swizzle;
            }

            return true;
        }

        public void ImportSlices(string path, int array, int depth, int mip)
            => ImportSlices(new List<ImportSlice>() { 
                new ImportSlice()
                {
                    Image = Image.Load<Rgba32>(path),
                    Array = array,
                    Depth = depth, 
                    Mip = mip,
                },
            });

        public void ImportSlices(List<ImportSlice> slices)
        {
            foreach (var slice in slices)
            {
                // For slice injection, we inject with our target size
                var image = slice.Image;
                var mip_width = Math.Max(this.Width >> slice.Mip, 1);
                var mip_height = Math.Max(this.Height >> slice.Mip, 1);
                if (image.Width != mip_width || image.Height != mip_height)
                    image.Mutate(x => x.Resize((int)mip_width, (int)mip_height));
                // Next encode the data
                slice.Encoded = this.ImageFormat.Encode(image.GetSourceInBytes(), mip_width, mip_height);
            }
            // Finally inject into the slice of swizzle or unswizzled data
            this.PlatformSwizzle.SwizzleSlices(this, slices);
        }

        /// <summary>
        /// Imports a DDS image.
        /// Returns false if file is not supported or fails.
        /// </summary>
        /// <param name="dds"></param>
        public bool Import(DdsFile dds)
        {
            if (!this.IsFormatSupported(dds.Format))
                return false;

            this.Width = dds.MainHeader.Width;
            this.Height = dds.MainHeader.Height;
            this.Depth = dds.MainHeader.Depth;
            this.ArrayCount = dds.ArrayCount;
            this.MipCount = dds.MainHeader.MipCount;
            this.Type = TextureType.Texture2D;
            this.ImageFormat = new ImageFormat(dds.Format);

            if (dds.IsCubeMap)
                this.Type = TextureType.TextureCube;

            var swizzle = this.PlatformSwizzle.SwizzleAllSurfaces(this, dds.ImageData);
            this.Data = swizzle;

            return true;
        }

        /// <summary>
        /// Imports a ASTC image.
        /// Returns false if file is not supported or fails.
        /// </summary>
        /// <param name="dds"></param>
        public bool Import(AstcFile astc)
        {
            var format = new ImageFormat(astc.GetEncoder());
            if (!this.IsFormatSupported(format))
                return false;

            this.Width = astc.Width;
            this.Height = astc.Height;
            this.Depth = astc.Depth;
            this.ArrayCount = 1;
            this.MipCount = 1;
            this.Type = TextureType.Texture2D;
            this.ImageFormat = new ImageFormat(astc.GetEncoder());

            var swizzle = this.PlatformSwizzle.SwizzleAllSurfaces(this, astc.DataBlock);
            this.Data = swizzle;

            return true;
        }

        public List<Mipmap> GetMipmaps()
        {
            int offset = 0;

            List<Mipmap> list = new List<Mipmap>();
            for (int a = 0; a < this.ArrayCount; a++)
            {
                for (int m = 0; m < this.MipCount; m++)
                {
                    var mip_width = Math.Max(this.Width >> m, 1);
                    var mip_height = Math.Max(this.Height >> m, 1);
                    var size = (uint)this.ImageFormat.GetEncoder().CalculateSize((uint)mip_width, (uint)mip_height);

                    list.Add(new Mipmap()
                    {
                        Array = a,
                        Mip = m,
                        Width = mip_width,
                        Height = mip_height,
                        Data = this.Data.Slice(offset, (int)size).ToArray()
                    });

                    offset += (int)(size);
                }
            }
            return list;
        }

        public class Mipmap
        {
            public int Array;
            public int Mip;
            public uint Width;
            public uint Height;
            public Memory<byte> Data;
        }

        /// <summary>
        /// Gets a list of supported file extensions
        /// </summary>
        /// <returns></returns>
        public virtual List<(string desc, string ext)> GetSupportedImportFileFilters()
        {
            List<(string, string)> filters = new();
            filters.Add(("Portable Network Graphics", ".png"));
            filters.Add(("Direct Draw Surface", ".dds"));
            filters.Add(("Joint Photographic Experts Group", ".jpg"));
            filters.Add(("Bitmap Image", ".bmp"));
            filters.Add(("Tagged Image File Format", ".tiff"));
            filters.Add(("TGA", ".tga"));
            filters.Add(("Graphics Interchange Format", ".gif"));
            filters.Add(("Portable Bitmap file", ".pbm"));
            return filters;
        }

        /// <summary>
        /// Gets a list of supported file extensions
        /// </summary>
        /// <returns></returns>
        public virtual List<(string desc, string ext)> GetSupportedExportFileFilters()
        {
            List<(string, string)> filters = new();
            filters.Add(("Portable Network Graphics", ".png"));
            filters.Add(("Direct Draw Surface", ".dds"));
            filters.Add(("Joint Photographic Experts Group", ".jpg"));
            filters.Add(("Bitmap Image", ".bmp"));
            filters.Add(("Tagged Image File Format", ".tiff"));
            filters.Add(("TGA", ".tga"));
            filters.Add(("Graphics Interchange Format", ".gif"));
            filters.Add(("Portable Bitmap file", ".pbm"));
            return filters;
        }

        /// <summary>
        /// Gets the raw rgba data that returns the output in bytes, width, and height.
        /// Cubemaps will output as a cross image.
        /// </summary>
        /// <param name="arrayLevel"></param>
        /// <param name="mipLevel"></param>
        /// <returns></returns>
        public OutputRgba GetRgba(ExportSettings settings = null)
        {
            settings ??= new ExportSettings();

            var w = PlatformSwizzle.GetSwizzleWidth(this.Width);
            var h = PlatformSwizzle.GetSwizzleHeight(this.Height);

            uint mipWidth = Math.Max(1, w >> settings.MipLevel);
            uint mipHeight = Math.Max(1, h >> settings.MipLevel);

            // Cubemaps will load on an array level and turn into a cross image
            if (IsCubemap && settings.CrossImageCubemap)
            {
                List<byte[]> arrays = new List<byte[]>();
                for (int i = 0; i < 6; i++)
                {
                    byte[] deswizzle = this.PlatformSwizzle.Deswizzle(this, i, settings.MipLevel);
                    var decoded = this.ImageFormat.Decode(deswizzle, mipWidth, mipHeight);
                    arrays.Add(decoded.Data);
                }
                var output = CrossCubemapConverter.ToCrossImage(arrays, (int)mipWidth, (int)mipHeight);
                return new OutputRgba()
                {
                    Rgba = output.Item1,
                    Width = (uint)output.Item2,
                    Height = (uint)output.Item3
                };
            }
            else
            {
                byte[] deswizzle = this.PlatformSwizzle.Deswizzle(this, settings.ArrayLevel, settings.MipLevel);
                var decoded = this.ImageFormat.Decode(deswizzle, mipWidth, mipHeight);
                var rgba = decoded.Data;

                if (settings.UseComponentChannels)
                    rgba = ImageUtil.ProcessComponentChannels(this, rgba, decoded.Width, decoded.Height);

                if (settings.ChannelType != TextureChannelType.RGBA)
                    rgba = ImageUtil.ProcessComponentChannels(rgba, decoded.Width, decoded.Height,
                        settings.ChannelType,
                        settings.ChannelType,
                        settings.ChannelType,
                        TextureChannelType.One);


                return new OutputRgba()
                {
                    Rgba = rgba,
                    Width = decoded.Width,
                    Height = decoded.Height,
                };
            }
        }

        /// <summary>
        /// Directly exports the image as a DDS file.
        /// </summary>
        /// <param name="path"></param>
        public void ExportDDS(string path)
        {
            DdsFile dds = new DdsFile(false);
            dds.MainHeader.Width = this.Width;
            dds.MainHeader.Height = this.Height;
            dds.MainHeader.Depth = this.Depth;
            dds.MainHeader.MipCount = this.MipCount;

            dds.ImageData = this.PlatformSwizzle.DeswizzleAllSurfaces(this);
            dds.Format = this.ImageFormat.GetDDSFormat();

            // Format may need decoding if RGBA8, apply raw rgba surfaces
            if (dds.Format == DdsFile.DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM)
                dds.ImageData = ImageUtil.DecodeAllSurfaces(this, dds.ImageData);

            dds.MainHeader.PitchOrLinearSize = (uint)dds.ImageData.Length / this.Depth;
            dds.SetFlags(dds.Format, !IsCubemap && ArrayCount > 6, IsCubemap);
            dds.Save(path);
        }

        /// <summary>
        /// Exports the image to a file path.
        /// The extension determines the file output.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="arrayLevel"></param>
        /// <param name="mipLevel"></param>
        public virtual void Export(string path, ExportSettings settings = null)
        {
            settings ??= new ExportSettings();

            if (path.EndsWith(".dds"))
                this.ExportDDS(path); 
            else if (path.EndsWith(".astc") && this.ImageFormat.GetEncoder() is Astc astcEncoder)
            {
                var deswizzled = this.PlatformSwizzle.DeswizzleAllSurfaces(this);
                AstcFile astc = new AstcFile(astcEncoder, this.Width, this.Height, this.Depth, deswizzled);
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                    astc.Save(fs);
                }
            }
            else
            {
                if (this.Depth > 1 || this.ArrayCount > 1)
                {
                    string ext = Path.GetExtension(path);
                    for (int i = 0; i < this.ArrayCount; i++)
                    {
                        settings.ArrayLevel = i;
                        var image = this.ToImage(settings);
                        image.Save($"{path.Replace(ext, "")}{i}.png");
                    }
                }
                else
                {
                    var image = this.ToImage(settings);
                    image.Save(path);
                }
            }
        }

        /// <summary>
        /// Gets the image of a surface as rgba.
        /// </summary>
        /// <param name="arrayLevel"></param>
        /// <param name="mipLevel"></param>
        /// <returns></returns>
        public Image<Rgba32> ToImage(ExportSettings settings = null)
        {
            var output = this.GetRgba(settings);
            return Image.LoadPixelData<Rgba32>(output.Rgba, (int)output.Width, (int)output.Height);
        }

        /// <summary>
        /// Returns true if the given image format is supported.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public bool IsFormatSupported(DdsFile.DXGI_FORMAT format) {
            return IsFormatSupported(new ImageFormat(format));
        }


        /// <summary>
        /// Returns true if the given image format is supported.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public bool IsFormatSupported(IImageFormat format) {
            return this.SupportedFormats.Any(x => x.ToString() == format.ToString());  
        }

        public class OutputRgba
        {
            public byte[] Rgba;
            public uint Width;
            public uint Height;
        }

        public class ImportSlice
        {
            public Image<Rgba32> Image;
            public int Array;
            public int Mip;
            public int Depth;
            public byte[] Encoded;
        }

        public class PropertyDisplay
        {
            public string Name => _texture.Name;
            public uint Width => _texture.Width;
            public uint Height => _texture.Height;
            public uint Depth => _texture.Depth;
            public uint ArrayCount => _texture.ArrayCount;
            public uint MipCount => _texture.MipCount;
            public string ImageFormat => _texture.ImageFormat.ToString();
            public string Type => _texture.Type.ToString();

            private GenericTextureBase _texture;

            public PropertyDisplay(GenericTextureBase texture)
            {
                _texture = texture;
            }
        }
    }
}
