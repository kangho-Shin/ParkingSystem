# ParkingSystem 통합 데이터베이스 설계

작성일: 2026-09-22  
대상 브랜치: `codex/server-edge-foundation`  
대상 DB: MySQL 5.7  
신규 스키마 파일: `database/mysql/newfull_schema.sql`

## 1. 목적

현재 `000_full_schema.sql`의 EventId, 중앙·Edge 구성, 장비 연결, 설정 동기화 기능과 기존 `PISM.xls`의 현장 운영 기능을 하나로 합친다.

- 기존 `000_full_schema.sql`은 수정하지 않는다.
- 신규 설치 검토용 스키마는 `newfull_schema.sql`로 작성한다.
- 현장에서 익숙한 `tparkinfo`, `tperiodmember` 같은 짧은 테이블명을 사용한다.
- `sitenum`, `groupnum`, `devicenum`을 공통 현장 식별자로 사용한다.
- 일반차량과 등록차량은 각각 한 행에서 입차부터 출차까지 확인할 수 있게 한다.
- 상세 결제와 할인정보는 별도 테이블로 분리한다.
- EventId를 이용하여 Edge 재전송이 중복 입·출차로 처리되지 않게 한다.

## 2. 공통 설계 기준

### 2.1 이름과 자료형

- 테이블명은 소문자 `t` 접두어를 사용한다.
- 컬럼명은 소문자이며 불필요한 밑줄을 사용하지 않는다.
- 업무 기본키는 `xindex BIGINT AUTO_INCREMENT`를 기본으로 한다.
- 장비·차로처럼 외부에서 지정하는 키는 `deviceid`, `laneid`를 사용한다.
- GUID는 사람이 DB에서 확인하기 쉬운 32자리 형식의 `CHAR(32)`로 저장한다.
- 금액은 원 단위 정수로 저장한다.
- 날짜와 시각은 MySQL 5.7 호환을 위해 `DATE`, `TIME`, `DATETIME`을 사용한다.
- 문자열은 `utf8mb4`로 저장한다.
- 비밀번호와 API 인증키는 평문으로 저장하지 않는다.

### 2.2 공통 상태값

- 일반차량 `outflag`: `I` 입차, `X` 정산 완료, `O` 출차 완료
- 등록차량 `outflag`: `I` 입차, `O` 출차 완료
- 사용 여부 `useflag`: `1` 사용, `0` 사용 중지
- 수동 처리 `manual`: `1` 수동·자동보정, `0` 정상 자동처리

### 2.3 삭제 원칙

- 마스터자료는 삭제보다 `useflag=0`을 사용한다.
- 결제와 입·출차 이력은 삭제하지 않는다.
- 원격할인 취소는 `tdiscountinfo`의 해당 적용 행을 실제 삭제한다.
- 미인식 차량은 수동 입차 성공과 동시에 `tnorecognition`에서 삭제한다.

### 2.4 정합성 원칙

- 입·출차 이벤트는 `eventid`로 중복 처리 여부를 판단한다.
- 카운트와 업무자료 변경은 같은 DB 트랜잭션에서 처리한다.
- MySQL 5.7에서 `CHECK` 제약은 신뢰하지 않고 API와 서비스에서 상태값을 검증한다.
- 외래키는 변경이 거의 없는 핵심 관계에만 사용하고, 장비 재구성이나 과거자료 조회를 막는 연쇄삭제는 사용하지 않는다.

## 3. 현재 스키마 대응

| 현재 테이블 | 신규 테이블 | 처리 |
|---|---|---|
| `parking_site` | `tparkings` | 이름 변경·기능 확장 |
| `parking_lane` | `tlaneinfo` | 이름 변경 |
| `parking_device` | `tdeviceinfo` | 이름 변경·기능 확장 |
| `parking_device_link` | `tdevicelink` | 이름 변경 |
| `parking_site_sync` | `tparksync` | 이름 변경·Edge별 상태 추가 |
| `parking_event` | `tparkevent` | 이름 변경·원본과 결과 확장 |
| `parking_session` | `tparkinfo` | 기존 일반차량 기능과 통합 |
| `payment` | `tbcardinfo` | 카드 승인·취소 이력으로 확장 |
| `parking_session_discount` | `tdiscountinfo` | 이름 변경·원격할인 기능 통합 |
| `vehicle_eligibility` | `twelfare` | 별도 테이블 제거 후 통합 |
| `tperiodmember` | `tperiodmember` | 기존 현장 필드 보완 |
| `tperiodinout` | `tperiodinout` | 입차·출차 통합 유지 |

