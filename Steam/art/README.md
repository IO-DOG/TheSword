# Steam 그림 — 게임 그림으로 합성한 임시본

Steam 스토어·라이브러리·업적에 올릴 그림 전부. **프로젝트에 이미 있는, 사람이 그린 그림만** 잘라 붙였다 —
도트 스프라이트, 타이틀 도트, 인트로·로딩 삽화, 로고. 이미지 생성 모델은 한 번도 쓰지 않았다.
새로 그린 것은 그라데이션·둥근 사각형·디더링한 빛 같은 도형뿐이다.

- **도트는 정수 배율 NEAREST 로만 키웠다.** 삽화와 로고는 **줄이기만** 했다(LANCZOS·BOX). 흐린 확대는 없다.
- 업적 아이콘이 쓰는 스프라이트도 정수 배율이다 — 예외는 킹 슬라임 하나(큰 1x 시트를 2/3 로 면적 평균 축소).
- 이것은 **의뢰 그림이 오기 전까지의 임시본**이다. `Documents/TheSword_Art_Music_Prompts.md` 의 묶음 A(캡슐·키 아트),
  B(로고·라이브러리 로고·앱 아이콘), P(업적 20×2)가 오면 바꾼다 — 아래 표의 "의뢰 그림이 오면" 칸.

원본 경로는 모두 `Assets/@Resources/Sprites/` 기준이다(아니면 따로 적었다).
타이틀 장면 = `UI/UI_Title/ForShow/MainTitle_Animation_Animation 1_0.png`(480×270, 타이틀 화면 한 프레임).
로고 = `TitleImages/더 소드 화이트 수정본.png`(1482×498, 흰색) — 쓸 때마다 어두운 테두리와 그림자를 붙였다.

## 파일

| 파일 | 크기 | Steamworks 칸 | 원본과 만든 법 | 의뢰 그림이 오면 |
|---|---|---|---|---|
| `header_capsule_920x430.png` | 920×430 | Header capsule | 타이틀 장면 ×3, 검을 오른쪽 3분의 1에. 왼쪽을 어둡게 해 로고(500 px) | **바꾼다** (A) |
| `small_capsule_462x174.png` | 462×174 | Small capsule | 타이틀 장면 ×3 의 잎(검 오른쪽), 로고 410 px — 거의 꽉 찬다 | **바꾼다** (A) |
| `main_capsule_1232x706.png` | 1232×706 | Main capsule | 타이틀 장면 ×4, 로고 640 px | **바꾼다** (A) |
| `vertical_capsule_748x896.png` | 748×896 | Vertical capsule | 타이틀 장면 ×4 세로 자르기, 로고 640 px | **바꾼다** (A) |
| `library_capsule_600x900.png` | 600×900 | Library capsule | `UI/Intro/Intro05.PNG`(1920×4789, 인트로 삽화 — 검은 해를 꿰뚫은 탑과 망토 입은 사람) 1180~4060 행을 600×900 으로 축소, 로고 520 px. 로고의 검 글자가 탑 줄기에 겹친다 | **바꾼다** (A) |
| `library_header_920x430.png` | 920×430 | Library header | 헤더 캡슐과 같은 그림(Valve: 비슷한 그림을 쓰라) | **바꾼다** (A) |
| `library_hero_3840x1240.png` | 3840×1240 | Library hero | 타이틀 장면 ×8(정확히 3840 폭), 원본 36~190 행. **글자 없음** | **바꾼다** (A) |
| `library_logo_1280.png` | 1280×491, 투명 | Library logo | 로고만 1168 px + 어두운 테두리·부드러운 그림자(밝은 풀 위에서도 읽히게) | **바꾼다** (B, 언어별 로고) |
| `page_background_1438x810.png` | 1438×810 | Page background | `LoadingIllustration/ForestColorIllust.PNG`(채색 삽화, 숲의 검) 0.75 축소, 흐림 2.2, 밝기 42% | 바꿔도 되고 둬도 된다 |
| `community_icon_184.jpg` | 184×184 JPG | Community icon | 로고의 'O'(외눈 검 글자, 로고의 609~873 열)를 흰색으로, 타이틀 숲빛 녹색 바탕. 모서리 없음 | **바꾼다** (B) |
| `shortcut_icon_256.png` | 256×256 | Client icon (PNG) | 같은 디자인, 둥근 사각형 | **바꾼다** (B) |
| `shortcut_icon.ico` | 16·24·32·48·64·128·256 | Client icon (ICO) | 같은 디자인을 **크기마다 따로** 그렸다(작은 크기는 글자를 굵게) — 256 을 줄인 것이 아니다 | **바꾼다** (B) |
| `icons/ACH_*.png` · `icons/ACH_*_locked.png` | 256×256 ×40 | 업적 Achieved / Unachieved | 아래 표. 이름은 `../achievements.csv` 의 `icon_achieved`·`icon_unachieved` 그대로 | 손 도트(P)가 오면 바꾼다 |
| `achievements_contact.png` | 1700×1960 | (올리지 않는다) | 업적 40장 한눈에 + 64 px(Steam 목록 크기) 미리보기 | — |
| `../../Assets/@Resources/Art/Icon/TheSword_Icon_512.png` | 512×512 | 실행 파일 아이콘(PlayerSettings) | 앱 아이콘과 같은 디자인. **512 인 까닭**: 글자가 498 px 높이 로고에서 오므로 1024 로는 키워야 한다 | **바꾼다** (B) |

