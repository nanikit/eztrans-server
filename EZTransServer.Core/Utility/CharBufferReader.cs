using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZTransServer.Core.Utility
{
    internal class CharBufferReader
    {
        public int Position { get; private set; }

        private readonly TextReader Base;
        private char[] Buffer;

        public CharBufferReader(TextReader reader, char[]? buffer = null)
        {
            Base = reader;
            Buffer = buffer ?? new char[2048];
        }

        public int TextCopyTo(TextWriter destination, int length)
        {
            int remain = length;
            while (remain > 0)
            {
                int read = Base.ReadBlock(Buffer, 0, Math.Min(remain, Buffer.Length));
                if (read <= 0)
                {
                    break;
                }
                destination.Write(Buffer, 0, read);
                remain -= read;
            }

            int totalRead = length - remain;
            Position += totalRead;
            return totalRead;
        }

        public string? ReadString(int length)
        {
            if (Buffer.Length < length)
            {
                Buffer = new char[length];
            }

            int read = Base.ReadBlock(Buffer, 0, length);
            Position += read;

            return read == length ? new string(Buffer, 0, length) : null;
        }
    }
}