## 4. 기존 명세 통합·제외

### 4.1 통합

- `tparkin`, 일반권 정산, 일반권 출차정보 → `tparkinfo`
- `tperiodin`, 등록차량 출차정보 → `tperiodinout`
- 카드 승인과 승인취소 → `tbcardinfo`의 개별 거래 행
- OCS·원격·복지 할인 적용 → `tdiscountinfo`

### 4.2 제외

- `tping`: 현재 Edge/API 방식에서 사용하지 않는다.
- `tdevicestate`: 현재 장비 상태는 Edge 실시간 상태 API로 확인한다.
- `vehicle_eligibility`: `twelfare`로 통합한다.
- 별도 일반차량 정산 테이블: `tparkinfo`와 `tbcardinfo`로 처리한다.

### 4.3 Edge 로컬 DB

Edge의 오프라인 전송 큐는 중앙 MySQL 테이블에 포함하지 않는다. 기존처럼 Edge 로컬 SQLite Outbox를 사용하며, 중앙은 `tparkevent.eventid`로 재전송 중복을 막는다.

## 5. 테이블 명세

### 5.1 `tparkings`

사이트·그룹별 주차장 기본정보다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `parkname`, `parktype`, `addr`, `tel`, `boss`, `sitekeyhash`, `useflag`, `regdate`, `moddate`

- 유일키: `sitenum + groupnum`
- 기존 원격접속 `ip`, `port`, `id`, `password`는 제거한다.
- Edge 인증키는 `sitekeyhash`에 해시로 저장한다.

### 5.2 `tlaneinfo`

입구·출구 차로 기준정보다.

주요 컬럼: `laneid`, `sitenum`, `groupnum`, `lanename`, `direction`, `useflag`, `regdate`, `moddate`, `note`

- `direction`: `ENTRY`, `EXIT`, `BOTH`
- `laneid`는 전체 시스템에서 중복되지 않게 관리한다.

### 5.3 `tdeviceinfo`

현장 장비 기준정보다.

주요 컬럼: `deviceid`, `sitenum`, `groupnum`, `laneid`, `devicenum`, `devicename`, `devicetype`, `ip`, `port`, `protocol`, `termid`, `posid`, `useflag`, `regdate`, `moddate`, `note`

- 유일키: `sitenum + groupnum + devicenum`
- 장비종류: `0` 기타, `1` 유인정산기, `2` 무인정산기, `3` LPR, `4` 티켓발행기, `5` PDA, `6` 전광판, `7` 차단기, `8` Edge
- IP만으로 중복을 제한하지 않는다.

### 5.4 `tdevicelink`

장비 간 명령과 이벤트 연결관계다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `sourcedeviceid`, `targetdeviceid`, `linktype`, `useflag`, `regdate`, `moddate`

- 유일키: `sourcedeviceid + targetdeviceid + linktype`
- `linktype`: `KIOSK`, `LDM`, `GATE`, `VOICE`
- 연결 방향은 `source → target`이다.

### 5.5 `tparksync`

중앙과 Edge의 설정 동기화 상태다.

주요 컬럼: `xindex`, `sitenum`, `deviceid`, `configversion`, `appliedversion`, `lastrequestdate`, `lastsyncdate`, `status`, `msg`, `moddate`

- 유일키: `sitenum + deviceid`
- `configversion`과 `appliedversion`이 같으면 동기화 완료다.

### 5.6 `tparkevent`

Edge가 보낸 입·출차 원본 이벤트와 처리 결과다.

