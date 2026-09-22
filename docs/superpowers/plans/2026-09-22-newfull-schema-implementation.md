# New Full Schema Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기존 `000_full_schema.sql`을 그대로 보존하면서 확정된 통합 DB 설계를 `database/mysql/newfull_schema.sql`로 작성하고 MySQL 5.7에서 신규 설치 가능함을 검증한다.

**Architecture:** 중앙 MySQL에는 현장 기준정보, 요금·할인, 일반·등록차량 운행이력, 결제와 원격제어 이력을 둔다. Edge 오프라인 큐는 기존 SQLite Outbox를 유지하며 중앙은 `tparkevent.eventid`로 멱등성을 보장한다. 새 스키마는 마스터부터 이력 테이블까지 의존 순서로 생성하고 시험현장 초기자료를 마지막에 넣는다.

**Tech Stack:** MySQL 5.7, InnoDB, utf8mb4, SQL, .NET 9 저장소 구조

**Spec:** `docs/superpowers/specs/2026-09-22-parking-database-redesign.md`

## Global Constraints

- 기존 `database/mysql/000_full_schema.sql`은 수정하지 않는다.
- 신규 파일명은 정확히 `database/mysql/newfull_schema.sql`이다.
- MySQL 5.7에서 지원되는 문법만 사용한다.
- 테이블과 컬럼은 소문자이며 불필요한 밑줄을 사용하지 않는다.
- GUID는 하이픈 없는 32자리 `CHAR(32)`로 저장한다.
- 날짜와 시각은 `DATE`, `TIME`, `DATETIME`을 사용한다.
- 모든 테이블은 InnoDB와 utf8mb4를 사용한다.
- 비밀번호와 API 인증키는 평문으로 저장하지 않는다.
- `tping`, `tdevicestate`, `vehicle_eligibility`는 생성하지 않는다.

## Review Focus

- 동일한 `eventid`를 다시 넣을 때 두 번째 입·출차가 생성되지 않도록 유일키가 있어야 한다.
- 할인취소로 `tdiscountinfo` 행이 삭제되어도 카드결제 이력과 주차이력은 영향을 받지 않아야 한다.
- 승인취소 음수금액을 저장할 수 있도록 `tbcardinfo.money`가 부호 있는 자료형이어야 한다.
- `tparkingnum` 누계는 자정 초기화 컬럼이나 현재대수 저장컬럼 없이 수동 보정할 수 있어야 한다.
- 초기자료를 두 번 실행해도 중복키 오류가 발생하지 않아야 한다.

---

### Task 1: 통합 신규 스키마 작성

**Files:**
- Read: `database/mysql/000_full_schema.sql`
- Read: `docs/superpowers/specs/2026-09-22-parking-database-redesign.md`
- Create: `database/mysql/newfull_schema.sql`

**Interfaces:**
- Consumes: 확정된 36개 중앙 MySQL 테이블 명세와 현재 시험현장 9001 초기자료
- Produces: 빈 MySQL 5.7 DB에서 단독 실행 가능한 `newfull_schema.sql`

- [ ] **Step 1: 기존 파일 해시를 기록한다**

Run:

```bash
sha256sum database/mysql/000_full_schema.sql
```

Expected: 64자리 SHA-256 값과 파일경로가 출력된다. Task 3에서 같은 값인지 비교한다.

- [ ] **Step 2: 생성 순서를 고정한다**

`newfull_schema.sql`의 CREATE 순서를 다음과 같이 작성한다.

```text
tparkings
tlaneinfo
tdeviceinfo
tdevicelink
tparksync
tparkfee
tdiscount
tholiday
tparkvariable
tperiodparktime
tcompany
tdepartment
tmanager
tdisaccount
tdisdept
talarmcode
tticketnum
tparkingnum
tperiodmember
tbangmun
twelfare
tparkevent
tparkinfo
tperiodinout
tbcardinfo
tdiscountinfo
tperiodaccount
taccountinfo
tperiodwaiting
tnorecognition
tblacklist
tdevicealarm
tvaninfo
tapsinfo
tgateinfo
tlogon
```

Expected: 참조 대상 테이블이 참조하는 테이블보다 먼저 생성된다.

- [ ] **Step 3: 36개 CREATE TABLE 문을 작성한다**

각 테이블에 다음 공통 옵션을 명시한다.

