# Device Number Range Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate test site 9001 to type-based device identifiers without dropping or recreating existing database tables or losing operational rows.

**Architecture:** External programs continue sending `sitenum + groupnum + devicenum`; central configuration resolves those values to the new internal `deviceid`. An idempotent MySQL migration clones the four legacy device rows to their new keys, rewrites every known reference, transfers links, removes only superseded device rows, and increments the configuration version. Schema scripts become non-destructive for existing tables.

**Tech Stack:** .NET 8/9, xUnit, MySQL 8, Dapper, WinForms, batch scripts

**Spec:** `docs/superpowers/specs/2026-09-21-device-number-range-migration-design.md`

## Global Constraints

- Use only database `parking000test` and connection environment variable `PARKING_RUNTIME_CONNECTION`.
- Common external identity remains `sitenum + groupnum`; device lookup adds `devicenum` and device type.
- Site 9001/group 2 mappings are KIOSK `2001/201`, entry LPR `4001/401`, exit LPR `4002/402`, and LDM `5001/501`.
- JPXLPR auxiliary cameras use `4003/403` and `4004/404` and remain disabled by default.
- Never use `DROP TABLE`, table rename/recreate, or table-wide data deletion.
- Preserve WinForms `.cs`, `.Designer.cs`, `.resx` structure.
- Do not restore removed JPXLPR REST, UDP, LocalSocket, LDM, barrier, or DB-model code.
- Run the full build and full test suite only after all implementation tasks are complete.

## Review Focus

- A destination ID or number already owned by an unrelated device must abort rather than overwrite that device; Task 2 adds collision assertions.
- Running the migration twice must keep one row per mapped device and preserve session/event counts; Task 2 adds idempotency checks.
- Legacy IDs in nullable in/out columns must migrate without turning nulls into values; Task 2 checks both populated and null references.
- EdgeService local configuration may contain old IDs until the sync version changes; Task 2 asserts the version increment and Task 4 verifies resolved IDs.
- Generic tests that intentionally use arbitrary IDs must not be mechanically rewritten; Task 4 changes only site-9001 contract fixtures and runs focused suites.

---

### Task 1: Make table creation non-destructive

**Files:**
- Create: `tests/Parking.Api.Tests/DatabaseScriptSafetyTests.cs`
- Modify: `database/mysql/001_initial.sql`
- Modify: `database/mysql/002_site_configuration.sql`
- Modify: `database/mysql/005_payment.sql`
- Modify: `database/mysql/006_discount.sql`

**Interfaces:**
- Consumes: repository SQL files under `database/mysql`.
- Produces: a source-level invariant that every MySQL table creation is guarded and no script drops a table.

- [ ] **Step 1: Write the failing schema-safety test**

```csharp
using System.Text.RegularExpressions;

namespace Parking.Api.Tests;

public sealed class DatabaseScriptSafetyTests
{
    [Fact]
    public void MySql_scripts_preserve_existing_tables()
    {
        string root = FindRepositoryRoot();
        foreach (string path in Directory.GetFiles(Path.Combine(root, "database", "mysql"), "*.sql"))
        {
            string sql = File.ReadAllText(path);
            Assert.DoesNotMatch(new Regex(@"\bDROP\s+TABLE\b", RegexOptions.IgnoreCase), sql);
            Assert.DoesNotMatch(new Regex(@"\bCREATE\s+TABLE\s+(?!IF\s+NOT\s+EXISTS\b)", RegexOptions.IgnoreCase), sql);
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ParkingSystem.sln")))
            current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("ParkingSystem.sln");
    }
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~DatabaseScriptSafetyTests`

Expected: FAIL on unguarded `CREATE TABLE` statements in `001`, `002`, `005`, and `006`.

- [ ] **Step 3: Guard existing table creation**

Change every statement shaped as:

```sql
CREATE TABLE parking_event (
```

to:

```sql
CREATE TABLE IF NOT EXISTS parking_event (
```

