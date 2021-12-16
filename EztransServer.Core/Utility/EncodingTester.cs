using System.Text;

namespace EztransServer.Core.Utility
{
    internal class EncodingTester
    {
        private readonly Encoder _encode;
        private readonly char[] _chars = new char[1];
        private readonly byte[] _bytes = new byte[8];

        public EncodingTester(int codepage)
        {
            _encode = TextUtility.GetSOHFallbackEncoding(codepage).GetEncoder();
        }

        public bool IsEncodable(char ch)
        {
            _chars[0] = ch;
            _encode.Convert(_chars, 0, 1, _bytes, 0, 8, false, out _, out _, out _);
            return _bytes[0] != '\x01';
        }
    }
}
