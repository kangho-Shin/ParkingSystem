# Parking.Operator 업무 흐름 설계

## 목표

`Parking.Operator`가 중앙 DB나 Parking.Api에 직접 접속하지 않고 `Parking.EdgeService`만 사용해 유인 정산의 기본 업무를 수행한다.

## 구현 범위

- 전체 차량번호와 뒤 4자리 검색 및 복수 후보 선택
- 선택 차량의 입차사진 표시
- 할인키를 포함한 선택 차량 요금 재계산
- 카드·현금 결제 결과 저장 기반
- 미출차 차량번호 정정
- 설정된 입·출차 차로와 장비를 이용한 수동 입차·출차
- 출구 장비에 연결된 LDM을 통한 차단기 수동 개방

## 통신 구조

`Parking.Operator → Parking.EdgeService → Parking.EdgeGateway → Parking.Api → MySQL` 흐름을 유지한다. 차단기 수동 개방과 이미지 다운로드만 현장 EdgeService 및 이미지 서버에서 처리한다.

## 안전 규칙

- 차량번호 정정은 `outflag<>'O'`인 행만 허용한다.
- 요금 계산 전 차량 선택을 요구한다.
- 결제 완료는 기존 PaymentId 멱등 API를 사용한다.
- 수동 입·출차도 새 EventId를 사용하며 기존 장비·방향 검증을 통과해야 한다.
- 실제 카드단말 승인은 이 단계에서 제외하고 승인번호 입력 기반을 사용한다.