주요 컬럼: `xindex`, `eventid`, `sitenum`, `groupnum`, `laneid`, `deviceid`, `devicenum`, `eventtype`, `carnum`, `eventdate`, `image`, `backimage`, `data`, `status`, `parktype`, `pindex`, `resultcode`, `msg`, `receivedate`, `completedate`

- `eventid`는 전체 시스템 유일키다.
- 같은 EventId가 재수신되면 기존 처리 결과를 반환한다.
- `eventtype`: `ENTRY`, `EXIT`
- `status`: `RECEIVED`, `COMPLETE`, `ERROR`

### 5.7 `tparkinfo`

일반차량의 입차·정산·출차 요약을 한 행에 저장한다.

주요 컬럼:

- 기본: `xindex`, `sitenum`, `groupnum`
- 이벤트: `ineventid`, `outeventid`
- 티켓: `ticketnum`, `ticketdata`
- 차량: `carnum`, `cartype`, `parktype`, `visitindex`
- 입차: `inlaneid`, `indevicenum`, `indate`, `inimage`, `inbackimage`
- 정산: `paydate`, `parktime`, `parkfee`, `discountfee`, `discounttime`, `payfee`, `paidfee`, `paytype`, `prepay`
- 출차: `outlaneid`, `outdevicenum`, `outdate`, `outimage`, `outbackimage`
- 기타: `parkonplace`, `outflag`, `manual`, `manflag`, `dendflag`, `tendflag`, `denddate`, `mid`, `mname`, `regdate`, `moddate`

- `cartype`: `1` 소형, `2` 중형, `3` 대형
- `parktype`: `0` 일반, `1` 분실, `2` 판독불가, `3` 무효

금액 의미:

- `parkfee`: 할인 전 정상요금
- `discountfee`: 적용된 총 할인금액
- `payfee`: 최종 결제할 금액
- `paidfee`: 성공한 결제와 취소를 합산한 실제 결제금액

### 5.8 `tbcardinfo`

카드 승인·취소 거래를 수정하지 않고 행 단위로 추가한다.

주요 컬럼: `xindex`, `paymentid`, `orgpaymentid`, `pindex`, `sitenum`, `groupnum`, `devicenum`, `carnum`, `termid`, `posid`, `dealtype`, `credittype`, `money`, `rescode`, `msg`, `dealnum`, `acceptnum`, `receiptnum`, `cardname`, `cardno`, `branchnum`, `dealdate`, `regdate`

- 승인 성공금액은 양수다.
- 승인취소·망취소 성공금액은 음수다.
- 실패 거래금액은 0이다.
- 카드번호는 마스킹 값만 저장한다.
- `dealtype`: `APPROVE`, `CANCEL`, `NETCANCEL`

### 5.9 `tdiscount`

할인 기준정보다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `diskey`, `distype`, `disvalue`, `title`, `maxcount`, `opt`, `useflag`, `mid`, `regdate`, `moddate`

- 유일키: `sitenum + groupnum + diskey`
- `distype`: `1` 시간, `2` 금액, `3` 퍼센트, `4` 고정금액, `5` 복수할인
- 복수할인은 `opt`에 할인키 목록을 저장한다.

### 5.10 `tdiscountinfo`

현재 유효한 할인 적용 행을 저장한다.

주요 컬럼: `xindex`, `discountid`, `pindex`, `sitenum`, `groupnum`, `carnum`, `diskey`, `distype`, `disvalue`, `disfee`, `source`, `sourceref`, `id`, `deptcode`, `devicenum`, `ip`, `image`, `indate`, `disdate`, `regdate`

- 적용 시 INSERT한다.
- 취소 시 해당 행을 DELETE한다.
- `pindex + source + sourceref`로 원격 중복 요청을 막는다.
- 삭제 후 남은 할인으로 `tparkinfo.discountfee`, `payfee`를 다시 계산한다.
- `useflag`와 취소상태 컬럼은 두지 않는다.

### 5.11 `tperiodmember`

정기권 계약과 현재 사용 차량을 한 행에 저장한다.

주요 컬럼:

