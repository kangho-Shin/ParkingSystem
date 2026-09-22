# 2026-09-22 다음 작업 인수인계

## 1. 저장소

- GitHub: `https://github.com/kangho-Shin/ParkingSystem`
- 작업 브랜치: `codex/server-edge-foundation`
- 기존 통합 스키마: `database/mysql/000_full_schema.sql`
- 신규 통합 스키마: `database/mysql/newfull_schema.sql`
- DB 설계 명세: `docs/superpowers/specs/2026-09-22-parking-database-redesign.md`
- 구현 계획: `docs/superpowers/plans/2026-09-22-newfull-schema-implementation.md`

## 2. 이번 작업 완료내용

기존 `PISM.xls` 명세와 현재 서버·Edge 스키마를 비교하여 `newfull_schema.sql`을 작성했다.

- 현장에서 익숙한 `tparkinfo`, `tperiodmember` 형식으로 테이블명을 정리했다.
- 기존 `000_full_schema.sql`은 수정하지 않았다.
- 신규 스키마에는 중앙 MySQL 테이블 36개가 있다.
- MySQL 5.7 금지문법, 테이블 생성순서, 외래키 참조순서, 초기자료 재실행 구조를 정적으로 검증했다.
- 현재 작업환경에 MySQL이 없어 실제 MySQL 5.7 실행시험은 아직 하지 못했다.
- Edge의 SQLite Outbox는 변경하지 않고 그대로 사용한다.

GitHub 원격 브랜치에서 다음 파일이 올라간 것을 확인했다.

- `database/mysql/newfull_schema.sql`
- `docs/superpowers/specs/2026-09-22-parking-database-redesign.md`
- `docs/superpowers/plans/2026-09-22-newfull-schema-implementation.md`

## 3. 확정된 주요 DB 규칙

### 일반차량

- `parking_session`을 `tparkinfo`로 변경한다.
- `tparkinfo` 한 행에서 입차 `I`, 정산완료 `X`, 출차완료 `O`까지 관리한다.
- 카드 상세정보는 `tbcardinfo`, 할인 상세정보는 `tdiscountinfo`에 저장한다.
- 기존 `I` 상태의 같은 차량이 다시 입차하면 기존 할인자료와 입차자료를 정리한 후 새 입차를 생성한다.
- 기존 `X` 상태이면 기존 행을 `O`로 마감한 후 새 입차를 생성한다.

### 등록차량

- `tperiodmember.carnum1`만 실제 등록차량 확인에 사용한다.
- `carnum2`는 관리용이며 사용할 때 다시 `carnum1`으로 등록한다.
- 입·출차 이력은 `tperiodinout` 한 행으로 관리하며 상태는 `I`, `O`만 사용한다.
- 중복 입차 시 기존 `I` 이력을 자동 출차 처리하고 새 입차를 생성한다.

### 결제와 할인

- `payment`를 `tbcardinfo`로 변경한다.
- 카드 승인 성공금액은 양수, 승인취소·망취소 성공금액은 음수로 행을 추가한다.
- 결제 행은 수정하거나 삭제하지 않는다.
- 원격할인 취소는 `tdiscountinfo` 해당 행을 실제 삭제한다.
- 할인 삭제 후 `tparkinfo.discountfee`, `discounttime`, `payfee`를 다시 계산한다.
- `vehicle_eligibility`는 제거하고 `twelfare`로 통합한다.

### 주차현황

- `tparkingnum`은 일반·등록차량 입차와 출차 누계만 저장한다.
- 현재 주차대수와 여유대수는 API에서 입차 누계와 출차 누계로 계산한다.
- 누계는 자정에 초기화하지 않으며 현장에서 수동으로 보정할 수 있다.

### 제외 테이블

- `tping`: 현재 Edge/API 방식에서 사용하지 않는다.
- `tdevicestate`: 현재 상태는 Edge 실시간 상태 API로 확인한다.
- `vehicle_eligibility`: `twelfare`로 통합한다.

## 4. 다음 작업 순서

반드시 아래 순서로 한 단계씩 진행한다. 앞 단계의 빌드와 테스트가 끝나기 전에 다음 프로젝트를 수정하지 않는다.

### 1단계: Parking.Api 수정

가장 먼저 중앙 API만 `newfull_schema.sql`에 맞춘다.

주요 작업:

