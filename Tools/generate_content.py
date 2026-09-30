"""TheSword 100층 콘텐츠 생성기.

산출물:
  Assets/@Resources/Data/Excel/{PlayerData,MonsterData,StageInfoData,ScriptData,ConsumableItemData}.csv
  Assets/@Resources/Data/JsonData/{...}.json          <- 런타임이 실제로 읽는 파일
  Assets/@Resources/Data/JsonData/MonsterData_Tower.json <- 탑의 법 몬스터 표 (같은 줄, 더 비싸다)
  Assets/StreamingAssets/Data/Excel/Dungeon_CC_FFF.csv <- 층 레이아웃 100장

설계 규칙 (사용자 지정):
  - 총 100층, 20층마다 테마(챕터) 변경 -> 챕터 00~04
  - 미로형이지만 "순서"가 핵심. 몬스터를 정해진 순서로 잡아야 레벨/HP가 맞는다.
  - 층의 몹을 다 잡으면 정확히 1레벨 오른다.

안전 규칙:
  - 기존 데이터 행은 절대 덮어쓰지 않는다 (번역 스크립트/챕터0 보스 보존).
    새 항목만 충돌하지 않는 ID 대역에 추가한다.
  - **1~4층은 손수 만든 원본이다.** 튜토리얼 / 마검 계약 / 킹슬라임 연출이
    DirectingManager 에서 그 층의 특정 오브젝트 이름(Items/CItem13, SpawnKingSlime …)을
    직접 찾기 때문에, 레이아웃을 새로 만들면 NullReference 로 인트로가 통째로 깨진다.
    생성은 5층부터. 5층의 목표 레벨은 1~4층을 실제로 시뮬레이션해서 얻는다.
"""

import collections
import csv
import functools
import json
import os
import random
import statistics

import bestiary
from ui_text import TEXT as UI_TEXT, append_rows, emit_bootstrap

from thesword_balance import (
    Creature, extend_player_table, exp_to_next, load_player_table,
    player_stats_at, simulate_battle,
    NONE, BEAST, MAGIC, GUARDIAN, IMMORTAL, KNIGHT, TITAN, ASSASSIN, ARMOR,
    TRAIT_NAME,
)
from layout_gen import (build_floor_layout, validate_layout, check_doors,
                        check_sealed_by_portal, check_vault_safe, check_behind_boss,
                        floor_choices, key_economy, KEY_ITEM, MIN_TOLLS)

# 층마다 반드시 치러야 하는 전투의 수. layout_gen 이 배치로 보장하는 값이고,
# 여기서는 "곁길을 전부 건너뛴다" 는 나쁜 선택을 재현할 때 쓴다 — 관문 셋만
# 잡고 나머지를 지나치면 층당 경험치의 5분의 2 를 버리는 셈이다.
FORCED_PER_FLOOR = MIN_TOLLS

# ConsumableItemData 의 회복% 와 짝. <b>여기 있는 값만 쓸 수 있다</b> —
# 표에 없는 비율을 예산에 적으면 시뮬레이터와 실제가 어긋난다. 실제로 0.15 가
# I_03(20%) 로 놓이고 있었고, 그래서 층당 예산을 10%p 낮게 세고 있었다.
POTION_BY_HEAL = {0.20: "I_03", 0.30: "I_04", 0.40: "I_05",
                  0.50: "I_06", 0.60: "I_07"}
from mapdata_gen import emit_mapdata

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
EXCEL = os.path.join(ROOT, "Assets", "@Resources", "Data", "Excel")
JSOND = os.path.join(ROOT, "Assets", "@Resources", "Data", "JsonData")
STREAM = os.path.join(ROOT, "Assets", "StreamingAssets", "Data", "Excel")

TOTAL_FLOORS = 100
FLOORS_PER_CHAPTER = 20
MOBS_PER_FLOOR = 5
# CurExp 세터가 PlayerDic[Level+1] 을 읽는다. 완주 레벨보다 4 이상 넉넉해야 한다 —
# build_all 과 validate_content 가 확인한다.
MAX_LEVEL_TABLE = 120
LEVEL_HEADROOM = 4
NEW_MONSTER_ID_BASE = 100      # (예전 대역. 아래 표로 대체됨)
# 층마다 몹이 5종이라 대역을 넉넉히 잡는다. 층 F 의 k 번째 몹 = BASE + F*8 + k.
MOB_ID_BASE = 1000             # 1040 ~ 1804
BOSS_ID_BASE = 900             # 900 ~ 904
MOB_NAME_BASE = 11000
MOB_DESC_BASE = 21000
BOSS_NAME_BASE = 10900
BOSS_DESC_BASE = 20900
SCRIPT_STAGE_NAME_BASE = 5100  # 기존 5000~5004 뒤
SCRIPT_MON_NAME_BASE = 10100   # 기존 10000~10008 뒤
SCRIPT_MON_DESC_BASE = 20100   # 기존 20000~20008 뒤

# 층당 전투 목표치 (플레이어 최대 HP 대비 손실 비율, 전투 지속 시간 초)
# 몹 5마리 * 최대 9.5% = 약 47%, 여기에 포션 2개(각 30%)로 층당 순회복이 되게 잡는다.
# 층의 몹은 뒤로 갈수록 아프다. 미로가 나무 구조라 경로가 유일하고 문·열쇠가
# 구역을 자르므로, 이 순서대로 만날 수밖에 없다 — 그게 이 층의 "정답 경로"다.
#
# 다섯 자리가 전부 다른 종이면 "이 놈에겐 공격 1점이 몇 대 값이다" 를 셀 수가
# 없다. 한 번 싸우고 마는 상대의 값은 계산할 이유가 없기 때문이다. 그래서
# 앞의 세 자리를 한 종으로 묶는다 — 같은 놈을 세 번 만나니 공격 +1 이 세 번
# 값을 하고, 거기서 "몇 점을 올리면 한 대가 준다" 는 임계가 생긴다.
# (원형인 매직 타워도 한 종을 평균 2.85 마리씩 세운다.)
#
# 묶인 자리는 손실 배수가 같아야 한다 — 스탯이 다르면 같은 놈이 아니다.
# 총합 5.10 은 예전(0.72+0.88+1.00+1.16+1.34)과 같아서 층당 1레벨은 그대로다.
MOB_LOSS_RAMP = [0.85, 0.85, 0.85, 1.05, 1.50]

# 같은 종이 몇 마리씩 연달아 서는가. 합은 MOBS_PER_FLOOR.
# 마지막 한 자리는 정예다 — 색이 진하고 몸집이 크므로(MapBuilder.MonsterBulk)
# 혼자 세워야 그 표시가 거짓말이 되지 않는다.
MOB_SPECIES_RUN = [3, 1, 1]
assert sum(MOB_SPECIES_RUN) == MOBS_PER_FLOOR
assert len(MOB_LOSS_RAMP) == MOBS_PER_FLOOR
# 묶인 자리끼리는 배수가 같아야 같은 놈이 된다.
_i = 0
for _run in MOB_SPECIES_RUN:
    assert len(set(MOB_LOSS_RAMP[_i:_i + _run])) == 1, MOB_LOSS_RAMP
    _i += _run

# 마검(공격 +10, 공속 +0.5)을 계산에 넣고 다시 조율한 값.
# 예전 0.042 는 마검 없이 잰 것이라 실제 전투는 1~7% 였다(예측이 늘 초록).
# 마검을 넣으면 0.042 그대로도 설계만큼(몹 한 마리 약 5%) 아프지만, 예측을 볼
# 이유가 되게 한 단계 올린다 — 몹 중앙값 약 6%·상위 10% 약 8%·보스 35%.
# 그래도 곁길·룬·물약을 거르면 11~17층에서 죽고, 실수는 12번 봐준다(route_check).
# 물약 하나만 버려도 죽는 층은 51·56·71·76·96층 다섯이다 — 전부 물약이 하나뿐인 인색 층이다.
#
# 전투 길이는 16초 -> 12초(보스 45 -> 34)다. 몬스터 방어를 올리면(아래 MONSTER_DEF_RATIO)
# 한 대가 가벼워져 같은 16초면 보는 시간이 171 -> 222분으로 는다. 12초면 1배속으로
# 약 138분 — 방어를 올린 값(임계)은 받고 기다리는 시간은 예전(171분)보다 짧다.
MOB_HP_LOSS = 0.060
MOB_DURATION = 12.0
BOSS_HP_LOSS = 0.35
BOSS_DURATION = 34.0

# 탑의 법 — 같은 지도·같은 물약, 더 비싼 몬스터(MonsterData_Tower.json). 새 게임에서 고른다.
#
# 물약을 줄이는 쪽(치유 x0.65~0.7)은 재 보니 보이는 대로 마시는 사람의 최저 HP 가 62~65% 로
# 그대로였고, 0.55 에서야 물었다가 0.5 에서 20층에 막혔다 — 비탈이 아니라 절벽이었다.
# 몬스터 손실을 올리면 넘치던 물약이 제값을 한다. 0.104 에서 보이는 대로 마셔도 챕터 1~4 의
# 최저 HP 가 30~38% 이고 물약을 18개 버려도 완주한다. 0.107 이면 버틸 실수가 9 로 문턱 아래고
# 0.11 이면 100층에서 죽는다 — 여유가 3% 뿐이다.
#
# 챕터마다 따로 둔다. 도입 챕터는 0.104 로는 최저 HP 가 55% 라 "30~40%" 밖이었다. 20층 보스
# 앞에서 한 번에 무는 절벽이다 — 0.112 에서 40%, 0.113 에서 36%, 0.116 에서 29%, 0.118 에서
# 22%(버틸 실수 12), 0.12 에서 16%(6), 0.13 이면 17층에서 죽는다. 그 가운데를 쓴다.
# 얇은 것은 물약 쪽이다 — 모든 물약을 x0.95 로 줄이면 최저 HP 가 8~29% 로, x0.9 면 20층에서
# 죽는다. 물약·특성·전투 길이를 바꾸면 [7/7] 이 다시 잰다(check_tower 가 문이다).
TOWER_MOB_HP_LOSS = (0.113, 0.104, 0.104, 0.104, 0.104)     # 챕터별
TOWER_MIN_WASTE = 10            # 보이는 대로 마시며 물약을 이만큼 버려도 완주해야 한다

# 몬스터 방어력 = 설계 플레이어 공격력 x 0.6 x 특성 배수, 단 0.7 을 넘지 않는다.
#
# 예전 0.30 에서는 공격력 +1~+3 이 한 대라도 줄이는 층이 96개 중 3/3/6 뿐이었다 —
# 한 대 값이 공격력의 70% 라서 몇 점 더해도 몇 대를 치느냐가 안 바뀐다. 매직 타워가
# 공격 1점을 세게 만드는 방법 그대로, 몬스터 방어를 플레이어 공격 가까이 올린다.
# (임계를 실제로 만드는 것은 HP 를 놓는 자리다 — breakpoint_of.)
#
# 상한은 야수·수호(1.5배)를 붙잡는다. 0.8 에서는 <b>공격력 벽</b>이 섰다 — 공격만 두 레벨
# 모자란 채 아껴 마시면(정답 경로) 11층에서, 세 레벨 모자라면 보이는 대로 마셔도 20층 야수
# 보스에서 죽었다. 그 보스에게 한 대가 19 에서 11(두 레벨)·7(세 레벨)로 줄어 40초 싸움이
# 66초·100초, 최대 HP 의 34% 가 66%·97% 가 된다(0.7 이면 21·17 — 49%·66%). 0.7 이면 공격이
# 두 레벨 모자라도 어느 물약 습관으로든 완주하고, 세 레벨 모자라면 보이는 대로 마실 때만 완주한다.
# [7/7] 의 문은 "두 레벨, 정답 경로" 다.
MONSTER_DEF_RATIO = 0.6
MONSTER_DEF_CAP = 0.7

# 층에 배치되는 포션 (ConsumableItemData 의 회복 % 와 대응)
# 구역별 회복 아이템. 미로의 막다른 길에 하나씩 둔다.
#   구역0 없음 / 구역1 20% / 구역2 20% / (보스층) 구역3 50%
#
# <b>넘치게 마시는 것은 벌줄 수 없다.</b> 물약은 밟으면 즉시 회복이고
# (ConsumableItem.PickUp) 층을 넘겨 들고 갈 수 없다. 그래서 "아껴 두는" 이득이
# 없고, 일찍 마신 쪽의 HP 가 늦게 마신 쪽보다 낮아지는 순간이 존재하지 않는다.
# 예산을 어떻게 깎아도 정답 경로가 먼저 죽는다 — route_check.py --budget 참조.
# 물약 판단에 값을 매기는 것은 "언제 마시나" 가 아니라 "어디까지 들르나" 다.
FLOOR_POTIONS = [0.20, 0.20]   # 구역1 / 구역2
EXIT_POTION = 0.20             # 구역3(계단 앞) — 다음 층으로 들고 가는 몫
BOSS_FLOOR_POTIONS = [0.50]    # 보스층은 계단 앞 대신 이걸 둔다
POTION_USE_THRESHOLD = 0.55  # 이 비율 밑으로 떨어지면 마신다 (실제 플레이 행동)

# 실재하는 몬스터 아트만 사용한다.
#
# Boss_C0_I000~003 은 킹 슬라임과 분열 3종 전용이다. 그 연출에서만 쓰고
# 다른 던전에는 내보내지 않는다 — 도입부의 상징이라 아무 층에서나 나오면
# 그 장면이 싱거워진다.
# 그래서 생성 층이 쓸 수 있는 것은 Mob_C0_I000~I007 여덟 종뿐이고,
# 여기에 챕터별 색 변형(MonsterTint)이 곱해져 실제로 보이는 것은 8 x 5 다.
# 정예와 보스는 그림이 아니라 크기와 색의 진하기로 구분한다.
# 0~7 은 원래 쓰던 여덟 종. 8~9 는 Assets 에 있으면서 클립이 없어 못 쓰던 시트를
# MonsterArtSetup 으로 살린 것이다 (Boss_C1_I000, 예전 Monster_Idle).
# 8~9 는 공격 시트가 없어서 공격에도 대기 클립을 쓴다 — 없는 상태를 가리키면
# 애니메이터가 "State could not be found" 만 찍고 아무것도 재생하지 않는다.
# 8 번은 공격 시트가 아예 없어서 공격에도 대기 클립을 쓴다 — 없는 상태를 가리키면
# 애니메이터가 경고만 찍고 아무것도 재생하지 않는다.
MOB_ART = ([(f"Mob_C0_I{i:03d}", f"Mob_C0_A{i:03d}") for i in range(8)]
           + [("Mob_C0_I008", "Mob_C0_I008"), ("Mob_C0_I009", "Mob_C0_A009")])

# 챕터 보스가 입는 그림 (MOB_ART 의 인덱스). 그림이 특성을 말하게 고른다 —
#   20층 늑대(야수) · 40층 고블린 방패병(수호) · 60층 잿빛 파수꾼(마법)
#   80층 해골 전사(불사) · 100층 심연의 거수(거대, 가장 큰 86x68)
# 두 애니메이터(맵/전투창) 모두 이 상태가 있다 — validate_content 가 확인한다.
BOSS_ART_INDEX = [4, 7, 8, 6, 9]

# 벽 프리팹은 Tilemap_C00_W01 / W02 / W03 만 실재한다 (W00 은 없음).
# 챕터별 분위기는 MapBuilder 의 틴트 + 조명 + BGM 으로 낸다.
# 챕터 이름·몬스터 접두어·종 이름은 네 언어로 bestiary.py 가 낸다 (스토리 도감이
# 원본). 종 이름은 MOB_ART 의 순서와 한 줄씩 짝이다 — 이름은 실제 그림을 따라간다.
#
# BGM 키는 챕터마다 따로다. 예전 키(BGM_000/100/001/101/102)는 손수 만든 1~4층과 겹쳤다 —
# 3층도 41~60층도 BGM_001 이라, 그 키에 곡을 넣으면 마검 방에서 용광로 곡이 나올 참이었다.
# 아직 어느 키에도 곡이 없다. 어드레서블에 없으면 GameManager.PlayChapterBGM 이 챕터 0 곡
# (Chapter0_BGM)을 ChapterTheme.BgmPitch 로 틀어 준다 — 지금 소리 그대로다. 곡이 오면 그 키로
# 등록하고 그 챕터의 BgmPitch 를 1 로 둔다.
CHAPTER_THEMES = [
    # (BGM, 벽 타일셋)
    ("BGM_CH0", ["W_01", "W_02"]),
    ("BGM_CH1", ["W_02", "W_03"]),
    ("BGM_CH2", ["W_03", "W_01"]),
    ("BGM_CH3", ["W_01", "W_03"]),
    ("BGM_CH4", ["W_02", "W_01"]),
]
BOOKS, BOOKS_MISSING = bestiary.tables()
KR = BOOKS["kr"]

