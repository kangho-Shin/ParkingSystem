# Device Number Identity Resolution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove `SITEID` and `APSDEVICEID` from runtime programs and resolve `SITENUM + GROUPNUM + DEVICETYPE + DEVICENUM` to the unique internal `deviceid` at the EdgeService boundary.

**Architecture:** Runtime clients send only operator-managed identity values. EdgeService loads its local site configuration, validates the site/group/type/number tuple, resolves one enabled `ParkingDevice`, and then reuses the existing `deviceid`-based connection, link, pending-event, gateway, and database flows. Central API contracts keep `deviceid` because calls reaching Central already crossed the resolution boundary.

**Tech Stack:** .NET 9, ASP.NET Core, SignalR, WinForms .NET 8, Dapper, Newtonsoft.Json, xUnit

**Spec:** `docs/superpowers/specs/2026-09-21-device-identity-resolution-design.md`

## Global Constraints

- Runtime identity is `sitenum`, `groupnum`, `devicetype`, `devicenum`.
- `deviceid` remains an internal database and server reference key.
- APSMain reads `SITENUM`, `GROUPNUM`, `APSNUM`; `SITEID` and `APSDEVICEID` are removed.
- APSMain WinForms files remain in `.cs`, `.Designer.cs`, `.resx` structure.
- Dapper and Newtonsoft.Json remain the serialization/data-access choices.
- Full build and tests are run together after all implementation tasks.
- Repository-root Windows batch files retain environment variables and `pause` between stages.

## Review Focus

- A KIOSK number that exists in another site must not register at the local site.
- A device on a lane belonging to another group must not register.
- A disabled device or disabled lane must not register.
- A reconnect must resend the same site/group/device-number tuple rather than a cached `deviceid`.
- An event completion identity that resolves to a different kiosk must return not found without completing the event.

---

### Task 1: EdgeService device identity resolver

**Files:**
- Create: `src/Edge/Parking.EdgeService/EdgeDeviceIdentity.cs`
- Create: `src/Edge/Parking.EdgeService/EdgeDeviceResolver.cs`
- Modify: `src/Edge/Parking.EdgeService/LocalConfigurationService.cs`
- Modify: `src/Edge/Parking.EdgeService/Program.cs`
- Test: `tests/Parking.Api.Tests/EdgeDeviceResolverTests.cs`

**Interfaces:**
- Consumes: `SiteConfiguration`, `ParkingDevice`, `ParkingLane`, `LocalConfigurationService.GetAsync`.
- Produces: `EdgeDeviceIdentity(long Sitenum, int Groupnum, int Devicenum, string DeviceType)`, `Task<ParkingDevice> EdgeDeviceResolver.ResolveAsync(EdgeDeviceIdentity, CancellationToken)`, `DeviceIdentityException.Code`.

- [ ] **Step 1: Write failing resolver tests**

Create tests that construct configurations directly and assert the internal ID is returned only for the exact enabled tuple:

```csharp
[Fact]
public async Task 현재_현장과_그룹의_KIOSK_장비번호를_DeviceId로_변환한다()
{
    SiteConfiguration config = Configuration(
        siteId: 9001,
        lanes: [new(9020, 9001, 2, "출차", "Exit", true)],
        devices: [new(9301, 9001, 9020, 201, "KIOSK", "출구무인", null, true)]);
    ParkingDevice device = EdgeDeviceResolver.Resolve(
        config, new(9001, 2, 201, "KIOSK"));
    Assert.Equal(9301, device.DeviceId);
}

[Theory]
[InlineData(9002, 2, 201)]
[InlineData(9001, 1, 201)]
[InlineData(9001, 2, 202)]
public void 다른_현장_그룹_장비번호는_거부한다(long site, int group, int number)
{
    DeviceIdentityException error = Assert.Throws<DeviceIdentityException>(() =>
        EdgeDeviceResolver.Resolve(Configuration9001(), new(site, group, number, "KIOSK")));
    Assert.Equal(site == 9002 ? "INVALID_SITE" : "DEVICE_NOT_FOUND", error.Code);
}
```

Add separate facts for disabled device, disabled lane, wrong device type, and duplicate matches (`DEVICE_AMBIGUOUS`).

