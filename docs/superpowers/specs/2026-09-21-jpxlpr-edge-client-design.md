# JPXLPR EdgeService Client Design

## Goal

JPXLPR을 카메라 영상 수신, 차량번호 인식, 이미지 저장, EdgeService 전송만 담당하는 현장 LPR 프로그램으로 정리한다. 중앙 API, APSMain, 전광판, 차단기는 JPXLPR이 직접 제어하지 않는다.

## Runtime Identity

- 공통 식별자는 `SITENUM`, `GROUPNUM`이다.
- 각 카메라는 설정된 `LANEID`, `DEVICENUM`, `DIRECTION`을 사용한다.
- `DEVICENUM`은 외부 장비번호이며 `deviceid`는 사용하지 않는다.
- 시험 현장은 `SITENUM=9001`, `GROUPNUM=2`이다.
- 입차 기본값은 `LANEID=9010`, `DEVICENUM=101`, `DIRECTION=Entry`이다.
- 출차 기본값은 `LANEID=9020`, `DEVICENUM=201`, `DIRECTION=Exit`이다.

## Responsibilities Kept

- Novitec 카메라 연결과 최대 4개 카메라 운용
- NGis/Novaeye 차량번호 인식
- 카메라별 노출 및 인식 설정 화면
- 입출차 이미지 저장
- 기존 `JPXLpr.cs`, `JPXLpr.Designer.cs`, `JPXLpr.resx` WinForms 구조
- 운영 로그와 카메라 상태 표시

## EdgeService Transmission

인식 완료 시 다음 파일명을 생성한다.

`SSS_GGG_DDD_LLL_(Entry|Exit)_yyyyMMddHHmmssfff_차량번호_EventId.jpg`

- `SSS`: sitenum 3자리 이상 실제 값
- `GGG`: groupnum 3자리
- `DDD`: devicenum 3자리
- `LLL`: laneid 4자리
- `EventId`: 새 GUID, 하이픈 없이 32자리

JPXLPR은 EdgeService의 LPR TCP 포트로 다음 프레임을 전송한다.

- 기본 호스트: `127.0.0.1`
- 기본 포트: `29200`
- 요청: `STX(0x02) + CP949 파일명 + ETX(0x03)`
- 성공 응답: `ACK|EventId`
- 실패 응답: `NACK|사유` 또는 연결·시간초과 오류

카메라별 송신은 서로 독립적이어야 한다. 연결이 끊어지면 해당 사건을 메모리 큐에 유지하고 제한된 간격으로 재전송한다. 동일 사건 재전송은 같은 파일명과 같은 EventId를 사용해 중앙 멱등성을 유지한다. 프로그램 종료 시 미전송 사건은 JSON 파일에 보존하고 다음 실행 때 복구한다.

## Removed Legacy Scope

다음 기능과 파일은 JPXLPR에서 제거한다.

- 중앙 REST API 직접 호출과 `Tcp/RestHelper.cs`
- APSMain 직접 연결과 `Tcp/LocalSocket.cs`
- 관리서버 UDP 연결과 `Tcp/UDPSocket.cs`
- `CarInWorker.cs`와 기존 입차·출차 API DTO
- `LprResultWorker.cs`의 미완성 API·전광판·차단기 자리 코드
- `LDM/` 전체, `LDMShow.cs`, `LDMShow.Designer.cs`, `LDMShow.resx`
- `DbModels/` 전체
- `HelpClass/BaseRequest.cs`, `CarInRequest.cs`, `CarInResponse.cs`
- `App.config`의 `APIURI`, `HOSTIP`, `HOSTPORT`, `APSUSE`, `APSIP`, `APSPORT`
- `JPXConfig`의 REST, UDP, APS, LDM, 요금·유예시간 관련 필드
- 메인 폼의 전광판 표시, 차단기 명령, UDP 패킷, APS 패킷, REST 처리 코드와 관련 메뉴

`HelpClass/CameraInfo.cs` 등 카메라 설정에 필요한 모델은 유지한다. `BaseClass/StructHelper.cs`와 `ApsCmd.cs`는 남은 카메라·EdgeService 흐름에서 참조하지 않으면 삭제한다.

## New Components

### `EdgeLprOptions`

App.config와 카메라 설정에서 EdgeService 주소 및 카메라별 식별자를 읽고 검증한다. sitenum, groupnum, laneid, devicenum이 0 이하이거나 direction이 Entry/Exit가 아니면 해당 카메라 전송을 시작하지 않고 오류를 표시한다.

### `EdgeLprFrame`

파일명 생성과 CP949 STX/ETX 프레임 생성을 담당한다. 파일명 규칙과 EventId 재사용을 단위시험할 수 있는 순수 코드로 작성한다.

### `EdgeLprClient`

TCP 연결, 프레임 송신, ACK/NACK 수신, 시간초과, 재연결을 담당한다. UI나 카메라 SDK를 참조하지 않는다.

### `EdgeLprOutbox`

미전송 사건의 큐, JSON 저장, 시작 시 복구를 담당한다. ACK를 받은 사건만 제거한다.

## Main Flow

1. 카메라가 이미지를 수신한다.
2. 기존 인식 엔진이 차량번호를 결정한다.
3. 카메라 설정에서 laneid, devicenum, direction을 읽는다.
4. EventId와 표준 파일명을 생성하고 이미지를 저장한다.
5. 사건을 Outbox에 먼저 등록한다.
6. EdgeLprClient가 EdgeService에 프레임을 전송한다.
7. `ACK|EventId`가 일치하면 Outbox에서 제거한다.
8. NACK, 연결 오류, 시간초과이면 같은 EventId로 재전송한다.

## Error Handling

- 설정 오류는 카메라 시작 전에 표시한다.
- 이미지 저장 실패 사건은 전송하지 않는다.
- ACK의 EventId가 요청과 다르면 성공으로 처리하지 않는다.
- NACK 사유와 재시도 횟수는 로그에 남긴다.
- 큐 파일 손상 시 원본을 별도 이름으로 보존하고 빈 큐로 시작한다.
- 종료 시 송신 작업을 중지하고 큐를 저장한 뒤 카메라를 닫는다.

## Testing

- 파일명 생성: 9001/2/101/9010 Entry와 9001/2/201/9020 Exit
- CP949 STX/ETX 프레임 생성
- 분할 수신 ACK 조립과 EventId 일치 확인
- NACK 및 시간초과 재시도
- 동일 EventId 재전송
- Outbox 저장·복구·ACK 후 삭제
- 카메라별 설정 검증
- JPXLPR 프로젝트 빌드
- 전체 `ParkingSystem.sln` 빌드와 전체 테스트
- 로컬 EdgeService `29200`을 이용한 입차·출차 실제 전송 확인

## Completion Criteria

- JPXLPR에서 REST, UDP, APS 직접 연결, LDM, 차단기, DB 모델 참조가 검색되지 않는다.
- 카메라 인식 결과가 표준 파일명으로 EdgeService에 전달되고 ACK를 받는다.
- 장애 후 재전송에서도 동일 EventId가 유지된다.
- WinForms Designer 구조와 카메라 설정 기능이 유지된다.
- 전체 빌드와 테스트가 성공한다.