```sql
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

명세의 유일키와 조회 인덱스를 포함하고 MySQL 5.7에서 실질적으로 강제되지 않는 `CHECK` 제약은 작성하지 않는다.

Expected: 다음 명령의 출력이 각각 36과 36이다.

```bash
rg -c '^CREATE TABLE IF NOT EXISTS ' database/mysql/newfull_schema.sql
rg -c 'ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;' database/mysql/newfull_schema.sql
```

- [ ] **Step 4: 필수 제약과 부호 자료형을 확인한다**

Run:

```bash
rg -n 'UNIQUE KEY.*eventid|money INT NOT NULL|discountid|commandid|inilbancnt|outilbancnt|inregcnt|outregcnt' database/mysql/newfull_schema.sql
```

Expected: EventId·할인·차단기 명령 중복방지 키, 부호 있는 카드금액, 네 개의 주차누계 컬럼이 모두 출력된다.

- [ ] **Step 5: 시험현장 초기자료를 추가한다**

다음 자료를 `INSERT ... ON DUPLICATE KEY UPDATE`로 작성한다.

```text
사이트 9001 / 그룹 2
입차차로 9010 / 출차차로 9020
입차 LPR / 출차 LPR / 출구 무인정산기 / 출구 전광판
LPR→무인정산기, LPR→전광판 연결
기본 요금표, 할인표, 운영변수
```

Expected: 초기자료 INSERT에 일반 `INSERT`만 있고 중복처리 절이 없는 문장이 존재하지 않는다.

- [ ] **Step 6: 작업내용을 커밋한다**

```bash
git add database/mysql/newfull_schema.sql
git commit -m "feat: add consolidated parking database schema"
```

### Task 2: MySQL 5.7 신규 설치 검증

**Files:**
- Test: `database/mysql/newfull_schema.sql`

**Interfaces:**
- Consumes: Task 1의 단독 실행 SQL
- Produces: 문법, 생성 개수, 멱등성, 핵심 제약 검증 결과

- [ ] **Step 1: 빈 시험 DB를 준비한다**

로컬 MySQL 5.7이 있으면 다음을 실행한다.

```bash
mysql -u root -p -e "DROP DATABASE IF EXISTS parking_schema_test; CREATE DATABASE parking_schema_test CHARACTER SET utf8mb4;"
```

Expected: 종료코드 0.

- [ ] **Step 2: 스키마를 처음 실행한다**

```bash
mysql -u root -p parking_schema_test < database/mysql/newfull_schema.sql
```

Expected: 오류 없이 종료코드 0.

- [ ] **Step 3: 테이블 수를 확인한다**

```bash
mysql -u root -p -N -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='parking_schema_test';"
```

Expected: `36`.

- [ ] **Step 4: 같은 스키마를 다시 실행한다**

```bash
mysql -u root -p parking_schema_test < database/mysql/newfull_schema.sql
```

Expected: 초기자료 중복키 오류 없이 종료코드 0.

- [ ] **Step 5: 핵심 구조를 조회한다**

```bash
mysql -u root -p -e "USE parking_schema_test; SHOW CREATE TABLE tparkevent; SHOW CREATE TABLE tbcardinfo; SHOW CREATE TABLE tdiscountinfo; SHOW CREATE TABLE tparkingnum;"
```

Expected:

```text
tparkevent.eventid UNIQUE
tbcardinfo.money INT
tdiscountinfo의 pindex+source+sourceref UNIQUE
tparkingnum에 입차·출차 누계 4개와 수용대수 2개 존재
```

- [ ] **Step 6: 검증용 DB를 삭제한다**

```bash
mysql -u root -p -e "DROP DATABASE parking_schema_test;"
```

Expected: 종료코드 0. 실제 운영 DB는 변경되지 않는다.

### Task 3: 명세 대조와 기존 파일 보존 확인

**Files:**
- Read: `docs/superpowers/specs/2026-09-22-parking-database-redesign.md`
- Read: `database/mysql/000_full_schema.sql`
- Modify: `database/mysql/newfull_schema.sql`

**Interfaces:**
- Consumes: Task 1 스키마와 Task 2 검증 결과
- Produces: 명세 누락이 없고 기존 파일이 보존된 최종 SQL

- [ ] **Step 1: 제외 테이블이 생성되지 않았는지 확인한다**

```bash
if rg -n '^CREATE TABLE.*(tping|tdevicestate|vehicle_eligibility)' database/mysql/newfull_schema.sql; then exit 1; fi
```

Expected: 출력 없이 종료코드 0.

- [ ] **Step 2: 필수 테이블 이름을 대조한다**

```bash
for t in tparkings tlaneinfo tdeviceinfo tdevicelink tparksync tparkevent tparkinfo tbcardinfo tdiscount tdiscountinfo tperiodmember tperiodinout tperiodaccount tperiodparktime tperiodwaiting tparkfee tholiday tparkvariable tdisaccount tdisdept taccountinfo tnorecognition tblacklist tbangmun twelfare tcompany tdepartment tparkingnum tticketnum tmanager tlogon talarmcode tdevicealarm tvaninfo tapsinfo tgateinfo; do rg -q "CREATE TABLE IF NOT EXISTS $t" database/mysql/newfull_schema.sql || exit 1; done
```

Expected: 출력 없이 종료코드 0.

- [ ] **Step 3: 기존 스키마 해시를 다시 확인한다**

```bash
sha256sum database/mysql/000_full_schema.sql
git diff --exit-code -- database/mysql/000_full_schema.sql
```

Expected: Task 1에서 기록한 해시와 같고 `git diff` 종료코드가 0이다.

- [ ] **Step 4: SQL 변경사항을 검토한다**

```bash
git diff --check
git status --short
```

Expected: 공백 오류가 없고 명세서·계획서·`newfull_schema.sql`만 변경목록에 나타난다.

- [ ] **Step 5: 최종 검증을 커밋한다**

```bash
git add docs/superpowers/specs/2026-09-22-parking-database-redesign.md docs/superpowers/plans/2026-09-22-newfull-schema-implementation.md database/mysql/newfull_schema.sql
git commit -m "docs: finalize parking database schema specification"
```
