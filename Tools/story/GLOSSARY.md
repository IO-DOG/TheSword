# TheSword 용어집 (번역가용)

원문은 `story_kr.json` 하나다. 이 표의 번역은 **제안**이다. 다만 한 번 정한 번역은 모든 줄에서 같게 쓴다.
특히 8절의 메아리 대사는 수십 층을 건너 되받는 복선이다. 같은 말로 옮겨야 복선이 살아난다.

- ☆ 표시는 이미 게임 데이터(`Tools/bestiary.py` 내장 문구, ScriptData)에 들어 있는 번역이다. 바꾸려면 그쪽도 함께 바꾼다.
- 번역 파일을 다 채우면 `python check_story.py en`(jp, cn)으로 빠진 키를 확인한다.

## 1. 인물

| 한국어 | 누구 | EN | JP | CN |
|---|---|---|---|---|
| 데미안 | 주인공. 후드 쓴 용병 | Damian | ダミアン | 达米安 |
| 에고소드 | 말하는 마검. 화자 이름 | Ego Sword | エゴソード | 自我之剑 |
| 마검 | 에고소드를 가리키는 일반 명사 | the demon sword | 魔剣 | 魔剑 |
| 촌장 | 가브 마을 촌장. 이름이 없다 | Village Chief | 村長 | 村长 |
| 렌 | 촌장의 아들. 두 번째 계약자 | Ren | レン | 伦 |
| 브람 | 왕국 조사관. 세 번째 계약자 | Bram | ブラム | 布拉姆 |
| 엘린 | 네 번째 계약자. 이름은 80층 대사에만 나온다 | Elin | エリン | 艾琳 |
| 세드릭 | 마지막 탑지기이자 첫 계약자 | Cedric | セドリック | 塞德里克 |
| 대장장이 | 마검을 벼린 이(무대 밖) | the Smith | 鍛冶師 | 铁匠 |
| 탑지기 | 왕좌 문을 지키던 기사들 | Towerwarden | 塔守 | 守塔人 |
| 검은 태양 | 탑이 꿰어 두었던 허기. 100층 보스 | the Black Sun | 黒い太陽 | 黑太阳 |

## 2. 자칭과 부름말

말투의 뼈대다. 누가 누구를 어떻게 부르는지가 곧 이야기의 진행이다.

| 한국어 | 누가 → 누구 | 주의 | EN | JP | CN |
|---|---|---|---|---|---|
| 이몸 | 마검의 자칭 | 거만한 옛말. 마검은 "나/저" 를 한 번도 쓰지 않는다. 기존 EN "This body" 는 버린다 | I (grand, archaic diction); "this blade" at key beats | 我輩 | 本大爷 |
| 허접 모험가 / 허접 | 마검 → 데미안 | 3층에서 데미안이 스스로를 낮춘 말(개작 100019)을 마검이 주워 별명으로 굳힌다. 61~89층에는 쓰지 않는다 | two-bit adventurer / Two-bit | ヘボ冒険者 / ヘボ | 三流冒险者 / 三流 |
| 잠꾸러기 | 데미안 → 마검 | 16층에서 생긴다 | Sleepyhead | 寝ぼすけ | 瞌睡虫 |
| 너 | 서로 | | you | お前 | 你 |
| 용병 | 세드릭 → 데미안 | 세드릭은 이름을 모른다 | mercenary | 傭兵 | 佣兵 |
| 그 검 | 세드릭 → 마검 | | that sword | その剣 | 那把剑 |
| 그 칼 | 촌장 → 마검 | 촌장은 일상어 "칼" 을 쓴다 | that blade | その剣 | 那把剑 |
| 영감 | 마검 → 촌장 | 새벽 후일담에서만 | old man | じいさん | 老头 |
| 데미안(부름) | 마검 → 데미안 | `ending_choice.11` 이 처음이다. 그 전에는 누구도 이름을 부르지 않으니 번역문에도 이름을 덧붙이지 않는다 | Damian | ダミアン | 达米安 |