Apply the same exact change to every unguarded table in the four listed scripts. Do not change columns, keys, seed rows, or existing conditional `ALTER TABLE` logic.

- [ ] **Step 4: Run the focused test and verify GREEN**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~DatabaseScriptSafetyTests`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add database/mysql/001_initial.sql database/mysql/002_site_configuration.sql database/mysql/005_payment.sql database/mysql/006_discount.sql tests/Parking.Api.Tests/DatabaseScriptSafetyTests.cs
git commit -m "fix: preserve existing database tables"
```

### Task 2: Add the idempotent device-key migration

**Files:**
- Create: `database/mysql/012_device_number_ranges.sql`
- Create: `tests/Parking.Api.Tests/DeviceNumberMigrationScriptTests.cs`

**Interfaces:**
- Consumes: existing site 9001 device IDs `9101`, `9201`, `9301`, `9401` and their known references.
- Produces: new IDs `4001`, `4002`, `2001`, `5001`, external numbers `401`, `402`, `201`, `501`, and `parking_site_sync.configversion + 1`.

- [ ] **Step 1: Write failing source-contract tests**

```csharp
namespace Parking.Api.Tests;

public sealed class DeviceNumberMigrationScriptTests
{
    [Fact]
    public void Migration_contains_every_device_and_reference_mapping()
    {
        string sql = ReadMigration();
        foreach (string mapping in new[] { "9301, 2001, 201", "9101, 4001, 401", "9201, 4002, 402", "9401, 5001, 501" })
            Assert.Contains(mapping, sql, StringComparison.OrdinalIgnoreCase);
        foreach (string reference in new[]
        {
            "parking_event", "parking_session", "tperiodinout",
            "parking_device_link", "parking_site_sync"
        }) Assert.Contains(reference, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_is_idempotent_and_has_no_table_recreation()
    {
        string sql = ReadMigration();
        Assert.Contains("ON DUPLICATE KEY UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("START TRANSACTION", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SIGNAL SQLSTATE '45000'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("information_schema.tables", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadMigration()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ParkingSystem.sln"))) current = current.Parent;
        return File.ReadAllText(Path.Combine(current!.FullName, "database", "mysql", "012_device_number_ranges.sql"));
    }
}
```

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~DeviceNumberMigrationScriptTests`

Expected: FAIL because `012_device_number_ranges.sql` does not exist.

- [ ] **Step 3: Implement explicit collision guards**

Create a temporary mapping table inside the migration:

```sql
START TRANSACTION;
CREATE TEMPORARY TABLE device_id_map(
    olddeviceid BIGINT PRIMARY KEY,
    newdeviceid BIGINT NOT NULL UNIQUE,
    newdevicenum INT NOT NULL UNIQUE,
    expectedtype VARCHAR(30) NOT NULL,
    laneid BIGINT NULL,
    devicename VARCHAR(100) NOT NULL
);
INSERT INTO device_id_map VALUES
(9301, 2001, 201, 'KIOSK', 9020, '출구무인'),
(9101, 4001, 401, 'LPR',   9010, '입차LPR'),
(9201, 4002, 402, 'LPR',   9020, '출차LPR'),
(9401, 5001, 501, 'LDM',   9020, '출구전광판');
```

Before mutations, use `SIGNAL SQLSTATE '45000'` through a stored temporary procedure when a site-9001 row owns a destination ID or number but its type does not equal `expectedtype`. Drop only the temporary procedure after the check; never drop a persistent table.

- [ ] **Step 4: Implement the data-preserving remap**

The SQL must perform these operations in order:

```sql
UPDATE parking_device d JOIN device_id_map m ON d.deviceid=m.olddeviceid
SET d.devicenum=-m.newdevicenum WHERE d.sitenum=9001;

INSERT INTO parking_device(deviceid,sitenum,laneid,devicenum,devicetype,devicename,ipaddr,port,useflag)
SELECT m.newdeviceid,9001,m.laneid,m.newdevicenum,m.expectedtype,m.devicename,
       old.ipaddr,old.port,COALESCE(old.useflag,1)
