using ImageLibrary;
using ImageLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public class ImageCodecs
    {
        public static List<IImageCodec> Codecs = new();

        public static void Subscribe(IImageCodec codec)
        {
            Codecs.Add(codec);
        }

        public static bool TryGetCodec(ImageEncoder imageFormat, out IImageCodec codec)
        {
            codec = null;
            foreach (var c in Codecs)
            {
                if (c.CanUse(imageFormat))
                {
                    codec = c;
                    return true;
                }
            }
            return false;
        }
    }

    public interface IImageCodec
    {
        bool CanUse(ImageEncoder encoder);
        byte[] Decode(ImageEncoder encoder, byte[] input, uint width, uint height);
        byte[] Encode(ImageEncoder encoder, byte[] input, uint width, uint height);
    }
}
