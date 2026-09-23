# Edge Vehicle Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Parking.EdgeManager에서 중앙 DB의 현재 입차차량과 정산·출차 이력을 조회하고 수동입차 및 입차차량 번호변경을 처리한다.

**Architecture:** EdgeManager의 기존 `EdgeManagementClient`는 로컬 EdgeService 모니터링을 유지하고 새 `CentralParkingClient`가 Parking.Api 관리 API를 직접 호출한다. Parking.Api는 MySQL의 `tparkinfo`와 `tperiodinout`을 통합 조회하며 모든 관리 요청에 현장 키를 검증한다.

**Tech Stack:** .NET 9, ASP.NET Core Controllers, Dapper, MySqlConnector, Newtonsoft.Json, WinForms, xUnit

**Spec:** `docs/superpowers/specs/2026-09-23-edge-vehicle-management-design.md`

## Global Constraints

- 작업 브랜치는 `codex/server-edge-foundation`이다.
- WinForms는 반드시 `.cs`, `.Designer.cs`, `.resx` 구조로 작성한다.
- JSON은 Newtonsoft.Json과 PascalCase 계약을 유지한다.
- 중앙 DB 테스트는 `PARKING_RUNTIME_CONNECTION`이 `parking000test`를 가리킬 때만 실행한다.
- 일반차량은 `tparkinfo`, 등록차량은 `tperiodinout`을 사용한다.
- 차량번호 변경은 `outflag='I'`인 차량만 허용한다.
- 출차 조회 기간은 최대 31일, 페이지 크기는 기본 200건·최대 500건이다.
- 기존 EdgeManager 실시간 모니터링과 설정 화면 동작을 변경하지 않는다.

## Review Focus

- 일반·등록차량의 `xindex`가 같아도 `SessionType`으로 정확히 구분하여 수정해야 한다. Task 2 저장소 테스트에서 같은 번호를 동시에 만든다.
- 뒤 4자리 차량번호 검색과 전체 차량번호 검색이 서로 다른 조건으로 동작해야 한다. Task 2 조회 테스트에서 둘 다 검증한다.
- 조회 도중 중앙 연결이 끊겨도 JIT 예외창 없이 화면에 오류를 표시하고 다시 조회할 수 있어야 한다. Task 5 클라이언트와 Task 6 화면 정책 테스트에서 검증한다.
- 수동입차 요청의 차로·장치·방향·현장 조합이 틀리면 DB를 변경하지 않아야 한다. Task 4 컨트롤러 테스트에서 검증한다.
- 페이지 사이에서 정렬 순서가 흔들리지 않도록 처리시각과 `SessionType`, `xindex`를 고정 정렬해야 한다. Task 2 페이지 테스트에서 검증한다.

---

### Task 1: 차량 관리 공용 계약과 조회 정책

**Files:**
- Create: `src/BuildingBlocks/Parking.Contracts/ParkingManagementModels.cs`
- Create: `src/Edge/Parking.EdgeManager.Core/ParkingManagementQueryPolicy.cs`
- Create: `tests/Parking.EdgeManager.Tests/ParkingManagementQueryPolicyTests.cs`

**Interfaces:**
- Consumes: 기존 `ParkingDevice`, `ParkingLane` 설정 모델
- Produces: `ParkingSessionType`, `ParkingManagementQuery`, `ParkingManagementItem`, `PagedParkingResult<T>`, `ManualEntryRequest`, `ManualEntryResponse`, `CentralConnectionResponse`, `ParkingManagementQueryPolicy`

- [ ] **Step 1: 조회 정책 실패 테스트 작성**

```csharp
public sealed class ParkingManagementQueryPolicyTests
{
    [Fact]
    public void 출차조회는_31일을_초과할수없다()
    {
        DateTimeOffset from = new(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(9));
        DateTimeOffset to = from.AddDays(32);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => ParkingManagementQueryPolicy.ValidateExitRange(from, to));

        Assert.Equal("to", error.ParamName);
    }

    [Theory]
    [InlineData(0, 200)]
    [InlineData(600, 500)]
    [InlineData(25, 25)]
    public void 페이지크기를_1에서_500사이로_정규화한다(int value, int expected) =>
        Assert.Equal(expected, ParkingManagementQueryPolicy.NormalizePageSize(value));
}
```

- [ ] **Step 2: 정책 테스트가 누락된 타입 때문에 실패하는지 확인**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj --filter FullyQualifiedName~ParkingManagementQueryPolicyTests`

Expected: FAIL with `ParkingManagementQueryPolicy` 또는 계약 타입을 찾을 수 없다는 컴파일 오류

- [ ] **Step 3: 공용 계약 작성**

```csharp
namespace Parking.Contracts;

public enum ParkingSessionType { General = 1, Period = 2 }

public sealed record ParkingManagementQuery(
    long SiteId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int? Groupnum = null,
    long? DeviceId = null,
    string? CarNumber = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 200);