FROM device_id_map m LEFT JOIN parking_device old ON old.deviceid=m.olddeviceid
ON DUPLICATE KEY UPDATE laneid=VALUES(laneid),devicenum=VALUES(devicenum),
devicetype=VALUES(devicetype),devicename=VALUES(devicename),useflag=VALUES(useflag);

UPDATE parking_event e JOIN device_id_map m ON e.deviceid=m.olddeviceid SET e.deviceid=m.newdeviceid WHERE e.sitenum=9001;
UPDATE parking_session s JOIN device_id_map m ON s.indeviceid=m.olddeviceid SET s.indeviceid=m.newdeviceid WHERE s.sitenum=9001;
UPDATE parking_session s JOIN device_id_map m ON s.outdeviceid=m.olddeviceid SET s.outdeviceid=m.newdeviceid WHERE s.sitenum=9001;
```

For optional legacy tables such as `tperiodinout`, select a complete update statement or `SELECT 1` through `information_schema.tables`, then execute it with `PREPARE`:

```sql
SET @migration_sql = IF(
    EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema=DATABASE() AND table_name='tperiodinout'),
    'UPDATE tperiodinout p JOIN device_id_map m ON p.indeviceid=m.olddeviceid SET p.indeviceid=m.newdeviceid WHERE p.sitenum=9001',
    'SELECT 1');
PREPARE migration_stmt FROM @migration_sql;
EXECUTE migration_stmt;
DEALLOCATE PREPARE migration_stmt;
```

Repeat that guarded form for `outdeviceid`. Copy device links with mapped source and target IDs using `INSERT ... SELECT ... ON DUPLICATE KEY UPDATE`, delete only links containing old IDs, then delete only the four superseded `parking_device` rows. Increment site 9001 `configversion`, drop the temporary mapping table, and `COMMIT`.

- [ ] **Step 5: Run focused migration tests**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter FullyQualifiedName~DeviceNumberMigrationScriptTests`

Expected: PASS twice against `parking000test` with unchanged operational-row counts.

- [ ] **Step 6: Commit**

```bash
git add database/mysql/012_device_number_ranges.sql tests/Parking.Api.Tests/DeviceNumberMigrationScriptTests.cs
git commit -m "feat: migrate test devices to type number ranges"
```

### Task 3: Update JPXLPR external device numbers

**Files:**
- Modify: `JPXLpr/BaseClass/CameraConfigFile.cs`
- Modify: `JPXLpr/caminfo.json`
- Modify: `tests/JPXLpr.Tests/CameraEdgeOptionsTests.cs`
- Modify: `tests/JPXLpr.Tests/EdgeLprFrameTests.cs`
- Modify: `tests/JPXLpr.Tests/EdgeLprOutboxTests.cs`

**Interfaces:**
- Consumes: camera ordering entry1, entry2, exit1, exit2.
- Produces: device numbers `401`, `403`, `402`, `404`; active entry1/exit1 only.

- [ ] **Step 1: Change tests first**

Update the entry identity expectations to `401` and exit identity expectations to `402`. Add this exact configuration assertion:

```csharp
[Theory]
[InlineData(0, 9010, 401, "Entry")]
[InlineData(1, 9010, 403, "Entry")]
[InlineData(2, 9020, 402, "Exit")]
[InlineData(3, 9020, 404, "Exit")]
public void Camera_defaults_follow_type_ranges(int index, int lane, int device, string direction)
{
    CAMINFO[] values = CameraConfigFile.CreateDefaultsForTest();
    Assert.Equal((lane, device, direction), (values[index].laneid, values[index].devicenum, values[index].direction));
}
```

Expose an internal test seam named `CreateDefaultsForTest()` that returns the same objects as the private default factory; grant `InternalsVisibleTo("JPXLpr.Tests")` if required.

- [ ] **Step 2: Run JPXLPR tests and verify RED**

Run: `dotnet test tests/JPXLpr.Tests/JPXLpr.Tests.csproj`

