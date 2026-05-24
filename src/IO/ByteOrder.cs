using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.IO
{
    /// <summary>
    /// The byte order for reading or writing data.
    /// </summary>
    public enum ByteOrder : ushort
    {
        LittleEndian = 0xFFFE,
        BigEndian = 0xFEFF
    }
}
