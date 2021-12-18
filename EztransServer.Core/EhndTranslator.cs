using Nanikit.Ehnd;
using System;
using System.Threading.Tasks;

namespace EztransServer.Core.Translator {
  /// <summary>
  /// It provides an EZTransXP japanese to korean translator.
  /// </summary>
  public class EhndTranslator : ITranslator {
    public EhndTranslator(string? dllPath = null) {
      _ehnd = new Ehnd(dllPath);
      _batchEhnd = new BatchEhnd(_ehnd);
    }

    public Task<string> Translate(string source) {
      return _batchEhnd.TranslateAsync(source);
    }

    public void Dispose() {
      _batchEhnd.Dispose();
      GC.SuppressFinalize(this);
    }

    private readonly Ehnd _ehnd;
    private readonly BatchEhnd _batchEhnd;
  }
}
