# 순위표 (Leaderboards)

한 판을 결말까지 끝내면 점수 하나를 올린다. 점수는 **마검의 장부**가 계산한다(`SwordLedger`, `GameEvents.RunScored`):

    점수 = round(1000 · 치른 값 / 기준 값)        낮을수록 좋다. 1000 = 설계대로, 700 = 설계의 70% 만 치렀다

- **치른 값**은 싸움으로 잃은 HP 의 합(흡혈로 되찾은 것은 뺀다) + 제단에 바친 HP. **기준 값**은 그 판에서 **싸운** 몬스터의
  기준 값(`MonsterData.ParLoss`) 합이다 — 곁길 몬스터를 건너뛰면 기준도 같이 준다.
- 기준 값이 0 인 판(장부가 없던 옛 저장)은 점수가 없다. 최종 HP 로 줄 세우지 않는다(MASTER_PLAN §9).
- 점수는 장부가 판을 닫을 때 난다(`SwordLedger.FinishRun` → `GameEvents.RunScored`, 결말 직전). **판마다 한 번** — 100층 입구 체크포인트에서
  다시 올라도 같은 판은 다시 내지 않는다(`LEDGER_SCORED_RUN`).
- **열린 문제 (2026-10, 출시를 막는다):** `FinishRun` 은 지금 100층 **위 계단**(`PortalController`)에서만 불린다. 그런데 계약한 판은 100층
  보스(904)가 쓰러지자마자 `StoryDirector.OnBossDefeated` → `CoEnding` 이 결말을 틀고 엔딩 씬으로 가서, 보스 뒤의 그 계단을 밟지 않는다.
  그래서 실제 완주는 **점수를 내지 않는다** — 이 표도, `Records` 의 최고 점수도, 판 카드도 비고, 마지막 띠(96~100층)가 닫히지 않아
  챕터 4 의 `ACH_CH_3STAR` 도 못 푼다. 결말 길에서 마지막 띠를 닫고 `FinishRun` 을 부르면(STORY/RUN 레인) 이 절의 나머지가 그대로 맞는다.
- 올리는 곳은 `SteamHooks`(`Assets/@Scripts/Managers/Core/SteamHooks.cs`). 점수를 받으면 곧바로 PlayerPrefs `STEAM_PENDING_SCORE` 에
  적고(판 id·표·점수·자세한 값), Steam 이 받았다고 답해야 지운다. 결말을 기다리다 꺼지거나 Steam 없이 끝낸 판은 다음에 Steam 을 켠 채
  씬이 설 때(타이틀) 올라간다. 봇 판은 점수 자체가 없고, 체험판은 100층까지 가지 않는다(올리지도 않는다).

## 표

| API Name | 규칙 | Sort Method | Display Type | Writes | Reads | Community Name |
|---|---|---|---|---|---|---|
| `LB_NORMAL_V1` | 보통 (`GameMode.Normal`) | **Ascending** | **Numeric** | Trusted 끔 | Friends 끔 | 보통 — 점수 (낮을수록 좋다) / Normal — Score (lower is better) |
| `LB_TOWER_V1` | 탑의 법 (`GameMode.Tower`) | **Ascending** | **Numeric** | Trusted 끔 | Friends 끔 | 탑의 법 — 점수 / Tower's Law — Score |

- **Ascending**: 낮은 점수가 1위다(Steam: "For positional based leaderboards, use Ascending").
- 올리기는 **KeepBest** — Steam 이 그 사람의 가장 좋은(낮은) 점수만 남긴다. 더 나쁜 판을 끝내도 기록이 나빠지지 않는다.
- **Writes 의 Trusted 는 끈다.** 켜면 게임(클라이언트)이 점수를 못 쓰고 Web API 로만 쓸 수 있다.
- **Community Name 을 채워야** 커뮤니티 허브에 보인다. 비우면 표는 있어도 안 보인다.

## Steamworks 에 넣기

1. App Admin → **Stats & Achievements → Leaderboards** → *New Leaderboard*. 위 표대로 둘을 만든다.
2. **Publish**.
3. `python Steam/check_steam.py` — 코드(`SteamHooks.cs`)의 이름과 이 표의 이름이 다르면 FAIL.

코드는 `FindOrCreateLeaderboard` 로 찾는다 — 없으면 같은 정렬·표시로 만든다. 그렇게 만든 표는 커뮤니티 이름이 없어 허브에
안 보이고, 이미 있는 표에는 정렬·표시 인자가 무시된다. **먼저 App Admin 에서 만드는 것이 정석이다.**

## 자세한 값 (score details)

점수마다 int32 를 64개까지 붙일 수 있다. 게임 안에서 보여 주지는 않는다 — 나중에 순위표 창을 만들거나 Web API
(`ISteamLeaderboards`)로 판을 들여다볼 때 쓴다. 순서가 곧 형식이다. 바꾸면 `_V` 를 올린다.