## 3. 곳

| 한국어 | 설명 | EN | JP | CN |
|---|---|---|---|---|
| 가브 마을 | 탑이 떨어진 마을 | Gabu Village ☆ | ガブ村 | 加布村 |
| 탑 | 거꾸로 박힌 검은 탑 | the Tower | 塔 | 塔 |
| 왕좌 | 탑 꼭대기. 땅속 가장 깊은 곳 | the Throne | 玉座 | 王座 |
| 이끼 낀 지하 묘소 | 챕터 0 (5~20층). 대사에서는 "묘소" | Mossy Catacombs ☆ | 苔むした地下墓所 ☆ | 苔藓地下墓穴 ☆ |
| 무너진 수로 | 챕터 1 (21~40층). "수로", "물길" | Collapsed Aqueduct ☆ | 崩れた水路 ☆ | 坍塌水道 ☆ |
| 잿빛 용광로 | 챕터 2 (41~60층). "용광로" | Ashen Furnace ☆ | 灰色の溶鉱炉 ☆ | 灰烬熔炉 ☆ |
| 얼어붙은 심층 | 챕터 3 (61~80층) | Frozen Depths ☆ | 凍てついた深層 ☆ | 冰封深层 ☆ |
| 왕좌의 균열 | 챕터 4 (81~100층). "균열", "틈" | Rift of the Throne ☆ | 玉座の亀裂 ☆ | 王座裂隙 ☆ |
| 모험가 길드 / 길드 게시판 | 인트로 | Adventurers' Guild / guild board | 冒険者ギルド / 掲示板 | 冒险者公会 / 告示板 |
| 왕국 | 의뢰를 건 나라 | the Kingdom | 王国 | 王国 |

**챕터 부제** (`bestiary.chapters[].subtitle`)

| 한국어 | EN | JP | CN |
|---|---|---|---|
| 탑이 삼킨 옛 무덤 | The Old Graves the Tower Swallowed | 塔が呑んだ古き墓 | 被塔吞没的古墓 |
| 하늘의 비가 흐르던 길 | Where the Sky's Rain Once Ran | 空の雨が流れた道 | 天雨曾流经之路 |
| 무언가를 벼리던 불 | The Fire That Forged Something | 何かを鍛えた火 | 曾锻造某物之火 |
| 온기마저 먹힌 곳 | Where Even Warmth Was Eaten | ぬくもりさえ喰われた場所 | 连温暖都被吞噬之地 |
| 검은 해가 파고드는 틈 | The Rift the Black Sun Bores Into | 黒い陽が食い込む裂け目 | 黑日钻入的裂隙 |

## 4. 몬스터

게임은 이름을 **"{접두어} {종}"** 으로 만든다. EN 은 띄어 쓰고, JP·CN 은 붙인다(`bestiary.py`의 `JOIN`).
보기: 이끼 늑대 → Moss Wolf / 苔むした狼 / 苔藓狼. 다른 몬스터와 이름이 겹치면 생성기가 멈춘다.

**접두어** ☆: 이끼 Moss 苔むした 苔藓 · 수렁 Mire 沼の 泥沼 · 잿불 Ember 熾火の 余烬 · 서리 Frost 霜の 寒霜 · 심연 Abyssal 深淵の 深渊

**종** ☆

| art | 한국어 | EN | JP | CN |
|---|---|---|---|---|
| 0 | 라임 슬라임 | Lime Slime | ライムスライム | 青柠史莱姆 |
| 1 | 망고 슬라임 | Mango Slime | マンゴースライム | 芒果史莱姆 |
| 2 | 크로우 | Crow | クロウ | 乌鸦 |
| 3 | 정령 | Spirit | 精霊 | 精灵 |
| 4 | 늑대 | Wolf | 狼 | 狼 |
| 5 | 고블린 창병 | Goblin Spearman | ゴブリン槍兵 | 哥布林枪兵 |
| 6 | 해골 전사 | Skeleton Warrior | 骸骨戦士 | 骷髅战士 |
| 7 | 고블린 방패병 | Goblin Shieldbearer | ゴブリン盾兵 | 哥布林盾兵 |
| 8 | 파수꾼 | Sentinel | 番兵 | 哨兵 |
| 9 | 거수 | Behemoth | 巨獣 | 巨兽 |

