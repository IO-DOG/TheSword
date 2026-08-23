<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-15 | Updated: 2026-08-20 -->

# @Resources

## Purpose
자체 제작 에셋 루트. 이름과 달리 Unity `Resources` 폴더가 아니라 **Addressables로 로드**된다
(타이틀 씬에서 "PreLoad" 라벨 일괄 선로드 → `Managers.Resource.Load(key)`).

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Data/Excel/` | 데이터 테이블 원본 CSV (PlayerData, MonsterData, ScriptData, EquipData 등) |
| `Data/JsonData/` | CSV에서 변환된 JSON — `DataManager.Init()`이 실제로 로드하는 파일 |
| `Prefabs/` | 게임 오브젝트/UI 프리팹 (키 = 프리팹 이름) |
| `Animations/` | 애니메이션 클립·컨트롤러 (보스 연출, 포탈, 이모지 등) |
| `Maps/` | 맵 관련 에셋 |
| `Sounds/` | BGM/효과음 |
| `Sprites/`, `Textures/`, `Materials/`, `Shaders/`, `Lights/`, `Paticles/`, `Font/` | 각 유형별 에셋 |

## For AI Agents

### Working In This Directory
- 에셋 추가만으로는 로드되지 않는다 — **Addressables 그룹 등록 + "PreLoad" 라벨** 필수
- 스프라이트의 Addressables 키는 `이름.sprite` 형식 (`ResourceManager`가 접미사를 자동 보정 시도)
- 데이터 수정 시 CSV(원본)와 JsonData(런타임 로드 대상) **둘 다** 갱신해야 한다 — 런타임은 JSON만 읽는다
- **PlayerData/MonsterData/StageInfoData/ScriptData는 `Tools/`의 생성기가 덮어쓴다.**
  손으로 고치면 다음 `generate_content.py --write`에 지워진다 — 생성기 쪽을 고칠 것
- 몬스터 애니메이션 클립은 손으로 만들지 말고 `Editor/MonsterArtSetup.Build`로 생성한다
  (`Assets/@Scripts/Editor/AGENTS.md` 참조)
- 던전 맵 그리드 CSV는 여기가 아니라 `Assets/StreamingAssets/Data/Excel/`에 있음 (셀 코드는 루트 `CLAUDE.md` 참조)
- `ScriptData`(로컬라이즈 시트) 이스케이프: `\n`=줄바꿈, `^`=쉼표

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
