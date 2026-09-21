# Local Configuration Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make each Edge site own and edit its complete local configuration in SQLite while synchronizing versioned changes with the central server.

**Architecture:** EdgeService exclusively owns `edge.db` and exposes local configuration APIs to EdgeManager and field programs. Configuration writes and sync-outbox writes share a SQLite transaction; Gateway and Central API persist a site-scoped copy and exchange versioned snapshots.

**Tech Stack:** .NET 9, ASP.NET Core, WinForms, SQLite, Dapper, Newtonsoft.Json, xUnit

**Spec:** `docs/superpowers/specs/2026-09-20-local-configuration-image-server-design.md`

## Global Constraints

- One Edge installation stores exactly one administrator-entered `sitenum`.
- Initial fields are central server URL, image server URL, `sitenum`, and site authentication key.
- Local SQLite is authoritative during central-server outages.
- Every read and write is scoped by `sitenum`.
- Configuration names retain the existing `SiteId`, `Groupnum`, `LaneId`, `DeviceId`, and `DeviceNumber` contract names.
- Programs other than EdgeService never open `edge.db` directly.

## Review Focus

- Empty database: EdgeService must report setup-required instead of silently using site `0`.
- Wrong site key: central registration must reject the request without replacing local settings.
- Interrupted synchronization: the pending configuration change must remain in the outbox.
- Simultaneous edits: older version/timestamp must not overwrite a newer value.
- Cross-site request: credentials for one site must never read or modify another site.

---

### Task 1: Bootstrap settings and versioned contracts

**Files:**
- Create: `src/BuildingBlocks/Parking.Contracts/EdgeConfigurationModels.cs`
- Create: `src/Edge/Parking.EdgeService/LocalBootstrapStore.cs`
- Create: `tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj`
- Create: `tests/Parking.EdgeService.Tests/LocalBootstrapStoreTests.cs`
- Modify: `src/Edge/Parking.EdgeService/Program.cs`

**Interfaces:**
- Produces: `EdgeBootstrapSettings`, `VersionedSiteConfiguration`, `LocalBootstrapStore.GetAsync`, `LocalBootstrapStore.SaveAsync`
- Consumes: existing SQLite connection string construction in `Program.cs`

- [ ] **Step 1: Write the failing bootstrap persistence tests**

Test that an empty temporary SQLite database returns `null`, saving valid values survives a new store instance, and `SiteId <= 0`, blank URLs, or blank authentication key are rejected.

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj --filter FullyQualifiedName~LocalBootstrapStoreTests`

Expected: FAIL because `LocalBootstrapStore` and `EdgeBootstrapSettings` do not exist.

- [ ] **Step 3: Add the contracts and SQLite store**

Define:

```csharp
public sealed record EdgeBootstrapSettings(
    long SiteId,
    string CentralServerUrl,
    string ImageServerUrl,
    string SiteAuthKey,
    DateTimeOffset UpdatedAtUtc);

public sealed record VersionedSiteConfiguration(
    SiteConfiguration Configuration,
    long Version,
    DateTimeOffset UpdatedAtUtc,
    bool Deleted = false);
```

Create a single-row `edge_bootstrap` table and validate absolute HTTP/HTTPS URLs before saving. Register and initialize the store in `Program.cs`.

- [ ] **Step 4: Run the focused test and full EdgeService suite**

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bat
git add src\BuildingBlocks\Parking.Contracts src\Edge\Parking.EdgeService tests\Parking.EdgeService.Tests
git commit -m "feat: add Edge bootstrap settings store"
```

### Task 2: Replace snapshot JSON with editable local configuration records

**Files:**
- Modify: `src/Edge/Parking.EdgeService/LocalConfigurationStore.cs`
- Create: `src/Edge/Parking.EdgeService/LocalConfigurationService.cs`
- Create: `tests/Parking.EdgeService.Tests/LocalConfigurationStoreTests.cs`
- Create: `tests/Parking.EdgeService.Tests/LocalConfigurationServiceTests.cs`

**Interfaces:**
- Consumes: `EdgeBootstrapSettings.SiteId`, existing `SiteConfiguration` records
- Produces: `GetAsync`, `SaveSiteAsync`, `SaveLaneAsync`, `DeleteLaneAsync`, `SaveDeviceAsync`, `DeleteDeviceAsync`, `SaveDeviceLinkAsync`, `DeleteDeviceLinkAsync`, `SaveOperationVariableAsync`, `DeleteOperationVariableAsync`

