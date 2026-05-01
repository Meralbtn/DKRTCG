using System;
using System.IO;

namespace CardGameServer
{
    public class NetPackage : IDisposable
    {
        private MemoryStream _stream;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public int Length => (int)_stream.Length;
        public int Position => (int)_stream.Position;

        // ================= 构造 =================

        public NetPackage()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
        }

        public NetPackage(byte[] data)
        {
            _stream = new MemoryStream(data);
            _reader = new BinaryReader(_stream);
        }

        // ================= 写 =================

        public void WriteInt(int value)
        {
            _writer.Write(value);
        }

        public void WriteBytes(byte[] data)
        {
            _writer.Write(data);
        }

        public void WriteString(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            WriteInt(bytes.Length);
            WriteBytes(bytes);
        }

        // ================= 读 =================

        public int ReadInt()
        {
            return _reader.ReadInt32();
        }

        public byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }

        public string ReadString()
        {
            int length = ReadInt();
            var bytes = ReadBytes(length);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        // ================= 工具 =================

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        public void ResetPosition()
        {
            _stream.Position = 0;
        }

        public void Dispose()
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
        }
    }
}