- 기본: `xindex`, `sitenum`, `groupnum`, `devicenum`
- 정기권: `cardno`, `serialno`, `periodtype`, `groupcode`
- 차량: `carnum1`, `cartype1`, `carnum2`, `cartype2`
- 회원: `name`, `tel`, `addr`, `companycode`, `deptcode`
- 기간: `startdate`, `enddate`, `serviceday`
- 조건: `parktype`, `parktimecode`, `parkarea`, `parkvalidday`, `parklevel`
- 금액: `parkprice`, `diskey`, `paytype`
- 상태: `useflag`, `outflag`
- 관리: `mid`, `mname`, `regdate`, `moddate`

- 실제 입차 차량은 `carnum1`만 검사한다.
- `carnum2`는 관리용이며 차량 변경 시 다시 `carnum1`으로 등록한다.
- `parkarea`, `parkvalidday`는 7자리 `0/1` 문자열을 사용한다.
- 신규 회원의 `outflag` 기본값은 출차상태인 `O`다.

### 5.12 `tperiodinout`

등록차량 입차부터 출차까지 한 행으로 저장한다.

주요 컬럼: `xindex`, `periodindex`, `sitenum`, `groupnum`, `ineventid`, `outeventid`, `cardno`, `name`, `enddate`, `carnum`, `cartype`, `inlaneid`, `indevicenum`, `indate`, `inimage`, `inbackimage`, `outlaneid`, `outdevicenum`, `outdate`, `outimage`, `outbackimage`, `parktime`, `parkonplace`, `outflag`, `manual`, `mid`, `mname`, `note`, `regdate`, `moddate`

- 상태는 `I`, `O`만 사용한다.
- 중복 입차 시 기존 `I` 행을 새 입차시각으로 자동 출차 처리하고 `note='DUPLICATE_ENTRY'`를 기록한다.

### 5.13 `tperiodaccount`

정기권 신규·연장·취소 수금 이력이다.

주요 컬럼: `xindex`, `accountid`, `orgaccountid`, `periodindex`, `sitenum`, `groupnum`, `devicenum`, `devicetype`, `cardno`, `carnum`, `name`, `companycode`, `deptcode`, `accounttype`, `beforedate`, `startdate`, `enddate`, `money`, `paytype`, `paymentid`, `mid`, `mname`, `regdate`

- `accounttype`: `NEW`, `EXTEND`, `CANCEL`
- 신규·연장은 양수, 취소는 음수금액 행을 추가한다.
- 카드 승인정보는 `tbcardinfo`에 저장한다.

### 5.14 `tperiodparktime`

정기권 사용 가능 시간대다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `code`, `description`, `starttime`, `endtime`, `useflag`, `mid`, `mname`, `regdate`, `moddate`, `note`

- 유일키: `sitenum + groupnum + code`
- 종료시각이 시작시각보다 작으면 다음 날까지 사용하는 시간대다.

### 5.15 `tperiodwaiting`

정기권 대기 신청과 배정 순서를 관리한다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `periodtype`, `carnum`, `cartype`, `name`, `tel`, `waitingdate`, `waitingno`, `parkarea`, `status`, `periodindex`, `mid`, `mname`, `regdate`, `moddate`

- `status`: `WAIT`, `CONTACT`, `ASSIGNED`, `CANCEL`
- 배정 완료 시 생성된 `tperiodmember.xindex`를 저장한다.

### 5.16 `tparkfee`

일반요금 단계와 정기권 상품가격을 관리한다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `ptype`, `pname`, `weektype`, `dayshift`, `cartype`, `feestep`, `parktime`, `parkfee`, `maxcount`, `periodtype`, `periodname`, `useflag`, `mid`, `modmid`, `regdate`, `moddate`

- `ptype`: `0` 일반요금, `1` 정기권 상품
- `weektype`: `1` 평일, `2` 주말·휴일
- `dayshift`: `0` 주간, `1` 야간
- `cartype`: `1` 소형, `2` 중형, `3` 대형
- 일반요금 유일 기준: `sitenum + groupnum + weektype + dayshift + cartype + feestep`
- `maxcount=0`은 무제한 반복이다.

