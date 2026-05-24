using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats.Encoders.Gcn
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Color32 : IEquatable<Color32>
    {
        public byte R, G, B, A;

        public Color32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public bool Equals(Color32 other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object obj) => obj is Color32 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public override string ToString()
        {
            return string.Format("[Color32] (r: {0} g: {1} b: {2} a: {3})", R, G, B, A);
        }

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return R;
                    case 1:
                        return G;
                    case 2:
                        return B;
                    case 3:
                        return A;

                    default:
                        throw new ArgumentOutOfRangeException("index");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        R = value;
                        break;

                    case 1:
                        G = value;
                        break;

                    case 2:
                        B = value;
                        break;

                    case 3:
                        A = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("index");
                }
            }
        }
    }
}
