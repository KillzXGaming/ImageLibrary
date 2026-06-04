using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    /// <summary>
    /// Represents a format for getting texture data.
    /// </summary>
    public interface ITextureFormat
    {
        GenericTextureBase Texture { get; }
    }

    /// <summary>
    /// Represents a format for getting supported formats.
    /// </summary>
    public interface ISupportedImageFormats
    {
        List<IImageFormat> SupportedFormats { get; }
    }
}
