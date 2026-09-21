# APSMain EdgeService 연동 설계

## 1. 목적

기존 `APSMain_C(API)V2`의 WinForms 화면, BF 기능, 카드단말, 프린터, 음성안내를 유지하면서 서버 통신을 현재 주차 시스템 구조에 연결한다.

APSMain은 중앙 서버나 MySQL에 직접 연결하지 않고 현장 `Parking.EdgeService`만 사용한다. 출구 LPR 수신, 전광판 표시, 차단기 제어는 EdgeService가 담당한다.

## 2. 적용 범위

### 유지

- 15인치·24인치 WinForms 화면
- `.cs`, `.Designer.cs`, `.resx` 구조
- BF 음성·확대·고대비·키보드 이동
- 스마트로·KICC 승인, 취소, 망취소
- 프린터와 영수증 출력
- 차량번호 수동 입력과 복수 차량 선택
- 사전정산과 출구정산 화면 흐름

### 교체

- 기존 `/api/Insert`, `/api/Carcalc`, `/api/CarPay`, `/api/Carout` 호출
- APSMain의 출구 LPR 직접 수신
- APSMain의 LDM·차단기 직접 제어
- 로컬 `ParkFeeCalculator`를 이용한 최종 요금 확정
- MySQL·EF Core 직접 연결

### 이번 1차 구현

- EdgeService SignalR 연결 및 장비 등록
- `ExitVehicleDetected` 수신
- 차량검색
- 단일 차량 요금조회
- 복수 차량 후보 전달
- 중복 EventId 방지와 재연결

결제완료, 사건완료, 할인 재계산은 후속 단계에서 연결한다.

## 3. 디렉터리 구조

기존 APSMain 프로젝트를 복사하지 않는다. 다음 연동 계층을 프로젝트 내부에 추가한다.

```text
APSMain_C(API)V2/
  Integration/
    EdgeService/
      EdgeServiceOptions.cs
      EdgeServiceClient.cs
      KioskSignalRClient.cs
      KioskExitContext.cs
      ParkingSearchResult.cs
      EdgeServiceContracts.cs
```

폼은 이 클래스들을 호출하고 결과만 표시한다. 15인치와 24인치 폼에 HTTP·SignalR 코드를 중복해서 넣지 않는다.

## 4. 연결 설정

`App.config`에는 다음 현장 설정만 둔다.

- `EDGESERVICEURL`: 기본값 `http://localhost:5200`
- `SITENUM`: 현장 ID
- `GROUPNUM`: 그룹 번호
- `APSNUM`: 현장 무인정산기 장비번호
- `APSNUM`: 기존 무인 내부 장치번호가 필요할 때만 유지

DB 계정, DB 비밀번호, 중앙 API 주소, API 키는 APSMain에서 제거한다.

APSMain은 `SITENUM`, `GROUPNUM`, `APSNUM`만 전달하고 EdgeService가 현장 설정에서 실제 `parking_device.deviceid`를 찾아 내부 처리에 사용한다.

## 5. 구성요소

### EdgeServiceOptions

App.config 값을 읽고 필수값을 검증한다. URL, Sitenum, Groupnum, Devicenum을 제공한다.

### EdgeServiceClient

하나의 `HttpClient`를 재사용한다.

- `SearchParkingAsync`
- `QuoteSessionAsync`
- `CompletePaymentAsync`
- `CompleteKioskEventAsync`

HTTP 오류, 시간초과, JSON 오류를 공통 결과 형식으로 반환한다. UI 폼에서 `GetAwaiter().GetResult()`를 사용하지 않는다.

### KioskSignalRClient

- `/hubs/kiosk` 연결
- 연결 후 `Register(SITENUM, GROUPNUM, APSNUM)` 호출
- 자동 재연결 후 재등록
- `ExitVehicleDetected` 이벤트 전달
- 시작·정지·재연결 상태 로그

SignalR 콜백에서는 화면을 직접 조작하지 않는다. MainForm이 UI 스레드로 전환해 처리한다.

### KioskExitContext

한 출구 사건의 상태를 보관한다.

- EventId
- SiteId, Groupnum
- LprDeviceId, KioskDeviceId
- CarNumber
- OutDateTime, OutImage
- ParkingSessionId
- OriginalFee, DiscountFee, PreviousPaidAmount, PayableAmount
- 선택한 DiscountKeys
- PaymentId, ApprovalNumber
- 처리 상태

### EventId 중복방지

SignalR 재연결 시 미완료 사건이 다시 전송될 수 있다. EventId별 처리 상태를 보관하여 동일 화면과 카드승인이 중복 실행되지 않게 한다.

완료 전 사건은 다시 처리할 수 있고, 완료된 사건은 무시한다.

## 6. 출구정산 흐름

```text
JPXLpr
  → EdgeService LPR TCP
  → SignalR ExitVehicleDetected
  → APSMain 차량검색
  → 요금표시·할인·카드승인
  → 결제완료 API
  → Kiosk 사건완료 API
  → EdgeService LDM·차단기
```

### 차량검색

```http
GET /api/v1/local/parking/search
    ?siteId={SiteId}
    &groupnum={Groupnum}
    &carNumber={CarNumber}
    &exitAt={OutDateTime}
```

- 단일 후보: 서버가 요금 결과를 반환하므로 바로 정산화면을 표시한다.
- 복수 후보: 후보 목록을 차량선택 화면에 표시한 후 선택한 `ParkingSessionId`로 다시 요금을 조회한다.
- 404: 등록차량 또는 입차내역 없음 가능성이 있으므로 결제화면을 열지 않는다. 후속 사건완료 단계에서 중앙 판정 결과를 표시한다.
- 503·시간초과: 사건완료를 보내지 않고 연결 복구 후 재처리한다.

