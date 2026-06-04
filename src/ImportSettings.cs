using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public class ImportSettings
    {
        public bool FlipVertical = false;

        public int MipCount = 1;
        public bool AutomateMipmaps = false;
        public bool CrossCubemap = false;
    }
}
