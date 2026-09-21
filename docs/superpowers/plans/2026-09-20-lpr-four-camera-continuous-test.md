# LPR Four-Camera Continuous Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a repeatable TCP test tool that holds one to four LPR camera connections, sends continuous CP949 STX/ETX frames, verifies every ACK, and reports a failing process exit code for any loss or protocol error.

**Architecture:** A new .NET 9 console project contains argument parsing, the LPR wire protocol, one connection runner per camera, and an aggregate coordinator. Unit tests cover pure parsing/protocol/result rules, while a loopback TCP integration test proves four concurrent persistent connections and repeated request/reply framing. A root Windows batch file starts the real stack with pauses and then runs the tool.

**Tech Stack:** .NET 9, C#, `TcpClient`, CP949 via `System.Text.Encoding.CodePages`, xUnit, Windows batch.

**Spec:** `docs/superpowers/specs/2026-09-20-lpr-four-camera-continuous-test-design.md`

## Global Constraints

- Support one to four camera connections only.
- Use the existing STX/ETX and CP949 LPR wire format.
- Do not reference `Parking.EdgeService` from the test tool.
- Return 0 only when every sent event receives a valid ACK containing an EventId.
- Keep connection strings out of source control and do not override the existing EdgeService data directory.
- Keep PascalCase for C# public members.

## Review Focus

- A server that closes a connection between frames must be counted as a connection failure and produce exit code 2.
- A partial response split across multiple TCP reads must still produce one complete ACK.
- A response containing two frames in one read must not corrupt the next request's response.
- Duplicate or blank device numbers must be rejected before opening TCP connections.
- Cancellation during delay or network I/O must close all clients and return a controlled failure instead of hanging.

---

### Task 1: Options and LPR wire protocol

**Files:**
- Create: `src/Tools/Parking.LprStressTester/Parking.LprStressTester.csproj`
- Create: `src/Tools/Parking.LprStressTester/LprStressOptions.cs`
- Create: `src/Tools/Parking.LprStressTester/LprWireProtocol.cs`
- Create: `tests/Parking.Api.Tests/LprStressOptionsTests.cs`
- Create: `tests/Parking.Api.Tests/LprWireProtocolTests.cs`

**Interfaces:**
- Produces: `LprStressOptions.Parse(string[] args)`, `LprWireProtocol.EncodeRequest(string)`, `LprReplyParser.Append(ReadOnlySpan<byte>)`.

- [ ] **Step 1: Write failing option tests**

Add tests asserting defaults, explicit values, rejection of zero/five devices, rejection of duplicates, and rejection of nonpositive count/timeout.

- [ ] **Step 2: Run option tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~LprStressOptionsTests`

Expected: FAIL because `LprStressOptions` does not exist.

- [ ] **Step 3: Implement immutable options and parser**

Use exact defaults from the spec. `Parse` returns an options value on success and throws `ArgumentException` with a user-readable Korean message for invalid input. Normalize `--devices` to distinct positive integers and preserve input order.

- [ ] **Step 4: Run option tests and verify GREEN**

Run the Task 1 option test command; expected all pass.

- [ ] **Step 5: Write failing protocol tests**

Assert request bytes are `0x02 + CP949 filename + 0x03`; feed an ACK one byte at a time; feed `ACK|<32 hex>`, `NAK|<32 hex>|INVALID_LANE`, a malformed payload, and two frames in one append.

- [ ] **Step 6: Run protocol tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~LprWireProtocolTests`

Expected: FAIL because protocol types do not exist.

- [ ] **Step 7: Implement request and reply framing**

Register `CodePagesEncodingProvider`, use encoding 949 with exception fallbacks, retain unread response bytes between `Append` calls, and expose `LprReply(bool Acknowledged, Guid? EventId, string? ErrorCode, bool Valid)`.

- [ ] **Step 8: Run all Task 1 tests and commit**

Run both Task 1 filters; expected all pass.

Commit: `feat: add LPR stress options and protocol`

### Task 2: Concurrent camera runner

**Files:**
- Create: `src/Tools/Parking.LprStressTester/LprCameraRunner.cs`
- Create: `src/Tools/Parking.LprStressTester/LprStressCoordinator.cs`
- Create: `src/Tools/Parking.LprStressTester/LprFileNameFactory.cs`
- Create: `tests/Parking.Api.Tests/LprStressCoordinatorTests.cs`

**Interfaces:**
- Consumes: Task 1 protocol and options.
- Produces: `Task<LprCameraResult> LprCameraRunner.RunAsync(...)` and `Task<LprStressResult> LprStressCoordinator.RunAsync(...)`.

- [ ] **Step 1: Write failing loopback integration tests**

