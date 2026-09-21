# Parking.EdgeManager Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows EdgeManager that shows local service health, configuration, current entries, settlement/exit activity, and automatically changing LPR images through EdgeService management APIs.

**Architecture:** EdgeService owns a SQLite monitoring store and read-only management API; EdgeGateway exposes a health probe that distinguishes Gateway health from central API reachability. A platform-neutral EdgeManager.Core project performs HTTP polling and presentation-state selection, while a thin .NET 9 WinForms project renders that state.

**Tech Stack:** .NET 9, ASP.NET Core, WinForms, Dapper, Microsoft.Data.Sqlite, Newtonsoft.Json, xUnit

**Spec:** `docs/superpowers/specs/2026-09-19-edge-manager-design.md`

## Global Constraints

- JSON property names remain PascalCase.
- EdgeManager reads EdgeService APIs only; it never opens SQLite or image directories directly.
- Entry and activity API limits are integers from 1 through 1,000; invalid limits return HTTP 400.
- Current entries disappear only after an allowed exit; blocked or failed exits keep the entry.
- Settlement and exit are separate activity rows and only the newest 1,000 activity rows are retained.
- Every new event overrides a manual image selection and displays the newest event images.
- Image requests accept a file name only and cannot escape `Edge:ImageDirectory`.
- Existing entry, exit, fee, payment, configuration, and Outbox behavior must remain compatible.

## Review Focus

- A duplicate EventId or PaymentId must update/reuse one monitoring row, never append a duplicate.
- An offline entry without a ParkingSessionId must later acquire the ID after Outbox delivery without losing its original image.
- A blocked exit must remain visible as an activity while its current-entry row remains present.
- A payment whose ParkingSessionId is unknown locally must still create an activity with blank vehicle/image fields.
- A slow or failed poll must not overlap the next poll, clear the last good screen, or terminate EdgeManager.

---

### Task 1: Monitoring contracts and existing store counters

**Files:**
- Create: `src/BuildingBlocks/Parking.Contracts/EdgeManagementModels.cs`
- Modify: `src/Edge/Parking.EdgeService/LocalConfigurationStore.cs`
- Modify: `src/Edge/Parking.EdgeService/SqliteOutboxRepository.cs`
- Test: `tests/Parking.Api.Tests/EdgeManagementStoreTests.cs`

**Interfaces:**
- Produces: `EdgeServiceStatus`, `EdgeEntryItem`, `EdgeActivityItem`, `EdgeActivityType`, `EdgeDeliveryState`, and `EdgeManagementSnapshot` contracts.
- Produces: `LocalConfigurationStore.GetSyncedAtAsync(CancellationToken)` and `SqliteOutboxRepository.CountPendingAsync(CancellationToken)`.

- [ ] **Step 1: Write failing contract and counter tests**

```csharp
[Fact]
public async Task 설정동기화시각과_대기Outbox수를_조회한다()
{
    await using TemporaryEdgeDatabase db = await TemporaryEdgeDatabase.CreateAsync();
    await db.Configuration.SaveAsync(SiteConfigurationFixture.Create(), default);
    await db.Outbox.EnqueueEntryAsync(EntryFixture.Create(), default);

    Assert.NotNull(await db.Configuration.GetSyncedAtAsync(default));
    Assert.Equal(1, await db.Outbox.CountPendingAsync(default));
}

[Fact]
public void 관리계약은_입차와_정산출차를_구분한다()
{
    EdgeActivityItem item = new(Guid.NewGuid(), EdgeActivityType.Payment, 10, 1, 1,
        null, null, "12가3456", DateTimeOffset.UtcNow, "in.jpg", null,
        1000, 200, 800, "Card", null, EdgeDeliveryState.Completed, "OK");
    Assert.Equal(EdgeActivityType.Payment, item.ActivityType);
}
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeManagementStoreTests"`

Expected: FAIL because the management contracts and counter methods do not exist.

- [ ] **Step 3: Add minimal contracts and counter queries**