### 5.17 `tholiday`

공휴일·대체공휴일·현장 지정휴일이다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `holiday`, `msg`, `useflag`, `mid`, `regdate`, `moddate`

- 유일키: `sitenum + groupnum + holiday`
- 토요일과 일요일은 FeeEngine에서 자동 판정한다.

### 5.18 `tparkvariable`

사이트·그룹별 운영 설정이다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `cmdtype`, `val`, `opt`, `msg`, `useflag`, `mid`, `mname`, `regdate`, `moddate`

- 유일키: `sitenum + groupnum + cmdtype`
- 설정 변경 시 `tparksync.configversion`을 증가시킨다.

### 5.19 `tdisaccount`

원격할인 로그인 계정이다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `id`, `passhash`, `name`, `tel`, `donghosu`, `deptcode`, `grade`, `useflag`, `lastlogindate`, `regdate`, `moddate`

- 사무실은 직원별 계정을 만들고 같은 `deptcode`로 묶는다.
- 아파트는 `donghosu` 단위 공동계정을 사용할 수 있다.

### 5.20 `tdisdept`

원격할인 계정을 묶는 업체·부서정보다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `deptcode`, `deptname`, `donghosu`, `maxcount`, `carmaxcount`, `useflag`, `regdate`, `moddate`

- 사무실의 여러 직원 계정을 같은 업체로 집계한다.
- 아파트 공동계정은 `tdisaccount`에 직접 관리할 수 있다.

### 5.21 `taccountinfo`

계정별·할인키별 현재 사용 횟수다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `id`, `diskey`, `nowcount`, `maxcount`, `lastresetdate`, `regdate`, `moddate`

- 유일키: `sitenum + groupnum + id + diskey`
- 할인 적용 시 증가하고 할인 취소 시 감소한다.
- 업체 요금 징수 후 `nowcount`를 0으로 초기화한다.

### 5.22 `tnorecognition`

아직 처리하지 않은 미인식 입차정보만 저장한다.

주요 컬럼: `xindex`, `eventid`, `sitenum`, `groupnum`, `laneid`, `devicenum`, `devicename`, `ip`, `iodate`, `image`, `backimage`, `regdate`

- 운영자가 차량번호를 입력하고 `tparkinfo.manual=1`로 입차 등록한다.
- 수동 입차 성공과 같은 트랜잭션에서 해당 행을 삭제한다.

### 5.23 `tblacklist`

입차 금지 차량 목록이다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `carnum`, `startdate`, `enddate`, `msg`, `useflag`, `mid`, `mname`, `regdate`, `moddate`

- 활성 기간에는 차단기를 열지 않고 입차자료도 만들지 않는다.
- 거부 이벤트는 `tparkevent`에 남긴다.

### 5.24 `tbangmun`

기간형 방문차량 예약정보다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `cardid`, `carnum`, `name`, `tel`, `visitobject`, `visitplace`, `startdate`, `enddate`, `diskey`, `reservedtype`, `useflag`, `mid`, `mname`, `regdate`, `moddate`

- 유효기간 동안 여러 번 입·출차할 수 있다.
- 각 일반차량 입차 행은 `tparkinfo.visitindex`로 방문예약을 참조한다.
- `reservedtype`: `0` 일반, `1` 긴급, `2` 외상

### 5.25 `twelfare`

복지·공공 사전할인 차량이다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `welfareid`, `welfaretype`, `carnum`, `name`, `tel`, `startdate`, `enddate`, `diskey`, `useflag`, `mid`, `regdate`, `moddate`

- 유효한 차량이면 `tdiscountinfo`에 자동할인을 등록한다.
- 기존 `vehicle_eligibility` 기능을 이 테이블로 통합한다.

### 5.26 `tcompany`

정기권 회원의 회사·소속기관 기본정보다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `companycode`, `companyname`, `boss`, `addr`, `tel`, `fax`, `useflag`, `mid`, `mname`, `regdate`, `moddate`, `note`

- 유일키: `sitenum + groupnum + companycode`

### 5.27 `tdepartment`

