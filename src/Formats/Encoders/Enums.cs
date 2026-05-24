using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public enum DataType
    {
        UNorm,
        SNorm,
        Float,
        UInt,
        SInt,
    }
    public enum DataFormat
    {
        Bit16,
        Bit32,
    }
    public enum ChannelLayout
    {
        R,
        RG,
        RGBA
    }
}
