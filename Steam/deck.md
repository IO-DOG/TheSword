# Steam Deck

목표는 **Playable**(MASTER_PLAN §7). 게임은 패드를 직접 읽지 않고 키보드·마우스만 받는다(`activeInputHandler: 0`, 레거시 Input).
그래서 Steam Input 공식 설정이 패드 단추를 키로 바꿔 준다. **Verified** 는 네이티브 패드 입력과 단추 그림(글리프)이 있어야 해서
출시 뒤로 미룬다. Valve 의 정의: Playable = "동작하지만 사용자가 손을 좀 써야 할 수 있다(예: 터치스크린으로 런처를 조작)".

## 조작표 (공식 키보드 설정)

| Deck | Xbox 패드 | 키 | 게임에서 |
|---|---|---|---|
| 왼쪽 스틱 · 방향 패드 | 왼쪽 스틱 · 방향 패드 | W A S D | 이동. 메뉴·도감·확인 창에서 위아래·좌우 고르기 |
| A | A | Enter | 확인, 대사 넘기기, 안내 창 닫기, 타이틀 "아무 키나", 게임오버(2초 뒤)·로딩 그림 넘기기. 누르고 있으면 크레디트 ×5 |
| B | B | Esc | 창 닫기·메뉴(안내 창도 닫힌다). 인트로는 3초 누르면 건너뛴다 |
| ☰ (Menu) | ≡ (Menu) | Esc | 메뉴 — MASTER_PLAN 표에 없는 것을 더했다. 휴대기에서 멈춤 단추를 찾는 자리라서 |
| X / Y / R1 | X / Y / RB | 1 / 2 / 3 | 전투 스킬 강타 / 철벽 / 흡혈 |
| L1 | LB | Tab | 맵: 워프 창(워프석 반지 뒤). 전투: 건너뛰기(보스 아님, 치명타 수업 뒤). 대사: 본 장면은 누르면, 처음 보는 장면은 0.6초 누르고 있으면 건너뛴다 |
| ⧉ (View) | View | M | 몬스터 도감 |
| L3 (왼쪽 스틱 누르기) | LS 누르기 | V | 맵 위 예측 숫자 켜고 끄기 |
| R2 (누르고 있기) | RT (누르고 있기) | Space | 누르는 동안 전투 8배속 |
| 오른쪽 트랙패드 | — | 마우스 | 가리키기(툴팁). **누르면 왼쪽 클릭** |
| 오른쪽 스틱 | 오른쪽 스틱 | 마우스 휠 | 카메라 줌 |
| 나머지 (L2, 왼쪽 트랙패드, L4·L5·R4·R5) | LT 등 | 비움 | |

- 이 표는 **2026-10-01 작업 트리**의 키를 옮긴 것이다(`PlayerController`, `UI_GameScene`, `UI_BattlePopup` — 가속 Space, 건너뛰기 Tab,
  `StoryUI.SkipKey`, `UI_GuidePopup`, `WarpUI`, 각 창의 키 처리). 이번 웨이브(B1·O2·O3)가 넣은 키가 섞여 있으니 통합 뒤 한 번 더 맞춰 본다.
- **Shift+Tab 은 Steam 오버레이 단축키다.** 그래서 전투 가속은 Shift 를 받지 않고 **Space 만** 받는다
  (`UI_BattlePopup.Speed`) — R2 를 Space 로 매핑한다. Space 는 대사·안내·확인 창에서 Enter 처럼 동작하지만
  누르는 순간만 치고, 누르고 있는 것은 치지 않는다.

## 공식 설정 만들기와 올리기

두 길이 있다. **지금은 1번을 쓴다** — 빌드를 건드리지 않고, 문서가 In-Game Actions 파일을 전제하지 않는 길이다(README [S14]).

