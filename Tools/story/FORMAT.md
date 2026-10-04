# 스토리 데이터 형식

스토리 원본은 `Tools/story/story_kr.json` 한 파일이다. 번역은 같은 키로 `story_en.json`,
`story_jp.json`, `story_cn.json` 에 둔다. 게임 데이터(ScriptData/EventData)는 `Tools/story_gen.py` 가
이 파일들에서 굽는다 — **게임 JSON 을 손으로 고치지 않는다.**

작업 중에는 부분 파일 `Tools/story/parts/<part>.json` 을 같은 형식으로 쓰고, 편집자가 합친다.

## 최상위

```json
{
  "version": 1,
  "speakers": { "<speakerId>": { "kr": "화면에 뜨는 이름" } },
  "scenes": [ <scene>, ... ],
  "prologue_rewrites": { "<ScriptData id>": "고쳐 쓴 문구", ... },
  "bestiary": { ... }            // 도감·이름 (아래)
}
```

부분 파일은 필요한 키만 있으면 된다(예: `scenes` 만).

## 화자 (speakerId)

| id | 초상화 | 비고 |
|---|---|---|
| `damian` | 데미안 | 감정: Normal AHA Confusion Panic Question Silence Surprise Thinking |
| `sword` | 에고소드 | 감정: Normal Angry Madness Nerve Panic Question Silence Sleep Smile Surprise Treasure |
| `narration` | 없음 | 이름도 안 뜬다. 설명·독백·카드 문구 |
| `boss0` ~ `boss4` | 그 챕터 보스의 전투 스프라이트 | 20/40/60/80/100층 보스. 이름은 `speakers` 에 적는다 |
| 그 밖 (`chief` 등) | 없음 | 이름만 뜬다. `speakers` 에 반드시 등록 |

`emotion` 은 damian·sword 만 쓴다. 다른 화자는 생략.

## 장면 (scene)

```json
{
  "id": "ch1_start",                 // 영문 snake_case, 파일 전체에서 유일
  "kind": "dialogue",                // dialogue | card | bark | choice | credits
  "trigger": "chapter_start:1",      // 아래 트리거 목록에서만
  "staging": "구현자에게 주는 연출 메모 (카메라, 동작, 이펙트, 음악). 사용 가능한 것만 (BRIEF 2절)",
  "lines": [
    { "key": "ch1_start.01", "speaker": "sword", "emotion": "Smile", "kr": "크하핫!\n물 냄새가 나는군." },
    { "key": "ch1_start.02", "speaker": "damian", "emotion": "Thinking", "kr": "..." }
  ]
}
```

- `key` 는 `<scene id>.<두 자리 번호>`. 번역 파일이 이 키로 짝을 찾는다.
- 한 줄(`kr`)은 **최대 3줄**, 줄마다 한국어 **22자 안팎**. 줄바꿈은 `\n`.
- `card`: 전체 화면 그림 + 내레이션. 줄마다 `"image"` 를 줄 수 있다(없으면 앞 그림 유지).
  쓸 수 있는 그림 키: `Intro01`(길드 게시판) `Intro02`(의뢰서를 찢는 손) `Intro03`(떨어지는 탑·마을)
  `Intro04`(같은 장면 밤) `Intro05`(벼랑 위 후드와 검은 태양, 세로로 김) `TowerArrival`(탑 밑동에 도착한 용병)
  `TowerGraves`(탑 밑동의 무덤과 꽂힌 검) `Parchment`(양피지, 글만 보일 때) `ForestLine` `ForestColor`
  `Loading1`~`Loading6`(치비 삽화) `GameOver1` `Ending`(숲의 빛 속 검을 든 데미안)
- `bark`: 대화창을 열지 않고 화면 위쪽에 잠깐 뜨는 한 줄. **움직임을 막지 않는다.**
  같은 트리거에 여러 장면을 두면 변주가 된다. 층 유형 바크는 `story_gen` 이 층마다 변주를 돌려 미리 정하고(`GeneratedStory.FloorBarks`),
  죽음 바크는 그중 하나가 무작위로 나온다.
- `choice`: `lines` 로 상황을 말한 뒤 선택지를 띄운다.
  `"choices": [ { "id": "seal", "kr": "검을 왕좌에 꽂는다" }, { "id": "take", "kr": "..." } ]`
  선택 결과는 `ending:<choice id>` 트리거의 장면으로 이어진다.
- `credits`: `lines` 가 위로 흘러가는 크레딧 문구. `speaker` 는 `narration`.

