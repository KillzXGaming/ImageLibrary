using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ImageLibrary
{
    public class ImageSharpTextureHelper
    {
        /// <summary>
        /// Exports a uncompressed rgba8 image given the file path, data, width and height
        /// </summary>
        public static void ExportFile(string filePath, byte[] data, int width, int height)
        {
            var file = Image.LoadPixelData<Rgba32>(data, width, height);
            file.Save(filePath);
        }

        /// <summary>
        /// Resizes the given image with a new width and height using the Lanczos3 resampler algoritim.
        /// </summary>
        public static void Resize(Image<Rgba32> baseImage, int newWidth, int newHeight) {
            baseImage.Mutate(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
        }

        /// <summary>
        /// Generates mipmaps with the given mipmap count from the image provided.
        /// </summary>
        public static Image<Rgba32>[] GenerateMipmaps(Image<Rgba32> baseImage, uint mipLevelCount)
        {
            Image<Rgba32>[] mipLevels = new Image<Rgba32>[mipLevelCount];
            mipLevels[0] = baseImage;
            int i = 1;

            int currentWidth = baseImage.Width;
            int currentHeight = baseImage.Height;
            while ((currentWidth != 1 || currentHeight != 1) && i < mipLevelCount)
            {
                int newWidth = Math.Max(1, currentWidth / 2);
                int newHeight = Math.Max(1, currentHeight / 2);
                Image<Rgba32> newImage = baseImage.Clone(context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                Debug.Assert(i < mipLevelCount);
                mipLevels[i] = newImage;

                i++;
                currentWidth = newWidth;
                currentHeight = newHeight;
            }

            Debug.Assert(i == mipLevelCount);

            return mipLevels;
        }

        /// <summary>
        /// Edits a specific channel with a providied image.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static Image<Rgba32> SetChannelEdit(Image<Rgba32> src, 
            Image<Rgba32> image, ComponentTarget channel)
        {
            if (src.Width != image.Width || src.Height != image.Height)
                image.Mutate(x => x.Resize(src.Width, src.Height));

            //original image
            var rgba = src.GetSourceInBytes();
            //target edit
            var target = image.GetSourceInBytes();

            int index = 0;
            for (int w = 0; w < image.Width; w++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    switch (channel)
                    {
                        case ComponentTarget.Red: rgba[index + 0] = target[index + 0]; break; //set red
                        case ComponentTarget.Green: rgba[index + 1] = target[index + 0]; break; //set green
                        case ComponentTarget.Blue: rgba[index + 2] = target[index + 0]; break; //set blue
                        case ComponentTarget.Alpha: rgba[index + 3] = target[index + 0]; break; //set alpha
                        case ComponentTarget.Color: //set color only
                            rgba[index + 0] = target[index + 0];
                            rgba[index + 1] = target[index + 1];
                            rgba[index + 2] = target[index + 2];
                            break;

                    }
                    index += 4;
                }
            }
            //dispose old
            image.Dispose();
            //newly edited image
            return Image.LoadPixelData<Rgba32>(rgba, (int)image.Width, (int)image.Height);
        }


        /// <summary>
        /// Gets a specific channel with a providied image.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static Image<Rgba32> GetChannel(Image<Rgba32> image, ComponentTarget channel)
        {
            byte[] rgba = new byte[image.Width * image.Height * 4];

            //original image
            var src = image.GetSourceInBytes();

            int index = 0;
            for (int w = 0; w < image.Width; w++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    switch (channel)
                    {
                        case ComponentTarget.Red:
                        case ComponentTarget.Green: 
                        case ComponentTarget.Blue:
                        case ComponentTarget.Alpha:
                            var channelIdx = channel - ComponentTarget.Red;
                            rgba[index + 0] = src[index + channelIdx];
                            rgba[index + 1] = src[index + channelIdx];
                            rgba[index + 2] = src[index + channelIdx];
                            rgba[index + 3] = 255;
                            break; //set alpha
                        case ComponentTarget.Color: //set color only
                            rgba[index + 0] = src[index + 0];
                            rgba[index + 1] = src[index + 1];
                            rgba[index + 2] = src[index + 2];
                            rgba[index + 3] = 255;
                            break;

                    }
                    index += 4;
                }
            }
            //dispose old
            image.Dispose();
            //newly edited image
            return Image.LoadPixelData<Rgba32>(rgba, (int)image.Width, (int)image.Height);
        }
    }
}
