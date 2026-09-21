# Entry, Exit, Image, and Vehicle Search Phase A Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 일반차량의 입차·정산·출차 상태를 `outflag I/X/O`로 통일하고, 입출차 이미지 정보와 전체번호·뒤 4자리 차량조회 및 선택 후 요금계산을 구현한다.

**Architecture:** `parking_session` 한 행에 일반차량 입·출차 정보를 계속 보존한다. EdgeService가 이미지 파일명과 사건정보를 받아 Outbox에 저장하고, API는 MySQL 세션을 갱신한다. 차량조회는 한 건이면 즉시 견적을 반환하고 여러 건이면 후보를 반환하며 선택 후 `ParkingSessionId`로 견적을 요청한다.

**Tech Stack:** .NET 9, ASP.NET Core, C#, Dapper, MySqlConnector, Microsoft.Data.Sqlite, Newtonsoft.Json, xUnit

**Spec:** `docs/05-entry-exit-image-design.md`

## Global Constraints

- JSON 속성명은 PascalCase를 유지한다.
- DB 접근은 Dapper, JSON 처리는 Newtonsoft.Json을 사용한다.
- `outflag`: I=입차, X=실제 정산완료, O=출차완료.
- 조회와 요금계산만으로는 `outflag=I`를 유지한다.
- 결제완료 또는 최종요금 0원 확정 시에만 `outflag=X`로 변경한다.
- 일반차량은 `parking_session` 한 행에 입·출차 정보를 보존하고 삭제하지 않는다.
- 이미지명의 Sitenum, Groupnum, LaneId는 각각 3자리 고정이다.
- 기존 API 호출이 깨지지 않도록 새 계약 필드는 선택 기본값을 가진다.

## Review Focus

- 전체번호와 뒤 4자리 결과가 각각 한 건일 때 즉시 동일한 견적을 반환해야 한다.
- 뒤 4자리 결과가 여러 건이면 어느 세션도 임의 선택하거나 상태 변경하지 않아야 한다.
- 같은 EventId 재전송은 이미지 정보를 포함해 최초 결과를 그대로 반환해야 한다.
- 결제되지 않은 유료 세션은 요금계산 후에도 `outflag=I`여야 한다.
- 999를 넘는 Site, Group, Lane 값은 이미지명에서 잘리지 않고 최소 3자리 형식으로 표현돼야 한다.

---

### Task 1: Outflag와 이미지 DB 마이그레이션

**Files:**
- Create: `database/mysql/007_entry_exit_images.sql`
- Modify: `database/mysql/001_initial.sql`
- Modify: `src/Central/Parking.Central.Data/ParkingEventRepository.cs`
- Modify: `src/Central/Parking.Central.Data/ParkingExitRepository.cs`
- Modify: `src/Central/Parking.Central.Data/PaymentRepository.cs`
- Test: `tests/Parking.Api.Tests/ParkingEventRepositoryTests.cs`
- Test: `tests/Parking.Api.Tests/ParkingExitRepositoryTests.cs`
- Test: `tests/Parking.Api.Tests/PaymentRepositoryTests.cs`

**Interfaces:**
- Consumes: 기존 `parking_event`, `parking_session`, `payment` 스키마
- Produces: `parking_session.outflag`, `indeviceid`, `inimage`, `outdeviceid`, `outimage`; `parking_event.groupnum`, `imagepath`

- [ ] **Step 1: outflag 상태 전이 실패 테스트 작성**

입차 후 `outflag='I'`, 결제 완료 후 `outflag='X'`, 출차 후 `outflag='O'`를 DB에서 직접 조회해 검증한다. 요금조회만 실행한 세션은 `I`인지 검증한다.

- [ ] **Step 2: 테스트 실행 및 실패 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~OutFlag"`

Expected: FAIL because `outflag` column and transition logic do not exist.

- [ ] **Step 3: 마이그레이션 작성**

