"""생성된 100층 콘텐츠 검증 (Unity 없이 실행 가능).

Assets/@Scripts/Editor/ContentValidator.cs 와 같은 검사를 파이썬으로 수행한다.
Unity 에디터가 프로젝트를 점유 중이어도 돌릴 수 있다.

    python validate_content.py
"""

import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
JSOND = os.path.join(ROOT, "Assets", "@Resources", "Data", "JsonData")
ASSETS = os.path.join(ROOT, "Assets")

EXPECTED_FLOORS = 100
NUM_OF_KEYS = 3
KEY_COLOR = ("초록", "노랑", "빨강")

# 손수 만든 도입부. 프리팹을 그대로 쓰므로 CSV 는 세이브 인덱스 계산용일 뿐이다.
# 구조(계단/스폰/벽 타일)를 생성 규칙으로 재단하면 안 된다 — 예컨대 00_002 는
# 마검 이벤트용 막다른 방이라 위층 계단이 아예 없는 게 정상이다.
HAND_AUTHORED = {"00_000", "00_001", "00_002", "00_003"}
# 챕터 보스 5 + 손수 만든 킹슬라임(00_003) 1
EXPECTED_BOSS_FLOORS = 6

# Define.ObjectType
VOID, FLOOR, WALL, CITEM, EITEM, DOOR, PORTAL, MONSTER, BOSS, SPAWN, LEVER, PILLAR = range(12)
DUNGEON_BOSS = 2  # Define.DungeonType.Boss

errors, warnings = [], []


def load(name, key):
    with open(os.path.join(JSOND, name + ".json"), encoding="utf-8") as f:
        return json.load(f)[key]


def prefab_names():
    names = set()
    for dirpath, _, files in os.walk(ASSETS):
        for fn in files:
            if fn.endswith(".prefab"):
                names.add(fn[:-7])
    return names


