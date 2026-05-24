using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Interfaces
{
    /// <summary>
    /// Interface to access block sizes for compressed block image formats such as BCN and ASTC.
    /// </summary>
    public interface ImageBlockFormat
    {
        /// <summary>
        /// Gets the block width.
        /// </summary>
        uint BlockWidth { get; }
        /// <summary>
        /// Gets the block height.
        /// </summary>
        uint BlockHeight { get; }
        /// <summary>
        /// Gets the block depth.
        /// </summary>
        uint BlockDepth { get; }
    }
}
