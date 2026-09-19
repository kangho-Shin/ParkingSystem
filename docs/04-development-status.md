# 개발이력 및 현재 상태

이 문서는 새 채팅에서 개발을 바로 이어가기 위한 인수인계 문서다.

## 1. 현재 기준

- 저장소: `https://github.com/kangho-Shin/ParkingSystem`
- 작업 브랜치: `codex/server-edge-foundation`
- 기준일: 2026-09-19
- Target Framework: .NET 9
- 중앙 DB: MySQL 8.x
- 현장 임시 DB: SQLite
- DB 접근: Dapper
- JSON: Newtonsoft.Json, PascalCase 유지
- 최신 빌드 성공 확인
- 등록차량 입출차 저장소 및 회원관리 API 통합시험 성공
- 등록차량 관리 추가 후 전체 명령 `dotnet test ParkingSystem.sln`은 아직 재실행 전

## 2. 주요 개발이력

### 2026-09-18: 서버·중계서버 기반

- 솔루션, 공통 계약, 중앙 MySQL 스키마 생성
- Parking.Api 입차 API와 EventId 중복방지 구현
- Parking.EdgeGateway 입차 중계 구현
- Parking.EdgeService Windows 서비스와 SQLite Outbox 구현
- 기본 저장경로와 서버 포트 정리
- 미출차 조회와 출차 API 구현
- 출차 사건의 EdgeService→Gateway→API 흐름 구현
- 현장·차로·장비 설정 API와 현장 SQLite 동기화 구현
- 중앙 장애 시 입차 허용, 출차 차단 정책 구현

### 2026-09-19: 요금·결제·통합 흐름

- 기존 요금 계산 코드를 `Parking.FeeEngine`으로 분리
- Dapper와 Newtonsoft.Json 사용방식 통일
- `tparkfee`, `tdiscount`, `tholiday`, `tparkvariable` 설정 로딩
- 요금 계산 API와 차량번호 기반 견적 API 구현
- 사이트·그룹·차종, 할인키, 기존 결제금액 반영
- 무료 유예시간, 서비스시간, 사전정산 유예시간 반영
- 결제 완료 API와 PaymentId 중복방지 구현
- 미결제·무료차량·결제완료·추가요금 출차 판단 구현
- 요금 견적과 결제 완료의 EdgeService→Gateway→API 중계 구현
- 결제 결과 SQLite Outbox 저장과 장애 재전송 구현
- 임시 SQLite 테스트의 연결 풀 잠금 문제 수정
- SQLite `INTEGER`를 Dapper `Int64`로 읽도록 수정
- Parking.Simulator 입차 명령과 `--event`, `--repeat-event` 구현
- Simulator를 솔루션에 등록하고 전체 빌드·테스트 성공
- 일반차량 `outflag`를 I/X/O로 통일하고 입·출차 이미지와 장비정보 저장
- 전체번호·뒤 4자리 미출차 차량검색과 복수 후보 선택 흐름 구현
- 단일 후보 즉시 견적과 `ParkingSessionId` 선택 견적 구현
- 최종 결제금액 0원 또는 결제완료 시 `outflag=X` 처리
- 검색·선택견적의 EdgeService→Gateway→API 중계 구현
- Simulator 입차·출차 `EventType`, 이미지명 생성 구현
- 동일차량 10초 중복입차 재사용과 이전 I/X 상태 정리 구현
- 입·출차 요청의 사이트·그룹·차로·장비·방향 설정 일치 검증
- 등록차량 유효회원 조회와 `tperiodinout` 입차 생성·출차 업데이트
- `000_full_schema.sql`에 일반·등록차량 포함 전체 MySQL 스키마 통합
- 등록차량 회원 전체 필드 조회·추가·수정·실제 삭제 API 구현
- `useflag`는 사용/사용중지 상태로 유지하고 삭제는 실제 `DELETE`로 처리

## 3. 실제 통합시험 완료 내용

2026-09-19 로컬 3개 서버와 Simulator로 확인했다.

1. Parking.Api `:5000`, EdgeGateway `:5100`, EdgeService `:5200` 실행
2. 차량 `12가3456` 입차를 같은 EventId로 두 번 전송
3. 두 응답 모두 같은 `ParkingSessionId=53` 반환
4. EdgeGateway를 중지한 뒤 차량 `34나5678` 입차
5. `EDGE_OFFLINE_ENTRY`, `ParkingSessionId=null`, `OpenBarrier=true` 반환
6. EdgeGateway 재시작 후 같은 EventId 재전송
7. 중앙 복구 처리 후 `ParkingSessionId=54` 반환
8. 마지막 `dotnet test ParkingSystem.sln` 전체 성공

따라서 정상 입차, 중복방지, 중앙 단절 중 현장저장, 복구 후 재전송이 확인됐다.

## 4. 완료된 범위

- 중앙 입차·미출차조회·출차
- 입출차 EventId 멱등 처리
- 현장 SQLite Outbox 선저장과 자동 재전송
- 중앙 장애 제한운영: 입차 허용, 출차 차단
- 현장·차로·장비 설정 CRUD와 현장 캐시 동기화
- 요금표·할인·휴일·운영변수 로딩
- 요금 계산과 차량번호 정산 견적
- 기존 결제와 사전정산 유예시간 반영
- 결제 완료 저장과 PaymentId 멱등 처리
- 요금·결제 중계 및 결제 Outbox 재전송
- Simulator 입차·중복·장애복구 시험
- 자동시험과 실제 로컬 통합시험
- 일반차량 입·출차 이미지 및 `InDateTime`/`OutDateTime` 계약
- 일반차량 전체번호·뒤 4자리 검색과 선택 정산 API
- 등록차량 회원 조회·추가·수정·실제 삭제 관리 API 및 통합시험

