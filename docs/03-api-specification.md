# API 및 통신 규격

## 1. 공통 규칙

- HTTP JSON 인코딩: UTF-8
- JSON 속성명: PascalCase
- 날짜 형식: ISO 8601
- 운전 시간 DB 저장: 한국시간, 초 단위
- 입출차 멱등키: `EventId`
- 결제 멱등키: `PaymentId`
- 기본 주소
  - Parking.Api: `http://localhost:5000`
  - Parking.EdgeGateway: `http://localhost:5100`
  - Parking.EdgeService: `http://localhost:5200`

현장 인증이 필요한 Gateway 요청은 `X-Site-Key`를 사용한다.

## 2. Parking.Api

### 입출차

| Method | URL | 기능 |
|---|---|---|
| POST | `/api/v1/parking/entries` | 입차 저장 |
| GET | `/api/v1/parking/open` | 차량번호 미출차 조회 |
| POST | `/api/v1/parking/exits` | 출차 판정과 저장 |
| POST | `/api/v1/parking/exits/kiosk-offline-open` | Kiosk 장애 개방 출차 |
| GET | `/api/v1/parking/search` | 전체번호·뒤 4자리 검색 |
| PUT | `/api/v1/parking/sessions/{parkingSessionId}/car-number` | 미출차 차량번호 정정 |

### 요금과 결제

| Method | URL | 기능 |
|---|---|---|
| POST | `/api/v1/fees/calculate` | 입력값 기준 요금 계산 |
| POST | `/api/v1/fees/quote` | 차량번호 기준 정산 견적 |
| POST | `/api/v1/fees/quote/session` | 선택 세션 재계산 |
| POST | `/api/v1/payments/complete` | 결제완료 저장 |

차량조회와 유료 견적만으로 `outflag`를 변경하지 않는다. 실제 결제완료 또는 최종금액 0원 확정 시 `X`로 변경한다.

### 등록차량

| Method | URL | 기능 |
|---|---|---|
| GET | `/api/v1/period/members/search` | 유효 등록회원 판정 |
| GET | `/api/v1/period/open` | 등록차량 미출차 조회 |
| GET | `/api/v1/period/members` | 관리목록 조회 |
| GET | `/api/v1/period/members/{memberId}` | 한 건 조회 |
| POST | `/api/v1/period/members` | 추가 |
| PUT | `/api/v1/period/members/{memberId}` | 수정 |
| DELETE | `/api/v1/period/members/{memberId}` | 실제 삭제 |

### 현장 설정

| Method | URL |
|---|---|
| GET | `/api/v1/config/sites/{siteId}` |
| GET | `/api/v1/config/sites/{siteId}/versioned` |
| PUT | `/api/v1/config/sites/{siteId}/versioned` |
| PUT | `/api/v1/config/sites/{siteId}` |
| PUT | `/api/v1/config/lanes/{laneId}` |
| PUT | `/api/v1/config/devices/{deviceId}` |
| PUT | `/api/v1/config/device-links/{sourceDeviceId}/{targetDeviceId}` |

## 3. Parking.EdgeGateway

기본 경로는 `/api/v1/edge`다.

| Method | URL | 기능 |
|---|---|---|
| POST | `/events` | 입차 중계 |
| POST | `/exits` | 출차 중계 |
| POST | `/exits/kiosk-offline-open` | 장애 개방 출차 중계 |
| GET | `/config/sites/{siteId}` | 현장 설정 조회 |
| GET/PUT | `/config/sites/{siteId}/versioned` | 버전 설정 동기화 |
| POST | `/fees/quote` | 요금 견적 |
| GET | `/parking/search` | 차량 검색 |
| POST | `/fees/quote/session` | 선택 세션 견적 |
| POST | `/payments/complete` | 결제완료 |
| PUT | `/parking/sessions/{parkingSessionId}/car-number` | 차량번호 정정 |
| GET | `/health` | Gateway와 중앙 상태 |

## 4. Parking.EdgeService 로컬 API

### 운전 API

