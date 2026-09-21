# JPXLPR EdgeService Client Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert JPXLPR into a camera/recognition/image client that sends durable LPR events only to EdgeService TCP port 29200.

**Architecture:** Pure protocol and outbox classes isolate filename/frame creation, TCP ACK handling, and durable retry from WinForms and the camera SDK. The existing form keeps camera and recognition responsibilities, then hands one immutable event to the outbox; all REST, UDP, APS, LDM, barrier, and legacy DB code is deleted.

**Tech Stack:** C# 12, .NET 8 WinForms, Newtonsoft.Json, TCP sockets, xUnit

**Spec:** `docs/superpowers/specs/2026-09-21-jpxlpr-edge-client-design.md`

## Global Constraints

- WinForms remains `.cs`, `.Designer.cs`, `.resx`.
- Runtime identity is `SITENUM + GROUPNUM + DEVICENUM`; JPXLPR never uses internal `deviceid`.
- Test site is `SITENUM=9001`, `GROUPNUM=2`.
- EdgeService protocol is `STX + CP949 filename + ETX`, response `ACK|EventId` or `NACK|reason`.
- EventId and filename remain unchanged across retries.
- Camera SDK, recognition engine, exposure controls, and four-camera UI remain intact.
- Tests are implemented with each unit but the user-facing verification is one final full build/test run.

## Review Focus

- A split TCP ACK must be reassembled before matching EventId; Task 2 tests byte-by-byte response delivery.
- A stale ACK for another EventId must not remove the current outbox item; Task 2 tests mismatched IDs.
- Process termination after enqueue but before send must preserve the event; Task 3 tests save-before-send and reload.
- Multiple cameras must not overwrite each other's events; Task 3 tests distinct IDs and filenames in one outbox.
- Invalid lane/device/direction configuration must block only that camera's transmission; Task 1 tests every invalid field.

---

### Task 1: Edge Identity and Frame Contract

**Files:**
- Create: `JPXLpr/Edge/EdgeLprOptions.cs`
- Create: `JPXLpr/Edge/EdgeLprEvent.cs`
- Create: `JPXLpr/Edge/EdgeLprFrame.cs`
- Create: `tests/JPXLpr.Tests/JPXLpr.Tests.csproj`
- Create: `tests/JPXLpr.Tests/EdgeLprFrameTests.cs`
- Modify: `ParkingSystem.sln`

**Interfaces:**
- Consumes: sitenum, groupnum, laneid, devicenum, direction, timestamp, car number, EventId.
- Produces: `EdgeLprOptions.Validate()`, `EdgeLprEvent.FileName`, `EdgeLprFrame.Encode(string)`, `EdgeLprFrame.TryParseResponse(...)`.

- [ ] **Step 1: Add failing frame and validation tests**

```csharp
[Theory]
[InlineData(0, 2, 9010, 101, "Entry")]
[InlineData(9001, 0, 9010, 101, "Entry")]
[InlineData(9001, 2, 0, 101, "Entry")]
[InlineData(9001, 2, 9010, 0, "Entry")]
[InlineData(9001, 2, 9010, 101, "Unknown")]
public void Invalid_identity_is_rejected(int site, int group, int lane, int device, string direction)
{
    EdgeLprOptions value = new(site, group, lane, device, direction, "127.0.0.1", 29200);
    Assert.Throws<InvalidOperationException>(() => value.Validate());
}

[Fact]
public void Entry_filename_and_cp949_frame_match_edge_protocol()
{
    Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");
    EdgeLprEvent value = EdgeLprEvent.Create(9001, 2, 101, 9010, "Entry",
        new DateTime(2026, 9, 21, 10, 20, 30, 456), "12가3456", id);
    Assert.Equal("9001_002_101_9010_Entry_20260921102030456_12가3456_11111111222233334444555555555555.jpg", value.FileName);
    byte[] frame = EdgeLprFrame.Encode(value.FileName);
    Assert.Equal(0x02, frame[0]);
    Assert.Equal(0x03, frame[^1]);
    Assert.Equal(value.FileName, Encoding.GetEncoding(949).GetString(frame[1..^1]));
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj --filter "Invalid_identity|Entry_filename"`

Expected: compile failure because Edge classes do not exist.

- [ ] **Step 3: Implement the contract classes**

