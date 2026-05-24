using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    /// <summary>
    /// Represents a format for getting a list of textures.
    /// </summary>
    public interface ITextureContainerFormat
    {
        IEnumerable<GenericTextureBase> GetTextures();
    }
}
