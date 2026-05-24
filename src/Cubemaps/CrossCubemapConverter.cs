using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Cubemaps
{
    public class CrossCubemapConverter
    {
        /// <summary>
        /// Converts a cross image cubemap into a list of RGBA byte[] surfaces.
        /// </summary>
        /// <param name="rgba"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static List<byte[]> FromCrossImage(byte[] rgba, int width, int height)
        {
            var image = Image.LoadPixelData<Rgba32>(rgba, width, height);
            return FromCrossImage(image);
        }

        /// <summary>
        /// Converts a cross image cubemap into a list of RGBA byte[] surfaces.
        /// </summary>
        /// <param name="rgba"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static List<byte[]> FromCrossImage(Image<Rgba32>  image)
        {
            List<byte[]> surfaces = new List<byte[]>();

            int faceWidth = image.Width / 4;
            int faceHeight = image.Height / 3;

            void SetSurface(int x, int y)
            {
                var face = image.Clone();
                face.Mutate(c => c.Crop(new Rectangle(x, y, faceWidth, faceHeight)));

                surfaces.Add(face.GetSourceInBytes());
                face.Dispose();
            }

            // +x face
            SetSurface(2 * faceWidth, 1 * faceHeight);
            // -x face
            SetSurface(0 * faceWidth, 1 * faceHeight);
            // +y face
            SetSurface(1 * faceWidth, 0 * faceHeight);
            // -y face
            SetSurface(1 * faceWidth, 2 * faceHeight);
            // +z face
            SetSurface(1 * faceWidth, 1 * faceHeight);
            // -z face
            SetSurface(3 * faceWidth, 1 * faceHeight);

            return surfaces;
        }

        /// <summary>
        /// Converts a list of rgba surfaces to a single rgba image.
        /// </summary>
        /// <param name="surfaces"></param>
        /// <param name="faceWidth"></param>
        /// <param name="faceHeight"></param>
        /// <returns> Returns the rgba image data, width, and height. </returns>
        /// <exception cref="Exception"></exception>
        public static (byte[], int, int) ToCrossImage(List<byte[]> surfaces, int faceWidth, int faceHeight)
        {
            if (surfaces.Count != 6)
                throw new Exception($"Invalid surface count {surfaces.Count}! Expected 6.");

            var width = faceWidth * 4;
            var height = faceHeight * 3;
            var buffer = new byte[width * height * 4];

            var image = Image.LoadPixelData<Rgba32>(buffer, width, height);

            void SetSurface(int x, int y, byte[] face_data)
            {
                var face_image = Image.LoadPixelData<Rgba32>(face_data, faceWidth, faceHeight);
                image.Mutate(c => c.DrawImage(face_image, new Point(x, y), 1.0f));
                face_image.Dispose();
            }

            // +x face
            SetSurface(2 * faceWidth, 1 * faceHeight, surfaces[0]);
            // -x face
            SetSurface(0 * faceWidth, 1 * faceHeight, surfaces[1]);
            // +y face
            SetSurface(1 * faceWidth, 0 * faceHeight, surfaces[2]);
            // -y face
            SetSurface(1 * faceWidth, 2 * faceHeight, surfaces[3]);
            // +z face
            SetSurface(1 * faceWidth, 1 * faceHeight, surfaces[4]);
            // -z face
            SetSurface(3 * faceWidth, 1 * faceHeight, surfaces[5]);

            var output = image.GetSourceInBytes();
            image?.Dispose();

            return (output, width, height);
        }
    }
}
