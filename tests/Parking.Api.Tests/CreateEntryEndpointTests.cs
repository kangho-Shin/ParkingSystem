using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

public class CreateEntryEndpointTests
{
    [Fact]
    public async Task 정상_입차요청은_차단기개방결과를_반환한다()
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();

        FieldEventRequest request = new( Guid.NewGuid(), 1, 10,101,"12가3456",DateTimeOffset.UtcNow);

        HttpResponseMessage response = await PostAsync(client, "/api/v1/parking/entries", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        FieldEventResponse? result = JsonConvert.DeserializeObject<FieldEventResponse>(json);

        Assert.NotNull(result);
        Assert.True(result.Accepted);
        Assert.True(result.OpenBarrier);
        Assert.Equal(123, result.ParkingSessionId);
    }

    [Fact]
    public async Task EventId가_비어있으면_입차요청을_거부한다()
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();

        FieldEventRequest request = new(
            Guid.Empty,
            1,
            10,
            101,
            "12가3456",
            DateTimeOffset.UtcNow);

        HttpResponseMessage response =
            await PostAsync(client, "/api/v1/parking/entries", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 10, 101)]
    [InlineData(1, 0, 101)]
    [InlineData(1, 10, 0)]
    public async Task 장비식별번호가_유효하지_않으면_입차요청을_거부한다(long siteId, long laneId, long deviceId)
    {
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();

        FieldEventRequest request = new(
            Guid.NewGuid(),
            siteId,
            laneId,
            deviceId,
            "12가3456",
            DateTimeOffset.UtcNow);

        HttpResponseMessage response =
            await PostAsync(client, "/api/v1/parking/entries", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string uri, object value)
    {
        string json = JsonConvert.SerializeObject(value);
        return client.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
                services.AddSingleton<IParkingEventRepository>(
                    new FakeParkingEventRepository()));
        }
    }

    private sealed class FakeParkingEventRepository : IParkingEventRepository
    {
        public Task<FieldEventResponse> SaveEntryAsync(
            FieldEventRequest request,
            CancellationToken cancellationToken)
        {
            FieldEventResponse response = new(
                request.EventId,
                true,
                123,
                "ENTRY_ACCEPTED",
                "입차되었습니다.",
                true);

            return Task.FromResult(response);
        }
    }
}