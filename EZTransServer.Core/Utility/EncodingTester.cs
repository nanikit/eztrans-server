using System.Text;

namespace EZTransServer.Core.Utility
{
    internal class EncodingTester
    {
        private readonly Encoder Encode;
        private readonly char[] Chars = new char[1];
        private readonly byte[] Bytes = new byte[8];

        public EncodingTester(int codepage)
        {
            Encode = TextUtility.GetSOHFallbackEncoding(codepage).GetEncoder();
        }

        public bool IsEncodable(char ch)
        {
            Chars[0] = ch;
            Encode.Convert(Chars, 0, 1, Bytes, 0, 8, false, out _, out _, out _);
            return Chars[0] != '\x01';
        }
    }
}
