# 업적

다섯 언어(한국어·영어·간체·번체·일본어) 이름·설명과 아이콘 파일 이름은 `achievements.csv` 에 있다(Excel 에서 바로 열리게 BOM 있는 UTF-8).
이 문서는 **무엇을 하면 풀리는가**와 Steamworks 에 넣는 순서를 적는다.

- 20개 모두 **연결됨(wired=yes)** — `SteamHooks` 가 부른다. `wired=planned` 는 기능이 들어오기 전의 업적을 적는 칸이다:
  그런 업적은 Steamworks 에 만들지 않는다 — 못 따는 업적이 목록에 보이면 안 된다. `check_steam.py` 가 코드와 이 칸을 대조한다.
- 봇 실행(`GameEvents.IsAutoPlaying`)과 데모(`DEMO`)에서는 아무것도 풀리지 않는다(MASTER_PLAN §7). 데모 앱에는 업적을 만들지 않는다.
- 새 앱은 처음에 업적 100개까지다. 20개는 넉넉하다.

## 표

| API | 이름 | 풀리는 때 | 숨김 | 연결 | `SteamHooks` 가 보는 것 (2026-10-01 작업 트리) |
|---|---|---|---|---|---|
| `ACH_CONTRACT` | 계약 | 3층 잊혀진 숲에서 마검과 계약 | | yes | `PlayerData.IsContractedSword` — 계약 이벤트가 없어 `HudRefreshed`·`FloorEntered`·`BattleEnded` 때 본다 |
| `ACH_KINGSLIME` | 왕은 이몸 하나다 | 4층 킹 슬라임 격파 | | yes | `BossDefeated`, monsterId = `Define.KingSlime` |
| `ACH_BOSS20` | 해 지기 전에 | 20층 묘소의 늑대 | | yes | `BossDefeated`, 층(stageId+1)이 20의 배수 → `"ACH_BOSS" + 층` |
| `ACH_BOSS40` | 내리지 않는 방패 | 40층 수로의 방패병 | | yes | 〃 40 |
| `ACH_BOSS60` | 타다 남은 목소리 | 60층 재의 파수꾼 | | yes | 〃 60 |
| `ACH_BOSS80` | 마지막 문 | 80층 얼어붙은 기사 | | yes | 〃 80 |
| `ACH_BOSS100` | 검은 태양 | 100층 검은 태양 | | yes | 〃 100 |
| `ACH_END_SEAL` | 잘 자, 잠꾸러기 | 마지막 선택 "검을 왕좌에 꽂는다" | | yes | `EndingReached("seal")`, 그리고 층에 들어설 때마다 `Records` 에 있는 결말 |
| `ACH_END_HOLD` | 영원한 동반자 | "검을 놓지 않는다", 레벨이 `StoryDirector.DawnLevel` 미만 | | yes | `EndingReached("hold")` |
| `ACH_END_DAWN` | 배부른 검 | "검을 놓지 않는다", 레벨이 `DawnLevel` 이상 | **숨김** | yes | `EndingReached("dawn")` |
| `ACH_END_ALL` | 세 갈래 길 | 세 결말을 모두 봄 (판을 넘어 센다) | | yes | `Records.EndingsSeen == 3`. 진행 통계 `ENDINGS_SEEN` (0~3) |
| `ACH_CRIT_CARRY` | 이어지는 숨 | 첫 타가 치명타인 싸움에서 이긴다 — HUD 가 노란 "치명까지 1타", 몬스터 정보가 "첫 타 치명" 일 때 | | yes | `BattleEnded` won 이고 `LastBattle.FirstHitCrit` |
| `ACH_NO_SPILL` | 한 방울도 | 다섯 층 연속, 회복이 한 번도 넘치지 않음 | | yes | 처음 오는 바로 다음 층에 들어설 때 끝낸 층을 하나 센다. `ItemPicked` overflow > 0 이면 처음부터(되살아나면 그 층은 다시) |
| `ACH_SLIVER` | 한 끗 | 최대 체력의 1% 이하를 남기고 승리 | | yes | `BattleEnded` won, 이긴 순간의 HP(그 한 대의 레벨 업 몫을 뺀 값)가 0 초과 `LastBattle.MaxHp` 의 1% 이하 |
| `ACH_BOSS_BARE` | 맨손의 셈 | 스킬 없이 챕터 보스(20·40·60·80·100층) 격파 | | yes | 위 보스 업적과 같은 때, `LastBattle.MonsterId` 가 그 보스이고 `SkillsUsed` 가 false. 킹 슬라임(4층)은 20의 배수가 아니어서 빠진다 |
| `ACH_UNDER_PAR` | 예언보다 싸게 | 한 층을 기준(par)보다 적게 치르고 끝낸다 | | yes | `FloorTallied(stageId, paid, par)` — 장부가 새 층에 처음 들어서며 앞 층을 닫을 때. par > 0 이고 paid < par |
| `ACH_CH_3STAR` | 값을 아는 자 | 한 챕터의 결산(다섯 층 띠)이 모두 ★★★ | | yes | `BandTallied(band, stars)` 와 장부의 `BandStars`(띠 번호 순, 0 = 기준 없는 띠는 건너뛴다). 챕터 0 은 띠 1~3(6~20층 — 띠 0 은 생성 층이 5층뿐), 챕터 c 는 띠 4c~4c+3. 층에 들어설 때마다 체크포인트의 장부로 다시 본다 — 체험판에서 끝낸 챕터 0 도 본편에서 풀린다 |
| `ACH_DAWN_PAR` | 배불리, 싸게 | 판 전체를 기준(par) 이하로 치르고 새벽 결말 | **숨김** | yes | `EndingReached("dawn")` 이고 장부의 `Paid ≤ Par`, `Par > 0` |
| `ACH_TOWER` | 탑의 법 | 탑의 법에서 100층 보스 격파(결말까지) | | yes | `EndingReached` 이고 `PlayerData.Mode == Tower`. 층에 들어설 때마다 `Records.ModeCleared(Tower)` 로도 — Steam 없이 끝낸 판 |
| `ACH_TOWER_DAWN` | 법 위의 새벽 | 탑의 법에서 새벽 결말 | **숨김** | yes | `EndingReached("dawn")` 이고 `Mode == Tower` |

