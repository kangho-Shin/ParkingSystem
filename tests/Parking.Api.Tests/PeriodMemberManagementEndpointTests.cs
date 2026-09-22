using System.Net;
using System.Net.Http.Json;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class PeriodMemberManagementEndpointTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 저장소에서_등록차량을_추가하고_조회한다()
    {
        await ClearAsync();
        PeriodMemberManagementRepository repository = new(ConnectionString);

        long memberId = await repository.CreateAsync(CreateRequest(), CancellationToken.None);
        PeriodMemberDetail? member = await repository.GetAsync(memberId, CancellationToken.None);

        Assert.NotNull(member);
        Assert.Equal("12가3456", member.CarNumber1);
    }

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
        Assert.Equal(1, created.UseFlag);

        List<PeriodMemberDetail>? members = await client.GetFromJsonAsync<
            List<PeriodMemberDetail>>(
            "/api/v1/period/members?siteId=9001&groupnum=2&carNumber=3456");
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
        Assert.Equal(0, updated.UseFlag);
        Assert.Equal("사용중지회원", updated.Name);

        HttpResponseMessage deleteResponse = await client.DeleteAsync(
            $"/api/v1/period/members/{created.MemberId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        PeriodMemberDetail? deleted = await client.GetFromJsonAsync<PeriodMemberDetail>(
            $"/api/v1/period/members/{created.MemberId}");
        Assert.NotNull(deleted);
        Assert.Equal(0, deleted.UseFlag);
    }

    private static PeriodMemberSaveRequest CreateRequest() => new()
    {
        SiteId = 9001,
        Groupnum = 2,
        CardNumber = 100,
        Name = "등록회원",
        CarNumber1 = "12가3456",
        CarType1 = 1,
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddMonths(1),
        ParkArea = "0100000",
        UseFlag = 1,
        OutFlag = "O"
    };

    private static async Task ClearAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("DELETE FROM tperiodinout; DELETE FROM tperiodmember;");
    }

    private sealed class TestApplication : WebApplicationFactory<global::Parking.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:ParkingDatabase", ConnectionString);
        }
    }
}