`007_entry_exit_images.sql`은 `outflag CHAR(1)`, 입·출차 장비와 이미지 경로, 사건의 `groupnum`과 `imagepath`를 추가한다. 기존 `Entered`는 I, `Paid`는 X, `Exited`는 O로 변환한 뒤 문자열 `status`를 제거한다. `CHECK (outflag IN ('I','X','O'))`를 추가한다.

- [ ] **Step 4: 저장소 SQL을 outflag로 변경**

미출차 조건은 `outflag<>'O'`, 입차 생성은 I, 결제완료는 X, 출차완료는 O를 사용한다. 무료차량 출차는 출차 트랜잭션 안에서 바로 O로 변경한다.

- [ ] **Step 5: 저장소 테스트 실행**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~RepositoryTests"`

Expected: PASS.

- [ ] **Step 6: 커밋**

```bash
git add database/mysql src/Central/Parking.Central.Data tests/Parking.Api.Tests
git commit -m "feat: store parking lifecycle with outflag"
```

### Task 2: 입출차 계약과 이미지 파일명

**Files:**
- Create: `src/BuildingBlocks/Parking.Contracts/ParkingEventType.cs`
- Create: `src/BuildingBlocks/Parking.Contracts/VehicleImageName.cs`
- Modify: `src/BuildingBlocks/Parking.Contracts/FieldEventRequest.cs`
- Modify: `src/BuildingBlocks/Parking.Contracts/ExitEventRequest.cs`
- Test: `tests/Parking.Domain.Tests/ContractTests.cs`

**Interfaces:**
- Produces: `ParkingEventType.Entry`, `ParkingEventType.Exit`, `VehicleImageName.Create(...)`
- Produces: 입차 요청의 `InDateTime`, `Groupnum`, `EventType`, `InImage`; 출차 요청의 `OutDateTime`, `OutImage`

- [ ] **Step 1: 파일명과 방향 계약 실패 테스트 작성**

`VehicleImageName.Create(1,2,10,Entry,time,"12가3456",eventId)`가 `001_002_010_Entry_..._12가3456_...jpg`를 반환하는지 검증한다. 1000 이상의 값은 잘리지 않는지, 파일명 금지문자가 제거되는지 검증한다.

- [ ] **Step 2: 테스트 실행 및 실패 확인**

Run: `dotnet test tests/Parking.Domain.Tests/Parking.Domain.Tests.csproj`

Expected: FAIL because the image contract and formatter do not exist.

- [ ] **Step 3: 최소 계약 구현**

입차 요청은 `InDateTime`과 `InImage`, 출차 요청은 `OutDateTime`과 `OutImage`를 사용한다. 별도 촬영시간은 만들지 않는다. 파일명 숫자는 `D3`, 시각은 해당 입·출차시각의 `yyyyMMddHHmmssfff`, EventId는 구분자 없는 전체 UUID를 사용한다.

- [ ] **Step 4: 계약 테스트 실행**

Run: `dotnet test tests/Parking.Domain.Tests/Parking.Domain.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: 커밋**

```bash
git add src/BuildingBlocks/Parking.Contracts tests/Parking.Domain.Tests
git commit -m "feat: define parking event image contracts"
```

### Task 3: 입출차 이미지와 방향 저장

**Files:**
- Modify: `src/Central/Parking.Api/Features/Entries/CreateEntryEndpoint.cs`
- Modify: `src/Central/Parking.Api/Features/Entries/CreateEntryHandler.cs`
- Modify: `src/Central/Parking.Api/Features/Exits/ExitEndpoints.cs`
- Modify: `src/Central/Parking.Central.Data/ParkingEventRepository.cs`
- Modify: `src/Central/Parking.Central.Data/ParkingExitRepository.cs`
- Test: `tests/Parking.Api.Tests/CreateEntryEndpointTests.cs`
- Test: `tests/Parking.Api.Tests/ExitEndpointTests.cs`

**Interfaces:**
- Consumes: Task 2의 `EventType`, `Groupnum`, `InImage`, `OutImage`
- Produces: MySQL 사건과 세션의 입·출차 이미지 저장

- [ ] **Step 1: 방향 불일치와 이미지 저장 실패 테스트 작성**

입차 API에 Exit를 보내면 400, 출차 API에 Entry를 보내면 400인지 검증한다. 정상 요청은 `parking_event.imagepath`, `parking_session.inimage/outimage`, 입·출차 device 값을 저장하는지 검증한다.

- [ ] **Step 2: 테스트 실행 및 실패 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EntryEndpointTests|FullyQualifiedName~ExitEndpointTests"`

