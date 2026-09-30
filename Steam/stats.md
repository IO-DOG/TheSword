# 통계 (Stats)

App Admin → **Stats & Achievements → Stats** 에 넷을 만들고 *Publish* 한다. 값은 판을 넘어 남는 기록 `Records.json`
(`Assets/@Scripts/Managers/Core/Records.cs`)과 같은 것을 센다 — 새 게임이 지우지 않고, 봇 실행(`GameEvents.IsAutoPlaying`)은 세지 않는다.
Steam 에 넣는 일은 `SteamHooks` 가 한다(MASTER_PLAN §7). 이 표의 API 이름이 코드와 한 글자라도 다르면 조용히 안 올라간다 —
`python Steam/check_steam.py` 가 대조한다.

| API Name | Type | Set By | Increment Only | Max Change | Min | Max | Default | Aggregated | Display Name |
|---|---|---|---|---|---|---|---|---|---|
| `MAX_FLOOR` | INT | Client | ✓ | 비움 | 0 | 100 | 0 | ✗ | 도달한 층 / Highest Floor |
| `ENDINGS_SEEN` | INT | Client | ✓ | 비움 | 0 | 3 | 0 | ✗ | 본 결말 / Endings Seen |
| `FIGHTS_WON` | INT | Client | ✓ | 비움 | 0 | 비움 | 0 | ✓ | 이긴 싸움 / Fights Won |
| `DEATHS` | INT | Client | ✓ | 비움 | 0 | 비움 | 0 | ✓ | 쓰러진 횟수 / Deaths |

| API Name | 게임에서 무엇 | 출처 |
|---|---|---|
| `MAX_FLOOR` | 들어가 본 가장 높은 층 번호(1~100) | `Records.MaxFloor` (`GameEvents.FloorEntered`, stageId+1) |
| `ENDINGS_SEEN` | 본 결말 수(seal·hold·dawn, 0~3). **ACH_END_ALL 의 진행 통계** | `Records.EndingsSeen` (`GameEvents.EndingReached`) |
| `FIGHTS_WON` | 이긴 전투 수(보스 포함) | `Records.FightsWon` (`GameEvents.BattleEnded` won) |
| `DEATHS` | 진 전투 수 = 쓰러진 횟수 | `Records.Deaths` (`GameEvents.BattleEnded` !won) |

## 왜 이렇게

- **Increment Only.** 넷 다 줄어들 일이 없다. `Records.json` 이 지워지거나 다른 PC 의 옛 값으로 덮여도 Steam 쪽 값이 뒤로 가지 않는다.
- **Max Change 는 비운다.** 데모에서 넘어온 기록이나, `SteamHooks` 가 들어가기 전부터 쌓인 `Records.json` 을 처음 올릴 때
  값이 한 번에 크게 뛴다. 제한을 걸면 그 첫 `SetStat` 이 거부된다.
- **Aggregated** 는 전 세계 합계다. 이긴 싸움·쓰러진 횟수만 켠다(합쳐서 뜻이 있는 것). 층 번호·결말 수를 더한 값은 뜻이 없다.
- **진행 통계는 ACH_END_ALL 에만 건다** (Min 0, Max 3). Steam 은 진행 통계가 끝값에 닿으면 그 업적을 **저절로 풀어 준다**.
  그래서 보스 업적에 `MAX_FLOOR` 를 걸면 안 된다 — 100층에 **들어서기만** 해도 ACH_BOSS100 이 풀린다.
- 데모 앱에는 통계·업적을 만들지 않는다(Valve 권장). 데모에서 쌓인 `Records.json` 은 같은 폴더·같은 클라우드라
  본편 첫 실행에서 `SteamHooks` 가 올린다(MASTER_PLAN §7).

## 시험

- Steam 콘솔(`steam://open/console`)에서 `reset_all_stats <AppID>` 로 내 계정의 통계·업적을 지운다. 코드로는 `SteamUserStats.ResetAllStats(true)`.
- 봇 한 판(`PlaythroughRecorder`) 뒤 네 값이 **그대로**여야 한다 — 봇은 아무것도 올리지 않는다.
- 사람 판: 5층 입장 → `MAX_FLOOR` 5, 한 번 지고 이어하기 → `DEATHS` +1. 내 계정의 값은 Web API
  `ISteamUserStats/GetUserStatsForGame/v2` (appid·steamid·API 키)로 읽는다. `ENDINGS_SEEN` 은 Steam 업적 목록의 `ACH_END_ALL` 진행 막대(0/3)로도 보인다.
