# 장비번호 기반 Edge 장비 식별 설계

## 목적

현장 실행 프로그램은 DB 내부키인 `deviceid`를 설정으로 갖지 않는다. 모든 현장 프로그램은 공통으로 `sitenum`, `groupnum`, 프로그램별 `devicenum`을 사용하고, `Parking.EdgeService`가 현장 설정에서 실제 `deviceid`를 찾아 내부 처리에 사용한다.

## 식별 규칙

- 외부 실행 프로그램의 장비 식별값: `(sitenum, groupnum, devicetype, devicenum)`
- 서버·Edge 내부 참조값: `deviceid`
- 같은 `devicenum`은 서로 다른 현장에서는 중복될 수 있지만 한 현장 안에서는 장비 종류와 관계없이 고유하다.
- 한 현장 설정 안에서 위 외부 식별 조합은 정확히 한 장비와 일치해야 한다.
- 장비가 없거나 둘 이상이면 등록을 거부하고 원인을 로그와 오류 응답에 남긴다.

APSMain 시험 구성은 다음과 같다.

- `SITENUM=9001`
- `GROUPNUM=2`
- `APSNUM=201`
- `DEVICETYPE=KIOSK`는 APSMain 코드에서 고정한다.
- 위 조합은 현장 설정의 `deviceid=9301`로 해석된다.

## 설정 정리

APSMain `App.config`에서 `SITEID`, `APSDEVICEID`를 제거한다. Edge 연동 설정은 기존 APSMain 설정인 `SITENUM`, `GROUPNUM`, `APSNUM`을 그대로 읽는다. `EDGESERVICEUSE`, `EDGESERVICEURL`은 유지한다.

다른 현장 실행 프로그램도 동일한 원칙을 사용한다. LPR은 파일/패킷의 `sitenum`, `groupnum`, `devicenum`을 사용하고, KioskSimulator는 `--site`, `--group`, `--device-number`를 사용한다. 중앙·Edge 관리 프로그램이 설정과 연결관계를 관리할 때만 `deviceid`를 표시하거나 편집할 수 있다.

## EdgeService 장비 해석

`LocalConfigurationService`에 장비 해석 기능을 둔다. 현재 로컬 현장 설정을 읽고 다음 조건을 모두 검사한다.

1. 요청 `sitenum`이 EdgeService에 설정된 현장과 같은가.
2. `parking_device`의 `DeviceType`이 요청 종류와 같은가.
3. `DeviceNumber`가 요청 `devicenum`과 같은가.
4. 장비가 사용 중인가.
5. 장비의 차로가 존재하며 해당 차로의 `GroupNumber`가 요청 `groupnum`과 같은가.

정확히 한 장비가 확인되면 `ParkingDevice.DeviceId`를 반환한다. 이 이후의 연결 레지스트리, 장비 링크, 대기 이벤트 저장, 전광판 선택에는 기존 `deviceid`를 그대로 사용한다.

## SignalR 등록 계약

Kiosk Hub 등록 계약을 다음과 같이 변경한다.

```text
Register(sitenum, groupnum, devicenum)
```

Hub는 `devicetype=KIOSK`로 장비를 해석한 뒤 `KioskConnectionRegistry`에 내부 `deviceid`와 SignalR 연결 ID를 저장한다. 재연결 시에도 동일한 세 값을 다시 전송한다.

기존 `Register(deviceid)` 계약은 제거한다. 잘못된 현장, 없는 장비, 중복 장비는 `HubException`으로 등록을 거부한다.

## Kiosk 사건 완료 계약

APSMain과 KioskSimulator는 완료 요청에 내부 `deviceid`를 넣지 않는다.

```text
POST /api/v1/local/kiosks/events/{eventId}/complete
body: { sitenum, groupnum, devicenum }
```

EdgeService는 동일한 규칙으로 KIOSK 장비를 해석하고, 해석된 `deviceid`와 대기 사건의 `KioskDeviceId`가 같은지 확인한 뒤 기존 완료 처리를 실행한다.

## 중앙 API의 역할

중앙 API와 EdgeGateway가 받는 입차·출차 사건에는 EdgeService가 이미 해석한 내부 `deviceid`를 사용한다. 중앙 API는 DB 내부키의 유효성, 현장, 차로, 그룹 방향을 기존처럼 검증한다. 중앙 API에 장비번호 변환을 중복 구현하지 않는다.

현장 설정 API는 `ParkingDevice`의 `DeviceId`와 `DeviceNumber`를 모두 유지한다. `DeviceId`는 설정·연결관계의 내부키이고 `DeviceNumber`는 현장 프로그램이 사용하는 번호다.

## APSMain 변경

- `EdgeServiceOptions`를 `BaseAddress`, `Sitenum`, `Groupnum`, `DeviceNumber`로 변경한다.
- `SITENUM`, `GROUPNUM`, `APSNUM`을 읽고 양수 여부를 검증한다.
- SignalR 연결과 재연결 시 세 값을 등록한다.
- 수동 차량조회는 `Sitenum`, `Groupnum`을 사용한다.
- 완료 요청은 새 URL과 JSON 본문을 사용한다.
- 화면과 로그에는 사용자가 관리하는 `APSNUM`을 표시한다.

WinForms 파일은 기존대로 `.cs`, `.Designer.cs`, `.resx` 구조를 유지한다.

## KioskSimulator 및 실행 배치

KioskSimulator 인수를 `--site 9001 --group 2 --device-number 201`로 변경한다. 전체 실행 배치는 이 인수를 사용하고 각 실행 단계 사이의 `pause`를 유지한다.

## 오류 처리

- 현장 불일치: `INVALID_SITE`
- 장비 없음: `DEVICE_NOT_FOUND`
- 장비 중복: `DEVICE_AMBIGUOUS`
- 장비 비활성 또는 그룹 불일치: 외부에는 `DEVICE_NOT_FOUND`로 처리하고 상세 원인은 EdgeService 로그에 남긴다.
- 완료 사건과 등록 장비 불일치: HTTP 404

## 시험 범위

- 동일 `devicenum`이 다른 현장에 있어도 현재 현장의 장비만 선택한다.
- 동일 현장의 다른 그룹 장비를 선택하지 않는다.
- 비활성 장비를 등록하지 않는다.
- SignalR 최초 연결과 재연결에서 동일한 외부 식별값을 사용한다.
- 완료 요청이 외부 식별값을 내부 `deviceid`로 해석한다.
- APSMain 옵션에 `SITEID`, `APSDEVICEID`가 없어도 정상 로드된다.
- 잘못된/중복 장비는 명확하게 실패한다.
- 기존 LPR 장비번호 변환과 중앙 API의 `deviceid` 검증 회귀시험이 유지된다.

## 완료 조건

- APSMain과 KioskSimulator 설정·인수에 내부 `deviceid`가 없다.
- `SITEID`, `APSDEVICEID` 참조가 APSMain 코드·설정·현재 문서에서 제거된다.
- 런타임 프로그램은 공통 `sitenum`, `groupnum`, `devicenum`으로 EdgeService에 접속한다.
- EdgeService 이후 내부 흐름은 고유한 `deviceid`를 사용한다.
- 전체 솔루션, APSMain, APSMain 연동 테스트가 빌드·통과한다.
