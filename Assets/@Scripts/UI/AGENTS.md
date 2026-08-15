<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-08-15 | Updated: 2026-08-15 -->

# UI

## Purpose
enum 기반 자동 바인딩(`UI_Base.Bind`)을 쓰는 UI 계층. `UI_Popup`은 UIManager 스택으로,
`UI_Scene`은 씬당 하나로 관리된다. 텍스트는 전부 TextMeshPro.

## Key Files
| File | Description |
|------|-------------|
| `UI_Base.cs` | 추상 베이스: enum 이름→자식 GameObject 바인딩, `BindEvent`, 페이드/팝업 연출 헬퍼 |
| `UI_EventHandler.cs` | 클릭/드래그/포인터 이벤트를 Action으로 노출하는 EventSystem 핸들러 |
| `DamageFont.cs` | 데미지 숫자 플로팅 텍스트 |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `Popup/` | `UI_Popup` 파생 팝업들 (인벤토리, 메뉴, 설정, 대화, 게임오버, 보스명, 레터박스 등) |
| `Scene/` | `UI_Scene` 파생 씬 UI (`UI_TitleScene`이 PreLoad+`Data.Init()` 담당, `legacy_`·`SHJTest`는 비사용) |
| `SubItem/` | 재사용 위젯: 카드(`UI_BaseCard`←Monster/Player), 정보 패널, `UI_Fade` |
| `Toast/` | `UI_Toast` 알림 |

## For AI Agents

### Working In This Directory
- **바인딩 enum 멤버 이름은 프리팹 하이어라키의 자식 GameObject 이름과 정확히 일치해야 한다**
  (`Init()`에서 `BindButton(typeof(Buttons))` 등 호출, 실패 시 콘솔에 "Failed to bind" 로그만 남음)
- 새 팝업: `UI_Popup` 파생 클래스 + 동명 프리팹(Addressables "PreLoad" 라벨) → `Managers.UI.ShowPopupUI<T>()`
- `Init()`은 중복 호출 가드(`_init`)가 있는 lazy 초기화 — `Start()`보다 먼저 접근하면 직접 `Init()` 호출
- 노출 문자열은 `Managers.GetString(scriptId)` 사용 (하드코딩 금지)

### Common Patterns
- 버튼 이벤트: `GetButton((int)Buttons.Xxx).gameObject.BindEvent(액션)` 또는 `BindEvent(go, action, type)`
- 팝업 열림 연출: `PopupOpenAnimation(contentObject)` (DOTween 스케일)
- 화면 전환 페이드: `FadeEffect`/`Fade` → `UI_Fade` 프리팹 인스턴스화

## Dependencies

### Internal
- `Managers.UI`(스택), `Managers.Resource`(프리팹 로드), `Utils/Util.FindChild`

### External
- TextMeshPro, DOTween, uGUI

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
