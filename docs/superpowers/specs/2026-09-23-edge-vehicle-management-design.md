# Edge 차량 관리 화면 설계

작성일: 2026-09-23  
대상 브랜치: `codex/server-edge-foundation`

## 1. 목적

Parking.EdgeManager에 중앙 DB 기반 차량 관리 기능을 추가한다.

- 현재 주차 중인 입차차량을 별도 화면에서 조회한다.
- 정산·출차 차량을 별도 화면에서 기간 및 장치별로 조회한다.
- 관리자가 수동입차를 등록하고 현재 입차차량의 차량번호를 변경할 수 있게 한다.
- 기존 메인 화면의 실시간 모니터링과 현장 설정 기능은 유지한다.

## 2. 연결 구조

Parking.EdgeManager는 목적별로 두 개의 HTTP 클라이언트를 사용한다.

1. `EdgeManagementClient`
   - EdgeService의 로컬 주소에 연결한다.
   - 실시간 입차·처리 목록, 사진, 장치 상태, 현장 설정을 담당한다.
2. `CentralParkingClient`
   - Parking.Api의 중앙 주소에 직접 연결한다.
   - 입차·출차 조회, 수동입차, 차량번호 변경을 담당한다.

EdgeManager는 시작할 때 EdgeService의 로컬 전용 `GET /api/v1/local/central-connection`에서 `SiteId`, 중앙 API 주소와 현장 인증키를 읽는다. 일반 설정 조회 응답에는 인증키를 노출하지 않는다. 중앙 관리 API 요청에는 `X-Site-Key`를 넣는다. 원격 관리 호스트를 추가할 때도 같은 중앙 관리 API를 재사용할 수 있다. 실시간 원격 모니터링 기능은 별도로 EdgeGateway를 사용한다.

## 3. 메인 화면 변경

Parking.EdgeManager.MainForm의 상단에 다음 버튼을 추가한다.

- `입차차량 관리`
- `출차차량 조회`

현재 실시간 목록, 사진, 상태 표시 및 환경설정 UI는 그대로 유지한다. 각 버튼은 비모달 창이 아니라 소유자가 지정된 모달 관리 창을 연다. 관리 창을 닫은 뒤 메인 실시간 목록을 다시 읽는다.

모든 WinForms 화면은 `.cs`, `.Designer.cs`, `.resx` 파일로 분리한다.

## 4. 입차차량 관리 화면

### 4.1 조회 조건

- 현재 현장
- 입차 시작일·종료일(선택 조건)
- 그룹
- 입차 장치
- 차량번호 전체 또는 뒤 4자리

날짜를 지정하지 않으면 오래된 차량을 포함한 현재 `I` 상태 차량 전체를 조회한다.

### 4.2 목록

- 일반/등록 구분
- 주차번호
- 차량번호
- 차종
- 그룹
- 입차 차로
- 입차 장치번호와 장치명
- 입차 일시
- 수동입차 여부
- 입차 사진 파일명

선택한 차량의 입차 사진을 화면 오른쪽에 표시한다.

### 4.3 수동입차

별도 `ManualEntryForm`에서 다음 값을 입력한다.

- 차량번호
- 그룹
- 입차 차로와 입차 장치
- 입차 일시(현재 시각 기본값, 수정 가능)
- 차종(1 소형, 2 중형, 3 대형)

기존 입차 처리기의 중복입차 및 등록차량 판별 규칙을 재사용한다. 관리 API가 EventId를 생성하고 내부 입차 요청에 `CarType`과 `IsManual=true`를 전달한다. 등록차량이면 `tperiodinout`, 일반차량이면 `tparkinfo`에 저장한다. 일반차량은 선택한 차종을 사용하고 두 테이블 모두 `manual=1`로 기록한다. 성공 후 입차차량 목록과 메인 목록을 다시 읽는다.

### 4.4 차량번호 변경

선택한 `I` 상태 차량만 수정할 수 있다. 일반차량과 등록차량을 구분하여 각각 `tparkinfo` 또는 `tperiodinout`을 수정한다. `X`와 `O` 상태 차량은 수정할 수 없다. 성공 후 목록을 다시 읽는다.

## 5. 출차차량 조회 화면

### 5.1 조회 조건

- 처리 시작일·종료일
- 상태: 전체, 정산완료 `X`, 출차완료 `O`
- 그룹
- 처리 장치
- 차량번호 전체 또는 뒤 4자리

기본 기간은 오늘 00:00부터 현재 시각까지다.

### 5.2 시간 및 장치 기준

- `X`: `tparkinfo.paydate`와 최신 `tbcardinfo.devicenum`
- `O`: `tparkinfo.outdate`와 `tparkinfo.outdevicenum`
- 등록차량: `I → O`로 처리되므로 `O` 이력만 `tperiodinout.outdate`와 `outdevicenum` 기준으로 조회한다. 정산일과 금액 항목은 비운다.

### 5.3 목록

- 일반/등록 구분
- 주차번호
- 차량번호
- 상태
- 차종
- 그룹
- 입차 일시
- 정산 일시
- 출차 일시
- 처리 장치번호와 장치명
- 주차시간
- 원금
- 할인금액
- 결제금액
- 입차·출차 사진 파일명

