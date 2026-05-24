using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public class GenericTextureCache
    {
        /// <summary>
        /// A cache for accessing texture instances quickly.
        /// This must be added manually if desired to be used.
        /// </summary>
        public static Dictionary<string, GenericTextureBase> Textures = new Dictionary<string, GenericTextureBase>();


        /// <summary>
        /// A cache for accessing texture instances quickly as icons.
        /// This must be added manually if desired to be used.
        /// </summary>
        public static Dictionary<string, Image<Rgba32>> TextureIcons = new Dictionary<string, Image<Rgba32>>();
    }
}
