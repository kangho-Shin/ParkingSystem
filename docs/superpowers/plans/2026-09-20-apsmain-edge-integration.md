# APSMain EdgeService 1차 연동 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** APSMain이 MySQL, LPR, LDM에 직접 연결하지 않고 EdgeService SignalR 사건을 수신해 차량 검색과 단일 요금·복수 후보 결과를 기존 15/24인치 화면 흐름으로 전달한다.

**Architecture:** 기존 WinForms 프로젝트 안에 `Integration/EdgeService` 경계를 추가한다. HTTP와 SignalR 통신, 사건 중복 방지, 폼 전달을 이 경계에 모으고 `DbJobWorker`를 포함한 레거시 DB 코드는 보존하되 신규 경로에서는 참조하지 않는다.

**Tech Stack:** .NET 8, WinForms, `Microsoft.AspNetCore.SignalR.Client`, `System.Net.Http`, Newtonsoft.Json, xUnit

**Spec:** `docs/06-apsmain-edge-integration-design.md`

## Global Constraints

- APSMain은 MySQL에 직접 접속하지 않는다. `DbJobWorker`는 삭제하지 않지만 신규 EdgeService 연동 코드에서 사용하지 않는다.
- 기존 15인치·24인치 WinForms 파일은 `.cs`, `.Designer.cs`, `.resx` 구조를 유지한다.
- APSMain은 출구 LPR 수신, LDM 표시, 차단기 제어를 새 EdgeService 운전모드에서 수행하지 않는다.
- `SITENUM`, `GROUPNUM`, `APSNUM`을 등록하고 EdgeService가 실제 `parking_device.deviceid`로 변환한다.
- UI 이벤트에서는 동기 대기(`GetAwaiter().GetResult()`)를 사용하지 않는다.
- 여러 프로그램 실행시험은 저장소 최상위 배치 파일 하나에 환경변수와 연결 문자열을 포함하고 각 단계 사이에 `pause`를 둔다.

## Review Focus

- 같은 `EventId`가 연속 수신되어도 화면 처리는 한 번만 시작되고, 실패한 사건은 다시 처리할 수 있어야 한다.
- SignalR 재연결 뒤 `Register(SITENUM, GROUPNUM, APSNUM)`이 반드시 다시 호출되어야 한다.
- 검색 404는 정상적인 빈 결과로, 503·시간초과·잘못된 JSON은 통신 실패로 구분되어야 한다.
- 설정값이 없거나 `SITENUM`, `GROUPNUM`, `APSNUM` 중 하나가 0 이하이면 프로그램이 명확한 설정 오류를 남겨야 한다.
- 폼이 닫히는 도중 콜백이 도착해도 폐기된 컨트롤을 조작하지 않아야 한다.

---

### Task 1: 설정, 계약, 사건 중복 방지

**Files:**
- Create: `APSMain_C(API)V2/Integration/EdgeService/EdgeServiceOptions.cs`
- Create: `APSMain_C(API)V2/Integration/EdgeService/EdgeServiceContracts.cs`
- Create: `APSMain_C(API)V2/Integration/EdgeService/KioskExitContext.cs`
- Create: `APSMain_C(API)V2/Integration/EdgeService/KioskEventTracker.cs`
- Create: `tests/APSMain.EdgeIntegration.Tests/APSMain.EdgeIntegration.Tests.csproj`
- Create: `tests/APSMain.EdgeIntegration.Tests/EdgeServiceOptionsTests.cs`
- Create: `tests/APSMain.EdgeIntegration.Tests/KioskEventTrackerTests.cs`
- Modify: `ParkingSystem.sln`

**Interfaces:**
- Consumes: `NameValueCollection` 형식의 App.config 값.
- Produces: `EdgeServiceOptions.Load(NameValueCollection)`, `KioskExitNotification`, `KioskExitContext`, `KioskEventTracker.TryBegin/MarkFailed/MarkCompleted`.

- [ ] **Step 1: 설정 누락, 잘못된 장비번호, 기본 URL, EventId 중복/실패 재처리 테스트를 작성한다.**
- [ ] **Step 2: `dotnet test tests/APSMain.EdgeIntegration.Tests/APSMain.EdgeIntegration.Tests.csproj`를 실행해 타입 부재로 실패하는지 확인한다.**
- [ ] **Step 3: URL·SITENUM·GROUPNUM·APSNUM 검증과 사건 상태 전이를 최소 구현한다.**
- [ ] **Step 4: 같은 테스트를 실행해 통과하는지 확인한다.**
- [ ] **Step 5: `feat: add APSMain EdgeService integration contracts`로 커밋한다.**

### Task 2: EdgeService HTTP 클라이언트

**Files:**
- Create: `APSMain_C(API)V2/Integration/EdgeService/EdgeServiceClient.cs`
- Create: `APSMain_C(API)V2/Integration/EdgeService/ParkingSearchResult.cs`
- Create: `tests/APSMain.EdgeIntegration.Tests/EdgeServiceClientTests.cs`