Expected: FAIL because direction validation and image persistence do not exist.

- [ ] **Step 3: API 검증과 저장 구현**

입차는 Entry, 출차는 Exit만 허용한다. 이벤트와 세션 저장을 같은 MySQL 트랜잭션에서 처리한다. 이미지 경로가 비어 있어도 기존 장비 호환을 위해 사건 자체는 허용하되 새 LPR 요청에서는 필수로 보내도록 문서화한다.

- [ ] **Step 4: API 테스트 실행**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~EntryEndpointTests|FullyQualifiedName~ExitEndpointTests"`

Expected: PASS.

- [ ] **Step 5: 커밋**

```bash
git add src/Central tests/Parking.Api.Tests
git commit -m "feat: persist entry and exit vehicle images"
```

### Task 4: 전체번호·뒤 4자리 차량조회

**Files:**
- Create: `src/BuildingBlocks/Parking.Contracts/ParkingSearchResponse.cs`
- Create: `src/Central/Parking.Central.Data/IParkingSearchRepository.cs`
- Create: `src/Central/Parking.Central.Data/ParkingSearchRepository.cs`
- Create: `src/Central/Parking.Api/Features/Search/ParkingSearchController.cs`
- Modify: `src/Central/Parking.Api/Program.cs`
- Test: `tests/Parking.Api.Tests/ParkingSearchRepositoryTests.cs`
- Test: `tests/Parking.Api.Tests/ParkingSearchEndpointTests.cs`

**Interfaces:**
- Produces: `GET /api/v1/parking/search?siteId=&groupnum=&carNumber=&exitAt=`
- Produces: 후보 목록 또는 단일 후보의 `QuoteParkingFeeResponse`

- [ ] **Step 1: 검색 실패 테스트 작성**

전체번호 한 건과 뒤 4자리 한 건은 즉시 견적을 반환하고, 뒤 4자리 여러 건은 최신순 후보 목록을 반환하며 DB outflag가 모두 I인지 검증한다. Groupnum이 다른 차량은 제외한다.

- [ ] **Step 2: 테스트 실행 및 실패 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~ParkingSearch"`

Expected: FAIL because search repository and endpoint do not exist.

- [ ] **Step 3: 검색 저장소와 API 구현**

입력 길이가 4이면 `RIGHT(carnum,4)=@CarNumber`, 그 외에는 `carnum=@CarNumber`를 사용한다. `outflag<>'O'`, 사이트·그룹 조건과 최신 입차순을 적용한다. 한 건이면 기존 FeeCalculationService로 즉시 견적을 계산하고 여러 건이면 후보만 반환한다.

- [ ] **Step 4: 검색 테스트 실행**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~ParkingSearch"`

Expected: PASS.

- [ ] **Step 5: 커밋**

```bash
git add src/BuildingBlocks src/Central tests/Parking.Api.Tests
git commit -m "feat: search open parking sessions by vehicle number"
```

### Task 5: 선택 세션 요금계산과 X 전환

**Files:**
- Modify: `src/Central/Parking.Api/Features/Fees/FeeCalculationController.cs`
- Modify: `src/Central/Parking.Central.Data/IParkingExitRepository.cs`
- Modify: `src/Central/Parking.Central.Data/ParkingExitRepository.cs`
- Modify: `src/Central/Parking.Central.Data/PaymentRepository.cs`
- Test: `tests/Parking.Api.Tests/FeeQuoteEndpointTests.cs`
- Test: `tests/Parking.Api.Tests/CompletePaymentEndpointTests.cs`

