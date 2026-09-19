# API 규격

## 1. 공통 규칙

- 형식: HTTP JSON, UTF-8
- JSON 속성명: PascalCase
- 날짜·시간: ISO 8601
- `EventId`: 입·출차 재전송 중복방지 UUID
- `PaymentId`: 결제 재전송 중복방지 UUID
- 기본 주소
  - Parking.Api: `http://localhost:5000`
  - Parking.EdgeGateway: `http://localhost:5100`
  - Parking.EdgeService: `http://localhost:5200`

## 2. 공통 입출차 모델

입차와 출차는 각각 `InDateTime`/`InImage`, `OutDateTime`/`OutImage`를 사용한다. 별도의 촬영시간 필드는 두지 않는다.

### FieldEventRequest

```json
{
  "EventId": "4a912811-cb4d-4c3f-a70a-fe60504c3ef7",
  "SiteId": 1,
  "LaneId": 10,
  "DeviceId": 101,
  "CarNumber": "12가3456",
  "InDateTime": "2026-09-19T15:30:00+09:00",
  "Groupnum": 1,
  "EventType": "Entry",
  "InImage": "C:\\ParkingSystem\\Images\\001_001_010_Entry_test.jpg"
}
```

### ExitEventRequest

`OutDateTime`, `Groupnum`, `CarType`, `DiscountKeys`, `EventType=Exit`, `OutImage`를 사용한다. 출차 계산은 DB에 저장된 세션의 `Groupnum`, `CarType`과 할인내역을 사용한다.

### FieldEventResponse

```json
{
  "EventId": "4a912811-cb4d-4c3f-a70a-fe60504c3ef7",
  "Accepted": true,
  "ParkingSessionId": 53,
  "ResultCode": "ENTRY_ACCEPTED",
  "DisplayMessage": "입차되었습니다.",
  "OpenBarrier": true
}
```

주요 결과코드:

| ResultCode | 의미 |
|---|---|
| `ENTRY_ACCEPTED` | 중앙 입차 저장 완료 |
| `EDGE_OFFLINE_ENTRY` | 중앙 단절, 현장 저장 후 입차 허용 |
| `EXIT_ACCEPTED` | 출차 완료 |
| `OPEN_SESSION_NOT_FOUND` | 미출차 주차 건 없음 |
| `PAYMENT_REQUIRED` | 미납 또는 추가요금 존재 |
| `CENTRAL_OFFLINE_EXIT_BLOCKED` | 중앙 단절로 출차 차단 |

## 3. Parking.Api

### POST `/api/v1/parking/entries`

- 요청: `FieldEventRequest`
- 성공: HTTP 200 + `FieldEventResponse`
- 오류: 빈 EventId 또는 잘못된 SiteId/LaneId/DeviceId는 HTTP 400
- `Sitenum`, `Groupnum`, `LaneId`, `DeviceId`, `EventType=Entry`가 활성 차로·장비 설정과 일치해야 한다.
- 같은 EventId를 다시 보내면 최초 `ParkingSessionId`와 결과를 반환한다.

### GET `/api/v1/parking/open?siteId={siteId}&carNumber={carNumber}`

- 성공: HTTP 200 + `OpenParkingSessionResponse`
- 없음: HTTP 404

응답 필드: `ParkingSessionId`, `SiteId`, `CarNumber`, `Groupnum`, `CarType`, `EntryLaneId`, `EntryAt`, `Paydate`, `Status`.

### POST `/api/v1/parking/exits`

- 요청: `ExitEventRequest`
- 성공 여부와 관계없이 정상 처리 결과는 HTTP 200 + `FieldEventResponse`
- 요청 형식 오류 또는 출차시각이 입차시각보다 빠르면 HTTP 400
- `Sitenum`, `Groupnum`, `LaneId`, `DeviceId`, `EventType=Exit`가 활성 차로·장비 설정과 일치해야 한다.
- 현재 요금, 할인, 기존 결제, 사전정산 유예시간을 다시 계산하여 `OpenBarrier`를 결정한다.

### POST `/api/v1/fees/calculate`

요청:

```json
{
  "Sitenum": 1,
  "Groupnum": 1,
  "EntryAt": "2026-09-19T08:00:00",
  "ExitAt": "2026-09-19T09:00:00",
  "CarType": 1,
  "DiscountKeys": [10]
}
```

성공 응답: HTTP 200

```json
{
  "OriginalFee": 600,
  "FinalFee": 300,
  "DiscountFee": 300,
  "ParkingMinutes": 60,
  "DailyCharges": []
}
```

필수값이 잘못되면 HTTP 400.

### POST `/api/v1/fees/quote`

요청:

```json
{
  "Sitenum": 1,
  "CarNumber": "12가3456",
  "ExitAt": "2026-09-19T09:00:00+09:00"
}
```

응답 필드:

- `ParkingSessionId`, `CarNumber`, `EntryAt`, `ExitAt`
- `Fee`: 원요금·최종요금·할인액·주차시간·일자별요금
- `PreviousPaidAmount`: 기존 결제금액
- `PayableAmount`: 현재 추가 결제금액
- `IsPrepayGrace`: 사전정산 유예시간 적용 여부

미출차 차량이 없으면 HTTP 404, 요청 오류는 HTTP 400.

### GET `/api/v1/parking/search`

쿼리: `siteId`, `groupnum`, `carNumber`, `exitAt`.

- `carNumber`가 4자리이면 뒤 4자리, 그 외에는 전체번호로 조회한다.
- `Sitenum`, `Groupnum`, `outflag<>'O'` 조건을 적용한다.
- 여러 건이면 최신 입차순 `Candidates`를 반환한다.
- 한 건이면 즉시 `QuoteParkingFeeResponse`를 반환한다.

