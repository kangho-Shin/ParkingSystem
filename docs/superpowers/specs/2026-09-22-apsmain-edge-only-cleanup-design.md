# APSMain Edge 전용 구조 정리 설계

## 목적

APSMain_C(API)V2를 기존 ipims DB 직접 연결 및 레거시 REST API 의존 구조에서 분리하고, EdgeService만 호출하는 무인정산기 프로그램으로 정리한다.

## 확정 기준

- 운영 및 시험 DB는 parking000test 하나만 사용한다.
- DB 연결 문자열 환경변수는 PARKING_RUNTIME_CONNECTION 하나만 사용한다.
- sitenum과 groupnum을 공통 식별자로 사용한다.
- APSMain은 MySQL에 직접 연결하지 않는다.
- parking000test 접근은 Central API와 EdgeService 계층에서만 수행한다.
- APSMain은 EdgeService의 로컬 API와 SignalR만 사용한다.
- WinForms 폼은 .cs, .Designer.cs, .resx 구조를 유지한다.
- Dapper와 Newtonsoft.Json 사용 정책은 서버 계층에서 유지한다.

## 정리 대상

1. ipims 기준으로 생성된 DbModels와 UparkdbContext
2. Microsoft.EntityFrameworkCore.Tools 및 Pomelo.EntityFrameworkCore.MySql 패키지
3. 기존 DB Scaffold 명령, 접속 문자열, 비밀번호가 포함된 주석
4. 사용하지 않는 레거시 Api/Request 및 Api/Response
5. EdgeService 전환 후 사용하지 않는 DB 작업 및 기존 통신 코드
6. 중복 이미지와 사용하지 않는 리소스는 코드 참조를 확인한 후 제거

## 유지 및 이동 대상

- 화면에서 현재 사용하는 차량, 요금, 할인, 등록차량 자료형은 DB 엔터티가 아닌 일반 Models 또는 EdgeService 계약형으로 재정의한다.
- Integration/EdgeService 계약을 기준으로 화면과 결제 흐름을 연결한다.
- 카드단말기, 프린터, 음성, 접근성 UI 및 24인치/15인치 화면은 유지한다.
- 실제 사용 여부가 확인되지 않은 장치 코드는 임의로 삭제하지 않는다.

## 작업 순서

1. DbModels 형식별 외부 참조를 조사한다.
2. 필요한 형식을 Models 또는 Integration/EdgeService 계약으로 옮긴다.
3. 기존 Api 모델과 화면 참조를 EdgeService 계약으로 교체한다.
4. DbModels, UparkdbContext, EF Core와 Pomelo 참조를 제거한다.
5. 하드코딩된 예전 DB 정보와 주석을 제거한다.
6. 컴파일 오류를 기준으로 남은 레거시 의존성을 정리한다.
7. APSMain을 빌드한 뒤 전체 솔루션 빌드와 전체 테스트를 한 번에 실행한다.
8. run-jpxlpr-edge-test.bat으로 sitenum=9001, groupnum=2 실제 흐름을 확인한다.

## 완료 조건

- APSMain 프로젝트에 DbModels와 EF Core/Pomelo 의존성이 없다.
- APSMain 소스에 ipims 연결 정보와 비밀번호가 없다.
- APSMain이 DB에 직접 접속하지 않는다.
- 기존 24인치 및 15인치 WinForms 화면 구조가 유지된다.
- 전체 빌드와 테스트가 성공한다.
- JPXLPR → EdgeService → APSMain 흐름이 정상 동작한다.
