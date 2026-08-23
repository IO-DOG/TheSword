<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-20 | Updated: 2026-08-20 -->

# Utils

## Purpose
전역 상수·enum과 정적 헬퍼. 네임스페이스 없이 어디서나 `Define.*`, `Util.*`로 쓴다.

## Key Files
| File | Description |
|------|-------------|
| `Define.cs` | 모든 enum과 상수 (`Scene`, `Layer`, `Sound`, `Trait`, `Boss`, `ObjectType`, `StageName`, `GameEvent` 등) |
| `Util.cs` | 컴포넌트/자식 조회, 색 계산, 페이드·플래시 코루틴, 좌표 변환 |
| `Extension.cs` | 확장 메서드: `GetOrAddComponent`, `BindEvent`, `IsValid`, `DestroyChilds`, `Shuffle` |
| `ChapterTheme.cs` | 챕터(20층)마다 바뀌는 배경 분위기 — 해의 각도·세기·색, 안개. `Apply()` |
| `MonsterTint.cs` | 몬스터 색 변형. 색조=챕터, 진하기=층 안 서열, 다섯 층마다 색 갈래가 바뀐다 |

## For AI Agents

### Working In This Directory
- **컴포넌트 조회는 두 갈래를 구분해서 쓴다:**

  | | 탐색 범위 | 대상 |
  |---|---|---|
  | `Util.Find<T>` | 자신 → 자식 → 조상 자신 | 몬스터·아이템처럼 **여럿인 것** |
  | `Util.FindInTile<T>` | 부모로 올라가며 그 아래 전체 | 문·레버·포탈처럼 **타일에 하나뿐인 것** |

  여럿인 것에 넓은 탐색을 쓰면 형제를 집는다 — 실제로 슬라임을 밀었는데 옆 칸 늑대와 전투가 열렸다
- `Define.Layer` 값은 Unity 레이어 설정의 실제 번호와 일치해야 한다 — 한쪽만 고치면 충돌 판정이 조용히 어긋난다
- 세이브 인덱스 상수(`Define`의 열쇠/포션 인덱스)는 프리팹에 구워진 값과 겹치면 안 된다.
  과거 3층 열쇠가 2층 포션과 인덱스를 공유해 포션을 먹으면 열쇠가 사라졌다
- 벽 아트가 챕터 00 세트뿐이라 챕터 구분은 `ChapterTheme` + `MonsterTint`가 전부 짊어진다.
  색만 바꾸면 필터처럼 보이므로 **그림자 방향과 안개를 함께** 바꾼다

### Common Patterns
- 새 enum·상수는 반드시 `Define.cs`에 모은다 (다른 파일에 흩어놓지 않는다)

## Dependencies

### Internal
- 없음 — 하위 계층. 다른 폴더가 여기를 참조하지 그 반대는 아니다

### External
- DOTween, TextMeshPro

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
