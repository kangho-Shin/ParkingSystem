# 실행 및 시험

## 1. 준비

```bat
git pull --ff-only origin codex/server-edge-foundation
mysql -u test -p parking000test < database\mysql\000_full_schema.sql
set PARKING_RUNTIME_CONNECTION=Server=localhost;Port=3306;Database=parking000test;User ID=test;Password=실제비밀번호;
set ConnectionStrings__ParkingDatabase=%PARKING_RUNTIME_CONNECTION%
```

비밀번호와 인증키가 들어간 로컬 설정은 커밋하지 않는다.

## 2. 기본 서버 실행 순서

### Parking.Api

```bat
dotnet run --no-launch-profile --project src\Central\Parking.Api\Parking.Api.csproj
```

### Parking.EdgeGateway

```bat
dotnet run --no-launch-profile --project src\Central\Parking.EdgeGateway\Parking.EdgeGateway.csproj
```

### Parking.EdgeService

```bat
set EDGE_SITE_AUTH_KEY=<현장인증키>
dotnet run --no-launch-profile --project src\Edge\Parking.EdgeService\Parking.EdgeService.csproj
```

기본 포트는 각각 5000, 5100, 5200이며 LPR TCP는 29200이다.

## 3. 연결 확인

```bat
curl.exe -i http://localhost:5000/health
curl.exe -i http://localhost:5100/health
curl.exe -i http://localhost:5200/api/v1/management/status
curl.exe -i http://localhost:5200/api/v1/local/config
```

로컬 설정에서 현장 9001, 그룹 2, 차로 9010/9020, 장치 2001·4001~4004·5001과 연결정보를 확인한다.

## 4. 전체 빌드와 시험

```bat
dotnet build ParkingSystem.sln
dotnet test ParkingSystem.sln
```

변경을 모은 뒤 전체 시험을 한 번 실행한다. 실패하면 첫 오류의 원인을 해결한 뒤 전체 명령을 다시 실행한다.

## 5. JPXLpr 시험

```bat
run-jpxlpr-edge-test.bat
```

- 입차 `devicenum=401`, 출차 `devicenum=402`
- 표준 이미지 파일명
- TCP 29200 전송과 `ACK|EventId`
- `NAK|EventId|ResultCode`와 응답 EventId 검증
- 장애 시 같은 EventId 재전송
- DB의 `indeviceid=4001`, `outdeviceid=4002`

## 6. APSMain 시험

```bat
run-apsmain-edge-test.bat
```

설정은 `SITENUM=9001`, `GROUPNUM=2`, `APSNUM=201`, `EDGESERVICEURL=http://localhost:5200`을 사용한다.

1. SignalR 연결과 장비 등록
2. 출구 LPR 사건 수신
3. 차량 조회 후 LDM에 `요금 1,000원` 고정 표시
4. 표시 중 시계 전송 중지
5. 이전·홈·자동 홈에서 즉시 현재 시각 복귀
6. 카드 승인 후 결제완료
7. Kiosk 사건완료 후 완료 문구
8. `parking_session.outflag=O` 확인

## 7. DB 시간 확인

```bat
mysql -u test -p -e "USE parking000test; SELECT xindex,carnum,indate,paydate,outdate,outflag FROM parking_session ORDER BY xindex DESC LIMIT 10;"
```

시간이 한국시간이고 소수점 없이 저장되는지 확인한다.

## 8. WinForms 확인

모든 폼은 `Form.cs`, `Form.Designer.cs`, `Form.resx` 구조를 유지한다. 현재 Designer nullable 주석과 `components` 초기화 경고는 별도 정리 대상이다.

## 9. 현재 사용하면 안 되는 배치

삭제된 시뮬레이터를 참조할 수 있어 정리 전까지 실행 기준으로 사용하지 않는다.

- `run-foundations.bat`
- `run-terminal-agent-test.bat`
- `run-lpr-four-camera-test.bat`

## 10. Git과 설정 파일

`appsettings.json`, `App.config`, SQLite 실행 DB, 로그, 이미지, `bin`, `obj`는 로컬 값을 보존하고 Git에 올리지 않는다.
