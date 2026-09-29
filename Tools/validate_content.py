"""생성된 100층 콘텐츠 검증 (Unity 없이 실행 가능).

Assets/@Scripts/Editor/ContentValidator.cs 와 같은 검사를 파이썬으로 수행한다.
Unity 에디터가 프로젝트를 점유 중이어도 돌릴 수 있다.

    python validate_content.py
"""

import collections
import json
import os
import sys

import bestiary
import generate_content as G

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

# MonsterClassData 의 i 번째 행 = Define.Trait i. 이름은 30000+i, 설명은 30010+i 를
# 가리킨다 — 예전에는 한 칸 밀려서 "없음" 이 "비스트", 갑옷이 설명 문구를 이름으로 썼다.
TRAIT_NAME_BASE, TRAIT_DESC_BASE = 30000, 30010
# 몬스터 그림은 두 애니메이터에 상태로 있어야 한다. 없는 상태를 재생하면 경고만
# 찍히고 그림이 안 나온다.
CONTROLLERS = ("Animations/MonsterAnimations/MonsterAnim.controller",
               "Animations/UIMonsterAnimations/UIMonsterAnimController.controller")

errors, warnings = [], []


def load(name, key):
    with open(os.path.join(JSOND, name + ".json"), encoding="utf-8") as f:
        return json.load(f)[key]


def controller_states(rel):
    """애니메이터 컨트롤러의 상태 이름 (AnimatorState 는 클래스 id 1102)."""
    with open(os.path.join(ROOT, "Assets", "@Resources", rel), encoding="utf-8") as f:
        blocks = f.read().split("--- !u!")
    return {line.split(":", 1)[1].strip() for b in blocks if b.startswith("1102 ")
            for line in b.splitlines() if line.strip().startswith("m_Name:")}


