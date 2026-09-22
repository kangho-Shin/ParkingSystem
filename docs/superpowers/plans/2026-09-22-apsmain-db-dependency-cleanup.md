# APSMain DB 의존성 제거 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** APSMain에서 생성형 EF 모델과 MySQL 직접 연결 코드를 제거하면서 화면과 EdgeService 코드가 쓰는 자료형은 일반 모델로 유지한다.

**Architecture:** 화면 동작은 바꾸지 않고 실제 사용 중인 13개 DB 형식을 `APSMain.Models`의 순수 모델로 옮긴다. 이후 EF Core, Pomelo, MySql.Data, Dapper와 미사용 직접 DB 작업 코드를 제거하고 소스 경계 테스트로 재유입을 막는다. 기존 REST 호출의 EdgeService 완전 전환은 다음 계획에서 처리한다.

**Tech Stack:** .NET 8 WinForms, Newtonsoft.Json, xUnit, EdgeService HTTP/SignalR

**Spec:** `docs/superpowers/specs/2026-09-22-apsmain-edge-only-cleanup-design.md`

## Global Constraints

- 운영 및 시험 DB는 `parking000test` 하나만 사용한다.
- 연결 문자열 환경변수는 `PARKING_RUNTIME_CONNECTION` 하나만 사용한다.
- APSMain은 MySQL에 직접 연결하지 않는다.
- WinForms 폼은 `.cs`, `.Designer.cs`, `.resx` 구조를 유지한다.
- 24인치와 15인치 화면을 함께 유지한다.
- 소스 정리를 마친 뒤 전체 테스트를 한 번에 실행한다.

## Review Focus

- 모델 namespace 변경 후 24인치와 15인치 폼이 모두 빌드되어야 한다.
- APSMain에 EF Core, Pomelo, MySql.Data, Dapper 패키지가 남지 않아야 한다.
- `DbModels`, `UparkdbContext`, `DbJobWorker`가 남지 않아야 한다.
- 예전 DB명과 하드코딩 접속 문자열이 소스에 남지 않아야 한다.
- `EdgeParkingMapper`가 `Tparkinfo`를 계속 정상 변환해야 한다.

---

### Task 1: DB 경계 회귀 테스트 추가

**Files:**
- Create: `tests/Parking.TerminalAgent.Tests/APSMainSourceBoundaryTests.cs`

**Interfaces:**
- Consumes: 저장소의 `APSMain_C(API)V2` 소스와 프로젝트 파일
- Produces: DB 직접 연결 의존성 재유입을 막는 xUnit 테스트

- [ ] **Step 1: 실패하는 테스트 작성**

```csharp
namespace Parking.TerminalAgent.Tests;

public sealed class APSMainSourceBoundaryTests
{
    [Fact]
    public void APSMain은_DB_직접연결_패키지를_사용하지_않는다()
    {
        string root = FindRepositoryRoot();
        string project = File.ReadAllText(Path.Combine(root, "APSMain_C(API)V2", "APSMain.csproj"));
        foreach (string name in new[] { "EntityFrameworkCore", "Pomelo", "MySql.Data", "Dapper" })
            Assert.DoesNotContain(name, project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void APSMain에는_DB_생성모델과_직접작업자가_없다()
    {
        string root = FindRepositoryRoot();
        string aps = Path.Combine(root, "APSMain_C(API)V2");
        Assert.False(Directory.Exists(Path.Combine(aps, "DbModels")));
        Assert.False(File.Exists(Path.Combine(aps, "BaseClass", "DbJobWorker.cs")));
    }

    [Fact]
    public void APSMain에는_하드코딩_DB_접속정보가_없다()
    {
        string root = FindRepositoryRoot();
        string aps = Path.Combine(root, "APSMain_C(API)V2");
        string text = string.Join("\n", Directory.EnumerateFiles(aps, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj" or ".config")
            .Select(File.ReadAllText));
        Assert.DoesNotContain("database=ipims", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database=uparkdb", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MySqlConnection", text, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ParkingSystem.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("저장소 최상위 디렉터리를 찾을 수 없습니다.");
    }
}
```

- [ ] **Step 2: 정리 전 실패 확인**

Run: `dotnet test tests\Parking.TerminalAgent.Tests\Parking.TerminalAgent.Tests.csproj --filter FullyQualifiedName~APSMainSourceBoundaryTests`

Expected: DB 패키지, `DbModels`, `DbJobWorker`, 하드코딩 접속정보 검출로 FAIL.

