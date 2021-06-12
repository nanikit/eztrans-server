using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EZTransServer.Core.Utility
{
    internal static class TextUtility
    {
        public const string UNICODE_TABLES_FOR_JAPANESE = ""
          + @"\u2E80-\u2EFF" // 한,중,일 부수 보충, ⺀-⻿
          + @"\u3040-\u309F" // 히라가나, ぀-ゟ
          + @"\u30A0-\u30FF" // 가타카나, ゠-ヿ
          + @"\u31F0-\u31FF" // 가타카나 음성 확장, ㇰ-ㇿ
          + @"\u31C0-\u31EF" // CJK Strokes, ㇀-㇯
          + @"\u3200-\u32FF" // Enclosed CJK Letters and Months, ㈀-㋿
          + @"\u3400-\u4DBF\u4E00-\u9FBF\uF900-\uFAFF" // CJK Unified ~, 㐀-䶿一-龿豈-﫿
                                                       //+ @"\uFF64-\uFF9F" // half-width katakana
          + @"\uFF00-\uFF9F" // Full-width alphabet, half-width katakana ,＀-ﾟ
          ;

        public static bool Match(this Regex rx, string input, int index, out Match m)
        {
            m = rx.Match(input, index);
            return m.Success;
        }

        public static int Count(this string str, char ch)
        {
            char[] ar = str.ToCharArray();
            int len = ar.Length;
            int cnt = 0;
            for (int i = 0; i < len; i++)
            {
                if (ar[i] == ch)
                {
                    cnt++;
                }
            }
            return cnt;
        }

        public static int CountLines(string str)
        {
            char[] ar = str.ToCharArray();
            int len = ar.Length;
            int cnt = 0;
            for (int i = 0; i < len; i++)
            {
                char c = ar[i];
                if (c == '\n')
                {
                    cnt++;
                }
                else if (c == '\r')
                {
                    cnt++;
                    int ni = i + 1;
                    if (ni < len && ar[ni] == '\n')
                    {
                        i++;
                    }
                }
            }
            return cnt;
        }

        public static Encoding SOHFallbackUTF8
        {
            get;
            private set;
        }

        static TextUtility()
        {
            SOHFallbackUTF8 = GetSOHFallbackUTF8();
        }

        public static Encoding GetSOHFallbackEncoding(int codepage)
        {
            EncoderFallback efall = new EncoderReplacementFallback("\x01");
            DecoderFallback dfall = new DecoderReplacementFallback("\x01");
            return Encoding.GetEncoding(codepage, efall, dfall);
        }

        private static Encoding GetSOHFallbackUTF8()
        {
            return GetSOHFallbackEncoding(65001);
        }

        public static int SkipUTF8Chars(MemoryStream raw, int chars)
        {
            int readChars = 0;
            while (readChars < chars)
            {
                int headByte = raw.ReadByte();
                if (headByte == -1)
                {
                    break;
                }
                int byteLength = GetUTF8CharByteLength(headByte);

                int readByte = 1;
                while (readByte < byteLength)
                {
                    int tailByte = raw.ReadByte();
                    if (tailByte == -1)
                    {
                        return readChars + 1;
                    }
                    if ((tailByte & 0xC0) != 0x80)
                    {
                        break;
                    }
                    if (readByte == 1)
                    {
                        bool utf8UpperBoundExceeded = headByte == 0xF4 && tailByte > 0x8F;
                        bool isOverlongEncoding4 = headByte == 0xF0 && tailByte < 0x90;
                        if (utf8UpperBoundExceeded || isOverlongEncoding4)
                        {
                            readByte = byteLength;
                            readChars--;
                            break;
                        }
                        bool isOverlongEncoding3 = headByte == 0xE0 && tailByte < 0xA0;
                        bool isUtf16Surrogate = headByte == 0xED && tailByte >= 0xA0;
                        if (isOverlongEncoding3 || isUtf16Surrogate)
                        {
                            readByte = byteLength;
                            break;
                        }
                    }
                    readByte++;
                }

                bool charComplete = readByte == byteLength;
                if (!charComplete)
                {
                    raw.Position--;
                }
                readChars += (charComplete && byteLength == 4) ? 2 : 1;
            }
            return readChars;
        }

        private static int GetUTF8CharByteLength(int headByte)
        {
            // ASCII
            if (headByte < 0x80)
            {
                return 1;
            }
            else if (headByte < 0xC2)
            {
                // Less than 0xC0 means continuation. error, so 1.
                // 0xC1 causes overlapping bit with ASCII range, this is called
                // overlong encoding, and it's not permitted.
                return 1;
            }
            else if (headByte < 0xE0)
            {
                return 2;
            }
            else if (headByte < 0xF0)
            {
                return 3;
            }
            // UTF-8 upper bound is 0x10FFFF, first byte is 0xF4
            else if (headByte < 0xF5)
            {
                return 4;
            }
            else
            {
                // error.
                return 1;
            }
        }
    }
}