**Interfaces:**
- Consumes: `HttpClient`, `EdgeServiceOptions`, EdgeService의 `/api/v1/local/parking/search`와 `/api/v1/local/fees/quote/session`.
- Produces: `SearchParkingAsync(string, DateTimeOffset, CancellationToken)`, `QuoteSessionAsync(long, DateTimeOffset, IReadOnlyList<int>, CancellationToken)`, 성공·없음·일시 장애·잘못된 응답을 구분하는 `EdgeCallResult<T>`.

- [ ] **Step 1: 검색 URL 인코딩, 단일 요금 응답, 복수 후보 응답, 404, 503, 시간초과, 잘못된 JSON 테스트를 작성한다.**
- [ ] **Step 2: 해당 테스트를 실행해 구현 부재로 실패하는지 확인한다.**
- [ ] **Step 3: 재사용 `HttpClient`와 Newtonsoft.Json 역직렬화 기반 최소 구현을 작성한다.**
- [ ] **Step 4: 해당 테스트와 Task 1 테스트를 실행해 통과하는지 확인한다.**
- [ ] **Step 5: `feat: add APSMain EdgeService HTTP client`로 커밋한다.**

### Task 3: SignalR 연결과 폼 전달 조정자

**Files:**
- Create: `APSMain_C(API)V2/Integration/EdgeService/IKioskHubConnection.cs`
- Create: `APSMain_C(API)V2/Integration/EdgeService/KioskSignalRClient.cs`
- Create: `APSMain_C(API)V2/Integration/EdgeService/KioskExitCoordinator.cs`
- Create: `tests/APSMain.EdgeIntegration.Tests/KioskSignalRClientTests.cs`
- Create: `tests/APSMain.EdgeIntegration.Tests/KioskExitCoordinatorTests.cs`
- Modify: `APSMain_C(API)V2/APSMain.csproj`

**Interfaces:**
- Consumes: SignalR `/hubs/kiosk`, `Register(long)`, `ExitVehicleDetected`, `EdgeServiceClient`, `KioskEventTracker`.
- Produces: `KioskSignalRClient.StartAsync/StopAsync`, `ExitVehicleDetected` 이벤트, `KioskExitCoordinator.HandleAsync`, `SearchResolved` 이벤트.

- [ ] **Step 1: 최초 등록, 재연결 재등록, 중복 사건 무시, 실패 사건 재처리, 단일 요금·복수 후보 전달 테스트를 작성한다.**
- [ ] **Step 2: 해당 테스트를 실행해 구현 부재로 실패하는지 확인한다.**
- [ ] **Step 3: SignalR 어댑터와 UI 비종속 조정자를 최소 구현하고 SignalR Client 패키지를 추가한다.**
- [ ] **Step 4: 전체 연동 테스트를 실행해 통과하는지 확인한다.**
- [ ] **Step 5: `feat: receive APSMain kiosk events from EdgeService`로 커밋한다.**

### Task 4: 15/24인치 MainForm 연결과 실행시험

**Files:**
- Modify: `APSMain_C(API)V2/MainForm.cs`
- Modify: `APSMain_C(API)V2/MainForm15.cs`
- Modify: `APSMain_C(API)V2/App.config`
- Create: `run-apsmain-edge-test.bat`
- Modify: `docs/04-development-status.md`

**Interfaces:**
- Consumes: `KioskSignalRClient`, `KioskExitCoordinator.SearchResolved`.
- Produces: 폼 로드 시 비동기 연결, 종료 시 구독 해제·연결 종료, UI 스레드에서 단일 요금 또는 복수 후보 흐름 진입.

- [ ] **Step 1: 폼 연결 코드에 대한 소스 구조 검증 테스트를 추가해 EdgeService 모드에서 LPR/LDM 초기화 금지와 안전한 UI 전환을 고정한다.**
- [ ] **Step 2: 검증 테스트를 실행해 기존 폼 코드 때문에 실패하는지 확인한다.**
- [ ] **Step 3: 두 MainForm에 동일한 연동 수명주기와 결과 전달을 연결하고 `EDGESERVICEURL`, `SITENUM`, `GROUPNUM`, `APSNUM` 설정을 사용한다.**
- [ ] **Step 4: 저장소 최상위 배치 파일에 전체 restore/build/test, EdgeService·APSMain 실행, 환경변수·연결 문자열, 단계별 `pause`를 작성한다.**
- [ ] **Step 5: 가능한 환경에서 `dotnet build ParkingSystem.sln`과 `dotnet test ParkingSystem.sln --no-build`를 실행하고, 현재 환경에 .NET SDK가 없으면 배치 파일을 Windows에서 실행해야 한다는 사실과 미검증 항목을 기록한다.**
- [ ] **Step 6: `docs/04-development-status.md`에 완료 범위, `DbJobWorker` 보존/미사용, 다음 단계(결제완료·사건완료)를 기록한다.**
- [ ] **Step 7: `feat: connect APSMain forms to EdgeService`로 커밋한다.**
