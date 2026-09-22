# 2026-09-22 작업 인수인계

## Git 기준

- 저장소: `https://github.com/kangho-Shin/ParkingSystem`
- 작업 브랜치: `codex/server-edge-foundation`
- 앞으로 이 브랜치에 직접 반영한다.
- 사용자는 다음 명령으로 변경 내용을 받는다.

```bat
git pull --ff-only origin codex/server-edge-foundation
```

- 번들 방식은 사용하지 않는다.
- `appsettings.json`, `App.config`, 실행 DB, 빌드 결과는 Git에 올리지 않는다.
- 사용자 로컬 설정 파일을 삭제하거나 덮어쓰지 않는다.
- 변경 후에는 환경에 .NET 컴파일 도구가 없다는 설명을 반복하지 않는다.
- 사용자에게는 간단히 `테스트 후 결과를 알려주세요.`라고 안내한다.

## 확정 환경

- DB: `parking000test` 하나만 사용
- 연결 환경변수: `PARKING_RUNTIME_CONNECTION`
- 시험 현장: `sitenum=9001`, `groupnum=2`
- KIOSK: `deviceid=2001`, `devicenum=201`, `laneid=9020`
- 입차 LPR: `deviceid=4001`, `devicenum=401`, `laneid=9010`
- 출차 LPR: `deviceid=4002`, `devicenum=402`, `laneid=9020`
- 보조 입차 LPR: `deviceid=4003`, `devicenum=403`, 미사용
- 보조 출차 LPR: `deviceid=4004`, `devicenum=404`, 미사용
- LDM: `deviceid=5001`, `devicenum=501`, `laneid=9020`, `192.168.0.151:35000`
- 사용하지 않는 장치 9402~9404는 통합 스키마에 넣지 않는다.

## 오늘 완료한 내용

### APSMain과 전광판

- 요금 조회 시 전광판에 `요금 1,000원` 형식으로 고정 표시한다.
- 요금 표시 시간은 100초이며 이 시간 동안 시계 표시를 중지한다.
- 결제 완료 후 연결된 출차 LPR 4002를 통해 실제 출차 완료 API를 호출한다.
- 출차 성공 시에만 `정산 완료되었습니다.`를 표시한다.
- 출차가 완료되면 `parking_session.outflag`가 `O`가 된다.
- 이전, 홈, 자동 홈으로 정산 화면을 나가면 요금 표시를 즉시 지우고 현재 시간을 표시한다.
- 평상시 전광판 두 번째 줄에 현재 시간을 1분마다 전송한다.

### 시간 저장

- `parking_session.indate`, `paydate`, `outdate`, 결제 및 이벤트 시간을 한국시간 `UTC+9`로 저장한다.
- 초 이하 값은 제거한다.
- 기존 DB용 `database/mysql/013_korea_time_seconds.sql`을 추가했다.
- 신규 통합 스키마의 운전 시간 컬럼은 `DATETIME`으로 변경했다.

### 통합 DB 스키마

- `database/mysql/000_full_schema.sql`은 새 시험 DB를 한 번에 구성하는 최종 통합본이다.
- 다음 기본값을 포함한다.
  - 현장 9001, 그룹 2
  - 입·출차 차로 9010, 9020
  - 장치 2001, 4001~4004, 5001
  - KIOSK/LPR/LDM 장치 연결
  - 시험 현장 인증키 `site-9001-key`의 SHA-256 값
  - 9001/2 기본 요금과 운영변수
  - `tdiscount` 시험 할인: 50%, 30분, 1,000원, 최종금액 1,000원
- `parking_session_discount`는 차량별 할인 이력이므로 기본 INSERT하지 않는다.
- DB 변경 시 기존 DB 적용 SQL과 `000_full_schema.sql`을 반드시 함께 갱신한다.

## 다음 작업 순서

1. 최신 브랜치를 내려받는다.
2. 비운 `parking000test`에 `000_full_schema.sql`을 실행한다.

```bat
mysql -u test -p parking000test < database\mysql\000_full_schema.sql
```

3. `/api/v1/local/config`에서 9001/2 차로, 장치, 연결 정보를 확인한다.
4. 전체 빌드와 전체 테스트를 한 번 실행한다.
5. APSMain 실제 흐름을 확인한다.
   - 차량 조회 후 `요금 1,000원`이 흐르지 않고 고정되는지
   - 이전 또는 홈을 누르면 즉시 현재 시간이 나오는지
   - 결제 완료 후 완료 문구가 나오는지
   - 해당 차량의 `outflag`가 `O`인지
6. DB의 `indate`, `paydate`, `outdate`가 한국시간이며 소수점 없이 저장되는지 확인한다.
7. 필요하면 설정 테이블의 남아 있는 `updatedat DATETIME(6)`도 `DATETIME`으로 통일한다.

## 작업 원칙

- 기존 테이블을 임의로 DROP하거나 시험 데이터를 자동 삭제하지 않는다.
- 테스트 배치가 기존 DB에 스키마 생성이나 기본 데이터 INSERT를 하지 않게 유지한다.
- WinForms는 반드시 `.cs`, `.Designer.cs`, `.resx` 구조로 작성한다.
- 사용자가 수정한 로컬 설정 파일은 보존한다.
- 한 단계씩 간결하게 진행한다.
