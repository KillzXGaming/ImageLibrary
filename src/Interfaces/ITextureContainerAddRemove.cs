using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Interfaces
{
    public interface ITextureContainerAddRemove
    {
        GenericTextureBase CreateNewTexture();
        void AddTexture(GenericTextureBase textureBase);
        void RemoveTexture(GenericTextureBase textureBase);
        void ClearTextures();
    }
}