**챕터 보스.** 화면 이름은 스포일러가 없어야 한다. 정체(렌·브람·엘린·세드릭)는 대사 안에서만 밝힌다.
`bestiary.py` 의 옛 내장 보스 이름("묘지기 늑대" 등)은 이 이름으로 바뀐다.

| 층 | 이름 / 칭호 | EN | JP | CN |
|---|---|---|---|---|
| 20 | 묘소의 늑대 / 해 질 녘을 기다리는 것 | Wolf of the Catacombs / The One Who Waits for Dusk | 墓所の狼 / 日暮れを待つもの | 墓穴之狼 / 等待黄昏之物 |
| 40 | 수로의 방패병 / 내리지 않는 방패 | Shieldbearer of the Aqueduct / The Shield Never Lowered | 水路の盾兵 / 下ろされぬ盾 | 水道之盾兵 / 从不放下的盾 |
| 60 | 재의 파수꾼 / 타다 남은 목소리 | Sentinel of Ash / The Voice That Did Not Burn | 灰の番兵 / 燃え残りの声 | 灰之哨兵 / 燃剩的声音 |
| 80 | 얼어붙은 기사 / 마지막 문의 파수 | The Frozen Knight / Warden of the Last Door | 凍てついた騎士 / 最後の扉の番 | 冰封骑士 / 最后之门的守卫 |
| 100 | 검은 태양 / 끝없는 허기 | The Black Sun / Endless Hunger | 黒い太陽 / 果てなき飢え | 黑太阳 / 无尽的饥饿 |
| 4 | 킹 슬라임 (분열: 메이스·대거·방패) | King Slime (mace, dagger, shield) | キングスライム (メイス・ダガー・盾) | 史莱姆王 (钉锤·匕首·盾) |

## 5. 특성 ☆

| id | 한국어 | EN | JP | CN | 도감 설명 속 말 |
|---|---|---|---|---|---|
| 0 | 없음 | None | なし | 无 | |
| 1 | 야수 | Beast | 野獣 | 野兽 | 일어선다 = rises again |
| 2 | 마법 | Magic | 魔法 | 魔法 | |
| 3 | 수호 | Guardian | 守護 | 守护 | 방패 = 방어 상태 (shield up) |
| 4 | 불사 | Immortal | 不死 | 不死 | |
| 5 | 검사 | Swordsman | 剣士 | 剑士 | |
| 6 | 거대 | Titan | 巨体 | 巨躯 | 포효·운다 = roar |
| 7 | 암살 | Assassin | 暗殺 | 暗杀 | 숨다 = hide, 모습을 드러내다 = reveal itself |
| 8 | 갑옷 | Armor | 鎧 | 铠甲 | 껍질 = shell |

## 6. 게임 용어와 마검의 말

대사에는 HP·레벨·경험치·스탯·데미지·크리티컬·버프·쿨타임·체크포인트를 쓰지 않는다. 아래 왼쪽 말로 대신한다.
도감(`bestiary`)은 UI 문구라서 "치명타", "체력", "방어력" 을 그대로 쓴다.