## 5. 아직 구현하지 않은 범위

### 등록차량 후속 구현

- 기존 등록차량 관리 프로그램의 신규 API 연동
- 기존 관리 프로그램 소스는 현재 저장소에 없으므로 소스 확보 후 진행
- 일반차량 차량번호 검색 화면에 등록차량 후보를 통합할지는 무인정산기 연동 시 확정

상세 내용은 [입출차 및 차량 이미지 설계](05-entry-exit-image-design.md)를 따른다.

### 다음 우선순위

1. `dotnet test ParkingSystem.sln` 전체 회귀시험
2. `Parking.EdgeManager` 최소 기능 설계 및 뼈대
3. 실제 LPR 결과 입력 계약과 차로 처리기
4. 가상 전광판·차단기 출력
5. 기존 LPR 프로그램을 `Parking.LprHost` 구조로 개편
6. 기존 무인정산기를 `Parking.Kiosk` 구조로 개편
7. 기존 등록차량 관리 프로그램 소스 확보 후 신규 API 연동

### 후속 프로그램

- `Parking.Operator`
- `Parking.TerminalAgent`
- `Parking.Payment`
- `Parking.Worker`
- `Parking.AdminWeb`
- `Parking.CustomerWeb`
- `Parking.FeeTester`

### 보류

- 제조사별 범용 플러그인과 `Parking.DeviceSdk`
- DriverHost
- 중앙 영상 재인식
- RabbitMQ 기반 비동기 작업
- 중앙 서버 이중화
- 고급 보안·개인정보 정책

차량검지기는 접점으로 LPR 카메라에 직접 연결하므로 EdgeService의 최초 사건은 차량감지가 아니라 LPR 인식 결과다.

## 6. 중요한 결정사항

- 무인정산기와 운영 프로그램은 중앙에 직접 접속하지 않고 EdgeService만 호출한다.
- 현장은 EdgeGateway를 통해 중앙 API에 접속한다.
- 입차·출차·결제는 중앙 전송 전에 SQLite에 저장한다.
- 신규 결제는 중앙 연결이 필요한 것을 원칙으로 하되 이미 승인된 결과는 Outbox로 보존한다.
- 결제 완료 API는 복잡한 요금 재검증과 견적 저장을 하지 않는다.
- 사이트 안에서도 지하·지상 등 그룹별 요금이 다를 수 있으므로 `Sitenum`과 `Groupnum`을 함께 사용한다.
- `CarType`: 1=소형, 2=중형, 3=대형.
- 제조사별 장비 플러그인은 자체 장비 흐름을 완성한 뒤 진행한다.
- 이미지 파일 전송과 웹 조회는 기존 프로그램을 재사용하며 DB에는 파일명만 저장한다.
- 일반차량은 입차 행을 다른 테이블로 이동·삭제하지 않고 같은 `parking_session` 행에 출차정보를 업데이트한다.
- 등록차량은 입차 시 `tperiodinout`을 생성하고 출차 시 같은 행을 업데이트한다.
- 등록차량 `useflag`는 사용/사용중지이며 삭제 표시에 사용하지 않는다.
- 등록차량 회원 삭제 API는 `tperiodmember` 행을 실제 삭제한다.
- 요금조회와 요금계산만으로는 `outflag=I`를 유지한다.
- 실제 결제완료 또는 최종요금 0원 확정 시에만 `outflag=X`로 변경한다.
- 출차완료 시 `outflag=O`로 변경한다.

## 7. 로컬 실행 방법

MySQL 연결 문자열 환경변수 `PARKING_TEST_CONNECTION`이 설정돼 있어야 한다.

### CMD 1: Parking.Api

```bat
set ConnectionStrings__ParkingDatabase=%PARKING_TEST_CONNECTION%
dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
```

### CMD 2: Parking.EdgeGateway

```bat
dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
```

### CMD 3: Parking.EdgeService

```bat
dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
```

### CMD 4: Simulator

```bat
dotnet run --project src\Tools\Parking.Simulator\Parking.Simulator.csproj -- entry --site 1 --group 1 --lane 10 --device 101 --car 12가3456 --repeat-event

dotnet run --project src\Tools\Parking.Simulator\Parking.Simulator.csproj -- exit --site 1 --group 1 --lane 20 --device 201 --car 12가3456
```

## 8. 시험 명령

```bat
dotnet test ParkingSystem.sln
```

MySQL 저장소 시험만 실행할 때도 `PARKING_TEST_CONNECTION`이 필요하다.

## 9. 새 채팅 시작 문구

다음 문장과 이 문서를 함께 제공하면 된다.

> ParkingSystem의 `codex/server-edge-foundation` 브랜치 작업을 계속 진행해줘. `docs/04-development-status.md`를 먼저 읽고, 완료된 작업을 반복하지 말고 아직 구현하지 않은 다음 우선순위부터 한 단계씩 진행해줘.

현재 즉시 할 일은 `dotnet test ParkingSystem.sln` 전체 회귀시험이다. 성공하면 `Parking.EdgeManager` 최소 기능의 범위를 먼저 확정하고 구현한다.
