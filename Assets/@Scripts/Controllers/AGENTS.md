<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-15 | Updated: 2026-08-15 -->

# Controllers

## Purpose
씬에 배치되거나 스폰되는 액터들의 MonoBehaviour 로직. 플레이어/몬스터/보스 전투와 이동,
카메라 추적, 포탈·트리거 처리.

## Key Files
| File | Description |
|------|-------------|
| `PlayerController.cs` | 플레이어 이동·공격·피격·상호작용 입력 처리 |
| `MonsterController.cs` | 일반 몬스터 AI(추적/공격/사망). 보스의 베이스 클래스이기도 함 |
| `CameraController.cs` | 플레이어 추적 카메라 (`Define.DEFALUT_CAMERA_OFFSET` 기준) |
| `PortalController.cs` | 맵 간 이동 포탈. `Item/BossDoor`가 이를 상속 |
| `InteractObjectController.cs` | 범용 상호작용 오브젝트 감지/실행 |
| `BossEventTriggerController.cs` | 보스방 진입 트리거 → 보스전 연출 시작 |
| `BlackSlimeController.cs` | 블랙 슬라임 개별 로직 |
| `Boss/BossMonsterController.cs` | 보스 공통 추상 클래스 (`MonsterController` 상속) |
| `Boss/KingSlimeController.cs` | 킹슬라임 보스 패턴 |
| `Boss/SplitSlimeController.cs` | 분열 슬라임 보스 패턴 |
| `Boss/EnterKingSlime.cs` | 킹슬라임 보스전 진입 연출 |

## For AI Agents

### Working In This Directory
- 상속 사슬: `MonsterController` ← `BossMonsterController`(abstract) ← 개별 보스. 공통 동작 수정은 상위에서
- 스탯·데이터는 컨트롤러에 하드코딩하지 말고 `Managers.Data`의 테이블(MonsterDic 등)에서 읽는다
- 충돌 판정은 `Define.Layer`의 레이어 번호에 의존 — 레이어 변경 시 Define과 Unity 설정 동시 수정
- 스폰/제거는 `Managers.Object`와 활성화 딕셔너리(`MonsterActiveDic` 등)를 경유해야 세이브에 반영된다

### Common Patterns
- 연출(카메라 셰이크, 컷신)은 직접 구현하지 말고 `Managers.Directing` 호출

## Dependencies

### Internal
- `Managers/` 전반, `Class/CreatureClass.cs`, `Utils/Define.cs`

### External
- DOTween, Cinemachine

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