| 한국어 | 뜻 | EN | JP | CN |
|---|---|---|---|---|
| 값 | 전투 예측. 그 싸움에서 잃을 체력. 이야기 전체의 핵심어 | price | 値段 | 价码 |
| 얼마? | 데미안의 입버릇. 둘 사이의 온도계 | How much? | いくらだ？ | 多少？ |
| 숫자 | 몬스터 머리 위 예측 숫자 | the number | 数字 | 数字 |
| ✖ / 빨간 가위표 | 지면 죽는 싸움. 기호는 그대로 둔다. 주황 숫자(비싸도 산다)와 구별한다(`mech_fatal.01`) | ✖ / the red X | ✖ / 赤いバツ | ✖ / 红叉 |
| 진심 | 치명타(마검의 말). `mech_crit.02` 에서 데미안이 "치명타 말이야?" 로 잇는다 | in earnest / an earnest cut | 本気 | 真格 (动真格) |
| 치명타 | UI·도감 용어 | critical hit | 会心の一撃 ☆ | 暴击 |
| 숨 | 치명 주기, 곧 세던 횟수. 싸움이 끝나도 이어진다 | breath | 息 | 呼吸 |
| 평타 | 치명타가 아닌 보통 공격 | a plain hit | 通常攻撃 | 普通攻击 |
| 크다 / 컸다 | 레벨업 | grow | 大きくなる | 长大 |
| 먹다 / 먹이다 | 마검이 마물의 마력을 먹는 것(경험치) | eat / feed | 食う / 食わせる | 吃 / 喂 |
| 배고프다 / 배부르다 | 참 결말 조건(레벨)의 말 | hungry / full | 腹が減る / 満腹 | 饿 / 饱 |
| 문턱 | 그 층에 처음 들어선 자리. 되살아나는 곳. 탑 안이다(`ending_seal.09` 의 "탑 밖까지" 와 구별) | threshold | 敷居 | 门槛 |
| 탑의 글자 | 룬(마검의 말) | the Tower's glyph | 塔の文字 | 塔之文字 |
| 룬 | 공격·방어·체력 룬 | Rune ☆ | ルーン ☆ | 符文 ☆ |
| 강타 · 철벽 · 흡혈 | 스킬 셋. UI 와 같게 (코드 이름 Smash·Guard·Drain) | Smash · Bulwark · Drain (방패 4042 "Iron Wall" 과 겹치지 않게) | 強打 · 鉄壁 · 吸血 | 强击 · 铁壁 · 吸血 |
| 물약 | 밟는 즉시 마신다 | potion | ポーション | 药水 |
| 열쇠 (초록·노랑·빨강) | 같은 색 문 하나 | key (green / yellow / red) | 鍵 (緑・黄・赤) | 钥匙 (绿·黄·红) |
| 여분 열쇠 | 골방 파수꾼 뒤의 열쇠 | spare key | 予備の鍵 | 备用钥匙 |
| 금고 | 마지막 구역의 네 번째 문. 안에 룬 | vault | 金庫 | 金库 |
| 골방 | 입구가 한 칸뿐인 막다른 곳 | alcove | 小部屋 | 小隔间 |
| 구역 | 문이 잘라 놓은 층의 한 부분 | zone | 区画 | 区域 |
| 곁길 | 지나쳐도 되는 길 | side path | 脇道 | 岔路 |
| 둘 중 하나 | 하나를 집으면 짝이 사라지는 보상 | either-or reward | 二者択一 | 二选一 |
| 층 유형: 기본·인색·관문·넉넉·보물 | 다섯 층 주기 | Basic · Stingy · Gate · Plenty · Treasure | 基本・倹約・関門・潤沢・宝物 | 普通・吝啬・关卡・富足・宝藏 |
| 워프석 반지 | 20층 보스가 떨군다 | Warpstone Ring ☆ | ワープ石の指輪 ☆ | 传送石戒指 ☆ |
| 몬스터 도감 / 책 | 마검의 눈이 알아보는 것을 옮긴 책. 마검은 대사에서 "책" 이라 부른다(`mech_forecast.06`) | Monster Manual ☆ / the book | モンスター図鑑 ☆ / 本 | 怪物图鉴 ☆ / 书 |
| 계약 / 계약서 / 계약자 | | contract / the contract / contractor | 契約 / 契約書 / 契約者 | 契约 / 契约书 / 契约者 |
| 영원한 동반자 | 계약서 조항 (hold 결말이 되받는다) | eternal companion | 永遠の伴侶 | 永恒的伴侣 |
| 파기 불가 | 계약서 조항 (80층·새벽 결말이 되받는다) | irrevocable | 破棄不可 | 不可废除 |
| 자유의지로 서명 | 계약서 끝 | signed of one's own free will | 自由意志により署名 | 以自由意志签署 |
| 자물쇠 | 마검의 정체. 왕좌에 꽂혀 검은 태양을 붙들던 것 | the Lock | 錠 | 锁 |
| 제 발로 앉다 | 마검에 묶인 넋이 왕좌를 잠그는 유일한 방식. 묶이지 않은 넋은 앉아도 받지 않는다 | sit of one's own accord | 自らの足で座る | 自愿坐上 |
| 넋 | 영혼. 세드릭과 마검의 말("그 검에 묶인 넋") | soul | 魂 | 魂魄 |
| 도로 부르다 | 빈 왕좌가 자물쇠를 끌어당기는 것. 100층 "돌아와라" 와 같은 끌림이다 | call back | 呼び戻す | 召回 |
| 하사품 / 하사품 공고 | 왕국이 내리는 상 / 게시판에 남은 그 공고(`epilogue_hold.04` `epilogue_dawn.07`) | royal reward / reward notice | 下賜品 / 下賜品の告示 | 赏赐 / 赏赐告示 |
| 의뢰 / 의뢰서 | 길드의 낡은 의뢰. 의뢰서는 데미안이 떼어 품었다(900005). 하사품 공고와 다르다 | request / request slip | 依頼 / 依頼書 | 委托 / 委托书 |
| 왕국 조사관 | 브람의 직함 | Royal Investigator | 王国調査官 | 王国调查官 |
| 가브 풍습 | 전사의 무덤에 그가 쓰던 칼을 꽂는다 | the Gabu custom | ガブの習わし | 加布的习俗 |
| 뱉다 / 뱉어 내다 | 탑이 돌아오지 않는 자의 칼(과 반지)을 밑동 무덤 사이로 내놓는 것(`village_chief.05` `epilogue_hold.02`) | spit out | 吐き出す | 吐出 |