### Task 2: 사용 중인 모델 13개 이동

**Files:**
- Move to `APSMain_C(API)V2/Models/`: `Tbcardinfo.cs`, `Tdiscount.cs`, `Tdiscountinfo.cs`, `Tdiscounttable.cs`, `Tdisperson.cs`, `Tholiday.cs`, `Tparkfee.cs`, `Tparkin.cs`, `Tparkinfo.cs`, `Tparkvariable.cs`, `Tperiodin.cs`, `Tperiodinout.cs`, `Tperiodmember.cs`
- Modify: `using APSMain.DbModels;`가 있는 모든 APSMain 파일
- Test: `tests/APSMain.EdgeIntegration.Tests/EdgeParkingMapperTests.cs`

**Interfaces:**
- Consumes: 기존 13개 클래스의 이름과 공개 속성
- Produces: 같은 클래스명과 속성을 갖는 `APSMain.Models` 일반 모델

- [ ] **Step 1: 13개 파일을 `git mv`로 `Models`에 이동**
- [ ] **Step 2: 각 namespace를 `APSMain.Models`로 변경하되 클래스명과 속성은 유지**
- [ ] **Step 3: 모든 소비 파일의 using을 `APSMain.Models`로 변경**
- [ ] **Step 4: `git grep -n "APSMain.DbModels" -- "APSMain_C(API)V2"` 결과가 없는지 확인**

### Task 3: 생성형 모델과 직접 DB 코드 제거

**Files:**
- Delete: `APSMain_C(API)V2/DbModels/`의 나머지 파일
- Delete: `APSMain_C(API)V2/BaseClass/DbJobWorker.cs`
- Modify: `APSMain_C(API)V2/APSMain.csproj`, `Program.cs`, `MenuForm.cs`
- Modify: DB 관련 using 또는 주석이 남은 APSMain `.cs` 파일

**Interfaces:**
- Consumes: Task 2의 `APSMain.Models`
- Produces: DB 드라이버 없이 컴파일되는 APSMain

- [ ] **Step 1: 남은 `DbModels`와 `DbJobWorker.cs` 삭제**
- [ ] **Step 2: csproj에서 Dapper, EF Core Tools, MySql.Data, Pomelo 패키지 및 `DbModels`/`DummyContext` Folder 항목 삭제**
- [ ] **Step 3: `using Dapper`, `using MySql.Data.MySqlClient`, `using Microsoft.EntityFrameworkCore.Metadata.Internal` 삭제**
- [ ] **Step 4: 주석으로 남은 `IDbConnection`, `MySqlConnection`, `QueryAsync`, EF 설치 및 Scaffold 예제 삭제**
- [ ] **Step 5: `git grep -n -E "DbModels|UparkdbContext|EntityFrameworkCore|Pomelo|MySqlConnection|database=(ipims|uparkdb)" -- "APSMain_C(API)V2"` 결과가 없는지 확인**

### Task 4: 빌드와 전체 테스트

**Files:**
- Verify: `APSMain_C(API)V2/APSMain.csproj`
- Verify: `tests/APSMain.EdgeIntegration.Tests/APSMain.EdgeIntegration.Tests.csproj`
- Verify: `tests/Parking.TerminalAgent.Tests/Parking.TerminalAgent.Tests.csproj`

**Interfaces:**
- Consumes: DB 의존성을 제거한 APSMain
- Produces: 레거시 REST 호출을 EdgeService로 전환할 수 있는 정상 기준점

- [ ] **Step 1: `dotnet build "APSMain_C(API)V2\APSMain.csproj" -c Release` 실행, 오류 0 확인**
- [ ] **Step 2: `dotnet test "tests\APSMain.EdgeIntegration.Tests\APSMain.EdgeIntegration.Tests.csproj" -c Release --no-restore` 실행**
- [ ] **Step 3: `dotnet test ParkingSystem.sln -c Release --no-restore`로 전체 테스트 실행**
- [ ] **Step 4: `git status --short`에서 계획된 변경만 있는지 확인**
- [ ] **Step 5: `git commit -m "refactor: remove APSMain database dependencies"`로 커밋**

## 다음 계획

기존 `RestHelper`, `Api/Request`, `Api/Response`, `/api/Carcalc`, `/api/CarPay`, `/api/Carout`, `/api/env/*` 호출을 EdgeService 로컬 API로 전환하고 레거시 파일을 삭제한다.
