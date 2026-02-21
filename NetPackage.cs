using System;
using System.Collections.Generic;
using System.Text;

//网络包体类封装，字节流转换
//int、float、string、bool、byte[]等类型的写入和读取


namespace UnityServer
{
    internal class Packet : IDisposable
    {
        private List<byte> buffer = null;
        private byte[] bufferArray = null;
        private int readPos = 0;
        private bool disposed = false;
        public Packet()
        {
            buffer = new List<byte>();
            readPos = 0;
        }

        public Packet(byte[] data)
        {
            buffer = new List<byte>();
            readPos = 0;
            WriteBytes(data);
            bufferArray = buffer.ToArray();
        }

        public byte[] GetBytesArray()
        {
            bufferArray = buffer.ToArray();
            return bufferArray;
        }
       
        public void WriteBytes(byte[] value)
        {
            buffer.AddRange(value);

        }

        public void WriteInt(int value)
        {
            buffer.AddRange(BitConverter.GetBytes(value));

        }

        public void WriteFloat(float value)
        {
            buffer.AddRange(BitConverter.GetBytes(value));

        }

        public void WriteString(string value)
        {
            WriteInt(value.Length);
            buffer.AddRange(Encoding.ASCII.GetBytes(value));

        }
        public void WriteBoolean(bool value)
        {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        public int ReadInt(bool moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                int value = BitConverter.ToInt32(bufferArray, readPos);
                if (moveReadPos)
                {
                    readPos += 4;
                }
                return value;
            }
            else
            {
                throw new Exception("Could not read value of type 'int'!");
            }
        }

        public float ReadFloat(bool moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                float value = BitConverter.ToSingle(bufferArray, readPos);
                if (moveReadPos)
                {
                    readPos += 4;
                }
                return value;
            }
            else
            {
                throw new Exception("Could not read value of type 'float'!");
            }
        }

        public string ReadString(bool moveReadPos = true)
        {
            int length = ReadInt(true);
            if (buffer.Count > readPos)
            {
                string value = Encoding.ASCII.GetString(bufferArray, readPos, length);
                if (moveReadPos)
                {
                    readPos += length;
                }
                return value;
            }
            else
            {
                throw new Exception("Could not read value of type 'string'!");
            }
        }

        public bool ReadBoolean(bool moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                bool value = BitConverter.ToBoolean(bufferArray, readPos);
                if (moveReadPos)
                {
                    readPos += 1;
                }
                return value;
            }
            else
            {
                throw new Exception("Could not read value of type 'bool'!");
            }
        }

        public byte[] ReadBytes(int length, bool moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                byte[] value = bufferArray.Skip(readPos).Take(length).ToArray();
                if (moveReadPos)
                {
                    readPos += length;
                }
                return value;
            }
            else
            {
                throw new Exception("Could not read value of type 'byte[]'!");
            }
        }

//清除数据，释放资源
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    buffer.Clear();
                    buffer = null;
                    bufferArray = null;
                    readPos = 0;
                }
                disposed = true;
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