Expected: FAIL with old `101/102/201/202` expectations.

- [ ] **Step 3: Implement the camera mapping**

Use the fixed ordered array rather than arithmetic that could swap the primary exit camera:

```csharp
int[] deviceNumbers = { 401, 403, 402, 404 };
```

Set `caminfo.json` to the same numbers, keeping cameras 1 and 3 active and cameras 2 and 4 inactive. Do not change IP addresses, recognition settings, or EdgeService framing.

- [ ] **Step 4: Run JPXLPR tests and verify GREEN**

Run: `dotnet test tests/JPXLpr.Tests/JPXLpr.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add JPXLpr/BaseClass/CameraConfigFile.cs JPXLpr/caminfo.json tests/JPXLpr.Tests
git commit -m "fix: assign JPXLPR four hundred device numbers"
```

### Task 4: Update internal device resolution contracts

**Files:**
- Modify: `tests/Parking.Api.Tests/KioskDeviceContractTests.cs`
- Modify: `tests/Parking.Api.Tests/EdgeDeviceResolverTests.cs`
- Modify: `tests/Parking.Api.Tests/LprLaneProcessorTests.cs`
- Modify: `tests/Parking.Api.Tests/ParkingEventRepositoryTests.cs`
- Modify: `tests/Parking.Api.Tests/ParkingExitRepositoryTests.cs`
- Modify: `tests/Parking.Api.Tests/ParkingSearchRepositoryTests.cs`
- Modify: `tests/Parking.Api.Tests/ParkingSearchEndpointTests.cs`
- Modify: `tests/Parking.Api.Tests/ExitEndpointTests.cs`
- Modify: `tests/Parking.EdgeManager.Tests/EdgeManagerListMapperTests.cs`
- Modify: `tests/Parking.Api.Tests/EdgeManagementStoreTests.cs`
- Modify: site-9001-specific LDM/link fixtures under `tests/Parking.Api.Tests`

**Interfaces:**
- Consumes: unchanged resolver APIs and new database mapping.
- Produces: contract coverage for `201 -> 2001`, `401 -> 4001`, `402 -> 4002`, `501 -> 5001`.

- [ ] **Step 1: Update resolver tests before fixtures**

Use these canonical assertions:

```csharp
Assert.Equal(2001, kiosk.DeviceId);
Assert.Equal(4001, entryRecognition.DeviceId);
Assert.Equal(4002, exitRecognition.DeviceId);
Assert.Equal(5001, display.DeviceId);
```

Change only fixtures explicitly modeling site `9001`; leave generic unit tests using arbitrary IDs such as `101`, `201`, `202`, and `301` unchanged.

- [ ] **Step 2: Run focused suites and verify RED**

Run:

```bat
dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~KioskDeviceContractTests|FullyQualifiedName~EdgeDeviceResolverTests|FullyQualifiedName~LprLaneProcessorTests|FullyQualifiedName~ParkingEventRepositoryTests|FullyQualifiedName~ParkingExitRepositoryTests"
dotnet test tests\Parking.EdgeManager.Tests\Parking.EdgeManager.Tests.csproj
```

Expected: FAIL where site-9001 fixtures still contain legacy IDs/numbers.

- [ ] **Step 3: Update site-9001 fixtures**

Apply the exact mapping table from the spec. For LPR filenames, replace entry number `101` with `401` and exit number `201` with `402`; keep KIOSK external number `201`. Update stored `indeviceid/outdeviceid` assertions to `4001/4002`.

- [ ] **Step 4: Run focused suites and verify GREEN**

