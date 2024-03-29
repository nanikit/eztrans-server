using EztransServer.Core.Translator;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EztransServer.Core.Http {
  /// <summary>
  /// It provides a translator server.
  /// </summary>
  public class TranslationServer : IDisposable {

    private readonly static char[] paramDelimiter = new char[] { '=' };

    private static async Task<string?> GetTextParam(HttpListenerRequest request) {
      switch (request.HttpMethod) {
      case "GET":
        if (request.Url?.Query.Length < 1) {
          return null;
        }

        string query = request.Url!.Query[1..];
        foreach (string keyVal in query.Split('&')) {
          string[] pair = keyVal.Split(paramDelimiter, 2);
          if (pair.Length > 1 && pair[0] == "text") {
            string unplused = pair[1].Replace('+', ' ');
            string unescaped = Uri.UnescapeDataString(unplused);
            return unescaped;
          }
        }
        return null;
      case "POST": {
          if (!request.HasEntityBody) {
            return null;
          }

          using var stream = request.InputStream;
          Encoding encoding = request.ContentEncoding;
          using var reader = new StreamReader(stream, encoding);
          string body = await reader.ReadToEndAsync().ConfigureAwait(false);
          return body;
        }
      }
      return null;
    }

    private static string GetOrigin(Uri endpoint) {
      string url = endpoint.AbsoluteUri;
      int idxAfterPath = url.Length - endpoint.PathAndQuery.Length + 1;
      string origin = url[..Math.Max(0, idxAfterPath)];
      return origin;
    }

    /// <summary>
    /// When receiving http request.
    /// </summary>
    public event Action<IPEndPoint, string?> OnRequest = delegate { };

    /// <summary>
    /// When ITranslator throws.
    /// </summary>
    public event Action<string, Exception> OnException = delegate { };

    /// <summary>
    /// Asynchronous task for processing requests.
    /// </summary>
    public Task? Server { get; private set; }

    /// <summary>
    /// Create a new translator server provider instance.
    /// </summary>
    /// <param name="translator">Translator provider to use.</param>
    public TranslationServer(ITranslator translator) {
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
      GC.SuppressFinalize(this);
    }

    private readonly ITranslator _translator;
    private readonly HttpListener _listener = new();
    private readonly TaskCompletionSource<bool> _cancellationSource = new();

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

      HttpListenerRequest request = ctx.Request;
      using HttpListenerResponse response = ctx.Response;

      if (request.Url?.LocalPath == listenPath) {
        await ProcessTranslation(request, response).ConfigureAwait(false);
      }
      else if (request.Url?.LocalPath == "/favicon.ico") {
        response.AddHeader("Cache-Control", "Max-Age=99999");
      }
    }

    private bool IsCancelled() {
      var task = _cancellationSource.Task;
      return task.IsCompleted || task.IsCanceled || task.IsFaulted;
    }

    private async Task ProcessTranslation(HttpListenerRequest request, HttpListenerResponse response) {
      string? originalText = await GetTextParam(request).ConfigureAwait(false);
      OnRequest(request.RemoteEndPoint, originalText);
      if (originalText == null) {
        return;
      }

      byte[] body;
      try {
        string translated = await _translator.Translate(originalText ?? "").ConfigureAwait(false);
        body = Encoding.UTF8.GetBytes(translated);
      }
      catch (Exception exception) {
        OnException(originalText, exception);
        response.StatusCode = 500;
        body = Encoding.UTF8.GetBytes($"Internal server error: {exception.Message}");
      }

      response.ContentType = "text/plain; charset=utf-8";
      response.ContentLength64 = body.LongLength;
      await response.OutputStream.WriteAsync(body).ConfigureAwait(false);
    }
  }
}
