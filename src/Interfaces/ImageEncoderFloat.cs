using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public interface IImageEncoderFloat
    {
        float[] DecodeFloats(byte[] data, uint width, uint height);
        byte[] EncodeFloats(float[] data, uint width, uint height);
    }
}
