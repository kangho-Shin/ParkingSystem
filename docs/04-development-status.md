# 개발이력 및 현재 상태

이 문서는 새 채팅에서 개발을 바로 이어가기 위한 인수인계 문서다.

## 1. 현재 기준

- 저장소: `https://github.com/kangho-Shin/ParkingSystem`
- 작업 브랜치: `codex/server-edge-foundation`
- 기준일: 2026-09-21
- Target Framework: .NET 9
- 중앙 DB: MySQL 8.x
- 현장 임시 DB: SQLite
- DB 접근: Dapper
- JSON: Newtonsoft.Json, PascalCase 유지
- 마지막 사용자 실행 결과 `Parking.Api.Tests` 전체 135개 성공
- 등록차량 입출차 저장소 및 회원관리 API 통합시험 성공
- `dotnet test ParkingSystem.sln` 전체 회귀시험 성공

시험 현장 `sitenum=9001`, `groupnum=2`의 현재 장치 식별자는 다음과 같다.

| 장치 | laneid | deviceid | devicenum | 사용 |
|---|---:|---:|---:|---|
| KIOSK | 9020 | 2001 | 201 | 사용 |
| 입차 JPXLPR | 9010 | 4001 | 401 | 사용 |
| 출차 JPXLPR | 9020 | 4002 | 402 | 사용 |
| 보조 입차 JPXLPR | 9010 | 4003 | 403 | 미사용 |
| 보조 출차 JPXLPR | 9020 | 4004 | 404 | 미사용 |
| LDM | 9020 | 5001 | 501 | 사용 |

기존 DB는 `database/mysql/012_device_number_ranges.sql`을 실행해 운전 데이터를 보존한 채 위 식별자로 이전한다. 모든 DB 스크립트는 기존 테이블을 삭제하거나 다시 만들지 않는다.

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
- EdgeService SQLite 입차·정산·출차 모니터링 저장소 구현
- Gateway·중앙 API 연결상태 분리 조회 구현
- EdgeService 관리·설정·목록·LPR 이미지 조회 API 구현
- Parking.EdgeManager WinForms 상태·목록·사진 모니터링 1차 구현
- Parking.EdgeManager 포함 전체 회귀시험 성공
- LPR STX/ETX·KS5601 TCP 수신과 ACK/NAK 응답 구현
- LPR 파일명의 현장·그룹·장비·차로·방향 검증 및 기존 입출차 흐름 연결
- 전광판을 `DeviceType=LDM`, 차로, IP, TCP 포트로 등록하는 장비설정 확장
- 기존 LDM 2줄 표시 및 차단기 열림 패킷을 EdgeService 출력으로 연결
- LPR 장비 수 제한 없이 동시접속하고 접속 수를 EdgeManager에 표시
- 카메라별 `DeviceId`와 전광판·무인정산기를 `parking_device_link` 다대다 관계로 연결
- 출구 LPR과 무인정산기 SignalR 연계 및 완료 회신 구현
- 무인 오프라인 `OPEN`/`BLOCK` 그룹별 정책 구현
- `OPEN` 장애출차 SQLite Outbox 보존과 복구 후 자동 전송 구현
- `Parking.KioskSimulator` SignalR 알림·완료 회신 시험 도구 구현
- 실제 DB 식별자와 LPR 3자리 `devicenum` 분리 및 실제 `deviceid` 변환 구현

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

### 2026-09-20: 출구 LPR·무인 SignalR 통합시험

1. 사이트 `9001`, 그룹 `2`, 출구 차로 `9020` 설정 동기화
2. 출구 LPR `deviceid=4002`, `devicenum=402`와 무인 `2001` 연결
3. LPR TCP로 차량 `12가3456` 출차 파일명 전송
4. EdgeService에서 실제 LPR `deviceid=4002`로 변환 후 `ACK` 응답
5. 무인 SignalR에서 사건 수신 후 완료 API HTTP 200 응답
6. 중앙 DB `parking_session.xindex=298`의 `outflag=O`, `outdeviceid=4002`, 출차 이미지 저장 확인

따라서 출구 LPR→EdgeService→SignalR 무인→완료 회신→중앙 출차 저장 흐름이 실제로 확인됐다.

### 2026-09-20: 실제 LDM 전광판 TCP 연결시험

