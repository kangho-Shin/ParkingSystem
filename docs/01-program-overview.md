# 프로그램 구성과 역할

## 1. 현재 구조

```text
Parking.Simulator / 향후 Kiosk·Operator·LprHost
                    │
                    ▼
          Parking.EdgeService :5200
                    │ SQLite Outbox
                    ▼
          Parking.EdgeGateway :5100
                    │
                    ▼
             Parking.Api :5000
                    │
                    ▼
                 MySQL
```

- 현장 프로그램은 중앙 API에 직접 연결하지 않고 `Parking.EdgeService`를 사용한다.
- `Parking.EdgeService`는 입차·출차·결제 사건을 SQLite Outbox에 먼저 저장한다.
- 중앙 연결은 `Parking.EdgeGateway`를 거쳐 `Parking.Api`로 전달한다.
- `EventId`와 `PaymentId`를 재전송 식별자로 사용하여 중복 저장을 막는다.
- JSON 속성명은 PascalCase를 유지한다.
- DB 처리는 Dapper, JSON 처리는 Newtonsoft.Json을 기본으로 사용한다.

## 2. 현재 구현된 프로그램

| 프로그램 | 실행 위치 | 역할 | 상태 |
|---|---|---|---|
| `Parking.Api` | 중앙 Linux/Windows 시험환경 | 입차·출차·요금·결제·현장설정 업무 처리, MySQL 저장 | 구현 완료 |
| `Parking.EdgeGateway` | 중앙 | 현장 요청을 Parking.Api로 전달 | 구현 완료 |
| `Parking.EdgeService` | 현장 Windows | 현장 API, SQLite Outbox, 장애 제한운영, 설정 동기화 | 구현 완료 |
| `Parking.Simulator` | 개발 PC | 가상 입차 및 중복·통신장애 시험 | 입차 기능 완료 |
| `Parking.FeeEngine` | 공통 라이브러리 | 요금표·할인·유예시간 계산 | 1차 구현 완료 |
| `Parking.Central.Data` | 중앙 공통 라이브러리 | MySQL 저장소 구현 | 구현 완료 |
| `Parking.Contracts` | 중앙·현장 공통 | API 요청·응답 계약 | 구현 완료 |
| `Parking.Domain` | 공통 | 주차 상태 등 도메인 형식 | 기본 구현 완료 |
| `Parking.Api.Tests` | 개발·CI | API·DB·중계·Outbox 통합시험 | 구현 및 전체 통과 |
| `Parking.Domain.Tests` | 개발·CI | 공통 계약 시험 | 구현 및 전체 통과 |

## 3. 설계만 완료되고 아직 만들지 않은 프로그램

| 프로그램 | 예정 역할 | 상태 |
|---|---|---|
| `Parking.EdgeManager` | 현장 설정, 서비스 점검, 로그 확인, 연결시험 | 미구현 |
| `Parking.LprHost` | 카메라별 독립 프로세스, UDP 영상·TCP 제어·LPR 결과 전달 | 미구현 |
| `Parking.Kiosk` | 사전·출구 무인정산, 15/24인치, BF UI | 기존 프로그램 개편 전 |
| `Parking.Operator` | 유인정산, 차량정정, 수동 입출차, 원격 개방 | 미구현 |
| `Parking.TerminalAgent` | 자동실행, 장애복구, 업데이트, 로그 전송 | 미구현 |
| `Parking.Payment` | PG 승인·취소·망취소·매출대사 | 미구현 |
| `Parking.Worker` | 실패 재처리, 집계, 알림, 정리 작업 | 미구현 |
| `Parking.AdminWeb` | 본사·운영회사·현장 통합관리 | 미구현 |
| `Parking.CustomerWeb` | 차량조회, 모바일 결제, 정기권, 영수증 | 미구현 |
| `Parking.FeeTester` | 실제 요금표 자동검증 | 미구현 |

제조사별 범용 플러그인, `Parking.DeviceSdk`, `DriverHost`, 중앙 영상 재인식은 자체 시스템 완성 후 2차로 보류한다.

## 4. 데이터 저장소

### MySQL

- `parking_event`: 입·출차 원본 사건과 처리 결과
- `parking_session`: 입차부터 출차까지의 주차 건
- `parking_site`, `parking_lane`, `parking_device`: 현장·차로·장비 설정
- `tparkfee`, `tdiscount`, `tholiday`, `tparkvariable`: 요금 계산 설정
- `payment`: 결제 결과
- `vehicle_eligibility`: 할인 자격 조회 결과용 스키마
- `parking_session_discount`: 주차 건별 할인 적용 내역

### SQLite

- `outbox_message`: 입차·출차·결제 중앙 전송 대기열
- 현장 설정 캐시: `LocalConfigurationStore`가 현장·차로·장비 설정 저장

