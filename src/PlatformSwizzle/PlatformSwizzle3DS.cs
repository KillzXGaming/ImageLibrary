using ImageLibrary.WiiU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.PlatformSwizzle.Algorithms.Ctr;
using ImageLibrary.Utils;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageLibrary.PlatformSwizzle
{
    /// <summary>
    /// A swizzle platform base for the 3DS pica texture format.
    /// </summary>
    public class PlatformSwizzle3DS : PlatformSwizzleBase
    {
        /// <summary>
        /// The 3DS swizzle width, set to the power of 2
        /// </summary>
        public override uint GetSwizzleWidth(uint width) => width + 7u & ~7u;
        /// <summary>
        /// The 3DS swizzle height, set to the power of 2
        /// </summary>
        public override uint GetSwizzleHeight(uint height) => height + 7u & ~7u;
        /// <summary>
        /// The swizzle transformation.
        /// </summary>
        public PICASwizzleTransformation Transformation { get; set; }

        public PlatformSwizzle3DS()
        {
            Transformation = PICASwizzleTransformation.None;
        }

        /// <summary>
        /// Deswizzles a specific surface level.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="arrayLevel"></param>
        /// <param name="mipLevel"></param>
        /// <returns></returns>
        public override byte[] Deswizzle(GenericTextureBase texture, int arrayLevel, int mipLevel)
        {
            int offset = 0;
            for (int m = 0; m < texture.MipCount; m++)
            {
                uint mipWidth = Math.Max(1, texture.Width >> m);
                uint mipHeight = Math.Max(1, texture.Height >> m);
                var size = Swizzle3DS.CalculateLength((int)mipWidth, (int)mipHeight, texture.ImageFormat);

                if (offset + size > texture.Data.Length)
                    return texture.Data.ToArray();

                if (arrayLevel == 0 && mipLevel == 0)
                {
                    var swizzle = Swizzle3DS.Deswizzle(texture.Data.Slice(offset,
                        texture.Data.Length - offset).ToArray(),
                      (int)texture.Width, (int)texture.Height,
                      texture.ImageFormat, Transformation);

                    return swizzle;
                }

                offset += (int)size;
            }
            return DeswizzleAllSurfaces(texture);
        }


        /// <summary>
        /// Deswizzles all surfaces, including mipmaps.
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public override byte[] DeswizzleAllSurfaces(GenericTextureBase texture)
        {
            List<byte[]> surfaces = new List<byte[]>();

            int offset = 0;
            for (int m = 0; m < texture.MipCount; m++)
            {
                uint mipWidth = Math.Max(1, texture.Width >> m);
                uint mipHeight = Math.Max(1, texture.Height >> m);
                var size = Swizzle3DS.CalculateLength((int)mipWidth, (int)mipHeight, texture.ImageFormat);
                var data = texture.Data.Slice(offset, size).ToArray();

                var swizzle = Swizzle3DS.Deswizzle(data,
                          (int)texture.Width, (int)texture.Height,
                          texture.ImageFormat, Transformation);

                surfaces.Add(swizzle);
                offset += (int)size;
            }
            return ByteUtil.CombineByteArray(surfaces.ToArray());
        }

        /// <summary>
        /// Swizzles a specific surface level.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="imageData"></param>
        /// <param name="arrayLevel"></param>
        /// <param name="mipLevel"></param>
        /// <returns></returns>
        public override byte[] Swizzle(GenericTextureBase texture, byte[] imageData, int arrayLevel, int mipLevel)
        {
            var swizzle = Swizzle3DS.Swizzle(imageData,
                  (int)texture.Width, (int)texture.Height,
                  texture.ImageFormat, Transformation);

            int offset = 0;
            for (int m = 0; m < texture.MipCount; m++)
            {
                uint mipWidth = Math.Max(1, texture.Width >> m);
                uint mipHeight = Math.Max(1, texture.Height >> m);
                var size = Swizzle3DS.CalculateLength((int)mipWidth, (int)mipHeight, texture.ImageFormat);

                if (arrayLevel == 0 && mipLevel == m)
                {
                    return texture.Data.Slice(offset, size).ToArray();
                }

                offset += (int)size;
            }
            return SwizzleAllSurfaces(texture, imageData);
        }

        /// <summary>
        /// Swizzles to all surfaces, including the mipmaps.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public override byte[] SwizzleAllSurfaces(GenericTextureBase texture, byte[] imageData)
        {
            return imageData;

            List<byte[]> surfaces = new List<byte[]>();

            int offset = 0;
            for (int m = 0; m < texture.MipCount; m++)
            {
                uint mipWidth = Math.Max(1, texture.Width >> m);
                uint mipHeight = Math.Max(1, texture.Height >> m);
                var size = Swizzle3DS.CalculateLength((int)mipWidth, (int)mipHeight, texture.ImageFormat);

                var swizzle = Swizzle3DS.Swizzle(imageData,
                          (int)texture.Width, (int)texture.Height,
                          texture.ImageFormat, Transformation);

                surfaces.Add(swizzle);
                offset += (int)size;
            }
            return ByteUtil.CombineByteArray(surfaces.ToArray());
        }
    }
}
