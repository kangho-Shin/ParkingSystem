# 무인정산기 SignalR 및 장비 연결 설계

## 1. 목적

실제 무인정산기 프로그램을 수정하기 전에 EdgeService에 무인정산기용 실시간 알림, 장애 정책, 장비 연결 구조를 구현하고 시험용 클라이언트로 검증한다. LPR과 무인정산기는 연계 또는 단독으로 운영할 수 있고, 전광판은 양쪽에서 공유할 수 있다.

## 2. 확정 운영 원칙

- 실제 LPR 입력은 현재 TCP `STX + CP949 파일명 + ETX`를 그대로 사용한다.
- 가상 LPR API는 만들지 않는다.
- 차량조회, 요금계산, 할인, 결제는 기존 EdgeService REST API를 재사용한다.
- 실시간 출구차량 통보만 SignalR을 사용한다.
- 장비가 서로 직접 통신하지 않고 모든 판단과 명령은 EdgeService를 거친다.
- 파일명의 `DeviceId`는 LPR 본체가 아니라 카메라별 장치번호다.
- LPR 본체 한 대의 카메라 수는 1~4대이며, LPR 본체 접속 수에는 4대 제한을 두지 않는다.

## 3. 지원 구성

### 3.1 LPR 단독

출차 LPR 사건을 EdgeService가 처리한다. 연결된 무인정산기가 없으면 EdgeService가 LPR에 연결된 전광판으로 차량번호와 처리결과를 표시하고, 출차 가능 결과일 때 차단기 열림 명령을 보낸다.

### 3.2 무인정산기 단독

운전자가 차량번호를 직접 조회한다. 무인정산기는 기존 REST API로 조회·정산·결제를 처리하고, 완료 결과를 EdgeService에 전달한다. EdgeService는 무인정산기에 연결된 전광판으로 안내와 차단기 명령을 보낸다.

### 3.3 LPR과 무인정산기 연계

출차 LPR 사건을 EdgeService가 처리한 뒤 연결된 무인정산기가 온라인이면 SignalR로 전달한다. 무료차량·등록차량도 LPR이 직접 차단기를 열지 않고 반드시 무인정산기가 음성 안내와 화면 처리를 완료한 뒤 결과를 EdgeService에 반환한다. 이후 EdgeService가 전광판 표시와 차단기 명령을 처리한다.

## 4. 장비 연결 모델

단일 `parking_device.linkeddeviceid` 필드는 사용하지 않는다. 한 장비가 여러 장비와 연결되고 하나의 전광판을 LPR과 무인정산기가 공유할 수 있으므로 별도 연결 테이블을 사용한다.

### 4.1 `parking_device_link`

| 열 | 의미 |
|---|---|
| `sitenum` | 현장번호 |
| `sourcedeviceid` | 사건 또는 명령을 발생시키는 장비 |
| `targetdeviceid` | 통보 또는 제어 대상 장비 |
| `linktype` | `KIOSK` 또는 `LDM` |
| `useflag` | 사용 여부 |

기본키는 `(sitenum, sourcedeviceid, targetdeviceid, linktype)`이다. 출발·대상 장비는 동일 현장에 등록되어야 하고 자기 자신을 연결할 수 없다.

예시는 다음과 같다.

- LPR 카메라 → 출구 무인정산기: `linktype=KIOSK`
- LPR 카메라 → 전광판: `linktype=LDM`
- 출구 무인정산기 → 전광판: `linktype=LDM`

대상 장비의 `DeviceType`이 연결 종류와 일치해야 한다. LPR과 무인의 연계 여부는 활성 `KIOSK` 연결 존재 여부로 판단한다.

후속 마이그레이션은 기존 `linkeddeviceid` 값을 대상 장비의 `DeviceType`에 따라 연결 테이블로 옮긴 뒤 기존 외래키와 열을 제거한다. 따라서 `009_device_link.sql`을 이미 적용한 DB와 신규 DB를 모두 지원한다.

## 5. SignalR 연결

EdgeService에 `/hubs/kiosk` 허브를 추가한다. 무인정산기는 연결 직후 자신의 `DeviceId`를 등록한다. 서버는 SignalR 연결 ID와 무인정산기 DeviceId를 일대일로 관리하며 재접속 시 최신 연결로 교체한다.

서버가 보내는 `ExitVehicleDetected` 알림은 다음 값을 포함한다.

- `EventId`
- `SiteId`
- `Groupnum`
- LPR 카메라 `DeviceId`
- 대상 무인정산기 `KioskDeviceId`
- `CarNumber`
- `OutDateTime`
- `OutImage`
- 기존 출차 처리의 `ResultCode`, `DisplayMessage`, `OpenBarrier`

