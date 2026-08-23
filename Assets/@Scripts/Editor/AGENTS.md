<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-20 | Updated: 2026-08-20 -->

# Editor

## Purpose
에디터 전용 도구. 전부 `TheSword/` 메뉴 항목이면서 동시에 **`-executeMethod`로 헤드리스 실행**된다
(Unity 에디터를 띄우지 않고 검증·생성·녹화를 돌리기 위한 것). 런타임에는 빌드되지 않는다.

## Key Files
| File | Description |
|------|-------------|
| `ContentValidator.cs` | 100층 콘텐츠가 진행 가능한 형태인지 검증 (스테이지 그래프·맵·프리팹·레벨 테이블) |
| `ProgressionTest.cs` | 1층→100층이 실제로 이어지는지 확인 |
| `MapBuilderSmokeTest.cs` | `MapBuilder`가 100층을 실제로 조립하는지 확인 |
| `MonsterArtSetup.cs` | 스프라이트 시트 → 애니메이션 클립 생성 후 맵/전투창 두 컨트롤러에 등록 |
| `AddressableSetup.cs` | `MapBuilder`가 런타임에 로드하는 프리팹을 Addressables("PreLoad" 라벨)에 등록 |
| `PlaythroughRecorder.cs` | `AutoPlayer` 봇을 돌리며 Unity Recorder로 통째로 녹화 (`DryRun`은 녹화 없이) |
| `HandFloorDump.cs` | 손수 만든 1~4층 프리팹 내용을 그대로 로그로 덤프 |

## 실행

```bash
Unity.exe -projectPath . -batchmode -quit -executeMethod ContentValidator.Validate
Unity.exe -projectPath . -executeMethod MonsterArtSetup.Build
Unity.exe -projectPath . -executeMethod PlaythroughRecorder.Record
```

## For AI Agents

### Working In This Directory
- 새 몬스터 시트를 추가하면 **클립을 손으로 만들지 말고** `MonsterArtSetup.Wanted()`에 한 줄 넣고
  `Build`를 돌린다. 잘려 있지 않은 시트는 `NeedSlicing`에 넣으면 가로로 균등 분할한다
- 떨군 장비가 재생하는 `EquipItem_{id}` 상태도 같은 도구가 채운다 —
  **없는 상태를 재생하면 경고만 찍히고 그림이 안 나온다(= 떨어진 장비가 보이지 않는다)**
- 헤드리스 검증은 Unity 에디터가 프로젝트를 점유 중이면 돌지 않는다.
  같은 검사를 Unity 없이 하려면 `Tools/validate_content.py`
- 완주 검증(`ProgressionTest`, `PlaythroughRecorder`)은 세이브를 건드린다 —
  실행 전 세이브 초기화 여부를 확인할 것(`PlaythroughRecorder.ClearSaveData`)

### Testing Requirements
- 이 폴더가 곧 테스트다. 콘텐츠/맵 관련 변경 후에는 `ContentValidator` → `MapBuilderSmokeTest` →
  `ProgressionTest` 순으로 돌린다

## Dependencies

### Internal
- `Managers/Contents/MapBuilder.cs`, `Managers/Core/DataManager.cs`, `AutoPlay/AutoPlayer.cs`

### External
- Addressables(에디터 API), Unity Recorder(`PlaythroughRecorder`만)

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
