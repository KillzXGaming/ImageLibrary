using PVRTexLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Helpers
{
    internal class PvrTextureHelper
    {
        public const ulong Rgba8888 = 0x0808080861626772ul;

        public static PVRTexture CreateTexture(byte[] tex, PVRTexLibPixelFormat format, uint width, uint height)
            => CreateTexture(tex, (ulong)format, width, height);

        public static unsafe PVRTexture CreateTexture(byte[] tex, ulong format, uint width, uint height)
        {
            GCHandle dataPtr = GCHandle.Alloc(tex, GCHandleType.Pinned);

            var header = new PVRTextureHeader(format, (uint)width, (uint)height,
                colourSpace: PVRTexLibColourSpace.Linear, channelType: PVRTexLibVariableType.UnsignedByteNorm);
            var texture = new PVRTexture(header, (void*)dataPtr.AddrOfPinnedObject());

            dataPtr.Free();

            return texture;
        }

        public static unsafe byte[] GetData(PVRTexture texture)
        {
            var dataPtr = (nint)texture.GetTextureDataPointer();
            ulong dataSize = texture.GetTextureDataSize(0);

            var data = new byte[dataSize];
            Marshal.Copy(dataPtr, data, 0, data.Length);

            return data;
        }
    }
}
