# LPR TCP 입력 및 차로 처리기 설계

## 1. 목적

현장 LPR 프로그램이 인식 결과를 TCP로 `Parking.EdgeService`에 전달하면 EdgeService가 패킷과 이미지 파일명을 검증하고, 기존 입차·출차·Outbox·모니터링 흐름으로 연결한다. LPR은 차단기나 전광판을 직접 판단하지 않으며 EdgeService가 차로 처리 결과를 다음 단계의 전광판 출력으로 전달한다.

## 2. 전체 흐름

```text
LPR
  └─ TCP: STX + 이미지 파일명 + ETX
                  ↓
       Parking.EdgeService TCP 수신기
                  ↓
          파일명·현장설정 검증
                  ↓
              차로 처리기
          ├─ Entry → 기존 입차 처리
          └─ Exit  → 기존 출차 처리
                  ↓
             ACK 또는 NAK
```

자체 전광판은 EdgeService와 TCP로 연결되고 전광판이 RS-232로 차단기를 제어한다. 전광판 출력은 다음 구현 단계에서 별도 설계하며 이번 범위에는 포함하지 않는다.

## 3. TCP 서버

- EdgeService 내부 `BackgroundService`로 실행한다.
- 기본 수신 포트는 `29200`이며 `Edge:LprListenPort`에서 변경할 수 있다.
- `Edge:LprListenPort`가 0이면 TCP 수신기를 시작하지 않는다.
- 모든 네트워크 인터페이스에서 수신한다.
- 여러 LPR 연결을 동시에 허용한다.
- 한 연결 안에서는 패킷 순서와 응답 순서를 유지한다.
- 서로 다른 연결은 독립적으로 처리한다.
- 클라이언트가 연결을 끊으면 해당 연결의 미완성 프레임을 폐기한다.
- 서비스 종료 시 Listener와 모든 연결 처리를 취소한다.

## 4. 패킷 형식

### 요청

```text
STX(0x02) + DATA + ETX(0x03)
```

- DATA는 이미지 파일명 한 개다.
- DATA 인코딩은 기존 Windows 멀티바이트 프로그램과 맞춘 KS5601 코드페이지 949다.
- 디코딩 시 잘못된 바이트를 대체문자로 바꾸지 않고 `INVALID_ENCODING`으로 거부한다.
- STX와 ETX는 인코딩된 문자열이 아니라 단일 제어 바이트다.
- 최대 DATA 길이는 1,024바이트다.
- 한 번의 TCP 수신에 패킷 일부만 오거나 여러 패킷이 합쳐져 올 수 있다.

### 성공 응답

```text
STX + ACK|EventId + ETX
```

### 실패 응답

```text
STX + NAK|EventId|ErrorCode + ETX
```

- ACK와 NAK는 `0x06`, `0x15`가 아니라 ASCII 영문 문자열이다.
- 응답 DATA는 ASCII 범위 문자만 사용한다.
- EventId를 추출할 수 없는 오류는 EventId 자리를 비운 `NAK||ErrorCode`로 반환한다.

## 5. TCP 프레임 파서

연결마다 독립적인 FSM 파서를 사용한다.

1. STX 이전 바이트는 버린다.
2. STX를 받으면 새 프레임을 시작한다.
3. 프레임 수신 중 다시 STX를 받으면 이전 미완성 프레임을 버리고 새 프레임을 시작한다.
4. ETX를 받으면 현재 DATA를 한 패킷으로 확정한다.
5. DATA가 1,024바이트를 초과하면 해당 프레임을 폐기하고 `NAK||FRAME_TOO_LONG`을 반환한다.
6. 빈 DATA는 `NAK||EMPTY_DATA`를 반환한다.
7. 같은 수신 버퍼에 이어진 다음 STX부터 계속 파싱한다.

## 6. 이미지 파일명

```text
SSS_GGG_DDD_LLL_Entry_yyyyMMddHHmmssfff_차량번호_EventId.jpg
SSS_GGG_DDD_LLL_Exit_yyyyMMddHHmmssfff_차량번호_EventId.jpg
```

예:

```text
001_002_101_010_Entry_20260919153025123_12가3456_4a912811cb4d4c3fa934de51805a52d1.jpg
```

| 구분 | 규칙 |
|---|---|
| SSS | 사이트번호, 숫자 3자리 |
| GGG | 그룹번호, 숫자 3자리 |
| DDD | DeviceId, 숫자 3자리 |
| LLL | LaneId, 숫자 3자리 |
| Direction | `Entry` 또는 `Exit` |
| 인식시각 | `yyyyMMddHHmmssfff`, 17자리 |
| 차량번호 | 공백과 `_`가 없는 한글·영문·숫자 문자열 |
| EventId | 하이픈 없는 Guid `N` 형식, 32자리 |
| 확장자 | `.jpg`만 허용, 대소문자 구분 없음 |

파일명은 정확히 8개 구간으로 분리한다. 숫자 필드는 0보다 커야 하며 인식시각은 존재하는 날짜와 시간이어야 한다. 차량번호가 비었거나 EventId가 Guid로 변환되지 않으면 요청을 거부한다. DB와 기존 입출차 계약에는 받은 파일명 전체를 이미지 파일명으로 저장한다.

파일명의 인식시각은 현장 PC의 로컬 시각이다. 파서는 `TimeZoneInfo.Local`의 해당 시각 UTC 오프셋을 붙여 `DateTimeOffset`으로 변환한다. 현장 Edge PC의 Windows 시간대는 실제 현장 시간대로 설정돼 있어야 한다.

## 7. 입력 계약

파싱 성공 결과는 공통 계약 `LprRecognition`으로 표현한다.

