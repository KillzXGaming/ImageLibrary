using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.PlatformSwizzle
{
    public class PlatformSwizzleBase
    {
        /// <summary>
        /// The expected width dimension for swizzling.
        /// </summary>
        /// <param name="width"></param>
        /// <returns></returns>
        public virtual uint GetSwizzleWidth(uint width) => width;
        /// <summary>
        /// The expected height dimension for swizzling.
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        public virtual uint GetSwizzleHeight(uint height) => height;
        /// <summary>
        /// Deswizzles all surface layers including both array and mip levels.
        /// </summary>
        /// <param name="imageInfo"></param>
        /// <returns></returns>
        public virtual byte[] DeswizzleAllSurfaces(GenericTextureBase imageInfo)
        {
            return imageInfo.Data.ToArray();
        }

        /// <summary>
        /// Deswizzles a specific layer given an array and mip level.
        /// </summary>
        /// <param name="imageInfo"></param>
        /// <param name="arrayLevel"></param>
        /// <param name="mipLevel"></param>
        /// <returns></returns>
        public virtual byte[] Deswizzle(GenericTextureBase imageInfo, int arrayLevel, int mipLevel)
        {
            // Deswizzling between platforms will only get the specified surface/mip levels
            int ofs = 0;
            for (int a = 0; a < imageInfo.ArrayCount; a++)
            {
                for (int m = 0; m < imageInfo.MipCount; m++)
                {
                    uint w = Math.Max(1, imageInfo.Width >> m);
                    uint h = Math.Max(1, imageInfo.Height >> m);
                    var size = imageInfo.ImageFormat.GetSize(w, h);

                    if (mipLevel == m && arrayLevel == a)
                        return imageInfo.Data.Slice(ofs, imageInfo.Data.Length - ofs).ToArray();

                    ofs += (int)size;
                }
            }
            return imageInfo.Data.ToArray();
        }

        /// <summary>
        /// Swizzles a specific surface level.
        /// </summary>
        /// <param name="imageInfo"></param>
        /// <param name="imageData"></param>
        /// <param name="arrayLevel"></param>
        /// <param name="mipLevel"></param>
        /// <returns></returns>
        public virtual byte[] Swizzle(GenericTextureBase imageInfo, byte[] imageData, int arrayLevel, int mipLevel)
        {
            return imageData;
        }

        /// <summary>
        /// Swizzles all layers including arrays and mip maps.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        public virtual byte[] SwizzleAllSurfaces(GenericTextureBase texture, byte[] imageData)
        {
            return imageData;
        }
    }
}
