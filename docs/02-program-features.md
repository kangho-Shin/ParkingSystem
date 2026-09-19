# 프로그램별 기능 상세

## Parking.Api

중앙 업무 서버이며 모든 최종 주차 판단과 MySQL 저장을 담당한다.

- 입차 요청 검증과 주차 세션 생성
- 입차·출차 차량 이미지 경로와 방향정보 저장 예정
- 같은 `EventId` 재요청 시 최초 처리 결과 반환
- 미출차 차량 조회
- 요금 설정 로딩과 요금 계산
- 차량번호 기반 정산 견적
- 할인키, 기존 결제금액, 사전정산 유예시간 반영
- 출차 시 현재 요금을 다시 계산하여 미납·추가요금 여부 판단
- 결제 완료 저장과 같은 `PaymentId` 중복 방지
- 현장·차로·장비 설정 조회 및 저장

출차는 다음 경우에만 허용한다.

- 최종 계산요금이 0원
- 기존 결제금액으로 현재 요금이 모두 충당됨
- 사전정산 유예시간 이내라 추가요금이 없음

## Parking.EdgeGateway

현장과 중앙 업무 서버 사이의 중계 서버다. 주차·요금 판단은 하지 않는다.

- 입차 사건을 `Parking.Api`로 전달
- 출차 사건을 `Parking.Api`로 전달
- 현장 설정 조회 중계
- 요금 견적 JSON과 HTTP 상태를 원문 그대로 중계
- 결제 완료 JSON과 HTTP 상태를 원문 그대로 중계
- 중앙 API 연결 실패 또는 시간초과 시 HTTP 503 반환

## Parking.EdgeService

Windows 서비스로 실행되는 현장 중계 프로그램이다.

- 현장 프로그램이 호출하는 로컬 HTTP API 제공
- 입차·출차·결제 결과를 전송 전에 SQLite Outbox에 저장
- 중앙 연결 정상 시 즉시 Gateway로 전송하고 완료 처리
- 중앙 장애 시 입차는 `EDGE_OFFLINE_ENTRY`로 차단기 개방 허용
- 중앙 장애 시 출차는 `CENTRAL_OFFLINE_EXIT_BLOCKED`로 차단
- 중앙 장애 시 결제 결과는 HTTP 202 `PAYMENT_PENDING_SYNC`로 보관
- Outbox를 1초 간격으로 확인하고 생성순으로 최대 20건 재전송
- 실패 시 지수형 재시도, 최대 대기 60초
- 5분 간격으로 현장 설정을 중앙에서 받아 SQLite에 저장
- 사용자 실행 시 `%LOCALAPPDATA%\ParkingSystem\edge.db` 사용
- Windows 서비스 실행 시 `%PROGRAMDATA%\ParkingSystem\edge.db` 사용

## Parking.FeeEngine

DB와 화면에 의존하지 않는 공통 요금 계산 라이브러리다.

- 사이트·그룹·차종별 단계 요금
- 평일·주말 요금 구분
- 일자별 요금 분리와 일 최대요금
- 주간·야간 시간 구분
- 무료 유예시간 `CMD_GRACE_TIME`
- 사전정산 유예시간 `CMD_PREPAY_GRACE`
- 서비스 차감시간 `CMD_SERVICE_TIME`
- 시간 할인과 퍼센트 할인
- 휴일·주말 제외 설정
- 기존 결제금액을 반영한 추가 결제금액 계산

현재 `CarType` 규칙은 1=소형, 2=중형, 3=대형이며, 같은 사이트라도 `Groupnum`별로 다른 요금표를 적용할 수 있다.

## Parking.Central.Data

Dapper와 MySqlConnector를 사용하는 저장소 계층이다.

- `ParkingEventRepository`: 입차 사건과 세션을 한 트랜잭션으로 저장
- `ParkingExitRepository`: 미출차 조회, 출차 사건과 결과 저장
- `PaymentRepository`: 결제 저장, 중복·충돌 검사, 세션 Paid 처리
- `SettlementRepository`: 할인키와 누적 결제금액 조회
- `SiteConfigurationRepository`: 현장·차로·장비 설정 저장과 조회

## Parking.Simulator

현재는 가상 입차 시험을 지원한다.

```bat
dotnet run --project src\Tools\Parking.Simulator\Parking.Simulator.csproj -- entry --site 1 --lane 10 --device 101 --car 12가3456
```

옵션:

- `--url`: EdgeService 주소, 기본값 `http://localhost:5200`
- `--event`: 지정한 UUID 재사용
- `--repeat-event`: 같은 UUID를 즉시 두 번 전송하여 중복방지 시험

향후 출차·결제·LPR·전광판·차단기 모의 기능을 추가한다.

## 출구·사전무인 차량조회

출구무인과 사전무인은 차량번호 뒤 4자리로 일반차량과 등록차량의 미출차 내역을 함께 조회한다. 여러 건이면 차량번호, 입차시간, 입차이미지를 표시하고 사용자가 선택한다. 이 기능은 설계가 확정됐으며 아직 구현 전이다.

## 공통 라이브러리

- `Parking.Contracts`: 입차, 출차, 결제, 설정, 공통 응답 모델
- `Parking.Domain`: `Entered`, `Paid`, `Exited`, `Unpaid`, `ManualExit` 상태