선택한 차량의 입차 사진과 출차 사진을 오른쪽에 표시한다. 출차 화면은 조회 전용이다.

## 6. 중앙 API

### 6.1 입차차량 조회

`GET /api/v1/management/parking/entries`

쿼리:

- `siteId` 필수
- `from`, `to`, `groupnum`, `deviceId`, `carNumber` 선택

`tparkinfo`와 `tperiodinout`을 합쳐 `I` 상태만 반환한다. `deviceId`는 `tdeviceinfo`의 현장·그룹·장치번호 연결로 조회한다.

결과는 `page`와 `pageSize`로 조회하며 기본 200건, 최대 500건이다. 응답에는 전체 건수와 현재 페이지를 포함한다.

### 6.2 출차차량 조회

`GET /api/v1/management/parking/exits`

쿼리:

- `siteId`, `from`, `to` 필수
- `status`, `groupnum`, `deviceId`, `carNumber` 선택

일반차량의 `X/O`와 등록차량의 `O` 이력을 합쳐 최신 처리 시각 순으로 반환한다. 조회 기간은 최대 31일이며 결과는 입차 조회와 같은 방식으로 페이지 처리한다.

### 6.3 수동입차

`POST /api/v1/management/parking/manual-entries`

요청에는 현장, 그룹, 차로, 장치, 차량번호, 입차시각과 차종을 포함한다. 서버가 EventId를 생성하고 `CarType`, `IsManual`이 추가된 내부 입차 요청으로 기존 입차 처리 규칙을 실행한다. 응답은 주차번호, 일반/등록 구분과 처리 결과 코드를 포함한다.

### 6.4 차량번호 변경

`PUT /api/v1/management/parking/sessions/{sessionType}/{parkingSessionId}/car-number`

- `sessionType`: `General` 또는 `Period`
- `outflag='I'` 조건에서만 갱신한다.
- 대상이 없거나 이미 정산·출차되면 변경하지 않는다.

기존 키오스크용 차량번호 변경 API는 호환성을 위해 유지하되, 저장소의 상태 조건은 `I`로 통일한다.

## 7. 인증과 검증

- 모든 관리 API는 `siteId`와 `X-Site-Key`를 검증한다.
- 요청의 현장과 인증키의 현장이 다르면 `401 Unauthorized`를 반환한다.
- 차로·장치가 해당 현장과 그룹의 활성 입차 방향인지 검증한다.
- 차량번호는 공백 제거 후 빈 값과 허용 길이를 검사한다.
- 차종은 1~3만 허용한다.
- 조회 기간 역전과 과도한 기간은 `400 Bad Request`로 거부한다.

## 8. 오류 처리

- 중앙 API 연결 실패: 관리 화면 상단에 연결 오류를 표시하고 조회·수정 버튼을 비활성화한다.
- 조회 결과 없음: 빈 목록을 정상 표시한다.
- 수동입차 중복: 기존 입차 처리 결과 코드를 그대로 표시한다.
- 차량번호 변경 충돌: 이미 상태가 바뀌었으면 목록을 다시 읽고 사용자에게 변경 불가를 알린다.
- 사진 다운로드 실패: 목록 조회는 유지하고 사진 영역에 `사진 없음`을 표시한다.
- 모든 비동기 UI 이벤트에서 예외를 처리하여 JIT 예외창이 나타나지 않게 한다.

## 9. 코드 구성

### Parking.Contracts

- 차량 관리 검색 조건 및 목록 응답 모델
- 수동입차 요청·응답 모델
- 일반/등록 세션 구분 열거형

### Parking.Central.Data

- `IParkingManagementRepository`
- `ParkingManagementRepository`
- 일반·등록 통합 조회
- 수동입차 표시 및 차량번호 수정

### Parking.Api

- `ParkingManagementController`
- 현장 인증과 입력 검증
- 기존 `CreateEntryHandler` 재사용

### Parking.EdgeManager.Core

- `ICentralParkingClient`
- `CentralParkingClient`
- EdgeService 로컬 중앙 연결정보 응답 모델
- 조회 조건과 장치 선택용 매핑 로직

### Parking.EdgeManager

- `EntryVehicleForm.cs/.Designer.cs/.resx`
- `ExitVehicleForm.cs/.Designer.cs/.resx`
- `ManualEntryForm.cs/.Designer.cs/.resx`
- 차량번호 변경 확인 대화상자
- MainForm 버튼 및 창 실행 코드

## 10. 테스트

- 일반·등록 입차차량 통합 조회
- `I` 이외 상태가 입차 목록에서 제외되는지 검증
- 기간·그룹·장치·차량번호 필터 검증
- `X/O` 처리 시각과 장치 선택 규칙 검증
- 수동입차의 일반·등록 분기 및 `manual=1` 검증
- 차량번호 변경이 `I`에만 허용되는지 검증
- 잘못된 현장 인증키 거부
- EdgeManager 중앙 클라이언트의 URL, 쿼리, JSON 계약 검증
- 폼의 조회 조건 및 버튼 활성화 정책을 Core 단위 테스트로 검증

## 11. 제외 범위

- 출차차량 이력 수정
- 수동출차와 결제 취소
- 사용자 계정 및 권한 관리
- 원격 관리 호스트 프로그램 자체 구현
- EdgeGateway 실시간 원격 모니터링 확장