- [ ] **Step 1: Write failing CRUD and isolation tests**

Exercise site, lane, device, link, and operation-variable insert/update/delete. Assert that duplicate `DeviceNumber` inside one site is rejected and a different `SiteId` cannot be written through the local service.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj --filter "FullyQualifiedName~LocalConfiguration"`

Expected: FAIL because the normalized tables and service methods do not exist.

- [ ] **Step 3: Implement normalized SQLite tables and transactions**

Create tables `local_site`, `local_lane`, `local_device`, `local_device_link`, and `local_operation_variable`. Include `site_id`, `version`, `updated_at_utc`, and `deleted` where synchronization requires them. Rebuild `SiteConfiguration` from active rows in `GetAsync`.

- [ ] **Step 4: Implement the site-bound service**

The service loads the bootstrap `SiteId`, rejects mismatched request data, increments the local version inside the write transaction, and returns the resulting `VersionedSiteConfiguration`.

- [ ] **Step 5: Run focused and complete tests**

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj`

Expected: PASS with no warning or skipped test.

- [ ] **Step 6: Commit**

```bat
git add src\Edge\Parking.EdgeService tests\Parking.EdgeService.Tests
git commit -m "feat: add editable local site configuration"
```

### Task 3: Local configuration management API

**Files:**
- Create: `src/Edge/Parking.EdgeService/LocalConfigurationController.cs`
- Modify: `src/Edge/Parking.EdgeService/ManagementController.cs`
- Modify: `src/Edge/Parking.EdgeService/Program.cs`
- Create: `tests/Parking.EdgeService.Tests/LocalConfigurationEndpointTests.cs`

**Interfaces:**
- Consumes: `LocalBootstrapStore`, `LocalConfigurationService`
- Produces: `GET/PUT /api/v1/local/setup`, `GET /api/v1/local/config`, and typed lane/device/link/variable CRUD routes

- [ ] **Step 1: Write failing endpoint tests**

Assert that `GET /api/v1/local/setup` returns `404` before setup, valid `PUT` returns `200`, malformed URL returns `400`, and all configuration mutation routes reject a mismatched site.

- [ ] **Step 2: Run endpoint tests and verify RED**

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj --filter FullyQualifiedName~LocalConfigurationEndpointTests`

Expected: FAIL with missing routes.

- [ ] **Step 3: Implement explicit routes**

Use separate endpoints for lanes, devices, device links, and operation variables. Return `409 Conflict` for duplicate keys and `404 NotFound` for missing targets. Never accept a `SiteId` different from bootstrap setup.

- [ ] **Step 4: Remove runtime dependence on `Edge:SiteId`**

Update `EdgeManagementService`, `EdgeController`, `LprLaneProcessor`, `KioskExitCoordinator`, `DisplayBoardOutput`, and `ConfigurationSyncWorker` to obtain the site from `LocalBootstrapStore`.

- [ ] **Step 5: Run EdgeService tests**

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```bat
git add src\Edge\Parking.EdgeService tests\Parking.EdgeService.Tests
git commit -m "feat: expose local configuration management API"
```

### Task 4: Versioned central synchronization

**Files:**
- Modify: `src/Edge/Parking.EdgeService/SqliteOutboxRepository.cs`
- Modify: `src/Edge/Parking.EdgeService/ConfigurationSyncWorker.cs`
- Modify: `src/Edge/Parking.EdgeService/GatewayClient.cs`
- Modify: `src/Central/Parking.EdgeGateway/EdgeGatewayController.cs`
- Modify: `src/Central/Parking.EdgeGateway/ParkingApiClient.cs`
- Create: `src/Central/Parking.Api/Features/Configuration/EdgeConfigurationEndpoints.cs`
- Create: `src/Central/Parking.Api/Features/Configuration/EdgeConfigurationRepository.cs`
- Create: `tests/Parking.Api.Tests/EdgeConfigurationSyncTests.cs`
- Create: `tests/Parking.EdgeService.Tests/ConfigurationSyncWorkerTests.cs`

**Interfaces:**
- Consumes: `VersionedSiteConfiguration`, site authentication key
- Produces: `PUT/GET /api/v1/edge/config/sites/{siteId}` with version conflict behavior

- [ ] **Step 1: Write failing central isolation and version tests**

Test valid-site update, wrong-key rejection, cross-site rejection, newer update acceptance, and older update `409 Conflict` without data replacement.

- [ ] **Step 2: Write failing Edge outbox retry test**

Make the fake Gateway fail once; assert the configuration outbox remains pending. Succeed on the next call; assert it is completed.

- [ ] **Step 3: Run both focused suites and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~EdgeConfigurationSyncTests`

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj --filter FullyQualifiedName~ConfigurationSyncWorkerTests`

Expected: FAIL because versioned upload endpoints are absent.

- [ ] **Step 4: Implement central storage and authenticated Gateway relay**

Store one current version per site. Gateway passes `X-Site-Key` and never substitutes a site from request content. Central returns its current version on conflict.

- [ ] **Step 5: Add configuration outbox event handling**

Write configuration mutation and `ConfigurationChanged` outbox record in one SQLite transaction. Sync newest local version, retain failures, and apply a newer central version only when it wins the version/timestamp comparison.

- [ ] **Step 6: Run all affected tests**

Run: `dotnet test tests/Parking.EdgeService.Tests/Parking.EdgeService.Tests.csproj`

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj`