1. 출구 LDM `deviceid=5001`, `devicenum=501`, TCP `35000` 등록
2. 출구 LPR `4002`와 무인정산기 `2001`을 같은 LDM에 연결
3. 컴아날라이저로 표시 패킷과 차단기 열림 패킷 수신 확인
4. 기존 운영 규격에 맞춰 즉시표시 명령을 `I`, 표시시간을 11초로 수정
5. 각 표시 줄 앞에 `0x10 + W` 흰색 제어코드 추가
6. 문자 표시 후 차단기 명령까지 100ms 간격 적용
7. 실제 전광판에 차량번호와 출차문구 표시 및 차단기 개방 확인

따라서 EdgeService→LDM TCP 문자 표시→차단기 개방 흐름이 실제 장비에서 확인됐다.

### 2026-09-20: 잔여 프로그램 실행 기반

- `Parking.Operator` WinForms 기본 화면과 EdgeService 상태 확인 구현
- `Parking.TerminalAgent` Windows Service 기반 프로세스 상태 감시 루프 구현
- `Parking.Worker` Windows Service 기반 Parking.Api 상태 확인 루프 구현
- `Parking.FeeTester` WinForms 입력 화면과 임시 요금설정 기반 FeeEngine 호출 구현
- 네 프로젝트를 `ParkingSystem.sln`에 등록
- 저장소 최상위 `run-foundations.bat`에 연결 문자열과 환경변수, 단계별 실행 및 `pause` 구성
- Windows 개발 PC에서 `dotnet build ParkingSystem.sln` 성공
- Windows 개발 PC에서 `dotnet test ParkingSystem.sln` 전체 성공
- `run-foundations.bat`로 서버·EdgeService·KioskSimulator·잔여 프로그램 순차 실행
- 통합 실행 시 기존 EdgeService 현장설정 DB를 그대로 사용

### 2026-09-20: Parking.Operator 유인정산 기본 업무

- 전체 차량번호·뒤 4자리 검색과 복수 후보 선택 화면 구현
- 선택 차량 요금조회와 할인키 적용 구현
- 카드·현금 결제 완료 저장 기반 구현
- 미출차 차량번호 정정 API와 화면 구현
- 설정된 장비를 이용한 수동 입차·출차 구현
- 출구 LDM 연결을 이용한 차단기 수동개방 구현
- 입차사진 조회 화면 구현

### 2026-09-20: LPR 4카메라 연속수신 시험 도구

- `Parking.LprStressTester` .NET 9 콘솔 프로젝트 추가
- LPR 장비 한 대의 카메라별 장치번호 1~4개를 독립 TCP 연결로 동시 시험
- 기존 운영 규격과 같은 `STX + CP949 파일명 + ETX` 요청 생성
- 분할 수신과 연속 프레임을 처리하는 ACK/NAK 응답 조립기 구현
- 카메라별 전송·ACK·NAK·시간초과·프로토콜·연결 오류 집계
- 모든 전송이 EventId가 포함된 ACK를 받아야 종료코드 0 반환
- 루프백 TCP 서버를 이용한 4연결 연속 송수신 자동시험 추가
- 저장소 최상위 `run-lpr-four-camera-test.bat`에 전체 빌드, 자동시험, 서버 실행, 연속수신 시험, DB 확인과 단계별 `pause` 구성

배치 파일은 사이트 `9001`, 입차 차로 `9010`에 전용 카메라 내부 키 `4011,4012,4013,4014`와 장치번호 `411,412,413,414`가 미리 등록돼 있는지 검증하고 EdgeService 설정 동기화를 최대 30초 기다린다. 장비가 없거나 식별자·종류·차로·사용 상태가 다르면 DB를 변경하지 않고 시험을 중단한다. 기본 시험은 카메라별 20건, 전체 80건이며 성공 결과는 `전송=80, ACK=80`이다.

### 2026-09-20: LPR 4카메라 실제 연속수신 시험

1. `run-lpr-four-camera-test.bat` 전체 솔루션 빌드 성공
2. 전체 자동시험 성공
3. 사전 등록된 전용 카메라 장치 `4011~4014 / 411~414` 검증과 EdgeService 설정 동기화 성공
4. 전용 카메라 장치번호 `411,412,413,414`가 각각 독립 TCP 연결로 20건씩 연속 전송
5. 전체 80건 모두 ACK 수신, NAK·시간초과·프로토콜·연결 오류 0건
6. MySQL 입차 저장 확인 성공

