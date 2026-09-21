# 잔여 프로그램 기반 구조 설계

## 1. 목표

기존 서버·현장 중계 구조를 유지하면서 아직 없는 프로그램 네 개의 실행 가능한 기반 프로젝트를 먼저 만든다. 이번 단계는 전체 솔루션의 뼈대를 완성하는 것이 목적이며 실제 유인정산, 자동 업데이트, 중앙 집계, 요금 검증 업무는 후속 단계에서 구현한다.

대상 프로그램은 다음과 같다.

- `Parking.Operator`
- `Parking.TerminalAgent`
- `Parking.Worker`
- `Parking.FeeTester`

기존 LPR, 무인정산기, CustomerWeb, AdminWeb는 현재 운영 소스를 나중에 신규 API 구조로 수정한다. 모바일 결제도 별도 단계에서 현재 웹 프로그램을 기준으로 설계하므로 신규 프로젝트를 만들지 않는다.

## 2. 전체 구조

```text
현장 Windows
  Parking.Operator ──HTTP──▶ Parking.EdgeService
  Parking.TerminalAgent ───▶ 현장 프로그램 상태 감시

중앙 서버
  Parking.Worker ──────────▶ Parking.Api 및 중앙 작업

개발·운영 시험 PC
  Parking.FeeTester ───────▶ Parking.FeeEngine
```

현장 업무 프로그램은 중앙 API나 MySQL에 직접 연결하지 않는다. `Parking.Operator`는 `Parking.EdgeService`만 호출한다. `Parking.TerminalAgent`는 주차 업무 판단을 하지 않고 현장 프로그램의 실행 상태만 관리한다. `Parking.Worker`는 실시간 입출차 요청을 처리하지 않고 중앙 예약 작업만 수행한다.

## 3. Parking.Operator

### 프로젝트 형식

- .NET 9 WinForms
- 실행 위치: 현장 Windows PC
- 참조: `Parking.Contracts`
- 통신: `HttpClient`로 `Parking.EdgeService` 호출

### 이번 단계 구현

- 프로그램이 정상 실행되는 기본 폼
- EdgeService 주소를 읽는 `appsettings.json`
- DI, 로깅, `HttpClient` 등록
- EdgeService 상태 확인 결과 표시
- 빌드와 실행 확인

### 후속 기능

- 차량번호 전체·뒤 4자리 조회
- 요금조회와 할인 적용
- 카드·현금 결제
- 차량번호 정정
- 수동 입차·출차와 원격 차단기 개방
- 입출차 사진 표시

## 4. Parking.TerminalAgent

### 프로젝트 형식

- .NET 9 Worker Service
- 실행 위치: 현장 Windows PC
- Windows Service 실행 지원
- 참조: 공통 계약 없음

### 이번 단계 구현

- 서비스 시작·종료 로그
- 감시주기를 읽는 `appsettings.json`
- 등록된 프로그램 목록을 읽는 설정 모델
- 주기적으로 프로세스 상태를 확인하는 기본 루프
- 실제 프로세스 실행·종료·업데이트는 수행하지 않고 상태만 기록

### 후속 기능

- EdgeService, EdgeManager, LPR, 무인정산기 자동실행
- 비정상 종료 감지와 재시작
- 버전 확인과 안전한 업데이트
- 로그 압축과 중앙 전송
- 반복 장애 시 재시작 제한과 관리자 알림

## 5. Parking.Worker

### 프로젝트 형식

- .NET 9 Worker Service
- 실행 위치: 중앙 서버
- 참조: `Parking.Contracts`
- 통신: `Parking.Api` HTTP 호출

### 이번 단계 구현

- 서비스 시작·종료 로그
- Parking.Api 주소와 작업주기를 읽는 `appsettings.json`
- 주기적으로 API 상태를 확인하는 기본 루프
- 실제 데이터 변경 작업은 수행하지 않음

### 후속 기능

- 일·월 주차 및 매출 집계
- 오래된 사건·로그·이미지 정리 요청
- 중앙 실패 작업 재처리
- 운영 이상 알림

현장 Outbox 재전송은 계속 `Parking.EdgeService`가 담당하며 `Parking.Worker`로 옮기지 않는다.

## 6. Parking.FeeTester

### 프로젝트 형식

- .NET 9 WinForms
- 실행 위치: 개발·운영 시험 PC
- 참조: `Parking.FeeEngine`, `Parking.Contracts`

### 이번 단계 구현

- 프로그램이 정상 실행되는 기본 폼
- 사이트, 그룹, 차종, 입차시각, 출차시각 입력 영역
- 계산 실행 버튼과 결과 표시 영역
- 이번 단계에서는 임시 기본 요금설정으로 `Parking.FeeEngine` 호출 구조만 연결

### 후속 기능

- Parking.Api에서 실제 요금표·할인·휴일·운영변수 로딩
- 정상요금·할인·결제금액·주차시간 상세 표시
- 기대값을 가진 시험 시나리오 저장과 일괄 실행
- 현장별 회귀시험 결과 저장

## 7. 공통 구성

- 네 프로젝트를 `ParkingSystem.sln`에 추가한다.
- 설정은 각 프로젝트의 `appsettings.json`에 둔다.
- 비밀번호와 연결 문자열을 소스에 저장하지 않는다.
- JSON 계약은 PascalCase를 유지한다.
- 프로젝트 이름, 네임스페이스, 폴더명은 동일하게 유지한다.
- 실행 실패는 로그에 원인을 남기고 프로그램 전체가 조용히 종료되지 않게 한다.

## 8. 시험 기준

- 네 프로젝트가 개별 빌드된다.
- `ParkingSystem.sln` 전체 빌드가 성공한다.
- `Parking.Operator`와 `Parking.FeeTester` 기본 폼이 실행된다.
- `Parking.TerminalAgent`가 설정된 감시주기로 상태 로그를 남긴다.
- `Parking.Worker`가 Parking.Api 상태 확인 로그를 남긴다.
- 기존 자동시험이 모두 유지된다.

## 9. 제외 범위

- 기존 LPR 프로그램 수정
- 기존 무인정산기 수정
- CustomerWeb와 AdminWeb 수정
- 모바일 결제와 PG 연동
- 실제 결제단말 제어
- 자동 업데이트와 프로세스 강제 재시작
- 중앙 집계 테이블과 정리 정책 구현
- 완성된 유인정산·요금검증 화면
