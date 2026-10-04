# TheSword Steam 출시 키트

회사 **CozyByte** · 제품 **TheSword** (로고는 "THE SWORD", 한국어로 "더 소드" — `Sprites/TitleImages/더 소드 화이트 수정본.png`) · 1.0.0
저장 폴더 `%USERPROFILE%\AppData\LocalLow\CozyByte\TheSword` · 설정은 레지스트리 `HKCU\Software\CozyByte\TheSword`

위에서부터 순서대로 하면 출시까지 간다. `[S…]` 는 맨 아래 출처(Steamworks 문서, 2026-09-30 확인)다.

| 파일 | 무엇 |
|---|---|
| `README.md` | 이 문서 |
| `achievements.csv` · `achievements.md` | 업적 20개(모두 연결) — 다섯 언어 이름·설명, 숨김, 아이콘 파일 이름 |
| `leaderboards.md` | 순위표 2개(보통·탑의 법) — 이름, 정렬, 점수의 자세한 값, 콘텐츠 버전마다 `_V` 올리기 |
| `stats.md` | 통계 4개 |
| `rich_presence/*.vdf` | 친구 목록에 "이끼 낀 지하 묘소 12층"(탑의 법이면 "탑의 법 · …")을 띄우는 언어별 파일 5개 |
| `scripts/` | SteamPipe 업로드 — VDF 템플릿(본편·데모), `upload.ps1`, 채우는 법 |
| `store/*.md` | 스토어 문구 — 한국어·영어·간체·번체·일본어 |
| `deck.md` | Steam Deck 조작표, 글자 크기·fps 점검, 알려진 틈 |
| `check_steam.py` | 이 폴더가 게임 데이터·코드와 맞는지 본다: `python Steam/check_steam.py` |

## 0. 먼저 정할 것

- [ ] **D2 가격** — 8절.
- [ ] **스토어 이름과 공식 부제.** Steam 에 "The Sword Game"(app 3784730)이 이미 있고, "The Sword" 로 찾으면 다른 게임이 먼저 뜬다(r04).
      Coming Soon 전에 정한다. 캡슐에는 게임 이름과 공식 부제 말고는 글을 넣을 수 없다 [S15].
      언어별 이름(한국어 "더 소드" 등)은 Steamworks 에서 따로 넣을 수 있고, 그 언어 사용자의 검색 제안에 쓰인다 [S19].
- [ ] **D3** 번체 중국어를 데모에 넣을지(번역가에 달렸다). 번체는 아직 게임에 없다 — `store/zh-TW.md` 머리말.
- [ ] **K0** `ProjectSettings.asset` 의 `companyName: CozyByte` 를 커밋한다(지금 작업 트리에만 있다). 저장 폴더·클라우드 경로가 이 이름에 달렸다 —
      공개 빌드가 나간 뒤에 바꾸면 모든 세이브가 고아가 된다(CLAUDE.md).

## 1. 계정과 앱