따라서 LPR 본체 한 대의 최대 4개 카메라 사건을 EdgeService가 동시에 연속 수신하는 실제 시험까지 완료됐다.

### 2026-09-20: 임시 무인정산기 운영 결정

- 전체 서버·현장 구조가 완성될 때까지 `Parking.KioskSimulator`를 무인정산기 대역으로 사용한다.
- SignalR 연결 등록, `ExitVehicleDetected` 수신, 무인 처리 완료 API 회신은 현재 구현을 그대로 사용한다.
- `run-foundations.bat`가 무인 장비 `9001/2/201`로 `Parking.KioskSimulator --complete`를 실행하고 EdgeService가 내부 `deviceid=2001`로 변환해 SignalR 수신 후 완료 API까지 자동 회신한다.
- 실제 무인정산기의 화면·음성·카드단말·영수증 소스 전환은 전체 구조 완성 후 진행한다.

### 2026-09-20: WinForms Designer 구조 정리

- `Parking.EdgeManager`의 `MainForm`, `SetupForm`, `ConfigurationForm`을 `.cs`와 `.Designer.cs`로 분리
- `Parking.Operator.MainForm`을 업무 로직과 Designer 화면 코드로 분리
- `Parking.FeeTester.MainForm`을 계산 로직과 Designer 화면 코드로 분리
- Visual Studio 디자이너가 열 수 있도록 각 폼에 매개변수 없는 디자인용 생성자와 프로젝트 `DependentUpon` 메타데이터 추가
- `InitializeComponent()`에서 사용자 메서드와 반복문을 제거하고 실행 구성은 일반 `.cs`로 이동
- Windows Visual Studio에서 Designer 화면이 정상적으로 열리고 동작하는 것 확인
- 앞으로 새 WinForms는 처음부터 `Form.cs`, `Form.Designer.cs` 구조로 작성

### 2026-09-20: Parking.TerminalAgent 자동 실행·재시작 관리

- Windows 서비스 방식은 유지하고 콘솔·백그라운드 프로그램만 관리하도록 범위 확정
- Windows 세션 0 격리로 화면이 표시되지 않는 `Parking.EdgeManager`, `Parking.Operator`, `Parking.FeeTester` 등 WinForms는 감시 대상에서 제외
- 설정된 실행 파일 경로·인수·작업 폴더를 이용한 프로그램 자동 실행 구현
- 프로세스 종료 감지 후 자동 재시작 구현
- 기본 5분 동안 최대 3회까지만 실행하고 반복 종료 시 재시작을 제한
- 기본 5분 이상 안정 실행하면 이전 재시작 횟수를 초기화
- 로그를 `logs/TerminalAgent/연도/월/일/TerminalAgent.log` 구조로 저장
- `Parking.TerminalAgent.Tests`에 재시작 횟수 제한, 제한 해제, 안정 실행 초기화, 자동 실행, 로그 저장 시험 추가
- 저장소 최상위 `run-terminal-agent-test.bat`에 전체 빌드·테스트, 서버 실행, KioskSimulator 자동 실행, 강제 종료·재시작·반복 장애 제한 확인 절차 구성
- 현재 작업 실행환경에는 .NET SDK가 없어 전체 빌드·테스트와 Windows 배치 실행은 Windows 개발 PC에서 확인 필요

### 2026-09-21: APSMain EdgeService 1차 연동