## 7. 말투

| 화자 | 한국어 | EN | JP | CN |
|---|---|---|---|---|
| 마검 | 거만한 해라체(~다·~군·~냐·~마·~라), "크하핫!". 진심일 때는 웃음이 빠지고 짧아진다 | grand, blustering, food metaphors | 我輩＋尊大な口調 | 本大爷＋粗豪口吻 |
| 데미안 | 짧은 반말, 건조함. 촌장에게만 해요체. 느낌표를 거의 안 쓰고 "고마워·미안해" 를 말하지 않는다. 어미는 "-지·-네·-어", "-군" 은 처음 깨달을 때만. 마검처럼 "~나?" 로 묻거나 규칙을 "~는다" 로 선언하지 않는다 | terse and dry | 俺、ぶっきらぼう（村長には丁寧語） | 简短冷淡（对村长用敬语） |
| 촌장 | 하오체. 느리고 짧고 "…" 가 많다. 느낌표 없음 | slow, old-fashioned formal | 老人語（〜じゃ、〜のう） | 老者口吻 |
| 렌 | 끝맺지 못하는 부서진 혼잣말 | broken, trailing off | 途切れ途切れ | 断断续续 |
| 브람 | 보고서처럼 끊는 "~다." | clipped report style | 報告書調 | 报告体短句 |
| 엘린 | 두세 낱말 조각뿐. 온전한 문장은 한 번도 없다 | fragments only | 単語の断片だけ | 只有词语碎片 |
| 세드릭 | 느리고 무거운 해라체 | slow, heavy, archaic | 重く古風な口調 | 沉重古朴 |
| 검은 태양 | 두세 낱말을 되풀이 | two or three words, repeated | 二、三語の反復 | 两三个词反复 |
| 내레이션 | 과거형, 담백함 | plain past tense | 簡潔な過去形 | 简洁的叙述 |

