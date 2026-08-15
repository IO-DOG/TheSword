<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-15 | Updated: 2026-08-15 -->

# Assets

## Purpose
Unity 에셋 루트. **`@` 접두사 폴더(`@Scripts`, `@Resources`, `@Scenes`, `@Textures`)만 이
프로젝트의 자체 제작물**이고, 나머지는 스토어에서 받은 서드파티 에셋 팩이다 — 수정하지 않는다.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `@Scripts/` | 모든 게임 코드 (see `@Scripts/AGENTS.md`) |
| `@Resources/` | 프리팹·데이터 테이블·사운드 등 자체 에셋, Addressables로 로드 (see `@Resources/AGENTS.md`) |
| `@Scenes/` | 게임 씬. TitleScene/IntroScene/GameScene/EndingScene이 실제 사용, `legacy_`·`*Test*`·`DecoJSJ`는 작업용 |
| `@Textures/` | 자체 텍스처 |
| `AddressableAssetsData/` | Addressables 그룹/설정 (에디터에서 관리) |
| `StreamingAssets/Data/` | 던전 맵 CSV 그리드(`Excel/Dungeon_*.csv`)와 변환 산출물 `JsonData/MapData.json` |
| `Resources/` | (레거시) Unity Resources 폴더 — 신규 에셋은 넣지 말 것 |
| 그 외 (`BroAudio/`, `Plugins/`, `Retro Arsenal/`, `AssetPacks/`, `UniGLTF/` 등) | 서드파티 — 수정 금지 |

## For AI Agents

### Working In This Directory
- 새 프리팹/에셋은 Addressables에 등록하고 **"PreLoad" 라벨**을 붙여야 런타임에 로드된다
- 씬 추가 시 `Define.Scene` enum과 `BaseScene` 파생 클래스도 함께 추가
- 서드파티 폴더의 버그는 우회(래핑)하고 원본을 고치지 않는다

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
