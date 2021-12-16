using System.IO;
using System.Text;

namespace EztransServer.Terminal.Utility {
  /// <summary>
  /// Provides functions related to file I/O.
  /// </summary>
  public static class FileManager {
    /// <summary>
    /// Writes a text file in the specified path.
    /// </summary>
    /// <param name="filePath">Path where the text file will be saved.</param>
    /// <param name="text">Text to be writed.</param>
    /// <param name="encoding">The text encoding to use.</param>
    public static void WriteTextFile(string filePath, string text, Encoding encoding) {
      using (Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write)) {
        using (StreamWriter writer = new StreamWriter(stream, encoding)) {
          writer.Write(text);
        }
      }
    }

    /// <summary>
    /// Reads a text file in the specified path.
    /// </summary>
    /// <param name="filePath">Path where the text file will be read.</param>
    /// <param name="encoding">The text encoding to use.</param>
    /// <returns>Text</returns>
    public static string ReadTextFile(string filePath, Encoding encoding) {
      string temp = string.Empty;

      using (StreamReader reader = new StreamReader(filePath, encoding)) {
        temp = reader.ReadToEnd();
      }

      return temp;
    }
  }
}