def check_scripts(stages, monsters, classes, equips, events, scripts):
    """화면에 뜨는 문자열이 있는가 — 몬스터·층 이름, 특성, 장비, 대사.

    생성한 행(층 이름·생성 몬스터·특성·도감)은 네 언어가 다 차야 하고, 영어·일본어·
    중국어 칸에 한글이 있으면 <b>오류</b>다. 스토리 도감이 없어도 bestiary.py 의 내장
    문구가 네 언어를 다 채우므로, 한글이 남았다면 번역 파일에 한국어가 들어간 것이다.
    (도감이 없다는 것 자체는 경고로만 알린다.)
    손으로 쓴 행의 일본어·중국어 빈칸은 <b>경고</b>다 — 번역이 들어오는 중이라
    오류로 두면 트리 전체가 실패한다. 한국어·영어가 비면 오류다.
    """
    refs, generated = {}, set()
    for mid, m in monsters.items():
        for field in ("MonsterNameId", "MonsterDescId"):
            refs[m[field]] = f"몬스터 {mid} {field}"
            if mid >= 100:
                generated.add(m[field])
    for s in stages.values():
        refs[s["DungeonNameScriptID"]] = f"{s['DungeonID']} 층 이름"
        if s["DungeonID"] not in HAND_AUTHORED:
            generated.add(s["DungeonNameScriptID"])
    for c in classes:
        for field in ("ClassName", "ClassDesc"):
            refs[c[field]] = f"특성 {c['id']} {field}"
            generated.add(c[field])
    for e in equips:
        for field, what in (("NameId", "이름"), ("DescId", "설명")):
            refs[e[field]] = f"장비 {e['id']} {what}"
            # 아래에서 0 은 "문자열 없음" 으로 건너뛰는데, 그래도 되는 장비는 0 번("-", 안 낀
            # 칸의 자리표)뿐이다. 처음 쥐는 블레이드(9)가 0 을 가리킨 채 통과하고 있었다.
            if e[field] <= 0 and e["id"] != 0:
                errors.append(f"장비 {e['id']}({e['Name']}) {what}: ScriptData id 가 {e[field]}"
                              " — 인벤토리에 뜨면 빈칸이다")
        if e["id"] == G.BOOK_EQUIP_ID:
            generated.update((e["NameId"], e["DescId"]))
    for e in events:
        refs[e["ScriptID"]] = f"대사 {e['id']}"

    pending = []
    for sid, where in sorted(refs.items()):
        if sid <= 0:
            continue                        # 0 = 문자열 없음 (0 번 장비, 빈 몬스터 행, 이모티콘 대사)
        row = scripts.get(sid)
        if row is None:
            errors.append(f"{where}: ScriptData {sid} 없음")
            continue
        empty = [lang for lang, col in zip(bestiary.LANGS, bestiary.COLUMNS)
                 if not (row.get(col) or "").strip()]
        if sid in generated or "kr" in empty or "en" in empty:
            if empty:
                errors.append(f"{where}: ScriptData {sid} 의 {'/'.join(empty)} 가 비었다")
        elif empty:
            pending.append(sid)
        if sid in generated:
            korean = [lang for lang, col in zip(bestiary.LANGS[1:], bestiary.COLUMNS[1:])
                      if bestiary.has_hangul(row.get(col))]
            if korean:
                errors.append(f"{where}: ScriptData {sid} 의 {'/'.join(korean)} 에 한글이 있다")
    # 다른 몬스터가 같은 이름을 달면 도감에 같은 이름이 스탯만 다르게 뜬다.
    # 같은 챕터·같은 그림은 같은 종이라 층마다 같은 이름이 맞다.
    kinds = collections.defaultdict(set)
    for mid, m in monsters.items():
        if mid >= 100:
            kind = f"챕터 {m['Chapter']} {m['IdleAnimStr']}" if mid >= G.MOB_ID_BASE else f"보스 {mid}"
            row = scripts.get(m["MonsterNameId"]) or {}
            for col in bestiary.COLUMNS:
                if (row.get(col) or "").strip():
                    kinds[col, row[col].strip()].add(kind)
    for (col, name), ks in sorted(kinds.items()):
        if len(ks) > 1:
            errors.append(f"{col} '{name}' 을 다른 몬스터가 같이 쓴다: {', '.join(sorted(ks)[:3])}")
    if pending:
        warnings.append(f"손으로 쓴 문자열 {len(pending)}건의 일본어·중국어가 비었다 "
                        f"(예: {', '.join(map(str, pending[:6]))}) — 번역 대기")
    _, missing = bestiary.tables()
    if missing:
        warnings.append("스토리 도감이 없어 생성 문구는 내장 문구다: "
                        + ", ".join(f"story_{lang}.json" for lang in missing))


def check_traits(classes, monsters):
    """특성 id = MonsterClassData 행 번호 = Define.Trait. 아이콘·이름이 그 줄을 따라간다."""
    ids = sorted(c["id"] for c in classes)
    if ids != list(range(len(ids))):
        errors.append(f"MonsterClassData id 가 0 부터 이어지지 않는다: {ids}")
    for c in classes:
        i = c["id"]
        want = (TRAIT_NAME_BASE + i, TRAIT_DESC_BASE + i, f"BattleUI_AbilityIcon{i}")
        have = (c["ClassName"], c["ClassDesc"], c["AbilityImage"])
        if have != want:
            errors.append(f"MonsterClassData {i}: {have} (기대 {want})")
    for mid, m in monsters.items():
        if m["Ability"] not in ids:
            errors.append(f"몬스터 {mid}: MonsterClassData 에 없는 특성 {m['Ability']}")


def check_grants(stages, maps, monsters, equips):
    """같은 장비를 두 번 주지 않는가 — 바닥에 놓인 것 + 몬스터가 떨구는 것.

    손수 만든 층의 몬스터 id 는 CSV 와 프리팹이 같다. 킹 슬라임은 CSV 에 B_00
    자리표로만 있고 분열 셋은 연출이 띄우므로 따로 센다.
    """
    source = collections.defaultdict(list)
    for sid, m in maps.items():
        did = stages[sid]["DungeonID"] if sid in stages else str(sid)
        for o in m["Objects"]:
            t, oid = o["ObjectType"], o["Id"]
            if t == EITEM:
                source[oid].append(f"{did} 바닥")
            elif t == MONSTER or (t == BOSS and did not in HAND_AUTHORED):
                if oid in monsters and monsters[oid]["RewardItem"] > 0:
                    source[monsters[oid]["RewardItem"]].append(f"{did} 몬스터 {oid}")
    for mid in (G.HANDMADE_BOSS_ID,) + tuple(G.SPLIT_SLIME_IDS):
        if monsters[mid]["RewardItem"] > 0:
            source[monsters[mid]["RewardItem"]].append(f"00_003 몬스터 {mid}")
    names = {e["id"]: e["Name"] for e in equips}
    for eid, where in sorted(source.items()):
        if len(where) > 1:
            errors.append(f"장비 {eid}({names.get(eid, '?')}) 를 {len(where)}번 준다: "
                          + ", ".join(where[:4]))
        if eid not in names:
            errors.append(f"없는 장비 {eid}: {where[0]}")