```csharp
namespace Parking.Contracts;

public enum EdgeActivityType { Payment, Exit }
public enum EdgeDeliveryState { Pending, Completed, Failed }

public sealed record EdgeServiceStatus(
    bool ServiceConnected, bool GatewayConnected, bool CentralConnected,
    DateTimeOffset? ConfigurationSyncedAt, int PendingOutboxCount);

public sealed record EdgeEntryItem(
    Guid EventId, long? ParkingSessionId, long SiteId, int Groupnum,
    long LaneId, long DeviceId, string CarNumber, DateTimeOffset InDateTime,
    string? InImage, EdgeDeliveryState DeliveryState, string? ResultCode);

public sealed record EdgeActivityItem(
    Guid ActivityId, EdgeActivityType ActivityType, long? ParkingSessionId,
    long SiteId, int Groupnum, long? LaneId, long? DeviceId, string CarNumber,
    DateTimeOffset OccurredAt, string? InImage, string? OutImage,
    long? OriginalFee, long? DiscountFee, long? PaidAmount, string? PaymentMethod,
    bool? OpenBarrier, EdgeDeliveryState DeliveryState, string? ResultCode);

public sealed record EdgeManagementSnapshot(
    IReadOnlyList<EdgeEntryItem> Entries,
    IReadOnlyList<EdgeActivityItem> Activities);
```

Add `SELECT synced_at_utc ...` and `SELECT COUNT(*) ... WHERE state=0` methods, parsing SQLite integers as `long` before checked conversion to `int`.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeManagementStoreTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BuildingBlocks/Parking.Contracts/EdgeManagementModels.cs src/Edge/Parking.EdgeService/LocalConfigurationStore.cs src/Edge/Parking.EdgeService/SqliteOutboxRepository.cs tests/Parking.Api.Tests/EdgeManagementStoreTests.cs
git commit -m "feat: add edge management contracts"
```

### Task 2: SQLite monitoring repository

**Files:**
- Create: `src/Edge/Parking.EdgeService/EdgeMonitoringRepository.cs`
- Modify: `src/Edge/Parking.EdgeService/Program.cs`
- Test: `tests/Parking.Api.Tests/EdgeMonitoringRepositoryTests.cs`

**Interfaces:**
- Consumes: Task 1 management contracts.
- Produces: `RecordEntryAsync`, `CompleteEntryDeliveryAsync`, `RecordPaymentAsync`, `RecordExitAsync`, `CompleteActivityDeliveryAsync`, `GetEntriesAsync`, and `GetActivitiesAsync`.

- [ ] **Step 1: Write failing repository tests**

```csharp
[Fact]
public async Task 입차는_중복없이_저장되고_성공출차때만_삭제된다()
{
    await using TemporaryEdgeDatabase db = await TemporaryEdgeDatabase.CreateAsync();
    FieldEventRequest entry = EntryFixture.Create();
    await db.Monitor.RecordEntryAsync(entry, default);
    await db.Monitor.RecordEntryAsync(entry, default);
    Assert.Single(await db.Monitor.GetEntriesAsync(1000, default));

    ExitEventRequest blocked = ExitFixture.Create(entry.CarNumber);
    await db.Monitor.RecordExitAsync(blocked, new FieldEventResponse(
        blocked.EventId, false, null, "UNPAID", "미결제", false), default);
    Assert.Single(await db.Monitor.GetEntriesAsync(1000, default));

    ExitEventRequest allowed = ExitFixture.Create(entry.CarNumber);
    await db.Monitor.RecordExitAsync(allowed, new FieldEventResponse(
        allowed.EventId, true, 10, "OK", "출차", true), default);
    Assert.Empty(await db.Monitor.GetEntriesAsync(1000, default));
}

[Fact]
public async Task 처리이력은_정산과_출차를_각각남기고_최근1000건만_유지한다()
{
    await using TemporaryEdgeDatabase db = await TemporaryEdgeDatabase.CreateAsync();
    await db.Monitor.RecordPaymentAsync(PaymentFixture.Create(10), "COMPLETED", default);
    await db.Monitor.RecordExitAsync(ExitFixture.Create("12가3456"),
        ExitFixture.AllowedResponse(), default);
    Assert.Equal(2, (await db.Monitor.GetActivitiesAsync(1000, default)).Count);

    for (int i = 0; i < 1001; i++)
        await db.Monitor.RecordPaymentAsync(PaymentFixture.Create(i + 100), "COMPLETED", default);
    Assert.Equal(1000, (await db.Monitor.GetActivitiesAsync(1000, default)).Count);
}
```

Add these named cases in the same file with the shown assertions:

```csharp
[Fact] public async Task 같은식별자는_한행만_유지한다() =>
    Assert.Single(await SaveSamePaymentTwiceAsync());