- [ ] Steamworks 가입(https://partner.steamgames.com). 법적 이름은 별명이 아니라 실명·법인명이고, 은행 예금주와 같아야 한다.
      세금 정보 확인에 10~15 영업일, 추가 서류를 요구할 수 있다 [S1].
- [ ] **앱 크레딧 $100**(앱 하나마다). 조정 총수익 $1,000 을 넘으면 돌려받는다 [S2]. **낸 날부터 21일이 지나야 출시할 수 있다** [S1] — 일찍 낸다.
- [ ] 본편 앱: *Create new app* → **AppID** 를 적어 둔다.
- [ ] 데모 앱: 본편 앱 랜딩 페이지 → *All associated packages, DLC, demos and tools* → **Add Demo** [S3]. AppID 가 따로 나온다.
      ('Create new app' 이 아니어서 크레딧을 쓰지 않는 것으로 알려져 있지만 공식 문서에 명시는 없다 — 만들 때 확인.)
- [ ] 코드: `Assets/@Scripts/Managers/Core/SteamManager.cs` 의 **`GameAppId`**(본편)·**`DemoAppId`**(데모) 상수(지금 둘 다 0)에 적는다.
      빌드가 쓰는 번호(`AppId`)는 DEMO 정의가 고른다. 0 이 아니면 릴리스 빌드를 Steam 밖에서 켰을 때 Steam 을 거쳐 다시 켜진다(`RestartAppIfNecessary`).
      `GameAppId` 는 데모 끝 카드의 **찜하기** 단추도 쓴다 — 0 이면 단추가 없다(2.2). 둘이 같으면 `check_steam.py` 가 FAIL.
- [ ] 저장소 루트 `steam_appid.txt`(지금 **480** = Valve 테스트 앱 Spacewar, Steamworks.NET 이 처음 깔릴 때 적었다)를 본편 AppID 로 바꾼다.
      에디터·로컬 시험 전용이다. 배포 depot 은 이 파일을 뺀다(`scripts/depot_build_*.vdf`).
- [ ] **Depot**: App Admin → SteamPipe → Depots. 본편 하나, 데모 하나. OS Windows.
- [ ] **설치**: App Admin → Installation → General Installation. 설치 폴더 `TheSword`, Launch Option: 실행 파일 `TheSword.exe`, OS Windows, 64-bit.
- [ ] **브랜치**: SteamPipe → Builds → 새 브랜치 **`beta`**(비밀번호를 건다). 업로드는 여기에 켜진다.
- [ ] 빌드 계정 권한 *Edit App Metadata*, *Publish App Changes To Steam*. 출시된 앱에 빌드를 켜려면 그 계정에 휴대폰 번호나 Steam 모바일 인증기가
      있어야 하고, 보안 정보를 바꾸면 3일 동안 켜지 못한다 [S4].
- [ ] App Admin 에서 바꾼 것은 **Publish** 탭에서 올려야 반영된다.

## 2. 빌드와 업로드

에디터가 프로젝트를 잠그므로 **에디터를 닫고** 저장소 루트에서:

```bash
Unity.exe -quit -batchmode -nographics -projectPath . -executeMethod GameBuild.Prepare
Unity.exe -quit -batchmode -nographics -projectPath . -executeMethod GameBuild.Windows
```

- 두 번으로 나누는 까닭: `Prepare` 의 `AddressableSetup.RegisterRuntimePrefabs` 는 배치 모드에서 끝나면 에디터를 끈다. 같은 실행에 이어 적은 빌드는
  **오류 없이 안 돈다** — 로그에 "등록 6건", 종료 코드 0 이라 성공처럼 보인다(CLAUDE.md).
- `Windows` 는 `Tools/validate_content.py` 가 통과해야 굽고, 출력 폴더를 비운 뒤 LZ4HC 로 굽고, `*_DoNotShip` 폴더를 `Build/Symbols/<버전>/` 으로 옮기고,
  개발 빌드가 아니면 `steam_appid.txt` 를 지운다(2026-09-30 작업 트리의 `GameBuild`).

- [ ] `Build/Windows/TheSword_Data/StreamingAssets/aa` 가 있다. 없으면 실행 파일은 타이틀에서 한 발짝도 못 나간다.
- [ ] 로컬 스모크: `Build\Windows` 에 `steam_appid.txt`(본편 AppID)를 **잠깐** 두고 `TheSword.exe` → 타이틀 → 새 게임 → 1층. 끝나면 지운다.
      (없으면 AppID 가 박힌 릴리스 빌드는 Steam 으로 다시 켜진다 — 정상이다.)
- [ ] 처음 한 번: `scripts/README.md` 대로 AppID·DepotID 를 채운다.
- [ ] steamcmd 를 받아(https://developer.valvesoftware.com/wiki/SteamCMD 또는 Steamworks SDK 의 `tools\ContentBuilder\builder`) `Steam\steamcmd\steamcmd.exe` 에 둔다.
      이 폴더는 .gitignore 다 — 로그인 토큰이 여기에 남는다.
- [ ] 미리보기: `powershell -ExecutionPolicy Bypass -File Steam\scripts\upload.ps1 -Preview` → `Steam\output` 의 파일 목록에 `.pdb`·`steam_appid.txt` 가 없다.
- [ ] 올리기: `powershell -ExecutionPolicy Bypass -File Steam\scripts\upload.ps1` (계정은 `$env:STEAM_USER` 또는 입력. 처음엔 비밀번호와 Steam Guard 코드를 묻는다).
      빌드와 aa 폴더를 먼저 확인하고, 자리표시가 남았으면 멈추고, 끝나면 BuildID 와 빌드 페이지 주소를 찍는다.
- [ ] Steam 클라이언트: 라이브러리 → TheSword → 속성 → 베타 → `beta`(비밀번호) → 받아서 2.1 을 한다.
- [ ] 통과하면 App Admin → SteamPipe → Builds 에서 그 빌드를 **default** 에 켠다. 스크립트의 `SetLive` 로는 default 를 켤 수 없다 [S4].
      빌드 검토 전에 default 에 거의 최종 빌드가 있어야 한다 [S7].
- 데모: 2.2 대로 `Build\WindowsDemo` 를 굽고 같은 순서에 `-Demo`. 본편을 데모 앱에 올리면 본편 전체가 무료로 풀린다 — 그래서 데모는 폴더가 따로다:
  `Build\WindowsDemo` 는 DEMO 정의로 굽는 `GameBuild.WindowsDemo` 만 쓰고, `-Demo` 는 그 폴더만 올린다(`app_build_DEMO_APPID.vdf` 의 ContentRoot, `check_steam.py` 가 본다).

### 2.1 beta 스모크 테스트

- [ ] Steam 에서 실행 → 타이틀. Shift+Tab 으로 오버레이가 뜬다(오버레이는 Steam 을 거쳐 켰을 때만 붙는다).
      맵에서 Shift+Tab 을 눌렀을 때 게임의 Tab(워프 창, B1 뒤로는 건너뛰기)이 같이 먹는지 본다 — 먹으면 알려진 겹침이다(r04 §2.8, `deck.md`).
- [ ] 새 게임 → 3층 계약 → `ACH_CONTRACT` 알림. 4층 킹 슬라임 → `ACH_KINGSLIME`.
- [ ] 5층에 들어서면 친구 목록에 "이끼 낀 지하 묘소 5층"(`https://steamcommunity.com/dev/testrichpresence` 로도 본다) [S11].
- [ ] 끄고 다시 켜서 이어하기. 한 번 지고 이어하기.
- [ ] 처음 켤 때의 언어: 레지스트리 `HKCU\Software\CozyByte\TheSword` 에서 `SET_LANGUAGE` 로 시작하는 값을 지우고, Steam 의 게임 언어(게임 속성 → 일반 → 언어)를
      바꿔 켜면 그 언어로 뜬다(`SteamManager.DefaultLanguage`).
- [ ] 클라우드(4절). Deck(`deck.md`).

### 2.2 체험판 (MASTER_PLAN D1)

같은 콘텐츠(지도·몬스터 표·이야기)에 **DEMO 정의**만 얹은 빌드다. 1단계는 본편과 같고, 2단계만 바꾼다:

```bash
Unity.exe -quit -batchmode -nographics -projectPath . -executeMethod GameBuild.Prepare
Unity.exe -quit -batchmode -nographics -projectPath . -executeMethod GameBuild.WindowsDemo    # -> Build/WindowsDemo
```

- 에디터 메뉴로는 **TheSword → Build Windows Demo Player**.
- DEMO 는 그 빌드의 스크립트 컴파일에만 붙는다(`BuildPlayerOptions.extraScriptingDefines`). 플레이어 설정의 정의 목록은 건드리지 않아서
  되돌릴 것이 없다 — 에디터와 본편 빌드에는 DEMO 가 없다. 기호 폴더는 `Build/Symbols/<버전>-demo/` 로 따로 간다.
- AppID: `SteamManager.DemoAppId`(데모 빌드가 Steam 을 거쳐 다시 켜질 때), `SteamManager.GameAppId`(찜하기가 여는 본편 스토어). 1절.

**체험판에 든 것**
- 1~20층 전부 — 숲(1~4층, 계약·킹 슬라임), 챕터 0 이끼 낀 지하 묘소(5~20층), 20층 보스와 렌의 반지(워프석)까지. 본편과 같은 판이다.
- 20층에서 계단을 올라 **21층에 서면 끝 카드**(`UI_DemoEndPopup`, `DemoGate`): "체험판은 여기까지", 이번 판의 마검의 장부(예언·싸움에서
  치른 값 — 스킬 없이 싸웠으면 둘이 같다. 제단에 바쳤으면 셋째 숫자 "제단" 으로 따로),
  **찜 목록에 추가**(Steam 오버레이로 본편 스토어, Steam 이 없으면 브라우저, `GameAppId` 가 0 이면 단추 없음), **타이틀로**.
  21층 체크포인트를 이어 해도 같은 카드가 뜬다. Esc·메뉴·워프·도감은 막힌다. 봇(`IsAutoPlaying`)은 막지 않는다.
- 업적·통계·순위표·Rich Presence 는 **하나도 올리지 않는다**(`SteamHooks.Active`). 데모 앱에는 그것들을 만들지 않는다.
- **저장은 본편이 잇는다 — 지도가 같은 동안만.** 체험판도 `LocalLow\CozyByte\TheSword` 에 쓴다(회사·제품 이름이 같다). 계단으로 오면 끝 카드는
  `EnterFloor` 바로 앞의 HUD 갱신에서 뜨고, 21층 입구 체크포인트는 같은 호출 안에서 그 뒤에 적힌다.
  같은 PC 에서 본편을 켜고 이어하기를 누르면 21층에서 계속된다 — **본편의 MapData 해시가 체험판과 같을 때만**이다. `ValidateCheckpoint` 는 해시가
  다른 체크포인트를 거절한다(지우지는 않는다). 체험판을 낸 뒤 본편의 지도를 한 칸이라도 고치면 모든 체험판 저장이 막히므로 **K1(옛 MapData 를
  해시별로 실어 맞는 것을 읽는다)이 체험판 출시를 막는 일이다** — 아니면 본편 지도를 체험판 것으로 얼린다. 그래서 끝 카드는 잇는다고 약속하지
  않고 "저장은 21층 입구에 남아 있습니다" 라고만 한다(`Tools/ui_text_parts/steam2.py` 561, K1 이 들어오면 되돌린다). 이을 수 있으면 챕터 1 을 여는 이야기가 뜬다
  (체험판은 그 장면을 틀기 전에 걷는다). 체험판에서 한 계약·잡은 보스(킹 슬라임·20층)·챕터 0 의 ★★★ 는 본편이 그 저장으로 층을 옮길 때 업적으로 풀린다.
  다른 PC 로는 본편 출시 뒤 클라우드를 이을 때부터 넘어간다(4절).
- 같은 폴더를 쓰니 본편을 하던 사람이 체험판에서 **새 게임**을 누르면 본편 저장이 지워진다 — 새 게임 확인 창("지금 저장이 지워집니다")이 막는 전부다.
- 시험: 체험판 빌드로 새 게임 → 20층 보스 → 반지 → 계단 → 끝 카드. 장부 숫자(예언·치름, 제단을 샀으면 제단)가 뜨는지(스킬 없이 싸웠으면
  예언 = 치름), 찜하기가 스토어를 여는지,
  타이틀로 → 이어하기 → 다시 카드. 그다음 **본편 빌드**로 이어하기 → 21층에서 챕터 1 장면과 함께 계속된다.

## 3. 일정

| 언제 | 할 일 |
|---|---|
| 지금 (W0) | 가입·크레딧(21일 시계가 돈다), K0 커밋, 이름·부제 |
| Coming Soon 올리기 7 영업일 전 | 스토어 페이지 **Mark As Ready For Review**. 검토는 보통 3~5 영업일 [S5]. 통과하면 *Post as Coming Soon* [S6] |
| 출시 **2주 전까지** | Coming Soon 이 떠 있어야 한다 [S6]. 그때부터 찜(위시리스트)이 쌓인다 |
| 2027-01-10 23:59 PST | **Next Fest 2월 등록 마감** [S8] |
| 2027-01-18 | 공식 Next Fest 트레일러용 영상 수집 |
| 2027-01-25 | 데모 빌드·스토어 페이지를 검토에 **제출** — Press Preview 시작부터 데모를 띄우려면. 데모에 따로 스토어 페이지가 없어도 스토어 페이지 검토는 필요하다 [S8] |
| 2027-02-08 | 필요한 항목을 전부 검토에 올린다 |
| 2027-02-11 10:00 PST | Press Preview 시작 |
| 2027-02-22 10:00 ~ 03-01 10:00 PST | **Next Fest.** 데모는 22일 10시 전에 공개 |
| 2027-03-01 이후 | 출시 가능(축제 참가작은 축제가 끝난 뒤 출시해야 한다) |
| 출시 7 영업일 전 | 빌드 검토 제출. 보통 3~5 영업일 [S5]. 출시 단추는 저절로 눌리지 않는다 [S7] |

- Next Fest 조건 [S8]: 스토어 페이지가 **공개**돼 있어야 하고, 공개 데모가 축제 시작 전에 있어야 하고, **한 게임은 한 번만** 나간다.
  참가하는 앱이 다른 게임의 프롤로그·1장 같은 "데모판" 이어서는 안 된다 — 본편 TheSword 는 해당하지 않는다.
- 데모를 처음 띄운 뒤 **2주 안에 한 번** 찜한 사람들에게 알림을 보낼 수 있다 [S3].
- MASTER_PLAN 의 웨이브(W3 데모 1/10 등록·1/25 빌드, W6 5월 출시)와 맞춘 일정이다.

## 4. Steam Cloud (Auto-Cloud — 코드 없음)

App Admin → Application → **Steam Cloud** [S9].

- [ ] Byte quota per user **10,000,000**(10 MB), Number of files allowed per user **30**. 실제로는 많아야 14개, 약 0.5 MB 다
      (체크포인트 한 벌 약 37 KB × 12 + `StorySeen.json` + `Records.json`, 2026-09-30 세이브 폴더 실측).
- [ ] Root Paths 에 다섯 줄. 모두 Root **`WinAppDataLocalLow`**, Subdirectory **`CozyByte/TheSword`**, OS **Windows**, **Recursive 끔**:

| Pattern | 무엇 | 코드 |
|---|---|---|
| `Checkpoint.json` | 층 입구 체크포인트 | `SaveStore.FileName` |
| `Checkpoint.json.bak` | 바로 앞 체크포인트(깨졌을 때 대신) | `SaveStore.Write` |
| `Floor_*.json` | 층별 사본, 최근 10개 | `SaveStore.WriteHistory` |
| `StorySeen.json` | 이번 판에 본 장면 | `StoryDirector` |
| `Records.json` | 판을 넘어 남는 기록(본 결말·장면·통계). 새 게임이 지우지 않는다 | `Records` |

- 올리지 **않는** 것: `Player.log`·`Player-prev.log`(같은 폴더에 있다 — 그래서 `*` 가 아니라 이름을 적는다), 쓰는 중에 잠깐 생기는 `*.tmp`,
  레지스트리의 설정(해상도·소리·언어). Valve 도 기기마다 다른 설정은 올리지 말라고 한다 [S9].
- **데모는 같은 폴더에 쓴다**: 같은 PC 에서는 클라우드 없이도 본편이 데모 세이브를 이어받는다(둘 다 `LocalLow\CozyByte\TheSword`,
  콘텐츠도 같다 — D1). 데모 빌드의 회사·제품 이름(`CozyByte` / `TheSword`)을 바꾸면 폴더가 달라져 세이브가 안 넘어간다.
- **데모와 본편의 클라우드는 본편 출시 뒤에 잇는다.** 출시되지 않은 본편을 *Shared cloud APP ID* 로 가리키면 파일이 동기화되지 않고
  Valve 가 권하지 않는 설정이다. 데모 문서도 세이브 공유는 본편이 이미 출시돼 있어야 된다고 적는다 [S9][S3].
  데모는 본편 출시 전(Next Fest)에 나가므로 그동안 데모 앱의 Shared cloud APP ID 는 **0**, 클라우드 설정은 비워 둔다.
  **본편 출시일에** 데모 앱 Steam Cloud 에 위와 같은 쿼터·다섯 줄을 넣고 Shared cloud APP ID = 본편 AppID 로 Publish 한다.
- Deck(Proton)에서 Windows 경로가 맞게 옮겨지는지는 문서에서 확인하지 못했다 — 실기에서 시험한다(`deck.md`).

**두 PC 시험**
1. PC A 에서 Steam 콘솔(`steam://open/console`) → `testappcloudpaths <AppID>` — 위 패턴에 걸리는 파일이 나온다.
   자세한 로그는 `set_spew_level 4 4`. 시험이 끝나면 `testappcloudpaths 0` 과 `set_spew_level 0 0` 을 넣는다 [S9].
2. PC A 에서 6층까지 하고 끈다. 라이브러리에서 클라우드 상태가 "최신" 이 될 때까지 기다린다.
3. PC B 에 받아 켠다 → 이어하기가 6층 입구, 타이틀의 결말 수(`Records`)가 같다. **메뉴 → 체크포인트 목록의 순서**도 본다
   (층별 사본은 파일 수정 시각으로 줄을 세운다 — 내려받은 파일의 시각이 달라지면 순서가 바뀐다).
4. 두 PC 를 오프라인으로 각각 진행한 뒤 온라인 → 충돌 창이 뜨고 고른 쪽이 남는다.
5. **본편 출시 뒤**(데모의 Shared cloud APP ID 를 바꾼 다음): PC A 의 데모에서 5층까지 → PC B 의 본편에서 이어하기. 출시 전에는 동기화되지 않는다 [S9].
   같은 PC 에서 데모 → 본편 이어하기는 출시 전에도 된다(같은 폴더).

## 5. 업적·통계·Rich Presence

- [ ] **통계 먼저**(업적의 진행 통계가 통계를 가리킨다): `stats.md` 의 표대로 넷 → Publish.
- [ ] **업적**: `achievements.md` 순서대로 20개. 아이콘 256×256 × 2장씩(아직 없다 — 임시 아이콘으로 먼저 넣어도 된다) → Publish.
- [ ] **순위표**: `leaderboards.md` 대로 둘(오름차순·숫자, Trusted 끔, Community Name) → Publish.
- [ ] **Rich Presence**: App Admin → Community → Rich Presence 에서 `rich_presence/` 의 다섯 파일을 하나씩 올린다 → Publish.
      언어별 파일(`"lang" { "Language" … "Tokens" { … } }`)도, 여러 언어를 한 파일에 넣어 한 번에 올리는 것도 문서에 있다 — 올린 파일에 든 언어만
      덮어쓴다 [S11]. 번역을 고친 언어만 다시 올리기 쉽게 나눠 두었다. 언어 이름은 `english`·`koreana`·`schinese`·`tchinese`·`japanese` [S12].
- 게임이 보내는 키(`SteamHooks`): `steam_display`, `floor` = 1~100, `chapter` = 0~4, `mode` = `normal` · `tower`(`PlayerData.Mode`).
  - `#S_Floor` → "{챕터 이름} {층}층" (5~100층). 게임 화면의 층 이름(ScriptData 5105~5200)과 같은 모양이다.
  - `#S_Forest` → 1~4층(마물의 숲·잊혀진 숲·숲의 유적, ScriptData 5000~5003) 이름.
  - 탑의 법이면 `#S_FloorTower` · `#S_ForestTower` — 앞에 규칙 이름(ScriptData 533, 새 게임의 규칙 고르기와 같은 글)과 " · " 를 붙인 것.
    `mode` 는 토큰이 쓰지 않고 따로 보낸다 — 규칙마다 토큰을 따로 둔 까닭은 아래 줄.
  - `steam_display` 가 가리키는 토큰이 없으면 Rich Presence 가 아예 안 보인다 [S11]. 빈 문자열 토큰(`{#M_%mode%}` 에 보통은 "")은 쓰지 않았다(되는지 문서에 없다).
- [ ] `python Steam/check_steam.py` — 이름이 코드·게임 데이터와 어긋나면 FAIL.
- 봇 실행과 데모는 아무것도 올리지 않는다(`SteamHooks.Active`).

## 6. Steam Input 과 Deck

`deck.md` 가 전부다. 요약:
- [ ] 공식 키보드 설정(조작표)을 만들어 Workshop 파일 ID 로 올린다 → App Admin → Steam Input → *Custom Configuration* → Publish [S14].
- [ ] Deck 에서 1~5층 막힘 없음. 그리고 우리 목표(MASTER_PLAN 종료 기준 7): 1280×800 에서 글자 9px 이상, 30fps 이상.
      이 둘은 Valve 의 **Verified** 항목이다 — 어겨도 대개 Playable 로 나오지만, 우리 기준이라 출시 전에 맞춘다 [S13].
- [ ] 검토 신청: 빌드 검토를 통과한 뒤 앱 랜딩 페이지 → Technical Tools → *Steam Hardware Compatibility Review*. 목표는 **Playable**, Verified 는 출시 뒤.
      **이 링크는 모든 파트너에게 열려 있지 않다.** 없으면 Valve 가 검토 대기열에 올리기를 기다린다(Valve 가 고르거나, 출시 뒤 자동 기준에 걸릴 때) [S13].

## 7. 스토어 페이지

**문구**: `store/` 의 언어별 파일. 짧은 설명(300자 이하), 게임 정보(Steam 서식 태그), 시스템 요구 사항, AI 공개 문단이 들어 있다.
간체를 먼저 다듬는다 — 魔塔 장르 리뷰의 84% 가 간체다(MASTER_PLAN §1).

**지원 언어** (Store Page → Supported Languages, *Interface* 와 *Subtitles*, 음성은 없으니 *Full Audio* 는 끈다):
한국어 · 영어 · 일본어 · 간체 중국어. **번체는 게임에 들어간 뒤에만**(LOC1).

**그림** [S15]

| 무엇 | 크기 | 비고 |
|---|---|---|
| Header capsule | 920×430 | 로고가 읽혀야 한다 |
| Small capsule | 462×174 | 로고가 거의 꽉 차게 |
| Main capsule | 1232×706 | |
| Vertical capsule | 748×896 | 세일 페이지 |
| Page background | 1438×810 | 선택. 없으면 마지막 스크린샷으로 만든다 |
| Screenshots | 1920×1080 이상, 16:9, **5장 이상** | **게임 화면만**(콘셉트 아트·컷신 스틸 금지). 4장 이상은 "모든 연령" 표시 |
| Library capsule | 600×900 | |
| Library header | 920×430 | |
| Library hero | 3840×1240 | **글자 금지**, 안전 영역 860×380 |
| Library logo | PNG 투명, 폭 1280 또는 높이 720 | 로고 글자만 |
| Shortcut icon | 256×256 또는 512×512, ICO·PNG | |
| App icon | 184×184 JPG | |
| Event cover / header | 800×450 / 1920×622 | 이벤트·공지 |
| Trailer | 첫 5초: "−31" 표시 → 싸움 → 정확히 −31 → 레벨 업 → 모든 값이 줄어든다 (MASTER_PLAN §7) | |

- 캡슐에는 게임 이름과 공식 부제 말고 글을 넣지 않는다(평점·수상·"지금 할인" 금지) [S15].
- 그림 속 글자는 언어마다 바꿔 올려야 한다 [S16]. 한국어·영어·간체·일본어 로고(MASTER_PLAN §8 "logos in 5 languages").
- 캡슐·키 아트·로고는 **생성하지 않는다**(MASTER_PLAN §8). 새 아트가 온 뒤 만든다 — 캡슐이 플레이어가 처음 보는 것이다.

**태그**: `store/ko.md` 의 17개. **Roguelike·Roguelite 금지**(층이 고정이고 무작위가 없다). 최대 20, 출시 전 최소 5 [S17].

**가격 정책**: 한 가격. DLC 없음, 게임 안 결제 없음(MASTER_PLAN §7). 魔塔 장르의 나쁜 리뷰는 과금 이야기가 4.8배 많다(§1).

**Content Survey** — 스토어·빌드 검토 **전에** 채운다 [S18].
- [ ] 폭력·성·약물 등 문항: `store/ko.md` 의 "콘텐츠 설명 메모" 에 사실을 적어 두었다(마물과의 판타지 전투, 플레이어가 쓰러질 때 붉은 입자, 절단·시체 없음).
- [ ] **AI 공개**: *Pre-Generated* 에 체크하고 `store/*.md` 의 "AI 사용 공개" 문단을 적는다. Valve 의 Pre-Generated 는 서사·현지화를 포함해
      플레이어가 보는 콘텐츠 가운데 AI 도움을 받은 전부다 [S18]. 이 게임에서는 이야기 대본(`Tools/story/`), 몬스터·보스·특성 이름과 설명
      (`Tools/bestiary.py`, 대본의 `bestiary`), 안내·도움말·UI 문구(`Tools/ui_text.py`, `Tools/ui_text_parts/*.py`), 그리고 모든 번역 —
      이 파일들을 바꾼 커밋(`59080e52` `22835e4f` `7f7362d4` `267880e9` `48466782`)에 Claude 공동 저자 표시가 있다.
      새 그림·음악이 오면 파일별 출처 기록을 보고 AI 로 만든 것을 더한다.
      개발 도구의 효율 향상(코드 도우미)은 이 항목의 관심사가 아니다 [S18].
- [ ] **독일**: 2024-11-15 부터 등급이 없는 게임은 독일에서 보이지 않는다. 설문을 채워야 Valve 가 등급을 준다 [S18].
- [ ] **한국 등급(GRAC)**: Steam 은 한국 자체등급 사업자가 아니어서 직접 받아야 한다고 조사됐다(r04, 게임메카 2025 보도 — 직접 확인하지 않았다).
      소규모 개발사는 수수료 감면이 있다고 한다. 신청 전에 게임물관리위원회에서 확인한다.

## 8. 가격 (D2 — 소유자 결정)

같은 장르의 Steam 가격(Steam 스토어, 2026-09-30 수집):

| 게임 | 미국 가격 | 리뷰 |
|---|---|---|
| 绯红编年史 ~ Chronicle of Scarlet ~ | $12.99 | 705/788 매우 긍정적 |
| The Dungeon of Lulu Farea | $12.99 | 173/192 매우 긍정적 |
| Requiem of Reuinis | $11.99 | 358/394 매우 긍정적 |
| DungeonUp | $4.99 | 419/524 대체로 긍정적 |
| Soulestination | $2.99 | 572/622 매우 긍정적 |
| 魔塔地牢 | $2.99 | 174/247 대체로 긍정적 |
| Magic Tower 3D | $2.99 | 4/16 대체로 부정적 — 건너뛸 수 없는 전투가 원인(§1) |
| Desktop Dungeons (결정적 퍼즐 RPG) | $14.99 | 1976/2364 매우 긍정적 |

- 魔塔 장르 게임은 **$2.99~12.99**, 결정적 퍼즐 RPG 비교작 Desktop Dungeons 는 $14.99. 제안은 **$7.99~9.99**(MASTER_PLAN D2, 추론).
- 지역 가격은 Steamworks 가격 페이지의 권장 가격을 쓴다.
- 이 표는 그날의 가격이다. 정하기 전에 다시 본다.

## 9. 출시 체크리스트

- [ ] K0 `CozyByte` 커밋, `SteamManager.GameAppId`·`DemoAppId`, `steam_appid.txt` 에 본편 AppID
- [ ] 세이브가 패치를 견딘다(K1 — 층 배치를 고친 패치가 옛 세이브를 거부하지 않는다). 안 되면 출시 뒤 첫 패치가 모든 진행을 막는다
- [ ] 크레딧·고지: Silver 글꼴(Poppy Works, **CC BY 4.0 — 표기 의무**), DNF BitBit(표기 권장), MIT 고지(UIEffect, ParticleEffectForUGUI, **Steamworks.NET**)를
      크레딧이나 게임 폴더의 고지 파일로 싣는다(r07 — 지금 크레딧은 6줄뿐이다)
- [ ] 버전 번호가 타이틀이나 메뉴에 보인다(r07)
- [ ] 통계 4 → 업적 20 → 순위표 2 → Rich Presence 5 → Publish, `check_steam.py` 통과, 봇 한 판 뒤 풀린 업적 0·순위표 기록 0
- [ ] Steam Cloud 5줄, 두 PC 시험 1~4. 데모의 Shared cloud APP ID 는 0 — **출시일에** 본편 AppID 로 바꾸고 시험 5(4절)
- [ ] Steam Input 공식 설정, Deck 점검(`deck.md`), 호환성 검토 신청(링크가 열려 있으면 — 6절)
- [ ] 스토어: 문구 4~5개 언어, 캡슐·라이브러리 그림, 스크린샷 5장 이상, 트레일러, 태그, 지원 언어, 시스템 요구 사항(측정값)
- [ ] Content Survey(AI 공개 포함), GRAC
- [ ] Coming Soon 2주 이상, 빌드를 default 에 켜고 빌드 검토 통과
- [ ] 출시 단추: App Admin 의 *Release App* → *Publish Now* → *Release Now*. 필요한 권한은 *Publish app changes to Steam*, *Manage pricing and discounts* [S7]

## 출처

- [S1] 온보딩 — https://partner.steamgames.com/doc/gettingstarted/onboarding
- [S2] Steam Direct 수수료 — https://partner.steamgames.com/doc/gettingstarted/appfee
- [S3] 데모 — https://partner.steamgames.com/doc/store/application/demos
- [S4] SteamPipe 업로드 — https://partner.steamgames.com/doc/sdk/uploading · steamcmd `run_app_build [-preview] [-desc <text>]` 목록: https://gist.github.com/dgibbs64/79a7f3ab3c96a48275c4
- [S5] 검토 — https://partner.steamgames.com/doc/store/review_process
- [S6] Coming Soon — https://partner.steamgames.com/doc/store/coming_soon
- [S7] 출시 — https://partner.steamgames.com/doc/store/releasing
- [S8] Next Fest 2027년 2월 — https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/feb_2027
- [S9] Steam Cloud — https://partner.steamgames.com/doc/features/cloud
- [S10] 업적·통계 — https://partner.steamgames.com/doc/features/achievements · 아이콘 256×256 권장: https://godotsteam.com/tutorials/achievement_icons/
- [S11] Rich Presence — https://partner.steamgames.com/doc/features/enhancedrichpresence · https://partner.steamgames.com/doc/api/ISteamFriends
- [S12] 언어 코드 — https://partner.steamgames.com/doc/store/localization/languages
- [S13] Deck 호환성 — https://partner.steamgames.com/doc/steamdeck/compat
- [S14] Steam Input 공식 설정 — https://partner.steamgames.com/doc/features/steam_controller/workshop_uploaded_configs · https://partner.steamgames.com/doc/features/steam_controller/action_manifest_file
- [S15] 그림 — https://partner.steamgames.com/doc/store/assets · https://partner.steamgames.com/doc/store/assets/standard · https://partner.steamgames.com/doc/store/assets/libraryassets
- [S16] 설명 — https://partner.steamgames.com/doc/store/page/description
- [S17] 태그 — https://partner.steamgames.com/doc/store/tags
- [S18] Content Survey — https://partner.steamgames.com/doc/gettingstarted/contentsurvey · 독일 — https://partner.steamgames.com/doc/gettingstarted/contentsurvey/germany
- [S19] Valve 발표 "Localizing your game's presence on Steam"(언어별 게임 이름) — https://steamcdn-a.akamaihd.net/steamcommunity/public/images/steamworks_docs/english/LocalizingYourGamesPresenceonSteam.pdf
- 내부: MASTER_PLAN §1·§7·§8·§10, 조사 r04(Steam), r07(완성도), `Tools/story/GLOSSARY.md`
