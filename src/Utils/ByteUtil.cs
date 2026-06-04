using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Utils
{
    public class ByteUtil
    {
        public static float[] ConvertBytesToFloat(byte[] bytes)
        {
            return bytes.Select(x => (float)(x / 255.0f)).ToArray();
        }
        public static byte[] ConvertFloatToBytes(float[] bytes)
        {
            return bytes.Select(x => ByteUtil.ConvertToByte(x)).ToArray();
        }

        public static byte ConvertToByte(float v)
        {
            return (byte)(Math.Clamp(v, 0f, 1f) * 255);
        }

        /// <summary>
        /// Combines an array of byte[] into a singular byte[]
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] CombineByteArray(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}
