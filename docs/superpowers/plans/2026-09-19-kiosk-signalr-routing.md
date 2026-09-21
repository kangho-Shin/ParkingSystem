# 무인정산기 SignalR 연동 구현계획

> 작업별로 테스트를 먼저 작성한다. 전체 구현과 GitHub 반영이 끝난 후 전체 테스트를 한 번만 요청한다.

**목표:** 출구 LPR 사건을 장비 구성에 따라 무인정산기로 SignalR 통보하거나 LPR 단독으로 처리하고, 무인 연결 장애 시 `OPEN/BLOCK` 정책을 적용한다.

**구조:** 단일 `LinkedDeviceId`를 다대다 장비 연결 테이블로 교체한다. EdgeService는 무인 접속 상태와 LPR 사건 대기를 관리하고, 연결된 무인이 온라인이면 음성·화면 처리가 끝날 때까지 출차와 전광판 제어를 보류한다.

**기술:** .NET 9, ASP.NET Core SignalR, Dapper, MySQL 8, SQLite, Newtonsoft.Json, xUnit

**설계 문서:** `docs/superpowers/specs/2026-09-19-kiosk-signalr-routing-design.md`

## 전체 제약사항

- 가상 LPR HTTP API는 만들지 않고 기존 LPR TCP 입력을 사용한다.
- 이미지 파일명의 `DeviceId`는 LPR 카메라별 장치번호다.
- LPR 한 대에는 카메라가 1~4대 연결될 수 있으며 LPR 접속 대수는 제한하지 않는다.
- 연결된 무인이 온라인이면 무료·등록차량도 반드시 무인의 화면과 음성 안내를 거친다.
- 무인 처리 완료 전에는 기존 출차 API와 전광판·차단기를 실행하지 않는다.
- `CMD_KIOSK_OFFLINE_POLICY`는 `val=OPEN|BLOCK`이며 누락·오류 시 `OPEN`이다.
- 전광판과 차단기 명령은 항상 EdgeService가 실행한다.
- 기존 차량조회·요금·결제·출차·Outbox·모니터링·LDM 코드를 재사용한다.

## 중점 검토사항

- 같은 무인 장치번호가 재접속하면 가장 최근 SignalR 연결만 유효해야 한다.
- 다른 무인이나 다른 사이트의 사건을 완료할 수 없어야 한다.
- 무인 완료와 장애출차 EventId는 중복 호출해도 한 번만 처리해야 한다.
- 비활성 연결, 장비종류 불일치, 자기 자신 연결은 사용할 수 없어야 한다.
- SignalR 전송 중 연결이 끊겨도 사건을 잃지 않고 `OPEN/BLOCK` 정책으로 전환해야 한다.

## 1단계: 다대다 장비 연결 구조

**변경 파일**

- `database/mysql/010_device_links.sql`
- `database/mysql/000_full_schema.sql`
- `database/mysql/002_site_configuration.sql`
- `src/BuildingBlocks/Parking.Contracts/SiteConfiguration.cs`
- `src/Central/Parking.Central.Data/SiteConfigurationRepository.cs`
- `src/Central/Parking.Api/Features/Configuration/SiteConfigurationEndpoints.cs`
- `tests/Parking.Api.Tests/DeviceLinkSelectorTests.cs`

**구현 내용**

- `ParkingDeviceLink(SiteId, SourceDeviceId, TargetDeviceId, LinkType, Enabled)` 계약 추가
- `parking_device_link` 테이블과 설정 CRUD 추가
- LPR→무인, LPR→전광판, 무인→전광판 연결 지원
- 기존 `linkeddeviceid` 데이터를 새 테이블로 이동한 후 기존 열과 외래키 제거
- `DeviceLinkSelector.FindTargets(...)`로 활성 대상 장비 검색
- 자기 자신 연결, 장비종류 불일치, 다른 사이트 연결 거부

**시험:** `dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~DeviceLinkSelectorTests"`

**커밋:** `feat: add device link routing model`

## 2단계: 무인 장애 정책 설정 동기화

**변경 파일**

- `src/BuildingBlocks/Parking.Contracts/SiteConfiguration.cs`
- `src/Central/Parking.Central.Data/SiteConfigurationRepository.cs`
- `database/mysql/000_full_schema.sql`
- `database/mysql/004_sample_fee_data.sql`
- `tests/Parking.Api.Tests/EdgeOperationPolicyTests.cs`

**구현 내용**

- `ParkingOperationVariable(Groupnum, CommandType, Value)` 추가
- 사이트 설정 동기화에 `CMD_KIOSK_OFFLINE_POLICY` 포함
- 그룹별 `OPEN/BLOCK` 해석기 추가
- 값 누락 또는 오류 시 `OPEN` 반환

**시험:** `dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~EdgeOperationPolicyTests"`

**커밋:** `feat: sync kiosk offline policy`

## 3단계: 무인정산기 SignalR 연결 관리

**변경 파일**

- `src/BuildingBlocks/Parking.Contracts/KioskModels.cs`
- `src/Edge/Parking.EdgeService/KioskConnectionRegistry.cs`
- `src/Edge/Parking.EdgeService/KioskHub.cs`
- `src/Edge/Parking.EdgeService/Program.cs`
- `tests/Parking.Api.Tests/KioskConnectionRegistryTests.cs`

**구현 내용**

- SignalR 주소 `/hubs/kiosk` 추가
- 무인 연결 후 `Register(DeviceId)` 호출
- 장치번호와 SignalR 연결번호를 양방향 관리
- 같은 무인이 재접속하면 최신 연결로 교체
- `ExitVehicleDetected` 알림 모델 추가

**시험:** `dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~KioskConnectionRegistryTests"`

**커밋:** `feat: register kiosk signalr connections`