| 칸 | 값 | 출처 |
|---|---|---|
| 0 | 결말: 0 모름 · 1 seal · 2 hold · 3 dawn | `EndingReached`. 결말을 이미 본 판(크레딧만 도는 길)이나 선택 전에 끈 판은 0 |
| 1 | 치른 값 (판 전체) | `LedgerState.Paid` |
| 2 | 기준 값 (판 전체) | `LedgerState.Par` |
| 3 | 예언한 값 (판 전체) | `LedgerState.Foretold`. 스킬 없이 싸우면 치른 값과 같다 — **예언 − 치른 값 = 스킬로 아낀 HP** |
| 4~8 | 챕터 0~4 에서 치른 값, 모르면 -1 | 챕터 첫 층(21·41·61·81층)에 처음 들어설 때의 누적 치른 값의 차. 1칸처럼 1~4층 싸움·제단을 포함한다 — 다 알면 합이 1칸이다 |

- 점수는 판 점수를 받은 순간(`RunScored`)의 장부와 규칙(표)으로 적는다. 결말만 뒤에 채운다(둘 중 무엇이 먼저 오든 모이면 올린다).
  결말은 같은 판(`LedgerState.RunId`)의 것만 붙인다 — 결말 전에 꺼진 판을 다음에 올리면 0 이다.
- 챕터별 값은 시간 구간이다 — 21층에 처음 선 뒤 워프로 내려가 20층 이하에서 싸운 값은 챕터 1 에 들어간다(장부의 층·띠 결산과 같은 방식).
  경계의 누적 값은 **이 PC 의 PlayerPrefs**(`STEAM_CHAPTER_PAID_1`~`4`)에 둔다. 새 판(2층에 처음)이 지우고, 앞 챕터의 체크포인트로
  돌아가면 경계를 다시 밟으며 덮어쓴다. 다른 PC 에서 넘긴 챕터는 -1 이다. 체험판에서 넘긴 21층은 같은 PC 의 본편이 잇는다.
- **스킬 쓴 횟수는 없다.** 장부(`LedgerState`)에 판 단위로 남지 않는다 — 전투 하나의 `LastBattle.SkillsUsed` 뿐이다. 3칸(예언)으로 대신한다.
  장부에 생기면 9칸에 붙인다(`_V` 를 올린다).

## 버전 (`_V`)

점수는 콘텐츠에 달려 있다 — 몬스터 값(`ParLoss`)·지도·물약을 바꾸면 같은 플레이가 다른 점수를 받는다. 그래서 **콘텐츠 버전마다 표를 새로 연다.**

- `Tools/content_versions.json` 의 MapData 해시가 바뀌는 패치(= 새 ContentVersion, MASTER_PLAN §5), 또는 몬스터 표·기준 값을 다시 뽑은 패치
  → `SteamHooks` 의 `BoardNormal`·`BoardTower` 를 `_V2` 로 올리고, 이 문서의 표에 새 줄을 더하고, Steamworks 에 새 표 둘을 만든다.
- 옛 표는 지우지 않는다(옛 기록). Community Name 에 "(v1)" 을 붙여 둔다.
- 자세한 값의 순서를 바꿀 때도 올린다 — 한 표 안의 행들은 같은 형식이어야 한다.
- 출시 전(Playtest)까지는 임시다(MASTER_PLAN §7) — 출시할 때 App Admin 에서 `_V1` 두 표를 지우고 같은 설정으로 다시 만들어 빈 표로 시작해도 된다.

## 시험

- Steam 을 켠 채 에디터·개발 빌드로 한 판을 끝낸다(아직 점수를 내지 않은 판의 100층 입구 체크포인트에서 이어 해도 된다 — 위의 열린
  문제가 풀린 뒤라야 점수가 난다). 로그: `[Steam] 순위표 LB_NORMAL_V1 점수 874 (바뀜 True, 순위 1)`, 그 뒤 PlayerPrefs 의
  `STEAM_PENDING_SCORE` 가 없다. 못 올리면 "찾지 못했다" / "올리지 못했다 — 다음 씬에서 다시" 가 찍히고 그 값이 남는다.
- 꺼져도 남는지: 점수가 난 뒤 결말을 고르기 전에 게임을 끈다 → `STEAM_PENDING_SCORE` 가 남아 있다 → 다시 켜면 타이틀에서 올라가고
  (결말 칸 0) 값이 지워진다. Steam 을 끈 채 끝낸 판도 같다.
  앱 등록 전(`steam_appid.txt` 가 480 = Spacewar)에는 Valve 의 시험 앱에 표가 생길 수 있다 — 시험 이름으로 하는 편이 낫다(아래).
- 내 점수: Steam 커뮤니티의 게임 허브 → 순위표(Community Name 을 넣은 뒤), 또는 Web API
  `ISteamLeaderboards/GetLeaderboardEntries/v1` (파트너 키).
- 봇 한 판(`PlaythroughRecorder`) 뒤에는 표에 아무것도 없어야 한다.
- 시험 기록이 진짜 표에 남지 않게: 시험은 `_V1` 이 아니라 시험용 이름(예: `LB_TEST`)으로 코드를 잠깐 바꿔 하고, 끝나면 App Admin 에서 그 표를 지운다.
  (`reset_all_stats` 는 통계·업적만 지운다고 알려져 있다 — 순위표 기록이 같이 지워지는지는 확인하지 않았다.)