public sealed record ParkingManagementItem(
    ParkingSessionType SessionType,
    long ParkingSessionId,
    long SiteId,
    int Groupnum,
    string CarNumber,
    int CarType,
    string Status,
    long InLaneId,
    int InDeviceNumber,
    string InDeviceName,
    DateTimeOffset InDateTime,
    DateTimeOffset? PaidAt,
    long? OutLaneId,
    int? OutDeviceNumber,
    string? ProcessDeviceName,
    DateTimeOffset? OutDateTime,
    int ParkingMinutes,
    int? OriginalFee,
    int? DiscountFee,
    int? PaidFee,
    bool IsManual,
    string? InImage,
    string? OutImage);

public sealed record PagedParkingResult<T>(
    IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);

public sealed record ManualEntryRequest(
    long SiteId, int Groupnum, long LaneId, long DeviceId,
    string CarNumber, DateTimeOffset InDateTime, int CarType);

public sealed record ManualEntryResponse(
    ParkingSessionType SessionType, long? ParkingSessionId,
    bool Accepted, string ResultCode, string Message);

public sealed record ManagementCarNumberRequest(long SiteId, string CarNumber);

public sealed record CentralConnectionResponse(
    long SiteId, string CentralServerUrl, string SiteAuthKey);
```

- [ ] **Step 4: 조회 정책 최소 구현**

```csharp
namespace Parking.EdgeManager.Core;

public static class ParkingManagementQueryPolicy
{
    public static int NormalizePage(int page) => Math.Max(page, 1);
    public static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? 200 : Math.Min(pageSize, 500);

    public static void ValidateExitRange(DateTimeOffset from, DateTimeOffset to)
    {
        if (to < from) throw new ArgumentOutOfRangeException(nameof(to));
        if (to - from > TimeSpan.FromDays(31))
            throw new ArgumentOutOfRangeException(nameof(to));
    }
}
```

- [ ] **Step 5: 정책 테스트 실행**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj --filter FullyQualifiedName~ParkingManagementQueryPolicyTests`

Expected: PASS

- [ ] **Step 6: 커밋**

```bash
git add src/BuildingBlocks/Parking.Contracts/ParkingManagementModels.cs src/Edge/Parking.EdgeManager.Core/ParkingManagementQueryPolicy.cs tests/Parking.EdgeManager.Tests/ParkingManagementQueryPolicyTests.cs
git commit -m "feat: add parking management contracts"
```

### Task 2: 일반·등록차량 통합 조회 및 번호변경 저장소

**Files:**
- Create: `src/Central/Parking.Central.Data/IParkingManagementRepository.cs`
- Create: `src/Central/Parking.Central.Data/ParkingManagementRepository.cs`
- Create: `tests/Parking.Api.Tests/ParkingManagementRepositoryTests.cs`

**Interfaces:**
- Consumes: Task 1의 `ParkingManagementQuery`, `ParkingManagementItem`, `PagedParkingResult<T>`, `ParkingSessionType`
- Produces: `IParkingManagementRepository.SearchEntriesAsync`, `SearchExitsAsync`, `CorrectCarNumberAsync`

- [ ] **Step 1: DB 테스트 픽스처와 실패 테스트 작성**

테스트 현장 `990101`, 차로 `991010/991020`, 장치 `994011/994012`를 사용하고 각 테스트의 `finally`에서 `tbcardinfo → tperiodinout → tperiodmember → tparkinfo → tdeviceinfo → tlaneinfo → tparkings` 순으로 삭제한다.

```csharp
[Fact]
public async Task 입차조회는_일반과_등록의_I만_합친다()
{
    await SeedAsync();
    try
    {
        PagedParkingResult<ParkingManagementItem> result =
            await _repository.SearchEntriesAsync(
                new ParkingManagementQuery(SiteId, CarNumber: "1234"),
                CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.SessionType == ParkingSessionType.General);
        Assert.Contains(result.Items, x => x.SessionType == ParkingSessionType.Period);
        Assert.All(result.Items, x => Assert.Equal("I", x.Status));
    }
    finally { await ClearAsync(); }
}

[Fact]
public async Task 같은xindex라도_세션구분에_따라_I차량번호만_수정한다()
{
    await SeedSameSessionIdsAsync();
    try
    {
        bool changed = await _repository.CorrectCarNumberAsync(
            SiteId, ParkingSessionType.Period, SharedId, "99가9999",
            CancellationToken.None);

        Assert.True(changed);
        Assert.Equal("11가1111", await GeneralCarNumberAsync(SharedId));
        Assert.Equal("99가9999", await PeriodCarNumberAsync(SharedId));
    }
    finally { await ClearAsync(); }
}
```

같은 파일에 다음 테스트를 추가한다.

- 전체번호는 완전일치, 4자리는 `RIGHT(carnum,4)`로 검색한다.
- 출차 조회는 일반 `X/O`와 등록 `O`만 반환한다.
- `X`는 `paydate`와 최신 `tbcardinfo.devicenum`, `O`는 `outdate/outdevicenum`을 사용한다.
- 등록차량 금액은 `null`이다.
- `(처리시각 DESC, SessionType, ParkingSessionId DESC)` 정렬이 페이지 간 유지된다.
- `X/O` 차량번호 변경은 `false`를 반환한다.