def check_level_headroom(players, monsters):
    """출고 데이터로 5~100층을 완주시켜 마지막 레벨을 얻고, 레벨 표가 넉넉한지.

    CurExp 세터가 PlayerDic[Level+1] 을 읽는다. 생성기와 같은 시뮬레이터를 쓰되
    표는 게임이 읽는 JSON 에서 만든다.
    """
    ptable = {lv: dict(need_exp=p["NeedExp"], total_exp=p["TotalExp"], atk=p["Attack"],
                       dfn=p["Defence"], hp=p["MaxHP"], aspd=p["AttackSpeed"],
                       dspd=p["DefenceSpeed"], crit=p["Critical"], crit_atk=p["CriticalAttack"],
                       mspd=p["MoveSpeed"]) for lv, p in players.items()}
    start, err = G.simulate_handmade(ptable)
    if err:
        errors.append(f"도입부 시뮬레이션 실패: {err}")
        return None
    run = []
    for mid, m in monsters.items():
        if mid >= G.MOB_ID_BASE:
            floor, order = divmod(mid - G.MOB_ID_BASE, 8)
            run.append(dict(m, _floor=floor, _boss=False, _order=order))
        elif G.BOSS_ID_BASE <= mid < G.BOSS_ID_BASE + len(G.CHAPTER_THEMES):
            run.append(dict(m, _floor=(mid - G.BOSS_ID_BASE + 1) * G.FLOORS_PER_CHAPTER,
                            _boss=True))
    ok, log, err = G.simulate_run(ptable, run, start, verbose=False)
    if not ok:
        errors.append(f"출고 데이터로 완주하지 못한다: {err}")
        return None
    final = log[-1]["exit_level"]
    if max(players) < final + G.LEVEL_HEADROOM:
        errors.append(f"PlayerData 최대 레벨 {max(players)} — 완주 레벨 {final} 보다 "
                      f"{G.LEVEL_HEADROOM} 이상 커야 한다 (CurExp 세터가 Level+1 을 읽는다)")
    return final


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
    classes = load("MonsterClassData", "monsterClasses")
    equips = load("EquipData", "equips")
    scripts = {s["id"]: s for s in load("ScriptData", "scripts")}
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

    # ---- 레벨 테이블 · 문자열 · 특성 · 장비
    final_level = check_level_headroom(players, monsters)
    check_scripts(stages, monsters, classes, equips, load("EventData", "events"), scripts)
    check_traits(classes, monsters)
    check_grants(stages, maps, monsters, equips)

    # ---- 몬스터 그림은 맵·전투창 두 애니메이터에 상태가 있어야 한다
    for rel in CONTROLLERS:
        states = controller_states(rel)
        for mid, mon in monsters.items():
            if mid < 100:
                continue  # 기존 몬스터는 손대지 않았다 (킹 슬라임은 Boss_C0_* 를 쓴다)
            for field in ("IdleAnimStr", "AttackAnimStr"):
                if mon[field] not in states:
                    errors.append(f"몬스터 {mid}: {os.path.basename(rel)} 에 "
                                  f"'{mon[field]}' 상태가 없다")

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
    print(f"  통과 — 스테이지 {len(stages)}층, 맵 {len(maps)}개, 몬스터 {len(monsters)}종, "
          f"완주 레벨 {final_level} / 레벨 표 {max(players)} (경고 {len(warnings)}건)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
