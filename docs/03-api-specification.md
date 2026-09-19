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

### FieldEventRequest

```json
{
  "EventId": "4a912811-cb4d-4c3f-a70a-fe60504c3ef7",
  "SiteId": 1,
  "LaneId": 10,
  "DeviceId": 101,
  "CarNumber": "12가3456",
  "OccurredAt": "2026-09-19T15:30:00+09:00"
}
```

### ExitEventRequest

`FieldEventRequest`와 같은 필드에 `Groupnum`, `CarType`, `DiscountKeys`가 추가된다. 현재 출차 계산은 DB에 저장된 세션의 `Groupnum`, `CarType`과 할인내역을 사용한다.

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
- 같은 EventId를 다시 보내면 최초 `ParkingSessionId`와 결과를 반환한다.

### GET `/api/v1/parking/open?siteId={siteId}&carNumber={carNumber}`

- 성공: HTTP 200 + `OpenParkingSessionResponse`
- 없음: HTTP 404

응답 필드: `ParkingSessionId`, `SiteId`, `CarNumber`, `Groupnum`, `CarType`, `EntryLaneId`, `EntryAt`, `Paydate`, `Status`.

### POST `/api/v1/parking/exits`

- 요청: `ExitEventRequest`
- 성공 여부와 관계없이 정상 처리 결과는 HTTP 200 + `FieldEventResponse`
- 요청 형식 오류 또는 출차시각이 입차시각보다 빠르면 HTTP 400
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
| POST | `/api/v1/edge/payments/complete` | API `/api/v1/payments/complete` |

요금·결제 중계는 JSON 본문과 HTTP 상태를 그대로 반환한다. 연결 실패와 시간초과는 HTTP 503이다.

## 5. Parking.EdgeService

| Method | URL | 기능 |
|---|---|---|
| GET | `/api/v1/local/config` | SQLite에 저장된 현장 설정 조회 |
| POST | `/api/v1/edge/events` | 입차를 Outbox 저장 후 중앙 전송 |
| POST | `/api/v1/edge/exits` | 출차를 Outbox 저장 후 중앙 전송 |
| POST | `/api/v1/local/fees/quote` | 요금 견적 중계 |
| POST | `/api/v1/local/payments/complete` | 결제 결과 Outbox 저장 후 중계 |

결제 중앙 전송이 실패하면 HTTP 202를 반환한다.

```json
{
  "Accepted": true,
  "ResultCode": "PAYMENT_PENDING_SYNC",
  "Message": "결제결과 중앙 전송 대기 중입니다."
}
```

