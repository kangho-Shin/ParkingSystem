using System.Net;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager.Tests;

public sealed class ImageServerClientTests
{
    [Fact]
    public async Task 잘못된_과거이미지명은_예외없이_빈사진으로_처리한다()
    {
        HttpClient httpClient = new(new ResponseHandler(HttpStatusCode.BadRequest));
        ImageServerClient client = new(httpClient);

        byte[]? result = await client.DownloadAsync(
            "http://localhost:5400/", "invalid.jpg", CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class ResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public ResponseHandler(HttpStatusCode statusCode) => _statusCode = statusCode;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_statusCode));
    }
}