- [ ] **Step 2: Run resolver tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~EdgeDeviceResolverTests`

Expected: FAIL because `EdgeDeviceIdentity`, `EdgeDeviceResolver`, and `DeviceIdentityException` do not exist.

- [ ] **Step 3: Implement the resolver**

Implement exact matching and stable error codes:

```csharp
public sealed record EdgeDeviceIdentity(long Sitenum, int Groupnum, int Devicenum, string DeviceType);

public sealed class DeviceIdentityException : Exception
{
    public DeviceIdentityException(string code) : base(code) { Code = code; }
    public string Code { get; }
}

public static ParkingDevice Resolve(SiteConfiguration config, EdgeDeviceIdentity identity)
{
    if (identity.Sitenum != config.Site.SiteId) throw new DeviceIdentityException("INVALID_SITE");
    HashSet<long> laneIds = config.Lanes
        .Where(x => x.Enabled && x.GroupNumber == identity.Groupnum)
        .Select(x => x.LaneId).ToHashSet();
    List<ParkingDevice> matches = config.Devices.Where(x =>
        x.Enabled && x.SiteId == identity.Sitenum && x.DeviceNumber == identity.Devicenum &&
        string.Equals(x.DeviceType, identity.DeviceType, StringComparison.OrdinalIgnoreCase) &&
        x.LaneId is long laneId && laneIds.Contains(laneId)).ToList();
    return matches.Count switch
    {
        1 => matches[0],
        > 1 => throw new DeviceIdentityException("DEVICE_AMBIGUOUS"),
        _ => throw new DeviceIdentityException("DEVICE_NOT_FOUND")
    };
}
```

Register a scoped `EdgeDeviceResolver` that loads `LocalConfigurationService.GetAsync`, rejects missing configuration, and delegates to the pure resolver.

- [ ] **Step 4: Run resolver tests and verify GREEN**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~EdgeDeviceResolverTests`

Expected: PASS for exact match, wrong site/group/type/number, disabled lane/device, and ambiguity.

- [ ] **Step 5: Commit**

```bash
git add src/Edge/Parking.EdgeService tests/Parking.Api.Tests/EdgeDeviceResolverTests.cs
git commit -m "feat: resolve Edge devices by site and device number"
```

### Task 2: Change Kiosk SignalR and completion API contracts

**Files:**
- Create: `src/BuildingBlocks/Parking.Contracts/KioskDeviceIdentity.cs`
- Modify: `src/Edge/Parking.EdgeService/KioskHub.cs`
- Modify: `src/Edge/Parking.EdgeService/KioskEventController.cs`
- Modify: `src/Edge/Parking.EdgeService/KioskExitCoordinator.cs`
- Modify: `src/Tools/Parking.KioskSimulator/Program.cs`
- Test: `tests/Parking.Api.Tests/KioskDeviceContractTests.cs`

**Interfaces:**
- Consumes: `EdgeDeviceResolver.ResolveAsync`, existing `KioskConnectionRegistry.Register(deviceId, connectionId)`, existing `KioskExitCoordinator.CompleteAsync(deviceId, eventId, token)`.
- Produces: `KioskDeviceIdentity(long Sitenum, int Groupnum, int Devicenum)`, Hub `Register(long sitenum, int groupnum, int devicenum)`, HTTP `POST api/v1/local/kiosks/events/{eventId}/complete`.

- [ ] **Step 1: Write failing contract tests**

Add tests proving the public request has no `DeviceId`, preserves PascalCase JSON, and the coordinator only receives the resolved internal key:

```csharp
[Fact]
public void Kiosk_외부식별계약에는_DeviceId가_없다()
{
    string json = JsonConvert.SerializeObject(new KioskDeviceIdentity(9001, 2, 201));
    Assert.Equal("{\"Sitenum\":9001,\"Groupnum\":2,\"Devicenum\":201}", json);
    Assert.DoesNotContain("DeviceId", json);
}
```

Add resolver-backed tests for correct `9301`, mismatched completion kiosk, and missing identity values returning bad request.