Run the two commands from Step 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/Parking.Api.Tests tests/Parking.EdgeManager.Tests
git commit -m "test: align device identity range contracts"
```

### Task 5: Align runtime defaults and current documentation

**Files:**
- Modify: `docs/04-development-status.md`
- Modify: current non-historical configuration examples that state site-9001 device IDs
- Verify: `APSMain_C(API)V2/App.config`
- Verify: `src/Edge/Parking.TerminalAgent/appsettings.json`
- Verify: `run-foundations.bat`
- Verify: `run-terminal-agent-test.bat`
- Verify: `run-apsmain-edge-test.bat`
- Verify: `run-jpxlpr-edge-test.bat`

**Interfaces:**
- Consumes: new mapping and unchanged APS/KIOSK external number `201`.
- Produces: operational instructions that distinguish external `devicenum` from internal `deviceid`.

- [ ] **Step 1: Add a configuration regression test**

Extend the existing runtime identity configuration tests to assert:

```csharp
Assert.Contains("--device-number 201", kioskRuntimeText);
Assert.DoesNotContain("--device 2001", kioskRuntimeText);
Assert.Contains("APSNUM", apsConfigText);
```

The KIOSK and APS runtime arguments remain `201`; internal `2001` must never be passed as an external device number.

- [ ] **Step 2: Run runtime configuration tests**

Run: `dotnet test tests/Parking.TerminalAgent.Tests/Parking.TerminalAgent.Tests.csproj --filter FullyQualifiedName~RuntimeIdentityConfigurationTests`

Expected: PASS or fail only where a runtime file still exposes a legacy internal ID.

- [ ] **Step 3: Update current documentation**

Add one canonical mapping table to `docs/04-development-status.md`. Replace current operational statements using `9101/9201/9301/9401` and LPR numbers `101/201`; preserve historical plan/spec files as historical records except where they are linked as current instructions.

- [ ] **Step 4: Verify runtime defaults**

Confirm APSMain and KioskSimulator still use external `201`. Confirm JPXLPR uses `401/402`. Confirm no batch file assigns a DB-internal ID to `APSNUM`, `--device-number`, or a JPXLPR camera number.

- [ ] **Step 5: Commit**

```bash
git add docs/04-development-status.md tests/Parking.TerminalAgent.Tests "APSMain_C(API)V2/App.config" src/Edge/Parking.TerminalAgent/appsettings.json run-*.bat
git commit -m "docs: publish type based device identity ranges"
```

### Task 6: Final verification and handoff

**Files:**
- Verify: all changed files
- Update: `docs/04-development-status.md` only if verification reveals a command/result correction

**Interfaces:**
- Consumes: Tasks 1-5.
- Produces: one reviewed branch and the single Windows execution path for user verification.

- [ ] **Step 1: Run static safety checks**

```bash
git diff --check
rg -n -P "DROP TABLE|CREATE TABLE (?!IF NOT EXISTS)" database/mysql -g "*.sql"
rg -n "9101|9201|9301|9401" JPXLpr src tests run-*.bat docs/04-development-status.md
```

Expected: no unsafe table statements; remaining legacy IDs must be intentionally historical or generic and individually reviewed.

- [ ] **Step 2: Run the complete build and tests together**

```bat
dotnet build ParkingSystem.sln
dotnet build JPXLpr\JPXLpr.csproj
dotnet build tests\JPXLpr.Tests\JPXLpr.Tests.csproj
dotnet test ParkingSystem.sln --no-build
dotnet test tests\JPXLpr.Tests\JPXLpr.Tests.csproj --no-build
```

Expected: all commands exit 0.

- [ ] **Step 3: Run the migration twice and inspect mappings**

```bat
mysql -u root -p parking000test < database\mysql\012_device_number_ranges.sql
mysql -u root -p parking000test < database\mysql\012_device_number_ranges.sql
```

Then query site 9001 and confirm exactly `2001/201`, `4001/401`, `4002/402`, `5001/501`, with unchanged operational row counts.

- [ ] **Step 4: Request whole-branch code review**

Review collision handling, reference coverage, idempotency, and accidental changes to generic test IDs. Fix every Critical and Important finding before upload.

- [ ] **Step 5: Commit any verification-only corrections and upload**

```bash
git status --short
git log --oneline -8
git push origin codex/server-edge-foundation
```

Use the repository connector if credentialless `git push` is unavailable. Do not force-push.