```csharp
public sealed record EdgeLprOptions(int Sitenum, int Groupnum, int Laneid,
    int Devicenum, string Direction, string Host, int Port)
{
    public void Validate()
    {
        if (Sitenum <= 0 || Groupnum <= 0 || Laneid <= 0 || Devicenum <= 0)
            throw new InvalidOperationException("LPR identity is invalid.");
        if (Direction is not ("Entry" or "Exit"))
            throw new InvalidOperationException("Direction must be Entry or Exit.");
        if (string.IsNullOrWhiteSpace(Host) || Port is < 1 or > 65535)
            throw new InvalidOperationException("EdgeService endpoint is invalid.");
    }
}
```

Register code page support once in `EdgeLprFrame` and encode the exact filename between `0x02` and `0x03`.

- [ ] **Step 4: Run Task 1 tests**

Run: `dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj`

Expected: all JPXLpr tests pass.

- [ ] **Step 5: Commit Task 1**

```bat
git add JPXLpr\Edge tests\JPXLpr.Tests ParkingSystem.sln
git commit -m "feat: add JPXLPR Edge frame contract"
```

### Task 2: TCP Client and ACK Parser

**Files:**
- Create: `JPXLpr/Edge/EdgeLprClient.cs`
- Create: `JPXLpr/Edge/EdgeLprResponseParser.cs`
- Create: `tests/JPXLpr.Tests/EdgeLprClientTests.cs`

**Interfaces:**
- Consumes: `EdgeLprOptions`, `EdgeLprEvent`, cancellation token.
- Produces: `Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token)`.

- [ ] **Step 1: Add failing loopback TCP tests**

```csharp
[Fact]
public async Task Split_ack_is_joined_and_matching_event_is_accepted()
{
    await using EdgeTestServer server = await EdgeTestServer.StartAsync(
        "ACK|11111111-2222-3333-4444-555555555555", splitEveryByte: true);
    EdgeLprClient client = new(server.Options);
    EdgeLprSendResult result = await client.SendAsync(server.Event, CancellationToken.None);
    Assert.True(result.Accepted);
}

[Fact]
public async Task Ack_for_another_event_is_rejected()
{
    await using EdgeTestServer server = await EdgeTestServer.StartAsync(
        "ACK|aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", splitEveryByte: false);
    EdgeLprSendResult result = await new EdgeLprClient(server.Options)
        .SendAsync(server.Event, CancellationToken.None);
    Assert.False(result.Accepted);
    Assert.Equal("ACK_EVENT_MISMATCH", result.Code);
}
```

- [ ] **Step 2: Run the focused tests and confirm RED**

Run: `dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj --filter "Split_ack|Ack_for_another"`

Expected: compile failure because TCP client classes do not exist.

- [ ] **Step 3: Implement TCP send and response parsing**

```csharp
public sealed record EdgeLprSendResult(bool Accepted, string Code, string Message);

public async Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token)
{
    using TcpClient tcp = new();
    await tcp.ConnectAsync(_options.Host, _options.Port, token);
    NetworkStream stream = tcp.GetStream();
    await stream.WriteAsync(EdgeLprFrame.Encode(value.FileName), token);
    string response = await EdgeLprResponseParser.ReadAsync(stream, token);
    return EdgeLprResponseParser.Match(response, value.EventId);
}
```

Use a 10-second linked cancellation token. Read until newline, socket close, or a complete ACK/NACK token is recognized; cap response length at 1024 bytes.

- [ ] **Step 4: Run Task 2 tests**

Run: `dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj`

Expected: all JPXLpr tests pass.

- [ ] **Step 5: Commit Task 2**

```bat
git add JPXLpr\Edge tests\JPXLpr.Tests\EdgeLprClientTests.cs
git commit -m "feat: send JPXLPR events to EdgeService"
```

### Task 3: Durable LPR Outbox

**Files:**
- Create: `JPXLpr/Edge/EdgeLprOutbox.cs`
- Create: `JPXLpr/Edge/EdgeLprOutboxItem.cs`
- Create: `tests/JPXLpr.Tests/EdgeLprOutboxTests.cs`

**Interfaces:**
- Consumes: `EdgeLprEvent`, `IEdgeLprSender`, JSON path.
- Produces: `Start()`, `Enqueue(EdgeLprEvent)`, `StopAsync()`, durable retry and ACK removal.

- [ ] **Step 1: Add failing persistence and retry tests**