[Fact] public async Task 모르는주차번호의_정산도_빈차량번호로_남긴다() =>
    Assert.Equal("", (await SaveUnknownPaymentAsync()).CarNumber);
[Fact] public async Task 목록은_최신발생순이다() =>
    Assert.True(await IsNewestFirstAsync());
[Fact] public async Task 재전송입차는_기존행에_세션번호를_채운다() =>
    Assert.Equal(77, (await CompleteOfflineEntryAsync()).ParkingSessionId);
[Fact] public async Task 차단된출차는_현재입차를_지우지않는다() =>
    Assert.Single(await RecordBlockedExitAsync());
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeMonitoringRepositoryTests"`

Expected: FAIL because `EdgeMonitoringRepository` does not exist.

- [ ] **Step 3: Implement the repository and schema**

Create `monitor_entry` keyed by `entry_event_id`, with a unique nullable `parking_session_id`, and `monitor_activity` keyed by `(activity_id, activity_type)`. Use transactions for exit insert plus conditional entry deletion, and payment insert plus trimming:

```sql
DELETE FROM monitor_activity
WHERE rowid IN (
    SELECT rowid FROM monitor_activity
    ORDER BY occurred_at_utc DESC, rowid DESC
    LIMIT -1 OFFSET 1000);
```

Match exits to current entries by `ParkingSessionId` when supplied by the response; otherwise use the newest matching site, group, and car number. Delete the entry only when `OpenBarrier == true`. Register and initialize the repository in `Program.cs` with the same SQLite connection string as the Outbox.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeMonitoringRepositoryTests"`

Expected: PASS, including 1,000-row trimming and duplicate cases.

- [ ] **Step 5: Commit**

```bash
git add src/Edge/Parking.EdgeService/EdgeMonitoringRepository.cs src/Edge/Parking.EdgeService/Program.cs tests/Parking.Api.Tests/EdgeMonitoringRepositoryTests.cs
git commit -m "feat: store edge monitoring activity"
```

### Task 3: Record entry, payment, exit, and retry outcomes

**Files:**
- Modify: `src/Edge/Parking.EdgeService/EdgeEventService.cs`
- Modify: `src/Edge/Parking.EdgeService/PaymentRelayService.cs`
- Modify: `src/Edge/Parking.EdgeService/OutboxWorker.cs`
- Test: `tests/Parking.Api.Tests/EdgeMonitoringFlowTests.cs`

**Interfaces:**
- Consumes: Task 2 repository methods.
- Produces: monitoring rows synchronized with immediate and Outbox-delayed processing results.

- [ ] **Step 1: Write failing flow tests**

```csharp
[Fact]
public async Task 오프라인입차는_대기로보이고_재전송성공후_세션번호를_가진다()
{
    // Gateway handler fails first, succeeds with ParkingSessionId=77 second.
    // AcceptEntryAsync must create Pending entry; one worker cycle must mark it Completed.
    Assert.Equal(EdgeDeliveryState.Pending, pending.DeliveryState);
    Assert.Null(pending.ParkingSessionId);
    Assert.Equal(77, completed.ParkingSessionId);
}

[Fact]
public async Task 정산과_출차는_별도행이고_출차허용때_입차가_사라진다()
{
    // Arrange one entry, complete payment, then allowed exit.
    Assert.Equal(new[] { EdgeActivityType.Exit, EdgeActivityType.Payment },
        activities.Select(x => x.ActivityType));
    Assert.Empty(entries);
}
```

Add the exact regression cases:

```csharp
[Fact] public async Task 차단출차후에도_입차행은_남는다() =>
    Assert.Single(await RunBlockedExitFlowAsync());
[Fact] public async Task 같은PaymentId재전송은_정산행을_중복하지않는다() =>
    Assert.Single(await RunDuplicatePaymentFlowAsync());
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeMonitoringFlowTests"`

