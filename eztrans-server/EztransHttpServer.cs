#nullable enable
using System;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Windows.Input;

namespace eztrans_server {

  class EztransHttpServer : IDisposable {
    private static readonly EztransXp Translator;

    static EztransHttpServer() {
      Task<EztransXp> task = Task.Run(() => EztransXp.Create());
      task.Wait();
      Translator = task.Result;
    }

    public readonly string url;
    public HttpListener Listener;
    public event Action<IPEndPoint, string?> OnRequest = delegate { };
    public Task? Server { get; private set; }
    private CancellationTokenSource CancellationToken = new CancellationTokenSource();

    public EztransHttpServer(string url = "http://localhost:8000/") {
      this.url = url;
      Listener = new HttpListener();
      Listener.Prefixes.Add(url);
    }

    public Task Run() {
      Listener.Start();
      Server = HandleIncomingConnections();
      return Server;
    }

    private async Task HandleIncomingConnections() {
      while (!CancellationToken.IsCancellationRequested) {
        HttpListenerContext ctx = await Listener.GetContextAsync().ConfigureAwait(false);

        HttpListenerRequest req = ctx.Request;
        HttpListenerResponse resp = ctx.Response;

        if (req.Url.LocalPath == "/translate") {
          await ProcessTranslation(req, resp).ConfigureAwait(false);
        }
        if (req.Url.LocalPath == "/favicon.ico") {
          resp.AddHeader("Cache-Control", "Max-Age=99999");
        }

        resp.Close();
      }

      Listener.Close();
    }

    private readonly static char[] pathDelimiter = new char[] { '?' };
    private readonly static char[] paramDelimiter = new char[] { '=' };

    private async Task ProcessTranslation(HttpListenerRequest req, HttpListenerResponse resp) {
      string? japanese = GetTextParam(req);
      OnRequest(req.RemoteEndPoint, japanese);
      if (japanese == null) {
        return;
      }

      string? korean = await Translator.Translate(japanese ?? "").ConfigureAwait(false);

      resp.ContentType = "text/plain; charset=utf-8";
      byte[] buf = Encoding.UTF8.GetBytes(korean);
      resp.ContentLength64 = buf.LongLength;
      await resp.OutputStream.WriteAsync(buf, 0, buf.Length);
    }

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

    public void Dispose() {
      CancellationToken.Cancel();
    }
  }
}