# ---------------------------------------------------------------- 특성 (기획서 13·53·71쪽)
#
# 기획서 13쪽: "몬스터 단위 공략이 아닌, 챕터 별 유리한 특성과 공략 방법 고민."
# 그래서 특성을 무작위로 흩지 않고 챕터마다 성격을 준다. 층 안에서는 서열이
# 올라갈수록 까다로운 특성이 나온다(MOB_LOSS_RAMP 와 같은 순서).
#
#   챕터 0  도입      — 특성 없는 상대와 야수로 기본을 익힌다
#   챕터 1  방어       — 수호·갑옷, 그리고 방어 1점이 두 번 막는 검사
#   챕터 2  화력       — 마법·거대·검사. 오래 끌면 죽는다, 빨리 끝내야 한다
#   챕터 3  치명타     — 불사·암살. 치명 주기를 언제 쓸지가 문제가 된다
#   챕터 4  총복습     — 앞의 것이 전부 섞여 나온다
#
# <b>다섯 층마다 한 줄</b>이다(BAND_TRAITS, 스무 줄). 예전 표는 챕터마다 다섯 자리였는데
# 몹 다섯 중 앞 셋이 한 종이라(MOB_SPECIES_RUN) 0·3·4번 자리만 실렸다 — 1·2번 칸은 죽은
# 데이터였고, 그래서 방어 챕터에 갑옷이 없었고(첫 갑옷이 81층) 야수는 61층에야 몹으로
# 나왔고, 새 규칙 없이 18층씩 흐르는 구간이 넷이었다.
#
# 한 줄 = (셋이 서는 종, 넷째, 정예). 띠는 (층-1)//5 — 1~5, 6~10, 11~15, 16~20 … 이고
# MonsterTint 의 색 갈래도 같은 경계로 바뀐다. 그림과 규칙이 같이 바뀌어야 "다른 놈" 으로 읽힌다.
# 챕터 안의 네 띠는 소개 -> 전개 -> 비틀기 -> 시험 순서다. 새 특성은 먼저 정예로 혼자 나오고
# (한 번 겪는다), 다음 띠에서 셋이 서는 종이 된다(같은 놈에게 공격 1점이 세 번 값을 한다).
# 챕터의 마지막 띠는 다음 챕터 특성 하나를 미리 보여 준다 — 16~20층 검사, 36~40층 마법,
# 56~60층 불사, 76~80층 거대(마지막 보스). 불사·암살은 56층 전에는 안 나온다 —
# 치명 주기 퍼즐은 치명타 챕터의 몫이고, 방어 챕터에 섞으면 챕터 성격이 무너진다.
BAND_FLOORS = 5
BAND_TRAITS = [
    # 챕터 0 도입 (5~20층, 보스 야수)
    (NONE,     NONE,     NONE),        #  1~5   (생성은 5층뿐) 맨몸의 셈
    (NONE,     NONE,     BEAST),       #  6~10  야수 소개 — 한 번 더 일어선다
    (BEAST,    NONE,     NONE),        # 11~15  야수가 셋 선다
    (BEAST,    NONE,     KNIGHT),      # 16~20  시험 + 검사 예고
    # 챕터 1 방어 (21~40층, 보스 수호)
    (KNIGHT,   NONE,     GUARDIAN),    # 21~25  검사가 셋, 수호 소개
    (GUARDIAN, KNIGHT,   ARMOR),       # 26~30  수호가 셋, 갑옷 소개
    (ARMOR,    GUARDIAN, KNIGHT),      # 31~35  갑옷이 셋 — 방어 0, 껍질부터
    (GUARDIAN, ARMOR,    MAGIC),       # 36~40  시험 + 마법 예고
    # 챕터 2 화력 (41~60층, 보스 마법)
    (MAGIC,    KNIGHT,   TITAN),       # 41~45  마법이 셋, 거대 소개
    (TITAN,    MAGIC,    KNIGHT),      # 46~50  거대가 셋
    (KNIGHT,   TITAN,    MAGIC),       # 51~55  빠른 검사 셋 + 전부 진심인 마법
    (MAGIC,    TITAN,    IMMORTAL),    # 56~60  시험 + 불사 예고
    # 챕터 3 치명타 (61~80층, 보스 불사)
    (IMMORTAL, NONE,     ASSASSIN),    # 61~65  불사가 셋, 암살 소개
    (ASSASSIN, NONE,     IMMORTAL),    # 66~70  암살이 셋
    (NONE,     IMMORTAL, ASSASSIN),    # 71~75  평범한 셋 사이로 숨을 맞춰 도착한다
    (ASSASSIN, IMMORTAL, TITAN),       # 76~80  시험 + 거대 예고(검은 태양)
    # 챕터 4 총복습 (81~100층, 보스 거대)
    (ARMOR,    TITAN,    ASSASSIN),    # 81~85
    (BEAST,    MAGIC,    IMMORTAL),    # 86~90
    (GUARDIAN, KNIGHT,   ASSASSIN),    # 91~95
    (TITAN,    ARMOR,    IMMORTAL),    # 96~100
]
assert len(BAND_TRAITS) * BAND_FLOORS == 100
assert all(len(row) == len(MOB_SPECIES_RUN) for row in BAND_TRAITS)
# 챕터 보스의 특성. 챕터의 성격을 보스가 대표한다 — 도입(야수)·방어(수호)·
# 화력(마법)·치명타(불사), 그리고 마지막은 가장 오래 버티는 거대.
BOSS_TRAITS = [BEAST, GUARDIAN, MAGIC, IMMORTAL, TITAN]

# 챕터 보스가 떨구는 장비 (EquipData.csv 의 ID).
# 능력치가 0 이고 유틸만 해금하는 것들이라 전투 밸런스를 건드리지 않는다.
#   1~4 부츠   : 이동 속도 등급 — 보스가 아니라 20·40·60·80층 바닥에 있다
#                (CHAPTER_EQUIP_REWARD)
#   5   목걸이 : 킹 슬라임의 기념품. 6~8 은 이제 아무도 주지 않는다 — 전투 배속이 설정이
#                되어(1/2/4배, B1) 배속 목걸이가 풀어 줄 것이 없다. 40·60·80층 보스 뒤에는
#                대신 공격·방어 룬 "둘 중 하나" 가 놓인다(BOSS_RUNE_FLOORS).
#                <b>B1 과 같이 들어가야 한다.</b> 배속 설정 없이 이것만 들어가면 배속은 목걸이
#                (EquipUtility.Apply)만 주므로 100층 내내 킹 슬라임 목걸이의 2배가 끝이다.
#   32  반지   : 워프석 반지 — 다녀온 층으로 워프 해금
#
# 워프는 20층 보스가 준다 — 매직 타워의 층 이동처럼 일찍 풀려야 남은 열쇠를 들고
# 앞 층 금고로 돌아가는 판단이 생긴다(60층이면 쓸 일이 거의 없었다).
# 100층 보스는 아무것도 주지 않는다 — 떨굴 곳이 없다. -1 은 "없음" 이다
# (UI_MonsterCard.DropReward 가 0 이하를 건너뛴다. 0 은 EquipData 의 빈 자리표다).
# 같은 장비를 두 번 주지 않는다 — validate_content 가 모든 출처를 센다.
BOSS_REWARD = [32, -1, -1, -1, -1]

# 특성별 스탯 성격 (기획서 53쪽의 "능력치 특징"을 배수로 옮긴 것).
# 절대값이 아니라 배수다 — 실제 수치는 특성을 켠 시뮬레이터로 역산하므로,
# 여기서는 "어느 쪽으로 치우친 상대인가" 만 정한다.
TRAIT_FLAVOR = {
    NONE:     dict(aspd=1.00, dfn=1.0, dspd=1.00, dur=1.0),
    BEAST:    dict(aspd=0.90, dfn=1.5, dspd=1.00, dur=1.2),  # 체력·방어 높음
    MAGIC:    dict(aspd=0.70, dfn=0.6, dspd=1.00, dur=0.8),  # 낮은 체력, 느린 마법
    GUARDIAN: dict(aspd=0.90, dfn=1.5, dspd=2.00, dur=1.0),  # 높은 방어, 빠른 방어속도
    IMMORTAL: dict(aspd=0.60, dfn=0.6, dspd=0.60, dur=1.0),  # 낮은 스탯, 매우 느림
    KNIGHT:   dict(aspd=2.00, dfn=0.8, dspd=1.00, dur=0.9),  # 쾌속 = 50%x2회를 공속으로
    TITAN:    dict(aspd=0.80, dfn=1.0, dspd=0.80, dur=1.3),  # 많은 체력, 높은 공격
    ASSASSIN: dict(aspd=1.30, dfn=0.5, dspd=0.50, dur=1.0),  # 은신, 빠른 공격속도
    ARMOR:    dict(aspd=0.90, dfn=0.0, dspd=0.01, dur=1.0),  # 방어 불가, 방어력 0
}


def band_of(floor):
    return (floor - 1) // BAND_FLOORS


def trait_of(floor, run):
    """그 층의 run 번째 묶음(0 셋이 서는 종, 1 넷째, 2 정예)의 특성."""
    return BAND_TRAITS[band_of(floor)][run]


# ---------------------------------------------------------------- 룬 (기획서 65·81쪽)
#
# "능력치 증가 아이템 / 획득 시, 공방체 능력치 증가", "획득과 동시에 사용되며
# 공방체 스텟을 영구히 증가시켜준다".
#
# 값은 새로 만들지 않고 ConsumableItemData 에 이미 있는 9/10/11 을 쓴다.
# 공격력 +1 은 총량으로 보면 작아 보이지만, 플레이어가 한 레벨에 얻는 것이 +5 다.
# 즉 룬 하나가 반 레벨의 5분의 1 — 세 층에 하나씩 같은 종류가 돌아오므로
# 자연 성장의 약 13% 를 룬이 맡는다. 층이 올라가도 이 비율은 그대로다
# (레벨당 증가치가 일정하므로 챕터별로 등급을 나눌 필요가 없다).
RUNE_CYCLE = ["I_09", "I_10", "I_11"]
RUNE_GAIN = {"I_09": ("atk", 1.0), "I_10": ("dfn", 1.0), "I_11": ("hp", 5.0)}

# 크기가 붙은 룬 — 금고와 "둘 중 하나" 에만 놓는다. 완주 계산(rune_bonus)에는 없다.
#
# 계단 룬(+1)은 완주 보장의 일부라 그대로 둔다. 그런데 금고에도 같은 +1 을 넣었더니 챕터 4 의
# 공격 +1 이 전투 하나에 최대 HP 의 0.2% 값이었다 — 열쇠를 쓸지 아낄지 따질 거리가 없었다.
# 매직 타워의 보석처럼 챕터마다 크기를 정하되 <b>능력치마다 다르게</b> 정한다. 방어 1점이
# 공격 1점보다 늘 세다(몬스터 한 대가 몇 점뿐이라 1점이 한 대의 몇 할이다 — 7층 몹은 한 대가 4점).
#
# 값은 stat_value 로 잰다: 공격·방어는 주운 다음 층부터 20층(RUNE_HORIZON) 동안 설계 플레이어가
# 아끼는 HP, 체력은 얻은 양에 그 20층 동안 놓인 물약이 더 채우는 몫(정답 경로가 실제로 마시는
# 비율만큼)을 더한다. 예전에는 "그 챕터의 보스까지" 만 셌다 — 챕터 끝에서 주운 공격·방어는
# 한두 층 값만 쳐서(39층 방어 +1 이 3%) 체력이 거기서 이기는 것처럼 보였고, 20층·100층으로
# 늘리면 공격 49% · 체력 10%, 공격 88% 로 무너졌다. 크기 룬이 놓이는 59개 층에서 셋 중 가장
# 값진 것이 공격 25% · 방어 37% · 체력 37% 다(report_rune_tiers — 다른 셈법의 몫도 같이 찍는다).
# 셈법이 하나로 안 모이는 것은 체력이 다른 화폐라서다. 체력 룬은 싸움값을 한 푼도 안 깎는다(장부
# 점수로는 0) — 대신 보이는 대로 마시는 사람의 다음 20층 최저 HP 를 3~9%p 올린다(방어 +1 은
# 0.6~2.7%p, 공격은 0~1%p). 점수를 볼지 안전을 볼지에 따라 답이 갈리니 셋 다 고를 까닭이 있다.
#
# 공격이 +3 인 챕터(0·3)와 +6 인 챕터(1·2·4)가 있다. 임계를 1~3·4~6·7~9 로 나눴으므로
# (breakpoint_of) 설계 상태에서 +3 은 층마다 한 종의 임계만 넘는다 — 방어 +1 을 이기려면 두 칸을
# 넘는 +6 이어야 했다. 도입 챕터(방어 1점이 한 대의 4분의 1)와 치명타 챕터(불사·암살이 평타를 흘린다)는
# +6 으로도 방어·체력을 못 이겨서 +3 으로 둔다 — 임계 칸을 덜 쓴다.
RUNE_TIERS = [      # 챕터별 (공격, 방어, 체력)
    (3, 1, 60),
    (6, 1, 50),
    (6, 1, 65),
    (3, 1, 80),
    (6, 1, 60),
]
RUNE_HORIZON = 20   # 영구 능력치의 값을 셀 층 수 (stat_value)
SIZED_RUNE_BASE = 12                       # ConsumableItemData 에서 크기 룬이 시작하는 id
SIZED_RUNE_NAME_BASE = 4100                # 그 이름("공격 룬 +3")의 ScriptData — 설명은 기본 룬(130~132)
# 행은 (능력치 자리, 크기) 하나에 하나다 — 챕터가 달라도 크기가 같으면 같은 물건이다.
# 능력치 자리는 RUNE_CYCLE 순서(0 공격, 1 방어, 2 체력). ConsumableItem.NUM_OF_RUNES 가
# 이 행 수까지 덮어야 한다 — validate_content 가 확인한다.
SIZED_RUNES = sorted({(s, tier[s]) for tier in RUNE_TIERS for s in range(3)})


