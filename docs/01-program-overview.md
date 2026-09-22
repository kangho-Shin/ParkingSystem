# ParkingSystem 전체 구조

## 1. 기본 구조

```text
JPXLpr / APSMain / Parking.Operator / Parking.EdgeManager
                         │
                         ▼
              Parking.EdgeService :5200
              LPR TCP :29200 / SignalR
                         │
                    SQLite Outbox
                         ▼
              Parking.EdgeGateway :5100
                         │
                         ▼
                 Parking.Api :5000
                         │
                         ▼
                       MySQL
```

현장 프로그램은 중앙 API와 MySQL에 직접 연결하지 않는다. 모든 현장 요청은 `Parking.EdgeService`를 거치며 중앙 통신은 `Parking.EdgeGateway`를 통해 `Parking.Api`로 전달한다.

## 2. 공통 설계 원칙

- 현장 식별자는 `sitenum`, `groupnum`, `devicenum`을 사용한다.
- DB 내부와 장치 연결에는 전역 고유키 `deviceid`를 사용한다.
- EdgeService가 외부 장비번호를 내부 `deviceid`로 변환한다.
- 입출차는 `EventId`, 결제는 `PaymentId`로 재전송 중복을 방지한다.
- JSON 속성명은 PascalCase를 유지한다.
- 중앙 DB 접근은 Dapper와 MySqlConnector를 사용한다.
- JSON 처리는 Newtonsoft.Json을 기본으로 사용한다.
- 운전 시간은 한국시간으로 저장하며 초 이하 값은 제거한다.
- WinForms는 반드시 `.cs`, `.Designer.cs`, `.resx` 구조를 유지한다.

## 3. 중앙 프로그램

| 프로그램 | 역할 | 현재 상태 |
|---|---|---|
| `Parking.Api` | 입차, 출차, 요금, 결제, 등록차량, 현장설정과 MySQL 저장 | 구현 |
| `Parking.EdgeGateway` | 현장 요청 중계와 중앙 연결상태 제공 | 구현 |
| `Parking.Central.Data` | Dapper 기반 MySQL 저장소 | 구현 |
| `Parking.Worker` | 중앙 예약 작업용 Windows Service 기반 | 기본 실행·상태 확인 구현 |
| `ParkImageServer` | 이미지 업로드, 존재확인, 다운로드 | 기존 프로그램 포함 |

## 4. 현장 프로그램

| 프로그램 | 역할 | 현재 상태 |
|---|---|---|
| `Parking.EdgeService` | 현장 API, SQLite Outbox, 설정 동기화, LPR TCP, SignalR, LDM 제어 | 구현 |
| `Parking.EdgeManager` | 상태·설정·입출차 목록·사진 모니터링 및 현장 설정 | 1차 구현 |
| `Parking.Operator` | 차량조회, 할인·요금, 결제기록, 차량번호 정정, 수동 입출차·개방 | 기본 업무 구현 |
| `Parking.TerminalAgent` | 콘솔·백그라운드 프로그램 자동 실행과 재시작 제한 | 구현 |
| `JPXLpr` | 최대 4개 카메라, 번호인식, 이미지 저장, EdgeService 전송 | Edge 전용 구조 적용 |
| `APSMain_C(API)V2` | 15/24인치 무인정산, 카드·프린터·음성·BF UI | EdgeService 연동 적용 |
| `ImageUploadAgent` | 로컬 이미지를 이미지 서버로 재전송 | 기존 프로그램 포함 |

## 5. 공통 라이브러리와 시험

| 프로젝트 | 역할 |
|---|---|
| `Parking.Contracts` | 중앙·현장 공통 요청과 응답 |
| `Parking.Domain` | 주차 상태와 도메인 형식 |
| `Parking.FeeEngine` | DB·화면과 분리된 요금 계산 |
| `Parking.FeeTester` | 요금 계산 확인용 WinForms |
| `Parking.Api.Tests` | API, 저장소, Edge 흐름 통합시험 |
| `Parking.Domain.Tests` | 도메인과 요금 시험 |
| `Parking.EdgeManager.Tests` | Edge 관리 기능 시험 |
| `Parking.TerminalAgent.Tests` | 자동실행·재시작 및 APSMain 경계 시험 |
| `APSMain.EdgeIntegration.Tests` | APSMain Edge 연동 시험 |
| `JPXLpr.Tests` | LPR 프레임, Outbox, Edge 전송 시험 |

초기 개발용 시뮬레이터 3개는 역할이 끝나 삭제했다.

## 6. 저장소 구분

### MySQL

중앙의 최종 원본이다. 입출차 사건, 주차 세션, 결제, 등록차량, 요금·할인, 현장·차로·장치 설정을 저장한다.

### SQLite

EdgeService가 현장 운영을 위해 사용한다.

- 중앙 전송 대기 Outbox
- 현장 설정 캐시와 변경 버전
- Kiosk 대기 사건
- EdgeManager 모니터링 자료

중앙 연결이 끊겨도 마지막 현장 설정과 Outbox를 이용해 제한 운영하고 연결 복구 후 같은 식별자로 재전송한다.

## 7. 책임 경계

- JPXLpr은 인식과 이미지 저장·전송만 담당한다.
- APSMain은 화면, 음성, 카드 승인·취소·망취소, 영수증을 담당한다.
- EdgeService는 현장 장비 연결, 실시간 전달, 전광판과 차단기 명령을 담당한다.
- Parking.Api는 최종 주차·요금·출차 판단과 중앙 저장을 담당한다.
- 별도 `Parking.Payment` 프로그램은 만들지 않는다.