## 4단계: 무인 대기 사건 저장과 재전송

**변경 파일**

- `src/Edge/Parking.EdgeService/KioskPendingRepository.cs`
- `src/Edge/Parking.EdgeService/KioskNotificationService.cs`
- `src/Edge/Parking.EdgeService/KioskHub.cs`
- `src/Edge/Parking.EdgeService/Program.cs`
- `tests/Parking.Api.Tests/KioskPendingRepositoryTests.cs`
- `tests/Parking.Api.Tests/KioskNotificationServiceTests.cs`

**구현 내용**

- 원래 LPR 인식정보와 대상 무인 장치번호를 SQLite에 저장
- EventId 중복 저장 방지와 발생순 조회
- 온라인이면 SignalR 즉시 전송
- 전송 실패 시 그룹의 `OPEN/BLOCK` 정책 적용
- `BLOCK` 사건만 무인 재접속 후 재전송
- `OPEN` 장애출차 사건은 재전송하지 않고 이력만 보존

**시험:** `dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~KioskPendingRepositoryTests|FullyQualifiedName~KioskNotificationServiceTests"`

**커밋:** `feat: persist and deliver kiosk events`

## 5단계: 무인 장애 OPEN 출차 기록

**변경 파일**

- `src/BuildingBlocks/Parking.Contracts/OfflineExitModels.cs`
- `src/Central/Parking.Central.Data/IParkingEventRepository.cs`
- `src/Central/Parking.Central.Data/ParkingEventRepository.cs`
- `src/Central/Parking.Api/Features/Exits/ExitEndpoints.cs`
- `src/Central/Parking.EdgeGateway/EdgeGatewayController.cs`
- `src/Central/Parking.EdgeGateway/ParkingApiClient.cs`
- `src/Central/Parking.EdgeGateway/FieldEventRelay.cs`
- `src/Edge/Parking.EdgeService/GatewayClient.cs`
- `src/Edge/Parking.EdgeService/SqliteOutboxRepository.cs`
- `src/Edge/Parking.EdgeService/OutboxWorker.cs`
- `tests/Parking.Api.Tests/OfflineKioskExitTests.cs`

**구현 내용**

- 장애출차 전용 요청과 중앙 API 추가
- 결과코드 `KIOSK_OFFLINE_OPEN`, `OpenBarrier=true`
- 중앙 전송 전에 SQLite Outbox 저장
- 중앙에서 열린 주차 세션을 `O`로 변경하고 출차정보 저장
- 동일 EventId 재전송 멱등 처리
- 일치하는 세션이 없어도 다른 차량을 변경하지 않고 장애사건만 기록

**시험:** `dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~OfflineKioskExitTests"`

**커밋:** `feat: record offline kiosk exits`

## 6단계: LPR·무인 출차 흐름 연결

**변경 파일**

- `src/Edge/Parking.EdgeService/KioskEventController.cs`
- `src/Edge/Parking.EdgeService/KioskExitCoordinator.cs`
- `src/Edge/Parking.EdgeService/LprLaneProcessor.cs`
- `src/Edge/Parking.EdgeService/DisplayBoardOutput.cs`
- `tests/Parking.Api.Tests/KioskExitCoordinatorTests.cs`

**처리 순서**

1. LPR 패킷과 카메라 장치 설정 검증
2. LPR 카메라에 연결된 활성 무인 검색
3. 연결이 없으면 기존 출차 처리 후 LPR→전광판 연결 사용
4. 연결된 무인이 온라인이면 SQLite 저장 후 SignalR 통보
5. 이 단계에서는 출차 API와 전광판·차단기를 실행하지 않음
6. 무인은 기존 조회·요금·결제 API 사용
7. 무인 완료 API 호출 후 저장된 LPR 사건으로 기존 출차 실행
8. 서버 결과로 무인→전광판 연결을 사용해 표시와 차단기 제어

**완료 API:** `POST /api/v1/local/kiosks/events/{eventId}/complete`, 본문 `Sitenum/Groupnum/Devicenum`

클라이언트는 차단기 개방 여부를 보내지 않고 EdgeService가 기존 출차 결과로 결정한다.

**시험:** `dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~KioskExitCoordinatorTests"`

**커밋:** `feat: coordinate kiosk linked exits`

## 7단계: 시험용 SignalR 무인 클라이언트

**추가 파일**

- `src/Tools/Parking.KioskSimulator/Parking.KioskSimulator.csproj`
- `src/Tools/Parking.KioskSimulator/Program.cs`
- `tests/Parking.Api.Tests/KioskSignalRIntegrationTests.cs`

**실행 방법**

```bat
dotnet run --project src\Tools\Parking.KioskSimulator -- --url http://localhost:5200 --site 9001 --group 2 --device-number 201 --complete
```

- SignalR 자동 재접속 및 장치번호 재등록
- 수신 알림을 JSON 한 줄로 출력
- `--complete` 사용 시 완료 API 자동 호출

**시험:** `dotnet test tests\Parking.Api.Tests\Parking.Api.Tests.csproj --filter "FullyQualifiedName~KioskSignalRIntegrationTests"`

**커밋:** `feat: simulate kiosk signalr client`

## 8단계: 문서와 전체 확인

수정 문서:

- `docs/01-program-overview.md`
- `docs/02-program-features.md`
- `docs/03-api-specification.md`
- `docs/04-development-status.md`

전체 구현을 GitHub에 올린 후 사용자 PC에서 한 번만 실행한다.

```bat
git pull origin codex/server-edge-foundation
mysql -u 계정 -p DB이름 < database\mysql\010_device_links.sql
dotnet test ParkingSystem.sln
```

기대 결과는 모든 프로젝트 빌드 성공과 전체 테스트 실패 0건이다.
