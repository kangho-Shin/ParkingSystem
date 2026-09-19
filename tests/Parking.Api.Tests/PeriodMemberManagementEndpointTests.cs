using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
public sealed class PeriodMemberManagementEndpointTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 등록차량을_추가_조회_사용중지_삭제한다()
    {
        await ClearAsync();
        await using TestApplication factory = new();
        HttpClient client = factory.CreateClient();
        PeriodMemberSaveRequest request = CreateRequest();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/period/members", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        PeriodMemberDetail? created =
            await createResponse.Content.ReadFromJsonAsync<PeriodMemberDetail>();
        Assert.NotNull(created);
        Assert.True(created.MemberId > 0);
        Assert.Equal("12가3456", created.CarNumber1);
        Assert.Equal((short)1, created.UseFlag);

        List<PeriodMemberDetail>? members = await client.GetFromJsonAsync<
            List<PeriodMemberDetail>>(
            "/api/v1/period/members?siteId=1&groupnum=1&carNumber=3456");
        Assert.NotNull(members);
        Assert.Contains(members, member => member.MemberId == created.MemberId);

        request.UseFlag = 0;
        request.Name = "사용중지회원";
        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/period/members/{created.MemberId}", request);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        PeriodMemberDetail? updated = await client.GetFromJsonAsync<PeriodMemberDetail>(
            $"/api/v1/period/members/{created.MemberId}");
        Assert.NotNull(updated);
        Assert.Equal((short)0, updated.UseFlag);
        Assert.Equal("사용중지회원", updated.Name);

        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/v1/period/members/{created.MemberId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage deletedResponse = await client.GetAsync(
            $"/api/v1/period/members/{created.MemberId}");
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    private static PeriodMemberSaveRequest CreateRequest() => new()
    {
        SiteId = 1,
        Groupnum = 1,
        CardId = 100,
        Name = "등록회원",
        CarNumber1 = "12가3456",
        CarType1 = "승용",
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddMonths(1),
        ParkArea = "10000000",
        UseFlag = 1,
        OutFlag = "O"
    };

    private static async Task ClearAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("DELETE FROM tperiodmember;");
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:ParkingDatabase", ConnectionString);
        }
    }
}
