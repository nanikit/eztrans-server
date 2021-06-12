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
        /// <param name="source">original text</param>
        /// <returns>translated text</returns>
        Task<string?> Translate(string source);
    }
}
