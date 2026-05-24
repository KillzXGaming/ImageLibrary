using ImageLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    /// <summary>
    /// Image format implementations, providing methods for encoding,
    /// decoding, and information related to the format.
    /// </summary>
    public interface IImageFormat
    {
        /// <summary>
        /// Gets the encoder associated with this image format.
        /// </summary>
        /// <returns>An instance of <see cref="ImageEncoder"/> for this format.</returns>
        ImageEncoder GetEncoder();

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
        /// Gets the number of bytes used per pixel for this image format.
        /// Defaults to 1.
        /// </summary>
        public uint GetBytesPerPixel() => 1;

        /// <summary>
        /// Gets the number of bits used per pixel for this image format.
        /// Defaults to 1.
        /// </summary>
        public uint GetBitsPerPixel() => 1;

        /// <summary>
        /// Calculates the total size in bytes of an image with the given dimensions.
        /// </summary>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <returns>The size in bytes.</returns>
        public uint GetSize(uint width, uint height) => GetBytesPerPixel() * width * height;

        /// <summary>
        /// Calculates total amount of mips possible for the image based on width/height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public uint CalculateMipCount(uint width, uint height)
        {
            return 1 + (uint)Math.Floor(Math.Log(Math.Max(width, height), 2));
        }

        /// <summary>
        /// Gets the corresponding DXGI format used in DDS files for this image format.
        /// If the format is RGBA8, it will attempt to decode as RGBA8 incase the format is not supported by DDS.
        /// </summary>
        /// <returns>A <see cref="DDS.DXGI_FORMAT"/> value representing the format.</returns>
        DDS.DXGI_FORMAT GetDDSFormat();
    }
}
