# 개발 현황과 다음 작업

기준일: 2026-09-22  
저장소: `https://github.com/kangho-Shin/ParkingSystem`  
브랜치: `codex/server-edge-foundation`

## 1. 완료된 기반

### 중앙과 Edge

- Parking.Api, EdgeGateway, EdgeService 기본 구조
- MySQL Dapper 저장소와 Newtonsoft.Json 계약
- SQLite Outbox 선저장과 재전송
- 입차·출차 EventId 멱등 처리
- 결제 PaymentId 멱등 처리
- 중앙 장애 중 입차 제한 허용과 출차 차단
- 현장 설정 조회·편집·버전 동기화
- 사이트·그룹별 요금·할인·운영변수

### 입출차와 장치

- 일반차량 `parking_session`의 `I → X → O` 흐름
- 등록차량 `tperiodmember`, `tperiodinout` 흐름
- 전체번호·뒤 4자리 조회와 복수 후보 선택
- LPR TCP STX/ETX·CP949 수신
- 장비번호를 내부 deviceid로 변환
- Kiosk SignalR 출차 전달과 완료 회신
- LPR·Kiosk·LDM 다대다 장치 연결
- 실제 LDM 문자 표시와 차단기 개방
- 입출차 이미지 파일명 저장

### 운영 프로그램

- EdgeManager 상태·목록·사진·설정 화면
- Operator 기본 유인정산 업무
- TerminalAgent 자동 실행·재시작 제한·파일 로그
- Worker 기본 실행과 API 상태 확인
- FeeTester 기본 FeeEngine 화면
- WinForms Designer 파일 분리

### JPXLpr와 APSMain

- JPXLpr의 중앙 REST·DB·APS·LDM 직접 의존 제거
- JPXLpr → EdgeService TCP 전송과 로컬 Outbox
- APSMain의 EF Core·MySQL 직접 의존 제거
- APSMain → EdgeService HTTP·SignalR 연동
- APSMain 차량조회, 요금·할인, 결제완료, 사건완료 연결
- LDM 요금 100초 고정 표시와 현재 시각 복귀

### DB와 시간

- 신규 설치용 `000_full_schema.sql`
- 시험 현장 9001/2와 장치·연결·요금·할인 기본값
- `013_korea_time_seconds.sql`
- 운전 시간 한국시간 저장과 초 이하 제거

## 2. 확인된 실제 흐름

- 같은 EventId 입차 재전송 시 동일 세션 반환
- 중앙 단절 입차 Outbox 보관 후 복구 전송
- 출구 LPR → EdgeService → APSMain → 결제/완료 → 중앙 출차
- 출차 세션의 `outflag=O`, 출차 장비와 이미지 저장
- EdgeService → 실제 LDM 표시 → 차단기 개방
- 전체 솔루션 빌드 성공

## 3. 2026-09-22 문서 정리

- 삭제된 초기 시험용 프로젝트 3개의 관련 문서 제거
- 날짜별 설계·구현계획과 영문 문서를 현재 상태 문서에 통합
- 문서를 7개 현재 기준 문서로 재구성
- 새 작업은 날짜별 문서를 추가하지 않고 이 문서를 갱신

## 4. 바로 확인할 작업

1. 비운 `parking000test`에 `000_full_schema.sql` 실행
2. `/api/v1/local/config`에서 현장·차로·장치·연결 확인
3. 전체 빌드와 전체 테스트 실행
4. APSMain 실제 정산 흐름 확인
5. `indate`, `paydate`, `outdate`의 한국시간·초 단위 확인
6. 설정 테이블의 남은 `updatedat DATETIME(6)` 통일 여부 결정

## 5. 코드 정리 후속 작업

- 삭제된 시뮬레이터를 참조하는 배치 파일 정리
- Designer nullable 및 `components` 경고 정리
- `Parking.Worker` 집계·정리·알림 업무 구현
- `Parking.FeeTester` 실제 API 설정과 회귀 시나리오 연결
- 등록차량 관리 운영 프로그램 소스 확보 후 신규 API 연동
- 실제 카드단말을 사용하는 Operator 후속 연동

## 6. 보류 범위

- Parking.AdminWeb
- Parking.CustomerWeb
- 제조사별 범용 장비 플러그인과 Parking.DeviceSdk
- DriverHost
- 중앙 영상 재인식
- RabbitMQ 기반 비동기 작업
- 중앙 서버 이중화
- 고급 개인정보·보안 정책

## 7. 작업 원칙

- 기존 테이블과 시험 데이터를 임의로 삭제하지 않는다.
- DB 변경은 순차 적용 SQL과 `000_full_schema.sql`을 함께 수정한다.
- 로컬 설정 파일을 삭제하거나 덮어쓰지 않는다.
- WinForms는 `.cs`, `.Designer.cs`, `.resx` 구조로 작성한다.
- 프로그램 변수명과 계약명은 기존 스타일을 유지한다.
- 기능 구현 후 전체 빌드와 전체 테스트를 한 번에 실행한다.