Start a loopback `TcpListener` on an ephemeral port. Accept four clients concurrently, parse every STX/ETX request, and return a unique ACK EventId. Assert four device results, `count * 4` sent and acknowledged, zero failures, and success. Add cases for split ACK, server disconnect, NAK, malformed response, and cancellation.

- [ ] **Step 2: Run coordinator tests and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~LprStressCoordinatorTests`

Expected: FAIL because coordinator types do not exist.

- [ ] **Step 3: Implement deterministic filenames**

Generate `site_group_device_lane_Entry_yyyyMMddHHmmssfff_car_uuid.jpg` using three-digit device numbers and a distinct Korean vehicle number per camera/sequence. Keep the generated filename within the existing parser contract.

- [ ] **Step 4: Implement one persistent runner per camera**

Connect with the configured timeout, write one request, read until exactly one reply is parsed, then delay before the next request. Keep extra parsed frames queued. Count Sent, Ack, Nak, Timeout, ProtocolError, and ConnectionError; dispose the client in all paths.

- [ ] **Step 5: Implement aggregate coordinator**

Create all camera tasks before awaiting them. Sum counters and set `Success` only when `Sent == Ack == devices * count` and every other counter is zero.

- [ ] **Step 6: Run coordinator tests and verify GREEN**

Run the Task 2 test command; expected all pass without hangs.

- [ ] **Step 7: Run Task 1 and Task 2 tests and commit**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~LprStress"`

Expected: all pass.

Commit: `feat: add concurrent LPR camera stress runner`

### Task 3: Console entry point and solution integration

**Files:**
- Create: `src/Tools/Parking.LprStressTester/Program.cs`
- Modify: `ParkingSystem.sln`
- Test: `tests/Parking.Api.Tests/LprStressOptionsTests.cs`

**Interfaces:**
- Consumes: Task 2 coordinator.
- Produces: console exit codes 0, 1, and 2 specified by the design.

- [ ] **Step 1: Add failing result-to-exit-code tests**

Assert invalid arguments map to 1, complete ACK results map to 0, and any nonzero failure counter maps to 2 through a pure `LprStressExitCode.FromResult` method.

- [ ] **Step 2: Run exit-code tests and verify RED**

Run the LPR stress test filter; expected FAIL because `LprStressExitCode` does not exist.

- [ ] **Step 3: Implement entry point and reporting**

Parse options, attach Ctrl+C cancellation, run the coordinator, print one line per camera plus one total line, and return the mapped exit code. Print argument errors and usage without a stack trace.

- [ ] **Step 4: Add the project to the solution and verify GREEN**

Run: `dotnet sln ParkingSystem.sln add src/Tools/Parking.LprStressTester/Parking.LprStressTester.csproj`

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~LprStress"`

Expected: all pass.

- [ ] **Step 5: Build the tool and commit**

Run: `dotnet build src/Tools/Parking.LprStressTester/Parking.LprStressTester.csproj`

Expected: build succeeds with zero errors.

Commit: `feat: add LPR stress tester console`

### Task 4: Windows batch runner and documentation

**Files:**
- Create: `run-lpr-four-camera-test.bat`
- Modify: `docs/04-development-status.md`

**Interfaces:**
- Consumes: `PARKING_TEST_CONNECTION`, existing site 9001/group 2 configuration, and optional `LPR_TEST_*` environment overrides.
- Produces: one root batch runner with a pause between server launches and after the test result.

- [ ] **Step 1: Create the root batch file**

Fail before launching when `PARKING_TEST_CONNECTION` is empty. Map it to `ConnectionStrings__ParkingDatabase`, set defaults for host, port, site, group, lane, devices, count, interval, and timeout only when their `LPR_TEST_*` variables are absent. Start API, Gateway, and EdgeService in separate `cmd /k` windows with a `pause` after each launch. Run the stress tester in the current window and preserve `%ERRORLEVEL%`.

- [ ] **Step 2: Add database verification and pauses**

Capture the UTC start time before the stress run. After the run, query `parking_session` for site/group entry rows created since that time when `mysql.exe` is available; otherwise print a clear skip message. Pause after test output and before final exit. Return the stress tester's saved exit code.

- [ ] **Step 3: Update development status**

Document the new project, command-line defaults, batch filename, expected `80/80 ACK` result, and the fact that real-device verification remains pending until the Windows test is run.

- [ ] **Step 4: Run complete verification**

Run: `dotnet build ParkingSystem.sln`

Run: `dotnet test ParkingSystem.sln`

Expected: both succeed with zero failed tests.

- [ ] **Step 5: Commit**

Commit: `test: add four-camera LPR continuous runner`