### MASTER_PLAN §7 과 다르게 적은 것

게임 안 글자와 맞췄다. 보스 이름·칭호는 ScriptData 10900~10904·20900~20904, 대사는 `Tools/story/story_*.json` 에서 그대로 가져왔다.

| API | MASTER_PLAN | 여기 | 까닭 |
|---|---|---|---|
| `ACH_BOSS40` | The Unlowered Shield | The Shield Never Lowered | 40층 보스 칭호의 게임 번역 (20901) |
| `ACH_BOSS60` | What the Fire Left | The Voice That Did Not Burn | 60층 보스 칭호 (20902). 중국어도 게임의 "未燃尽的声音" |
| `ACH_BOSS80` | The Last Gate | The Last Door | 80층 보스 칭호 "Warden of the Last Door" (20903) |
| `ACH_KINGSLIME` | Only One King | The Only King Here | 마검의 대사 "The only king here is this blade!" (`pro_kingslime_reveal.01`) |
| `ACH_END_SEAL` | 잘 자라 / Sleep Well | 잘 자, 잠꾸러기 / Sleep Well, Sleepyhead | 봉인 결말에서 데미안이 하는 말 (`ending_seal.08`) |
| `ACH_CRIT_CARRY` | Held Breath | The Breath Carries Over | "held breath" 는 숨을 참는다는 뜻이 된다. 게임은 "The breaths I count carry over" (`mech_crit.04`) |
| `ACH_DAWN_PAR` | 숨김 아님 | **숨김** | 설명이 새벽 결말을 말한다. `ACH_END_DAWN` 을 숨긴 것이 헛일이 된다 |
| 결말 셋의 설명 | 봉인/함께 남는/새벽 결말을 본다 | 선택지 문구로 | 게임의 선택지는 "검을 왕좌에 꽂는다 / 검을 놓지 않는다" (`ending_choice`). hold 와 dawn 을 가르는 것은 레벨(검의 배부름)이라 "배고픈 / 배부른" 으로 적었다 |
| `ACH_UNDER_PAR`·`ACH_DAWN_PAR` 설명 | 예언보다 적게 / 예언 이하의 값으로 | 기준보다 적게 / 판 전체를 기준 이하로 | 풀리는 조건은 기준(par, `ParLoss` 합)이다. 예언(foretold)은 스킬 없이 싸우면 치른 값과 같아서 조건이 되지 못한다. 결산 창의 말("예언 · 치름 · 기준", `ledger.py` 471)을 따랐다. 이름(예언보다 싸게)은 그대로 둔다 — 바꿀지는 소유자 결정 |
| `ACH_CH_3STAR` 설명 | 한 챕터 전 층 ★★★ | 한 챕터의 결산을 모두 ★★★ | 별은 층이 아니라 다섯 층 결산(띠)마다 받는다(`ledger.py` 470·472). 층별 별은 MASTER_PLAN §9 에서 뺐다 |
| `ACH_TOWER` 간체·번체 이름 | 塔之律 | 塔之法则 / 塔之法則 | 새 게임의 규칙 이름(ScriptData 533, `mode.py`)과 같게. 설명의 「…」 도 같은 이름. `check_steam.py` 가 대조한다 |

## Steamworks 에 넣기