def rune_tier(floor):
    """그 층에 놓인 크기 룬이 듣는 챕터의 크기. 줍는 층 다음부터 들으므로 floor+1 의 챕터다
    (40·60·80층 보스 방의 룬은 다음 챕터 몫)."""
    return RUNE_TIERS[min(floor // FLOORS_PER_CHAPTER, len(RUNE_TIERS) - 1)]


def sized_rune(floor, stat):
    """셀 코드. stat 은 RUNE_CYCLE 의 자리(0 공격, 1 방어, 2 체력)."""
    return f"I_{SIZED_RUNE_BASE + SIZED_RUNES.index((stat, rune_tier(floor)[stat])):02d}"


# ---------------------------------------------------------------- 제단 (매직 타워의 상점)
#
# 정답 경로 기준으로 필요한 회복의 1.82배가 층에 놓여 있다 — 나머지는 넘쳐서 버려진다.
# 그 몫을 판단거리로 바꾼다. 띠(5층)의 마지막 층 계단에서 HP 를 내고 공격이나 방어를 산다.
# 값은 최대 HP 의 a + b·n(n−1) % (n 번째 구매) — 50층 매직 타워 상점의 10n(n−1)+20 골드와
# 같은 꼴이라 갈수록 비싸진다. 한 번 들를 때 하나만 판다.
#
# 내고 나서 남아야 하는 HP(AltarReserve)는 <b>층마다, 몬스터 표마다</b> 잰다(altar_reserves).
# 제단 다음 층은 늘 인색 층(물약 하나)이고 그 입구가 체크포인트라, 거기 너무 적게 들고 올라가면
# 다시 해도 같은 HP 로 서서 영영 못 넘는다. 처음에는 "HP 가 값 이하면 거절" 이라 3% 로 올라가 첫
# 관문에서 죽었고, 그 다음 15% 하나로 막았는데 첫 관문만 보고 정한 값이었다 — 일반 표는 36·56·
# 76·96층에 18~19% 를, 탑의 법은 제단 다음 층 전부에 19~55% 를 들고 들어서야 100층까지 갔다.
# 15% 로 사면 일반 네 곳, 탑의 법 열아홉 곳 전부에서 그 입구에 갇혔다.
# 그래서 "그 HP 로 다음 층에 들어서면(설계 레벨, 산 것 없이, 보이는 대로 마시며) 100층까지 간다"
# 의 최솟값에 ALTAR_MARGIN 을 얹는다. [7/7] 이 그 문턱 그대로 들어서 보고(check_altar),
# 살 수 있을 때마다 사는 길을 두 표에서 다 돌린다.
#
# 맵 오브젝트가 아니라 계단의 물음이다 — MapData 가 안 바뀌어 세이브가 안 깨진다(구매 수는
# CurPlayerData 에 둔다). 값·양·문턱은 StageInfoData 의 Altar* 열 한 곳에 싣는다. 제단 C# 은 아직
# 없다(W5) — 만들 때 altar_sells 와 같은 식을 그 열에서 읽고, 탑의 법이면 AltarReserveTower 를 쓴다.
# 완주 계산에는 없다. [7/7] 이 "하나도 안 산다" 와 "살 수 있으면 다 산다" 둘 다 완주하는지 잰다.
#
# 한 번에 파는 양은 그 챕터의 크기 룬과 같다(RUNE_TIERS) — 제단은 "피로 사는 룬" 이다.
# 값 10 + n(n−1) % 가 얼마까지 제값을 하는지는 check_altar 가 구매마다 찍는다.
ALTAR_PRICE = 10.0              # a — 첫 구매, 최대 HP 의 %
ALTAR_PRICE_STEP = 1.0          # b
ALTAR_MARGIN = 5.0              # 다음 층을 넘길 최소 HP 위에 얹는 여유, 최대 HP 의 %p


def altar_at(floor):
    """그 층 계단의 제단이 파는 (공격, 방어). 띠의 마지막 층에만 있고, 100층은 오를 계단이 없다."""
    if floor % BAND_FLOORS or floor <= HANDMADE_FLOORS or floor >= TOTAL_FLOORS:
        return None
    atk, dfn, _ = rune_tier(floor)
    return atk, dfn


def altar_price(n):
    """n 번째 구매의 값 (최대 HP 의 %)."""
    return ALTAR_PRICE + ALTAR_PRICE_STEP * n * (n - 1)


def altar_sells(cur_hp, max_hp, n, reserve):
    """n 번째 구매를 지금 HP 로 살 수 있는가. reserve 는 그 층의 AltarReserve(%) — 제단 C# 도 이 식이어야 한다."""
    return cur_hp - max_hp * altar_price(n) / 100.0 >= max_hp * reserve / 100.0


# ---------------------------------------------------------------- 열쇠 (魔塔 분석)
#
# 원형의 50층을 재 보니 문:열쇠가 <b>1:1 이 아니다</b> — 노랑 239:169(71%),
# 파랑 39:20(51%), 빨강 11:3(27%). 색이 귀할수록 결핍이 가파르다. 그래야
# "어느 문을 열까" 가 질문이 된다. 우리는 초록·노랑·빨강 문 96개에 열쇠도
# 정확히 96개씩이라 고민할 일이 없었다.
#
# 그런데 <b>그냥 줄이면 진행이 막힌다.</b> 우리 층은 문 셋이 구역을 잘라
# 순서를 강제하고 각 구역에 그 다음 문의 열쇠가 하나씩 있다 — 큰길 열쇠는
# 한 개도 남는 것이 아니다. 그래서 줄이지 않고 <b>늘려서 모자라게</b> 한다.
#
#   여분 열쇠 층 : 골방 안쪽에 열쇠를 하나 더 둔다. 파수꾼을 잡아야 얻고,
#                  안 쓰면 다음 층으로 들고 간다(KeyInventory 는 층을 넘어
#                  유지된다 — 새 게임에서만 지워진다).
#   금고 층      : 골방 입구가 <b>네 번째 문</b>이 되고 안에 룬이 있다.
#                  큰길에는 걸리지 않으니 완주 보장은 그대로다.
#
# 금고가 여분 열쇠보다 많다. 색이 귀할수록 더 많다 — 초록 20:12, 노랑 16:8,
# 빨강 12:4. 전부 열 수는 없고, 무엇을 포기할지가 층을 넘는 판단이 된다.
#
# 24층 한 바퀴 x 4 = 96층. 열쇠가 먼저 나오고 금고가 뒤따르게 늘어놓았다.
# 보스층(20·40·60·80·100)에는 금고가 걸리지 않는다 — 그 층 마지막 구역은
# 보스와 룬이 이미 자리를 다투는 곳이라 골방까지 밀어 넣을 이유가 없다.
_ALCOVE_CYCLE = [
    ("key", 0),   None,         ("vault", 0), ("key", 1),
    ("vault", 0), None,         ("vault", 1), ("key", 0),
    ("vault", 2), None,         ("vault", 1), ("key", 2),
    ("vault", 0), None,         ("vault", 2), ("key", 1),
    ("vault", 1), ("vault", 0), None,         ("key", 0),
    ("vault", 2), ("vault", 1), ("vault", 0), None,
]


def _cycle_at(floor):
    return _ALCOVE_CYCLE[(floor - HANDMADE_FLOORS - 1) % len(_ALCOVE_CYCLE)]


def alcove_plan(floor):
    """그 층 골방의 쓰임새. (종류, 색, 놓을 셀코드) 또는 None(파수꾼 + 물약).

    금고마다 <b>다른 룬</b>을 넣는다. 같은 것만 나오면 "지금 열까 아껴 둘까"
    가 아니라 "빨리 열수록 이득" 뿐이라 고를 것이 없어진다 — 물약에서 이미
    겪은 일이다(즉시 회복이라 일찍 마시는 쪽이 지배 전략이었다).
    """
    plan = _cycle_at(floor)
    if plan is None:
        return None
    kind, color = plan
    if kind == "key":
        return ("key", color, KEY_ITEM[color])
    seq = sum(1 for f in range(HANDMADE_FLOORS + 1, floor)
              if (_cycle_at(f) or (None,))[0] == "vault")
    return ("vault", color, sized_rune(floor, seq % len(RUNE_CYCLE)))


# 40·60·80층 보스가 떨구던 배속 목걸이 자리. 배속은 이제 설정에서 고르므로(1/2/4배) 목걸이는
# 아무것도 풀어 주지 않는다. 대신 계단 방(보스 뒤)에 다음 챕터 크기의 공격 룬과 방어 룬을
# "둘 중 하나" 로 놓는다 — 값이 챕터마다 갈린다(다음 20층 셈으로 40층 공격 54% 대 방어 43%,
# 60층 25% 대 47%, 80층 20% 대 22% — stat_value).
# 보스를 잡아야 닿고 완주 계산에는 없다.
BOSS_RUNE_FLOORS = (40, 60, 80)


def choice_pair(floor):
    """그 층의 "둘 중 하나" 보상 (셀코드, 셀코드, 놓는 곳) 또는 None — build_floor_layout 의 choice."""
    if floor_type(floor) == 4:
        # 보물 층: 넉넉한 물약이냐, 계단 룬과 같은 능력치의 크기 룬이냐.
        return (POTION_BY_HEAL[GENEROUS_POTION],
                sized_rune(floor, RUNE_CYCLE.index(rune_of(floor))), "region")
    if floor in BOSS_RUNE_FLOORS:
        return (sized_rune(floor, 0), sized_rune(floor, 1), "stairs")
    return None


# ---------------------------------------------------------------- 층 유형 (매직타워 분석)
#
# 96개 층이 전부 같은 질문("물약을 언제 먹을까") 하나만 던져서 단조로웠다.
# 다섯 층을 주기로 질문을 돌린다. 층 구조는 그대로 두고 <b>예산</b>만 바꾼다 —
# 방을 복잡하게 만드는 것보다 이쪽이 판단을 만든다.
#
#   0 기본   물약 둘. 계산을 익힌다.
#   1 인색   물약 하나. 순서가 전부다.
#   2 관문   물약 둘. 대신 가장 센 놈이 길목에 선다(정예는 원래 통로 목에 있다).
#   3 넉넉   물약 셋. 인색한 층을 지나온 보상이자 다음 인색을 위한 비축.
#   4 보물   물약 둘 + 룬이 막다른 길로 간다.
#
# 생성기와 route_check 가 <b>같은 함수</b>를 봐야 한다. 한쪽만 바뀌면 완주 보장이
# 거짓이 된다 — 예전에 도입부 경험치에서 그 사고가 났다.
FLOOR_TYPES = ["기본", "인색", "관문", "넉넉", "보물"]
GENEROUS_POTION = 0.30


def floor_type(floor):
    return (floor - HANDMADE_FLOORS - 1) % len(FLOOR_TYPES)


def floor_potions(floor):
    """그 층 구역에 놓는 회복 아이템 비율 목록 (계단 앞 물약은 따로다)."""
    kind = floor_type(floor)
    if kind == 1:                      # 인색
        return [FLOOR_POTIONS[1]]
    if kind == 3:                      # 넉넉
        return [FLOOR_POTIONS[0], FLOOR_POTIONS[1], GENEROUS_POTION]
    return list(FLOOR_POTIONS)


def placed_heal(floor):
    """그 층에 놓인 회복의 합(최대 HP 대비) — 구역 물약 + 계단 앞 물약 + 보스층의 큰 물약."""
    return (sum(floor_potions(floor)) + EXIT_POTION
            + (BOSS_FLOOR_POTIONS[0] if is_boss_floor(floor) else 0.0))


def rune_in_dead_end(floor):
    """보물 층에서는 룬을 막다른 길로 보낸다 — 들를지 말지가 판단거리가 된다."""
    return floor_type(floor) == 4


def rune_of(floor):
    return RUNE_CYCLE[(floor - HANDMADE_FLOORS - 1) % len(RUNE_CYCLE)]


def rune_bonus(floor):
    """그 층에서 싸울 때 이미 들고 있는 룬의 합.

    룬은 층의 마지막 구역(계단 앞)에 있다. 그래서 F층의 룬은 F층 전투가 끝난
    뒤에 얻고, 효과는 F+1층부터다. 이 한 칸 차이를 맞춰야 역산과 실제가 어긋나지 않는다.
    """
    bonus = {"atk": 0.0, "dfn": 0.0, "hp": 0.0}
    for f in range(HANDMADE_FLOORS + 1, floor):
        stat, amount = RUNE_GAIN[rune_of(f)]
        bonus[stat] += amount
    return bonus


def stats_with_runes(ptable, level, floor, runes=True):
    """레벨 스탯 + 손에 든 검 + 그 층까지 모은 룬 (runes=False 면 룬 없이)."""
    s = with_equip(player_stats_at(ptable, level), sword_on(floor))
    if runes:
        for stat, amount in rune_bonus(floor).items():
            s[stat] += amount
    return s


def player_with_runes(ptable, level, floor, cur_hp=None):
    s = stats_with_runes(ptable, level, floor)
    c = Creature(s["hp"], s["atk"], s["dfn"], s["aspd"], s["dspd"],
                 s["crit"], s["crit_atk"])
    if cur_hp is not None:
        c.hp = cur_hp
    return c

# 챕터 클리어 보상 장비. EquipItemAnimator 에 실제로 상태가 있는 id(0~4)만 쓴다.
# ponytail: EquipData 의 스탯이 전부 0 이라 지금은 연출용이다.
#           장비로 밸런스를 잡으려면 EquipData 를 채우고 여기 id 를 바꿀 것.
CHAPTER_EQUIP_REWARD = {20: 1, 40: 2, 60: 3, 80: 4}

# ---- 손수 만든 도입부 (건드리지 않는다) -------------------------------------
HANDMADE_FLOORS = 4                      # 1~4층 = Dungeon_00_000 ~ 00_003
HANDMADE_BOSS_ID = 5                     # 킹 슬라임 (프리팹에 구워져 있는 값)
SPLIT_SLIME_IDS = (7, 6, 8)              # 노랑·빨강·파랑 (DirectingManager 가 띄운다)
SPLIT_POTION_ID = 7                      # 노랑 슬라임 자리에 함께 떨어지는 60% 물약
# 실제 진행 순서: (층, 싸우는 몬스터 — None 이면 그 층 CSV 전부, 더 놓이는 물약).
# 00_002(3층)는 마검 이벤트 방이라 몬스터가 없다. 킹 슬라임을 잡으면 분열 셋이
# 나오고 셋을 다 잡아야 계단이 열린다 — 반드시 치르는 전투인데 예전에는 빠져
# 있어서 경험치 420 을 덜 셌다. 넣으니 5층 진입 예측이 Lv14 -> Lv16 이 됐다.
# 자동 플레이 실측은 Lv17 이다 — 남은 1레벨(106 EXP 이상)은 아직 원인을 모른다.
HANDMADE_RUN = [("00_000", None, ()), ("00_001", None, ()),
                ("00_003", (HANDMADE_BOSS_ID,), ()),
                ("00_003", SPLIT_SLIME_IDS, (SPLIT_POTION_ID,))]

# ---- 손에 든 검 ----------------------------------------------------------------
# 1~2층은 블레이드(EquipData 9 = Define.EQUIP_SOWRD_FIRST), 3층 계약부터 끝까지
# 에고소드(10)다 (DirectingManager.ContractSword -> SwapEquip). 예전에는 완주 계산에
# 마검이 없었다 — 실제 플레이어는 공격력·공속만큼 늘 강해서, 설계한 손실의 절반
# 남짓만 치렀고 전투 예측은 거의 늘 초록이었다. 값은 표에서 그대로 읽는다.
SWORD_BLADE, SWORD_EGO = 9, 10
CONTRACT_FLOOR = 3
_EQUIP_STAT = dict(atk="ATK", dfn="DEF", hp="HP", aspd="ASPD", dspd="DSPD",
                   crit="CRI", crit_atk="CRIATK")


def _equip_rows():
    with open(os.path.join(JSOND, "EquipData.json"), "r", encoding="utf-8") as f:
        return {e["id"]: e for e in json.load(f)["equips"]}


EQUIPS = _equip_rows()


def sword_on(floor):
    return EQUIPS[SWORD_EGO if floor >= CONTRACT_FLOOR else SWORD_BLADE]


def with_equip(stats, equip):
    """레벨 스탯에 장비를 얹는다 (GameManager.SwapEquip 이 더하는 것과 같다)."""
    s = dict(stats)
    for stat, col in _EQUIP_STAT.items():
        s[stat] += equip[col]
    return s

# 원본 StageInfoData 의 1~4층 행. 선형이 아니다:
#   1층 -> 2층 -> 3층(막다른 마검방),  2층 -> [보스방] 4층 -> 5층
# 경험치는 원본의 두 배다. 원본이 100 인데 200 을 주는 이유:
# UI_MonsterCard.Dead 가 몬스터마다 두 번 돌고 있어서, 실제 게임은 늘 경험치를
# 두 배로 주고 있었다. 도입부의 난이도가 그 두 배에 맞춰 손수 조정돼 있어서,
# 중복 호출을 막자마자 1층에서 죽는다(단일 경험치로는 4층 종료 시 Lv8 HP21 —
# 칼끝이다). 버그를 되돌리는 대신 같은 경험치를 데이터로 정직하게 준다.
# 5층 목표 레벨은 이 값으로 다시 시뮬레이션해서 얻는다.
ORIGINAL_STAGES = [
    dict(id=0, DungeonID="00_000", Type=0, UpStage="00_001", DownStage="-",
         BossRoom="-", ATK=1, DEF=1, EXP=200, BGM="BGM_000",
         DungeonNameScriptID=5000),
    dict(id=1, DungeonID="00_001", Type=0, UpStage="00_002", DownStage="00_000",
         BossRoom="00_003", ATK=1, DEF=1, EXP=200, BGM="BGM_000",
         DungeonNameScriptID=5001),
    dict(id=2, DungeonID="00_002", Type=0, UpStage="-", DownStage="00_001",
         BossRoom="-", ATK=4, DEF=4, EXP=200, BGM="BGM_001",
         DungeonNameScriptID=5002),
    dict(id=3, DungeonID="00_003", Type=2, UpStage="00_004", DownStage="-",
         BossRoom="-", ATK=1, DEF=1, EXP=400, BGM="BGM_002",
         DungeonNameScriptID=5003),
]


def dungeon_id(floor):
    """1-based 층 번호 -> 'CC_FFF'"""
    ch = (floor - 1) // FLOORS_PER_CHAPTER
    idx = (floor - 1) % FLOORS_PER_CHAPTER
    return f"{ch:02d}_{idx:03d}", ch, idx


def is_boss_floor(floor):
    return floor % FLOORS_PER_CHAPTER == 0


# -------------------------------------------------- 손수 만든 1~4층 시뮬레이션

def _load_original_tables():
    """1~4층에 쓰이는 원본 몬스터(0~16)와 포션 회복률."""
    with open(os.path.join(JSOND, "MonsterData.json"), "r", encoding="utf-8") as f:
        monsters = {m["id"]: m for m in json.load(f)["creatures"]
                    if m["id"] < NEW_MONSTER_ID_BASE}

    potions = {}
    with open(os.path.join(EXCEL, "ConsumableItemData.csv"), "r",
              encoding="utf-8-sig") as f:
        reader = csv.reader(f)
        next(reader)
        for row in reader:
            if row and row[0].strip():
                potions[int(row[0])] = float(row[1])
    return monsters, potions


def _cells(dungeon_id_str, pattern):
    """층 CSV 에서 'M_003' 같은 셀의 숫자만 뽑는다."""
    import re
    path = os.path.join(STREAM, f"Dungeon_{dungeon_id_str}.csv")
    with open(path, "r", encoding="utf-8-sig") as f:
        text = f.read()
    return [int(re.sub(r"[^0-9]", "", c)) for c in re.findall(pattern, text)]


def simulate_handmade(ptable):
    """1~4층을 원본 데이터 그대로 완주시켜 (레벨, 잔여경험치, 현재HP) 를 얻는다.

    이 구간은 우리가 스탯을 정할 수 없으므로, "생성 구간이 어디서 시작하는지"를
    측정하는 것이 목적이다. 약한 몬스터부터 잡는 순서를 가정한다 — 그게 이 게임의
    설계 규칙이고, 파일 순서대로 싸우면 실제로 1층에서 죽는다.
    """
    monsters, potions_by_id = _load_original_tables()

    level, exp = 1, 0.0
    cur_hp = player_stats_at(ptable, level)["hp"]

    # 게임은 경험치에 스테이지 배율을 곱한다 (StageInfoData 의 EXP / 100).
    # 그걸 빼고 몬스터 원값만 더하고 있어서, 도입부에서 실제로 얻는 경험치보다
    # 적게 셌다 — 5층의 기준 레벨이 그만큼 낮게 잡혀 있었다.
    exp_scale = {st["DungeonID"]: st["EXP"] / 100.0 for st in ORIGINAL_STAGES}

    for did, ids, extra_potions in HANDMADE_RUN:
        scale = exp_scale.get(did, 1.0)
        floor = int(did.split("_")[1]) + 1
        mob_ids = list(ids) if ids is not None else _cells(did, r"M_[0-9]+")
        # 순서가 곧 난이도. 약한 놈부터.
        mob_ids.sort(key=lambda i: monsters[i]["MaxHP"] * monsters[i]["Attack"])

        potion_ids = (_cells(did, r"I_[0-9]+") if ids is None else []) + list(extra_potions)
        potions = sorted(p for p in (potions_by_id.get(i, 0.0) for i in potion_ids) if p > 0)

        for i, mid in enumerate(mob_ids):
            stats = with_equip(player_stats_at(ptable, level), sword_on(floor))
            while potions and cur_hp < stats["hp"] * POTION_USE_THRESHOLD:
                cur_hp = min(stats["hp"], cur_hp + stats["hp"] * potions.pop(0) / 100.0)

            p = Creature(stats["hp"], stats["atk"], stats["dfn"],
                         stats["aspd"], stats["dspd"], stats["crit"], stats["crit_atk"])
            p.hp = min(cur_hp, stats["hp"])

            md = monsters[mid]
            m = Creature(md["MaxHP"], md["Attack"], md["Defence"], md["AttackSpeed"],
                         md["DefenceSpeed"] or 0.1, md["Critical"] or 99,
                         md["CriticalAttack"] or 200, md.get("Ability", 0))

            won, _, _ = simulate_battle(p, m)
            cur_hp = p.hp
            if not won:
                return None, (f"손수 만든 {did} 층 {i + 1}번째 전투에서 사망 "
                              f"(Lv{level}, 몬스터 {md['Name']})")

            exp += md["RewardExp"] * scale
            while level + 1 in ptable and exp >= ptable[level + 1]["need_exp"]:
                exp -= ptable[level + 1]["need_exp"]
                level += 1
                cur_hp += ptable[level]["hp"]

    return (level, exp, cur_hp), None


# ------------------------------------------------------------------ 밸런싱

def breakpoint_of(floor, run):
    """임계(臨界) 거리 — 설계 공격력보다 몇 점 높으면 이 종이 한 대 덜 맞고 쓰러지는가.

    층의 세 종(셋 / 넷째 / 정예)이 1~3 · 4~6 · 7~9 를 하나씩 나눠 갖고, 층마다 돌려 가진다.
    예전에는 셋 다 1~3 에 두었다. 설계 상태에서는 공격 +3 이 층 96개 중 62개에서 1% 넘게
    아꼈지만, 5층 제단이나 7층 금고에서 공격 +3 을 한 번 집으면 임계가 전부 등 뒤로 가서
    그 다음 +3 은 10개 층에서만 값을 했다 — 임계 설계가 그 룬 하나를 집기 전까지만 살아 있었다.
    세 칸으로 나누면 룬 +3 을 몇 번 집었든(0·1·2번) 다음 +3 이 층마다 한 종의 임계를 넘는다
    (+0/+3/+6 에서 37/31/36개 층 — report_breakpoints). 셋째 칸 너머(+9~)는 저절로 생긴 임계뿐이다.
    """
    return 1 + (floor + 3 * run) % 9


BOSS_BREAKPOINT = 3
BOSS_CRIT = 20.0          # 보스는 20번째 공격이 치명타다 (일반 몹은 99 = 사실상 없음)
BOSS_DSPD_MIN = 0.15      # 보스 방어 속도의 하한 — 방패가 너무 드물면 보스가 그냥 샌드백이다


def solve_monster(ptable, level, hp_loss_target, duration_target, aspd, trait, floor,
                  breakpoint=0, crit=99, dspd_min=0.01):
    """플레이어 레벨에 맞춰 몬스터 스탯을 역산한다.

    HP  -> 전투 지속시간이 목표가 되도록 (플레이어 DPS 기준)
    ATK -> 플레이어 HP 손실이 목표 비율이 되도록
    둘 다 시뮬레이터를 기준으로 이분 탐색한다.

    특성을 켠 채로 역산한다. 이게 중요하다 — 암살은 치명타가 아닌 공격을 전부
    회피하고 불사는 80% 를 흘리므로, 특성을 끄고 뽑은 수치는 실제와 몇 배씩
    어긋난다. 이분 탐색이 특성까지 포함해서 답을 찾게 둔다.

    breakpoint 가 k 면 HP 를 "공격 +k 면 한 대 덜" 인 자리에 놓는다(breakpoint_of).
    crit·dspd_min 은 데이터에 실리는 값 그대로 푼다 — 보스는 20번째 공격이 치명타이고
    방어 속도가 0.15 아래로 안 내려간다. 예전에는 그 둘을 빼고 풀어서 20층 야수 보스가
    목표 35% 가 아니라 40% 를, 100층 거대가 39% 를 때렸다(방패가 더 자주 서서 싸움이 길다).
    """
    ps = stats_with_runes(ptable, level, floor)
    flavor = TRAIT_FLAVOR[trait]
    aspd = round(aspd * flavor["aspd"], 2)
    dspd = round(max(dspd_min, 0.1 * flavor["dspd"]), 3)
    duration_target *= flavor["dur"]
    dfn_m = max(0, int(ps["atk"] * min(MONSTER_DEF_CAP, MONSTER_DEF_RATIO * flavor["dfn"])))

    def build(hp, atk):
        return Creature(hp, atk, dfn_m, aspd, dspd, crit, 200, trait)

    # 1) HP 이분 탐색: 지속시간 목표
    lo, hi = 1.0, max(50.0, ps["atk"] * duration_target)
    for _ in range(40):
        mid = (lo + hi) / 2
        p = player_with_runes(ptable, level, floor)
        won, dur, _ = simulate_battle(p, build(mid, 1))
        if not won or dur < duration_target:
            lo = mid
        else:
            hi = mid
    hp_m = max(1.0, round(hi))

    # 암살은 치명타가 아닌 공격을 전부 흘린다. 그래서 HP 를 아무리 낮춰도
    # 전투가 "치명타를 기다리는 시간" 만큼은 걸리고, 지속시간 목표 밑으로
    # 내려가질 않는다 — 이분 탐색이 바닥까지 내려가 HP 1 짜리가 40마리 나왔다.
    # 불사는 반대로 평타 5분의 1 로도 목표 안에 죽어서, 숨(치명 주기)을 맞출 이유가 없었다.
    # 두 특성의 전투 길이는 지속시간 목표가 아니라 첫 치명타가 정한다(치명 주기 20대 ≈ 27초).
    # 그러니 "첫 치명타를 받고도 평타 두 대는 버틴다" 만큼만 세우고, 불사는 치명타 없이는
    # 못 쓰러뜨리게 한다. 손실 목표는 아래 ATK 탐색이 맡는다.
    #
    # 예전 바닥(0.25 x 공격력 x 지속시간)은 <b>모든 특성</b>에 걸렸다. 방어가 공격의 0.3 일 때는
    # 아무도 안 닿았는데, 0.6 으로 올리니 방어 0.8 인 야수·수호가 한 대가 가벼워 그 바닥에
    # 걸려 전투가 목표의 2.4배로 늘었다 — 그 긴 전투가 임계 수를 부풀리고 있었다.
    if trait in (ASSASSIN, IMMORTAL):
        p_atk = int(ps["atk"])
        plain = max(1, p_atk - dfn_m)
        crit_hit = max(1, int(round(p_atk * ps["crit_atk"] / 100.0)) - dfn_m)
        need = crit_hit + 2 * plain
        if trait == IMMORTAL:
            need = max(need, (int(ps["crit"]) - 1) * int(plain * 0.2) + 1)
        hp_m = max(hp_m, float(need))
    elif breakpoint:
        # 이분 탐색이 찾은 HP 는 늘 "한 대의 배수" 바로 위에 앉는다(목표 시간을 넘기는 가장
        # 작은 HP 를 반올림하니까). 그 자리에서 한 대를 덜려면 공격이 dmg/(n-2) 점, 층마다
        # 9~48점이 더 있어야 했다 — 공격 +1~+3 이 한 대라도 줄이는 층이 96개 중 11/14/20.
        # 같은 대수로 끝나는 HP 중 "공격 +k 면 한 대 먼저 끝나는" 가장 큰 값으로 옮긴다.
        # 설계 플레이어가 치르는 값(전투 시간·손실)은 그대로고, 임계만 k 점 앞에 생긴다.
        # 같은 대수 안에서만 옮기므로 k 는 한 대 값의 1/(n-1) 을 못 넘는다 — 초반 층(한 대가
        # 십몇 점)은 +7~+9 를 못 놓고 그대로 둔다. 거기는 저절로 생기는 임계가 두세 점마다 있다.
        def dur_at(bonus, hp):
            p = player_with_runes(ptable, level, floor)
            p.atk += bonus
            return simulate_battle(p, build(hp, 1))[1]
        d0 = dur_at(0, hp_m)
        lo, hi = 1, int(hp_m) * 2 + 2
        if dur_at(breakpoint, lo) < d0 <= dur_at(breakpoint, hi):
            while hi - lo > 1:
                mid = (lo + hi) // 2
                if dur_at(breakpoint, mid) < d0:
                    lo = mid
                else:
                    hi = mid
            if dur_at(0, lo) == d0:
                hp_m = float(lo)

    # 2) ATK 이분 탐색: HP 손실 목표
    target_loss = ps["hp"] * hp_loss_target
    # 하한은 0 이어야 한다. 예전에는 "공격력이 방어력보다 작으면 피해가 없다" 는
    # 셈으로 DEF 에서 시작했는데, 마법은 공격력에 치명 배율을 먼저 곱하므로
    # 하한에서 이미 목표를 넘어 이분 탐색이 한 칸도 움직이지 못했다.
    # 그 결과 마법 몬스터만 목표의 10배를 때렸다.
    lo, hi = 0.0, float(int(ps["dfn"])) + max(20.0, ps["hp"])
    for _ in range(40):
        mid = (lo + hi) / 2
        p = player_with_runes(ptable, level, floor)
        won, _, loss = simulate_battle(p, build(hp_m, mid))
        if not won or loss > target_loss:
            hi = mid
        else:
            lo = mid
    # 내림이다. 이분 탐색이 지킨 "손실 <= 목표" 를 반올림이 깼다 — 초반 층은 한 대가 몇 점뿐이라
    # 0.5 올림이 손실을 5~10% 부풀렸고(5층 목표 32.9% -> 실제 36.7%), 정답 경로에서 물약 하나만
    # 버려도 죽는 층이 17개였다(내리면 7개, 임계를 세 칸으로 나눈 지금은 5개).
    atk_m = max(1.0, float(int(lo)))

    # 특성이 속도까지 바꾸므로 실제로 쓴 값을 돌려준다. 데이터에 들어가는 값과
    # 검증에 쓰인 값이 달라지면 검증이 의미가 없다.
    return hp_m, atk_m, dfn_m, aspd, dspd


def target_level(floor, start_level):
    """F층 도착 시 목표 레벨.

    1~4층은 손수 만든 구간이라 우리가 정할 수 없다. 거기서 실제로 도달하는 레벨
    (start_level)을 5층의 기준으로 삼고, 그 뒤로는 층당 1레벨씩 올라간다.

    예전에는 5층을 start_level + 1 로 셌다. 그러면 챕터 0 내내 플레이어가 설계보다
    한 레벨 낮은 채로 싸워서(첫 보스의 경험치로 20층에서야 따라잡는다) 도입부가
    가장 비쌌다 — 몹 한 마리가 설계의 두 배(8~17%), 20층 보스가 49% 였다.
    """
    return start_level + (floor - HANDMADE_FLOORS - 1)


def build_monsters(ptable, start_level, mob_loss=None):
    """층별 몬스터 1종 + 챕터별 보스 1종을 만든다 (5~100층).

    mob_loss 를 주면 그 손실로 푼다(챕터별 목록도 된다) — 탑의 법(TOWER_MOB_HP_LOSS) 표가 같은
    길로 나온다. id·이름·그림·특성·경험치는 손실과 무관하므로 두 표가 같은 MapData 를 쓴다.
    """
    mob_loss = MOB_HP_LOSS if mob_loss is None else mob_loss
    monsters = []
    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        did, ch, idx = dungeon_id(floor)
        level = target_level(floor, start_level)
        boss = is_boss_floor(floor)
        loss = mob_loss[ch] if isinstance(mob_loss, (list, tuple)) else mob_loss

        # 챕터 안에서 조금씩 조여든다
        ramp = 1.0 + 0.35 * (idx / (FLOORS_PER_CHAPTER - 1))
        aspd = round(0.9 + 0.4 * (idx / (FLOORS_PER_CHAPTER - 1)), 2)

        # 층 몹 전부를 잡으면 정확히 1레벨
        reward = round(exp_to_next(ptable, level) / MOBS_PER_FLOOR)

        k = 0
        for ri, run in enumerate(MOB_SPECIES_RUN):
            # 묶음의 첫 자리로 한 번만 푼다 — 같은 종은 스탯도 특성도 같아야 한다.
            slot = k
            trait = trait_of(floor, ri)
            hp_k, atk_k, dfn_k, aspd_k, dspd_k = solve_monster(
                ptable, level, loss * ramp * MOB_LOSS_RAMP[slot],
                MOB_DURATION * ramp, aspd, trait, floor, breakpoint_of(floor, ri))
            # 층마다 다른 종이 서게 고른다. 가장 센 놈(정예)은 그림이 아니라
            # 색이 진하고 몸집이 커서 눈에 띈다 (MonsterTint / MapBuilder.SetupLook).
            art_idx = (idx + slot) % len(MOB_ART)
            art = MOB_ART[art_idx]
            for _ in range(run):
                monsters.append(dict(
                    id=MOB_ID_BASE + floor * 8 + k, Chapter=ch, Ability=trait,
                    Name=bestiary.mob_name(KR, "kr", ch, art_idx),
                    Attack=float(atk_k), Defence=float(dfn_k), MaxHP=float(hp_k),
                    AttackSpeed=float(aspd_k), DefenceSpeed=float(dspd_k),
                    Critical=99.0, CriticalAttack=200.0,
                    RewardExp=float(reward), RewardItem=-1,
                    IdleAnimStr=art[0], AttackAnimStr=art[1],
                    BattleParticleAttack="FX_WeaponSlash_00",
                    BattleParticleHit="FX_WeaponHit_14",
                    Shadow="Mob_Shadow_000",
                    MonsterNameId=MOB_NAME_BASE + floor * 8 + k,
                    MonsterDescId=MOB_DESC_BASE + floor * 8 + k,
                    _floor=floor, _boss=False, _order=k, _art=art_idx,
                ))
                k += 1

        if boss:
            # 보스는 층의 몹 다섯을 다 잡고 한 레벨 오른 뒤에 만난다(계단 방 입구).
            # 도착 레벨로 풀면 실제로는 설계보다 싸다 — 20층 보스가 28% 가 아니라 15% 였다.
            btrait = BOSS_TRAITS[ch % len(BOSS_TRAITS)]
            bhp, batk, bdfn, baspd, bdspd = solve_monster(
                ptable, level + 1, BOSS_HP_LOSS, BOSS_DURATION, 1.1, btrait, floor,
                BOSS_BREAKPOINT, crit=BOSS_CRIT, dspd_min=BOSS_DSPD_MIN)
            bart_idx = BOSS_ART_INDEX[ch % len(BOSS_ART_INDEX)]
            bart = MOB_ART[bart_idx]
            monsters.append(dict(
                id=BOSS_ID_BASE + ch, Chapter=ch, Ability=btrait,
                Name=KR["bosses"][ch][0],
                Attack=float(batk), Defence=float(bdfn), MaxHP=float(bhp),
                AttackSpeed=float(baspd), DefenceSpeed=float(bdspd),
                Critical=BOSS_CRIT, CriticalAttack=200.0,
                RewardExp=float(round(exp_to_next(ptable, level) * 0.6)),
                # 기획서 107쪽 — 보스를 잡으면 보상 아이템을 떨군다.
                # 능력치가 0 인 반지·목걸이만 준다(기획서 34쪽의 "유틸 기능 해금").
                # 공격력이 붙은 무기를 주면 그 뒤 층의 밸런스가 통째로 어긋난다.
                RewardItem=BOSS_REWARD[ch % len(BOSS_REWARD)],
                IdleAnimStr=bart[0], AttackAnimStr=bart[1],
                BattleParticleAttack="FX_WeaponSlash_00",
                BattleParticleHit="FX_WeaponHit_18",
                Shadow="Mob_Shadow_000",
                MonsterNameId=BOSS_NAME_BASE + ch,
                MonsterDescId=BOSS_DESC_BASE + ch,
                _floor=floor, _boss=True, _art=bart_idx,
            ))
    # 기준 값(par) — 설계한 플레이어(그 층 목표 레벨, 보스는 +1, 계단 룬, 마검, 체력 가득,
    # 치명 횟수 0)가 이 몬스터에게 잃는 HP. 비율이 아니라 <b>절대 HP</b> 다 — 장부(L1)가
    # 실제로 잃은 HP 와 그대로 더하고 뺀다. 세이브 해시(MapData) 밖이라 다시 뽑아도 세이브가 산다.
    for m in monsters:
        lv = target_level(m["_floor"], start_level) + (1 if m["_boss"] else 0)
        _, _, loss = simulate_battle(player_with_runes(ptable, lv, m["_floor"]), creature_of(m))
        m["ParLoss"] = float(loss)
    return monsters


def creature_of(md):
    """데이터 한 줄 -> 시뮬레이터의 몬스터 (데이터에 실린 값 그대로)."""
    return Creature(md["MaxHP"], md["Attack"], md["Defence"], md["AttackSpeed"],
                    md["DefenceSpeed"], md["Critical"], md["CriticalAttack"], md["Ability"])


# ------------------------------------------------------------------ 완주 검증

def simulate_run(ptable, monsters, start_state, verbose=True,
                 skip_optional=False, skip_runes=False, greedy=False,
                 skip_potions=False, behind=0, atk_behind=0, altar=None, waste_floors=(),
                 reserve=None, first=HANDMADE_FLOORS + 1):
    """5층부터 100층까지 실제 전투 공식으로 완주 시뮬레이션.

    1~4층(손수 만든 구간)은 simulate_handmade 가 이미 돌린 뒤라,
    그 결과 상태(start_state)를 이어받아 시작한다.
    포션은 층마다 배치된 것만 사용한다. 죽으면 즉시 실패로 보고한다.

    세 가지 <b>나쁜 선택</b>을 그대로 재현할 수 있다. 정답 경로만 돌려 보면
    "고를 것이 있다" 는 말을 증명할 수 없다 — 잘못 골랐을 때 실제로 죽는지도
    같은 공식으로 재야 한다.

      skip_optional  곁길 몬스터를 지나친다. 관문 셋만 잡으므로 층당 경험치의
                     5분의 2를 버린다. True 면 전 층, 층 번호 목록이면 그 층만.
      skip_runes     룬을 전부 지나친다 (계단 방 입구에 있으니 실제로는
                     밟게 되지만, 안 주웠을 때의 값을 재기 위한 것이다).
      greedy         물약을 보이는 대로 마신다. 가득 찬 채로 마시면 넘치는
                     만큼 그냥 버린다(ConsumableItem.PickUp 이 최대치에서 자른다).
                     <b>이것은 나쁜 선택이 아니다</b> — 아래 dominance 주석 참조.
      skip_potions   막다른 길의 구역 물약을 안 들른다. 계단 앞 물약만 밟는다.
                     이쪽이 진짜 나쁜 선택이다.
      behind         층마다 설계 레벨보다 이만큼 낮은 채로 들어선다. 층 안에서 오른 레벨은
                     다음 층 입구에서 다시 깎인다 — "늘 두 레벨 뒤처진 사람" 을 층마다 재현한다.
      atk_behind     공격력만 설계 레벨보다 이만큼 낮은 레벨의 값이다(방어·체력은 그대로).
                     몬스터 방어를 올린 값이 "공격력 벽" 을 쌓았는지 따로 잰다.
      altar          "atk"/"dfn" 이면 계단의 제단에서 살 수 있을 때마다 그 능력치를 산다(altar_at).
                     reserve 는 층마다 내고 나서 남길 HP(%) — 그 표의 altar_reserves.
      waste_floors   그 층들에서 가장 작은 물약 하나를 버린다(실수 한 번 — route_check 와 같다).
      first          이 층 입구에서 시작한다(start_state 는 그 입구의 상태). 설계 레벨로 들어선다고
                     보고 behind·atk_behind 의 기준도 거기서 센다 — 제단 문턱(altar_reserves)이 쓴다.

    플레이어는 마검을 든 채로 싸운다(stats_with_runes). 전투 사이에 치명 횟수와
    방어 게이지를 넘겨 받지 않는 것만 게임과 다르다 — thesword_balance 머리말 참조.
    """
    by_floor = {}
    for m in monsters:
        by_floor.setdefault(m["_floor"], []).append(m)

    bought = {"atk": 0.0, "dfn": 0.0}     # 제단에서 산 것 — 룬처럼 영구다
    purchases = 0
    base = start_state[0] - (first - HANDMADE_FLOORS - 1)     # 5층 입구의 설계 레벨

    def stats_at(level, floor):
        s = stats_with_runes(ptable, level, floor, runes=not skip_runes)
        s["atk"] += bought["atk"]
        s["dfn"] += bought["dfn"]
        short = target_level(floor, base) - atk_behind
        if atk_behind and level > short:
            s["atk"] -= player_stats_at(ptable, level)["atk"] - player_stats_at(ptable, short)["atk"]
        return s

    level, exp, cur_hp = start_state
    log = []
    spilled = 0.0   # 최대치에 잘려 버려진 회복량 누계 (HP)
    served = 0.0    # 실제로 몸에 들어간 회복량 누계 (HP)
    budget = 0.0    # 층에 놓인 회복량 누계 (HP)

    for floor in range(first, TOTAL_FLOORS + 1):
        floor_mons = by_floor[floor]
        mobs = sorted((m for m in floor_mons if not m["_boss"]),
                      key=lambda m: m["_order"])
        if skip_optional is True or (skip_optional and floor in skip_optional):
            # 관문에는 약한 놈부터 선다(layout_gen 의 배치 순서와 같다).
            # 보스는 계단 방 입구에 서 있으므로 지나칠 수 없다.
            mobs = mobs[:FORCED_PER_FLOOR]
        boss = next((m for m in floor_mons if m["_boss"]), None)

        if behind and level > target_level(floor, base) - behind:
            level, exp = target_level(floor, base) - behind, 0.0
            cur_hp = min(cur_hp, stats_at(level, floor)["hp"])

        entry_level, entry_hp = level, cur_hp
        low = 1.0           # 이 층에서 전투를 치른 직후 가장 낮았던 HP (최대 HP 대비)
        floor_spill = 0.0
        fight_cost = []     # (보스인가, 최대 HP 대비 잃은 %) — 전투 예측이 보여 줄 값
        # 미로가 강제하는 순서 그대로. 약한 놈부터, 마지막에 보스.
        fights = list(mobs) + ([boss] if boss else [])

        # 구역별 포션. (회복비율, 쓸 수 있게 되는 전투 인덱스)
        # 구역1 포션은 두 번째 전투부터, 구역2 포션은 네 번째 전투부터 닿는다.
        #
        # <b>층 유형이 정한 예산을 그대로 써야 한다.</b> 예전에는 여기만 늘
        # 두 개로 셌는데, 실제로 놓이는 것은 「인색」 층이 하나뿐이고
        # 「넉넉」 층이 셋이다 — 물약이 하나뿐인 19개 층을 두 개로 셈하고
        # "완주 보장" 이라 부르고 있었다. emit_layouts·route_check 와 같은
        # floor_potions 를 본다.
        heals = floor_potions(floor)
        potions = [] if skip_potions else [
            (h, min(1 + k * 2, len(fights) - 1)) for k, h in enumerate(heals)]
        # 보스층은 보스 직전에 쓸 큰 물약과, 올라가기 전에 채울 물약을 따로 둔다.
        # 큰 것 하나만 두면 보스를 잡고 빈사로 다음 층에 올라가 그대로 죽는다.
        # 이 둘은 구역3(계단 앞)이라 <b>반드시 밟는다</b> — skip_potions 로도 안 빠진다.
        # 큰 물약은 보스 앞, 계단 앞 물약은 보스 뒤에 놓인다 — build_floor_layout 이
        # 그렇게 놓고 check_behind_boss 가 완성된 격자에서 다시 잰다([4/7]).
        if boss:
            potions.append((BOSS_FLOOR_POTIONS[0], len(fights) - 1))
        potions.append((EXIT_POTION, len(fights)))
        if floor in waste_floors:
            potions.remove(min(potions, key=lambda t: t[0]))

        for i, md in enumerate(fights):
            stats = stats_at(level, floor)
            m = Creature(md["MaxHP"], md["Attack"], md["Defence"],
                         md["AttackSpeed"], md["DefenceSpeed"],
                         md["Critical"], md["CriticalAttack"], md["Ability"])

            # 포션은 주우면 즉시 회복이고 최대치에서 잘린다(ConsumableItem.PickUp).
            # 그래서 넘치게 마시면 그만큼 버리는 것이고, 정답 경로는 "죽지 않을
            # 만큼만, 가장 늦게" 든다. 이 전투를 그냥 치러 보고 죽을 때만 마신다.
            if greedy:
                # 나쁜 선택: 보이면 바로 마신다. 넘치는 만큼은 그대로 버린다.
                for t in [t for t in potions if t[1] <= i]:
                    heal = stats["hp"] * t[0]
                    floor_spill += max(0.0, cur_hp + heal - stats["hp"])
                    served += min(heal, stats["hp"] - cur_hp)
                    cur_hp = min(stats["hp"], cur_hp + heal)
                    potions.remove(t)
            while not greedy:
                probe = Creature(stats["hp"], stats["atk"], stats["dfn"],
                                 stats["aspd"], stats["dspd"], stats["crit"],
                                 stats["crit_atk"])
                probe.hp = min(cur_hp, stats["hp"])
                probe_m = Creature(md["MaxHP"], md["Attack"], md["Defence"],
                                   md["AttackSpeed"], md["DefenceSpeed"],
                                   md["Critical"], md["CriticalAttack"], md["Ability"])
                survives, _, _ = simulate_battle(probe, probe_m)
                if survives:
                    break
                usable = [t for t in potions if t[1] <= i]
                if not usable:
                    break
                heal, _ = usable[0]
                potions.remove(usable[0])
                floor_spill += max(0.0, cur_hp + stats["hp"] * heal - stats["hp"])
                served += min(stats["hp"] * heal, stats["hp"] - cur_hp)
                cur_hp = min(stats["hp"], cur_hp + stats["hp"] * heal)

            p = Creature(stats["hp"], stats["atk"], stats["dfn"],
                         stats["aspd"], stats["dspd"], stats["crit"],
                         stats["crit_atk"])
            p.hp = min(cur_hp, stats["hp"])

            won, dur, loss = simulate_battle(p, m)
            cur_hp = p.hp
            low = min(low, cur_hp / stats["hp"])
            fight_cost.append((md["_boss"], 100.0 * loss / stats["hp"]))
            if not won:
                return False, log, (
                    f"{floor}층 {i + 1}번째 전투에서 사망 "
                    f"(Lv{level}, 진입HP {entry_hp:.0f}/{stats['hp']:.0f}, "
                    f"몬스터 {md['Name']} HP{md['MaxHP']:.0f} ATK{md['Attack']:.0f})")

            # 경험치 -> 레벨업. 한 번에 여러 레벨이 오를 수 있다 — C# CurExp 세터도
            # 같은 while 이어야 한다 (도입부 킹 슬라임은 한 번에 두 레벨 넘게 준다).
            exp += md["RewardExp"]
            while level + 1 in ptable and exp >= ptable[level + 1]["need_exp"]:
                exp -= ptable[level + 1]["need_exp"]
                level += 1
                cur_hp += ptable[level]["hp"]  # LevelUp() 은 CurHP 도 같이 올린다

        # 계단 앞 회복은 올라가기 직전에 든다. 이미 가득하면 그만큼 버린다.
        # 이 층의 룬도 계단 앞에 있으니 여기서 최대 체력이 올라간다.
        stats_end = stats_at(level, floor + 1)
        for heal, avail_at in list(potions):
            if avail_at >= len(fights):
                floor_spill += max(0.0, cur_hp + stats_end["hp"] * heal - stats_end["hp"])
                served += min(stats_end["hp"] * heal, stats_end["hp"] - cur_hp)
                cur_hp = min(stats_end["hp"], cur_hp + stats_end["hp"] * heal)
                potions.remove((heal, avail_at))
        spilled += floor_spill
        budget += stats_end["hp"] * placed_heal(floor)

        # 제단은 계단에 있다 — 계단 앞 물약을 마신 뒤, 올라가기 직전에 한 번 묻는다.
        # 값은 최대 HP 의 %, 내고 나서 그 층의 문턱(reserve)이 안 남으면 팔지 않는다(ALTAR_* 주석).
        paid = 0.0
        gain = altar_at(floor) if altar else None
        if gain:
            price = stats_end["hp"] * altar_price(purchases + 1) / 100.0
            if altar_sells(cur_hp, stats_end["hp"], purchases + 1, reserve[floor]):
                cur_hp -= price
                paid = price
                purchases += 1
                bought[altar] += gain[0 if altar == "atk" else 1]

        log.append(dict(floor=floor, entry_level=entry_level, exit_level=level, exit_exp=exp,
                        hp=cur_hp, max_hp=stats["hp"], spill=floor_spill,
                        spill_total=spilled, served=served, budget=budget,
                        hp_pct=100.0 * cur_hp / stats["hp"], fights=fight_cost,
                        low=100.0 * low, paid=paid, purchases=purchases))
        if verbose and (floor % 10 == 0 or floor == HANDMADE_FLOORS + 1
                        or is_boss_floor(floor)):
            tag = "BOSS" if is_boss_floor(floor) else "    "
            print(f"  {tag} {floor:>3}층  Lv{entry_level:>3}->{level:>3}  "
                  f"HP {cur_hp:>6.0f}/{stats['hp']:>6.0f} ({100.0 * cur_hp / stats['hp']:>5.1f}%)")

    return True, log, None


def report_fight_costs(log):
    """전투 한 번의 값(최대 HP 대비 잃는 %)을 챕터별로 찍는다.

    전투 예측이 보여 줄 숫자다. 이게 몇 % 에 머무느냐가 예측을 볼 이유를 정한다 —
    마검을 빼고 셌을 때는 실제 전투가 1~7% 라 예측이 늘 초록이었다.
    """
    for ch in range(len(CHAPTER_THEMES)):
        rows = [r for r in log if dungeon_id(r["floor"])[1] == ch]
        mobs = sorted(p for r in rows for boss, p in r["fights"] if not boss)
        bosses = [p for r in rows for boss, p in r["fights"] if boss]
        print(f"      챕터 {ch} 전투 한 번의 값: 몹 중앙값 {statistics.median(mobs):.1f}% · "
              f"상위 10% {mobs[int(0.9 * (len(mobs) - 1))]:.1f}% · 최대 {mobs[-1]:.1f}%"
              + (f" · 보스 {bosses[0]:.0f}%" if bosses else ""))
    print("      (치명 횟수·방어 게이지는 전투마다 0 에서 센다. 게임은 전투 사이에 이어"
          " 받으므로 치명타가 같거나 먼저 나온다 — 합으로는 보수적이고, 한 전투만 보면"
          " 싸우는 순서에 따라 몇 대 어긋난다)")


# ------------------------------------------------------------------ 나쁜 선택
#
# 고를 것이 있다는 말은 <b>잘못 골랐을 때 값을 치른다</b>는 뜻이다. 값이 없으면
# 그건 선택이 아니라 장식이다. 그래서 정답 경로만 돌리지 않고 나쁜 선택도 같은
# 공식으로 돌려 어느 층에서 죽는지 잰다.
#
# <b>물약을 넘치게 마시는 것은 벌줄 수 없다.</b> 예산 문제가 아니라 규칙 문제다.
#
#   1) 전투에서 잃는 HP 는 시작 HP 와 무관하다 (플레이어 공격력·공속이 HP 에
#      안 걸리고 몬스터 HP 도 고정이라 전투 길이가 고정이다).
#      thesword_balance._self_check() 가 이 전제를 붙들고 있다.
#   2) 물약은 min(maxHP, hp + heal) 로 들어간다. 이 식은 hp 에 대해 단조 증가다.
#   3) 물약은 층을 넘겨 들고 갈 수 없다 — 안 마시면 그 층에서 그냥 사라진다.
#
# 셋을 합치면 "같은 물약을 더 일찍 마신 쪽" 의 HP 는 어느 시점에서도 늦게 마신
# 쪽보다 낮을 수 없다. 즉 <b>보이는 대로 마시는 것이 지배 전략</b>이다.
# 예산을 깎으면 넘침이 줄어드는 만큼 정답 경로가 먼저 죽는다 (route_check.py
# --budget 이 배율별로 찍는다: 0.7배에서 정답 11층 사망, 탐욕은 완주).
#
# 그래서 물약에서 값을 치르는 선택은 "언제 마시나" 가 아니라
# <b>"어디까지 들르나"</b> 다. 그쪽(skip_potions)은 실제로 문다.
#
# (이름, simulate_run 인자, 죽어야 하는가, 완주했을 때 붙일 말)
BAD_ROUTES = [
    ("곁길 몬스터를 전부 지나친다", dict(skip_optional=True), True, ""),
    ("룬을 전부 지나친다", dict(skip_runes=True), True, ""),
    ("막다른 길의 물약을 안 들른다", dict(skip_potions=True), True, ""),
    ("물약을 보이는 대로 마셔 넘친다", dict(greedy=True), False,
     "벌이 없는 것이 맞다 — 즉시 회복 + 최대치 절삭 + 이월 불가 라서 "
     "일찍 마시는 쪽이 지배 전략이다 (위 주석)"),
    # 몬스터 방어가 공격의 0.6~0.7 이라(MONSTER_DEF_RATIO) 한 대 값 max(1, 공격-방어) 가
    # 레벨에 민감하다. 가격표가 "공격력 벽" 을 쌓았다면 공격만 모자란 사람이 여기서 막힌다 —
    # 그래서 이것은 완주해야 한다(방어 0.8 에서는 20층 야수 보스에서 막혔다).
    ("공격력만 두 레벨 모자란 채 오른다", dict(atk_behind=2), False,
     "공격이 모자라도 벽이 아니라 값이다 — 더 아플 뿐 막히지는 않는다"),
    # 레벨이 통째로 둘 모자라면 죽는다. 이건 방어를 올리기 전에도 6층에서 죽었다 — 몬스터 공격이
    # 플레이어 방어에 맞춰 역산돼 한 대가 몇 점뿐이라, 방어 2~10 이 모자라면 한 대가 두세 배가 된다.
    # "층당 정확히 1레벨" 설계의 값이라 문으로 삼지 않고 어디서 죽는지만 찍는다.
    ("레벨이 통째로 둘 모자란 채 오른다 (물약은 보이는 대로)", dict(behind=2, greedy=True), None, ""),
    # 제단(ALTAR_*)은 완주 계산 밖이다. 안 사도(= 정답 경로, [3/7]) 다 사도 완주해야 한다.
    ("제단에서 살 수 있을 때마다 공격을 산다", dict(altar="atk"), False, ""),
    ("제단에서 살 수 있을 때마다 방어를 산다", dict(altar="dfn"), False, ""),
]


def optional_slack(ptable, monsters, start_state):
    """<b>몇 층부터</b> 곁길을 전부 지나쳐도 완주하는가.

    "다 지나치면 11층에서 죽는다" 만으로는 선택의 폭을 모른다. 재 보면 빡빡한
    곳은 아래쪽이다 — 5층의 곁길을 지나치면 10층에서 죽지만 50층·95층은
    지나쳐도 완주한다. 그래서 F층부터 위로 전부 지나쳐 보며 완주하는 가장 낮은
    F 를 찾는다.

    슬랙이 층수에 대해 단조롭다고 보고 이분 탐색한다 — 정확한 경계가 아니라
    폭을 대략 재는 것이다(전 층을 따로 재면 96번 돌려야 한다).
    """
    lo, hi = HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1
    while lo < hi:
        mid = (lo + hi) // 2
        ok, _, _ = simulate_run(ptable, monsters, start_state, verbose=False,
                                skip_optional=range(mid, TOTAL_FLOORS + 1))
        if ok:
            hi = mid
        else:
            lo = mid + 1
    return lo


def waste_tolerance(ptable, monsters, start_state, greedy=False, limit=20):
    """물약을 몇 개까지 버려도 완주하는가 — 층에 고르게 흩어 가장 작은 것을 하나씩 버린다
    (route_check.tolerance 와 같은 셈이고, greedy 면 보이는 대로 마시는 사람이다)."""
    floors = list(range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1))
    for n in range(1, limit + 1):
        picks = [floors[int(i * len(floors) / n)] for i in range(n)]
        if not simulate_run(ptable, monsters, start_state, verbose=False, greedy=greedy,
                            waste_floors=picks)[0]:
            return n - 1
    return limit


def check_tower(ptable, tower, start_state):
    """탑의 법 표의 완주 보장. 기준은 정답 경로가 아니라 <b>보이는 대로 마시는</b> 경로다 —
    그쪽이 지배 전략이고(나쁜 선택 주석), 탑의 법에서 정답 경로는 11층에서 죽는다."""
    loss = "/".join(f"{x:g}" for x in TOWER_MOB_HP_LOSS)
    ok, log, err = simulate_run(ptable, tower, start_state, verbose=False, greedy=True)
    if not ok:
        print(f"  [실패] 탑의 법 (챕터별 몹 손실 {loss}): 보이는 대로 마셔도 {err}")
        return False
    n = waste_tolerance(ptable, tower, start_state, greedy=True)
    bud, _, spl, _ = potion_ledger(log)
    bosses = [p for r in log for boss, p in r["fights"] if boss]
    head = "[실패]" if n < TOWER_MIN_WASTE else "      "
    print(f"      탑의 법 (챕터별 몹 손실 {loss}) … 보이는 대로 마시면 완주, 최종 Lv{log[-1]['exit_level']}")
    print(f"              챕터별 최저 HP {chapter_lows(log)} · 보스 한 판 {min(bosses):.0f}~{max(bosses):.0f}%")
    print(f"      {head} 물약을 {n}개 버려도 완주 (문턱 {TOWER_MIN_WASTE}) · 넘쳐 버린 회복 "
          f"{spl:,.0f} HP (놓인 것의 {100.0 * spl / bud:.0f}%)")
    return n >= TOWER_MIN_WASTE


def designed_price(ptable, monsters, start_level):
    """저울: 설계한 플레이어(목표 레벨, 보스는 +1)가 한 층의 몬스터 전부에게 잃는 HP 를 돌려주는
    함수. 공격·방어를 얹어 재면 크기 룬과 제단이 "몇 HP 값인가" 가 나온다."""
    by_floor = {}
    for m in monsters:
        by_floor.setdefault(m["_floor"], []).append(m)

    @functools.lru_cache(maxsize=None)
    def price(floor, datk=0, ddfn=0):
        tot = 0.0
        for md in by_floor.get(floor, ()):
            p = player_with_runes(ptable, target_level(floor, start_level) + (1 if md["_boss"] else 0), floor)
            p.atk += datk
            p.dfn += ddfn
            won, _, loss = simulate_battle(p, creature_of(md))
            tot += loss if won else p.max_hp
        return tot
    return price


def stat_value(price, ptable, start_level, floor, stat, amount, have=0, drink=0.0,
               horizon=RUNE_HORIZON):
    """floor 에서 얻은 능력치가 다음 horizon 층(floor+1 ~, 100층에서 끊는다) 동안 하는 값,
    floor 의 최대 HP 대비 %.
      공격·방어  설계 플레이어가 아끼는 HP. have 는 이미 얹은 같은 능력치.
      체력       얻은 양 + 그 층들에 놓인 물약이 최대 HP 에 비례해 더 채우는 몫. 물약은 다 마시지
                 않으므로 drink(놓인 회복 중 정답 경로가 마시는 비율, potion_ledger)만큼만 센다.
    """
    max_hp = stats_with_runes(ptable, target_level(floor, start_level), floor)["hp"]
    end = min(TOTAL_FLOORS, floor + horizon)
    if stat == 2:
        heal = sum(placed_heal(g) for g in range(floor + 1, end + 1))
        return 100.0 * amount * (1.0 + drink * heal) / max_hp
    a0, a1 = (have, have + amount) if stat == 0 else (0, 0)
    d0, d1 = (have, have + amount) if stat == 1 else (0, 0)
    return 100.0 * sum(price(g, a0, d0) - price(g, a1, d1) for g in range(floor + 1, end + 1)) / max_hp


STAT_NAME = ("공격", "방어", "체력")
STAT_OBJ = ("공격을", "방어를", "체력을")


def report_breakpoints(ptable, monsters, start_level):
    """임계가 룬을 집은 뒤에도 남는가. 설계보다 공격이 +0/+3/+6 인 사람에게 공격 +1/+2/+3 이
    층의 몹 다섯에게서 최대 HP 의 1% 넘게 아끼는 층 수(96개 중). 크기 룬·제단의 공격은 한 번에
    +3 이나 +6 이라, +0 만 재면 룬 하나 집기 전의 사람만 본다(예전 설계가 62 -> 10 이었다)."""
    by_floor = {}
    for m in monsters:
        if not m["_boss"]:
            by_floor.setdefault(m["_floor"], []).append(m)
    cols = []
    for off in (0, 3, 6):
        hits = [0, 0, 0]
        for f, mobs in by_floor.items():
            s = stats_with_runes(ptable, target_level(f, start_level), f)

            def paid(datk):
                tot = 0.0
                for md in mobs:
                    p = Creature(s["hp"], s["atk"] + datk, s["dfn"], s["aspd"], s["dspd"],
                                 s["crit"], s["crit_atk"])
                    won, _, lost = simulate_battle(p, creature_of(md))
                    tot += lost if won else s["hp"]
                return tot
            base = paid(off)
            for i in range(3):
                hits[i] += (base - paid(off + i + 1)) >= 0.01 * s["hp"]
        cols.append(hits)
    print("      임계 — 공격 +1/+2/+3 이 1% 넘게 아끼는 층: " +
          " · ".join(f"설계 +{o} 에서 {h[0]}/{h[1]}/{h[2]}" for o, h in zip((0, 3, 6), cols)))
    if cols[0][2] < 35:
        print(f"      [주의] 설계 상태에서 공격 +3 이 값을 하는 층이 {cols[0][2]}개다 (35 미만)")


def report_rune_tiers(ptable, monsters, start_level, drink):
    """크기 룬이 놓이는 층마다 그 챕터의 공격·방어·체력 룬 중 무엇이 가장 값진가(stat_value).
    한 능력치가 7할을 넘기면 금고 색도, 둘 중 하나도 고를 까닭이 없다 — [주의] 로 알린다
    (문으로 삼지 않는다: 설계 상태 하나로 잰 값이라 정답표가 아니라 경향이다). 셈법에 따라
    갈리는 것을 숨기지 않으려고 100층까지 센 몫도 같이 찍는다."""
    price = designed_price(ptable, monsters, start_level)
    offers = sorted({f for f in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1)
                     if (alcove_plan(f) or ("",))[0] == "vault" or choice_pair(f)})

    def shares(horizon):
        best = collections.Counter()
        for f in offers:
            tier = rune_tier(f)
            vals = [stat_value(price, ptable, start_level, f, s, tier[s], drink=drink, horizon=horizon)
                    for s in range(3)]
            best[max(range(3), key=lambda s: vals[s])] += 1
        return [100.0 * best[s] / len(offers) for s in range(3)]
    main, far = shares(RUNE_HORIZON), shares(TOTAL_FLOORS)
    print("      크기 룬 (챕터별 공격/방어/체력) " +
          " · ".join(f"{a}/{d}/{h}" for a, d, h in RUNE_TIERS))
    print(f"      크기 룬이 놓이는 {len(offers)}개 층에서 가장 값진 것 (다음 {RUNE_HORIZON}층 셈, 체력은 물약 몫 "
          f"{drink:.2f} 포함) — " + " · ".join(f"{STAT_NAME[s]} {main[s]:.0f}%" for s in range(3)) +
          "  (100층까지 세면 " + "/".join(f"{x:.0f}" for x in far) + ")")
    for s in range(3):
        if not 20.0 <= main[s] <= 70.0:
            print(f"      [주의] {STAT_NAME[s]} 룬이 {main[s]:.0f}% 층에서 가장 값지다 (20~70% 밖) — RUNE_TIERS 를 다시 맞출 것")


