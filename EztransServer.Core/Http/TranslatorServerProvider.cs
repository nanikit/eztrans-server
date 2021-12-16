using EztransServer.Core.Translator;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EztransServer.Core.Http {
  /// <summary>
  /// It provides a translator server.
  /// </summary>
  public class TranslatorServerProvider : IDisposable {
    private readonly ITranslator _translator;
    private readonly HttpListener _listener = new HttpListener();
    private TaskCompletionSource<bool> _cancellationSource = new TaskCompletionSource<bool>();

    /// <summary>
    /// An event that occurs when a new request is made.
    /// </summary>
    public event Action<IPEndPoint, string?>? OnRequest;

    /// <summary>
    /// Asynchronous task for processing requests.
    /// </summary>
    public Task? Server { get; private set; }

    /// <summary>
    /// Create a new translator server provider instance.
    /// </summary>
    /// <param name="translator">Translator provider to use.</param>
    public TranslatorServerProvider(ITranslator translator) {
      _translator = translator;
    }

    /// <summary>
    /// Start the translator server.
    /// </summary>
    /// <param name="endpoint">Endpoint of server.</param>
    /// <returns>Task result</returns>
    public Task Run(Uri endpoint) {
      _listener.Prefixes.Clear();
      _listener.Prefixes.Add(GetOrigin(endpoint));
      _listener.Start();

      Server = HandleIncomingConnections(endpoint.AbsolutePath);
      return Server;
    }

    /// <summary>
    /// Release the instance.
    /// </summary>
    public void Dispose() {
      _cancellationSource.TrySetResult(false);
    }

    private async Task HandleIncomingConnections(string listenPath) {
      using (_listener) {
        while (await AcceptRequest(listenPath).ConfigureAwait(false)) ;
        _listener.Close();
      }
    }

    private async Task<bool> AcceptRequest(string listenPath) {
      if (IsCancelled()) {
        return false;
      }

      Task<HttpListenerContext> listening = _listener.GetContextAsync();
      await Task.WhenAny(listening, _cancellationSource.Task).ConfigureAwait(false);

      if (IsCancelled()) {
        return false;
      }
      _ = Task.Run(() => SendResponse(listenPath, listening));

      return true;
    }

    private async Task SendResponse(string listenPath, Task<HttpListenerContext> listening) {
      HttpListenerContext ctx = await listening.ConfigureAwait(false);

      HttpListenerRequest req = ctx.Request;
      using HttpListenerResponse resp = ctx.Response;

      if (req.Url?.LocalPath == listenPath) {
        await ProcessTranslation(req, resp).ConfigureAwait(false);
      }
      if (req.Url?.LocalPath == "/favicon.ico") {
        resp.AddHeader("Cache-Control", "Max-Age=99999");
      }
    }

    private bool IsCancelled() {
      var task = _cancellationSource.Task;
      return task.IsCompleted || task.IsCanceled || task.IsFaulted;
    }

    private async Task ProcessTranslation(HttpListenerRequest req, HttpListenerResponse resp) {
      string? originalText = GetTextParam(req);
      OnRequest?.Invoke(req.RemoteEndPoint, originalText);
      if (originalText == null) {
        return;
      }

      string? translatedText = await _translator.Translate(originalText ?? "").ConfigureAwait(false);
      resp.ContentType = "text/plain; charset=utf-8";
      byte[] buf = Encoding.UTF8.GetBytes(translatedText);
      resp.ContentLength64 = buf.LongLength;
      await resp.OutputStream.WriteAsync(buf, 0, buf.Length);
    }

    private readonly static char[] paramDelimiter = new char[] { '=' };

    private static string? GetTextParam(HttpListenerRequest req) {
      if (req.Url?.Query.Length < 1) {
        return null;
      }

      string query = req.Url.Query.Substring(1);
      foreach (string keyVal in query.Split('&')) {
        string[] pair = keyVal.Split(paramDelimiter, 2);
        if (pair.Length > 1 && pair[0] == "text") {
          string unplused = pair[1].Replace('+', ' ');
          string unescaped = Uri.UnescapeDataString(unplused);
          return unescaped;
        }
      }

      return null;
    }

    private static string GetOrigin(Uri endpoint) {
      string url = endpoint.AbsoluteUri;
      int idxAfterPath = url.Length - endpoint.PathAndQuery.Length + 1;
      string origin = url.Substring(0, Math.Max(0, idxAfterPath));
      return origin;
    }
  }
}