### 할인 재계산

```http
POST /api/v1/local/fees/quote/session
```

요청에는 `ParkingSessionId`, `ExitAt`, `DiscountKeys`를 보낸다. 최종 요금은 서버 응답의 `PayableAmount`를 사용한다.

### 결제완료

카드단말 승인 성공 후에만 다음 API를 호출한다.

```http
POST /api/v1/local/payments/complete
```

APSMain이 새로운 `PaymentId`를 생성하고 승인번호, 단말기 ID, 승인시각을 함께 보낸다. 승인·취소·망취소 자체는 APSMain 책임이다.

### Kiosk 사건완료

결제완료가 성공했거나 서버 계산금액이 0원인 경우에 호출한다.

```http
POST /api/v1/local/kiosks/events/{EventId}/complete
```

EdgeService가 중앙 출차 저장, LDM 표시, 차단기 개방을 처리한다. APSMain은 같은 사건에 완료 API를 중복 호출하지 않는다.

## 7. 사전정산 흐름

사전정산기는 SignalR 사건 없이 기존 차량번호 입력 화면에서 시작한다.

```text
차량번호 입력
  → 차량검색
  → 단일 또는 복수 후보 선택
  → 서버 요금조회
  → APSMain 카드승인
  → 결제완료 API
```

사전정산은 Kiosk 사건완료 API를 호출하지 않으며 차단기를 제어하지 않는다.

## 8. 폼 연결

### MainForm / MainForm15

- 공통 SignalR 클라이언트 시작·종료
- 사건 수신 후 UI 스레드 전환
- `KioskExitContext` 생성
- 기존 LPR 서버와 LDM 초기화는 새 운전모드에서 비활성화

### CarInNumForm / CarInNumForm15

- `/api/Carcalc` 대신 `SearchParkingAsync` 사용
- 기존 `ParkCache` DB 모델 대신 새 검색 결과 사용

### ParkCalForm / ParkCalForm15

- 로컬 요금계산 대신 서버 요금 응답 표시
- 할인 변경 시 `QuoteSessionAsync` 호출
- 카드승인 성공 후 `CompletePaymentAsync` 호출
- 출구정산일 때만 `CompleteKioskEventAsync` 호출

폼별 차이는 화면 배치로 한정하고 업무 흐름은 공통 연동 계층에서 처리한다.

## 9. 서버 보완사항

APSMain 전체 기능 연결 전에 다음 API가 필요하다.

1. 사이트·그룹별 할인 코드와 표시명 조회
2. 무인정산기용 운영변수 조회
3. 등록차량 연장 견적과 결제완료
4. Kiosk용 입차 이미지 조회

1차 SignalR·차량검색 구현에는 기존 EdgeService API만 사용한다.

## 10. 장애 처리

- SignalR 단절: 자동 재연결하고 Sitenum, Groupnum, Devicenum 재등록
- EdgeService 503: 화면에 통신장애 표시, 결제 시작 금지
- 결제 승인 후 API 장애: 승인 결과를 로컬에 보존하고 동일 PaymentId로 재전송
- 사건완료 장애: 동일 EventId로 재시도
- 프로그램 재시작: 미전송 결제와 미완료 사건 복구
- UI 폼 종료: 이벤트 구독 해제 및 `IsDisposed`, `IsHandleCreated` 확인

결제 승인 결과의 로컬 영속 저장 방식은 결제 연동 단계에서 별도 설계한다.

## 11. 보안과 소스 정리

- App.config와 `UparkdbContext.cs`에 포함된 DB 비밀번호를 변경한다.
- Git 기록에 포함된 비밀번호는 폐기된 값으로 만든다.
- MySQL.Data, Pomelo EF Core, Dapper는 실제 참조 제거 후 프로젝트 패키지에서 제거한다.
- 새 설정에는 DB 비밀번호와 중앙 서버 인증정보를 저장하지 않는다.
- CP949 소스는 필요한 파일만 수정하고 전체 디렉터리 인코딩을 일괄 변환하지 않는다.

## 12. 구현 단계

1. 공통 계약과 EdgeService 통신 클래스 추가
2. SignalR 연결·재연결·EventId 중복방지 시험
3. 차량검색과 단일·복수 응답 시험
4. MainForm과 MainForm15에 공통 사건 연결
5. 서버 요금조회와 할인 재계산
6. 결제완료와 로컬 재전송 저장
7. Kiosk 사건완료와 실제 차단기 시험
8. 사전정산 연결
9. 등록차량 연장 연결
10. 기존 DB·LPR·LDM 직접 처리 제거

각 단계는 자동시험 후 Windows 실행시험을 진행한다. 여러 프로그램 실행시험은 저장소 최상위 배치 파일 하나에 환경변수와 연결 문자열을 포함하고 단계 사이에 `pause`를 둔다.

## 13. 1차 완료조건

- APSMain이 EdgeService SignalR에 연결된다.
- 현장·그룹·장비번호가 실제 Kiosk DeviceId로 변환되어 등록된다.
- LPR 출차 사건을 한 번만 화면 흐름에 전달한다.
- 연결이 끊겼다가 복구되면 자동 재등록한다.
- 차량검색의 단일 요금과 복수 후보를 구분한다.
- 15인치와 24인치가 같은 연동 클래스를 사용한다.
- 기존 화면·카드단말·프린터 동작에는 변경이 없다.
- APSMain이 MySQL, LPR, LDM에 새로 직접 접근하지 않는다.