def altar_entry(ptable, log, floor, pct):
    """floor 의 제단에서 사고 pct% 를 남긴 채 다음 층에 들어서는 상태 (레벨, 경험치, HP).
    레벨·경험치는 정답 경로(log) 그대로다 — 물약 습관과 무관하게 같은 순서로 같은 놈을 잡는다.
    경험치를 0 으로 두면 안 된다: 층의 몹 다섯이 주는 경험치는 반올림이라 딱 1레벨에 몇 점
    모자랄 수 있고, 도입부에서 넘겨받은 여분이 없으면 그 층 끝에 레벨이 안 올라 한 레벨 뒤처진다."""
    r = next(r for r in log if r["floor"] == floor)
    max_hp = stats_with_runes(ptable, r["exit_level"], floor + 1)["hp"]
    return r["exit_level"], r["exit_exp"], max_hp * pct / 100.0


def altar_reserves(ptable, monsters, log):
    """제단 층마다 내고 나서 남겨야 할 HP (최대 HP 의 %). 그 HP 로 다음 층에 들어서도 — 설계 레벨,
    산 것 없이, 보이는 대로 마시며 — <b>100층까지</b> 가는 가장 작은 값에 ALTAR_MARGIN 을 얹는다.
    다음 층만 넘기면 되는 것이 아니다: 층 입구마다 체크포인트라 그 뒤 어느 층에서 막혀도 같은
    HP 로 되살아나 갇힌다. 탑의 법은 넘치는 물약이 거의 없어(6%) 모자란 HP 가 몇 층씩 이어지므로
    문턱이 높다 — 제단은 거기서 "남는 HP 로만 산다" 가 된다.
    물약은 min(최대, 현재 + 회복) 이라 HP 가 많아서 손해 보는 일이 없으니 이분 탐색한다.
    100% 로도 못 가는 층은 None — [7/7] 이 실패로 알린다. log 는 [3/7] 정답 경로."""
    out = {}
    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS):
        if not altar_at(floor):
            continue

        def finishes(pct):
            return simulate_run(ptable, monsters, altar_entry(ptable, log, floor, pct), verbose=False,
                                greedy=True, first=floor + 1)[0]
        if not finishes(100):
            out[floor] = None
            continue
        lo, hi = 0, 100             # lo 로는 못 가고 hi 로는 간다
        while hi - lo > 1:
            mid = (lo + hi) // 2
            lo, hi = (lo, mid) if finishes(mid) else (mid, hi)
        out[floor] = hi + ALTAR_MARGIN
    return out


