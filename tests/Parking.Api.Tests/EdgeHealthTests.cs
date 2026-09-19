using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Parking.Contracts;
using Parking.EdgeGateway;

namespace Parking.Api.Tests;

public sealed class EdgeHealthTests
{
    [Fact]
    public async Task 중앙이_끊겨도_Gateway는_분리된상태를_반환한다()
    {
        ParkingApiClient apiClient = CreateApiClient(
            new ResponseHandler(HttpStatusCode.ServiceUnavailable, ""));
        HealthController controller = new(apiClient);

        ActionResult<GatewayHealthResponse> result =
            await controller.GetAsync(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        GatewayHealthResponse body = Assert.IsType<GatewayHealthResponse>(ok.Value);
        Assert.True(body.GatewayConnected);
        Assert.False(body.CentralConnected);
    }

    [Fact]
    public async Task 중앙응답이_정상이면_두연결은_정상이다()
    {
        ParkingApiClient apiClient = CreateApiClient(
            new ResponseHandler(HttpStatusCode.OK, "Parking Api"));
        HealthController controller = new(apiClient);

        ActionResult<GatewayHealthResponse> result =
            await controller.GetAsync(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        GatewayHealthResponse body = Assert.IsType<GatewayHealthResponse>(ok.Value);
        Assert.True(body.GatewayConnected);
        Assert.True(body.CentralConnected);
    }

    [Fact]
    public async Task 중앙접속예외도_Gateway정상_중앙끊김으로_반환한다()
    {
        ParkingApiClient apiClient = CreateApiClient(new ThrowHandler());
        HealthController controller = new(apiClient);

        ActionResult<GatewayHealthResponse> result =
            await controller.GetAsync(CancellationToken.None);

        GatewayHealthResponse body = Assert.IsType<GatewayHealthResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.True(body.GatewayConnected);
        Assert.False(body.CentralConnected);
    }

    [Fact]
    public async Task EdgeService는_Gateway상태응답을_읽는다()
    {
        const string json = "{\"GatewayConnected\":true,\"CentralConnected\":false}";
        RequestCaptureHandler handler = new(HttpStatusCode.OK, json);
        Parking.EdgeService.GatewayClient client = new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5100/")
        });

        GatewayHealthResponse result = await client.GetHealthAsync(CancellationToken.None);

        Assert.Equal("/health", handler.Path);
        Assert.True(result.GatewayConnected);
        Assert.False(result.CentralConnected);
    }

    [Fact]
    public async Task Gateway연결실패는_EdgeService호출자에게_전달된다()
    {
        Parking.EdgeService.GatewayClient client = new(new HttpClient(new ThrowHandler())
        {
            BaseAddress = new Uri("http://localhost:5100/")
        });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetHealthAsync(CancellationToken.None));
    }

    private static ParkingApiClient CreateApiClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") });

    private sealed class ResponseHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _content;

        public ResponseHandler(HttpStatusCode status, string content)
        {
            _status = status;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_content, Encoding.UTF8, "text/plain")
            });
    }

    private sealed class RequestCaptureHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _content;

        public RequestCaptureHandler(HttpStatusCode status, string content)
        {
            _status = status;
            _content = content;
        }

        public string? Path { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_content, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class ThrowHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("offline");
    }
}