```csharp
[Fact]
public async Task Enqueue_is_persisted_before_sender_runs_and_reloads_after_restart()
{
    string path = TempFile();
    BlockingSender sender = new();
    await using (EdgeLprOutbox first = new(path, sender))
    {
        first.Enqueue(TestEvent(101));
        Assert.Single(JsonConvert.DeserializeObject<List<EdgeLprOutboxItem>>(File.ReadAllText(path))!);
    }
    await using EdgeLprOutbox second = new(path, new AcceptingSender());
    Assert.Single(second.Snapshot());
}

[Fact]
public void Four_camera_events_keep_distinct_identity_and_event_ids()
{
    EdgeLprOutbox outbox = CreateStoppedOutbox();
    for (int camera = 1; camera <= 4; camera++) outbox.Enqueue(TestEvent(100 + camera));
    Assert.Equal(4, outbox.Snapshot().Select(x => x.Event.EventId).Distinct().Count());
    Assert.Equal(4, outbox.Snapshot().Select(x => x.Event.FileName).Distinct().Count());
}
```

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj --filter "Enqueue_is_persisted|Four_camera"`

Expected: compile failure because outbox classes do not exist.

- [ ] **Step 3: Implement atomic JSON persistence and one worker**

Persist to `EdgeLprOutbox.json.tmp`, then replace `EdgeLprOutbox.json`. Store EventId, filename, created time, retry count, and last error. The worker removes only an accepted matching ACK; otherwise it increments retry metadata and waits up to five seconds with cancellation support.

```csharp
public interface IEdgeLprSender
{
    Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token);
}
```

- [ ] **Step 4: Run Task 3 tests**

Run: `dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj`

Expected: all JPXLpr tests pass.

- [ ] **Step 5: Commit Task 3**

```bat
git add JPXLpr\Edge tests\JPXLpr.Tests\EdgeLprOutboxTests.cs
git commit -m "feat: persist JPXLPR Edge outbox"
```

### Task 4: Integrate Camera Recognition With Edge Outbox

**Files:**
- Modify: `JPXLpr/HelpClass/CameraInfo.cs`
- Modify: `JPXLpr/caminfo.json`
- Modify: `JPXLpr/BaseClass/JPXConfig.cs`
- Modify: `JPXLpr/App.config`
- Modify: `JPXLpr/JPXLpr.cs`
- Modify only if controls become unused: `JPXLpr/JPXLpr.Designer.cs`, `JPXLpr/JPXLpr.resx`
- Create: `tests/JPXLpr.Tests/CameraEdgeOptionsTests.cs`

**Interfaces:**
- Consumes: existing recognition callback and saved image path.
- Produces: one persisted `EdgeLprEvent` per accepted recognition.

- [ ] **Step 1: Add failing camera configuration mapping tests**

```csharp
[Fact]
public void Entry_camera_maps_site_group_lane_and_device()
{
    CAMINFO camera = new() { laneid = 9010, devicenum = 101, direction = "Entry" };
    EdgeLprOptions value = CameraEdgeOptions.Create(9001, 2, "127.0.0.1", 29200, camera);
    Assert.Equal(new[] { 9001, 2, 9010, 101 },
        new[] { value.Sitenum, value.Groupnum, value.Laneid, value.Devicenum });
    Assert.Equal("Entry", value.Direction);
}
```

- [ ] **Step 2: Run focused test and confirm RED**

Run: `dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj --filter "Entry_camera_maps"`

Expected: compile failure because camera mapping and new CAMINFO fields do not exist.

- [ ] **Step 3: Add current configuration only**

`App.config` keeps:

```xml
<add key="SITENUM" value="9001" />
<add key="GROUPNUM" value="2" />
<add key="EDGESERVICEHOST" value="127.0.0.1" />
<add key="EDGESERVICEPORT" value="29200" />
<add key="DUMMYTEST" value="true" />
<add key="DEBUGMODE" value="true" />
```

Add `laneid`, `devicenum`, and `direction` to each camera entry in `caminfo.json`; do not retain LDM address/port fields.

- [ ] **Step 4: Replace legacy result handling in the form**

Initialize one `EdgeLprOutbox` on load. At the existing point where recognition and image saving have succeeded, construct `EdgeLprEvent` from the camera config and enqueue it. On form close, stop and dispose the outbox before closing camera resources.

- [ ] **Step 5: Run JPXLpr tests and build the project**

Run:

```bat
dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj
dotnet build JPXLpr\JPXLpr.csproj
```

Expected: tests and build pass.

- [ ] **Step 6: Commit Task 4**

```bat
git add JPXLpr tests\JPXLpr.Tests
git commit -m "feat: route JPXLPR recognition through EdgeService"
```

### Task 5: Delete Legacy Communication and Device Control

**Files:**
- Delete: `JPXLpr/Tcp/RestHelper.cs`
- Delete: `JPXLpr/Tcp/LocalSocket.cs`
- Delete: `JPXLpr/Tcp/UDPSocket.cs`
- Delete: `JPXLpr/CarInWorker.cs`
- Delete: `JPXLpr/LprResultWorker.cs`
- Delete: `JPXLpr/LDM/Constants.cs`
- Delete: `JPXLpr/LDM/LDMDisplayClient.cs`
- Delete: `JPXLpr/LDM/LDMDisplayManager.cs`
- Delete: `JPXLpr/LDMShow.cs`
- Delete: `JPXLpr/LDMShow.Designer.cs`
- Delete: `JPXLpr/LDMShow.resx`
- Delete: `JPXLpr/HelpClass/BaseRequest.cs`
- Delete: `JPXLpr/HelpClass/CarInRequest.cs`
- Delete: `JPXLpr/HelpClass/CarInResponse.cs`
- Delete: `JPXLpr/DbModels/` entire directory
- Delete when unreferenced: `JPXLpr/BaseClass/ApsCmd.cs`, `JPXLpr/BaseClass/ParkHeader.cs`, `JPXLpr/BaseClass/StructHelper.cs`
- Modify: `JPXLpr/JPXLpr.cs`
- Modify: `JPXLpr/JPXLpr.csproj`

**Interfaces:**
- Consumes: completed Task 4 Edge-only flow.
- Produces: a JPXLPR project with no REST, UDP, APS, LDM, barrier, or DB-model dependencies.

- [ ] **Step 1: Remove main-form legacy fields, startup, handlers, menus, and shutdown code**

Delete `_udpSocket`, `_localSocket`, `_displays`, `_carInWorker`, `_lprResultWorker`, reconnection handlers, `CarInCompleted`, `CarOutCompleted`, UDP packet handling, LocalSocket handling, LDM timers, gate commands, and the LDM context-menu entry.

- [ ] **Step 2: Delete unreferenced legacy files**

Use `rg` before deleting optional base classes:

```bat
rg -n "ApsCmd|ParkHeader|StructHelper" JPXLpr --glob "*.cs"
```

Delete each optional file only when the only hit is the file itself or a file already being deleted.

- [ ] **Step 3: Prove legacy dependencies are absent**

Run:

```bat
rg -n "UDPSocket|LocalSocket|RestHelper|CarInWorker|LprResultWorker|LDMDisplay|LDMShow|APSMain\.DbModels|APIURI|APSUSE|APSPORT|HOSTPORT" JPXLpr
```

Expected: no matches.

- [ ] **Step 4: Build JPXLPR**

Run: `dotnet build JPXLpr\JPXLpr.csproj`

Expected: build succeeds with no legacy namespace errors.

- [ ] **Step 5: Commit Task 5**

```bat
git add -A JPXLpr
git commit -m "refactor: remove JPXLPR legacy device paths"
```

### Task 6: Final Verification and Documentation

**Files:**
- Modify: `docs/04-development-status.md`
- Create: `run-jpxlpr-edge-test.bat`

**Interfaces:**
- Consumes: completed Edge-only JPXLPR.
- Produces: one top-level batch file for final build, test, service startup, JPXLPR startup, and manual ACK verification.

- [ ] **Step 1: Add the top-level batch flow**

The batch must set `PARKING_RUNTIME_CONNECTION`, validate `parking000test`, require `EDGE_SITE_AUTH_KEY`, start Parking.Api, Parking.EdgeGateway, and Parking.EdgeService with `pause` between stages, synchronize site 9001, then launch JPXLPR. It must run the full solution tests once before program startup.

- [ ] **Step 2: Update development status**

Record removed JPXLPR legacy paths, the EdgeService TCP protocol, configuration keys, camera identity, and verification command without reintroducing `PARKING_TEST_CONNECTION`.

- [ ] **Step 3: Run final verification once**

Run:

```bat
dotnet build ParkingSystem.sln
dotnet build JPXLpr\JPXLpr.csproj
dotnet test ParkingSystem.sln --no-build
dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj --no-build
```

Expected: every build and test exits 0.

- [ ] **Step 4: Run actual local integration**

Run: `run-jpxlpr-edge-test.bat`

Expected: JPXLPR sends an Entry or Exit filename to EdgeService port 29200, receives `ACK|EventId`, and EdgeService logs the matching site/group/lane/devicenum.

- [ ] **Step 5: Commit final runner and status**

```bat
git add run-jpxlpr-edge-test.bat docs\04-development-status.md
git commit -m "docs: add JPXLPR Edge integration runner"
```