**Interfaces:**
- Produces: `POST /api/v1/fees/quote/session`
- Consumes: `{ ParkingSessionId, ExitAt }`

- [ ] **Step 1: 선택 견적과 상태 테스트 작성**

선택한 ParkingSessionId만 계산되는지, 유료 견적 후 I 유지, 결제완료 후 X, 0원 견적 확정 후 X가 되는지 검증한다.

- [ ] **Step 2: 테스트 실행 및 실패 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~FeeQuoteEndpointTests|FullyQualifiedName~CompletePaymentEndpointTests"`

Expected: FAIL because session quote and outflag transition do not exist.

- [ ] **Step 3: 선택 견적과 전환 구현**

ParkingSessionId로 미출차 세션을 조회하여 기존 견적 로직을 재사용한다. 유료면 I 유지, 최종 PayableAmount가 0이면 X로 변경한다. PaymentRepository는 성공 트랜잭션에서 X로 변경한다.

- [ ] **Step 4: 관련 테스트와 전체 테스트 실행**

Run: `dotnet test ParkingSystem.sln`

Expected: PASS.

- [ ] **Step 5: 커밋**

```bash
git add src/Central tests/Parking.Api.Tests
git commit -m "feat: quote selected parking session and finalize settlement"
```

### Task 6: Edge 중계와 Simulator 회귀시험

**Files:**
- Modify: `src/Central/Parking.EdgeGateway/EdgeGatewayController.cs`
- Modify: `src/Central/Parking.EdgeGateway/ParkingApiClient.cs`
- Modify: `src/Edge/Parking.EdgeService/EdgeController.cs`
- Modify: `src/Edge/Parking.EdgeService/GatewayClient.cs`
- Modify: `src/Tools/Parking.Simulator/Program.cs`
- Test: `tests/Parking.Api.Tests/FeeQuoteRelayTests.cs`
- Test: `tests/Parking.Api.Tests/SimulatorEntryCommandTests.cs`
- Modify: `docs/03-api-specification.md`
- Modify: `docs/04-development-status.md`

**Interfaces:**
- Consumes: Task 4 검색 API, Task 5 선택 견적 API
- Produces: EdgeService 로컬 검색·선택견적 중계와 Simulator 시험 명령

- [ ] **Step 1: 중계 실패 테스트 작성**

검색 JSON·상태와 선택견적 JSON·상태가 EdgeService→Gateway→API 경로에서 변하지 않는지 검증한다. Simulator 이미지명 생성과 EventType 전송을 검증한다.

- [ ] **Step 2: 테스트 실행 및 실패 확인**

Run: `dotnet test tests/Parking.Api.Tests/Parking.Api.Tests.csproj --filter "FullyQualifiedName~RelayTests|FullyQualifiedName~Simulator"`

Expected: FAIL because the new relay methods do not exist.

- [ ] **Step 3: 중계와 Simulator 구현**

Gateway와 EdgeService는 검색과 선택견적을 원문 중계한다. Simulator entry/exit 명령은 VehicleImageName을 사용하고 EventType을 명시한다.

- [ ] **Step 4: 전체 회귀시험 실행**

Run: `dotnet test ParkingSystem.sln`

Expected: PASS.

- [ ] **Step 5: 문서 갱신과 커밋**

완료된 API와 DB 필드를 현재 구현으로 이동하고 개발이력을 갱신한다.

```bash
git add src tests docs
git commit -m "feat: relay vehicle search and image events"
```

## Phase B: 등록차량

`tperiodmember`와 `tperiodinout`의 실제 운영 스키마를 받은 뒤 별도 계획으로 진행한다. 입차 시 `tperiodinout` 생성, 출차 시 같은 행 갱신, 입·출차 이미지와 장비정보 보존을 구현한다. 실제 스키마 확인 전에는 운영 테이블을 추측하여 생성하거나 변경하지 않는다.
