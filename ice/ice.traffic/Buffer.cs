using System.Collections.Generic;
using System.Text;
using System;

namespace ICENet.Traffic
{
    public partial struct Buffer
    {
        private List<byte> _buffer;

        private int _readPosition;

        public readonly int Length => _buffer.Count;

        public Buffer(int readPosition = 0)
        {
            _buffer = new List<byte>();
            _readPosition = readPosition;
        }

        public void LoadBytes(byte[] bytes) => _buffer.AddRange(bytes);

        public int Write(byte[] bytes)
        {
            _buffer.AddRange(bytes);
            Array.Reverse(bytes);
            return _buffer.Count;
        }

        public void Write(byte value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _buffer.AddRange(bytes);
        }

        public void Write(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _buffer.AddRange(bytes);
        }

        public void Write(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _buffer.AddRange(bytes);
        }

        public void Write(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _buffer.AddRange(bytes);
        }

        public void Write(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _buffer.AddRange(bytes);
        }

        public void Write(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _buffer.AddRange(bytes);
        }

        public void Write(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            _buffer.AddRange(bytes);
        }

        public void Write(char value)
        {
            var bytes = BitConverter.GetBytes(value);
            _buffer.AddRange(bytes);
        }

        public void Write(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            var stringSize = (ushort)bytes.Length;
            Write(stringSize);

            _buffer.AddRange(bytes);
        }

        public byte[] ReadBytes(int length)
        {
            if (_readPosition + length > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, length).ToArray();
            Array.Reverse(bytes);
            _readPosition += length;
            return bytes;
        }

        public byte ReadByte()
        {
            if (_readPosition + sizeof(byte) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(byte)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(byte);
            return bytes[0];
        }

        public short ReadInt16()
        {
            if (_readPosition + sizeof(short) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(short)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(short);
            return BitConverter.ToInt16(bytes, 0);
        }

        public ushort ReadUInt16()
        {
            if (_readPosition + sizeof(ushort) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(ushort)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(ushort);

            ushort value = (ushort)((bytes[1] << 8) | bytes[0]);
            return value;
        }

        public int ReadInt32()
        {
            if (_readPosition + sizeof(int) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(int)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(int);
            return BitConverter.ToInt32(bytes, 0);
        }

        public long ReadInt64()
        {
            if (_readPosition + sizeof(long) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(long)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(long);
            return BitConverter.ToInt64(bytes, 0);
        }

        public float ReadSingle()
        {
            if (_readPosition + sizeof(float) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(float)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(float);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble()
        {
            if (_readPosition + sizeof(double) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(double)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(double);
            return BitConverter.ToDouble(bytes, 0);
        }

        public bool ReadBoolean()
        {
            if (_readPosition + sizeof(bool) > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range.");

            byte[] bytes = _buffer.GetRange(_readPosition, sizeof(bool)).ToArray();
            Array.Reverse(bytes);
            _readPosition += sizeof(bool);
            return BitConverter.ToBoolean(bytes, 0);
        }

        public string ReadString()
        {
            int stringSize = ReadUInt16();

            if (_readPosition + stringSize > _buffer.Count)
                throw new IndexOutOfRangeException("Read position is out of range.");

            byte[] bytes = _buffer.GetRange(_readPosition, stringSize).ToArray();
            _readPosition += stringSize;
            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] ToArray() => _buffer.ToArray();
    }
}