**1. Workshop 파일 ID (빌드 수정 없음)**
1. Steam 을 Big Picture 로 켠다 → 라이브러리 → TheSword → 컨트롤러 설정(Controller Layout).
2. 템플릿 **Keyboard (WASD) and Mouse** 에서 시작해 위 표대로 바꾼다. Deck 에서 한 벌, Xbox 패드로 한 벌(설정은 컨트롤러 종류마다 따로다).
3. 저장: (Y) 또는 *Save As* → 제목·설명(언어별) → **PUBLIC** → *PUBLISH*. 나오는 **파일 ID** 를 적는다.
4. Steamworks → App Admin → Application → **Steam Input** → *Steam Input Template* 에서 **Custom Configuration** →
   *Add Custom Configuration* → 파일 ID 를 넣는다(여럿이면 쉼표, 첫째가 새 사용자 기본값) → 저장 → **Publish**.

**2. 게임에 싣기 (Valve 가 새 게임에 권하는 길 — 이 게임에서는 확인 전)**
Valve 문서의 순서는 In-Game Actions(IGA) 파일이 있는 게임을 전제로 한다. 이 게임은 IGA 없이 키보드 흉내만 쓰므로,
아래 매니페스트가 그 설정을 싣는지 **Deck 실기에서 먼저 확인**하고 나서 1번에서 옮긴다(README [S14]).
1. Big Picture → 설정 → 시스템 → 개발자 모드 → 개발자 → **Steam Input Layout Dev Mode** 켜기.
2. 설정 화면의 톱니 → *Export layout*. `steam://dumpcontrollerconfig?appid=<AppID>` 가 문서 폴더에 VDF 를 쓴다. 컨트롤러 종류마다 한 벌씩.
3. **Action Manifest** 파일을 만든다. 종류별로 내보낸 설정을 가리키는 목록이고, `path` 는 이 파일이 있는 폴더 기준이다.
   종류 이름은 `controller_neptune`(Deck), `controller_xboxone`, `controller_xbox360`, `controller_ps4`, `controller_ps5`, `controller_generic` 등.
   ```
   "Action Manifest"
   {
       "configurations"
       {
           "controller_neptune"  { "0" { "path" "deck.vdf" } }
           "controller_xboxone"  { "0" { "path" "xbox.vdf" } }
       }
       "actions" { }
       "localization" { }
   }
   ```
4. 매니페스트(예: `steam_input_manifest.vdf`)와 설정 파일들을 빌드 폴더에 넣고(`GameBuild.Windows` 가 복사하게 해야 한다 — CORE 레인 일)
   *Steam Input Template* = **Custom Configuration (Bundled with game)** 에 **매니페스트**의 경로를 적는다 → Publish.

같은 페이지에 컨트롤러 종류별(Xbox·PlayStation·일반 패드) Steam Input 사용 설정이 있으면 켠다. 게임이 패드를 직접 읽지 않으므로
Steam Input 이 꺼진 PC 패드는 아무것도 못 한다. Deck 의 내장 조작은 늘 Steam Input 을 거친다.

## 점검 (1280×800)

글자 9px·30fps 는 Valve 의 **Verified** 항목이다 — 어겨도 대개 Playable 로 나온다(README [S13]). 그래도 우리 기준(MASTER_PLAN 종료 기준 7)이라 출시 전에 맞춘다.

- [ ] **해상도.** Deck 은 1280×800(16:10) 이고 게임은 처음에 모니터 원래 크기, 창 없는 전체 화면으로 켠다.
      화면 구성은 16:9 로 짜여 있다(`UI_SettingPopup` 주석) — 16:10 에서 HUD 네 귀퉁이·전투 카드·팝업이 잘리지 않는지 본다. **아직 아무도 확인하지 않았다.**
- [ ] **글자 9px 이상**(Verified 기준, 권장 12px). 1280×800 스크린샷을 1:1 로 확대해 가장 작은 글자 한 자(대문자나 한글 한 글자)의 세로를 잰다.
      위험한 곳(QA 기록): 워프 창 층 이름(720p 에서 약 10px), 체크포인트 줄(1080p 에서 약 14px → 1280 폭에서 약 9~10px),
      인벤토리 숫자, 도감의 조작 안내 줄, HUD "치명까지 N타". 한국어·영어·일본어·중국어를 다 본다.
      에디터에서는 Game 뷰에 1280×800 고정 해상도를 더하고 Scale 1x 로 찍는다.
