# TheSword 음악 후보 (2026-11-26)

지금 게임 음악은 챕터 00 곡 하나를 챕터마다 조·속도만 바꿔 쓴다(`ChapterTheme.BgmPitch`). 사람 기준 10시간이 넘는 게임에서 가장 큰 약점으로 본다.
Claude 는 음악을 만들거나 사지 않는다 — 아래는 **오너가 고르고 사기 쉽게** 추린 목록이다. 사기 전에 각 페이지의 라이선스 원문을 꼭 직접 읽는다(아래는 11-26 페이지에서 읽은 요약).

## 필요한 것

| 자리 | 분위기 | 주소(어드레서블 키) |
|---|---|---|
| 챕터 0 이끼 낀 지하 묘소 (5~20층) | 고요·옛 무덤 | `BGM_CH0` |
| 챕터 1 무너진 수로 (21~40층) | 물·눅눅함 | `BGM_CH1` |
| 챕터 2 잿빛 용광로 (41~60층) | 불·쇠 | `BGM_CH2` |
| 챕터 3 얼어붙은 심층 (61~80층) | 얼음·적막 | `BGM_CH3` |
| 챕터 4 왕좌의 균열 (81~100층) | 심연·종말 | `BGM_CH4` |

모두 **반복(loop) 버전**이 있어야 한다. 1~4층(손수 만든 도입부)·보스·결말 곡은 지금 것을 그대로 둬도 된다.

## 후보

| 묶음 | 값 | 곡 | 라이선스(페이지 요약) | 맞는 자리 |
|---|---|---|---|---|
| [Owl Theory — RPG Music Pack: Dungeon Themes](https://owl-theory-music.itch.io/rpg-music-pack-dungeon-themes) | $12.99~ | 10곡, 모두 loop(WAV) — "Crypt of Voices", "Silence of Hell", "The Collapsing Labyrinth" … | "Unlimited Royalty-Free", 원문은 페이지에 없음 → 사기 전에 확인 | 묘소·왕좌·용광로(이름으로 짐작) |
| [jacoblivesmusic — Dungeons: Fantasy Action RPG Music](https://jacoblivesmusic.itch.io/dungeons-fantasy-action-rpg-music) | $19.99~(할인 중) | 9곡 + loop 10 — "Into The Catacombs", "The Merchant's Cave", "Bamboo Tunnel River" … | Royalty-Free, EULA 문서가 따로 링크돼 있음 | 묘소·수로 |
| [Thomas Brunet — Dark Fantasy RPG Music Pack Vol.1](https://thomas-brunet.itch.io/dark-fantasy-rpg-music-pack) | €12.50~ | 12곡(loop 6) — "Tundra", "Hunted", "Silver Sword" … | "Unlimited Commercial Use License … royalty free … usable in any engine without limitations" | 심층("Tundra" 는 loop 아님에 주의) |
| [Unity 에셋 스토어 — Dungeons: Fantasy Action RPG Music](https://marketplace.unity.com/packages/audio/music/dungeons-fantasy-action-rpg-music-243453) | 스토어 가격 | 위 jacoblivesmusic 와 같은 곡으로 보임 | Unity 에셋 스토어 EULA(게임에 넣어 파는 것 허용) | 위와 같음 |
| [OpenGameArt — 15 melodic rpg chiptunes](https://opengameart.org/content/15-melodic-rpg-chiptunes) | 무료 | 칩튠 15곡 | 검색 결과로는 CC0(표기 불필요) — 페이지를 직접 못 열어 확인 안 됨 | 도트 그림과는 맞지만 지금 곡(오케스트라풍)과 결이 다르다 |

추천: 한 작곡가의 묶음 하나로 다섯 챕터를 채우는 편이 결이 맞는다. 곡 이름만으로는 분위기를 알 수 없으니 **페이지의 미리듣기로 다섯 자리에 하나씩 짝을 지어** 본다.

## 사기 전에 꼭

- **저장소가 공개다.** 산 곡 파일을 그대로 커밋하면 누구나 받아 갈 수 있게 된다 — 거의 모든 로열티 프리 라이선스가 "곡 자체를 다시 배포하지 말라" 고 한다.
  저장소는 11-06 오너 결정으로 공개 그대로이니, 새 곡은 `Assets/@Resources/Sounds/BGM/Licensed/` 에 둔다 — 11-26 에 이 폴더를 `.gitignore` 에 넣어 두었다
  (빌드에는 들어가고 저장소에는 안 올라간다 — 다른 기기에서 빌드하려면 그 폴더를 따로 옮긴다).
- 라이선스 PDF/원문을 받아 `Steam/` 옆이 아니라 로컬에 보관한다. 크레딧(`Tools/ui_text_parts/credits.py`)·`Steam/THIRD_PARTY_NOTICES.txt` 에 작곡가 이름을 적는다(요구가 없어도 예의).
- 사람이 만든 곡이면 Steam AI 공개에는 더할 것이 없다.

## 넣는 법 (Claude 가 할 수 있다)

곡 파일을 주면: 어드레서블에 `BGM_CH1`~`BGM_CH4`(묘소는 `BGM_CH0`) 키와 "PreLoad" 라벨로 올리고, 그 챕터의 `ChapterTheme.BgmPitch` 를 1 로 둔다 —
키에 곡이 있으면 `GameManager.PlayChapterBGM` 이 알아서 집는다(CLAUDE.md "챕터 조명" 절). 소리 크기를 지금 곡과 맞추고 반복 이음매를 들어 본다.