- 기존 `APSMain_C(API)V2`를 복제하지 않고 `Integration/EdgeService` 공통 연동 계층 추가
- APSMain은 새 운전모드에서 MySQL, 출구 LPR, LDM·차단기에 직접 연결하지 않고 EdgeService만 사용
- `DbJobWorker`와 기존 DB 모델은 레거시 코드 보존을 위해 남겨 두되 새 연동 경로에서는 호출하지 않음
- SignalR `/hubs/kiosk` 연결, `SITENUM/GROUPNUM/APSNUM` 등록, 재연결 후 재등록, `ExitVehicleDetected` 수신 구현
- EventId 처리 중·완료 상태를 구분해 중복 화면 및 중복 결제 진입 방지
- 차량검색 404, 일시 장애, 잘못된 응답을 구분하고 단일 요금 결과와 복수 후보를 결제 없는 1차 전용 조회 화면으로 전달
- 복수 후보는 `ParkingSessionId`를 보존하고 선택 후 EdgeService 요금을 다시 조회하며, 기존 로컬 요금계산·결제·출차저장 경로는 실행하지 않음
- `EDGESERVICEUSE`, `EDGESERVICEURL`과 기존 `SITENUM`, `GROUPNUM`, `APSNUM` 설정 사용 및 App.config MySQL 연결 문자열 제거
- 기존 WinForms `.cs`, `.Designer.cs`, `.resx` 구조 유지
- 저장소 최상위 `run-apsmain-edge-test.bat`에 연결 문자열·환경변수, 전체 빌드·테스트, 서버와 APSMain 실행, 단계별 `pause` 구성
- 현재 작업환경에는 .NET SDK가 없어 Windows 개발 PC에서 배치 파일 전체 검증 필요

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
- Parking.EdgeManager 현재 입차 및 정산·출차 통합목록
- Parking.EdgeManager 최신 사건 LPR 사진 자동표시와 목록 선택 조회
- LPR TCP 다중접속 수신과 차로 처리기
- 장비 간 다대다 연결, 무인 SignalR 출차 라우팅 및 장애출차 보존
- 실제 LDM 전광판 TCP 문자 표시와 차단기 개방

## 5. 아직 구현하지 않은 범위

### 등록차량 후속 구현

- 기존 등록차량 관리 프로그램의 신규 API 연동
- 기존 관리 프로그램 소스는 현재 저장소에 없으므로 소스 확보 후 진행
- 일반차량 차량번호 검색 화면에 등록차량 후보를 통합할지는 무인정산기 연동 시 확정

상세 내용은 [입출차 및 차량 이미지 설계](05-entry-exit-image-design.md)를 따른다.

### 다음 우선순위

1. Windows 개발 PC에서 `run-terminal-agent-test.bat` 실행 및 결과 확인
2. `Parking.Worker` 중앙 예약 작업 구현
3. `Parking.FeeTester` 실제 API 설정 로딩과 회귀 시나리오 일괄 실행

기존 등록차량 관리 프로그램과 실제 무인정산기 운영 소스 연동은 소스 확보 전까지 진행하지 않는다.

### 후속 프로그램

- `Parking.AdminWeb`
- `Parking.CustomerWeb`

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
- 별도 `Parking.Payment` 프로그램은 만들지 않는다. 카드 승인·취소·망취소는 무인정산기가 담당하고 서버는 최종 결제 결과만 저장한다.
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

MySQL 연결 문자열 환경변수 `PARKING_RUNTIME_CONNECTION` 하나만 사용한다. DB는 `parking000test`만 사용한다.

### CMD 1: Parking.Api