def check_altar(ptable, tables, start_state, log):
    """제단 [7/7]. tables = [(이름, 몬스터 표, altar_reserves)], log 는 [3/7] 정답 경로.
      문턱   제단 다음 층에 그 표의 문턱 HP 그대로 들어서서 100층까지 간다(보이는 대로 마신다).
      다 산다 살 수 있을 때마다 공격(또는 방어)을 사도 완주한다 — 두 표 모두, 보이는 대로 마신다.
    일반 표는 구매마다 값과 그 구매가 다음 RUNE_HORIZON 층 동안 아끼는 HP 를 나란히 찍는다 —
    값보다 적게 아끼는 구매가 나와야 "다 사는 것" 이 답이 아니다."""
    ok_all = True
    for label, monsters, reserves in tables:
        edge = []
        for floor, pct in sorted(reserves.items()):
            if pct is None:
                edge.append(f"{floor + 1}층은 가득 찬 HP 로 들어서도 못 간다")
                continue
            ok, _, err = simulate_run(ptable, monsters, altar_entry(ptable, log, floor, pct),
                                      verbose=False, greedy=True, first=floor + 1)
            if not ok:
                edge.append(f"{floor}층 제단에서 {pct:.0f}% 를 남기고 올라가면 {err.split(' (')[0]}")
        pcts = [p for p in reserves.values() if p is not None]
        if edge:
            ok_all = False
            for line in edge[:5]:
                print(f"  [실패] 제단 문턱 ({label}): {line}")
        else:
            print(f"      제단 문턱 ({label}) 내고 남길 HP {min(pcts):.0f}~{max(pcts):.0f}% — 그 HP 로 다음 층에 "
                  f"들어서도 보이는 대로 마시면 100층까지 간다 ({len(pcts)}곳)")

        price = designed_price(ptable, monsters, start_state[0]) if label == "일반" else None
        for stat, name in ((0, "atk"), (1, "dfn")):
            ok, run, err = simulate_run(ptable, monsters, start_state, verbose=False, altar=name,
                                        greedy=True, reserve=reserves)
            if not ok:
                ok_all = False
                print(f"  [실패] 제단 ({label}) 살 수 있을 때마다 {STAT_OBJ[stat]} 사면 {err.split(' (')[0]}")
                continue
            print(f"      제단 ({label}) 살 수 있을 때마다 {STAT_OBJ[stat]} 산다 … 완주 · {run[-1]['purchases']}번, "
                  f"낸 HP {sum(r['paid'] for r in run):,.0f} · 최저 HP {chapter_lows(run)}")
            if price is None:
                continue
            have, cells, worth = 0, [], 0
            for r in run:
                if r["paid"] <= 0:
                    continue
                gain = altar_at(r["floor"])[stat]
                v = stat_value(price, ptable, start_state[0], r["floor"], stat, gain, have)
                cost = altar_price(r["purchases"])
                worth += v >= cost
                cells.append(f"{r['floor']}층 {cost:.0f}%:{v:.0f}%{'+' if v >= cost else '-'}")
                have += gain
            print(f"              제값 하는 구매 {worth}/{len(cells)} — 값:다음 {RUNE_HORIZON}층 동안 아끼는 HP  "
                  + " ".join(cells))
    return ok_all


