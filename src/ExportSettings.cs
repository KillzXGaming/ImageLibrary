using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public class ExportSettings
    {
        /// <summary>
        /// Exports cubemaps as a cross image if type is cubemap.
        /// </summary>
        public bool CrossImageCubemap { get; set; } = true;

        /// <summary>
        /// The array surface to export.
        /// </summary>
        public int ArrayLevel { get; set; } = 0;

        /// <summary>
        /// The mip surface to export.
        /// </summary>
        public int MipLevel { get; set; } = 0;
        /// <summary>
        /// Determines to use component channels on export.
        /// </summary>
        public bool UseComponentChannels { get; set; } = true;

        public TextureChannelType ChannelType { get; set; } = TextureChannelType.RGBA;
    }
}