- [ ] **Step 2: Run contract tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~KioskDeviceContractTests`

Expected: FAIL because the external identity contract and new endpoint signatures do not exist.

- [ ] **Step 3: Implement Hub and completion boundary resolution**

Change Hub registration to:

```csharp
public async Task Register(long sitenum, int groupnum, int devicenum)
{
    ParkingDevice kiosk = await _resolver.ResolveAsync(
        new(sitenum, groupnum, devicenum, "KIOSK"), Context.ConnectionAborted);
    _connections.Register(kiosk.DeviceId, Context.ConnectionId);
    await _notifications.DeliverPendingAsync(kiosk.DeviceId, Context.ConnectionAborted);
}
```

Change completion to accept `[FromBody] KioskDeviceIdentity identity`, resolve KIOSK, then call the existing coordinator with `kiosk.DeviceId`. Map invalid input to 400, identity resolution failure to 404, and retain 404 when the event belongs to another kiosk.

Update KioskSimulator parsing and calls:

```text
--site 9001 --group 2 --device-number 201 --url http://localhost:5200 --complete
Register(9001, 2, 201)
POST api/v1/local/kiosks/events/{eventId}/complete
{ "Sitenum":9001, "Groupnum":2, "Devicenum":201 }
```

- [ ] **Step 4: Run Edge contract and existing Kiosk tests**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~KioskDeviceContractTests|FullyQualifiedName~KioskConnectionRegistryTests|FullyQualifiedName~KioskExit"`

Expected: PASS; the registry and downstream processing still use internal `deviceid`.

- [ ] **Step 5: Commit**

```bash
git add src/BuildingBlocks/Parking.Contracts src/Edge/Parking.EdgeService src/Tools/Parking.KioskSimulator tests/Parking.Api.Tests
git commit -m "feat: register kiosks by site and device number"
```

### Task 3: Convert APSMain to shared site/group/device-number settings

**Files:**
- Modify: `APSMain_C(API)V2/Integration/EdgeService/EdgeServiceOptions.cs`
- Modify: `APSMain_C(API)V2/Integration/EdgeService/KioskSignalRClient.cs`
- Modify: `APSMain_C(API)V2/Integration/EdgeService/EdgeServiceClient.cs`
- Modify: `APSMain_C(API)V2/CarInNumForm.cs`
- Modify: `APSMain_C(API)V2/CarInNumForm15.cs`
- Modify: `APSMain_C(API)V2/App.config`
- Modify: `tests/APSMain.EdgeIntegration.Tests/EdgeServiceOptionsTests.cs`
- Modify: `tests/APSMain.EdgeIntegration.Tests/EdgeServiceClientTests.cs`

**Interfaces:**
- Consumes: Hub `Register(sitenum, groupnum, devicenum)` and `KioskDeviceIdentity` JSON shape.
- Produces: `EdgeServiceOptions(Uri BaseAddress, long Sitenum, int Groupnum, int Devicenum)`, `EdgeServiceClient.CompleteKioskEventAsync(Guid, CancellationToken)`.

- [ ] **Step 1: Replace option tests first**

```csharp
[Fact]
public void Load_uses_existing_APS_identity_settings()
{
    NameValueCollection values = new()
    {
        ["SITENUM"] = "9001", ["GROUPNUM"] = "2", ["APSNUM"] = "201"
    };
    EdgeServiceOptions options = EdgeServiceOptions.Load(values);
    Assert.Equal(9001, options.Sitenum);
    Assert.Equal(2, options.Groupnum);
    Assert.Equal(201, options.Devicenum);
}
```

Change the validation theory to reject empty/non-positive `SITENUM`, `GROUPNUM`, and `APSNUM`. Add a client test asserting search uses `siteId=9001&groupnum=2` and completion posts the three-field JSON body without `deviceid`.

- [ ] **Step 2: Run APSMain integration tests and verify RED**

Run: `dotnet test tests/APSMain.EdgeIntegration.Tests/APSMain.EdgeIntegration.Tests.csproj --filter "FullyQualifiedName~EdgeServiceOptionsTests|FullyQualifiedName~EdgeServiceClientTests"`

Expected: FAIL because the options and completion client still use internal `DeviceId`.

- [ ] **Step 3: Implement APSMain option, SignalR, search, and completion changes**

Use the existing setting names only:

```csharp
public sealed record EdgeServiceOptions(Uri BaseAddress, long Sitenum, int Groupnum, int Devicenum);

return new EdgeServiceOptions(
    baseAddress,
    ReadPositiveLong(values, "SITENUM"),
    ReadPositiveInt(values, "GROUPNUM"),
    ReadPositiveInt(values, "APSNUM"));
```

Register and re-register with `options.Sitenum`, `options.Groupnum`, `options.Devicenum`. Update search URL construction and the two vehicle number forms to use `Sitenum` and `Devicenum`. Add completion posting with Newtonsoft JSON and remove `SITEID` and `APSDEVICEID` from `App.config`.