- [ ] **30fps 이상**(Verified 기준, Deck 기본 설정에서). Deck 의 ⋯ → 성능 → 성능 오버레이 1단계(FPS). 볼 곳: 챕터 첫 층(21·41·61·81층 — 20개 층을
      한꺼번에 짓는다), 보스전, 81~100층(떠오르는 티끌), 횃불이 많은 층, 결말. 에디터 실측은 평균 약 47fps, 한 층을 그릴 때
      그리기 호출 1,460~1,570, 챕터 20개 층이 한꺼번에 켜져 있어 광원 165개(r08). 30 아래면 D4: 그림자 1024·캐스케이드 2, 그다음 안 보이는 층 끄기.
- [ ] **모든 내용에 닿는가.** 위 표로 1층 → 5층까지. 트랙패드가 필요한 곳(아래)을 빼고 막히는 곳이 없어야 한다.
- [ ] **글꼴.** 일본어·중국어 화면에서 □ 가 없는지. `FontFallback` 은 없는 글자를 Windows 글꼴(맑은 고딕·Yu Gothic·Microsoft YaHei)로
      받는데 Proton 에는 그 글꼴이 없을 수 있다. 지금 대본의 글자는 원본 Silver.ttf 가 다 갖고 있다(`FontFallback.cs` 주석) — 그래도 한 번 훑는다.
- [ ] **클라우드.** PC 에서 5층까지 하고 Deck 에서 이어하기(README 4절). Windows 경로가 Proton 안에서 맞게 옮겨지는지 여기서 확인된다.
- [ ] 오프라인으로 켜지는지(비행기 모드).
- [ ] 검토 신청: 빌드 검토를 통과한 뒤 앱 랜딩 페이지 → Technical Tools → **Steam Hardware Compatibility Review**. 결과는 대개 일주일 안.
      **모든 파트너에게 열린 링크가 아니다** — 없으면 Valve 가 대기열에 올리기를 기다린다(README [S13]).

## 알려진 틈

| 무엇 | 지금 | Playable 에 괜찮은가 |
|---|---|---|
| 단추 그림 | 화면 안내가 키보드 글자다 — "WASD 이동 · M 도감 · V 예측 표시 · Tab 워프 · 1/2/3 스킬 · Esc 메뉴"(ScriptData 274), 스킬 단추의 1/2/3. 그림에 박힌 WASD 도 있다(`GuidePanel_WASD`, 1층 표지판, `LoadingIllust1`) | 괜찮다. Verified 는 안 된다 |
| 마우스로만 되는 창 | 마검 계약 "예"(`UI_MagicalSwordCheckPopup`), 보스방 확인(`UI_BossRoomCheckPopup`), 언어 고르기, 설정(해상도는 일부러 마우스만), 워프 창에서 층 고르기, 인벤토리(HUD 단추로만 열린다). 안내 창은 O2 로 Enter·Space·Esc 가 된다 | 괜찮다 — 오른쪽 트랙패드로 누른다 |
| 툴팁 | 몬스터·아이템 정보는 마우스를 올려야 뜬다 | 트랙패드로 가리킨다. 같은 값을 도감(⧉ View)이 보여 준다 |
| 터치스크린 | 지원하지 않는다. 시험하지 않았다. 터치가 마우스 클릭처럼 들어갈 수는 있지만 가리키기(툴팁)는 안 된다 | 괜찮다(필수가 아니다) |
| 글자 입력 | 이름 입력 같은 것이 없다 | 해당 없음 |
| 런처·안티치트·동영상 | 없다 | 해당 없음 |
| 그래픽 API | Direct3D 11 전용 → Deck 에서는 Proton 의 DXVK | 실기에서 확인 |
| 프레임 제한 | 게임에 fps 상한이 없다(VSync 끄면 무제한) | Deck 은 시스템 제한기로 막을 수 있다 |
