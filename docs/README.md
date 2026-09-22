# ParkingSystem 문서

기준일: 2026-09-22  
기준 브랜치: `codex/server-edge-foundation`

이 디렉터리는 현재 구현 상태만 설명한다. 완료된 설계안과 작업계획은 Git 이력에서 확인하고 현재 문서에는 중복 보관하지 않는다.

## 문서 목록

1. [전체 구조](01-program-overview.md)
2. [프로그램별 기능](02-program-features.md)
3. [API 및 통신 규격](03-api-specification.md)
4. [DB와 현장 설정](04-database-configuration.md)
5. [실행 및 시험](05-operation-and-test.md)
6. [개발 현황과 다음 작업](06-development-status.md)

## 문서 관리 원칙

- 코드, DB 스키마, 현재 문서가 다르면 코드와 `database/mysql/000_full_schema.sql`을 먼저 확인한다.
- 기능 변경 시 관련 문서를 같은 커밋에서 갱신한다.
- 날짜별 인수인계 문서를 계속 추가하지 않고 `06-development-status.md`를 갱신한다.
- 완료된 구현계획, 임시 시험 절차, 삭제된 프로그램 설명은 현재 문서에서 제거한다.
- 문서는 한글을 기본으로 하고 API명, 클래스명, 설정키만 원문을 사용한다.