Expected: FAIL because services do not write monitoring state.

- [ ] **Step 3: Integrate monitoring writes**

Inject `EdgeMonitoringRepository` into the three services. Save the Pending record before network I/O, then update it with the returned result. Extract `OutboxWorker.ProcessOnceAsync(CancellationToken)` so the background loop and tests execute the same retry logic. For payment JSON, deserialize `CompletePaymentRequest`; for retry success, mark the matching monitoring row Completed. Never remove an entry on a blocked exit.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeMonitoringFlowTests"`

Expected: PASS.

- [ ] **Step 5: Run existing Edge relay and Outbox regressions**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~FeeQuoteRelayTests|FullyQualifiedName~CreateEntryEndpointTests|FullyQualifiedName~ExitEndpointTests"`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Edge/Parking.EdgeService/EdgeEventService.cs src/Edge/Parking.EdgeService/PaymentRelayService.cs src/Edge/Parking.EdgeService/OutboxWorker.cs tests/Parking.Api.Tests/EdgeMonitoringFlowTests.cs
git commit -m "feat: monitor edge event processing"
```

### Task 4: Gateway and central connectivity health

**Files:**
- Modify: `src/Central/Parking.EdgeGateway/ParkingApiClient.cs`
- Create: `src/Central/Parking.EdgeGateway/HealthController.cs`
- Modify: `src/Edge/Parking.EdgeService/GatewayClient.cs`
- Test: `tests/Parking.Api.Tests/EdgeHealthTests.cs`

**Interfaces:**
- Produces: Gateway `GET /health` response `GatewayHealthResponse(bool GatewayConnected, bool CentralConnected)`.
- Produces: `GatewayClient.GetHealthAsync(CancellationToken)`.

- [ ] **Step 1: Write failing health tests**

```csharp
[Fact]
public async Task 중앙이_끊겨도_Gateway는_200과_분리된상태를_반환한다()
{
    using HttpClient client = GatewayFactory.WithApiResponse(HttpStatusCode.ServiceUnavailable);
    using HttpResponseMessage response = await client.GetAsync("/health");
    GatewayHealthResponse body = await ReadAsync<GatewayHealthResponse>(response);
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.True(body.GatewayConnected);
    Assert.False(body.CentralConnected);
}
```

Add the two complementary cases:

```csharp
[Fact] public async Task 중앙응답정상일때_두연결은_정상이다() =>
    Assert.Equal(new GatewayHealthResponse(true, true), await GetHealthyGatewayAsync());
[Fact] public async Task Gateway연결실패는_호출자에게_예외로_전달한다() =>
    await Assert.ThrowsAsync<HttpRequestException>(() => GetUnreachableGatewayAsync());
```

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeHealthTests"`

Expected: FAIL because `/health` and the client method do not exist.

- [ ] **Step 3: Implement probes**

