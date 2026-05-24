using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;

namespace ImageLibrary
{
    /// <summary>
    /// Extension for image sharp images.
    /// </summary>
    public static class ImageSharpExtension
    {
        /// <summary>
        /// Gets the raw RGBA bytes for an image.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static byte[] GetSourceInBytes(this Image<Rgba32> image)
        {
            var _IMemoryGroup = image.GetPixelMemoryGroup();
            var data = _IMemoryGroup.SelectMany(row => MemoryMarshal.AsBytes(row.Span).ToArray())
                       .ToArray();

            return data;
        }
    }
}
