using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Formats
{
    public class DataConverterHelper
    {


        // ==== 16-bit conversions ====
        public static T Convert16<T>(ushort v, DataType type) where T : unmanaged
        {
            return type switch
            {
                DataType.UNorm => (typeof(T) == typeof(float))
                    ? (T)(object)(v / 65535f)
                    : (T)(object)v,
                DataType.SNorm => (typeof(T) == typeof(float))
                    ? (T)(object)(Math.Clamp((short)v / 32767f, -1f, 1f))
                    : (T)(object)(short)v,
                DataType.Float => (T)(object)(float)BitConverter.UInt16BitsToHalf(v),
                DataType.UInt => (T)(object)v,
                DataType.SInt => (T)(object)(short)v,
                _ => throw new NotSupportedException()
            }; 
        }

        public static ushort Encode16<T>(T val, DataType type) where T : unmanaged
        {
            return type switch
            {
                DataType.UNorm => (ushort)(Math.Clamp(Convert.ToSingle(val), 0f, 1f) * 65535f),
                DataType.SNorm => (ushort)(short)(Math.Clamp(Convert.ToSingle(val), -1f, 1f) * 32767f),
                DataType.Float => (ushort)BitConverter.HalfToUInt16Bits((Half)Convert.ToSingle(val)),
                DataType.UInt => (ushort)Convert.ToUInt32(val),
                DataType.SInt => (ushort)(short)Convert.ToInt32(val),
                _ => throw new NotSupportedException()
            }; 
        }

        // ==== 32-bit conversions ====
        public static T Convert32<T>(uint v, DataType type) where T : unmanaged
        {
            return type switch
            {
                DataType.UNorm => (typeof(T) == typeof(float))
                    ? (T)(object)(v / (float)uint.MaxValue)
                    : (T)(object)v,
                DataType.SNorm => (typeof(T) == typeof(float))
                    ? (T)(object)(Math.Clamp((int)v / 2147483647f, -1f, 1f))
                    : (T)(object)(int)v,
                DataType.Float => (T)(object)BitConverter.UInt32BitsToSingle(v),
                DataType.UInt => (T)(object)v,
                DataType.SInt => (T)(object)(int)v,
                _ => throw new NotSupportedException()
            };
        }

        public static uint Encode32<T>(T val, DataType type) where T : unmanaged
        {
            return type switch
            {
                DataType.UNorm => (uint)(Math.Clamp(Convert.ToSingle(val), 0f, 1f) * uint.MaxValue),
                DataType.SNorm => (uint)(int)(Math.Clamp(Convert.ToSingle(val), -1f, 1f) * 2147483647f),
                DataType.Float => BitConverter.SingleToUInt32Bits(Convert.ToSingle(val)),
                DataType.UInt => Convert.ToUInt32(val),
                DataType.SInt => (uint)Convert.ToInt32(val),
                _ => throw new NotSupportedException()
            };
        }

        // === Converters ===
        private static T ToUNorm<T>(ushort v) where T : unmanaged
        {
            if (typeof(T) == typeof(float))
                return (T)(object)(v / 65535f);
            if (typeof(T) == typeof(byte))
                return (T)(object)(byte)(v >> 8);
            if (typeof(T) == typeof(ushort))
                return (T)(object)v;
            throw new NotSupportedException();
        }

        private static T ToSNorm<T>(short v) where T : unmanaged
        {
            if (typeof(T) == typeof(float))
                return (T)(object)Math.Clamp(v / 32767f, -1f, 1f);
            if (typeof(T) == typeof(byte))
                return (T)(object)(byte)((v + 32768) >> 8);
            if (typeof(T) == typeof(short))
                return (T)(object)v;
            throw new NotSupportedException();
        }

        private static T ToFloat16<T>(ushort v) where T : unmanaged
        {
            if (typeof(T) != typeof(float))
                throw new NotSupportedException("Float16 only decodes to float.");
            return (T)(object)BitConverter.UInt16BitsToHalf(v);
        }

        private static ushort FromUNorm<T>(T val) where T : unmanaged
        {
            if (typeof(T) == typeof(float))
                return (ushort)(Math.Clamp((float)(object)val, 0f, 1f) * 65535f);
            if (typeof(T) == typeof(byte))
                return (ushort)((byte)(object)val * 257);
            if (typeof(T) == typeof(ushort))
                return (ushort)(object)val;
            throw new NotSupportedException();
        }

        private static ushort FromSNorm<T>(T val) where T : unmanaged
        {
            if (typeof(T) == typeof(float))
                return (ushort)(short)(Math.Clamp((float)(object)val, -1f, 1f) * 32767f);
            if (typeof(T) == typeof(byte))
                return (ushort)((sbyte)((byte)(object)val - 128));
            if (typeof(T) == typeof(short))
                return (ushort)(short)(object)val;
            throw new NotSupportedException();
        }

        private static ushort FromFloat16<T>(T val) where T : unmanaged
        {
            if (typeof(T) != typeof(float))
                throw new NotSupportedException("Float16 only encodes from float.");

            Half halfValue = (Half)((float)(object)val);
            return (ushort)BitConverter.HalfToUInt16Bits(halfValue);
        }

        private static T Cast<T>(object v) where T : unmanaged
            => (T)Convert.ChangeType(v, typeof(T));
    }
}