회사 아래 부서정보다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `companycode`, `deptcode`, `deptname`, `tel`, `fax`, `useflag`, `mid`, `mname`, `regdate`, `moddate`, `note`

- 유일키: `sitenum + groupnum + companycode + deptcode`

### 5.28 `tparkingnum`

주차장별 일반·등록차량 입·출차 누계와 수용대수를 저장한다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `inilbancnt`, `outilbancnt`, `inregcnt`, `outregcnt`, `ilbanfullnum`, `regfullnum`, `moddate`

- 사이트·그룹당 한 행이다.
- 누계는 자정에 초기화하지 않는다.
- 현장 실제 대수와 다르면 관리자가 입·출차 누계를 직접 수정할 수 있다.
- 현재 일반대수: `inilbancnt - outilbancnt`
- 현재 등록대수: `inregcnt - outregcnt`
- 여유대수는 API에서 수용대수와 현재대수로 계산한다.

### 5.29 `tticketnum`

발급장비별 티켓번호 상태다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `devicenum`, `ticketnum`, `minnum`, `maxnum`, `moddate`

- 유일키: `sitenum + groupnum + devicenum`
- 행 잠금 후 다음 번호를 발급하며 최대번호 다음에는 최소번호로 순환한다.
- `ticketdata`: 티켓번호 5자리 + 차종 1자리 + 사이트 3자리 + 장비 3자리

### 5.30 `tmanager`

담당자·운영자 계정이다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `mid`, `mpwhash`, `grade`, `name`, `tel`, `dept`, `useflag`, `lastlogindate`, `regdate`, `moddate`

- 유일키: `sitenum + groupnum + mid`
- 권한: `1` 근무자, `2` 관리자1, `3` 관리자2

### 5.31 `tlogon`

관리자 로그인·로그아웃 이력이다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `devicenum`, `devicename`, `devicetype`, `mid`, `mname`, `ip`, `logdatetime`, `logtype`

- `logtype`: `0` 로그아웃, `1` 로그인, `2` 로그인 실패
- 비밀번호나 비밀번호 해시는 복사하지 않는다.

### 5.32 `talarmcode`

장비 종류별 오류코드 기준정보다.

주요 컬럼: `xindex`, `devicetype`, `errcode`, `errname`, `errgrade`, `useflag`, `regdate`, `moddate`

- 유일키: `devicetype + errcode`
- 등급: `1` 안내, `2` 경고, `3` 장애, `4` 긴급

### 5.33 `tdevicealarm`

장비 장애 발생과 복구 이력이다.

주요 컬럼: `xindex`, `alarmid`, `sitenum`, `groupnum`, `deviceid`, `devicenum`, `devicename`, `devicetype`, `ip`, `errcode`, `alarmkind`, `alarmtime`, `repairtime`, `alarmmessage`, `repairmessage`, `mid`, `mname`

- 같은 장비·오류의 미복구 행이 있으면 중복 추가하지 않는다.
- 현재 장애는 `repairtime IS NULL`로 조회한다.

### 5.34 `tvaninfo`

주차장별 VAN사와 가맹점정보다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `vancode`, `vanname`, `branchcode`, `branchname`, `useflag`, `regdate`, `moddate`

- 단말기번호와 POS 번호는 `tdeviceinfo`에서 관리한다.
- 실제 승인내역은 `tbcardinfo`에 저장한다.

### 5.35 `tapsinfo`

원격 모니터링에 표시할 무인정산기 현재 상태다.

주요 컬럼: `xindex`, `sitenum`, `groupnum`, `apsip`, `devicenum`, `pindex`, `indatetime`, `outdatetime`, `totalfee`, `disfee`, `calfee`, `distime`, `status`

- 장치별 한 행만 유지한다.
- 상세 결제와 차량정보는 다른 테이블에서 조회한다.

### 5.36 `tgateinfo`

원격 차단기 제어 명령과 결과다.

주요 컬럼: `xindex`, `commandid`, `sitenum`, `groupnum`, `devicenum`, `command`, `status`, `mid`, `mname`, `regdate`, `completedate`, `msg`