def potion_ledger(log):
    """물약 장부 한 줄. (놓인 예산, 마신 것, 넘쳐 버린 것, 손도 안 댄 것)"""
    if not log:
        return 0.0, 0.0, 0.0, 0.0
    r = log[-1]
    return (r["budget"], r["served"], r["spill_total"],
            r["budget"] - r["served"] - r["spill_total"])


def chapter_lows(log):
    """챕터별 최저 HP (층 안, 전투 직후, 최대 HP 대비 %) — '42%@37' 꼴."""
    out = []
    for ch in range(len(CHAPTER_THEMES)):
        rows = [r for r in log if dungeon_id(r["floor"])[1] == ch]
        if rows:
            r = min(rows, key=lambda r: r["low"])
            out.append(f"{r['low']:.0f}%@{r['floor']}")
    return " · ".join(out)


def check_bad_routes(ptable, monsters, start_state, reserves):
    """나쁜 선택마다 어디서 죽는지 찍는다. 죽어야 할 것이 살아남거나,
    완주해야 할 것이 죽으면 실패. reserves 는 일반 표의 제단 문턱(altar_reserves)."""
    ok_all = True
    base_log = None
    for name, kwargs, must_die, note in BAD_ROUTES:
        ok, log, err = simulate_run(ptable, monsters, start_state, verbose=False,
                                    reserve=reserves, **kwargs)
        failed = ok == must_die            # must_die 가 None 이면 찍기만 한다
        if failed:
            ok_all = False
        greedy_only = kwargs == dict(greedy=True)
        head = "[실패]" if failed else "[참고]" if must_die is None else "      "
        tail = (("완주 — 벌이 없다" if must_die or greedy_only else "완주")
                if ok else err.split(" (")[0])
        print(f"      {head} {name} … {tail}")
        if ok and note:
            print(f"              {note}")
        if ok and must_die is False and not greedy_only:
            print(f"              챕터별 최저 HP {chapter_lows(log)}"
                  + (f" · 제단 {log[-1]['purchases']}번, 낸 HP {sum(r['paid'] for r in log):,.0f}"
                     if kwargs.get("altar") else ""))
        if greedy_only and ok:
            # 넘침의 값을 숫자로 찍는다. 정답 경로와 나란히 놓아야 "손해가 없다" 가
            # 인상이 아니라 계산이 된다.
            if base_log is None:
                _, base_log, _ = simulate_run(ptable, monsters, start_state,
                                              verbose=False)
            bud, srv, spl, idle = potion_ledger(log)
            _, bsrv, _, bidle = potion_ledger(base_log)
            lo = min(log, key=lambda r: r["hp_pct"])
            blo = min(base_log, key=lambda r: r["hp_pct"])
            print(f"              놓인 회복 예산 {bud:,.0f} HP 중 "
                  f"넘쳐 버림 {spl:,.0f} ({100.0 * spl / bud:.0f}%)")
            print(f"              그런데도 몸에 들어간 양은 정답 경로보다 많다 — "
                  f"{srv:,.0f} vs {bsrv:,.0f} HP")
            print(f"              (정답 경로는 예산의 {100.0 * bidle / bud:.0f}% 를 "
                  f"손도 안 대고 두고 간다)")
            print(f"              최저 HP  탐욕 {lo['floor']}층 {lo['hp_pct']:.1f}% "
                  f"vs 정답 {blo['floor']}층 {blo['hp_pct']:.1f}%")

    f = optional_slack(ptable, monsters, start_state)
    if f > TOTAL_FLOORS:
        print("             곁길에 여유가 생기는 층 … 없다 (끝까지 다 잡아야 한다)")
    else:
        print(f"             곁길에 여유가 생기는 층 … {f}층부터는 전부 "
              f"지나쳐도 완주 (그 아래는 못 지나친다)")
    return ok_all