**의성어·감탄사**: 크하핫! Kahaha! クハハッ！ 咔哈哈！ · …음냐 *mumble* むにゃ… 呼噜… · 킁, 킁 sniff, sniff クンクン 嗅嗅 ·
우물우물 munch munch もぐもぐ 吧唧吧唧 · 퉤, 퉤! Ptoo! ペッ、ペッ！ 呸、呸！ · 찌익 rrrip ビリッ 嘶啦

## 8. 메아리 대사 (같은 번역을 되풀이할 것)

| 한국어 | 처음 | 되받는 곳 |
|---|---|---|
| 숫자부터 봐라 | 개작 100031 | `pro_kingslime_clear.01`~`02` `bark_gate_1.01` |
| 해 지기 전에 | `village_chief.08` | `f16_nap.01` `boss0_defeat.01` `boss0_defeat.08` `mech_warp.02`, 도감 `warp_ring` |
| 이몸을 만난 건 행운이다 | 개작 100026 | `boss3_intro.08` |
| 배부르면? / 그런 날은 안 온다 | `pro_contract_after.05`~`06` | `ending_dawn.05` |
| 네 목숨은 이몸 거다 / 이몸 허락 없이는 못 죽는다 | `pro_contract_after.04` | `mech_death.02` `boss3_defeat.15` |
| 다음 한 대는 진심으로 벤다 | `mech_crit.01` | `boss3_defeat.01` ("진심이었군") |
| 세던 숨은 이어진다 | `mech_crit.04` | `f90_rule.01` `f100_lesson.05` |
| 누구부터 베느냐가 값 | `mech_crit.04` | `trait_assassin.02` `f100_lesson.04`, 도감 `traits[7]` |
| 비싼 놈은 나중에 | `mech_levelup.02` | `f90_rule.01` (`bark_basic_3.01`) |
| 아직 멀었다 → 충분하다 | `mech_levelup.04` `f31_trust.06` `f50_leftovers.10` | `f72_enough.02`~`03` `boss3_defeat.08` |
| 배고픈 놈은 먹히고, 배부른 놈이 먹는다 | `f11_motto.02` | `ending_hold.04` `ending_dawn.09` |
| 눈 뜨고 잔다 | `f16_nap.04` | `boss3_defeat.12` ("눈 한 번 못 감고") `ending_seal.04` |
| 넌 해 지기 전에 돌아갈 데 있나? / 없어. / …이몸도 없다. | `boss0_defeat.08`~`10` | `f76_nohome.01` `boss3_intro.16` `ending_hold.07`~`08` |
| …세드릭… 문 열어… | `f27_sleeptalk.02` | `boss3_intro.02`~`03` ("두 번째다") |
| 굶주림은 굶주림으로만 묶인다 | `f45_forge.01` | `boss3_intro.13` `f95_price.02` |
| 넌 내 이름도 안 물었지 / 필요 없다 | `f50_leftovers.07`~`08` | `ending_choice.11`~`13` |
| …네… 값… | `boss2_defeat.01` | `boss3_intro.10` |
| 값을 먼저 물어보시오 → 얼마야, 나. | `village_chief.11` | `boss3_defeat.07` `ending_dawn.14` |
| 그 검이 네 이름을 부르거든, 그땐 끝이다 | `boss3_intro.14` | `ending_choice.11` `ending_hold.10` `ending_dawn.11`~`12` |
| 이번엔, 놓지 마라 | `boss3_defeat.03` | 선택지 `ending_choice.hold` "검을 놓지 않는다" |
| 계약은 둘 중 하나가 부서져야 끝난다 | `boss3_defeat.14` | `ending_choice.07` `ending_dawn.16` ("배고픈 이몸은 방금 부서졌으니까") |
| 파기 불가라며 | `boss3_defeat.22` | 계약서(개작 8)의 ◀파기 불가▶, `ending_dawn.17` |
| 영원한 동반자 | 계약서(개작 8) | `ending_hold.09` |
| 제 발로 (앉다) | `boss3_intro.12` | `boss3_defeat.19` `f90_rule.05` `ending_hold.07` |
| 빈 왕좌는 제 자물쇠를 도로 부른다 | `boss3_intro.12` | `boss4_intro.02` `boss4_intro.05` ("돌아와라") |
| 넘친 만큼은 버려졌다 | `mech_overflow.01` | `f90_rule.01` ("넘치면 버려지니") `ending_dawn.07` `ending_dawn.10` `ending_dawn.13` |
| 배부른 것만이 굶주림을 끝낸다 | `f95_price.03` | `ending_dawn.09`~`10` |
| 사람은 안 돌아오오. 칼만 탑이 뱉어 내지 | `village_chief.05` | `epilogue_hold.02` ("뱉어져 있었다"), 도감 `0:6` |
| 값은 머리 위에 떠 있다 | `mech_forecast.05` | `boss0_intro.06` ("머리 위에 떠 있잖나") |
| 얼마? | `mech_forecast.04` | `boss0_intro.05` `f31_trust.01` `boss2_intro.05` `boss3_defeat.07` `f95_price.08` `boss4_intro.07` `ending_dawn.14` `epilogue_dawn.08` |
| 무덤이야. 먹지 마. | `village_chief.07` | `pro_contract_after.02` "내 칼은 먹지 마." 와 짝 |

