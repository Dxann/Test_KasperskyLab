using System.Net;
using System.Text;

public class AwesomeFilesApiClientTests
{
    [Fact]
    public async Task ListFilesAsync_ReturnsFiles()
    {
        var handler = new FakeHandler(req =>
        {
            Assert.Equal("/api/files", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[\"a.txt\",\"b.txt\"]", Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var api = new AwesomeFilesApiClient(http);

        var files = await api.ListFilesAsync(CancellationToken.None);
        Assert.Equal(new[] { "a.txt", "b.txt" }, files);
    }

    [Fact]
    public async Task CreateArchiveAsync_ThrowsBackendException_OnBadRequest()
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Отсутствующие файлы", Encoding.UTF8, "text/plain")
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var api = new AwesomeFilesApiClient(http);

        var ex = await Assert.ThrowsAsync<BackendException>(() => api.CreateArchiveAsync(new[] { "x.txt" }, CancellationToken.None));
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Отсутствующие файлы", ex.Message);
    }

    [Fact]
    public async Task GetStatusAsync_ParsesStatus()
    {
        var handler = new FakeHandler(req =>
        {
            Assert.StartsWith("/api/archive/", req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":\"00000000-0000-0000-0000-000000000001\",\"status\":\"Completed\",\"error\":null}", Encoding.UTF8, "application/json")
            };
        });

        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var api = new AwesomeFilesApiClient(http);

        var st = await api.GetStatusAsync("00000000-0000-0000-0000-000000000001", CancellationToken.None);
        Assert.Equal("Completed", st.Status);
        Assert.Equal("Архив создан.", st.ToUserMessage());
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
