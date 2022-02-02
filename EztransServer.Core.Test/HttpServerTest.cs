using EztransServer.Core.Http;
using EztransServer.Core.Translator;
using System;
using System.Net.Http;
using System.Text;
using Xunit;

namespace EztransServer.Core.Test {
  public class HttpServerTest : IDisposable {
    private readonly TranslationServer _server = new(new EhndTranslator());
    private readonly HttpClient _client = new();
    private readonly string _endpoint = "http://localhost:29292";
    private readonly string _japanese = "だんだん早くなる";
    private readonly string _korean = "점점 빨리 된다";

    public HttpServerTest() {
      _server.Run(new Uri("http://localhost:29292/"));
    }

    public void Dispose() {
      _server.Dispose();
      GC.SuppressFinalize(this);
    }

    [Fact]
    public async void TestGet() {
      var response = await _client.GetAsync($"{_endpoint}?text={_japanese}").ConfigureAwait(false);
      var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
      Assert.Equal(_korean, body);
    }

    [Fact]
    public async void TestPost() {
      var payload = new ReadOnlyMemoryContent(new(Encoding.UTF8.GetBytes(_japanese)));
      var response = await _client.PostAsync($"{_endpoint}", payload).ConfigureAwait(false);
      var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
      Assert.Equal(_korean, body);
    }
  }
}