# ------------------------------------------------------------------ 파일 출력

def write_csv(path, header, rows):
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(header)
        w.writerows(rows)


def write_json(path, root_key, items):
    with open(path, "w", encoding="utf-8") as f:
        json.dump({root_key: items}, f, ensure_ascii=False, indent=2)


def emit_consumable_items():
    """ConsumableItemData — 0~11 은 손으로 쓴 원본 그대로 두고, 12~ 는 크기 룬(SIZED_RUNES)을 쓴다.

    크기 룬은 같은 능력치의 기본 룬(9/10/11)에게서 그림·이펙트·그림자·설명을 빌린다 — 맵 위
    그림은 ConsumableItem 이 기본 룬 애니메이션으로 튼다(ItemAnimator 에 12~ 상태가 없다).
    """
    path = os.path.join(EXCEL, "ConsumableItemData.csv")
    with open(path, "r", encoding="utf-8-sig", newline="") as f:
        rows = list(csv.reader(f))
    header = rows[0]
    body = [r for r in rows[1:] if r and r[0].strip() and int(r[0]) < SIZED_RUNE_BASE]
    base = {int(r[0]): r for r in body}
    for i, (stat, amount) in enumerate(SIZED_RUNES):
        src = base[9 + stat]
        gain = [0, 0, 0]
        gain[stat] = amount
        body.append([str(SIZED_RUNE_BASE + i), "0", *map(str, gain), src[5], src[6], src[7],
                     str(SIZED_RUNE_NAME_BASE + i), src[9]])
    write_csv(path, header, body)
    write_json(os.path.join(JSOND, "ConsumableItemData.json"), "consumableItems", [
        dict(id=int(r[0]), Heal=float(r[1]), AttackUp=float(r[2]), DefenceUp=float(r[3]),
             HPUp=float(r[4]), Img=r[5], PrefabName=r[6], Shadow=r[7],
             ScriptNameId=int(r[8]), ScriptDescriptionId=int(r[9])) for r in body])


def emit_player_data(ptable):
    header = ["Lv", "NeedEXP", "TotalExp", "공격력", "방어력", "체력",
              "공격속도", "방어속도", "치명타", "치명공격력", "이동속도"]
    rows, items = [], []
    for lv in sorted(ptable):
        r = ptable[lv]
        rows.append([lv, int(r["need_exp"]), int(r["total_exp"]), r["atk"], r["dfn"],
                     r["hp"], r["aspd"], r["dspd"], r["crit"], r["crit_atk"], r["mspd"]])
        items.append(dict(id=lv, NeedExp=r["need_exp"], TotalExp=r["total_exp"],
                          Attack=r["atk"], Defence=r["dfn"], MaxHP=r["hp"],
                          AttackSpeed=r["aspd"], DefenceSpeed=r["dspd"],
                          Critical=r["crit"], CriticalAttack=r["crit_atk"],
                          MoveSpeed=r["mspd"]))
    write_csv(os.path.join(EXCEL, "PlayerData.csv"), header, rows)
    write_json(os.path.join(JSOND, "PlayerData.json"), "creatures", items)


TOWER_TABLE = "MonsterData_Tower"   # 탑의 법 몬스터 표 (JsonData/ 에 같은 꼴로)


def emit_monster_data(monsters, tower):
    """기존 몬스터(0~16)는 보존하고 새 몬스터만 뒤에 붙인다.

    보존하되 떨구는 장비만은 킹 슬라임 것만 남긴다. 손수 만든 표는 "몬스터 i 가
    장비 i 를 떨군다" 는 자리표여서, 1~2층의 망고 슬라임·크로우·정령·늑대가 부츠
    1~4 를, 분열 슬라임 셋이 목걸이 5 를 떨궜다 — 20~80층 바닥의 부츠와 킹 슬라임의
    목걸이를 도입부에서 이미 여러 켤레 받고 있었다. 같은 장비는 한 번만 준다.

    탑의 법 표(tower)도 같은 줄들로 통째로 쓴다 — 새 게임에서 MonsterData 대신 통째로
    갈아 끼울 수 있게. 손수 만든 줄은 둘이 같다. CSV 는 기본 표만 쓴다(사람이 읽는 사본).
    """
    existing_path = os.path.join(JSOND, "MonsterData.json")
    with open(existing_path, "r", encoding="utf-8") as f:
        existing = json.load(f)["creatures"]
    keep = [dict(m, RewardItem=m["RewardItem"] if m["id"] == HANDMADE_BOSS_ID else -1)
            for m in existing if m["id"] < NEW_MONSTER_ID_BASE]

    def table(ms):
        return list(keep) + [{k: v for k, v in m.items() if not k.startswith("_")} for m in ms]
    items = table(monsters)
    write_json(existing_path, "creatures", items)
    write_json(os.path.join(JSOND, TOWER_TABLE + ".json"), "creatures", table(tower))

    header = ["ID", "챕터", "특성 ID", "몹이름", "공격력", "방어력", "체력", "공격속도",
              "방어속도", "치명타", "치명공격력", "보상_경험치", "보상 아이템",
              "몬스터_대기", "몬스터_공격", "전투파티클_공격", "전투파티클_피격",
              "몬스터_그림자", "몹 이름 ID", "몹 설명 ID", "기준 손실(HP)"]
    rows = [[i["id"], i["Chapter"], i["Ability"], i["Name"], i["Attack"], i["Defence"],
             i["MaxHP"], i["AttackSpeed"], i["DefenceSpeed"], i["Critical"],
             i["CriticalAttack"], i["RewardExp"], i["RewardItem"], i["IdleAnimStr"],
             i["AttackAnimStr"], i["BattleParticleAttack"], i["BattleParticleHit"],
             i["Shadow"], i["MonsterNameId"], i["MonsterDescId"], i.get("ParLoss", 0.0)]
            for i in items]
    write_csv(os.path.join(EXCEL, "MonsterData.csv"), header, rows)


STAGE_EXTRA = ("ParLevelIn", "ParLevelOut", "AltarAtk", "AltarDef",
               "AltarPrice", "AltarPriceStep", "AltarReserve", "AltarReserveTower")


def stage_extra(floor, log_by_floor, reserves):
    """StageInfoData 의 덧붙인 열.

      ParLevelIn/Out  정답 경로가 그 층에 들어설 때·떠날 때의 레벨 (1~4층은 0 — 기준이 없다)
      Altar*          그 층 계단의 제단 (altar_at). 없는 층은 전부 0. 값은 최대 HP 의 %:
                      n 번째 구매 = AltarPrice + AltarPriceStep·n(n−1), 내고 나서
                      AltarReserve(탑의 법이면 AltarReserveTower)가 안 남으면 팔지 않는다
                      (altar_sells 와 같은 식). reserves = (일반, 탑의 법) 의 altar_reserves
    """
    r = log_by_floor.get(floor)
    gain = altar_at(floor)
    return dict(ParLevelIn=r["entry_level"] if r else 0, ParLevelOut=r["exit_level"] if r else 0,
                AltarAtk=float(gain[0]) if gain else 0.0, AltarDef=float(gain[1]) if gain else 0.0,
                AltarPrice=ALTAR_PRICE if gain else 0.0,
                AltarPriceStep=ALTAR_PRICE_STEP if gain else 0.0,
                AltarReserve=float(reserves[0][floor]) if gain else 0.0,
                AltarReserveTower=float(reserves[1][floor]) if gain else 0.0)


def emit_stage_info(log, reserves):
    """100층 스테이지 그래프.

    1~4층은 원본 그래프를 그대로 쓴다 (선형이 아니고 보스방이 옆으로 물려 있다).
    5층부터는 F -> F+1 한 방향 진행. log 는 [3/7] 정답 경로 — 기준 레벨(ParLevel)을 싣는다.
    reserves 는 (일반, 탑의 법) 표의 제단 문턱(altar_reserves).
    """
    log_by_floor = {r["floor"]: r for r in log}
    items, rows = [], []
    for it in ORIGINAL_STAGES:
        it = dict(it, **stage_extra(it["id"] + 1, log_by_floor, reserves))
        items.append(it)
        rows.append([it["id"], it["DungeonID"],
                     "Boss" if it["Type"] == 2 else "Common",
                     it["UpStage"], it["DownStage"], it["BossRoom"],
                     it["ATK"], it["DEF"], it["EXP"], it["BGM"],
                     it["DungeonNameScriptID"]] + [it[k] for k in STAGE_EXTRA])

    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        did, ch, idx = dungeon_id(floor)
        up = dungeon_id(floor + 1)[0] if floor < TOTAL_FLOORS else "-"
        down = dungeon_id(floor - 1)[0] if floor > 1 else "-"
        boss = is_boss_floor(floor)
        theme = CHAPTER_THEMES[ch]
        # Define.DungeonType: Common=0, Special=1, Boss=2
        # ATK/DEF/EXP 는 MonsterController.SetMonster() 에서 "곱셈" 계수로 쓰인다.
        #   Attack = StageInfo.ATK * MonsterData.Attack,  RewardExp = EXP/100 * RewardExp
        # 층별 수치는 이미 MonsterData 에 정확히 넣었으므로 여기서는 1배로 둔다.
        item = dict(id=floor - 1, DungeonID=did, Type=2 if boss else 0,
                    UpStage=up, DownStage=down, BossRoom="-",
                    ATK=1, DEF=1, EXP=100, BGM=theme[0],
                    DungeonNameScriptID=SCRIPT_STAGE_NAME_BASE + floor,
                    **stage_extra(floor, log_by_floor, reserves))
        items.append(item)
        rows.append([item["id"], did, "Boss" if boss else "Common", up, down, "-",
                     1, 1, 100, theme[0], item["DungeonNameScriptID"]]
                    + [item[k] for k in STAGE_EXTRA])

    header = ["ID", "Dungeon_ID", "Type", "Up_Stairs", "Down_Stairs", "Boss_Room",
              "ATK_보정 (n)", "DEF_보정 (n)", "EXP_보정 (%)", "BGM", "던전 이름_ID",
              "기준 레벨_입장", "기준 레벨_퇴장", "제단_공격", "제단_방어",
              "제단_값 (%)", "제단_값 증가 (%)", "제단_남길 HP (%)", "제단_남길 HP 탑의 법 (%)"]
    write_csv(os.path.join(EXCEL, "StageInfoData.csv"), header, rows)
    write_json(os.path.join(JSOND, "StageInfoData.json"), "stageInfos", items)
    return items


# 생성기가 통째로 소유하는 ScriptData 구간 (양끝 포함). 여기 있는 행은 매번 새로
# 쓰고 이번에 만들지 않은 행은 지운다 — 옛 생성기가 남긴 5101~5104 가 일본어·중국어
# 칸에 한국어를 담은 채 남아 있었다. 특성(MonsterClassData)·도감(EquipData 31)이
# 가리키는 행도 생성기가 쓴다.
GENERATED_SCRIPT_RANGES = [
    (SCRIPT_STAGE_NAME_BASE, SCRIPT_STAGE_NAME_BASE + TOTAL_FLOORS),
    (BOSS_NAME_BASE, BOSS_NAME_BASE + len(CHAPTER_THEMES) - 1),
    (BOSS_DESC_BASE, BOSS_DESC_BASE + len(CHAPTER_THEMES) - 1),
    (MOB_NAME_BASE, MOB_NAME_BASE + TOTAL_FLOORS * 8 + 7),
    (MOB_DESC_BASE, MOB_DESC_BASE + TOTAL_FLOORS * 8 + 7),
    (SIZED_RUNE_NAME_BASE, SIZED_RUNE_NAME_BASE + 49),
]
BOOK_EQUIP_ID = 31              # EquipData 의 몬스터 도감
RING_EQUIP_ID = 32              # EquipData 의 워프석 반지 — 새긴 글("렌에게. 해 지기 전에.")이 이야기의 일부다


def emit_scripts(monsters):
    """생성한 이름·설명을 네 언어로 쓴다. 손으로 쓴 행은 건드리지 않는다.

    예전에는 있는 ID 를 전부 건너뛰었다. 번역을 지키려던 것인데, 생성한 몬스터의
    이름까지 옛 문자열이 남았다 — 데이터에는 "이끼 낀 지하 묘소의 잿빛 파수꾼
    우두머리" 인데 화면에는 "이끼 낀 지하 묘소의 킹 슬라임" 이 떴다.
    그 다음에는 네 칸에 똑같이 한국어를 넣었다 — 영어 층 이름은 "5F" 뿐이었다.

    문구는 bestiary.py 가 낸다(스토리 도감이 원본, 없으면 내장 문구).
    특성 이름·설명은 MonsterClassData 의 i 번째 행(= Define.Trait i)이 가리키는 id 에 쓴다.
    """
    path = os.path.join(JSOND, "ScriptData.json")
    with open(path, "r", encoding="utf-8") as f:
        scripts = json.load(f)["scripts"]
    with open(os.path.join(JSOND, "MonsterClassData.json"), "r", encoding="utf-8") as f:
        classes = json.load(f)["monsterClasses"]

    made = {}                       # id -> {언어: 문구}

    def put(sid, text_of):
        assert sid not in made, f"ScriptData {sid} 를 두 번 쓴다"
        made[sid] = {lang: text_of(lang, BOOKS[lang]) for lang in bestiary.LANGS}

    # 1~4층 이름(5000~5003)은 원본 번역이 이미 있다.
    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        ch = dungeon_id(floor)[1]
        put(SCRIPT_STAGE_NAME_BASE + floor,
            lambda lang, t: bestiary.floor_name(t, lang, ch, floor))
    for m in monsters:
        ch, art = m["Chapter"], m["_art"]
        if m["_boss"]:
            put(m["MonsterNameId"], lambda lang, t: t["bosses"][ch][0])
            put(m["MonsterDescId"], lambda lang, t: t["bosses"][ch][1])
        else:
            put(m["MonsterNameId"], lambda lang, t: bestiary.mob_name(t, lang, ch, art))
            put(m["MonsterDescId"], lambda lang, t: bestiary.mob_desc(t, lang, ch, art))
    for c in classes:
        put(c["ClassName"], lambda lang, t: t["traits"][c["id"]][0])
        put(c["ClassDesc"], lambda lang, t: t["traits"][c["id"]][1])
    for eid, key in ((BOOK_EQUIP_ID, "book"), (RING_EQUIP_ID, "ring")):
        put(EQUIPS[eid]["NameId"], lambda lang, t, key=key: t[key][0])
        put(EQUIPS[eid]["DescId"], lambda lang, t, key=key: t[key][1])
    # 크기 룬 이름은 기본 룬 이름(hand_text 118~120) 뒤에 크기를 붙인다 — 원본은 한 곳이다.
    for i, (stat, amount) in enumerate(SIZED_RUNES):
        put(SIZED_RUNE_NAME_BASE + i, lambda lang, t, stat=stat, amount=amount:
            f"{UI_TEXT[118 + stat][bestiary.LANGS.index(lang)]} +{amount}")

    def owned(sid):
        return sid in made or any(lo <= sid <= hi for lo, hi in GENERATED_SCRIPT_RANGES)

    scripts = [row for row in scripts if not owned(row["id"])]
    for sid, texts in made.items():
        scripts.append(dict(id=sid, **{col: texts[lang] for lang, col
                                       in zip(bestiary.LANGS, bestiary.COLUMNS)}))

    # ui_text 행은 append_rows 가 말없이 덮는다 — 같은 id 를 둘이 쓰면 여기서 멈춘다.
    clash = sorted(set(made) & set(UI_TEXT))
    assert not clash, f"ScriptData {clash} 를 생성기와 ui_text 가 같이 쓴다 — 한쪽에서 지울 것"
    append_rows(scripts)
    emit_bootstrap(ROOT)
    scripts.sort(key=lambda s: s["id"])
    write_json(path, "scripts", scripts)
    header = ["id", "ScriptKr", "ScriptEn", "ScriptJp", "ScriptCn"]
    write_csv(os.path.join(EXCEL, "ScriptData.csv"), header, [[row.get(k, "") for k in header] for row in scripts])


# ------------------------------------------------------------------ 층 레이아웃