1. `Parking.Api`, `Parking.Central.Data`, 관련 Contracts와 API 테스트에서 기존 테이블·컬럼 사용처를 전부 검색한다.
2. Dapper SQL과 모델을 신규 테이블명과 컬럼명으로 변경한다.
3. `parking_session` → `tparkinfo`로 변경한다.
4. `payment` → `tbcardinfo`로 변경한다.
5. `parking_session_discount` → `tdiscountinfo`로 변경한다.
6. `vehicle_eligibility` → `twelfare`로 변경한다.
7. `parking_site`, `parking_lane`, `parking_device`, `parking_device_link`, `parking_site_sync`, `parking_event`를 각각 신규 `t...` 테이블로 변경한다.
8. EventId 저장형식을 기존 `BINARY(16)`에서 신규 `CHAR(32)` 형식에 맞춘다.
9. 일반차량·등록차량 입차, 요금계산, 할인, 결제, 출차 흐름을 수정한다.
10. `tparkingnum` 입·출차 누계를 같은 트랜잭션에서 갱신한다.
11. API 관련 테스트를 수정한다.
12. `Parking.Api.Tests`를 한 번에 실행하고 전체 솔루션을 빌드한다.

1단계 완료조건:

- 새 시험 DB에 `newfull_schema.sql`을 실행한다.
- API 관련 테스트가 모두 성공한다.
- `dotnet build ParkingSystem.sln`이 성공한다.
- 이 단계에서는 `Parking.EdgeGateway`, `Parking.EdgeService` 소스를 수정하지 않는다.

### 2단계: Parking.EdgeGateway 수정

API 수정과 테스트가 끝난 다음 진행한다.

주요 작업:

1. 사이트 인증을 `tparkings.sitekeyhash` 기준으로 변경한다.
2. 설정 조회를 `tparkings`, `tlaneinfo`, `tdeviceinfo`, `tdevicelink` 기준으로 변경한다.
3. 설정 버전과 적용상태를 `tparksync` 기준으로 변경한다.
4. Edge 이벤트 수신과 중복처리를 `tparkevent.eventid` 기준으로 변경한다.
5. 이벤트 처리결과가 `tparkinfo` 또는 `tperiodinout`과 연결되는지 확인한다.
6. Gateway 관련 테스트와 전체 솔루션 빌드를 실행한다.

2단계 완료조건:

- `/api/v1/edge/events` 입·출차 이벤트가 정상 처리된다.
- `/api/v1/edge/config/sites/{site}`에서 신규 장비·차로 설정이 내려온다.
- 동일 EventId 재전송 시 입·출차가 중복 생성되지 않는다.
- 이 단계가 끝난 뒤에만 EdgeService를 수정한다.

### 3단계: Parking.EdgeService 수정

Gateway 수정이 끝난 다음 진행한다.

주요 작업:

1. 신규 Gateway 설정 응답에 맞춰 현장 설정모델을 변경한다.
2. 장비·차로·연결관계를 `tdeviceinfo`, `tlaneinfo`, `tdevicelink` 구조에 맞춘다.
3. LPR 입·출차 EventId는 하이픈 없는 32자리 문자열로 전송한다.
4. 중앙 응답코드와 `tparkevent` 처리결과를 현장 ACK/NACK 흐름에 반영한다.
5. 중앙 미연결 시 기존 SQLite Outbox에 저장하고 재연결 후 같은 EventId로 재전송한다.
6. 무인정산기·전광판·차단기 연결제어가 기존처럼 동작하는지 확인한다.
7. EdgeService 관련 테스트와 전체 솔루션 빌드를 실행한다.

3단계 완료조건:

- LPR TCP 수신 후 중앙 입·출차 처리가 정상이다.
- 중앙 미연결 이벤트가 Outbox에 저장되고 재연결 후 한 번만 처리된다.
- 무인정산기 연결과 LPR 단독 제어가 `tdevicelink` 설정대로 동작한다.
- `dotnet test`와 `dotnet build ParkingSystem.sln`이 성공한다.

## 5. 개발 기준

- Dapper와 Newtonsoft.Json을 계속 사용한다.
- `Program.cs`는 기존 `Program`/`Startup` 형식을 유지한다.
- DB 컬럼과 C# 변수명은 짧고 일관되게 유지한다.
- WinForms 수정이 생기면 반드시 `.cs`, `.Designer.cs`, `.resx`로 분리한다.
- 테스트는 작은 명령을 반복해서 사용자에게 요구하지 말고 단계가 끝날 때 한 번에 실행한다.
- unrelated 파일과 기존 사용자 변경사항은 수정하지 않는다.

## 6. 다음 채팅 시작 지시문

다음 문장으로 시작하면 된다.

> GitHub 저장소 `https://github.com/kangho-Shin/ParkingSystem`, 브랜치 `codex/server-edge-foundation`에서 계속 진행해줘. 먼저 `docs/2026-09-22-next-session-handoff.md`와 `database/mysql/newfull_schema.sql`을 읽고, 인수인계 문서의 1단계인 Parking.Api 수정만 진행해줘. EdgeGateway와 EdgeService는 아직 수정하지 마.
