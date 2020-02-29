#nullable enable
using System;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eztrans_server {

  public class TranslationHttpServer : IDisposable {
    public event Action<IPEndPoint, string?>? OnRequest;
    public Task? Server { get; private set; }

    private readonly IJp2KrTranslator Translator;
    private readonly HttpListener Listener = new HttpListener();
    private readonly CancellationTokenSource CancellationToken = new CancellationTokenSource();

    public TranslationHttpServer(IJp2KrTranslator translator) {
      Translator = translator;
    }

    public Task Run(Uri endpoint) {
      Listener.Prefixes.Clear();
      Listener.Prefixes.Add(GetOrigin(endpoint));
      Listener.Start();

      Server = HandleIncomingConnections(endpoint.AbsolutePath);
      return Server;
    }

    public void Dispose() {
      if (CancellationToken.IsCancellationRequested) {
        return;
      }
      CancellationToken.Cancel();
      Listener.Abort();
    }

    private async Task HandleIncomingConnections(string listenPath) {
      while (!CancellationToken.IsCancellationRequested) {
        HttpListenerContext ctx = await Listener.GetContextAsync().ConfigureAwait(false);

        HttpListenerRequest req = ctx.Request;
        HttpListenerResponse resp = ctx.Response;

        if (req.Url.LocalPath == listenPath) {
          await ProcessTranslation(req, resp).ConfigureAwait(false);
        }
        if (req.Url.LocalPath == "/favicon.ico") {
          resp.AddHeader("Cache-Control", "Max-Age=99999");
        }

        resp.Close();
      }

      Listener.Close();
    }

    private async Task ProcessTranslation(HttpListenerRequest req, HttpListenerResponse resp) {
      string? japanese = GetTextParam(req);
      OnRequest?.Invoke(req.RemoteEndPoint, japanese);
      if (japanese == null) {
        return;
      }

      string? korean = await Translator.Translate(japanese ?? "").ConfigureAwait(false);

      resp.ContentType = "text/plain; charset=utf-8";
      byte[] buf = Encoding.UTF8.GetBytes(korean);
      resp.ContentLength64 = buf.LongLength;
      await resp.OutputStream.WriteAsync(buf, 0, buf.Length);
    }

    private readonly static char[] pathDelimiter = new char[] { '?' };
    private readonly static char[] paramDelimiter = new char[] { '=' };

    private static string? GetTextParam(HttpListenerRequest req) {
      string[] parts = req.Url.ToString().Split(pathDelimiter, 2);
      if (parts.Length < 2) {
        return null;
      }

      foreach (string part in parts[1].Split('&')) {
        string[] pair = part.Split(paramDelimiter, 2);
        if (pair.Length > 1 && pair[0] == "text") {
          return pair[1];
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