- [ ] **Step 2: 저장소 테스트가 타입 누락으로 실패하는지 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~ParkingManagementRepositoryTests`

Expected: FAIL with `IParkingManagementRepository` 또는 `ParkingManagementRepository`를 찾을 수 없음

- [ ] **Step 3: 저장소 인터페이스 작성**

```csharp
public interface IParkingManagementRepository
{
    Task<PagedParkingResult<ParkingManagementItem>> SearchEntriesAsync(
        ParkingManagementQuery query, CancellationToken cancellationToken);
    Task<PagedParkingResult<ParkingManagementItem>> SearchExitsAsync(
        ParkingManagementQuery query, CancellationToken cancellationToken);
    Task<bool> CorrectCarNumberAsync(
        long siteId, ParkingSessionType sessionType, long parkingSessionId,
        string carNumber, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: 통합 조회 SQL 구현**

입차 쿼리는 일반·등록 서브쿼리를 `UNION ALL`하고 외부 쿼리에서 공통 조건, 정렬, `LIMIT @PageSize OFFSET @Offset`을 적용한다. 장치는 다음 조건으로 결합한다.

```sql
LEFT JOIN tdeviceinfo d
  ON d.sitenum=p.sitenum
 AND d.groupnum=p.groupnum
 AND d.devicenum=p.indevicenum
```

차량번호 조건은 정규화된 길이에 따라 한 번만 선택한다.

```csharp
string carCondition = string.IsNullOrWhiteSpace(query.CarNumber) ? ""
    : query.CarNumber.Trim().Length == 4
        ? " AND RIGHT(p.carnum,4)=@CarNumber"
        : " AND p.carnum=@CarNumber";
```

출차 쿼리는 일반 `X/O`와 등록 `O`를 합치고 일반 `X`의 처리 장치는 최신 승인 결제행에서 구한다.

```sql
LEFT JOIN tbcardinfo pay
  ON pay.xindex=(SELECT MAX(c2.xindex) FROM tbcardinfo c2 WHERE c2.pindex=p.xindex)
```

- [ ] **Step 5: 세션 구분별 번호변경 구현**

```csharp
string table = sessionType == ParkingSessionType.General
    ? "tparkinfo" : "tperiodinout";
string sql = $"UPDATE {table} SET carnum=@CarNumber " +
             "WHERE xindex=@ParkingSessionId AND sitenum=@SiteId AND outflag='I';";
```

`table` 값은 enum 분기에서만 결정하고 요청 문자열을 SQL에 넣지 않는다.

- [ ] **Step 6: 저장소 테스트 실행**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~ParkingManagementRepositoryTests`

Expected: PASS

- [ ] **Step 7: 커밋**

```bash
git add src/Central/Parking.Central.Data/IParkingManagementRepository.cs src/Central/Parking.Central.Data/ParkingManagementRepository.cs tests/Parking.Api.Tests/ParkingManagementRepositoryTests.cs
git commit -m "feat: query central parking management records"
```

### Task 3: 수동입차 표시와 차종 전달

**Files:**
- Modify: `src/BuildingBlocks/Parking.Contracts/FieldEventRequest.cs`
- Modify: `src/Central/Parking.Central.Data/ParkingEventRepository.cs`
- Modify: `src/Central/Parking.Central.Data/PeriodVehicleRepository.cs`
- Create: `src/Central/Parking.Api/Features/Management/ManualEntryHandler.cs`
- Create: `tests/Parking.Api.Tests/ManualEntryHandlerTests.cs`
- Modify: `tests/Parking.Api.Tests/PeriodVehicleRepositoryTests.cs`

**Interfaces:**
- Consumes: 기존 `CreateEntryHandler`의 일반·등록 판별 방식, Task 1의 `ManualEntryRequest/Response`
- Produces: `FieldEventRequest.CarType`, `FieldEventRequest.IsManual`, `ManualEntryHandler.HandleAsync`

- [ ] **Step 1: 일반·등록 수동입차 실패 테스트 작성**

```csharp
[Fact]
public async Task 일반수동입차는_선택차종과_manual을_저장한다()
{
    ManualEntryResponse response = await _handler.HandleAsync(
        new ManualEntryRequest(SiteId, 2, EntryLaneId, EntryDeviceId,
            "31가3100", DateTimeOffset.Now, 3), CancellationToken.None);

    ParkingStoredRow row = await ReadGeneralAsync(response.ParkingSessionId!.Value);
    Assert.Equal(3, row.CarType);
    Assert.Equal(1, row.Manual);
    Assert.Equal(ParkingSessionType.General, response.SessionType);
}
```

등록차량 테스트는 유효한 `tperiodmember`를 준비하고 `tperiodinout.manual=1`, 회원의 차종 사용, `SessionType.Period`를 검증한다.

- [ ] **Step 2: 테스트가 새 요청 속성과 핸들러 누락으로 실패하는지 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~ManualEntryHandlerTests|FullyQualifiedName~PeriodVehicleRepositoryTests"`

Expected: FAIL with `ManualEntryHandler`, `CarType` 또는 `IsManual` 누락

- [ ] **Step 3: FieldEventRequest 하위 호환 확장**

기존 호출을 깨지 않도록 마지막 선택 인자로 추가한다.

```csharp
public sealed record FieldEventRequest(
    [property: Newtonsoft.Json.JsonConverter(typeof(GuidNJsonConverter))]
    Guid EventId, long SiteId, long LaneId, long DeviceId,
    string CarNumber, DateTimeOffset InDateTime,
    int Groupnum = 1, string EventType = ParkingEventType.Entry,
    string? InImage = null, int CarType = 1, bool IsManual = false);
```

- [ ] **Step 4: 두 저장소 INSERT에 차종과 수동값 적용**

일반차량 INSERT는 `cartype=@CarType, manual=@Manual`, 등록차량 INSERT는 `cartype=@MemberCarType, manual=@Manual`을 사용한다.

```csharp
CarType = request.CarType,
Manual = request.IsManual ? 1 : 0
```

- [ ] **Step 5: ManualEntryHandler 구현**

```csharp
public async Task<ManualEntryResponse> HandleAsync(
    ManualEntryRequest request, CancellationToken token)
{
    FieldEventRequest entry = new(
        Guid.NewGuid(), request.SiteId, request.LaneId, request.DeviceId,
        request.CarNumber.Trim(), request.InDateTime, request.Groupnum,
        ParkingEventType.Entry, null, request.CarType, true);
    PeriodMember? member = await _periodRepository.FindMemberAsync(
        request.SiteId, request.Groupnum, request.CarNumber,
        request.InDateTime, token);
    ParkingSessionType type = member is null
        ? ParkingSessionType.General : ParkingSessionType.Period;
    FieldEventResponse result = member is null
        ? await _repository.SaveEntryAsync(entry, token)
        : await _periodRepository.SaveEntryAsync(entry, member, token);
    return new(type, result.ParkingSessionId, result.Accepted,
        result.ResultCode, result.DisplayMessage);
}
```

- [ ] **Step 6: 수동입차 및 기존 입차 회귀 테스트 실행**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~ManualEntryHandlerTests|FullyQualifiedName~CreateEntry|FullyQualifiedName~PeriodVehicleRepositoryTests"`

Expected: PASS

- [ ] **Step 7: 커밋**

```bash
git add src/BuildingBlocks/Parking.Contracts/FieldEventRequest.cs src/Central/Parking.Central.Data/ParkingEventRepository.cs src/Central/Parking.Central.Data/PeriodVehicleRepository.cs src/Central/Parking.Api/Features/Management/ManualEntryHandler.cs tests/Parking.Api.Tests/ManualEntryHandlerTests.cs tests/Parking.Api.Tests/PeriodVehicleRepositoryTests.cs
git commit -m "feat: support operator manual entry"
```

### Task 4: 인증된 중앙 차량 관리 API

**Files:**
- Create: `src/Central/Parking.Api/Features/Management/ParkingManagementController.cs`
- Modify: `src/Central/Parking.Api/Program.cs`
- Modify: `src/Central/Parking.Central.Data/ParkingCorrectionRepository.cs`
- Create: `tests/Parking.Api.Tests/ParkingManagementControllerTests.cs`
- Create: `tests/Parking.Api.Tests/ParkingCorrectionRepositoryTests.cs`

**Interfaces:**
- Consumes: Tasks 1~3의 관리 계약, 저장소, `ManualEntryHandler`; 기존 `ISiteConfigurationRepository.ValidateSiteKeyAsync`
- Produces: 4개의 `/api/v1/management/parking` 엔드포인트

- [ ] **Step 1: 인증·검증 실패 테스트 작성**

```csharp
[Fact]
public async Task 잘못된_현장키는_입차조회를_거부한다()
{
    _controller.ControllerContext = ContextWithSiteKey("wrong-key");
    IActionResult result = await _controller.GetEntriesAsync(
        SiteId, null, null, null, null, null, 1, 200,
        CancellationToken.None);
    Assert.IsType<UnauthorizedResult>(result);
}

[Fact]
public async Task 수동입차는_입차방향_장치가_아니면_400이다()
{
    _controller.ControllerContext = ContextWithSiteKey(ValidKey);
    IActionResult result = await _controller.CreateManualEntryAsync(
        new ManualEntryRequest(SiteId, 2, ExitLaneId, ExitDeviceId,
            "12가1234", DateTimeOffset.Now, 1), CancellationToken.None);
    Assert.IsType<BadRequestObjectResult>(result);
    Assert.Equal(0, await CountParkingRowsAsync("12가1234"));
}
```

같은 테스트 파일에서 기간 역전, 31일 초과, 페이지 크기 501, 차종 0/4, 빈 차량번호, 다른 현장 세션 번호변경을 검증한다.

- [ ] **Step 2: 컨트롤러 테스트 실패 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~ParkingManagementControllerTests`

Expected: FAIL with `ParkingManagementController` 누락

- [ ] **Step 3: 컨트롤러와 인증 공통 메서드 구현**

```csharp
[ApiController]
[Route("api/v1/management/parking")]
public sealed class ParkingManagementController : ControllerBase
{
    private Task<bool> IsAuthorizedAsync(long siteId, CancellationToken token) =>
        _siteRepository.ValidateSiteKeyAsync(
            siteId, Request.Headers["X-Site-Key"].ToString(), token);
}
```

엔드포인트:

```text
GET  entries
GET  exits
POST manual-entries
PUT  sessions/{sessionType}/{parkingSessionId}/car-number
```

조회는 `Ok(PagedParkingResult<ParkingManagementItem>)`, 번호변경 성공은 `200`, 상태 불일치 또는 대상 없음은 `404`를 반환한다.

- [ ] **Step 4: DI 등록 및 기존 번호변경 상태 제한**

```csharp
builder.Services.AddScoped<IParkingManagementRepository>(
    _ => new ParkingManagementRepository(connectionString));
builder.Services.AddScoped<ManualEntryHandler>();
```

기존 `ParkingCorrectionRepository` SQL의 조건을 `outflag<>'O'`에서 `outflag='I'`로 변경한다. `ParkingCorrectionRepositoryTests`에 같은 현장의 `I/X/O` 행을 만들고 `I`만 변경되는 DB 테스트를 추가한다.

- [ ] **Step 5: API 테스트 실행**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~ParkingManagementControllerTests|FullyQualifiedName~ParkingCorrection"`

Expected: PASS

- [ ] **Step 6: 커밋**

```bash
git add src/Central/Parking.Api/Features/Management/ParkingManagementController.cs src/Central/Parking.Api/Program.cs src/Central/Parking.Central.Data/ParkingCorrectionRepository.cs tests/Parking.Api.Tests/ParkingManagementControllerTests.cs tests/Parking.Api.Tests/ParkingCorrectionRepositoryTests.cs
git commit -m "feat: expose authenticated parking management api"
```

### Task 5: Edge 중앙 연결정보와 CentralParkingClient

**Files:**
- Modify: `src/Edge/Parking.EdgeService/LocalConfigurationController.cs`
- Modify: `src/Edge/Parking.EdgeManager.Core/IEdgeManagementClient.cs`
- Modify: `src/Edge/Parking.EdgeManager.Core/EdgeManagementClient.cs`
- Create: `src/Edge/Parking.EdgeManager.Core/ICentralParkingClient.cs`
- Create: `src/Edge/Parking.EdgeManager.Core/CentralParkingClient.cs`
- Create: `src/Edge/Parking.EdgeManager.Core/ParkingCentralUnavailableException.cs`
- Create: `tests/Parking.EdgeManager.Tests/CentralParkingClientTests.cs`
- Modify: `tests/Parking.EdgeManager.Tests/EdgeManagerPresenterTests.cs`
- Modify: `tests/Parking.Api.Tests/EdgeManagementEndpointTests.cs`

**Interfaces:**
- Consumes: Task 1의 중앙 연결·차량 관리 계약과 Task 4 API
- Produces: `IEdgeManagementClient.GetCentralConnectionAsync`, `ICentralParkingClient`

- [ ] **Step 1: HTTP 계약 실패 테스트 작성**

```csharp
[Fact]
public async Task 입차조회는_현장키와_검색조건을_전송한다()
{
    CaptureHandler handler = new("""
        {"Items":[],"Page":1,"PageSize":200,"TotalCount":0}
        """);
    CentralParkingClient client = new(new HttpClient(handler)
    {
        BaseAddress = new Uri("http://central/")
    }, "site-key");

    await client.SearchEntriesAsync(
        new ParkingManagementQuery(9001, Groupnum: 2, CarNumber: "1234"),
        CancellationToken.None);

    Assert.Equal("site-key", handler.Request!.Headers.GetValues("X-Site-Key").Single());
    Assert.Equal("/api/v1/management/parking/entries?siteId=9001&groupnum=2&carNumber=1234&page=1&pageSize=200",
        handler.Request.RequestUri!.PathAndQuery);
}
```

추가 테스트:

- 출차 날짜는 ISO-8601로 전송한다.
- 수동입차는 PascalCase JSON을 전송한다.
- 번호변경 URL에 `General/Period`와 세션번호가 들어간다.
- HTTP 오류 응답의 `Message`를 예외 메시지로 전달한다.

- [ ] **Step 2: 클라이언트 테스트 실패 확인**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj --filter FullyQualifiedName~CentralParkingClientTests`

Expected: FAIL with `CentralParkingClient` 누락

- [ ] **Step 3: 로컬 중앙 연결정보 엔드포인트 구현**

```csharp
[HttpGet("central-connection")]
public async Task<IActionResult> GetCentralConnectionAsync(CancellationToken token)
{
    if (HttpContext.Connection.RemoteIpAddress is System.Net.IPAddress remote &&
        !System.Net.IPAddress.IsLoopback(remote))
        return Forbid();
    EdgeBootstrapSettings? value = await _bootstrap.GetAsync(token);
    return value is null ? NotFound() : Ok(new CentralConnectionResponse(
        value.SiteId, value.CentralServerUrl, value.SiteAuthKey));
}
```

`EdgeManagementEndpointTests`에서 loopback 주소는 연결정보를 받고 외부 주소는 `ForbidResult`를 받는지 검증한다.

`IEdgeManagementClient`와 구현에 다음 메서드를 추가한다.

```csharp
Task<CentralConnectionResponse?> GetCentralConnectionAsync(CancellationToken token);
```

- [ ] **Step 4: 중앙 클라이언트 인터페이스와 구현 작성**

```csharp
public interface ICentralParkingClient
{
    Task<PagedParkingResult<ParkingManagementItem>> SearchEntriesAsync(
        ParkingManagementQuery query, CancellationToken token);
    Task<PagedParkingResult<ParkingManagementItem>> SearchExitsAsync(
        ParkingManagementQuery query, CancellationToken token);
    Task<ManualEntryResponse> CreateManualEntryAsync(
        ManualEntryRequest request, CancellationToken token);
    Task CorrectCarNumberAsync(
        ParkingSessionType type, long parkingSessionId,
        ManagementCarNumberRequest request, CancellationToken token);
}
```

모든 요청에 `X-Site-Key`를 추가하고 Newtonsoft.Json으로 직렬화한다. `TaskCanceledException`과 `HttpRequestException`은 `ParkingCentralUnavailableException`으로 변환한다.

- [ ] **Step 5: 클라이언트와 기존 Core 테스트 실행**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Expected: PASS

- [ ] **Step 6: 커밋**

```bash
git add src/Edge/Parking.EdgeService/LocalConfigurationController.cs src/Edge/Parking.EdgeManager.Core/IEdgeManagementClient.cs src/Edge/Parking.EdgeManager.Core/EdgeManagementClient.cs src/Edge/Parking.EdgeManager.Core/ICentralParkingClient.cs src/Edge/Parking.EdgeManager.Core/CentralParkingClient.cs src/Edge/Parking.EdgeManager.Core/ParkingCentralUnavailableException.cs tests/Parking.EdgeManager.Tests/CentralParkingClientTests.cs tests/Parking.EdgeManager.Tests/EdgeManagerPresenterTests.cs tests/Parking.Api.Tests/EdgeManagementEndpointTests.cs
git commit -m "feat: add edge central parking client"
```

### Task 6: 입차차량 관리 WinForms

**Files:**
- Create: `src/Edge/Parking.EdgeManager/EntryVehicleForm.cs`
- Create: `src/Edge/Parking.EdgeManager/EntryVehicleForm.Designer.cs`
- Create: `src/Edge/Parking.EdgeManager/EntryVehicleForm.resx`
- Create: `src/Edge/Parking.EdgeManager/ManualEntryForm.cs`
- Create: `src/Edge/Parking.EdgeManager/ManualEntryForm.Designer.cs`
- Create: `src/Edge/Parking.EdgeManager/ManualEntryForm.resx`
- Create: `src/Edge/Parking.EdgeManager/CarNumberChangeForm.cs`
- Create: `src/Edge/Parking.EdgeManager/CarNumberChangeForm.Designer.cs`
- Create: `src/Edge/Parking.EdgeManager/CarNumberChangeForm.resx`
- Modify: `src/Edge/Parking.EdgeManager/Parking.EdgeManager.csproj`
- Create: `tests/Parking.EdgeManager.Tests/VehicleManagementFormPolicyTests.cs`
- Create: `src/Edge/Parking.EdgeManager.Core/VehicleManagementFormPolicy.cs`

**Interfaces:**
- Consumes: `ICentralParkingClient`, `IEdgeManagementClient.GetImageAsync`, 현장 차로·장치 설정
- Produces: 입차 조회, 수동입차, 번호변경 화면

- [ ] **Step 1: 화면 정책 실패 테스트 작성**

```csharp
[Fact]
public void 입차장치목록은_활성_ENTRY차로의_LPR만_포함한다()
{
    IReadOnlyList<ParkingDevice> result = VehicleManagementFormPolicy.EntryDevices(
        configuration);

    Assert.Equal(new long[] { 4001 }, result.Select(x => x.DeviceId));
}

[Fact]
public void 중앙연결실패후에도_다시조회할수있다()
{
    VehicleManagementButtonState state =
        VehicleManagementFormPolicy.AfterRequestFailed();
    Assert.True(state.SearchEnabled);
    Assert.True(state.CloseEnabled);
}
```

- [ ] **Step 2: 정책 테스트 실패 확인**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj --filter FullyQualifiedName~VehicleManagementFormPolicyTests`

Expected: FAIL with `VehicleManagementFormPolicy` 누락

- [ ] **Step 3: 화면 정책 구현**

활성 `ENTRY` 차로 ID 집합과 활성 `LPR` 장치를 교집합하여 수동입차 장치 목록을 만든다. 요청 시작에는 조회·수정 버튼을 끄고 완료 또는 실패 후 다시 켜는 상태 모델을 반환한다.

- [ ] **Step 4: EntryVehicleForm UI 작성**

Designer에 다음 컨트롤을 배치한다.

```text
상단: 시작일 사용 체크 + DateTimePicker, 종료일 사용 체크 + DateTimePicker,
      그룹 ComboBox, 장치 ComboBox, 차량번호 TextBox, 조회 Button
중앙: 읽기 전용 DataGridView + 오른쪽 입차 PictureBox
하단: 이전/다음 페이지, 총 건수, 수동입차, 차량번호 변경, 닫기
```

`SearchButtonClick`, `ManualEntryButtonClick`, `ChangeCarNumberButtonClick`, `GridSelectionChanged`는 모두 `async void` 이벤트 내부에서 `try/catch/finally`로 처리한다. `TaskCanceledException`을 UI 스레드 밖으로 보내지 않는다.

- [ ] **Step 5: 수동입차 및 번호변경 대화상자 작성**

`ManualEntryForm`은 차로 선택 시 해당 차로의 입차 LPR 장치만 표시하고 입력 검증 후 `ManualEntryRequest`를 노출한다. `CarNumberChangeForm`은 기존 번호와 새 번호를 표시하고 빈 번호를 허용하지 않는다.

- [ ] **Step 6: 프로젝트 메타데이터와 테스트 실행**

`.csproj`에 세 폼의 `SubType`, `DependentUpon`을 기존 폼과 같은 형태로 등록한다.

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Expected: PASS

- [ ] **Step 7: 커밋**

```bash
git add src/Edge/Parking.EdgeManager/EntryVehicleForm.cs src/Edge/Parking.EdgeManager/EntryVehicleForm.Designer.cs src/Edge/Parking.EdgeManager/EntryVehicleForm.resx src/Edge/Parking.EdgeManager/ManualEntryForm.cs src/Edge/Parking.EdgeManager/ManualEntryForm.Designer.cs src/Edge/Parking.EdgeManager/ManualEntryForm.resx src/Edge/Parking.EdgeManager/CarNumberChangeForm.cs src/Edge/Parking.EdgeManager/CarNumberChangeForm.Designer.cs src/Edge/Parking.EdgeManager/CarNumberChangeForm.resx src/Edge/Parking.EdgeManager/Parking.EdgeManager.csproj src/Edge/Parking.EdgeManager.Core/VehicleManagementFormPolicy.cs tests/Parking.EdgeManager.Tests/VehicleManagementFormPolicyTests.cs
git commit -m "feat: add entry vehicle management forms"
```

### Task 7: 출차차량 조회 WinForms

**Files:**
- Create: `src/Edge/Parking.EdgeManager/ExitVehicleForm.cs`
- Create: `src/Edge/Parking.EdgeManager/ExitVehicleForm.Designer.cs`
- Create: `src/Edge/Parking.EdgeManager/ExitVehicleForm.resx`
- Modify: `src/Edge/Parking.EdgeManager/Parking.EdgeManager.csproj`
- Modify: `tests/Parking.EdgeManager.Tests/VehicleManagementFormPolicyTests.cs`

**Interfaces:**
- Consumes: `ICentralParkingClient.SearchExitsAsync`, `IEdgeManagementClient.GetImageAsync`
- Produces: 기간·상태·그룹·장치·차량번호 출차 조회 화면

- [ ] **Step 1: 출차 조건 정책 실패 테스트 작성**

```csharp
[Fact]
public void 출차화면_기본기간은_오늘자정부터_현재까지다()
{
    DateTimeOffset now = new(2026, 9, 23, 14, 30, 0, TimeSpan.FromHours(9));
    (DateTimeOffset from, DateTimeOffset to) =
        VehicleManagementFormPolicy.DefaultExitRange(now);

    Assert.Equal(new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.FromHours(9)), from);
    Assert.Equal(now, to);
}
```

- [ ] **Step 2: 정책 테스트 실패 확인**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj --filter FullyQualifiedName~VehicleManagementFormPolicyTests`

Expected: FAIL because `DefaultExitRange` is missing

- [ ] **Step 3: 기본기간 정책과 ExitVehicleForm 구현**

Designer 구성:

```text
상단: 시작/종료 DateTimePicker, 상태 ComboBox, 그룹 ComboBox,
      처리장치 ComboBox, 차량번호 TextBox, 조회 Button
중앙: 읽기 전용 DataGridView + 오른쪽 입차/출차 PictureBox 2개
하단: 이전/다음 페이지, 총 건수, 닫기
```

상태 ComboBox 값은 `전체=null`, `정산완료=X`, `출차완료=O`로 매핑한다. 선택 행이 바뀔 때 기존 사진 요청을 취소하고 새 사진을 요청하며 취소 예외는 표시하지 않는다.

- [ ] **Step 4: 폼 정책과 Core 전체 테스트 실행**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Expected: PASS

- [ ] **Step 5: 커밋**

```bash
git add src/Edge/Parking.EdgeManager/ExitVehicleForm.cs src/Edge/Parking.EdgeManager/ExitVehicleForm.Designer.cs src/Edge/Parking.EdgeManager/ExitVehicleForm.resx src/Edge/Parking.EdgeManager/Parking.EdgeManager.csproj tests/Parking.EdgeManager.Tests/VehicleManagementFormPolicyTests.cs
git commit -m "feat: add exit vehicle inquiry form"
```

### Task 8: MainForm 연결과 전체 회귀 검증

**Files:**
- Modify: `src/Edge/Parking.EdgeManager/Program.cs`
- Modify: `src/Edge/Parking.EdgeManager/MainForm.cs`
- Modify: `src/Edge/Parking.EdgeManager/MainForm.Designer.cs`
- Modify: `src/Edge/Parking.EdgeManager/MainForm.resx`
- Modify: `tests/Parking.EdgeManager.Tests/EdgeManagerPresenterTests.cs`
- Modify: `tests/Parking.Api.Tests/EdgeManagementEndpointTests.cs`

**Interfaces:**
- Consumes: Tasks 5~7의 중앙 클라이언트와 세 폼
- Produces: 메인 화면에서 차량 관리 창을 여는 완성된 실행 흐름

- [ ] **Step 1: 시작 구성 실패 테스트 작성**

`CentralConnectionResponse`가 있을 때 `CentralParkingClient` 생성에 중앙 URL과 키가 전달되고, 연결정보가 없을 때 기존 MainForm은 실행되지만 관리 버튼이 비활성화되는 정책 테스트를 추가한다.

```csharp
[Fact]
public void 중앙연결정보가_없으면_관리버튼만_비활성화한다()
{
    Assert.False(VehicleManagementFormPolicy.ManagementEnabled(null));
}
```

- [ ] **Step 2: 정책 테스트 실패 확인**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj --filter FullyQualifiedName~VehicleManagementFormPolicyTests`

Expected: FAIL because `ManagementEnabled` is missing

- [ ] **Step 3: Program과 MainForm 연결**

Program에서 로컬 설정을 읽은 뒤 중앙 클라이언트를 생성한다.

```csharp
CentralConnectionResponse? central = client.GetCentralConnectionAsync(
    CancellationToken.None).GetAwaiter().GetResult();
ICentralParkingClient? centralClient = central is null ? null
    : new CentralParkingClient(new HttpClient
    {
        BaseAddress = new Uri(central.CentralServerUrl),
        Timeout = TimeSpan.FromSeconds(10)
    }, central.SiteAuthKey);
Application.Run(new MainForm(client, centralClient, central?.SiteId));
```

`GetCentralConnectionAsync` 실패는 별도로 잡아 `central=null`로 두며 EdgeManager 시작을 중단하지 않는다.

MainForm 상단에 `입차차량 관리`, `출차차량 조회` 버튼을 추가하고 각각 새 폼을 연다. 폼이 닫히면 `RefreshAsync()`를 호출한다. 중앙 연결정보가 없으면 두 버튼만 비활성화하고 기존 모니터링은 계속한다.

- [ ] **Step 4: 프로젝트별 빌드와 테스트 실행**

Run:

```bat
dotnet build src\Central\Parking.Api\Parking.Api.csproj
dotnet build src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
dotnet build src\Edge\Parking.EdgeManager\Parking.EdgeManager.csproj
dotnet test tests\Parking.EdgeManager.Tests\Parking.EdgeManager.Tests.csproj
dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj
```

Expected: 모든 명령 exit code 0, 실패 테스트 0

- [ ] **Step 5: 수동 확인**

1. Parking.Api, EdgeService, EdgeManager를 실행한다.
2. 입차차량 관리에서 일반·등록 `I` 차량을 확인한다.
3. 기간·장치·뒤 4자리 필터를 각각 확인한다.
4. 수동입차 후 `manual=1`, 차종, 테이블 분기를 확인한다.
5. `I` 차량 번호변경 성공과 `X/O` 변경 거부를 확인한다.
6. 출차 조회에서 일반 `X/O`, 등록 `O`, 사진 두 장을 확인한다.
7. Parking.Api를 종료하고 두 관리 화면에서 JIT 예외창 없이 연결 오류가 표시되는지 확인한다.

- [ ] **Step 6: 최종 커밋**

```bash
git add src/Edge/Parking.EdgeManager/Program.cs src/Edge/Parking.EdgeManager/MainForm.cs src/Edge/Parking.EdgeManager/MainForm.Designer.cs src/Edge/Parking.EdgeManager/MainForm.resx tests/Parking.EdgeManager.Tests/EdgeManagerPresenterTests.cs tests/Parking.Api.Tests/EdgeManagementEndpointTests.cs
git commit -m "feat: connect edge vehicle management workflow"
```