- `commandid`는 중복 실행 방지 유일키다.
- `command`: `OPEN`, `CLOSE`, `RESET`
- `status`: `WAIT`, `RUN`, `OK`, `ERROR`
- Edge가 `WAIT` 명령을 가져가 현장 차단기를 제어한다.

## 6. 주요 처리 흐름

### 6.1 일반 입차

1. `tparkevent.eventid` 중복을 확인한다.
2. `tblacklist`를 먼저 확인한다.
3. `tperiodmember.carnum1`을 확인한다.
4. `tbangmun`, `twelfare`를 확인한다.
5. 일반차량이면 티켓번호를 트랜잭션으로 발급한다.
6. `tparkinfo`를 `I` 상태로 생성한다.
7. `tparkingnum.inilbancnt`를 증가시킨다.

### 6.2 등록차량 입차

1. 유효기간, 서비스일, 주차구역, 요일, 시간대, 부제 조건을 검사한다.
2. 기존 미출차 `I` 행이 있으면 새 입차시각으로 자동 출차 처리한다.
3. `tperiodinout`을 `I`로 생성하고 `tperiodmember.outflag='I'`로 변경한다.
4. `tparkingnum.inregcnt`를 증가시킨다.

### 6.3 할인 적용과 취소

1. 할인 적용 시 `tdiscountinfo`에 한 행을 추가한다.
2. 원격할인은 `taccountinfo.nowcount`를 같은 트랜잭션에서 증가시킨다.
3. 할인 취소 시 적용 행을 삭제하고 원격할인 횟수를 감소시킨다.
4. 남은 할인으로 `tparkinfo.discountfee`, `discounttime`, `payfee`를 다시 계산한다.

### 6.4 카드 승인과 취소

1. 승인 성공은 `tbcardinfo.money`에 양수로 추가한다.
2. 승인취소와 망취소 성공은 원거래를 연결하고 음수로 추가한다.
3. 성공한 거래금액의 합계를 `tparkinfo.paidfee`에 반영한다.
4. 최종 결제가 완료되면 `tparkinfo.outflag='X'`로 변경한다.

### 6.5 출차

1. 동일 `outeventid` 재처리를 막는다.
2. 일반차량은 `tparkinfo`를 `O`로 변경하고 `outilbancnt`를 증가시킨다.
3. 등록차량은 `tperiodinout`과 `tperiodmember`를 `O`로 변경하고 `outregcnt`를 증가시킨다.
4. 0원, 서비스, 유예시간, 정상 결제 차량은 출차를 허용한다.

## 7. `newfull_schema.sql` 작성 기준

- `000_full_schema.sql`은 수정하지 않는다.
- 모든 테이블은 `CREATE TABLE IF NOT EXISTS`로 작성한다.
- 생성 순서는 마스터 → 장비·설정 → 요금·할인 → 회원 → 운영이력 순서로 한다.
- 외래키 생성 순서를 지켜 신규 DB에서 한 번에 실행되게 한다.
- MySQL 5.7에서 실행되지 않는 기능을 사용하지 않는다.
- 기본 시험현장 9001, 그룹 2, 입·출차 차로와 장비자료를 포함한다.
- 기본 요금, 할인, 운영변수는 현재 `000_full_schema.sql`의 시험값을 새 컬럼에 맞춰 옮긴다.
- 실제 테이블 생성 검증은 빈 시험 DB에서 수행한다.

## 8. 구현 영향

신규 스키마를 실제 적용하면 Dapper SQL과 모델의 테이블·컬럼명을 함께 변경해야 한다. 특히 다음 변경은 소스 영향이 크다.

- `parking_session` → `tparkinfo`
- `payment` → `tbcardinfo`
- `parking_session_discount` → `tdiscountinfo`
- `parking_site`, `parking_lane`, `parking_device`, `parking_device_link` 이름 변경
- `vehicle_eligibility` 제거와 `twelfare` 통합
- EventId의 저장형식 변경

따라서 `newfull_schema.sql` 작성과 검증 후 별도의 소스 변경 계획을 작성한다.
