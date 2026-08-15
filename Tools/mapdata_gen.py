"""StreamingAssets 의 Dungeon_*.csv 100장 -> MapData.json.

런타임 `DataManager.ResetActiveDic()` 과 **완전히 같은 순서/규칙**으로 파싱해야 한다.
프리팹에 구워지는 `_monsterIndex_forActive` 등이 런타임 ActiveDic 의 키와 1:1로 맞아야
"이미 잡은 몬스터"가 엉뚱한 층에서 사라지는 사고가 안 난다.

ResetActiveDic 과 다른 점은 하나뿐:
  바닥 셀("1")을 Floor 오브젝트로도 내보낸다. 원본은 이걸 버리고 층마다 손으로 그린
  FloorField 스프라이트를 깔았는데, 100층 분량을 손으로 그릴 수 없어서 타일로 깐다.
  카운터가 붙는 오브젝트가 아니라서 인덱스 정합성에는 영향이 없다.
"""

import json
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
STREAM = os.path.join(ROOT, "Assets", "StreamingAssets", "Data", "Excel")
JSOND = os.path.join(ROOT, "Assets", "@Resources", "Data", "JsonData")

TILE_SIZE = 0.32
HANDMADE_FLOORS = 4   # generate_content.HANDMADE_FLOORS 와 같아야 한다

# Define.ObjectType 와 값이 같아야 한다
VOID, FLOOR, WALL, CITEM, EITEM, DOOR, PORTAL, MONSTER, BOSS, SPAWN, LEVER, PILLAR = range(12)

_DIGITS = re.compile(r"[^0-9]")


def _obj(otype, id_, count, x, z):
    return {
        "Id": id_, "Count": count, "ObjectType": otype,
        "Position": {"X": round(x, 4), "Y": 0.0, "Z": round(z, 4)},
    }


def build_mapdata():
    files = sorted(f for f in os.listdir(STREAM)
                   if "Dungeon" in f and "meta" not in f)

    counts = dict(citem=0, eitem=0, monster=0, boss=0, door=0, pillar=0, lever=0)
    maps = []

    for map_id, name in enumerate(files):
        with open(os.path.join(STREAM, name), encoding="utf-8-sig") as f:
            lines = f.read().split("\n")

        objects = []
        for y, line in enumerate(lines):
            row = line.replace("\r", "").split(",")
            z = -1.0 * y * TILE_SIZE

            for x, block in enumerate(row):
                if len(block) == 0:
                    block = "0"
                digits = _DIGITS.sub("", block)
                id_ = int(digits) if digits else 0
                px = x * TILE_SIZE

                c = block[0]
                if c == "I":
                    objects.append(_obj(CITEM, id_, counts["citem"], px, z))
                    counts["citem"] += 1
                elif c == "E":
                    objects.append(_obj(EITEM, id_, counts["eitem"], px, z))
                    counts["eitem"] += 1
                elif c == "M":
                    objects.append(_obj(MONSTER, id_, counts["monster"], px, z))
                    counts["monster"] += 1
                elif c == "B":
                    # 보스도 "몬스터 카운터"를 쓴다.
                    # 전투 결말은 UI_MonsterCard.Dead() 하나뿐이고, 거기서
                    # MonsterActiveDic[IsActiveIndex] 를 끈다. 보스에게 별도 0~4 를 주면
                    # 1층 일반 몬스터 0~4 가 대신 죽은 것으로 기록된다.
                    objects.append(_obj(BOSS, id_, counts["monster"], px, z))
                    counts["monster"] += 1
                    counts["boss"] += 1
                elif c == "W":
                    objects.append(_obj(WALL, id_, 0, px, z))
                elif 3 <= id_ <= 8:
                    objects.append(_obj(DOOR, id_, counts["door"], px, z))
                    counts["door"] += 1
                elif id_ == 11:
                    objects.append(_obj(SPAWN, id_, 0, px, z))
                elif id_ == 12:
                    objects.append(_obj(LEVER, id_, counts["lever"], px, z))
                    counts["lever"] += 1
                elif id_ == 13:
                    objects.append(_obj(PILLAR, id_, counts["pillar"], px, z))
                    counts["pillar"] += 1
                elif id_ in (14, 15, 16):
                    objects.append(_obj(PORTAL, id_, 0, px, z))
                elif id_ == 0:
                    objects.append(_obj(VOID, 0, 0, px, z))
                elif id_ == 1:
                    # ResetActiveDic 은 여기서 아무것도 안 만든다. 우리는 바닥을 깐다.
                    objects.append(_obj(FLOOR, 1, 0, px, z))

        maps.append({"Key": map_id, "Objects": objects})

    return maps, counts


def emit_mapdata():
    maps, counts = build_mapdata()
    path = os.path.join(JSOND, "MapData.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"maps": maps}, f, ensure_ascii=False)
    return maps, counts


def _selftest():
    """포탈/스폰이 층마다 성립하는지 — 이게 깨지면 진행 자체가 막힌다."""
    maps, counts = build_mapdata()
    assert len(maps) == 100, f"맵 100개여야 하는데 {len(maps)}개"

    # 보스와 일반 몬스터는 MonsterActiveDic 을 공유한다. 인덱스가 겹치면
    # 보스를 잡을 때 엉뚱한 층의 몬스터가 죽은 것으로 기록된다.
    seen = {}
    for m in maps:
        for o in m["Objects"]:
            if o["ObjectType"] in (MONSTER, BOSS):
                prev = seen.get(o["Count"])
                assert prev is None, (
                    f"몬스터 인덱스 {o['Count']} 중복 "
                    f"({prev + 1}층 / {m['Key'] + 1}층)")
                seen[o["Count"]] = m["Key"]
    assert sorted(seen) == list(range(len(seen))), "몬스터 인덱스에 구멍이 있다"

    for m in maps:
        floor = m["Key"] + 1
        # 1~4층은 손수 만든 도입부. 프리팹이 실물이고 CSV 는 세이브 인덱스 계산용이라
        # 생성 규칙(스폰/계단/보스)으로 재단하면 안 된다.
        if floor <= HANDMADE_FLOORS:
            continue

        types = [o["ObjectType"] for o in m["Objects"]]
        ids = [o["Id"] for o in m["Objects"] if o["ObjectType"] == PORTAL]

        assert types.count(SPAWN) >= 1, f"{floor}층 스폰 지점 없음"
        if floor < 100:
            assert 14 in ids, f"{floor}층 올라가는 계단(14) 없음"
        if floor > 1:
            assert 15 in ids, f"{floor}층 내려가는 계단(15) 없음"
        assert types.count(MONSTER) >= 1, f"{floor}층 몬스터 없음"
        if floor % 20 == 0:
            assert types.count(BOSS) == 1, f"{floor}층 보스 없음"

    print(f"  self-test OK — 맵 {len(maps)}, {counts}")


if __name__ == "__main__":
    _selftest()
