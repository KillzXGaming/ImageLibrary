using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.IO
{
    public class FileReader : BinaryReader
    {
        /// <summary>
        /// The byte order of the file reader.
        /// </summary>
        public ByteOrder ByteOrder { get; set; } = ByteOrder.LittleEndian;

        /// <summary>
        /// Gets or sets the position of the file reader.
        /// </summary>
        public long Position
        {
            get => this.BaseStream.Position;
            set => this.BaseStream.Position = value;
        }

        /// <summary>
        /// The default encoding for reading strings.
        /// </summary>
        public Encoding Encoding = Encoding.UTF8;

        public FileReader(Stream input, bool leaveOpen = false) : base(input, Encoding.UTF8, leaveOpen)
        {
        }
        public FileReader(byte[] input) : base(new MemoryStream(input), Encoding.UTF8)
        {
        }
        public FileReader(Stream input, Encoding encoding, bool leaveOpen = false) : base(input, encoding, leaveOpen)
        {
            this.Encoding = encoding;
        }

        public void SetByteOrder(ushort v)
        {
            if (v == 0xFFFE)
                this.ByteOrder = ByteOrder.BigEndian;
            else
                this.ByteOrder = ByteOrder.LittleEndian;
        }

        public T ReadEnum<T>(bool strict = false)
        {
            // Get the underlying type of the enum
            Type underlyingType = Enum.GetUnderlyingType(typeof(T));

            object value;

            // Read the appropriate value based on the underlying type
            if (underlyingType == typeof(byte))
                value = this.ReadByte();
            else if (underlyingType == typeof(sbyte))
                value = this.ReadSByte();
            else if (underlyingType == typeof(short))
                value = this.ReadInt16();
            else if (underlyingType == typeof(ushort))
                value = this.ReadUInt16();
            else if (underlyingType == typeof(int))
                value = this.ReadInt32();
            else if (underlyingType == typeof(uint))
                value = this.ReadUInt32();
            else if (underlyingType == typeof(long))
                value = this.ReadInt64();
            else if (underlyingType == typeof(ulong))
                value = this.ReadUInt64();
            else
                throw new NotSupportedException($"Unsupported enum underlying type: {underlyingType}");

            T result = (T)Enum.ToObject(typeof(T), value);

            if (strict && !Enum.IsDefined(typeof(T), result))
                throw new InvalidDataException($"Value {value} is not defined for enum {typeof(T).Name}");

            return result;
        }

        public void SetByteOrder(bool isBigEndian)
        {
            this.ByteOrder = isBigEndian ? ByteOrder.BigEndian : ByteOrder.LittleEndian;
        }
        public void SetByteOrder(ByteOrder bom)
        {
            this.ByteOrder = bom;
        }

        #region Signatures

        public string GetSignature(int length = 4)
        {
            string magic = Encoding.ASCII.GetString(ReadBytes(length));
            this.Position = 0;

            Debug.WriteLine(magic);

            return magic;
        }

        public string ReadSignature(string expected_magic)
        {
            string magic = Encoding.GetString(ReadBytes(expected_magic.Length));
            if (expected_magic != magic)
                throw new Exception($"Expected {expected_magic} but got {magic} instead.");

            return magic;
        }
        public string ReadSignature(string[] expected_magic)
        {
            string magic = Encoding.GetString(ReadBytes(expected_magic.Length));
            if (!expected_magic.Contains(magic))
                throw new Exception($"Expected either magics {string.Join(",", expected_magic)} but got {magic} instead.");

            return magic;
        }

        public bool CheckSignature(uint expected_magic, long seek_pos = 0)
        {
            var pos = this.Position;

            if (seek_pos != 0 && seek_pos + sizeof(uint) <= this.BaseStream.Length)
                this.Seek(seek_pos, SeekOrigin.Begin);

            uint magic = ReadUInt32();
            this.Position = pos;

            return magic == expected_magic;
        }

        public bool CheckSignature(string expected_magic, long seek_pos = 0)
        {
            var pos = this.Position;

            if (seek_pos != 0 && seek_pos + expected_magic.Length <= this.BaseStream.Length)
                this.Seek(seek_pos, SeekOrigin.Begin);

            string magic = Encoding.GetString(ReadBytes(expected_magic.Length));

            this.Position = pos;

            return magic == expected_magic;
        }

        #endregion

        #region Align

        public void Align(int alignment)
        {
            var startPos = Position;
            long position = Seek((-Position % alignment + alignment) % alignment, SeekOrigin.Current);

            Seek(startPos, System.IO.SeekOrigin.Begin);
            while (Position != position)
            {
                ReadByte();
            }
        }

        #endregion

        #region Matrices

        public Matrix4x4[] ReadMatrix3x4s(int count, bool transpose = false)
        {
            Matrix4x4[] matrices = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
                matrices[i] = ReadMatrix3x4(transpose);
            return matrices;
        }

        public Matrix4x4[] ReadMatrix4x4s(int count, bool transpose = false)
        {
            Matrix4x4[] matrices = new Matrix4x4[count];
            for (int i = 0; i < count; i++)
                matrices[i] = ReadMatrix4x4(transpose);
            return matrices;
        }

        public Matrix4x4 ReadMatrix3x4(bool transpose = false)
        {
            float[] values = ReadSingles(12);

            Matrix4x4 matrix = new Matrix4x4(
                values[0], values[1], values[2], values[3],
                values[4], values[5], values[6], values[7],
                values[8], values[9], values[10], values[11],
                0, 0, 0, 1);

            return transpose ? Matrix4x4.Transpose(matrix) : matrix;
        }

        public Matrix4x4 ReadMatrix4x4(bool transpose = false)
        {
            Matrix4x4 matrix = new Matrix4x4(
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle(),
                ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

            return transpose ? Matrix4x4.Transpose(matrix) : matrix;
        }

        #endregion

        #region Vector

        public Vector2 ReadVector2() => new Vector2(ReadSingle(), ReadSingle());
        public Vector3 ReadVector3() => new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
        public Vector4 ReadVector4() => new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        public Quaternion ReadQuaternion() => new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());

        public Vector4 Read1010102SNorm()
        {
            int value = ReadInt32();
            return new Vector4(
                (value << 22 >> 22) / 511f,
                (value << 12 >> 22) / 511f,
                (value << 2 >> 22) / 511f,
                value >> 30);
        }

        #endregion

        #region Strings

        public string ReadFixedString(int length)
        {
            return this.Encoding.GetString(this.ReadBytes((int)length)).Replace('\0', ' ').Replace(" ", "");
        }

        public string ReadFixedString(int length, Encoding encoding)
        {
            return encoding.GetString(this.ReadBytes((int)length)).Replace('\0', ' ').Replace(" ", "");
        }

        public string[] ReadStrings(int count, Encoding encoding = null)
        {
            string[] strings = new string[count];
            for (int i = 0; i < count; i++)
                strings[i] = ReadStringZeroTerminated(encoding);

            return strings;
        }

        public string ReadStringZeroTerminated(Encoding encoding = null)
        {
            List<byte> values = new List<byte>();
            while (this.BaseStream.Position < this.BaseStream.Length)
            {
                byte v = this.ReadByte();
                if (v == 0)
                    break;

                values.Add(v);
            }
            return (encoding ?? Encoding).GetString(values.ToArray());
        }

        #endregion

        #region Seeking

        public long SeekBegin(long offset) => this.BaseStream.Seek(offset, SeekOrigin.Begin);
        public long Seek(long offset, SeekOrigin seekOrigin) => this.BaseStream.Seek(offset, seekOrigin);

        #endregion

        #region Structs

        public static Span<byte> AsSpan<T>(ref T val) where T : unmanaged
        {
            Span<T> valSpan = MemoryMarshal.CreateSpan(ref val, 1);
            return MemoryMarshal.Cast<T, byte>(valSpan);
        }

        #endregion

        #region Array Reading

        public sbyte[] ReadSBytes(int count)
        {
            sbyte[] values = new sbyte[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadSByte();
            return values;
        }

        public bool[] ReadBooleans(int count)
        {
            bool[] values = new bool[count];
            for (int i = 0; i < count; i++)
                values[i] = this.ReadBoolean();
            return values;
        }

        public short[] ReadInt16s(int count)
        {
            short[] result = new short[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadInt16();
            return result;
        }
        public ushort[] ReadUInt16s(int count)
        {
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadUInt16();
            return result;
        }
        public uint[] ReadUInt32s(int count)
        {
            uint[] result = new uint[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadUInt32();
            return result;
        }
        public int[] ReadInt32s(int count)
        {
            int[] result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadInt32();
            return result;
        }
        public ulong[] ReadUInt64s(int count)
        {
            ulong[] result = new ulong[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadUInt64();
            return result;
        }
        public long[] ReadInt64s(int count)
        {
            long[] result = new long[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadInt64();
            return result;
        }
        public float[] ReadSingles(int count)
        {
            float[] result = new float[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadSingle();
            return result;
        }
        public decimal[] ReadDecimals(int count)
        {
            decimal[] result = new decimal[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadDecimal();
            return result;
        }
        public double[] ReadDoubles(int count)
        {
            double[] result = new double[count];
            for (int i = 0; i < count; i++)
                result[i] = this.ReadDouble();
            return result;
        }
        #endregion

        #region Standard Reading

        public override short ReadInt16()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadInt16()
                : BitConverter.ToInt16(ReadReversedBytes(2), 0);
        }

        public override ushort ReadUInt16()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadUInt16()
                : BitConverter.ToUInt16(ReadReversedBytes(2), 0);
        }

        public override int ReadInt32()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadInt32()
                : BitConverter.ToInt32(ReadReversedBytes(4), 0);
        }

        public override uint ReadUInt32()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadUInt32()
                : BitConverter.ToUInt32(ReadReversedBytes(4), 0);
        }

        public override long ReadInt64()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadInt64()
                : BitConverter.ToInt64(ReadReversedBytes(8), 0);
        }

        public override ulong ReadUInt64()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadUInt64()
                : BitConverter.ToUInt64(ReadReversedBytes(8), 0);
        }

        public override float ReadSingle()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadSingle()
                : BitConverter.ToSingle(ReadReversedBytes(4), 0);
        }

        public override double ReadDouble()
        {
            return ByteOrder == ByteOrder.LittleEndian
                ? base.ReadDouble()
                : BitConverter.ToDouble(ReadReversedBytes(8), 0);
        }

        private byte[] ReadReversedBytes(int count)
        {
            var data = base.ReadBytes(count);
            Array.Reverse(data);
            return data;
        }

        #endregion
    }
}
