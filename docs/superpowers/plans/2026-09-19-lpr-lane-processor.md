# LPR TCP Lane Processor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Receive framed KS5601 LPR image file names over TCP, validate them against cached lane/device configuration, route them through the existing entry/exit flow, and return correlated ACK or NAK frames.

**Architecture:** A connection-local FSM extracts STX/ETX frames without knowing parking rules. A strict image-file-name parser produces an `LprRecognition` contract, and `LprLaneProcessor` validates cached configuration before calling the existing `EdgeEventService`; `LprTcpWorker` owns listener/client lifetime and ordered replies.

**Tech Stack:** .NET 9, ASP.NET Core hosted services, System.Net.Sockets, code page 949, xUnit

**Spec:** `docs/superpowers/specs/2026-09-19-lpr-lane-processor-design.md`

## Global Constraints

- Request framing is `0x02 + code-page-949 DATA + 0x03`.
- DATA is at most 1,024 bytes and contains exactly one image file name.
- File names contain exactly eight underscore-delimited fields and use a 32-character Guid `N` EventId.
- TCP parsing, file-name parsing, configuration validation, and parking processing remain separate units.
- ACK means the valid request reached the existing parking flow; a blocked exit still receives ACK.
- NAK is limited to framing, encoding, file-name, configuration, direction, or unexpected processing errors.
- Existing entry, exit, Outbox, monitoring, and EdgeManager paths are reused without duplication.
- New tests are accumulated and the user runs `dotnet test ParkingSystem.sln` once after the complete feature is uploaded.

## Review Focus

- Fragmented and coalesced TCP reads must produce exactly one ordered reply per complete frame.
- A second STX inside a partial frame must reset only that connection's current frame.
- Invalid code-page-949 byte sequences must NAK instead of silently introducing replacement characters.
- A valid but unpaid/offline-blocked exit must ACK because the request was processed.
- Listener cancellation and one disconnected client must not stop other LPR connections.

---

### Task 1: LPR contracts and strict image-file-name parser

**Files:**
- Create: `src/BuildingBlocks/Parking.Contracts/LprRecognition.cs`
- Create: `src/Edge/Parking.EdgeService/LprFileNameParser.cs`
- Modify: `src/Edge/Parking.EdgeService/Parking.EdgeService.csproj`
- Test: `tests/Parking.Api.Tests/LprFileNameParserTests.cs`

**Interfaces:**
- Produces: `LprRecognition(Guid EventId, long SiteId, int Groupnum, long DeviceId, long LaneId, string Direction, DateTimeOffset RecognizedAt, string CarNumber, string ImageFileName)`.
- Produces: `LprParseResult(bool Success, LprRecognition? Recognition, string ErrorCode, Guid? EventId)`.
- Produces: `LprFileNameParser.Parse(string fileName)`.

- [ ] **Step 1: Write parser tests first**

Cover a valid Entry and Exit, Korean car number, exactly three numeric digits, invalid date, empty car number, invalid direction, non-`N` Guid, extra underscore, and non-JPEG extension. The valid assertion is:

```csharp
LprParseResult result = parser.Parse(
    "001_002_101_010_Entry_20260919153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.jpg");
Assert.True(result.Success);
Assert.Equal(101, result.Recognition!.DeviceId);
Assert.Equal("12가3456", result.Recognition.CarNumber);
Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(
    new DateTime(2026, 9, 19, 15, 30, 25, 123)), result.Recognition.RecognizedAt.Offset);
```

- [ ] **Step 2: Implement exact parsing**

Use `Path.GetFileName(fileName) == fileName`, `.jpg` case-insensitive, `Split('_')` length 8, three-character ASCII-digit checks for the four IDs, exact `DateTime.TryParseExact("yyyyMMddHHmmssfff", InvariantCulture, DateTimeStyles.None)`, nonempty car number without whitespace, direction constants, and `Guid.TryParseExact(value, "N")`. Attach the local UTC offset with `new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime))`.

- [ ] **Step 3: Run focused tests internally and commit**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~LprFileNameParserTests"`

Expected: PASS.

Commit: `feat: parse lpr image file names`

### Task 2: Connection-local STX/ETX FSM and replies

**Files:**
- Create: `src/Edge/Parking.EdgeService/LprFrameParser.cs`
- Create: `src/Edge/Parking.EdgeService/LprProtocol.cs`
- Test: `tests/Parking.Api.Tests/LprFrameParserTests.cs`

**Interfaces:**
- Produces: `LprFrameParser.Append(ReadOnlySpan<byte>)` returning zero or more `LprFrameResult` values.
- Produces: `LprFrameResult(byte[]? Data, string? ErrorCode)`.
- Produces: `LprProtocol.Decode(byte[])` and `LprProtocol.CreateReply(bool acknowledged, Guid? eventId, string? errorCode)`.

- [ ] **Step 1: Write FSM and encoding tests first**

Test one frame, byte-by-byte fragmentation, two frames in one append, noise before STX, nested STX reset, empty frame, 1,024-byte success, 1,025-byte failure, Korean code-page-949 decoding, invalid byte sequence, and reply bytes. Assert replies begin `0x02`, end `0x03`, and decode to `ACK|<N-guid>` or `NAK||FRAME_TOO_LONG`.

- [ ] **Step 2: Implement the FSM**

