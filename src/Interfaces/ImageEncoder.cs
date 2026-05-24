using BCnEncoder.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Interfaces
{
    /// <summary>
    /// Image encoder for decoding and encoding image data.
    /// </summary>
    public interface ImageEncoder
    {
        /// <summary>
        /// The total bits per pixel.
        /// </summary>
        uint BitsPerPixel { get; }

        /// <summary>
        /// The total bytes per pixel.
        /// </summary>
        uint BytesPerPixel => (BitsPerPixel + 7) / 8;

        /// <summary>
        /// Decodes raw image data into a pixel buffer.
        /// </summary>
        /// <param name="data">The encoded image data.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <returns>A byte array containing the decoded pixel data.</returns>
        byte[] Decode(byte[] data, uint width, uint height);

        /// <summary>
        /// Encodes a pixel buffer into image data for this format.
        /// </summary>
        /// <param name="data">The raw pixel data.</param>
        /// <param name="width">The width of the image in pixels.</param>
        /// <param name="height">The height of the image in pixels.</param>
        /// <returns>A byte array containing the encoded image data.</returns>
        byte[] Encode(byte[] data, uint width, uint height);

        /// <summary>
        /// Calculates the total size in bytes of an image with the given dimensions.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <returns>The size in bytes.</returns>
        uint CalculateSize(uint width, uint height);
    }
}
