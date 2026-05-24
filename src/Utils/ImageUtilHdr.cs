using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Utils
{
    public class ImageUtilHdr
    {
        public static byte[] EncodeHDRAlpha(float[] rgba, uint width, uint height, float gamma = 2.2f)
        {
            byte[] output = new byte[width * height * 4];
            int pixelIndex = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Pack HDR data into alpha
                  
                    pixelIndex += 4;
                }
            }
            return output;
        }

        public static byte[] DecodeHDRAlphaToRgba8(byte[] rgba, int width, int height, float gamma = 2.2f)
        {
            float[] buffer = DecodeHDRAlpha(rgba, width, height, gamma);
            return buffer.Select(x => (byte)((Math.Clamp(x, 0.0f, 1.0f) * 255))).ToArray();
        }

        public static float[] DecodeHDRAlpha(byte[] rgba, int width, int height, float gamma = 2.2f)
        {
            float[] output = new float[width * height * 4];

            int pixelIndex = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Compute to HDR usable values
                    float alpha = rgba[pixelIndex + 3] / 255f;
                    for (int i = 0; i < 3; i++)
                    {
                        var col = (rgba[pixelIndex + i] / 255f) * (float)Math.Pow(alpha, 4) * 1024;
                        col = col / (col + 1.0f);
                        col = (float)Math.Pow(col, 1.0f / gamma);

                        output[pixelIndex + i] = col;
                    }
                    if (rgba[pixelIndex + 3] != 0)
                        output[pixelIndex + 3] = 1.0f;
                    pixelIndex += 4;
                }
            }
            return output;
        }
    }
}
