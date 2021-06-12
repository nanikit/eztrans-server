using System;
using System.Threading.Tasks;

namespace EZTransServer.Core.Translator
{
    /// <summary>
    /// The interface to provide translator.
    /// </summary>
    public interface ITranslator : IDisposable
    {
        /// <summary>
        /// Translate the source text.
        /// </summary>
        /// <param name="source">Original text.</param>
        /// <returns>Translated text</returns>
        Task<string?> Translate(string source);
    }
}
