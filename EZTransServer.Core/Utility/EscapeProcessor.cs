using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EZTransServer.Core.Utility
{
    /// <summary>
    /// Eztrans trims the string, so pre/post process are required to preserve spaces.
    /// </summary>
    /// <remarks>
    /// It can't be a simple text to text function. It will affect translation result
    /// to replace spaces at the end of line with non spaces.
    /// </remarks>
    internal class EscapeProcessor
    {
        enum EscapeKind
        {
            None,
            Symbol,
            Space
        }

        private static readonly string _escaper = "[;:}";
        private static readonly EncodingTester _sjis = new EncodingTester(932);
        private static readonly Regex _regexDecode = new Regex(@"(\r\n)|(\[;:})|[\r\[]|[^\r\[]+", RegexOptions.Compiled);

        /// <summary>
        /// Filter characters which can be modified if repeated.
        /// </summary>
        private static bool IsSequenceMutableSymbol(char c)
        {
            return "─―#\\".Contains(c);
        }

        /// <summary>
        /// Test whether there is a possibility of the single letter falsification 
        /// </summary>
        private static bool IsUnsafeChar(char c)
        {
            return c == '@' // Hdor escape character
              || c == '-' // It may be changed to ―
              || !_sjis.IsEncodable(c);
        }


        private readonly List<string> preserveds = new List<string>();
        private readonly StringBuilder buffer = new StringBuilder();
        private readonly StringBuilder escaping = new StringBuilder();
        private EscapeKind kind = EscapeKind.None;

        public string Escape(string notEscaped)
        {
            buffer.Clear();
            buffer.EnsureCapacity(notEscaped.Length * 3 / 2);

            foreach (char c in notEscaped)
            {
                if (FeedEscape(c))
                {
                    continue;
                }
                else if (IsUnsafeChar(c))
                {
                    SetEscapingKind(EscapeKind.None);
                    preserveds.Add(c.ToString());
                    buffer.Append(_escaper);
                }
                else
                {
                    buffer.Append(c);
                }
            }
            FlushSpaces();

            return buffer.ToString();
        }

        public string Unescape(string escaped)
        {
            buffer.Clear();

            List<string>.Enumerator hydrate = preserveds.GetEnumerator();
            foreach (Match m in _regexDecode.Matches(escaped))
            {
                if (m.Groups[1].Success || m.Groups[2].Success)
                {
                    hydrate.MoveNext();
                    buffer.Append(hydrate.Current);
                }
                else
                {
                    buffer.Append(m.Value);
                }
            }

            return buffer.ToString();
        }

        private bool FeedEscape(char c)
        {
            if (IsSequenceMutableSymbol(c))
            {
                SetEscapingKind(EscapeKind.Symbol);
                escaping.Append(c);
                return true;
            }
            else if (char.IsWhiteSpace(c))
            {
                SetEscapingKind(EscapeKind.Space);
                escaping.Append(c);
                return true;
            }
            else
            {
                FlushSpaces();
                return false;
            }
        }

        private void SetEscapingKind(EscapeKind value)
        {
            if (kind != value)
            {
                FlushSpaces();
                kind = value;
            }
        }

        private void FlushSpaces()
        {
            string space = escaping.ToString();
            escaping.Clear();

            if (space.Length == 0)
            {
                return;
            }
            else if (space.Contains('\n'))
            {
                buffer.Append("\r\n");
                preserveds.Add(space);
            }
            else if (space.Length == 1)
            {
                buffer.Append(space);
            }
            else
            {
                buffer.Append(_escaper);
                preserveds.Add(space);
            }
        }
    }
}