```text
EventId
SiteId
Groupnum
DeviceId
LaneId
Direction
RecognizedAt
CarNumber
ImageFileName
```

TCP 수신 계층은 바이트 프레임과 문자열 디코딩까지만 담당한다. 파일명 파서는 문자열을 `LprRecognition`으로 변환한다. 차로 처리기는 파싱된 계약만 받는다.

## 8. 현장 설정 검증

차로 처리기는 `LocalConfigurationStore`의 현재 현장 설정으로 다음 순서대로 검증한다.

1. SiteId가 현재 EdgeService의 `Edge:SiteId`와 일치한다.
2. Site가 사용 상태다.
3. LaneId가 존재하고 사용 상태다.
4. 차로의 SiteId와 GroupNumber가 파일명과 일치한다.
5. DeviceId가 존재하고 사용 상태다.
6. 장비의 SiteId와 LaneId가 파일명과 일치한다.
7. 장비 종류가 `LPR`이다. 대소문자는 구분하지 않는다.
8. 차로 Direction과 파일명의 `Entry` 또는 `Exit`가 일치한다.

설정 캐시가 없으면 중앙 조회를 직접 시도하지 않고 `CONFIG_NOT_READY`를 반환한다. 설정 불일치는 기존 입출차 API까지 전달하지 않는다.

## 9. 기존 입출차 흐름 연결

### Entry

`LprRecognition`을 다음 `FieldEventRequest`로 변환한다.

- EventId: 파일명의 EventId
- SiteId, Groupnum, LaneId, DeviceId: 파일명 값
- CarNumber: 파일명 차량번호
- InDateTime: 파일명 인식시각
- EventType: `Entry`
- InImage: 전체 이미지 파일명

`EdgeEventService.AcceptEntryAsync`를 호출한다.

### Exit

`LprRecognition`을 다음 `ExitEventRequest`로 변환한다.

- EventId: 파일명의 EventId
- SiteId, Groupnum, LaneId, DeviceId: 파일명 값
- CarNumber: 파일명 차량번호
- OutDateTime: 파일명 인식시각
- EventType: `Exit`
- OutImage: 전체 이미지 파일명
- CarType: 기본값 1
- DiscountKeys: null

`EdgeEventService.AcceptExitAsync`를 호출한다.

기존 서비스가 Outbox 선저장, 중앙 전송, 장애 처리와 EdgeManager 모니터링을 그대로 수행한다. 차로 처리기는 같은 기능을 다시 구현하지 않는다.

## 10. ACK와 NAK 기준

다음 경우 ACK를 반환한다.

- 입차가 정상 처리됨
- 중앙 장애 중 입차가 현장 Outbox에 저장됨
- 출차 결과가 차단기 개방임
- 출차가 미정산·추가요금·중앙 장애로 차단됐지만 요청 자체는 정상 처리됨
- 같은 EventId가 재전송되어 기존 결과가 반환됨

다음 경우 NAK를 반환한다.

| ErrorCode | 조건 |
|---|---|
| `EMPTY_DATA` | DATA가 비어 있음 |
| `FRAME_TOO_LONG` | DATA가 1,024바이트 초과 |
| `INVALID_ENCODING` | KS5601로 디코딩할 수 없음 |
| `INVALID_FILE_NAME` | 파일명 구간 또는 확장자 오류 |
| `INVALID_SITE` | 현재 현장과 불일치 |
| `CONFIG_NOT_READY` | 현장 설정 캐시 없음 |
| `INVALID_LANE` | 차로 없음·사용중지·사이트·그룹 불일치 |
| `INVALID_DEVICE` | 장비 없음·사용중지·차로 불일치·LPR 아님 |
| `DIRECTION_MISMATCH` | 차로 방향과 파일명 방향 불일치 |
| `PROCESSING_ERROR` | 예상하지 못한 내부 처리 오류 |

내부 예외의 상세 메시지나 경로는 LPR 응답에 포함하지 않고 서버 로그에만 기록한다.

## 11. 구성

`Parking.EdgeService/appsettings.json`에 다음 값을 추가한다.

```json
"Edge": {
  "SiteId": 1,
  "DataDirectory": "",
  "ImageDirectory": "",
  "LprListenPort": 29200
}
```

## 12. 시험 기준

- 한 패킷, 분할 패킷, 여러 패킷 결합 수신을 모두 파싱한다.
- STX 이전 잡음과 프레임 도중 새 STX를 정해진 규칙대로 처리한다.
- 1,024바이트 초과와 빈 프레임에 NAK를 반환한다.
- 한글 차량번호를 KS5601로 정상 복원한다.
- 잘못된 코드페이지 바이트는 대체문자로 처리하지 않고 `INVALID_ENCODING`을 반환한다.
- 유효한 Entry 파일명을 기존 입차 흐름으로 전달하고 ACK를 반환한다.
- 유효한 Exit 파일명을 기존 출차 흐름으로 전달하고 ACK를 반환한다.
- 출차 차단 결과도 ACK를 반환한다.
- 사이트·그룹·차로·장비·방향 불일치는 각각 정해진 NAK를 반환한다.
- 같은 EventId 재전송은 기존 멱등 처리 결과를 사용한다.
- 여러 LPR 연결이 서로의 프레임 상태를 공유하지 않는다.
- 서비스 취소 시 TCP Listener가 정상 종료된다.

## 13. 완료 기준

- 실제 LPR이 TCP로 보낸 파일명 패킷을 EdgeService가 수신한다.
- 유효한 패킷이 기존 입차·출차·Outbox·EdgeManager 흐름에 나타난다.
- LPR이 EventId가 포함된 ACK 또는 NAK를 수신한다.
- 기존 전체 회귀시험과 신규 TCP·파서·차로 처리기 시험이 성공한다.