| Method | URL | 기능 |
|---|---|---|
| POST | `/api/v1/edge/events` | 로컬 입차 접수 |
| POST | `/api/v1/edge/exits` | 로컬 출차 접수 |
| GET | `/api/v1/local/parking/search` | 차량 검색 |
| POST | `/api/v1/local/fees/quote` | 차량 견적 |
| POST | `/api/v1/local/fees/quote/session` | 선택 세션 견적 |
| POST | `/api/v1/local/payments/complete` | 결제완료 |
| PUT | `/api/v1/local/parking/sessions/{parkingSessionId}/car-number` | 차량번호 정정 |

### Kiosk와 LDM

| Method | URL | 기능 |
|---|---|---|
| POST | `/api/v1/local/kiosks/events/{eventId}/complete` | 출구 Kiosk 사건완료 |
| POST | `/api/v1/local/kiosks/display` | 요금·안내 표시 |
| POST | `/api/v1/local/kiosks/manual-exit/complete` | 수동 출차완료 |
| POST | `/api/v1/local/kiosks/display/reset` | 현재 시각 표시로 복귀 |
| POST | `/api/v1/local/operator/barrier/open` | 유인 수동개방 |

Kiosk 사건완료 요청 본문은 `Sitenum`, `Groupnum`, `Devicenum`을 사용한다. 클라이언트가 내부 `deviceid`나 차단기 개방 여부를 지정하지 않는다.

### 로컬 설정

| Method | URL |
|---|---|
| GET/PUT | `/api/v1/local/setup` |
| GET | `/api/v1/local/config` |
| PUT | `/api/v1/local/config/site` |
| PUT/DELETE | `/api/v1/local/config/lanes/{laneId}` |
| PUT/DELETE | `/api/v1/local/config/devices/{deviceId}` |
| PUT/DELETE | `/api/v1/local/config/device-links` |
| PUT/DELETE | `/api/v1/local/config/variables` |

### 관리 API

| Method | URL | 기능 |
|---|---|---|
| GET | `/api/v1/management/status` | 연결상태, Outbox, 설정 동기화 |
| GET | `/api/v1/management/entries?limit=1000` | 현재 입차 목록 |
| GET | `/api/v1/management/activities?limit=1000` | 최근 처리 목록 |
| GET | `/api/v1/management/configuration` | 현장 설정 |

## 5. SignalR

- Hub: `/hubs/kiosk`
- 등록: `Register(sitenum, groupnum, devicenum)`
- 서버 사건: `ExitVehicleDetected`

재연결 후 같은 외부 식별값으로 다시 등록한다. 잘못된 장치는 `INVALID_SITE`, `DEVICE_NOT_FOUND`, `DEVICE_AMBIGUOUS` 등으로 거부한다.

## 6. LPR TCP

### 요청

```text
STX(0x02) + CP949 이미지 파일명 + ETX(0x03)
```

기본 포트는 29200이며 DATA 최대 길이는 1,024바이트다.

### 이미지 파일명

```text
{sitenum}_{groupnum:000}_{devicenum:000}_{laneid:0000}_{Entry|Exit}_{yyyyMMddHHmmssfff}_{차량번호}_{EventId:N}.jpg
```

예:

```text
9001_002_402_9020_Exit_20260922153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.jpg
```

### 응답

```text
STX + ACK|EventId + ETX
STX + NAK|EventId|ErrorCode + ETX
```

출차가 미정산으로 차단돼도 패킷과 사건 처리가 정상이면 ACK다. 파일명, 인코딩, 현장·차로·장치·방향 오류만 NAK로 응답한다.

## 7. 주요 결과코드

| 코드 | 의미 |
|---|---|
| `ENTRY_ACCEPTED` | 중앙 입차 저장 |
| `EDGE_OFFLINE_ENTRY` | 중앙 장애 중 현장 보관과 입차 허용 |
| `EXIT_ACCEPTED` | 출차 완료 |
| `PAYMENT_REQUIRED` | 미납 또는 추가요금 |
| `OPEN_SESSION_NOT_FOUND` | 미출차 건 없음 |
| `CENTRAL_OFFLINE_EXIT_BLOCKED` | 중앙 장애로 출차 차단 |
| `PAYMENT_COMPLETED` | 결제 저장 또는 동일 결제 재전송 |
| `PAYMENT_PENDING_SYNC` | 승인 결과 Outbox 보관 |