1. App Admin → **Stats & Achievements → Achievements** → *New Achievement*. 아래 칸을 CSV 대로 채운다.
   - API Name (`api_name`), Display Name, Description — 먼저 **영어**로 만든다.
   - Set By: **Client** · Hidden: `hidden` 이 1 이면 켠다 · Progress Stat: `ACH_END_ALL` 만 `ENDINGS_SEEN`, 0~3 (`stats.md` 를 먼저 만든다).
   - 아이콘: Achieved = `icon_achieved`, Unachieved = `icon_unachieved`. **256×256** (Valve 권장 크기), PNG 또는 JPG.
     아직 없다 — 아트 발주 20×2장(MASTER_PLAN §8). 잠긴 쪽은 같은 그림을 어둡게 해도 된다. 임시 아이콘으로 먼저 넣고 나중에 바꿔도 된다.
2. 언어별 이름·설명: 각 업적 편집 화면에서 언어를 골라 CSV 의 `koreana`·`schinese`·`tchinese`·`japanese` 칸을 넣는다.
   한 번에 넣으려면 업적 페이지의 현지화 파일을 내려받아 채워 올린다 — 토큰 이름(`NEW_ACHIEVEMENT_x_y_NAME/_DESC`)은
   Steam 이 붙이므로 업적을 다 만든 **뒤에** 내려받는다.
3. **Publish** (App Admin 맨 위 *Publish* 탭). 안 하면 게임에서 안 보인다.
4. `python Steam/check_steam.py` — `SteamHooks.cs` 가 생긴 뒤에는 코드가 부르는 이름과 이 CSV 를 대조한다.

## 시험

- 개발 빌드(또는 에디터)를 Steam 을 켠 채 돌린다. 에디터에서는 저장소 루트의 `steam_appid.txt` 가 AppID 를 알려 준다.
- 풀린 업적은 Steam 오버레이 팝업, 라이브러리 → 게임 → 업적에서 본다. 다시 시험하려면 Steam 콘솔(`steam://open/console`)에서
  `reset_all_stats <AppID>`.
- 빠른 확인: 개발 빌드의 치트 키 F2(공격 +10000, `UI_GameScene`)로 보스를 한 대에 잡으면 보스·맨손 업적을 층마다 확인할 수 있다.
  이것도 봇과 마찬가지로 **시험 계정에만** 한다.
- 봇 한 판(`PlaythroughRecorder`) 뒤에는 **아무것도 풀리지 않아야** 한다 (MASTER_PLAN §3 기준 8).
- 장부·별·탑의 법 다섯:
  - `ACH_UNDER_PAR`: 한 층을 기준보다 적게 치르고 다음 층에 처음 들어서면 그 순간 풀린다(장부가 앞 층을 닫을 때). 사람 판에서는 스킬(흡혈·철벽)로
    아끼거나 설계보다 높은 레벨로 싸운다. 빠른 확인은 치트 F2(공격 +10000, 시험 계정만)로 층을 끝내고 계단을 오른다.
  - `ACH_CH_3STAR`: 한 챕터의 네 결산(챕터 0 은 6~20층의 셋)을 ★★★(기준의 70% 이하)로. 마지막 결산 창이 뜰 때 풀린다.
    치트 F2(공격 +10000)로 싸우면 치름이 거의 0 이라 빨리 확인된다(시험 계정만). 챕터 0~3 으로 시험한다 — 챕터 4 의 마지막 결산(96~100층)은
    100층 위 계단에서만 닫히는데 계약한 판은 보스 뒤 바로 결말로 가서 그 계단을 밟지 않는다(`leaderboards.md` 의 열린 문제). 그게 풀리기 전엔 챕터 4 로는 안 풀린다.
  - `ACH_TOWER`·`ACH_TOWER_DAWN`: 새 게임에서 탑의 법을 골라 결말까지. 새벽은 레벨이 `DawnLevel` 이상일 때. Steam 을 끈 채 탑의 법 결말을 본 뒤
    Steam 을 켜고 아무 판에서 층을 옮기면 `ACH_TOWER` 가 풀린다(`Records.ModeCleared`).
  - `ACH_DAWN_PAR`: 새벽 결말을 판 전체 치름 ≤ 기준으로 — 결산 창의 합계로 확인한다(결말 카드는 위 열린 문제가 풀려야 뜬다). 조건은 결말 순간의
    장부라 그 문제와 상관없이 풀린다 — 100층 보스의 값은 결말 전에 장부에 든다(BattleEnded 가 CoEnding 의 첫 대기보다 먼저다).
- 체험판(DEMO 빌드)에서는 아무것도 풀리지 않는다. 체험판에서 쌓인 것(본 결말·잡은 보스·챕터 0 의 별)은 같은 PC 의 본편이 그 저장으로 층을 옮길 때 풀린다.
- 결말 셋: 100층 입구 체크포인트에서 이어하기 → 보스 → 선택. hold 와 dawn 은 레벨로 갈리므로(`StoryDirector.DawnLevel`)
  한 판에서 둘 다 보려면 레벨이 모자란 체크포인트가 필요하다. `ENDINGS_SEEN` 이 3 이 되는 순간 `ACH_END_ALL` 이 함께 풀린다.