SignalR 알림은 처리 명령이 아니라 화면·음성 처리를 시작하라는 사건 통보다. 무인정산기는 REST API로 필요한 차량·요금 정보를 다시 조회한다.

## 6. 무인정산기 완료 API

시험용 클라이언트와 향후 실제 무인정산기가 공통으로 호출할 로컬 API를 추가한다.

- `POST /api/v1/local/kiosks/events/{eventId}/complete`와 본문의 `Sitenum/Groupnum/Devicenum`

요청은 무인정산기의 화면·음성 처리가 끝났음을 알린다. 클라이언트가 차단기 개방 여부를 임의로 지정하지 않는다. EdgeService는 호출한 무인정산기가 해당 LPR 사건과 연결되어 있는지 검증하고, 저장해 둔 원래 LPR 사건으로 기존 출차 처리를 실행한다. 서버가 계산한 `DisplayMessage`와 `OpenBarrier` 결과로 무인정산기에 연결된 전광판을 제어한다. 같은 `EventId`의 중복 완료는 동일 결과로 처리한다.

결제 저장은 기존 결제 완료 API를 계속 사용하며 이 API에서 결제를 다시 저장하지 않는다.

## 7. 무인 연결 장애 정책

사이트·그룹별 `tparkvariable`에 다음 값을 사용한다.

- `cmd_type`: `CMD_KIOSK_OFFLINE_POLICY`
- `val=OPEN`: 무인 연결 장애 시 전광판에 장애 안내를 표시하고 차단기를 개방한다.
- `val=BLOCK`: 차단기를 열지 않고 사건을 SQLite 대기 목록에 저장하며 무인 재접속 후 전달한다.

변수가 없거나 값이 잘못된 경우 기본값은 `OPEN`이다. 정책은 LPR 사건의 `SiteId`와 `Groupnum`으로 선택한다.

`OPEN`으로 장애 출차한 사건은 무인정산기에 재전달하지 않고 장애출차 이력만 남긴다. `BLOCK` 사건은 같은 무인정산기가 다시 연결되면 발생순서대로 재전달하고 완료 응답 전까지 대기 상태를 유지한다.

## 8. 설정 동기화

중앙 설정 응답에 다음을 포함한다.

- 현장의 `parking_device_link` 목록
- 그룹별 `CMD_KIOSK_OFFLINE_POLICY`

EdgeService는 기존 `LocalConfigurationStore`의 SQLite JSON에 함께 저장한다. 중앙 연결이 끊겨도 마지막 동기화 설정으로 장비 연결과 장애 정책을 판단한다.

## 9. 출차 처리 순서

1. LPR TCP 패킷과 파일명을 검증한다.
2. LPR 카메라에 활성 무인정산기 연결이 있는지 먼저 조회한다.
3. 연결이 없으면 기존 출차 API를 실행하고 LPR 단독 전광판 흐름을 처리한다.
4. 연결된 무인정산기가 온라인이면 원래 LPR 사건을 SQLite에 대기 저장하고 SignalR로 통보한다. 이 단계에서는 기존 출차 API, 전광판, 차단기를 실행하지 않는다.
5. 무인정산기는 기존 조회·견적·결제 API를 사용하고 무료차량도 화면과 음성 안내를 완료한다.
6. 무인 완료 API가 호출되면 저장된 LPR 사건으로 기존 출차 API를 실행하고 서버 결과에 따라 무인에 연결된 전광판을 제어한다.
7. 무인이 오프라인이면 `CMD_KIOSK_OFFLINE_POLICY`를 적용한다.

`OPEN` 정책은 기존 출차 계산 결과와 관계없이 물리 차단기를 개방하며 `KIOSK_OFFLINE_OPEN` 장애출차 이력을 남긴다. 중앙에는 별도의 장애출차 사건으로 전달하여 열린 주차 세션을 출차 완료 처리한다. `BLOCK` 정책은 기존 출차 API를 호출하지 않고 원래 LPR 사건을 대기 상태로 유지한다.

## 10. 시험 범위

- 장비 연결 다대다 조회
- 카메라 DeviceId별 올바른 무인·전광판 선택
- LPR 단독 처리
- 연계 무인 온라인 SignalR 통보와 LPR 직접제어 방지
- 무료차량도 무인으로 통보
- 오프라인 `OPEN` 전광판 표시와 차단기 개방
- 오프라인 `BLOCK` SQLite 대기와 재접속 재전달
- 무인 완료 API의 연결 검증과 EventId 멱등 처리
- 하나의 전광판을 LPR과 무인이 공유하는 구성
- 시험용 SignalR 무인 클라이언트 수신·완료 왕복

실제 무인정산기의 화면, 음성, 카드단말기, 영수증 코드는 이번 범위에 포함하지 않는다.
