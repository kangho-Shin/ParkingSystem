using System.Net;
using System.Text;
using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager.Tests;

public sealed class CentralParkingClientTests
{
    [Fact]
    public async Task 입차조회는_현장키와_검색조건을_전송한다()
    {
        CaptureHandler handler = new("""
            {"Items":[],"Page":1,"PageSize":200,"TotalCount":0}
            """);
        CentralParkingClient client = Client(handler);

        await client.SearchEntriesAsync(
            new ParkingManagementQuery(9001, Groupnum: 2, CarNumber: "1234"),
            CancellationToken.None);

        Assert.NotNull(handler.Request);
        Assert.Equal("site-key", handler.Request.Headers.GetValues("X-Site-Key").Single());
        Assert.Equal(
            "/api/v1/management/parking/entries?siteId=9001&groupnum=2&carNumber=1234&page=1&pageSize=200",
            handler.Request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task 서버오류의_Message를_예외에_전달한다()
    {
        CaptureHandler handler = new("{\"Message\":\"현장키 오류\"}", HttpStatusCode.Unauthorized);
        CentralParkingClient client = Client(handler);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SearchEntriesAsync(new ParkingManagementQuery(9001), CancellationToken.None));

        Assert.Equal("현장키 오류", error.Message);
    }

    [Fact]
    public async Task 연결실패는_중앙연결예외로_변환한다()
    {
        CentralParkingClient client = Client(new ThrowHandler());

        await Assert.ThrowsAsync<ParkingCentralUnavailableException>(() =>
            client.SearchEntriesAsync(new ParkingManagementQuery(9001), CancellationToken.None));
    }

    private static CentralParkingClient Client(HttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://central/") }, "site-key");

    private sealed class CaptureHandler(
        string response,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class ThrowHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("offline");
    }
}