def main():
    stages = {s["id"]: s for s in load("StageInfoData", "stageInfos")}
    monsters = {m["id"]: m for m in load("MonsterData", "creatures")}
    players = {p["id"]: p for p in load("PlayerData", "creatures")}
    items = {i["id"]: i for i in load("ConsumableItemData", "consumableItems")}
    maps = {m["Key"]: m for m in load("MapData", "maps")}
    prefabs = prefab_names()
    tower = dict(doors=[0] * NUM_OF_KEYS, keys=[0] * NUM_OF_KEYS,
                 vaults=[0] * NUM_OF_KEYS, spares=[0] * NUM_OF_KEYS)

    # ---- 스테이지 그래프
    if len(stages) != EXPECTED_FLOORS:
        errors.append(f"스테이지 수 {len(stages)} (기대 {EXPECTED_FLOORS})")

    boss_floors = 0
    for sid, s in sorted(stages.items()):
        if s["ATK"] <= 0 or s["DEF"] <= 0:
            errors.append(f"{s['DungeonID']}: ATK/DEF 계수가 0 -> 몬스터 스탯이 0 이 된다")
        if s["EXP"] <= 0:
            errors.append(f"{s['DungeonID']}: EXP 계수 0")
        if s["Type"] == DUNGEON_BOSS:
            boss_floors += 1
        if sid < EXPECTED_FLOORS - 1 and s["DungeonID"] not in HAND_AUTHORED:
            nxt = stages.get(sid + 1)
            if s["UpStage"] in ("-", "", None):
                errors.append(f"{s['DungeonID']}: 위층 연결 없음")
            elif nxt is None or nxt["DungeonID"] != s["UpStage"]:
                errors.append(f"{s['DungeonID']}: 위층 {s['UpStage']} 불일치")
    if boss_floors != EXPECTED_BOSS_FLOORS:
        errors.append(f"보스 층 {boss_floors}개 (기대 {EXPECTED_BOSS_FLOORS})")

    # 도입부에서 생성 구간으로 넘어가는 이음매는 반드시 확인한다.
    if stages[3]["UpStage"] != "00_004":
        errors.append("00_003(킹슬라임)에서 5층으로 올라가는 연결이 끊겼다")
    if stages[4]["DownStage"] != "00_003":
        errors.append("5층에서 4층으로 내려가는 연결이 끊겼다")

    # ---- 층 데이터
    for sid, s in sorted(stages.items()):
        did = s["DungeonID"]
        m = maps.get(sid)
        if m is None or not m.get("Objects"):
            errors.append(f"{did}: MapData 없음")
            continue

        # 손수 만든 층은 프리팹이 실물이다. CSV 구조로 판정하지 않는다.
        if did in HAND_AUTHORED:
            continue

        n = dict(floor=0, spawn=0, up=0, down=0, mob=0, potion=0)
        keys, doors = [0] * NUM_OF_KEYS, [0] * NUM_OF_KEYS
        for o in m["Objects"]:
            t, oid = o["ObjectType"], o["Id"]
            if t == FLOOR:
                n["floor"] += 1
            elif t == SPAWN:
                n["spawn"] += 1
            elif t == DOOR:
                doors[(oid - 3) % NUM_OF_KEYS] += 1
            elif t == PORTAL:
                if oid == 14:
                    n["up"] += 1
                elif oid == 15:
                    n["down"] += 1
            elif t in (MONSTER, BOSS):
                n["mob"] += 1
                if oid not in monsters:
                    errors.append(f"{did}: 없는 몬스터 ID {oid}")
            elif t == CITEM:
                if oid not in items:
                    errors.append(f"{did}: 없는 아이템 ID {oid}")
                elif oid < NUM_OF_KEYS:
                    keys[oid] += 1
                else:
                    n["potion"] += 1
            elif t == WALL:
                key = f"Tilemap_C00_W{oid:02d}"
                if key not in prefabs:
                    errors.append(f"{did}: 벽 프리팹 없음 '{key}'")

        if n["floor"] == 0:
            errors.append(f"{did}: 바닥 타일 없음")
        if n["spawn"] == 0:
            errors.append(f"{did}: 스폰 포인트 없음")
        if n["mob"] == 0:
            errors.append(f"{did}: 몬스터 없음")
        if n["up"] == 0 and sid < EXPECTED_FLOORS - 1:
            errors.append(f"{did}: 위층 계단(14) 없음")
        if n["down"] == 0 and sid > 0:
            errors.append(f"{did}: 아래층 계단(15) 없음 -> 아랫층에서 못 올라온다")
        # 열쇠는 <b>탑 전체에서 모자라다</b>. 예전에는 층마다 문:열쇠가 1:1 이라
        # "문 > 열쇠면 진행 불가" 로 충분했는데, 지금은 큰길 문 셋(색깔별 하나)
        # 말고 <b>금고 문</b>이 더 붙는 층이 있다. 금고는 앞 층에서 들고 온
        # 여분으로만 열리고 안 열어도 진행은 된다. 그래서 층 단위로 볼 것은
        # "큰길 몫이 있는가" 와 "금고가 층당 하나를 넘지 않는가" 다.
        for c in range(NUM_OF_KEYS):
            if doors[c] and keys[c] == 0:
                errors.append(f"{did}: {KEY_COLOR[c]} 문 {doors[c]}개인데 "
                              f"그 색 열쇠가 없다 -> 진행 불가")
            elif doors[c] > keys[c] + 1:
                errors.append(f"{did}: {KEY_COLOR[c]} 문 {doors[c]}개 / 열쇠 "
                              f"{keys[c]}개 -> 층당 금고가 둘 이상이다")
        tower["doors"] = [a + b for a, b in zip(tower["doors"], doors)]
        tower["keys"] = [a + b for a, b in zip(tower["keys"], keys)]
        # 큰길 몫(색깔별 하나씩)을 뺀 나머지 = 실제로 고를 수 있는 부분.
        # 전체 비율은 큰길 1:1 에 희석돼 늘 90%대로 보인다 — 볼 것은 이쪽이다.
        tower["vaults"] = [a + max(0, b - 1) for a, b in zip(tower["vaults"], doors)]
        tower["spares"] = [a + max(0, b - 1) for a, b in zip(tower["spares"], keys)]

    # ---- 공통 프리팹
    # 문은 색x방향 여섯 종이 다 있어야 한다 — 하나라도 없으면 BuildDoor 가
    # 폴백을 쓰고, 그 문만 그림이 어긋난다.
    for key in ("Tilemap_1", "Tilemap_14", "Tilemap_15",
                "Tilemap_3", "Tilemap_4", "Tilemap_5",
                "Tilemap_6", "Tilemap_7", "Tilemap_8",
                "Monster", "BossMonster", "ConsumableItem"):
        if key not in prefabs:
            errors.append(f"공통 프리팹 없음 '{key}'")

    # ---- 레벨 테이블 (100층 완주 시 레벨 101, 세터가 Level+1 을 읽는다)
    top = max(players)
    if top < 103:
        errors.append(f"PlayerData 최대 레벨 {top} -> 완주 시 예외")

    # ---- 몬스터 애니메이션 자산 실재 확인
    anim_dir = os.path.join(ROOT, "Assets", "@Resources", "Animations")
    anims = set()
    for dirpath, _, files in os.walk(anim_dir):
        for fn in files:
            if fn.endswith(".anim"):
                anims.add(fn[:-5])
    for mid, mon in monsters.items():
        if mid < 100:
            continue  # 기존 몬스터는 손대지 않았다
        for field in ("IdleAnimStr", "AttackAnimStr"):
            if mon[field] and mon[field] not in anims:
                warnings.append(f"몬스터 {mid}: 애니메이션 '{mon[field]}' 없음")

    # 탑 전체의 결핍. 이것이 0 이면 "어느 문을 열까" 가 질문이 아니게 된다.
    # <b>오류가 아니라 경고다.</b> 여기는 산출물이 깨졌는지 보는 곳이고,
    # 결핍은 설계 값이라 generate_content 의 [6/7] 이 하드 게이트로 막는다.
    # 여기서 오류로 올리면 아직 --write 를 안 돌린 트리가 통째로 실패한다.
    if sum(tower["keys"]) >= sum(tower["doors"]):
        warnings.append(f"열쇠 {sum(tower['keys'])} >= 문 {sum(tower['doors'])} "
                        "-> 탑 전체에서 모자라지 않다 (generate_content --write 필요)")

    print("===== TheSword 100층 콘텐츠 검증 =====")
    print("  문:열쇠 " + " / ".join(
        f"{KEY_COLOR[c]} {tower['doors'][c]}:{tower['keys'][c]}"
        for c in range(NUM_OF_KEYS) if tower["doors"][c]))
    if sum(tower["vaults"]):
        print("  그중 고를 수 있는 몫(금고:여분 열쇠) " + " / ".join(
            f"{KEY_COLOR[c]} {tower['vaults'][c]}:{tower['spares'][c]}"
            f"({round(100 * tower['spares'][c] / tower['vaults'][c])}%)"
            for c in range(NUM_OF_KEYS) if tower["vaults"][c]))
    for w in warnings[:10]:
        print("  [경고]", w)
    if len(warnings) > 10:
        print(f"  ... 경고 외 {len(warnings) - 10}건")
    if errors:
        for e in errors[:40]:
            print("  [오류]", e)
        if len(errors) > 40:
            print(f"  ... 오류 외 {len(errors) - 40}건")
        print(f"  실패: 오류 {len(errors)}건")
        return 1
    print(f"  통과 — 스테이지 {len(stages)}층, 맵 {len(maps)}개, 몬스터 {len(monsters)}종 "
          f"(경고 {len(warnings)}건)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
