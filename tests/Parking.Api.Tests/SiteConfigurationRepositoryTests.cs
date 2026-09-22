using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class SiteConfigurationRepositoryTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 버전설정의_MySql일시를_UTC로_조회한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        var configuration = await repository.GetVersionedAsync(9001, CancellationToken.None);

        Assert.NotNull(configuration);
        Assert.Equal(TimeSpan.Zero, configuration.UpdatedAtUtc.Offset);
    }

    [Fact]
    public async Task 신규_설정테이블에서_차로_장비_연결정보를_조회한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        SiteConfiguration? configuration = await repository.GetAsync(
            9001,
            CancellationToken.None);

        Assert.NotNull(configuration);
        Assert.Equal("시험현장", configuration.Site.SiteName);
        Assert.Contains(configuration.Lanes, x => x.LaneId == 9010 && x.Direction == "ENTRY");
        Assert.Contains(configuration.Devices, x => x.DeviceId == 4001 && x.DeviceType == "LPR");
        Assert.Contains(configuration.DeviceLinks!, x =>
            x.SourceDeviceId == 4002 && x.TargetDeviceId == 2001 && x.LinkType == "KIOSK");
    }

    [Fact]
    public async Task 사이트키는_tparkings의_SHA256값으로_검증한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        Assert.True(await repository.ValidateSiteKeyAsync(
            9001,
            "site-9001-key",
            CancellationToken.None));
        Assert.False(await repository.ValidateSiteKeyAsync(
            9001,
            "wrong-key",
            CancellationToken.None));
    }

    [Fact]
    public async Task 존재하지_않는_사이트_수정은_실패한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        bool saved = await repository.SaveSiteAsync(
            new ParkingSite(long.MaxValue, "없는 현장", true),
            CancellationToken.None);

        Assert.False(saved);
    }

    [Fact]
    public async Task 버전설정_저장시_변수별_val_opt_규칙을_보존한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync("""
            UPDATE tparkvariable SET val='1',opt='0'
            WHERE sitenum=9001 AND groupnum=2 AND cmdtype='CMD_WEEKENDUSE';
            """);
        try
        {
            VersionedSiteConfiguration current = await repository.GetVersionedAsync(
                9001, CancellationToken.None)
                ?? throw new InvalidOperationException("현장 설정이 없습니다.");

            bool saved = await repository.SaveVersionedAsync(
                current with { Version = current.Version + 1 },
                CancellationToken.None);

            Assert.True(saved);
            VariableStorageRow optionVariable =
                await connection.QuerySingleAsync<VariableStorageRow>("""
                    SELECT val Value,opt `Option`
                    FROM tparkvariable
                    WHERE sitenum=9001 AND groupnum=2 AND cmdtype='CMD_GRACE_TIME';
                    """);
            VariableStorageRow valueVariable =
                await connection.QuerySingleAsync<VariableStorageRow>("""
                    SELECT val Value,opt `Option`
                    FROM tparkvariable
                    WHERE sitenum=9001 AND groupnum=2 AND cmdtype='CMD_WEEKENDUSE';
                    """);
            Assert.Equal("0", optionVariable.Value);
            Assert.Equal("30", optionVariable.Option);
            Assert.Equal("1", valueVariable.Value);
            Assert.Null(valueVariable.Option);
        }
        finally
        {
            await connection.ExecuteAsync("""
                UPDATE tparkvariable SET val='0',opt='0'
                WHERE sitenum=9001 AND groupnum=2 AND cmdtype='CMD_WEEKENDUSE';
                """);
        }
    }

    private sealed class VariableStorageRow
    {
        public string? Value { get; set; }
        public string? Option { get; set; }
    }
}