```bat
set ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%
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

### CMD 5: Kiosk Simulator

```bat
dotnet run --project src\Tools\Parking.KioskSimulator\Parking.KioskSimulator.csproj -- --site 9001 --group 2 --device-number 201 --url http://localhost:5200 --complete
```

## 8. 시험 명령

```bat
dotnet test ParkingSystem.sln
```

MySQL 저장소 시험도 `PARKING_RUNTIME_CONNECTION`을 사용하며 `parking000test` 이외의 DB에서는 실행하지 않는다.

잔여 프로그램을 서버부터 순서대로 실행할 때는 다음 배치 파일을 사용한다.

```bat
run-foundations.bat
```

배치 파일은 `PARKING_RUNTIME_CONNECTION`을 `ConnectionStrings__ParkingDatabase`로 전달하고 각 프로그램 실행 사이에 `pause`한다.

LPR 4카메라 연속수신 전체 시험은 다음 배치 파일 하나를 사용한다.

```bat
run-lpr-four-camera-test.bat
```

기본값은 사이트 `9001`, 그룹 `2`, 입차 차로 `9010`, 전용 카메라 장치번호 `411,412,413,414`, 카메라별 20건이다. 필요하면 실행 전에 `LPR_TEST_SITE`, `LPR_TEST_GROUP`, `LPR_TEST_LANE`, `LPR_TEST_DEVICES`, `LPR_TEST_COUNT`, `LPR_TEST_INTERVAL_MS`, `LPR_TEST_TIMEOUT_SECONDS` 환경변수로 변경한다.

TerminalAgent 자동 실행·비정상 종료 재시작·반복 장애 제한 전체 시험은 다음 배치 파일 하나를 사용한다.

```bat
run-terminal-agent-test.bat
```

배치 파일은 `PARKING_RUNTIME_CONNECTION`을 사용해 전체 솔루션을 빌드·시험하고 Api, EdgeGateway, EdgeService를 순서대로 실행한다. 이후 TerminalAgent가 KioskSimulator를 자동 실행하도록 설정하고 강제 종료를 반복해 자동 재시작과 5분 내 3회 제한을 확인한다. 각 단계 사이에는 `pause`가 있다.

APSMain Edge 연동은 임시 `EdgeSettlementForm`을 더 이상 호출하지 않는다. 단일 차량은 기존 `ParkCalForm/ParkCalForm15`, 복수 차량은 기존 `CarSelectForm/CarSelectForm15`을 사용한다. EdgeService 요금조회 결과를 기존 정산 화면에 적용하고 할인 재조회·결제완료·출차사건 완료는 EdgeService API로 처리한다.

`run-apsmain-edge-test.bat`는 사이트 `9001`, 그룹 `2`, 환경변수 `EDGE_SITE_AUTH_KEY`로 EdgeService 설정 동기화를 확인한 뒤 APSMain을 실행한다. APSMain 설정은 `SITENUM=9001`, `GROUPNUM=2`, `APSNUM=201`이며 `deviceid`는 EdgeService가 `sitenum + groupnum + devicenum`으로 조회한다.

### 2026-09-21: JPXLPR EdgeService 전용화

- JPXLPR은 카메라 최대 4대, 번호인식, 이미지 저장만 담당
- 인식 결과를 `STX + CP949 표준 파일명 + ETX`로 EdgeService TCP `29200`에 전송
- 파일명에 `sitenum`, `groupnum`, `devicenum`, `laneid`, 방향, 시각, 차량번호, EventId 저장
- ACK를 받은 사건만 Outbox에서 삭제하고 장애 시 동일 EventId로 재전송
- 중앙 REST API, 관리 UDP, APS 직접 연결, LDM 전광판, 차단기, DB 모델 제거
- 설정은 `SITENUM`, `GROUPNUM`, `EDGESERVICEHOST`, `EDGESERVICEPORT`와 카메라별 `laneid`, `devicenum`, `direction` 사용
- 관리자 종료 비밀번호는 소스에 저장하지 않고 `JPXLPR_MANAGER_PASSWORD` 환경변수로 설정
- 전체 실행시험은 저장소 최상위 `run-jpxlpr-edge-test.bat` 사용

## 9. 새 채팅 시작 문구

다음 문장과 이 문서를 함께 제공하면 된다.

> GitHub 저장소 https://github.com/kangho-Shin/ParkingSystem 의 `codex/server-edge-foundation` 브랜치 작업을 계속 진행해줘. 먼저 `docs/04-development-status.md`를 읽고 완료된 작업은 반복하지 마. 다음 우선순위인 `Parking.TerminalAgent`의 프로그램 자동 실행, 비정상 종료 재시작, 반복 장애 재시작 제한과 로그 기능부터 설계·구현해줘. 기능을 한꺼번에 구현한 뒤 전체 빌드와 테스트를 몰아서 진행하고, 여러 프로그램 실행시험은 환경변수와 연결 문자열을 포함한 저장소 최상위 배치 파일 하나로 만들며 각 단계 사이에 `pause`를 넣어줘. WinForms는 반드시 `.cs`, `.Designer.cs`, `.resx` 구조로 작성해줘.

현재 확정사항:

- `Parking.KioskSimulator`를 임시 무인정산기 대역으로 사용한다.
- 실제 무인정산기와 등록차량 관리 프로그램 연동은 운영 소스 확보 후 진행한다.
- 별도 `Parking.Payment`는 만들지 않는다. 승인·취소·망취소는 무인정산기가 담당한다.
- LPR 4카메라 연속수신 80건 실제시험과 WinForms Designer 분리는 완료됐다.
