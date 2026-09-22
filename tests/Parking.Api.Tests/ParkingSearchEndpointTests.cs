using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class ParkingSearchEndpointTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 뒤4자리_여러건이면_최신순_후보목록을_반환한다()
    {
        await ClearTablesAsync();
        DateTime now = DateTime.UtcNow;
        long firstId = await CreateSessionAsync("12가3456", now.AddHours(-2), "IN-1.jpg");
        long secondId = await CreateSessionAsync("34나3456", now.AddHours(-1), "IN-2.jpg");
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        string exitAt = Uri.EscapeDataString(new DateTimeOffset(now, TimeSpan.Zero).ToString("O"));

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/parking/search?siteId=9001&groupnum=2&carNumber=3456&exitAt={exitAt}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ParkingSearchResponse? result =
            await response.Content.ReadFromJsonAsync<ParkingSearchResponse>();
        Assert.NotNull(result);
        Assert.Equal([secondId, firstId], result.Candidates.Select(x => x.ParkingSessionId));
    }

    [Fact]
    public async Task 전체번호_한건이면_즉시_요금견적을_반환한다()
    {
        await ClearTablesAsync();
        DateTime now = DateTime.UtcNow;
        long parkingSessionId = await CreateSessionAsync(
            "56다7890",
            now.AddHours(-1),
            "IN-SINGLE.jpg");
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        string exitAt = Uri.EscapeDataString(new DateTimeOffset(now, TimeSpan.Zero).ToString("O"));

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/parking/search?siteId=9001&groupnum=2&carNumber=56다7890&exitAt={exitAt}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(parkingSessionId, result.RootElement.GetProperty("ParkingSessionId").GetInt64());
        Assert.Equal("56다7890", result.RootElement.GetProperty("CarNumber").GetString());
        Assert.True(result.RootElement.TryGetProperty("Fee", out _));
    }

    private static async Task ClearTablesAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            DELETE FROM parking_session_discount;
            DELETE FROM payment;
            DELETE FROM parking_session;
            DELETE FROM parking_event;
            """);
    }

    private static async Task<long> CreateSessionAsync(
        string carNumber,
        DateTime inDateTime,
        string inImage)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indeviceid,
             indate,inimage,outflag)
            VALUES (9001,@EventId,@CarNumber,2,1,9010,4001,@InDateTime,@InImage,'I');
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = Guid.NewGuid().ToByteArray(),
            CarNumber = carNumber,
            InDateTime = inDateTime,
            InImage = inImage
        });
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:ParkingDatabase", ConnectionString);
        }
    }
}