### POST `/api/v1/fees/quote/session`

```json
{
  "ParkingSessionId": 53,
  "ExitAt": "2026-09-19T09:00:00+09:00"
}
```

여러 후보 중 선택한 주차 건만 다시 계산한다. 결제할 금액이 있으면 `I`를 유지하고, 최종 결제금액이 0원이면 `X`로 변경한다.

### POST `/api/v1/payments/complete`

요청:

```json
{
  "PaymentId": "11111111-1111-1111-1111-111111111111",
  "ParkingSessionId": 53,
  "SiteId": 1,
  "OriginalFee": 600,
  "DiscountFee": 0,
  "PaidAmount": 600,
  "PaymentMethod": "CARD",
  "ApprovalNumber": "12345678",
  "TerminalId": "KIOSK-01",
  "PaidAt": "2026-09-19T09:00:00+09:00"
}
```

응답: `PaymentId`, `ParkingSessionId`, `Accepted`, `ResultCode`, `Message`, `ExitAllowed`.

| HTTP | ResultCode | 의미 |
|---|---|---|
| 200 | `PAYMENT_COMPLETED` | 결제 저장 완료 또는 동일 결제 재전송 |
| 404 | `PARKING_SESSION_NOT_FOUND` | 주차 건 없음 |
| 409 | `PAYMENT_ID_CONFLICT` | 같은 PaymentId의 내용이 다름 |
| 409 | `PARKING_SESSION_ALREADY_PAID` | 다른 PaymentId로 이미 결제됨 |
| 409 | `PARKING_SESSION_EXITED` | 이미 출차됨 |
| 400 | - | 필수값 또는 금액 관계 오류 |

현재 정책은 별도 요금 견적 저장이나 서버 측 결제금액 재검증을 하지 않고, 요청 필드의 기본 관계와 PG 승인결과 저장만 처리한다.

### 설정 API

| Method | URL | 요청/응답 | 정상 상태 |
|---|---|---|---|
| GET | `/api/v1/config/sites/{siteId}` | `SiteConfiguration` 응답 | 200/404 |
| PUT | `/api/v1/config/sites/{siteId}` | `ParkingSite` | 204 |
| PUT | `/api/v1/config/lanes/{laneId}` | `ParkingLane` | 204 |
| PUT | `/api/v1/config/devices/{deviceId}` | `ParkingDevice` | 204 |

## 4. Parking.EdgeGateway

| Method | URL | 전달 대상 |
|---|---|---|
| POST | `/api/v1/edge/events` | API `/api/v1/parking/entries` |
| POST | `/api/v1/edge/exits` | API `/api/v1/parking/exits` |
| GET | `/api/v1/edge/config/sites/{siteId}` | API `/api/v1/config/sites/{siteId}` |
| POST | `/api/v1/edge/fees/quote` | API `/api/v1/fees/quote` |
| GET | `/api/v1/edge/parking/search` | API `/api/v1/parking/search` |
| POST | `/api/v1/edge/fees/quote/session` | API `/api/v1/fees/quote/session` |
| POST | `/api/v1/edge/payments/complete` | API `/api/v1/payments/complete` |

요금·결제 중계는 JSON 본문과 HTTP 상태를 그대로 반환한다. 연결 실패와 시간초과는 HTTP 503이다.

## 5. Parking.EdgeService

| Method | URL | 기능 |
|---|---|---|
| GET | `/api/v1/local/config` | SQLite에 저장된 현장 설정 조회 |
| POST | `/api/v1/edge/events` | 입차를 Outbox 저장 후 중앙 전송 |
| POST | `/api/v1/edge/exits` | 출차를 Outbox 저장 후 중앙 전송 |
| POST | `/api/v1/local/fees/quote` | 요금 견적 중계 |
| GET | `/api/v1/local/parking/search` | 전체번호·뒤 4자리 검색 중계 |
| POST | `/api/v1/local/fees/quote/session` | 선택 주차 건 견적 중계 |
| POST | `/api/v1/local/payments/complete` | 결제 결과 Outbox 저장 후 중계 |

결제 중앙 전송이 실패하면 HTTP 202를 반환한다.

```json
{
  "Accepted": true,
  "ResultCode": "PAYMENT_PENDING_SYNC",
  "Message": "결제결과 중앙 전송 대기 중입니다."
}
```

## 6. 등록차량 연동 예정 API

일반차량 검색은 구현됐으며, 아래 등록차량 통합은 실제 운영 테이블 스키마를 받은 뒤 진행한다.

### GET `/api/v1/parking/search?siteId={siteId}&groupnum={groupnum}&carNumber={number}`

- `carNumber`에 전체 차량번호 또는 뒤 4자리를 입력
- 일반차량과 등록차량의 미출차 내역을 통합 반환
- 최신 입차시간순 정렬
- 차량번호, 입차시간, 입차이미지, 차량구분, 주차 식별자 반환
- 여러 건을 그대로 반환하며 서버에서 임의 선택하지 않음
- 결과가 한 건이면 해당 주차 건의 요금을 즉시 계산
- 결과가 여러 건이면 정산기 선택 후 `ParkingSessionId`로 다시 계산 요청
- 전체 차량번호 조회도 같은 처리 흐름 사용
- 조회·계산만으로는 `outflag=I` 유지
- 결제완료 또는 최종요금 0원 확정 시 `outflag=X`

### 등록차량 선택 정산

여러 차량 중 정산기에서 선택한 주차 건을 다시 계산한다.

```json
{
  "ParkingSessionId": 53,
  "ExitAt": "2026-09-19T09:00:00+09:00"
}
```

선택은 차량번호가 아니라 `ParkingSessionId`를 사용한다.

이 API는 규격 확정 상태이며 아직 구현되지 않았다.
