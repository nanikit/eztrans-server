using System;
using System.IO;

namespace EztransServer.Core.Utility
{
    internal class CharBufferReader
    {
        public int Position { get; private set; }

        private readonly TextReader _base;
        private char[] _buffer;

        public CharBufferReader(TextReader reader, char[]? buffer = null)
        {
            _base = reader;
            _buffer = buffer ?? new char[2048];
        }

        public int TextCopyTo(TextWriter destination, int length)
        {
            int remain = length;
            while (remain > 0)
            {
                int read = _base.ReadBlock(_buffer, 0, Math.Min(remain, _buffer.Length));
                if (read <= 0)
                {
                    break;
                }
                destination.Write(_buffer, 0, read);
                remain -= read;
            }

            int totalRead = length - remain;
            Position += totalRead;
            return totalRead;
        }

        public string? ReadString(int length)
        {
            if (_buffer.Length < length)
            {
                _buffer = new char[length];
            }

            int read = _base.ReadBlock(_buffer, 0, length);
            Position += read;

            return read == length ? new string(_buffer, 0, length) : null;
        }
    }
}