### 왜 이렇게 했나

- **캡슐에 예측 숫자를 넣지 않았다.** 게임의 간판은 몬스터 머리 위의 정확한 값이지만, 캡슐에는 게임 이름(과 공식 부제)
  말고 글자를 넣을 수 없다[S15]. 그리고 캡슐 위의 "−31" 은 할인 딱지로 읽힌다(아트 문서 ST 브리프 4). 숫자는 스크린샷
  (`Build/Store/screenshots/`)과 트레일러 첫 5초가 맡는다.
- **가로 캡슐·라이브러리 히어로는 타이틀 장면 하나에서 나왔다.** 그림이 하나라 일관되고, 픽셀이 게임 화면과 똑같이 또렷하다.
  대신 인물·몬스터가 없다 — 의뢰할 키 아트가 채울 몫이다.
- **라이브러리 캡슐만 탑 삽화다.** 2:3 세로가 그 그림을 가장 잘 담는다. 748×896 은 폭이 넓어 아래의 사람이 머리만 남아서
  세로 캡슐은 타이틀 장면으로 했다. 아트 문서는 `Intro05` 를 마검의 설계도(검은 해 = 외눈, 초승달 = 휜 날)로 본다.
- **라이브러리 히어로의 안전 영역**(가운데 860×380): 검 손잡이(고리 머리 ~ 가로날, 원본 89~137 행)가 ×8 에서 392 px 라
  380 에 다 들어가지 않는다. 가운데에 맞춰 위아래로 6 px 씩 넘친다 — ×8 에서 가장 가깝다.
- **로고는 영어 한 벌뿐이다**(있는 로고가 그것뿐). Steam 은 캡슐을 언어별로 올릴 수 있다 — 5개 언어 로고(B)가 오면 언어판을 만든다.
- **실행 파일 아이콘**: 지금 PlayerSettings 의 기본 아이콘은 `Sprites/Icon/icon_1024x1024.PNG`(사람이 그린 돌 문양 + 로고)다.
  그 그림은 32 px 에서 "황갈색 사각형에 SWRD", 16 px 에서는 형체가 없다. 새 아이콘은 16 px 에서도 외눈 검으로 읽힌다.
  어느 쪽을 쓸지는 소유자가 정한다 — 큰 크기의 풍성함은 옛 그림이 낫다.

## 업적 아이콘

틀: `EquipIllust/Illust_SwordBG_01.png`(마검 인벤토리 그림의 판) — 네 귀퉁이 64×64 를 128 격자로 모으고 ×2(256 px, 64 px 에서
테두리 4 px). 안쪽 무늬는 그 판의 30 px 주기 무늬를 다시 깔았다. 그 위에 주제색 빛(6단, Bayer 디더 — 도트식 명암).
주제 스프라이트에는 1 px 어두운 외곽선을 둘렀다. 잠긴 판은 같은 그림을 회색(밝기 ×0.58)으로.
보스는 게임과 같은 색을 입었다: `MonsterTint.ForBoss(챕터)`(= 챕터색 ×0.8) — 그림은 `MonsterData` 의 `IdleAnimStr`.

