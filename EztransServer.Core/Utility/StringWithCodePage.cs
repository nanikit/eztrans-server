using System.IO;
using System.Text;

namespace EztransServer.Core.Utility {
  internal class StringWithCodePage {
    public string Content { get; set; }
    public Encoding Encoding { get; set; }

    public StringWithCodePage(string content, Encoding encoding) {
      Content = content;
      Encoding = encoding;
    }

    public static bool ReadAllTextAutoDetect(string path, out StringWithCodePage? guessed) {
      string[] encodingNames = new string[] { "utf-8", "shift_jis", "ks_c_5601-1987", "utf-16", "unicodeFFFE" };
      EncoderFallback efall = EncoderFallback.ExceptionFallback;
      DecoderFallback dfall = DecoderFallback.ExceptionFallback;
      foreach (string name in encodingNames) {
        try {
          var enc = Encoding.GetEncoding(name, efall, dfall);
          guessed = new StringWithCodePage(File.ReadAllText(path, enc), enc);
          return true;
        }
        catch (DecoderFallbackException) { }
      }
      guessed = null;
      return false;
    }
  }
}