- [ ] **Step 4: Run all APSMain integration tests**

Run: `dotnet test tests/APSMain.EdgeIntegration.Tests/APSMain.EdgeIntegration.Tests.csproj`

Expected: PASS with no option test or HTTP contract regression.

- [ ] **Step 5: Commit**

```bash
git add "APSMain_C(API)V2" tests/APSMain.EdgeIntegration.Tests
git commit -m "refactor: use APS site and device number identity"
```

### Task 4: Align launchers, TerminalAgent configuration, and documentation

**Files:**
- Modify: `run-foundations.bat`
- Modify: `start-ldm-test.bat`
- Modify: `run-terminal-agent-test.bat`
- Modify: `src/Edge/Parking.TerminalAgent/appsettings.json`
- Modify: `tests/Parking.TerminalAgent.Tests/ProgramSupervisorTests.cs`
- Modify: `docs/01-program-overview.md`
- Modify: `docs/02-program-features.md`
- Modify: `docs/03-api-specification.md`
- Modify: `docs/04-development-status.md`
- Modify: `docs/06-apsmain-edge-integration-design.md`
- Modify: `docs/superpowers/plans/2026-09-20-apsmain-edge-integration.md`

**Interfaces:**
- Consumes: KioskSimulator `--site`, `--group`, `--device-number`; APSMain `SITENUM`, `GROUPNUM`, `APSNUM`.
- Produces: consistent launch commands and current documentation with no active `SITEID`, `APSDEVICEID`, or KioskSimulator `--device` usage.

- [ ] **Step 1: Write failing launcher/config assertions**

Update `ProgramSupervisorTests` expected arguments to:

```csharp
Arguments = "--site 9001 --group 2 --device-number 201 --url http://localhost:5200 --complete"
```

Add a repository text check test that scans active `.bat`, APSMain `App.config`, and current docs and rejects `APSDEVICEID`, `<add key="SITEID"`, or KioskSimulator `--device `.

- [ ] **Step 2: Run launcher tests and verify RED**

Run: `dotnet test tests/Parking.TerminalAgent.Tests/Parking.TerminalAgent.Tests.csproj`

Expected: FAIL because launchers and configured arguments still use `deviceid=9301`.

- [ ] **Step 3: Update launchers and documentation**

Replace each active simulator invocation with:

```bat
--site 9001 --group 2 --device-number 201 --url http://localhost:5200 --complete
```

Keep existing environment variables, execution order, and every stage `pause`. Update current docs to say runtime programs use `sitenum/groupnum/devicenum`, EdgeService resolves `deviceid`, and Central API retains it internally. Historical plans may describe the superseded contract only when explicitly marked obsolete; otherwise update them.

- [ ] **Step 4: Run the complete build and test batch**

Run on Windows from repository root: `run-apsmain-edge-test.bat`

Expected: solution restore/build succeeds; `ParkingSystem.sln`, APSMain, all solution tests, and `APSMain.EdgeIntegration.Tests` pass; API, Gateway, EdgeService, and APSMain start in sequence with `pause` between stages.

- [ ] **Step 5: Run static stale-reference checks**

Run:

```bash
rg -n 'APSDEVICEID|<add key="SITEID"|Parking.KioskSimulator.*--device ' APSMain_C\(API\)V2 src tests *.bat docs
```

Expected: no active code, config, launcher, or current-document matches except migration/history text explicitly identifying the removed settings.

- [ ] **Step 6: Commit**

```bash
git add *.bat src/Edge/Parking.TerminalAgent tests/Parking.TerminalAgent.Tests docs
git commit -m "docs: standardize runtime device identity settings"
```

## Final Verification

- [ ] Run `git diff --check` and confirm no whitespace errors.
- [ ] Run `dotnet build ParkingSystem.sln --no-restore` on Windows.
- [ ] Run `dotnet build "APSMain_C(API)V2/APSMain.csproj" --no-restore` on Windows.
- [ ] Run `dotnet test ParkingSystem.sln --no-build` on Windows.
- [ ] Run `dotnet test tests/APSMain.EdgeIntegration.Tests/APSMain.EdgeIntegration.Tests.csproj --no-build` on Windows.
- [ ] Verify APSMain connects with `9001/2/201`, EdgeService logs resolved `deviceid=9301`, and vehicle suffix `1001` searches site 9001/group 2.