Add `ParkingApiClient.CheckHealthAsync` using `GET /`; catch request and timeout exceptions in `HealthController`, returning HTTP 200 with `CentralConnected=false`. Add the EdgeService Gateway client method without swallowing connection errors so the management service can distinguish an unreachable Gateway.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeHealthTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Central/Parking.EdgeGateway/ParkingApiClient.cs src/Central/Parking.EdgeGateway/HealthController.cs src/Edge/Parking.EdgeService/GatewayClient.cs tests/Parking.Api.Tests/EdgeHealthTests.cs
git commit -m "feat: report gateway and central health"
```

### Task 5: EdgeService management and protected image APIs

**Files:**
- Create: `src/Edge/Parking.EdgeService/EdgeManagementService.cs`
- Create: `src/Edge/Parking.EdgeService/ManagementController.cs`
- Modify: `src/Edge/Parking.EdgeService/appsettings.json`
- Modify: `src/Edge/Parking.EdgeService/Program.cs`
- Test: `tests/Parking.Api.Tests/EdgeManagementEndpointTests.cs`

**Interfaces:**
- Consumes: Tasks 1-4 repository, configuration, Outbox, and health methods.
- Produces: all `/api/v1/management/*` endpoints in the spec.

- [ ] **Step 1: Write failing endpoint tests**

```csharp
[Theory]
[InlineData(0)]
[InlineData(1001)]
public async Task 목록limit범위가_아니면_400이다(int limit)
{
    Assert.Equal(HttpStatusCode.BadRequest,
        (await client.GetAsync($"/api/v1/management/entries?limit={limit}")).StatusCode);
}

[Theory]
[InlineData("../secret.jpg")]
[InlineData("C:\\secret.jpg")]
[InlineData("sub/image.jpg")]
public async Task 이미지폴더를_벗어나는_파일명은_거부한다(string fileName)
{
    Assert.Equal(HttpStatusCode.BadRequest,
        (await client.GetAsync("/api/v1/management/images/" + Uri.EscapeDataString(fileName))).StatusCode);
}
```

Add named endpoint cases asserting `200` for status, entries, activities, configuration and JPEG; `404` for a missing image; and a `200` status body with `GatewayConnected=false` and `CentralConnected=false` when Gateway is unreachable. Deserialize every success response and assert at least one domain field, not only the HTTP code.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeManagementEndpointTests"`

Expected: FAIL with endpoint 404 responses.

- [ ] **Step 3: Implement service and controller**

Use explicit route methods and `Path.GetFileName(fileName) == fileName` plus rejection of directory separators and rooted paths. Resolve the full path under the configured, canonicalized `Edge:ImageDirectory`; serve only `.jpg`, `.jpeg`, and `.png` using known MIME types. Add:

```json
"Edge": {
  "SiteId": 1,
  "DataDirectory": "",
  "ImageDirectory": ""
}
```

Return the cached `SiteConfiguration` from the existing store and status with independent service, Gateway, and central flags.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeManagementEndpointTests"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Edge/Parking.EdgeService/EdgeManagementService.cs src/Edge/Parking.EdgeService/ManagementController.cs src/Edge/Parking.EdgeService/appsettings.json src/Edge/Parking.EdgeService/Program.cs tests/Parking.Api.Tests/EdgeManagementEndpointTests.cs
git commit -m "feat: expose edge management API"
```

### Task 6: Platform-neutral EdgeManager polling and presentation state

**Files:**
- Create: `src/Edge/Parking.EdgeManager.Core/Parking.EdgeManager.Core.csproj`
- Create: `src/Edge/Parking.EdgeManager.Core/EdgeManagementClient.cs`
- Create: `src/Edge/Parking.EdgeManager.Core/EdgeManagerPresenter.cs`
- Create: `src/Edge/Parking.EdgeManager.Core/IEdgeManagerView.cs`
- Create: `tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`
- Create: `tests/Parking.EdgeManager.Tests/EdgeManagerPresenterTests.cs`
- Modify: `ParkingSystem.sln`

**Interfaces:**
- Consumes: management contracts and APIs from Tasks 1 and 5.
- Produces: `EdgeManagementClient` and `EdgeManagerPresenter.RefreshAsync(CancellationToken)` for the WinForms shell.

- [ ] **Step 1: Write failing presenter tests**

```csharp
[Fact]
public async Task 새사건은_사용자선택보다_우선해_사진을_전환한다()
{
    FakeView view = new();
    FakeClient client = new(Snapshot.WithEntry("old", sequence: 1));
    EdgeManagerPresenter presenter = new(client, view);
    await presenter.RefreshAsync(default);
    presenter.SelectEntry("old");

    client.Snapshot = Snapshot.WithEntry("new", sequence: 2);
    await presenter.RefreshAsync(default);

    Assert.Equal("new-in.jpg", view.InImage);
}

[Fact]
public async Task 조회실패는_마지막자료를_지우지않고_연결상태만_바꾼다()
{
    // First refresh succeeds; second throws HttpRequestException.
    Assert.Equal(previousEntries, view.Entries);
    Assert.False(view.ServiceConnected);
}
```

Add named presenter cases for: a second concurrent `RefreshAsync` returning without another client call; an entry row click selecting its input image; an exit row click selecting both input/output images; and an unchanged snapshot preserving the user's manual selection. Assert both the client call count and the exact image file names.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Expected: FAIL because the Core project and presenter do not exist.

- [ ] **Step 3: Implement client and presenter**

Define `IEdgeManagementClient` for tests and `IEdgeManagerView` with methods to render status, entries, activities, configuration, and images. Guard `RefreshAsync` with `SemaphoreSlim.WaitAsync(0)`; retain last successful state. Detect new events by `(EventId, OccurredAt)` and `(ActivityId, ActivityType, OccurredAt)`, selecting the newest timestamp. Retrieve image bytes only when the displayed file name changes.

- [ ] **Step 4: Run focused tests and verify GREEN**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Edge/Parking.EdgeManager.Core tests/Parking.EdgeManager.Tests ParkingSystem.sln
git commit -m "feat: add edge manager polling core"
```

### Task 7: WinForms EdgeManager shell

**Files:**
- Create: `src/Edge/Parking.EdgeManager/Parking.EdgeManager.csproj`
- Create: `src/Edge/Parking.EdgeManager/Program.cs`
- Create: `src/Edge/Parking.EdgeManager/MainForm.cs`
- Create: `src/Edge/Parking.EdgeManager/MainForm.Designer.cs`
- Create: `src/Edge/Parking.EdgeManager/appsettings.json`
- Modify: `ParkingSystem.sln`
- Modify: `docs/01-program-overview.md`
- Modify: `docs/02-program-features.md`
- Modify: `docs/04-development-status.md`

**Interfaces:**
- Consumes: Task 6 presenter and view interface.
- Produces: runnable `Parking.EdgeManager.exe` with a one-second refresh timer.

- [ ] **Step 1: Add the WinForms project and compile to establish RED**

Create the project reference before its source files:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Parking.EdgeManager.Core\Parking.EdgeManager.Core.csproj" />
  </ItemGroup>
</Project>
```

Run: `dotnet build src/Edge/Parking.EdgeManager/Parking.EdgeManager.csproj`

Expected: FAIL because `Program` and `MainForm` do not exist.

- [ ] **Step 2: Implement the thin WinForms view**

Build one resizable form with a top status `TableLayoutPanel`, entry `DataGridView`, activity `DataGridView`, two `PictureBox` controls, and a read-only configuration grid. `MainForm` implements `IEdgeManagerView`; row clicks call presenter selection methods. A `System.Windows.Forms.Timer` ticks every 1,000 ms and awaits `RefreshAsync`; UI updates are performed on the UI thread. Replace and dispose old `Image` objects whenever image bytes change.

- [ ] **Step 3: Build the Windows project and run core tests**

Run: `dotnet build src/Edge/Parking.EdgeManager/Parking.EdgeManager.csproj && dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Expected: build succeeds and all presenter/client tests pass.

- [ ] **Step 4: Register the project and update documentation**

Add EdgeManager under the solution `Edge` folder. Document the default EdgeService URL, one-second refresh, two-list behavior, automatic LPR switching, `Edge:ImageDirectory`, and local run command. Change the development status from “EdgeManager unimplemented” to “minimum monitoring implemented” only after verification.

- [ ] **Step 5: Run the complete verification suite**

Run: `dotnet test ParkingSystem.sln`

Expected: all tests pass with zero failures.

Run: `dotnet build ParkingSystem.sln`

Expected: solution builds with zero errors.

- [ ] **Step 6: Commit**

```bash
git add src/Edge/Parking.EdgeManager ParkingSystem.sln docs/01-program-overview.md docs/02-program-features.md docs/04-development-status.md
git commit -m "feat: add edge manager monitoring UI"
```

## Final manual verification

1. Start Parking.Api, Parking.EdgeGateway, Parking.EdgeService, and Parking.EdgeManager.
2. Send one entry from Parking.Simulator and verify it appears in the entry list and its image is displayed.
3. Click an older row, then send a new entry and verify the image switches to the new vehicle.
4. Complete payment and verify one Payment row appears while the entry remains.
5. Send an allowed exit and verify the entry disappears, an Exit row remains, and entry/exit images appear together.
6. Stop Parking.Api while leaving Gateway running and verify `Gateway 정상 / 중앙 끊김`.
7. Stop Gateway and verify the screen retains its last data while showing both downstream connections unavailable.
