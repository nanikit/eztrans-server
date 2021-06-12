using EZTransServer.Core.Translator;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EZTransServer.Core.Http
{
    public class TranslatorServerProvider : IDisposable
    {
        #nullable enable

        public event Action<IPEndPoint, string?>? OnRequest;
        public Task? Server { get; private set; }
        
        #nullable disable

        private readonly ITranslator Translator;
        private readonly HttpListener Listener = new HttpListener();
        private TaskCompletionSource<bool> CancellationSource = new TaskCompletionSource<bool>();

        public TranslatorServerProvider(ITranslator translator)
        {
            Translator = translator;
        }

        public Task Run(Uri endpoint)
        {
            Listener.Prefixes.Clear();
            Listener.Prefixes.Add(GetOrigin(endpoint));
            Listener.Start();

            Server = HandleIncomingConnections(endpoint.AbsolutePath);
            return Server;
        }

        public void Dispose()
        {
            CancellationSource.TrySetResult(false);
        }

        private async Task HandleIncomingConnections(string listenPath)
        {
            using (Listener)
            {
                while (await AcceptRequest(listenPath).ConfigureAwait(false)) ;
                Listener.Close();
            }
        }

        private async Task<bool> AcceptRequest(string listenPath)
        {
            if (IsCancelled())
            {
                return false;
            }
            Task<HttpListenerContext> listening = Listener.GetContextAsync();
            await Task.WhenAny(listening, CancellationSource.Task).ConfigureAwait(false);
            if (IsCancelled())
            {
                return false;
            }
            _ = Task.Run(() => SendResponse(listenPath, listening));

            return true;
        }

        private async Task SendResponse(string listenPath, Task<HttpListenerContext> listening)
        {
            HttpListenerContext ctx = await listening.ConfigureAwait(false);

            HttpListenerRequest req = ctx.Request;
            using HttpListenerResponse resp = ctx.Response;

            if (req.Url.LocalPath == listenPath)
            {
                await ProcessTranslation(req, resp).ConfigureAwait(false);
            }
            if (req.Url.LocalPath == "/favicon.ico")
            {
                resp.AddHeader("Cache-Control", "Max-Age=99999");
            }
        }

        private bool IsCancelled()
        {
            var task = CancellationSource.Task;
            return task.IsCompleted || task.IsCanceled || task.IsFaulted;
        }

        private async Task ProcessTranslation(HttpListenerRequest req, HttpListenerResponse resp)
        {
            string? japanese = GetTextParam(req);
            OnRequest?.Invoke(req.RemoteEndPoint, japanese);
            if (japanese == null)
            {
                return;
            }

            string? korean = await Translator.Translate(japanese ?? "").ConfigureAwait(false);

            resp.ContentType = "text/plain; charset=utf-8";
            byte[] buf = Encoding.UTF8.GetBytes(korean);
            resp.ContentLength64 = buf.LongLength;
            await resp.OutputStream.WriteAsync(buf, 0, buf.Length);
        }

        private readonly static char[] paramDelimiter = new char[] { '=' };

        private static string? GetTextParam(HttpListenerRequest req)
        {
            if (req.Url.Query.Length < 1)
            {
                return null;
            }

            string query = req.Url.Query.Substring(1);
            foreach (string keyVal in query.Split('&'))
            {
                string[] pair = keyVal.Split(paramDelimiter, 2);
                if (pair.Length > 1 && pair[0] == "text")
                {
                    string unplused = pair[1].Replace('+', ' ');
                    string unescaped = Uri.UnescapeDataString(unplused);
                    return unescaped;
                }
            }

            return null;
        }

        private static string GetOrigin(Uri endpoint)
        {
            string url = endpoint.AbsoluteUri;
            int idxAfterPath = url.Length - endpoint.PathAndQuery.Length + 1;
            string origin = url.Substring(0, Math.Max(0, idxAfterPath));
            return origin;
        }
    }
}
