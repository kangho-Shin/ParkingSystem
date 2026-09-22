# DB와 현장 설정

## 1. 기준 DB

- 시험 및 현재 개발 DB: `parking000test`
- 연결 환경변수: `PARKING_RUNTIME_CONNECTION`
- 신규 설치 통합본: `database/mysql/000_full_schema.sql`
- 기존 DB 변경: `001_initial.sql`부터 `013_korea_time_seconds.sql`까지 순서별 적용

새 DB에 `000_full_schema.sql`을 실행한 경우 이전 순차 스크립트를 다시 실행하지 않는다. DB 변경 시 기존 DB 적용 SQL과 통합본을 함께 갱신한다.

## 2. 주요 테이블

| 테이블 | 용도 |
|---|---|
| `parking_site` | 현장 |
| `parking_lane` | 그룹별 입·출차 차로 |
| `parking_device` | LPR, KIOSK, LDM 장치 |
| `parking_device_link` | 장치 간 KIOSK·LDM 연결 |
| `parking_site_sync` | 인증키 해시와 설정 버전 |
| `parking_event` | EventId 기준 원본 입출차 사건 |
| `parking_session` | 일반차량 입차부터 출차까지 한 행 |
| `payment` | PaymentId 기준 결제 결과 |
| `tperiodmember` | 등록차량 회원 원본 |
| `tperiodinout` | 등록차량 입출차 한 행 |
| `tparkfee` | 요금 단계 |
| `tdiscount` | 할인 정의 |
| `tholiday` | 휴일 |
| `tparkvariable` | 운영변수 |
| `vehicle_eligibility` | 유공자 등 자격 조회 결과 |
| `parking_session_discount` | 주차 건별 할인 적용 이력 |

`parking_session_discount`는 운전 중 생성되는 이력이라 통합 스키마에 기본 데이터를 넣지 않는다.

## 3. 주차 상태

```text
I = 입차
X = 실제 정산완료 또는 최종금액 0원 확정
O = 출차완료
```

기본 흐름은 `I → X → O`다. 요금조회와 유료 견적만으로는 `I`를 유지한다. 출차 시 기존 입차 행을 삭제하거나 다른 테이블로 옮기지 않는다.

## 4. 시간 저장

다음 운전 시간은 한국시간 UTC+9 기준으로 초까지만 저장한다.

- `parking_event.eventat`
- `parking_session.indate`, `paydate`, `outdate`
- `payment.paydate`
- 등록차량 입출차 시간
- 할인과 자격 확인 시간

기존 DB는 `013_korea_time_seconds.sql`을 적용한다. 현재 통합 스키마의 설정 테이블 `updatedat` 일부는 아직 `DATETIME(6)`이며 운전 시간과 달리 후속 통일 대상이다.

## 5. 시험 현장 기본 구성

- `sitenum=9001`
- `groupnum=2`
- 입차 차로: `laneid=9010`
- 출차 차로: `laneid=9020`

| 장치 | deviceid | devicenum | laneid | 사용 |
|---|---:|---:|---:|---|
| 출구 KIOSK | 2001 | 201 | 9020 | 사용 |
| 입차 LPR | 4001 | 401 | 9010 | 사용 |
| 출차 LPR | 4002 | 402 | 9020 | 사용 |
| 보조 입차 LPR | 4003 | 403 | 9010 | 미사용 |
| 보조 출차 LPR | 4004 | 404 | 9020 | 미사용 |
| 출구 LDM | 5001 | 501 | 9020 | 사용 |

LDM의 IP와 포트는 공개 문서에 적지 않고 현장 로컬 설정에서 관리한다.

## 6. 장치 연결

| source | target | linktype |
|---:|---:|---|
| KIOSK 2001 | LDM 5001 | LDM |
| 출차 LPR 4002 | KIOSK 2001 | KIOSK |
| 출차 LPR 4002 | LDM 5001 | LDM |

외부 프로그램은 `sitenum + groupnum + devicenum`을 사용한다. EdgeService가 장치종류와 차로·그룹·사용 여부를 확인해 내부 `deviceid`로 변환한다.

## 7. 기본 요금과 운영변수

시험 현장 9001/2에는 평일·주말 소형차 기본 요금과 다음 할인키가 들어 있다.

| key | 종류 | 값 |
|---:|---|---:|
| 10 | 50% | 50 |
| 20 | 30분 | 30 |
| 30 | 1,000원 | 1000 |
| 40 | 최종금액 1,000원 | 1000 |

| cmd_type | 기본값 |
|---|---|
| `CMD_MAXDAILY_FEE` | 20000 |
| `CMD_GRACE_TIME` | 30분 |
| `CMD_PREPAY_GRACE` | 10분 |
| `CMD_SERVICE_TIME` | 0분 |
| `CMD_DUPLICATE_ENTRY_TIME` | 10초 |
| `CMD_KIOSK_OFFLINE_POLICY` | OPEN |

## 8. 현장 인증과 설정 동기화

`parking_site_sync`에는 현장 인증키의 SHA-256 해시만 저장한다. 실제 인증키는 실행 환경변수나 로컬 설정으로 전달하고 소스와 문서에 평문으로 기록하지 않는다.

EdgeService는 중앙 설정을 SQLite에 보관한다. 중앙 장애 시 마지막 설정으로 운영하고, 로컬 변경과 미전송 항목은 복구 후 동기화한다.

## 9. DB 보호 원칙

- 기존 테이블을 임의로 DROP하지 않는다.
- 시험 데이터를 자동 삭제하지 않는다.
- 테스트 배치가 스키마나 기본 데이터를 자동 생성하지 않게 한다.
- 로컬 설정 파일과 실행 DB를 Git에 올리지 않는다.
- 마이그레이션은 반복 실행 안전성과 기존 운전 데이터 보존을 우선한다.