## 트리거 (구현되는 것만)

| 트리거 | 언제 |
|---|---|
| `prologue:contract_after` | 계약 연출이 끝난 직후 (3층) |
| `prologue:kingslime_reveal` | 킹 슬라임 등장 연출 직후, 전투 전 (4층) |
| `prologue:kingslime_clear` | 분열 슬라임을 다 잡은 뒤 (4층) |
| `village` | 4층 계단을 올라 5층에 처음 들어가기 직전. 카드 셋뿐이다(탑 · 떨어진 밤 · 칼 꽂힌 무덤). 촌장과 두 번째 무덤 카드는 6층 `floor_first:6` |
| `chapter_start:<c>` | 챕터 c(0~4)의 첫 생성 층(5·21·41·61·81)에 처음 들어섰을 때 |
| `floor_first:<n>` | n층에 처음 들어섰을 때 (5~100, 챕터 첫 층은 위 트리거를 쓴다) |
| `trait_first:<t>` | 특성 t 몬스터에게 **처음 부딪혔을 때**, 싸움 직전(보스 등장 뒤, 첫 ✖ 수업 앞). 들어설 때가 아니다. 첫 층은 `generate_content.BAND_TRAITS` 가 정한다(바이블 부록 A.3). t = beast magic guardian immortal knight titan assassin armor |
| `floor_type:<t>` | 그 유형의 층에 들어설 때 가끔 (bark 전용). t = basic stingy gate plenty treasure |
| `mechanic_first:<m>` | m 을 처음 겪을 때. m = forecast(첫 예측 표시) fatal(처음으로 ✖ 상대에게 부딪힘) overflow(물약이 넘침) vault(금고 문 앞) spare_key(여분 열쇠 획득) choice(둘 중 하나 보상 앞) rune(첫 룬) nokey(열쇠 없이 문에 부딪힘) warp(워프석 반지 획득) death(처음 죽고 되살아났을 때) crit(치명타 한 대 전) levelup(첫 레벨업, 5층 이후) |
| `death` | 죽고 되살아날 때마다 (bark, 변주) |
| `boss_intro:<c>` | 챕터 c 보스에게 처음 부딪혔을 때, 전투 직전 |
| `boss_defeat:<c>` | 챕터 c 보스를 쓰러뜨린 직후 |
| `ending:choice` | 100층 보스 처치 후 |
| `ending:<id>` | 선택(또는 조건)에 따른 결말 |
| `epilogue:<id>` | 결말 뒤 짧은 후일담 카드 |
| `credits` | 크레딧 |

## 도감 (bestiary)

```json
"bestiary": {
  "chapters": [ { "name": "이끼 낀 지하 묘소", "subtitle": "챕터 카드 부제", "mob_prefix": "이끼" }, ... 5개 ],
  "species": [ { "art": 0, "name": "슬라임" }, ... art 0~9 순서 ],
  "mob_desc": { "<chapter>:<art>": "그 챕터 그 종의 도감 설명 (한두 문장, 규칙 없이 — 바이블 13절)" },
  "bosses": [ { "chapter": 0, "name": "보스 이름", "title": "칭호", "desc": "도감 설명" }, ... 5개 ],
  "traits": [ { "id": 0, "name": "없음", "desc": "..." }, { "id": 1, "name": "야수", "desc": "정확한 규칙 + 공략 한 줄" }, ... 0~8 ],
  "items": { "monster_book": { "name": "...", "desc": "..." }, "warp_ring": {...}, "key": {...}, "potion": {...}, "rune": {...} }
}
```

특성 id 순서: 0 없음, 1 야수, 2 마법, 3 수호, 4 불사, 5 검사, 6 거대, 7 암살, 8 갑옷.
종(art) 순서: 0 슬라임, 1 슬라임(다른 색), 2 크로우, 3 정령, 4 늑대, 5 고블린 창병, 6 해골 전사, 7 고블린 방패병, 8 잿빛 파수꾼, 9 심연의 거수.

몬스터 이름은 게임이 `"{mob_prefix} {species.name}"` 로 만든다(예: "이끼 슬라임").
접두어와 종 이름이 어색하게 겹치지 않게 지을 것("심연 심연의 거수" 같은 것).

## 번역 파일

```json
{ "lang": "en",
  "speakers": { "damian": "Damian", ... },
  "lines": { "<line key>": "translation", ... },
  "choices": { "<scene id>.<choice id>": "..." },
  "prologue_rewrites": { "<id>": "..." },
  "bestiary": { 같은 구조 }
}
```