Expected: PASS.

- [ ] **Step 7: Commit**

```bat
git add src\Edge\Parking.EdgeService src\Central tests
git commit -m "feat: synchronize versioned site configuration"
```

### Task 5: EdgeManager setup and configuration editing

**Files:**
- Modify: `src/Edge/Parking.EdgeManager.Core/IEdgeManagementClient.cs`
- Modify: `src/Edge/Parking.EdgeManager.Core/EdgeManagementClient.cs`
- Create: `src/Edge/Parking.EdgeManager/SetupForm.cs`
- Create: `src/Edge/Parking.EdgeManager/ConfigurationForm.cs`
- Modify: `src/Edge/Parking.EdgeManager/MainForm.cs`
- Modify: `src/Edge/Parking.EdgeManager/Program.cs`
- Create: `tests/Parking.EdgeManager.Tests/EdgeConfigurationPresenterTests.cs`

**Interfaces:**
- Consumes: local setup and configuration APIs from Task 3
- Produces: first-run setup UI and site/lane/device/link/variable CRUD UI

- [ ] **Step 1: Write failing presenter tests**

Test first-run setup-required state, URL/site/key validation, device CRUD refresh, and failed save preserving the edited values for retry.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj --filter FullyQualifiedName~EdgeConfigurationPresenterTests`

Expected: FAIL because setup/configuration presenters are absent.

- [ ] **Step 3: Implement API client and presenters**

Keep HTTP and validation out of WinForms event handlers. Return readable Korean validation messages for missing server URL, image URL, site number, and authentication key.

- [ ] **Step 4: Implement first-run and configuration forms**

Show `SetupForm` when local setup returns `404`. After setup succeeds, open `MainForm`. Add a configuration button that opens tabbed site, lane, device, device-link, and variable editors.

- [ ] **Step 5: Run tests and manual smoke test**

Run: `dotnet test tests/Parking.EdgeManager.Tests/Parking.EdgeManager.Tests.csproj`

Run: `dotnet run --project src\Edge\Parking.EdgeManager\Parking.EdgeManager.csproj`

Expected: empty DB opens setup; saved setup survives restart; device CRUD immediately updates the configuration tree.

- [ ] **Step 6: Update status documentation and commit**

```bat
git add src\Edge\Parking.EdgeManager src\Edge\Parking.EdgeManager.Core tests\Parking.EdgeManager.Tests docs\04-development-status.md
git commit -m "feat: manage local configuration in EdgeManager"
```

### Task 6: Full foundation verification

**Files:**
- Modify only files required by failures found in this task

**Interfaces:**
- Consumes: Tasks 1-5
- Produces: verified local configuration foundation

- [ ] **Step 1: Run the complete solution test suite**

Run: `dotnet test ParkingSystem.sln`

Expected: PASS with zero failed tests.

- [ ] **Step 2: Run the sequential local test launcher**

Use the existing batch launcher pattern: start Central API, Gateway, EdgeService, then EdgeManager with `pause` between programs and all required connection strings/environment values set before launch.

- [ ] **Step 3: Verify offline operation**

Stop Gateway, edit one device, confirm local reads use the new device and sync remains pending; restart Gateway and confirm pending count returns to zero.

- [ ] **Step 4: Commit verification-only corrections**

```bat
git add -A
git commit -m "test: verify local configuration synchronization"
```