| API | 그림 | 원본 | 배율 | 바탕 |
|---|---|---|---|---|
| `ACH_CONTRACT` | 계약 팝업의 외눈 검 문장 | `UI/UI_Popup/PopupUI_MagicalSwordCheck.png` 위쪽 문장(팝업 판은 색으로 걷어냄) | ×2 | 숲 초록 |
| `ACH_KINGSLIME` | 킹 슬라임 | `MonsterSprites/BossMonsterSprite/Idle/Boss_C0_I000.png` 첫 칸 | 2/3 축소(BOX) | 금색 |
| `ACH_BOSS20` | 묘소의 늑대 | `MonsterSprites/Idle/Mob_004_Idle.png` (= `Mob_C0_I004`) + 챕터 0 색 | ×4 | 숲 초록 |
| `ACH_BOSS40` | 수로의 방패병 | `MonsterSprites/Idle/Mob_007_Idle.png` (`Mob_C0_I007`) + 챕터 1 색 | ×6 | 물빛 |
| `ACH_BOSS60` | 재의 파수꾼 | `MonsterSprites/Idle/Boss_C1_I000.png` (`Mob_C0_I008`) + 챕터 2 색 | ×4 | 잿불 |
| `ACH_BOSS80` | 얼어붙은 기사 | `MonsterSprites/Idle/Mob_006_Idle.png` (`Mob_C0_I006`) + 챕터 3 색 | ×5 | 서리 |
| `ACH_BOSS100` | 검은 태양 | `LegacySprites/Monster_Idle.png` (`Mob_C0_I009`) + 챕터 4 색 | ×2 | 보랏빛 |
| `ACH_END_SEAL` | 잠든 검이 꽂혀 있다 — 날이 틀 아래로 들어간다 | `TitleImages/MainTitle_Sword.png` | ×2 | 밤 파랑 |
| `ACH_END_HOLD` | 검을 높이 든 주인공 | `Player/UnitHero_ContractSword_F.png` 첫 칸 | ×3 | 핏빛 |
| `ACH_END_DAWN` | 에고소드 — 손잡이와 외눈은 안에, 날은 틀 밖으로 | `EquipIllust/Illust_Sword_01.png` | ×2 | 새벽 + 빛살 |
| `ACH_END_ALL` | 세 자루(파랑·빨강·금) | `EquipIllust/Illust_Sword_01·04·03.png` | ×1 | 세 갈래(밤·피·새벽 = 세 결말) |
| `ACH_CRIT_CARRY` | 전투창 무기의 치명 번쩍임 | `UI/UI_Battle/BattleUI_Weppon0.png` 셋째 칸 | ×4 | 붉은 주황 |
| `ACH_NO_SPILL` | 물약 | `Item/Tilemap_Portion3.png` 첫 칸 | ×6 | 파랑 |
| `ACH_SLIVER` | 쓰러지기 직전의 주인공 | `Player/UnitHero_Death.png` 넷째 칸 | ×5 | 핏빛 |
| `ACH_BOSS_BARE` | 보스방 팝업의 뿔 가면 문장 | `UI/UI_BossRoomCheckPopup/PopupUI_BossRoomCheck.png` 위쪽 문장 | ×2 | 보라 |
| `ACH_UNDER_PAR` | HUD 의 외눈 검 단추(예측) | `UI/MainUI/MainUI_Sword_A.png` | ×3 | 초록(싼 예측의 색) |
| `ACH_CH_3STAR` | 별 셋 | `Assets/@Resources/Font/Silver.ttf` 의 ★ 를 본래 크기(19 px, 11×11 도트)로 — 결산 창의 별 | ×5 | 금색 |
| `ACH_DAWN_PAR` | 같은 단추의 눌린(밝은) 판 | `UI/MainUI/MainUI_Sword_B.png` | ×3 | 새벽 + 빛살 |
| `ACH_TOWER` | 보스 문의 검은 해 | `BossPortal/BossPortal_Idle.png` 첫 칸 | ×3 | 핏빛 |
| `ACH_TOWER_DAWN` | 같은 검은 해, 금빛 테 | 〃 | ×3 | 새벽 + 빛살 |

- ★ 는 글꼴 Silver(Poppy Works, CC BY 4.0)의 글리프다 — `../THIRD_PARTY_NOTICES.txt` 1.1 에 이미 표기돼 있다.
- 숨김 업적 셋(`ACH_END_DAWN`·`ACH_DAWN_PAR`·`ACH_TOWER_DAWN`)도 두 장씩 있다. 풀리기 전에는 Steam 이 자기 "숨김" 표시를 보인다.
- 64 px 에서 가장 약한 것: `ACH_BOSS60`·`ACH_BOSS100`(거의 검은 그림이라 실루엣과 빛 테두리로 읽힌다), `ACH_END_ALL`(×1 도트라 가늘다).

## AI 공개와의 관계

그림 속 모든 픽셀은 사람이 그린 프로젝트 그림(과 도형)이지만, **무엇을 어디서 잘라 어떻게 놓을지는 Claude(AI)가 스크립트로 정했다.**
Content Survey 의 AI 공개 칸에 적을지는 소유자가 정한다(`../README.md` §7). 의뢰 그림으로 바꾸면 이 문제는 사라진다.

## 다시 만들기

`gen.py`(Python + Pillow, `python -I gen.py [ach] [caps] [icons]`)가 위 전부를 결정적으로 다시 만든다. 이 작업은 그림과 이
README 만 쓰기로 해서 스크립트는 저장소 밖(작업 세션의 임시 폴더)에 있다 — 저장소에 둘지는 소유자가 정한다.
스크립트가 없어도 위 표의 원본·배율·자르기로 손으로 다시 만들 수 있다.

[S15] https://partner.steamgames.com/doc/store/assets · https://partner.steamgames.com/doc/store/assets/libraryassets
