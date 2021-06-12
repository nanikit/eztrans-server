using EZTransServer.Core.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EZTransServer.Core.Translator
{
    /// <summary>
    /// It merge or split translation works for single threaded translator.
    /// </summary>
    public class BatchTranslator : ITranslator
    {
        private class Work
        {
            public readonly string Text;
            public readonly TaskCompletionSource<string?> Client;

            public Work(string text)
            {
                Text = text;
                Client = new TaskCompletionSource<string?>();
            }
        }

        private readonly Task _worker;
        private readonly ITranslator _translator;
        private readonly ConcurrentBuffer<Work> _works = new ConcurrentBuffer<Work>();
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        /// <summary>
        /// Create a new batch translator instance.
        /// </summary>
        /// <param name="translator">Translator provider to use.</param>
        public BatchTranslator(ITranslator translator)
        {
            _translator = translator;
            _worker = ProcessQueue();
        }

        /// <summary>
        /// Translate the source text.
        /// </summary>
        /// <param name="source">Source text.</param>
        /// <returns>Task result</returns>
        public Task<string?> Translate(string source)
        {
            var work = new Work(source);
            _works.Enqueue(work);
            return work.Client.Task;
        }

        /// <summary>
        /// Process the queue.
        /// </summary>
        /// <returns>Task result</returns>
        public async Task ProcessQueue()
        {
            CancellationToken token = _cancellation.Token;
            while (!token.IsCancellationRequested)
            {
                List<Work> works = await GetWorksOfThisRound(token).ConfigureAwait(false);

                IEnumerable<string> texts = works.Select(x => x.Text);
                string mergedStart = string.Join("\n", texts);
                System.Diagnostics.Debug.WriteLine($"[[[{mergedStart}]]]");
                string mergedEnd = await _translator.Translate(mergedStart).ConfigureAwait(false) ?? "";

                string[] translateds = SplitBySegmentNewline(mergedEnd, texts).ToArray();

                for (int i = 0; i < works.Count; i++)
                {
                    works[i].Client.TrySetResult(translateds[i]);
                }
            }
        }

        /// <summary>
        /// Release the instance.
        /// </summary>
        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            _translator.Dispose();
        }

        private IEnumerable<string> SplitBySegmentNewline(string merged, IEnumerable<string> texts)
        {
            int startIdx = 0;
            foreach (string text in texts)
            {
                int newlineCount = text.Count('\n');
                int endIdx = GetNthNewlineFrom(merged, newlineCount, startIdx);
                yield return merged.Substring(startIdx, endIdx - startIdx);
                startIdx = endIdx + 1;
            }
        }

        private int GetNthNewlineFrom(string merged, int newlineCount, int startIdx)
        {
            int start = startIdx;
            int idx = merged.Length;
            for (int i = 0; i <= newlineCount; i++)
            {
                idx = merged.IndexOf('\n', start);
                if (idx == -1)
                {
                    return merged.Length;
                }
                start = idx + 1;
            }
            return idx;
        }

        private async Task<List<Work>> GetWorksOfThisRound(CancellationToken token)
        {
            var works = new List<Work>();
            do
            {
                Work work = await _works.ReceiveAsync(token).ConfigureAwait(false);
                works.Add(work);
            }
            while (_works.PendingSize > 0);
            return works;
        }
    }
}