Keep `_insideFrame`, a 1,024-byte buffer, and `_overflowed` per parser instance. STX clears current state and starts; ETX emits DATA, `EMPTY_DATA`, or `FRAME_TOO_LONG`; ordinary bytes outside a frame are ignored. Overflow stops buffering until ETX or a new STX.

- [ ] **Step 3: Implement strict code page 949**

Register `CodePagesEncodingProvider.Instance` once and construct encoding 949 with `EncoderFallback.ExceptionFallback` and `DecoderFallback.ExceptionFallback`. Convert decoder fallback exceptions to `INVALID_ENCODING`. Reply payloads use ASCII.

- [ ] **Step 4: Run focused tests internally and commit**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~LprFrameParserTests"`

Expected: PASS.

Commit: `feat: frame lpr tcp packets`

### Task 3: Cached configuration validation and lane processing

**Files:**
- Create: `src/Edge/Parking.EdgeService/LprLaneProcessor.cs`
- Test: `tests/Parking.Api.Tests/LprLaneProcessorTests.cs`

**Interfaces:**
- Consumes: Tasks 1-2 contracts/parser and existing `LocalConfigurationStore`, `EdgeEventService`.
- Produces: `LprProcessResult(bool Acknowledged, Guid? EventId, string? ErrorCode, FieldEventResponse? ParkingResponse)`.
- Produces: `LprLaneProcessor.ProcessAsync(string fileName, CancellationToken)`.

- [ ] **Step 1: Write validation and routing tests first**

Create cached site/lane/device fixtures and cover `INVALID_FILE_NAME`, `INVALID_SITE`, `CONFIG_NOT_READY`, `INVALID_LANE`, `INVALID_DEVICE`, `DIRECTION_MISMATCH`, Entry routing, Exit routing, offline Entry ACK, and blocked Exit ACK. Assert invalid configuration never adds an Outbox row.

- [ ] **Step 2: Implement validation order exactly as the spec**

Load the configured site from `LocalConfigurationStore`. Validate enabled site, lane site/group/enabled, device site/lane/type/enabled, then direction. Compare device type `LPR` and direction case-insensitively. Return the first matching error code.

- [ ] **Step 3: Route to existing services**

Map Entry to `FieldEventRequest` and Exit to `ExitEventRequest` using the full received file name for `InImage` or `OutImage`. Call only `EdgeEventService`. Return ACK after any normal `FieldEventResponse`, regardless of `Accepted` or `OpenBarrier`; catch unexpected exceptions, log them, and return `PROCESSING_ERROR`.

- [ ] **Step 4: Run focused tests internally and commit**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~LprLaneProcessorTests"`

Expected: PASS.

Commit: `feat: process lpr lane recognition`

### Task 4: TCP listener hosted service

**Files:**
- Create: `src/Edge/Parking.EdgeService/LprTcpWorker.cs`
- Modify: `src/Edge/Parking.EdgeService/Program.cs`
- Modify: `src/Edge/Parking.EdgeService/appsettings.json`
- Test: `tests/Parking.Api.Tests/LprTcpWorkerTests.cs`

**Interfaces:**
- Consumes: Task 3 `LprLaneProcessor` and Task 2 framing/reply helpers.
- Produces: configured multi-client TCP input on `Edge:LprListenPort`.

- [ ] **Step 1: Write loopback integration tests first**

Bind a free loopback port, start the worker, connect two `TcpClient` instances, send fragmented Korean Entry on client A and two coalesced frames on client B, and assert each connection receives ordered ACK/NAK replies. Disconnect A and verify B still processes another frame. Cancel the worker and assert the port closes.

- [ ] **Step 2: Implement listener and client loops**

Use one `TcpListener` and accept until cancellation. Create one `LprFrameParser` per client. Read into a 4 KiB buffer, process emitted frames sequentially, decode/route complete data, and write one reply before processing the next frame. Track client tasks so shutdown awaits them; client I/O errors end only that client.

- [ ] **Step 3: Register configuration and hosted service**

Add `"LprListenPort": 29200`. Register `LprFileNameParser`, `LprLaneProcessor`, and `LprTcpWorker`. A port value of 0 returns from the worker without binding.

- [ ] **Step 4: Run focused tests internally and commit**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~LprTcpWorkerTests"`

Expected: PASS.

Commit: `feat: receive lpr results over tcp`

### Task 5: Documentation and combined verification

**Files:**
- Modify: `docs/01-program-overview.md`
- Modify: `docs/02-program-features.md`
- Modify: `docs/03-api-specification.md`
- Modify: `docs/04-development-status.md`

**Interfaces:**
- Documents the TCP packet, image-file-name, ACK/NAK, port, and next display-output priority.

- [ ] **Step 1: Update operational documentation**

Record port 29200, STX/ETX, code page 949, exact file-name example, reply formats, error codes, and `Edge:LprListenPort=0` disable behavior. Mark LPR TCP input and lane routing implemented; set virtual display/barrier output as the next priority.

- [ ] **Step 2: Run one combined user verification**

Run only after every file above is uploaded:

```bat
git pull origin codex/server-edge-foundation
dotnet test ParkingSystem.sln
```

Expected: all tests pass with zero failures.

- [ ] **Step 3: Commit**

Commit: `docs: document lpr tcp processing`
