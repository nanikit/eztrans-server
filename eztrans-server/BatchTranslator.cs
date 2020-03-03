#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace eztrans_server {
  /// <summary>
  /// It merge / split translation works for single threaded translator.
  /// </summary>
  class BatchTranslator : IJp2KrTranslator {

    private class Work {
      public readonly string Text;
      public readonly TaskCompletionSource<string?> Client;

      public Work(string text) {
        Text = text;
        Client = new TaskCompletionSource<string?>();
      }
    }

    private readonly Task Worker;
    private readonly IJp2KrTranslator Translator;
    private readonly ConcurrentBuffer<Work> Works = new ConcurrentBuffer<Work>();
    private readonly CancellationTokenSource Cancellation = new CancellationTokenSource();

    public BatchTranslator(IJp2KrTranslator translator) {
      Translator = translator;
      Worker = ProcessQueue();
    }

    public Task<string?> Translate(string source) {
      var work = new Work(source);
      Works.Enqueue(work);
      return work.Client.Task;
    }

    public async Task ProcessQueue() {
      CancellationToken token = Cancellation.Token;
      while (!token.IsCancellationRequested) {
        List<Work> works = await GetWorksOfThisRound(token).ConfigureAwait(false);

        IEnumerable<string> texts = works.Select(x => x.Text);
        string mergedStart = string.Join("\n", texts);
        System.Diagnostics.Debug.WriteLine($"[[[{mergedStart}]]]");
        string mergedEnd = await Translator.Translate(mergedStart).ConfigureAwait(false) ?? "";

        string[] translateds = SplitBySegmentNewline(mergedEnd, texts).ToArray();

        for (int i = 0; i < works.Count; i++) {
          works[i].Client.TrySetResult(translateds[i]);
        }
      }
    }

    private IEnumerable<string> SplitBySegmentNewline(string merged, IEnumerable<string> texts) {
      int startIdx = 0;
      foreach (string text in texts) {
        int newlineCount = text.Count('\n');
        int endIdx = GetNthNewlineFrom(merged, newlineCount, startIdx);
        yield return merged.Substring(startIdx, endIdx - startIdx);
        startIdx = endIdx + 1;
      }
    }

    private int GetNthNewlineFrom(string merged, int newlineCount, int startIdx) {
      int start = startIdx;
      int idx = merged.Length;
      for (int i = 0; i <= newlineCount; i++) {
        idx = merged.IndexOf('\n', start);
        if (idx == -1) {
          return merged.Length;
        }
        start = idx + 1;
      }
      return idx;
    }

    private async Task<List<Work>> GetWorksOfThisRound(CancellationToken token) {
      var works = new List<Work>();
      do {
        Work work = await Works.ReceiveAsync(token).ConfigureAwait(false);
        works.Add(work);
      }
      while (Works.PendingSize > 0);
      return works;
    }

    public void Dispose() {
      Cancellation.Cancel();
      Cancellation.Dispose();
      Translator.Dispose();
    }
  }
}