## 9. 말장난과 함정

- **허접 → two-bit.** 값 모티프와 겹치게 고른 제안이다. 몸값이 싸다는 뜻이 함께 들린다.
- **저녁밥** (`boss0_intro.06`). 늑대가 "해가 진다" 고 한 바로 뒤다. 해 질 녘은 저녁밥 때다.
- **묻다** (`epilogue_hold.03`). "반지를 묻었다(bury)" 와 "이름은 끝내 묻지 못했다(ask)" 가 짝이다.
  둘 다 살리기 어려우면 뒤쪽을 지킨다. 촌장의 약속(`village_chief.10`)을 되받는 줄이다.
- **눈** (도감 `3:8`). 눈(雪)을 뒤집어쓰고도 붉은 눈(目)만은 식지 않았다.
- **칼과 검.** 둘 다 sword 다. "칼" 은 일상어(촌장, 가브 풍습)고 "검" 은 격식어(세드릭, 마검)다.
  `ending_seal.02` 의 "제가 쓰던 칼" 은 마검이고, `epilogue_seal.07` 의 "허리에 차던 제 칼" 은 데미안의 평범한 칼이다.
- **위와 발밑.** 탑 안의 "머리 위" 는 늘 검은 태양 쪽이다(5층 잎, 21층 물, 81층 티끌). 땅거죽(숲)은 발밑이다. "머리 위" 를 surface 쪽으로 옮기지 않는다.
- **뱉다** (`village_chief.05`). 탑이 무덤 사이로 칼을 내놓는다는 뜻이다. 토한다(vomit)로 옮기지 않는다.
- **검은 태양 / 검은 해 / 까만 해.** 모두 같은 것이다. "검은 태양" 은 이름(보스, 세드릭)이고 "검은 해" 는 서술이다.
  "까만 해" 는 4층에서 마검이 처음 떠올린 흐릿한 그림이라 이름이 아니라 모양으로 옮긴다(a black sun).
- **주어 생략.** `boss2_defeat.08` "녹이려다 그 꼴이 됐으니" 의 주어는 엘린이다. 마검은 자신을 "이몸" 으로만 부른다.
- **줄 수.** 대사 한 항목은 최대 3줄이다. 개작(`prologue_rewrites`)은 기존 연출이 줄 수에 맞춰져 있어 한국어와 같은 줄 수를 지킨다.
- **크레딧.** "TheSword" 와 제작진 이름은 옮기지 않는다. "제작" 은 Created by / 制作 / 制作.