def emit_layouts(monsters, write=True):
    """100층 레이아웃 CSV 출력. 도달 불가 레이아웃은 시드를 바꿔 재생성한다."""
    by_floor = {}
    for m in monsters:
        by_floor.setdefault(m["_floor"], []).append(m)

    written, failures, bad_doors, sealed_off, unsafe_vaults, behind_boss = 0, [], [], [], [], []
    choices, econ = {}, {}
    for floor in range(HANDMADE_FLOORS + 1, TOTAL_FLOORS + 1):
        did, ch, _ = dungeon_id(floor)
        mobs = sorted((m for m in by_floor[floor] if not m["_boss"]),
                      key=lambda m: m["_order"])
        boss = next((m for m in by_floor[floor] if m["_boss"]), None)
        walls = CHAPTER_THEMES[ch][1]

        # 구역별 회복 아이템 (없는 구역은 None)
        # 보스층 구역3 은 [보스 앞, 보스 뒤] 순서다 — build_floor_layout 이 첫째만 보스 앞에 놓는다.
        region3 = [POTION_BY_HEAL[EXIT_POTION]]
        if boss:
            region3.insert(0, POTION_BY_HEAL[BOSS_FLOOR_POTIONS[0]])
        # 층 유형이 예산을 정한다. 구역 1·2 에 나눠 놓고, 남으면 구역 2 에 겹쳐 둔다.
        heals = floor_potions(floor)
        region1 = [POTION_BY_HEAL[heals[0]]] if len(heals) > 0 else []
        region2 = [POTION_BY_HEAL[h] for h in heals[1:]]
        pots = [[], region1, region2, region3]

        grid = None
        for attempt in range(50):
            g, origins, doors = build_floor_layout(
                [m["id"] for m in mobs], boss["id"] if boss else None, walls,
                seed=floor * 1000 + attempt, mobs_in_floor=MOBS_PER_FLOOR,
                equip_id=CHAPTER_EQUIP_REWARD.get(floor), potions=pots,
                rune=rune_of(floor), alcove=alcove_plan(floor), layout_kind=floor_type(floor),
                choice=choice_pair(floor))
            if g is None:
                continue
            ok, err = validate_layout(g, origins, doors)
            if ok:
                grid = g
                break
        if grid is None:
            failures.append(f"Dungeon_{did}")
            continue

        # 문이 자리·그림·색까지 맞는지. 씨앗을 다시 뽑지 않고 알리기만 한다 —
        # 여기서 걸린다는 것은 규칙이 아니라 <b>번역표</b>가 어긋났다는 뜻이라
        # 다른 씨앗으로 다시 뽑아도 똑같이 어긋난다.
        for cell, x, y, why in check_doors(grid):
            bad_doors.append(f"Dungeon_{did} 셀 {cell} (행{y}, 열{x}) — {why}")

        # 포탈은 지나갈 수 없다. 그것이 어느 구역의 유일한 입구에 앉으면 그 뒤가
        # 통째로 막힌다 — 98층에서 골방이 위 계단에 막혀 있었다.
        for (cx, cy), what in check_sealed_by_portal(grid):
            sealed_off.append(f"Dungeon_{did} ({cx}, {cy}) {what} — 포탈에 막혀 못 간다")

        # 금고 문이 앞 구역에 앉으면 <b>이 층의 열쇠로 열린다</b> — 그러면
        # 큰길 문을 열 열쇠가 사라져 되돌릴 수 없이 갇힌다. 배치에서 이미
        # 막아 두지만, 그건 의도다. 완성된 격자에서 다시 잰다.
        for (cx, cy), what in check_vault_safe(grid, doors):
            unsafe_vaults.append(
                f"Dungeon_{did} ({cx}, {cy}) 금고 {what} — 세 문을 열기 전에 닿는다")

        # 완주 계산은 층의 몹과 큰 물약을 보스 <b>앞에서</b> 쓴다(simulate_run).
        for cell, what in check_behind_boss(grid, (POTION_BY_HEAL[BOSS_FLOOR_POTIONS[0]],)):
            behind_boss.append(f"Dungeon_{did} {what} " +
                               (f"{cell} — 보스를 잡아야 닿는다" if cell else "— 층에 없다"))

        econ[floor] = key_economy(grid, doors)

        # 강제/선택은 배치 <b>의도</b>가 아니라 완성된 격자에서 다시 잰다.
        # 우회로가 하나라도 생기면 관문이 조용히 곁길이 되기 때문이다 —
        # 곁길 통로를 내던 시절에 챕터 보스 다섯이 전부 그렇게 곁길이었다.
        choices[floor] = floor_choices(grid, doors)

        if write:
            path = os.path.join(STREAM, f"Dungeon_{did}.csv")
            with open(path, "w", encoding="utf-8", newline="") as f:
                for row in grid:
                    f.write(",".join(row) + "\n")
        written += 1
    return written, failures, bad_doors, choices, sealed_off, unsafe_vaults, behind_boss, econ


def report_choices(choices):
    """강제/선택 구조를 층수로 찍는다. 규칙이 깨진 층 목록을 돌려준다.

    말이 아니라 수로 확인해야 한다 — 예전에 "보스는 계단 앞에 선다" 고 적어
    두고서 다섯 챕터 보스가 전부 지나칠 수 있는 상대였다.
    """
    hist = collections.Counter(c["forced_mobs"] for c in choices.values())
    n = len(choices)
    tot = collections.Counter()
    for c in choices.values():
        tot.update(c)
    boss_floors = tot["boss"]

    print("      [강제] 관문 몬스터 %d마리 — 층당 %s" %
          (tot["forced_mobs"],
           " / ".join(f"{k}마리 {v}층" for k, v in sorted(hist.items()))))
    print("      [강제] 열쇠 %d개(큰길 %d + 여분 %d) · 반드시 밟는 룬 %d/%d" %
          (tot["keys"], n * 3, tot["keys"] - n * 3,
           tot["forced_runes"], tot["runes"]))
    print("      [선택] 금고 %d개 · 그 안에 잠긴 룬 %d개 (완주 계산에는 없다)" %
          (tot["vaults"], tot["vault_items"]))
    print("      [강제] 보스 %d층 중 관문인 보스 %d" % (boss_floors, tot["boss_forced"]))
    print("      [선택] 곁길 몬스터 %d마리(층당 %.2f) · 지나칠 수 있는 아이템 %d개" %
          (tot["optional_mobs"], tot["optional_mobs"] / n, tot["optional_items"]))
    print("      [선택] 막다른 골방 %d개 (층당 %.2f)" %
          (tot["dead_ends"], tot["dead_ends"] / n))
    print("      [선택] 둘 중 하나 보상 %d쌍 (보물 층마다 한 쌍 + 40·60·80층 보스 뒤, 완주 계산에는 없다)" %
          (tot["choice_rewards"] // 2))

    broken = []
    for floor, c in sorted(choices.items()):
        want = 2 if choice_pair(floor) else 0
        if c["choice_rewards"] != want:
            broken.append(f"{floor}층 둘 중 하나 보상 칸 {c['choice_rewards']}개 (!= {want})")
        if c["forced_mobs"] < FORCED_PER_FLOOR:
            broken.append(f"{floor}층 관문 {c['forced_mobs']}마리 (< {FORCED_PER_FLOOR})")
        if c["boss"] and not c["boss_forced"]:
            broken.append(f"{floor}층 보스를 지나칠 수 있다")
        if c["forced_runes"] < c["runes"]:
            broken.append(f"{floor}층 룬을 지나칠 수 있다")
    return broken


COLOR_NAME = ("초록", "노랑", "빨강")


def report_keys(econ):
    """열쇠 경제를 <b>수로</b> 찍는다. 규칙이 깨진 항목 목록을 돌려준다.

    "모자라게 했다" 는 말은 세어 보기 전까지 아무 뜻이 없다. 원형이 그랬듯
    <b>색마다</b> 결핍의 기울기가 달라야 하고, 그 결핍이 실제로 "못 여는 금고"
    로 나타나야 한다. 아래는 층을 순서대로 오르며 그것을 세어 본 것이다.

    가정 하나: 여분 열쇠는 골방 파수꾼 뒤에 있으므로 <b>그 곁길 전투를
    치른다</b>고 본다. 안 치르면 금고는 하나도 열 수 없다 — 그 수도 같이 찍는다.
    """
    floors = sorted(econ)
    main = [0, 0, 0]
    spare_total = [0, 0, 0]
    vault_total = [0, 0, 0]
    guarded = 0
    for f in floors:
        keys, m, v, g = econ[f]
        guarded += g
        for c in range(3):
            main[c] += m[c]
            spare_total[c] += keys[c] - 1          # 큰길 몫 하나를 뺀 나머지
            vault_total[c] += v[c]

    # 층을 순서대로 오른다. "보이면 연다" 와 "곁길을 전부 지나친다" 둘.
    spare = [0, 0, 0]
    opened = [0, 0, 0]
    locked = [0, 0, 0]
    surplus_floors = shortage_floors = decision_floors = 0
    for i, f in enumerate(floors):
        keys, _, v, _g = econ[f]
        for c in range(3):
            spare[c] += keys[c] - 1
        shortage = False
        for c in range(3):
            for _ in range(v[c]):
                # 뒤에 같은 색 금고가 또 있는가 = 아껴 둘 데가 있는가
                ahead = sum(econ[g][2][c] for g in floors[i + 1:])
                if spare[c] > 0:
                    if ahead > 0:
                        decision_floors += 1
                    spare[c] -= 1
                    opened[c] += 1
                else:
                    locked[c] += 1
                    shortage = True
        if shortage:
            shortage_floors += 1
        if sum(spare) > 0:
            surplus_floors += 1

    print("      색     큰길 문:열쇠   금고 문   여분 열쇠   열 수 있는 비율")
    for c in range(3):
        v, k = vault_total[c], spare_total[c]
        print("      %s   %3d:%-3d       %3d       %3d         %3d%%" %
              (COLOR_NAME[c], main[c], main[c], v, k,
               round(100 * min(k, v) / v) if v else 100))
    print("      전체   %3d:%-3d       %3d       %3d         %3d%%" %
          (sum(main), sum(main), sum(vault_total), sum(spare_total),
           round(100 * sum(spare_total) / sum(vault_total))))
    print("      보이는 대로 열면 금고 %d개를 열고 %d개를 영영 못 연다"
          % (sum(opened), sum(locked)))
    print("      열쇠가 남은 채 끝나는 층 %d / 열쇠가 모자란 층 %d (전체 %d층)"
          % (surplus_floors, shortage_floors, len(floors)))
    print("      '여기 쓸까 아껴 둘까' 가 실제로 갈리는 층 %d개"
          " — 열 수 있는데 같은 색 금고가 뒤에 또 있다" % decision_floors)
    print("       다만 아껴 두는 값은 룬의 크기가 아니라 종류다 —"
          " 급한 능력치가 아니면 먼저 여는 쪽이 대체로 낫다")
    print("      여분 열쇠 %d개 중 파수꾼 뒤에 있는 것 %d개"
          " — 곁길을 전부 지나치면 금고 %d개가 전부 잠긴다"
          % (sum(spare_total), guarded, sum(vault_total)))

    broken = []
    for c in range(3):
        if spare_total[c] >= vault_total[c]:
            broken.append(f"{COLOR_NAME[c]} 여분 열쇠 {spare_total[c]} >= 금고 "
                          f"{vault_total[c]} — 모자라지 않으니 고를 것이 없다")
    for c in range(1, 3):
        if vault_total[c] and vault_total[c - 1]:
            prev = spare_total[c - 1] / vault_total[c - 1]
            cur = spare_total[c] / vault_total[c]
            if cur >= prev:
                broken.append(f"{COLOR_NAME[c]} 가 {COLOR_NAME[c - 1]} 보다 안 귀하다 "
                              f"({cur:.0%} >= {prev:.0%})")
    if guarded < sum(spare_total):
        broken.append(f"여분 열쇠 {sum(spare_total)}개 중 {guarded}개만 파수꾼 뒤에 있다"
                      " — 나머지는 그냥 주워지므로 곁길 판단이 되지 않는다")
    for f in floors:
        keys, m, _v, _g = econ[f]
        if m != [1, 1, 1]:
            broken.append(f"{f}층 큰길 문 {m} — 층마다 색깔별로 하나여야 한다")
        if min(keys) < 1:
            broken.append(f"{f}층 열쇠 {keys} — 큰길 몫이 빈다")
    return broken


# ------------------------------------------------------------------ 진입점

def build_all(dry_run=False):
    ptable = load_player_table(os.path.join(EXCEL, "PlayerData.csv"))
    ptable = extend_player_table(ptable, MAX_LEVEL_TABLE)

    print(f"[1/7] 손수 만든 1~{HANDMADE_FLOORS}층 완주 시뮬레이션")
    start_state, err = simulate_handmade(ptable)
    if start_state is None:
        print(f"  [실패] {err}")
        return False
    start_level, start_exp, start_hp = start_state
    print(f"      {HANDMADE_FLOORS}층 종료 시 Lv{start_level}, HP {start_hp:.0f} "
          f"-> {HANDMADE_FLOORS + 1}층 목표 레벨 {target_level(HANDMADE_FLOORS + 1, start_level)}")

    print(f"[2/7] {HANDMADE_FLOORS + 1}~{TOTAL_FLOORS}층 몬스터 스탯 역산 중...")
    monsters = build_monsters(ptable, start_level)
    tower = build_monsters(ptable, start_level, mob_loss=TOWER_MOB_HP_LOSS)
    print(f"      몬스터 {len(monsters)}종 생성 (탑의 법 표도 같은 수 — 챕터별 몹 손실 "
          + "/".join(f"{x:g}" for x in TOWER_MOB_HP_LOSS) + ")")
    # 다른 종이 같은 이름을 달면 도감에 같은 이름이 스탯만 다르게 뜬다. 쓰기 전에 막는다.
    clashes = bestiary.name_clashes(BOOKS)
    if clashes:
        for line in clashes[:10]:
            print(f"  [실패] {line}")
        print(f"      몬스터 이름 겹침 {len(clashes)}건 — Tools/story 의 bestiary 를 고칠 것")
        return False

    print(f"[3/7] {HANDMADE_FLOORS + 1}~{TOTAL_FLOORS}층 완주 시뮬레이션")
    ok, log, err = simulate_run(ptable, monsters, start_state)
    if not ok:
        print(f"  [실패] {err}")
        return False
    worst = min(log, key=lambda r: r["hp_pct"])
    final = log[-1]["exit_level"]
    print(f"      완주 성공. 최저 HP 구간: {worst['floor']}층 {worst['hp_pct']:.1f}%")
    print(f"      최종 레벨: {final} (레벨 표 {MAX_LEVEL_TABLE}, 여유 {MAX_LEVEL_TABLE - final})")
    if MAX_LEVEL_TABLE < final + LEVEL_HEADROOM:
        print(f"  [실패] 레벨 표가 모자란다 — MAX_LEVEL_TABLE 을 {final + LEVEL_HEADROOM} 이상으로")
        return False
    report_fight_costs(log)

    print("[4/7] 층 레이아웃 생성 + 도달 가능성 검사")
    (written, failures, bad_doors, choices, sealed_off,
     unsafe_vaults, behind_boss, econ) = emit_layouts(monsters, write=False)
    print(f"      {written}/{TOTAL_FLOORS - HANDMADE_FLOORS} 층 생성 "
          f"(1~{HANDMADE_FLOORS}층은 원본 유지)")
    if failures:
        print(f"  [실패] 레이아웃 생성 불가: {', '.join(failures)}")
        return False
    if bad_doors:
        for line in bad_doors[:10]:
            print(f"  [실패] {line}")
        print(f"      문 규칙 위반 {len(bad_doors)}건")
        return False
    print("      문 자리/그림/색 위반 0건")
    if sealed_off:
        for line in sealed_off[:10]:
            print(f"  [실패] {line}")
        print(f"      포탈에 막혀 못 가는 것 {len(sealed_off)}건")
        return False
    print("      포탈에 막힌 몬스터·아이템 0건")
    if unsafe_vaults:
        for line in unsafe_vaults[:10]:
            print(f"  [실패] {line}")
        print(f"      열쇠를 잘못 써서 갇힐 수 있는 금고 {len(unsafe_vaults)}건")
        return False
    print("      앞 구역에 앉은 금고 0건 (열쇠를 잘못 써서 갇히는 수가 없다)")
    if behind_boss:
        for line in behind_boss[:10]:
            print(f"  [실패] {line}")
        print(f"      보스 뒤에 앉은 몬스터·큰 물약 {len(behind_boss)}건")
        return False
    print("      보스 뒤에 앉은 몬스터·큰 물약 0건 (완주 계산은 둘 다 보스 앞에서 쓴다)")

    print("[5/7] 강제와 선택 세기")
    broken = report_choices(choices)
    if broken:
        for line in broken[:10]:
            print(f"  [실패] {line}")
        print(f"      구조 위반 {len(broken)}건")
        return False

    print("[6/7] 열쇠는 탑 전체에서 모자란가")
    broken = report_keys(econ)
    if broken:
        for line in broken[:10]:
            print(f"  [실패] {line}")
        print(f"      열쇠 경제 위반 {len(broken)}건")
        return False

    print("[7/7] 나쁜 선택 재현 — 잘못 고르면 죽는가")
    reserves = altar_reserves(ptable, monsters, log)
    tower_reserves = altar_reserves(ptable, tower, log)
    if not check_bad_routes(ptable, monsters, start_state, reserves):
        return False
    if not check_tower(ptable, tower, start_state):
        return False
    if not check_altar(ptable, [("일반", monsters, reserves), ("탑의 법", tower, tower_reserves)],
                       start_state, log):
        return False
    report_breakpoints(ptable, monsters, start_level)
    bud, srv, _, _ = potion_ledger(log)
    report_rune_tiers(ptable, monsters, start_level, drink=srv / bud)

    if dry_run:
        print("      dry-run 이므로 파일은 쓰지 않음")
        return True

    print("      데이터 파일 출력")
    emit_player_data(ptable)
    emit_consumable_items()
    emit_monster_data(monsters, tower)
    emit_stage_info(log, (reserves, tower_reserves))
    emit_scripts(monsters)
    # [4/7] 은 검사만 한다 — 검사가 다 통과한 뒤에야 쓴다. 씨앗이 층수로
    # 정해지므로 다시 뽑아도 같은 격자가 나온다.
    emit_layouts(monsters, write=True)

    # 레이아웃 CSV 를 쓴 "다음에" 돌려야 한다. MapBuilder 가 읽는 것은 이 JSON 이다.
    print("      MapData.json 생성 (CSV -> 런타임 오브젝트 배치)")
    maps, counts = emit_mapdata()
    print(f"      맵 {len(maps)}장, {counts}")
    print("      완료")
    return True


if __name__ == "__main__":
    import sys
    ok = build_all(dry_run="--write" not in sys.argv)
    sys.exit(0 if ok else 1)
