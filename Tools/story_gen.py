"""스토리 대본(Tools/story)을 게임 데이터로 굽는다.

    python Tools/story_gen.py            # ScriptData(json+csv) 와 Assets/@Scripts/Data/GeneratedStory.cs 를 쓴다
    python Tools/story_gen.py --check    # 쓰지 않고 검사만 — 트리거·그림·화자·연출 신호·층 사실

원본은 Tools/story/story_kr.json 이다. 아직 없으면 parts/*.json 을 파트 순서대로 메모리에서 합친다.
번역은 story_{en,jp,cn}.json 의 같은 키. 빠진 번역은 영어 → 한국어 순으로 채우고 빠진 수를 찍는다.
check_story.py 가 있으면(스토리 팀의 검사) 먼저 돌리고, 실패하면 쓰지 않는다.

ScriptData 에서 이 도구가 쓰는 자리 — generate_content.py --write 는 여기를 건드리지 않는다
(그쪽은 GENERATED_SCRIPT_RANGES·특성·도감·UI 문구만 다시 쓰고 나머지 행은 그대로 둔다).
    300000-309999  통째로 소유한다. 매번 새로 쓰고, 이번에 만들지 않은 행은 지운다.
        300000-300099  화자 이름 (damian·sword 는 원래 쓰던 6·4018 을 제자리에서 고친다)
        300100-300104  챕터 카드 이름 (도감 이름 = HUD 층 이름과 같은 것)
        300110-300114  챕터 카드 부제
        300200-300299  선택지
        301000-        대사: 301000 + 장면 순번 * 40 + 줄 순번 (장면 하나는 40줄까지)
    제자리 개작: prologue_rewrites 의 id (900000-900006, 100011-100031, 7, 8, 4029) 와 화자 이름 6·4018.
    그 밖의 id 는 읽기만 한다.

GeneratedStory.cs 에는 장면 표(트리거는 형식을 갖춰 푼다), 화자 표, 연출 신호(아래 CUES),
그리고 트리거가 쓰는 층별 사실(층 유형·특성·금고 문·여분 열쇠·둘 중 하나)과 층 바크 일정(bark_schedule)을 싣는다.
층별 사실은 실제로 나가는 데이터(Dungeon CSV·MapData·MonsterData)에서 잰다.
"""

import argparse
import glob
import json
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import bestiary          # noqa: E402  챕터 이름 = generate_content 가 쓰는 도감 이름
import generate_content as gc   # noqa: E402  floor_type 과 ScriptData 쓰는 법(write_csv/write_json)을 같이 쓴다
import layout_gen        # noqa: E402  금고 문 판정은 생성기와 같은 훑기로

ROOT = gc.ROOT
STORY = os.path.join(HERE, "story")
SCRIPT_JSON = os.path.join(gc.JSOND, "ScriptData.json")
SCRIPT_CSV = os.path.join(gc.EXCEL, "ScriptData.csv")
OUT_CS = os.path.join(ROOT, "Assets", "@Scripts", "Data", "GeneratedStory.cs")
EMOJI_CONTROLLER = os.path.join(ROOT, "Assets", "@Resources", "Animations", "Emoji", "Image", "Emoji.controller")
ADDRESSABLE_GROUPS = os.path.join(ROOT, "Assets", "AddressableAssetsData", "AssetGroups")
ADDRESSABLE_SETUP = os.path.join(ROOT, "Assets", "@Scripts", "Editor", "AddressableSetup.cs")

OWNED = (300000, 309999)
SPEAKER_BASE = 300000
CHAPTER_NAME_BASE = 300100
CHAPTER_SUB_BASE = 300110
CHOICE_BASE = 300200
LINE_BASE = 301000
LINES_PER_SCENE = 40
FIXED_NAME_IDS = {"damian": 6, "sword": 4018}   # Define.PLAYER_DEFAULT_NAME / SWORD_DEFAULT_NAME

PART_ORDER = ["prologue", "act2", "act3", "act4", "systems"]
LANGS = bestiary.LANGS            # kr en jp cn
COLUMNS = bestiary.COLUMNS

KINDS = {"dialogue": "Dialogue", "card": "Card", "bark": "Bark", "choice": "Choice", "credits": "Credits"}
PROLOGUES = ["contract_after", "kingslime_reveal", "kingslime_clear"]          # StoryPrologue
MECHANICS = ["forecast", "fatal", "overflow", "vault", "spare_key", "choice",    # StoryMechanic
             "rune", "nokey", "warp", "death", "crit", "levelup"]
ENDINGS = ["choice", "seal", "hold", "dawn"]                                    # StoryEnding
TRAITS = {"beast": 1, "magic": 2, "guardian": 3, "immortal": 4,                 # Define.Trait
          "knight": 5, "titan": 6, "assassin": 7, "armor": 8}
FLOOR_TYPES = ["basic", "stingy", "gate", "plenty", "treasure"]                 # generate_content.floor_type 순서
CHAPTERS = 5

EMOTIONS = {
    "damian": ("Normal", "AHA", "Confusion", "Panic", "Question", "Silence", "Surprise", "Thinking"),
    "sword": ("Normal", "Angry", "Madness", "Nerve", "Panic", "Question", "Silence", "Sleep",
              "Smile", "Surprise", "Treasure"),
}
# 대화창 감정 아이콘의 애니메이터 상태. 컨트롤러의 이름이 바이블과 두 군데 다르다 —
# 마검의 Silence 는 "Silense"(오타 그대로), Madness 는 상태가 없고 "Bad" 가 그 그림을 튼다.
# Normal 은 아이콘을 띄우지 않는다 (Adventurer_Normal 은 움직임이 비어 있다).
EMOTION_STATE_FIX = {("sword", "Silence"): "Illust_MagicalSword_Silense",
                     ("sword", "Madness"): "Illust_MagicalSword_Bad"}
EMOJI_PREFIX = {"damian": "Illust_Adventurer_", "sword": "Illust_MagicalSword_"}

# 카드 그림 키 -> 스프라이트 주소 (Load<Sprite> 가 ".sprite" 를 붙인다). 주소는 파일 이름과 같아야
# 한다 — ResourceManager 가 "주소.sprite[이름]" 으로 스프라이트를 꺼내기 때문이다.
IMAGE_ADDRESS = dict(
    {f"Intro0{i}": f"Intro0{i}" for i in range(1, 6)},
    **{f"Loading{i}": f"LoadingIllust{i}" for i in range(1, 7)},
    ForestLine="ForestIllust", ForestColor="ForestColorIllust",
    TowerArrival="04_탑_용병수정", TowerGraves="05_마을", Parchment="00_배경",
    GameOver1="GameOver1", Ending="ThankYouForPlaying",
)

# ---------------------------------------------------------------- 연출 신호
# 장면마다 staging 메모를 옮긴 것. (줄, 종류, 인자). 줄 번호는 메모와 같이 1부터 센다 —
# 그 줄이 뜨는 순간에 친다. 0 은 대화창이 뜨기 전, "end" 는 장면이 끝난 뒤.
# 장면 무리가 늘 하는 일(보스 장면의 레터박스·카메라, 챕터 카드, 결말의 왕좌의 입)은
# StoryDirector 가 트리거를 보고 한다. 여기에는 그 장면에만 있는 것만 적는다.
#   Emote X      플레이어 머리 위 파티클 이모트 FX_Emoji_X
#   Shake        화면 흔들림            Flash  하얗게 번쩍
#   White/Dark   하얗게/까맣게 머문다    Clear  노출을 되돌린다
#   CamUp        카메라를 들었다 돌아온다   CamClose  데미안 머리 위로 붙는다
#   CamPlayer    데미안에게 돌아온다      CamMonster  가장 가까운 몬스터   CamBoss  보스가 있던 자리
#   Pose X       데미안 동작 (DrawSword·ContractSword·Idle)
#   Bgm X / BgmStop / BgmFloor(이 층의 챕터곡으로)
#   Fx X         데미안 자리에 이펙트    Boom  보스 폭발·흰 빛    Soul  보라 영혼
#   Hands / VortexIn / VortexStop / Walk   결말의 왕좌의 입
#   Backdrop     바로 앞 카드 그림을 깔아 둔 채 대화 (장면 전체)
CUES = {
    "pro_contract_after": [(0, "Flash"), (2, "Emote", "SweatDrop")],      # "01 직전" 번쩍 — 대화창보다 먼저
    "mech_forecast": [(5, "Emote", "Question")],          # 3층에는 몬스터가 없다 — 카메라는 데미안에게 (R11)
    "pro_kingslime_reveal": [(1, "Shake"), ("end", "Pose", "DrawSword")],
    "pro_kingslime_clear": [(0, "BgmStop"), (3, "Fx", "FX_PowerWave"), (6, "Dark"), (7, "Clear"),
                            (10, "Emote", "Sleepy"), ("end", "BgmFloor")],
    # 5층: 탑에 닿는 카드(인트로곡) → 챕터곡으로 돌리고 챕터 카드 → ch0_start. 6층: 무덤 카드(인트로곡) → 촌장 → 챕터곡.
    "village_card": [(0, "Bgm", "StartIntro_BGM"), ("end", "BgmFloor")],
    "village_graves": [(0, "Bgm", "StartIntro_BGM")],
    "village_chief": [(0, "Backdrop"), ("end", "BgmFloor")],
    "ch0_start": [(1, "Emote", "Question"), (1, "CamUp")],
    "f11_motto": [(6, "Emote", "SweatDrop")],
    "f16_nap": [(1, "Emote", "Sleepy"), (4, "Emote", "Surprise")],
    "boss0_defeat": [(0, "BgmStop"), (0, "Boom"), (2, "CamBoss"), (3, "Fx", "FX_PowerWave"), ("end", "BgmFloor")],
    "ch1_start": [(0, "CamUp"), (2, "Emote", "SweatDrop"), (4, "Emote", "Question")],
    "f27_sleeptalk": [(1, "Emote", "Sleepy"), (3, "Emote", "Question"), (4, "Emote", "Surprise"),
                      (6, "Emote", "Angry")],
    "f31_trust": [(0, "CamMonster"), (1, "CamPlayer"), (5, "Emote", "Grinning")],
    "f35_suspect": [(3, "Emote", "Question"), (4, "Emote", "SweatDrop")],
    "boss1_intro": [(8, "Emote", "Angry"), ("end", "Pose", "DrawSword")],    # 03 은 놀라지 않는다 (R11)
    "boss1_defeat": [(3, "Boom"), (3, "BgmFloor")],
    "ch2_start": [(0, "CamUp")],
    "f45_forge": [(2, "Emote", "Question"), (5, "Emote", "SweatDrop")],
    "f50_leftovers": [(1, "Emote", "Sleepy"), (3, "Emote", "Grinning"), (10, "Emote", "Grinning")],
    "f55_look": [(1, "CamClose"), (2, "Emote", "SweatDrop"), ("end", "CamPlayer")],
    "boss2_intro": [(3, "Emote", "Question")],
    "boss2_defeat": [(0, "BgmStop"), (3, "Boom"), (3, "White"), (6, "Clear"), ("end", "BgmFloor")],
    "f66_awake": [(4, "Emote", "Sleepy")],
    "f72_enough": [(0, "CamClose"), (3, "CamPlayer")],
    # 09 "네 머리 위에도 숫자가 있다" 에 데미안 머리 위로, 기사가 다시 말하는 12 에 기사에게 돌아온다.
    "boss3_intro": [(5, "Shake"), (9, "CamClose"), (12, "CamBoss"), ("end", "Pose", "DrawSword")],
    "boss3_defeat": [(0, "BgmStop"), (4, "Soul"), (6, "CamPlayer"), ("end", "BgmFloor")],
    "ch4_card": [(0, "BgmStop")],
    "ch4_start": [(1, "CamUp"), (1, "BgmFloor")],
    "f85_honest": [(2, "Emote", "NoAnswer")],
    "f95_price": [(7, "CamClose"), ("end", "CamPlayer")],
    "f99_eve": [(2, "Emote", "Sleepy")],
    "f100_lesson": [(0, "Emote", "Sleepy")],
    "boss4_intro": [(1, "Shake"), (2, "Shake"), (5, "Shake")],
    "ending_seal": [(0, "Pose", "ContractSword"), (0, "Fx", "FX_ContractSwordEffect"), (0, "VortexStop"),
                    (0, "Flash"), (3, "Pose", "Idle"), (10, "Emote", "Sleepy"), (10, "White"), (11, "Dark")],
    "ending_hold": [(0, "Bgm", "EgoSword_Encounter_Event"), (0, "Hands"), (0, "Pose", "ContractSword"),
                    (3, "Shake"), (8, "Walk"), (13, "Flash"), (13, "Dark")],
    "ending_dawn": [(0, "Hands"), (3, "Shake"), (11, "Pose", "ContractSword"), (11, "Fx", "FX_ContractSwordEffect"),
                    (11, "VortexIn"), (11, "Bgm", "MainTitle_BGM"), (13, "White"), (14, "Clear"), (14, "Pose", "Idle")],
    # mech_warp 은 반지를 주울 때 뜬다(R14) — 데미안이 반지 자리에 서 있으니 카메라는 그대로 둔다.
}
CUE_KINDS = ("Emote", "Shake", "Flash", "White", "Dark", "Clear", "CamUp", "CamClose", "CamPlayer",
             "CamMonster", "CamBoss", "Pose", "Bgm", "BgmStop", "BgmFloor", "Fx", "Boom", "Soul",
             "Hands", "VortexIn", "VortexStop", "Walk", "Backdrop")
CUE_NEEDS_ARG = {"Emote", "Pose", "Bgm", "Fx"}
POSES = ("DrawSword", "ContractSword", "Idle")
CUE_END = 999                     # "end" — 장면이 끝난 뒤

# ---------------------------------------------------------------- 층 바크 (바이블 R13·R15)
SWORD_QUIET = (81, 89)            # 이 층들의 마검은 값만 말한다 — 층·죽음 바크는 데미안 것만 (R13)
BARK_EVERY = 3                    # 바크가 설 수 있는 층 셋에 하나꼴 (예전 BarkChance 0.35 와 비슷하다)


# ---------------------------------------------------------------- 읽기

def _read(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def load_story():
    """(대본, 출처). story_kr.json 이 없으면 parts 를 파트 순서대로 합친다 (R1 의 '파일 순서')."""
    merged = os.path.join(STORY, "story_kr.json")
    if os.path.exists(merged):
        return _read(merged), "story_kr.json"

    def rank(path):
        name = os.path.splitext(os.path.basename(path))[0]
        return (PART_ORDER.index(name) if name in PART_ORDER else len(PART_ORDER), name)

    story = {"speakers": {}, "scenes": [], "prologue_rewrites": {}, "bestiary": {}}
    for path in sorted(glob.glob(os.path.join(STORY, "parts", "*.json")), key=rank):
        part = _read(path)
        story["speakers"].update(part.get("speakers") or {})
        story["scenes"] += part.get("scenes") or []
        story["prologue_rewrites"].update(part.get("prologue_rewrites") or {})
        story["bestiary"].update(part.get("bestiary") or {})
    return story, "parts/*.json (story_kr.json 없음)"


def load_translations():
    out = {}
    for lang in LANGS[1:]:
        path = os.path.join(STORY, f"story_{lang}.json")
        out[lang] = _read(path) if os.path.exists(path) else {}
    return out


def sheet(text):
    """ScriptData 칸의 모양: 줄바꿈은 두 글자 \\n, 쉼표는 ^ (GetString 이 되돌린다)."""
    return text.strip().replace("\r\n", "\n").replace("\n", "\\n").replace(",", "^")


def has_hangul(text):
    return bestiary.has_hangul(text)


def controller_states(path):
    with open(path, encoding="utf-8") as f:
        return set(re.findall(r"m_Name: (Illust_\w+)", f.read()))


def addressable_addresses():
    found = set()
    for path in glob.glob(os.path.join(ADDRESSABLE_GROUPS, "*.asset")):
        with open(path, encoding="utf-8") as f:
            for a in re.findall(r"m_Address: (.*)", f.read()):
                a = a.strip()
                # 유니티는 한글 주소를 "05_마을.sprite" 처럼 따옴표 + \u 이스케이프로 적는다 —
                # 그대로 비교하면 등록된 그림이 "등록 대기" 로 보인다. 큰따옴표 YAML 은 JSON 과 같은 \u 를 쓴다.
                if len(a) >= 2 and a[0] == a[-1] == '"':
                    a = json.loads(a)
                found.add(a)
    return found


def pending_addresses():
    """AddressableSetup.Required 에 올려 둔 것 — 에디터에서 등록을 돌리면 생긴다. {주소: 에셋 경로}"""
    with open(ADDRESSABLE_SETUP, encoding="utf-8") as f:
        return dict(re.findall(r'\{\s*"([^"]+)",\s*"([^"]+)"\s*\}', f.read()))


# ---------------------------------------------------------------- 층별 사실

def floor_facts():
    """층(1~100)마다 트리거가 쓰는 사실. 실제로 나가는 데이터에서 잰다."""
    stages = {s["id"]: s for s in _read(os.path.join(gc.JSOND, "StageInfoData.json"))["stageInfos"]}
    monsters = {m["id"]: m for m in _read(os.path.join(gc.JSOND, "MonsterData.json"))["creatures"]}
    maps = {m["Key"]: m["Objects"] for m in _read(os.path.join(gc.JSOND, "MapData.json"))["maps"]}

    def count_at(stage, otype, cell):
        x, y = cell
        for o in maps.get(stage, ()):
            p = o["Position"]
            if o["ObjectType"] == otype and abs(p["X"] - x * 0.32) < 1e-3 and abs(p["Z"] + y * 0.32) < 1e-3:
                return o["Count"]
        return -1

    facts, problems = {}, []
    for floor in range(1, gc.TOTAL_FLOORS + 1):
        stage = floor - 1
        did = stages[stage]["DungeonID"]
        with open(os.path.join(gc.STREAM, f"Dungeon_{did}.csv"), encoding="utf-8-sig") as f:
            grid = [[c.strip() for c in line.split(",")] for line in f.read().splitlines()]
        cells = [(x, y, c) for y, row in enumerate(grid) for x, c in enumerate(row)]
        fact = dict(type=-1, traits=0, vault=-1, spare=-1, choice=False, boss=None)
        facts[floor] = fact
        if floor <= gc.HANDMADE_FLOORS:
            continue   # 손수 만든 도입부. 이야기 트리거는 5층부터다

        fact["type"] = gc.floor_type(floor)
        fact["choice"] = any(c.endswith("~") for _, _, c in cells)
        for _, _, c in cells:
            if c[:2] in ("M_", "B_"):
                mid = int(c[2:].rstrip("~"))
                ability = monsters[mid]["Ability"]
                if 1 <= ability <= 8:
                    fact["traits"] |= 1 << ability
                if c.startswith("B_"):
                    fact["boss"] = mid

        # 금고 문: 막아도 계단까지 길이 남는 문 (큰길 문은 막으면 계단이 끊긴다).
        spawn, up = layout_gen._endpoints(grid)
        vaults = []
        if spawn is not None and up is not None:
            doors = [(x, y) for x, y, c in cells if c in layout_gen.DOOR_CELLS]
            main = [d for d in doors if up not in layout_gen._flood_locked(grid, spawn, {d})]
            vaults = layout_gen.vault_doors(grid, main)
        if len(vaults) > 1:
            problems.append(f"{floor}층: 금고 문이 {len(vaults)}개")
        if vaults:
            fact["vault"] = count_at(stage, 5, vaults[0])      # Define.ObjectType.Door

        # 여분 열쇠: 같은 색 열쇠가 둘인 층의, 파수꾼 뒤 막다른 칸의 열쇠.
        keys = [(x, y, c) for x, y, c in cells if c in layout_gen.KEY_ITEM.values()]
        colors = [c for _, _, c in keys]
        spare = []
        for x, y, c in keys:
            if colors.count(c) < 2:
                continue
            nbr = [(x + dx, y + dy) for dx, dy in layout_gen.NEIGHBORS
                   if layout_gen._inside((x + dx, y + dy)) and layout_gen._passable(grid, (x + dx, y + dy))]
            if len(nbr) == 1 and grid[nbr[0][1]][nbr[0][0]].startswith("M_"):
                spare.append((x, y))
        if len(spare) > 1:
            problems.append(f"{floor}층: 여분 열쇠 후보가 {len(spare)}개")
        if spare:
            fact["spare"] = count_at(stage, 3, spare[0])       # Define.ObjectType.CItem
        if (fact["vault"] == -1) != (not vaults) or (fact["spare"] == -1) != (not spare):
            problems.append(f"{floor}층: 금고 문/여분 열쇠가 MapData 에 없다 (mapdata_gen 을 다시 돌릴 것)")
    return facts, problems


def chapter_first_floor(chapter):
    """챕터 카드가 뜨는 층 — 챕터의 첫 생성 층 (챕터 0 은 손수 만든 1~4층 다음)."""
    return max(gc.HANDMADE_FLOORS + 1, chapter * gc.FLOORS_PER_CHAPTER + 1)


def entry_floors(scenes):
    """들어설 때 장면이 걸리는 층 (StoryDirector.FloorChain 과 같은 규칙). 그 층은 바크 대신 장면이 뜬다.
    특성 수업(TraitFirst)은 들어설 때가 아니라 그 특성에 처음 부딪힐 때 뜬다(StoryDirector.TraitGate) — 여기 없다."""
    floors = set()
    for s in scenes:
        t, a = s["trigger"], s["arg"]
        if t == "Village":
            floors.add(gc.HANDMADE_FLOORS + 1)
        elif t == "ChapterStart":
            floors.add(chapter_first_floor(a))
        elif t == "FloorFirst":
            floors.add(a)
    return floors


def bark_schedule(scenes, speaker_rows, facts):
    """층(0~100) -> 들어설 때 뜨는 층 유형 바크(scenes 의 번호), 없으면 -1. GeneratedStory.FloorBarks 가 된다.

    바크가 설 수 있는 층(이야기 장면·보스가 없는 층) BARK_EVERY 개에 하나씩 고르게 세우고, 유형마다 변주를 파일
    순서대로 돌린다(R15 — 예전의 층 번호 씨앗 난수로는 두 변주가 한 번도 안 뽑혔다). SWORD_QUIET 층에서는 마검
    변주를 건너뛴다(R13). 몇 번째 층부터 셀지(위상)는 변주가 다 뜨는 첫 것 — 안 되면 check 가 실패로 잡는다."""
    pools = {}
    for i, s in enumerate(scenes):
        if s["trigger"] == "FloorType":
            pools.setdefault(s["arg"], []).append(i)
    taken = entry_floors(scenes) | {20 * (c + 1) for c in range(CHAPTERS)}
    free = [f for f in range(gc.HANDMADE_FLOORS + 1, gc.TOTAL_FLOORS + 1)
            if facts[f]["type"] in pools and f not in taken]
    plan = [-1] * (gc.TOTAL_FLOORS + 1)
    for phase in range(BARK_EVERY):
        plan, turn = [-1] * (gc.TOTAL_FLOORS + 1), {}
        for floor in free[phase::BARK_EVERY]:
            kind = facts[floor]["type"]
            quiet = SWORD_QUIET[0] <= floor <= SWORD_QUIET[1]
            usable = [i for i in pools[kind] if not (quiet and speaker_rows[scenes[i]["lines"][0][0]][0] == "sword")]
            if usable:
                plan[floor] = usable[turn.get(kind, 0) % len(usable)]
                turn[kind] = turn.get(kind, 0) + 1
        if all(i in plan for pool in pools.values() for i in pool):
            break
    return plan


# ---------------------------------------------------------------- 대본 풀기

def parse_trigger(text, choice_ids):
    """(C# StoryTrigger 이름, 정수 인자) 또는 예외."""
    head, _, arg = text.partition(":")
    if head in ("village", "death", "credits") and not arg:
        return head.capitalize(), 0
    if head == "prologue" and arg in PROLOGUES:
        return "Prologue", PROLOGUES.index(arg)
    if head in ("chapter_start", "boss_intro", "boss_defeat") and arg.isdigit() and int(arg) < CHAPTERS:
        return {"chapter_start": "ChapterStart", "boss_intro": "BossIntro",
                "boss_defeat": "BossDefeat"}[head], int(arg)
    if head == "floor_first" and arg.isdigit() and gc.HANDMADE_FLOORS < int(arg) <= gc.TOTAL_FLOORS:
        return "FloorFirst", int(arg)
    if head == "trait_first" and arg in TRAITS:
        return "TraitFirst", TRAITS[arg]
    if head == "floor_type" and arg in FLOOR_TYPES:
        return "FloorType", FLOOR_TYPES.index(arg)
    if head == "mechanic_first" and arg in MECHANICS:
        return "Mechanic", MECHANICS.index(arg)
    if head in ("ending", "epilogue") and arg in ENDINGS and (arg in choice_ids or arg in ("choice", "dawn")):
        return head.capitalize(), ENDINGS.index(arg)
    raise ValueError(f"알 수 없는 트리거 '{text}'")


class Book:
    """ScriptData 행을 모은다. 번역이 없으면 영어 → 한국어로 채우고 빠진 수를 센다."""

    def __init__(self, translations, current):
        self.tr = translations
        self.current = current          # 지금 ScriptData (제자리 개작의 옛 번역을 볼 때)
        self.rows = {}
        self.missing = {lang: 0 for lang in LANGS[1:]}
        self.kept = 0

    def put_all(self, sid, texts):
        """네 언어가 이미 다 있는 행 (도감이 번역까지 갖고 있는 챕터 이름)."""
        assert sid not in self.rows, f"ScriptData {sid} 를 두 번 쓴다"
        self.rows[sid] = dict(id=sid, **{col: sheet(texts[lang]) for lang, col in zip(LANGS, COLUMNS)})

    def put(self, sid, kr, lookup, keep_old=False):
        """lookup(번역 파일) -> 그 언어 문구 또는 None.

        keep_old: 제자리 개작이다. 새 영어가 없으면 지금 칸의 영어가 멀쩡할 때 그것을 둔다 —
        뜻은 조금 낡아도 한국어를 영어 칸에 넣는 것보다 낫다. CSV 가 쪼갠 행(따옴표로 시작)은 버린다.
        """
        assert sid not in self.rows, f"ScriptData {sid} 를 두 번 쓴다"
        texts = {"kr": sheet(kr)}
        found = {lang: lookup(self.tr[lang]) for lang in LANGS[1:]}
        en = sheet(found["en"]) if found["en"] else None
        if en is None and keep_old:
            old = (self.current.get(sid) or {}).get("ScriptEn")
            if old and not has_hangul(old) and not old.startswith('"'):
                en = old
                self.kept += 1
        for lang in LANGS[1:]:
            if found[lang]:
                texts[lang] = sheet(found[lang])
            else:
                self.missing[lang] += 1
                texts[lang] = en or texts["kr"]
        self.rows[sid] = dict(id=sid, **{col: texts[lang] for lang, col in zip(LANGS, COLUMNS)})


def _get(d, *keys):
    for k in keys:
        if not isinstance(d, dict):
            return None
        d = d.get(k)
    return d if isinstance(d, str) and d.strip() else None


def _chapter_subtitle(doc, c):
    rows = (doc.get("bestiary") or {}).get("chapters") or []
    return _get(rows[c], "subtitle") if c < len(rows) else None


def build(story, translations, current, facts, checks):
    """ScriptData 행들과 C# 에 쓸 표를 만든다. 문제는 checks 에 쌓는다."""
    errors, warnings = checks["errors"], checks["warnings"]
    book = Book(translations, current)
    emoji_states = controller_states(EMOJI_CONTROLLER)

    # 화자 --------------------------------------------------------------
    speakers = story.get("speakers") or {}
    order = ["damian", "sword", "narration"] + [s for s in speakers if s not in ("damian", "sword", "narration")]
    speaker_rows = []
    next_id = SPEAKER_BASE
    for sid in order:
        if sid == "narration":
            speaker_rows.append((sid, 0, "None", 0))
            continue
        name = _get(speakers, sid, "kr")
        if name is None:
            errors.append(f"화자 '{sid}' 의 이름(kr)이 없다")
            continue
        if sid in FIXED_NAME_IDS:
            name_id = FIXED_NAME_IDS[sid]
            book.put(name_id, name, lambda t, s=sid: _get(t, "speakers", s), keep_old=True)
        else:
            name_id = next_id
            next_id += 1
            book.put(name_id, name, lambda t, s=sid: _get(t, "speakers", s))
        m = re.fullmatch(r"boss(\d)", sid)
        portrait = "Damian" if sid == "damian" else "Sword" if sid == "sword" else "Boss" if m else "None"
        speaker_rows.append((sid, name_id, portrait, int(m.group(1)) if m else 0))
    speaker_index = {row[0]: i for i, row in enumerate(speaker_rows)}

    # 챕터 카드 — 이름은 도감이 네 언어로 낸 것(HUD 의 층 이름과 같다), 부제는 스토리 도감 -------
    books, _ = bestiary.tables()
    for c in range(CHAPTERS):
        book.put_all(CHAPTER_NAME_BASE + c, {lang: books[lang]["chapters"][c].replace("\\n", "\n")
                                             for lang in LANGS})
        sub_kr = _chapter_subtitle(story, c)
        if sub_kr:
            book.put(CHAPTER_SUB_BASE + c, sub_kr, lambda t, c=c: _chapter_subtitle(t, c))
        else:
            errors.append(f"도감 chapters[{c}] 의 subtitle 이 없다 (챕터 카드 부제)")

    # 장면 -------------------------------------------------------------
    scenes = story.get("scenes") or []
    ids = [s.get("id") for s in scenes]
    for dup in sorted({i for i in ids if ids.count(i) > 1}):
        errors.append(f"장면 id '{dup}' 가 둘 이상이다")
    choice_ids = [c.get("id") for s in scenes if s.get("kind") == "choice" for c in s.get("choices") or []]
    out_scenes = []
    next_choice = CHOICE_BASE
    for si, scene in enumerate(scenes):
        sid = scene.get("id", f"#{si}")
        kind = KINDS.get(scene.get("kind"))
        if kind is None:
            errors.append(f"{sid}: kind '{scene.get('kind')}' 를 모른다")
            continue
        try:
            trig, arg = parse_trigger(scene.get("trigger", ""), choice_ids)
        except ValueError as e:
            errors.append(f"{sid}: {e}")
            continue
        lines = scene.get("lines") or []
        if not lines:
            errors.append(f"{sid}: 줄이 없다")
        if len(lines) > LINES_PER_SCENE:
            errors.append(f"{sid}: 줄이 {len(lines)}개 — 장면 하나는 {LINES_PER_SCENE}줄까지")
        if kind == "Bark" and len(lines) != 1:
            errors.append(f"{sid}: 바크는 한 줄이어야 한다")
        out_lines = []
        for li, line in enumerate(lines[:LINES_PER_SCENE]):
            key = line.get("key", "")
            if key != f"{sid}.{li + 1:02d}":
                errors.append(f"{sid}: {li + 1}번째 줄의 key '{key}' (≠ {sid}.{li + 1:02d})")
            spk = line.get("speaker")
            if spk not in speaker_index:
                errors.append(f"{key}: 화자 '{spk}' 가 speakers 에 없다")
                continue
            if kind in ("Card", "Credits") and spk != "narration":
                errors.append(f"{key}: {kind.lower()} 는 narration 만 쓴다")
            emotion = line.get("emotion")
            state = None
            if emotion is not None:
                if spk not in EMOTIONS or emotion not in EMOTIONS[spk]:
                    errors.append(f"{key}: 감정 '{emotion}' 는 {spk} 가 쓸 수 없다")
                elif emotion != "Normal":
                    state = EMOTION_STATE_FIX.get((spk, emotion), EMOJI_PREFIX[spk] + emotion)
                    if state not in emoji_states:
                        errors.append(f"{key}: 감정 아이콘 상태 '{state}' 가 Emoji.controller 에 없다")
            image = line.get("image")
            address = None
            if image is not None:
                address = IMAGE_ADDRESS.get(image)
                if address is None:
                    errors.append(f"{key}: 그림 키 '{image}' 를 모른다")
                if kind != "Card":
                    warnings.append(f"{key}: card 가 아닌 장면의 image 는 쓰지 않는다")
            text = line.get("kr")
            if not isinstance(text, str) or not text.strip():
                errors.append(f"{key}: kr 문구가 없다")
                continue
            if text.strip().count("\n") > 2 and kind != "Credits":
                warnings.append(f"{key}: 3줄을 넘는다")
            script_id = LINE_BASE + si * LINES_PER_SCENE + li
            book.put(script_id, text, lambda t, k=key: _get(t, "lines", k))
            out_lines.append((speaker_index[spk], state, script_id, address))

        out_choices = []
        if kind == "Choice":
            for choice in scene.get("choices") or []:
                cid = choice.get("id")
                if cid not in ("seal", "hold"):
                    errors.append(f"{sid}: 선택지 id '{cid}' 는 결말 트리거가 될 수 없다 (seal·hold, dawn 은 조건으로만 간다)")
                    continue
                book.put(next_choice, choice.get("kr", ""), lambda t, k=f"{sid}.{cid}": _get(t, "choices", k))
                out_choices.append((cid, next_choice))
                next_choice += 1
            if len(out_choices) != 2:
                errors.append(f"{sid}: 선택지는 둘이어야 한다")
        elif scene.get("choices"):
            errors.append(f"{sid}: choice 가 아닌 장면에 choices 가 있다")

        out_cues = []
        for cue in CUES.get(sid, ()):
            line_no, ck = cue[0], cue[1]
            carg = cue[2] if len(cue) > 2 else None
            n = CUE_END if line_no == "end" else line_no
            if ck not in CUE_KINDS:
                errors.append(f"연출 신호 {sid} {cue}: 종류를 모른다")
            elif (ck in CUE_NEEDS_ARG) != (carg is not None):
                errors.append(f"연출 신호 {sid} {cue}: 인자가 맞지 않는다")
            elif n != CUE_END and not 0 <= n <= len(lines):
                errors.append(f"연출 신호 {sid} {cue}: {len(lines)}줄짜리 장면이다")
            elif ck == "Pose" and carg not in POSES:
                errors.append(f"연출 신호 {sid} {cue}: 동작 '{carg}' 를 모른다")
            out_cues.append((n, ck, carg))
        out_scenes.append(dict(id=sid, kind=kind, trigger=trig, arg=arg,
                               lines=out_lines, choices=out_choices, cues=out_cues))
    for sid in CUES:
        if sid not in ids:
            warnings.append(f"연출 신호가 있는데 장면 '{sid}' 가 대본에 없다")

    # 프롤로그 개작 -------------------------------------------------------
    for key, text in (story.get("prologue_rewrites") or {}).items():
        if not str(key).isdigit():
            errors.append(f"prologue_rewrites 의 키 '{key}' 가 ScriptData id 가 아니다")
            continue
        rid = int(key)
        if OWNED[0] <= rid <= OWNED[1] or rid in book.rows:
            errors.append(f"prologue_rewrites {rid} 가 이 도구의 다른 행과 겹친다")
            continue
        if rid not in current:
            errors.append(f"prologue_rewrites {rid} 가 ScriptData 에 없다 (제자리에서만 고친다)")
            continue
        book.put(rid, text, lambda t, k=key: _get(t, "prologue_rewrites", k), keep_old=True)
    return book, speaker_rows, out_scenes


# ---------------------------------------------------------------- 검사

def check(scenes, speaker_rows, facts, fact_problems, checks, plan):
    errors, warnings = checks["errors"], checks["warnings"]
    errors.extend(fact_problems)
    by = {}
    for s in scenes:
        by.setdefault((s["trigger"], s["arg"]), []).append(s)

    # 층 바크는 변주마다 적어도 한 층에서 뜬다(R15). 81~89층의 죽음 바크는 데미안 것만 뜬다(R13).
    for i, s in enumerate(scenes):
        if s["trigger"] == "FloorType" and i not in plan:
            errors.append(f"{s['id']}: 이 바크가 뜨는 층이 없다 (bark_schedule — 변주보다 설 층이 적다)")
    speaker_of = lambda s: speaker_rows[s["lines"][0][0]][0] if s["lines"] else None
    if by.get(("Death", 0)) and all(speaker_of(s) == "sword" for s in by[("Death", 0)]):
        errors.append(f"death 바크가 전부 마검 것이다 — {SWORD_QUIET[0]}~{SWORD_QUIET[1]}층에서 뜰 것이 없다 (R13)")

    # 트리거가 실제로 걸릴 수 있는가
    for s in scenes:
        t, a = s["trigger"], s["arg"]
        if t == "TraitFirst" and not any(f["traits"] >> a & 1 for f in facts.values()):
            errors.append(f"{s['id']}: 특성 {a} 몬스터가 나오는 층이 없다")
        if t == "FloorType" and not any(f["type"] == a for f in facts.values()):
            errors.append(f"{s['id']}: 그 유형의 층이 없다")
        if t in ("BossIntro", "BossDefeat") and facts[20 * (a + 1)]["boss"] is None:
            errors.append(f"{s['id']}: {20 * (a + 1)}층에 보스가 없다")
        if t == "Mechanic" and MECHANICS[a] == "vault" and not any(f["vault"] >= 0 for f in facts.values()):
            errors.append(f"{s['id']}: 금고 문이 있는 층이 없다")
        if t == "Mechanic" and MECHANICS[a] == "spare_key" and not any(f["spare"] >= 0 for f in facts.values()):
            errors.append(f"{s['id']}: 여분 열쇠가 있는 층이 없다")
        if t == "Mechanic" and MECHANICS[a] == "choice" and not any(f["choice"] for f in facts.values()):
            errors.append(f"{s['id']}: 둘 중 하나 보상이 있는 층이 없다")
        if s["kind"] == "Bark" and t not in ("FloorType", "Death", "Mechanic"):
            errors.append(f"{s['id']}: 바크는 floor_type·death·mechanic_first 에서만 뜬다")
        if t in ("FloorType", "Death") and s["kind"] != "Bark":
            errors.append(f"{s['id']}: floor_type·death 는 바크 전용이다")
        if s["kind"] == "Bark" and t == "Mechanic" and MECHANICS[s["arg"]] not in ("spare_key", "nokey"):
            warnings.append(f"{s['id']}: 이 mechanic 은 대화로 트는 자리다 — 바크로 두면 막지 않고 지나간다")
    # 같은 트리거에 장면이 여럿이면 파일 순서대로 잇는다(R1). 바크가 아닌 것이 섞이면 안 된다.
    for (t, a), group in by.items():
        kinds = {s["kind"] for s in group}
        if "Bark" in kinds and len(kinds) > 1:
            errors.append(f"트리거 {t}:{a} 에 바크와 대화가 섞여 있다")
    # 결말의 짝
    choice = by.get(("Ending", 0))
    if not choice or choice[0]["kind"] != "Choice":
        errors.append("ending:choice 장면(kind choice)이 없다")
    else:
        for cid, _ in choice[0]["choices"]:
            if ("Ending", ENDINGS.index(cid)) not in by:
                errors.append(f"선택지 {cid} 로 갈 ending:{cid} 장면이 없다")
    if ("Ending", ENDINGS.index("dawn")) not in by:
        warnings.append("ending:dawn 이 없다 — hold 는 늘 hold 로 간다")
    for e in ENDINGS[1:]:
        if ("Ending", ENDINGS.index(e)) in by and ("Epilogue", ENDINGS.index(e)) not in by:
            warnings.append(f"epilogue:{e} 가 없다")
    if ("Credits", 0) not in by:
        errors.append("credits 장면이 없다")
    for c in range(CHAPTERS):
        if ("ChapterStart", c) not in by:
            warnings.append(f"chapter_start:{c} 장면이 없다 — 챕터 카드만 뜬다")
    if not any(row[0] == "damian" for row in speaker_rows) or not any(row[0] == "sword" for row in speaker_rows):
        errors.append("speakers 에 damian·sword 가 있어야 한다")

    # 그림: 주소가 등록돼 있거나 AddressableSetup.Required 에 올라 있어야 한다
    registered = addressable_addresses()
    pending = pending_addresses()
    used = {line[3] for s in scenes for line in s["lines"] if line[3]}
    for address in sorted(used):
        key = address + ".sprite"
        if key in registered:
            continue
        if key not in pending:
            errors.append(f"그림 '{address}' 가 어드레서블에 없다 (AddressableSetup.Required 에 올릴 것)")
        elif not os.path.exists(os.path.join(ROOT, pending[key])):
            errors.append(f"그림 '{address}' 의 파일이 없다: {pending[key]}")
        else:
            warnings.append(f"그림 '{address}' 는 등록 대기 중 (AddressableSetup.RegisterRuntimePrefabs)")
    # 연출 신호가 부르는 에셋
    for s in scenes:
        for n, ck, carg in s["cues"]:
            key = {"Emote": f"FX_Emoji_{carg}", "Fx": carg, "Bgm": carg}.get(ck)
            if key and key not in registered:
                errors.append(f"연출 신호 {s['id']} {ck} {carg}: 어드레서블 '{key}' 가 없다")
    for key in ("UI_ConversationPopup", "UI_IntroScene", "UI_StageNamePopup", "UI_GameScene", "BossPortal",
                "FX_BossPortal_A", "BossDeathBoom", "BossDeathLight", "DeathSoulPurple", "UIMonsterAnimController",
                "Adventurer.sprite", "MagicalSword.sprite", "Boss_Entry_BGM", "MainTitle_BGM"):
        if key not in registered:
            errors.append(f"스토리 UI·연출이 쓰는 '{key}' 가 어드레서블에 없다")


def run_story_check():
    """스토리 팀의 검사기(Tools/story/check_story.py). 없으면 None."""
    path = os.path.join(STORY, "check_story.py")
    if not os.path.exists(path):
        return None
    proc = subprocess.run([sys.executable, path], cwd=STORY, capture_output=True, text=True,
                          encoding="utf-8", errors="replace")
    return proc.returncode, (proc.stdout + proc.stderr).strip()


# ---------------------------------------------------------------- 쓰기

def cs(value):
    return "null" if value is None else json.dumps(value, ensure_ascii=False)


def emit_cs(scenes, speaker_rows, facts, source, plan):
    out = ["// Tools/story_gen.py 가 Tools/story/ 대본에서 굽는다. 손으로 고치지 않는다.",
           f"// 대본: {source}. 문구는 ScriptData 에 있다 (화자 300000~, 챕터 300100~, 선택지 300200~, 대사 301000~).",
           "public static class GeneratedStory", "{",
           "    public static readonly StorySpeaker[] Speakers =", "    {"]
    for sid, name_id, portrait, chapter in speaker_rows:
        out.append(f"        new StorySpeaker({cs(sid)}, {name_id}, StoryPortrait.{portrait}, {chapter}),")
    out += ["    };", "", "    public static readonly StoryScene[] Scenes =", "    {"]
    for s in scenes:
        out.append(f"        new StoryScene({cs(s['id'])}, StoryKind.{s['kind']}, StoryTrigger.{s['trigger']}, {s['arg']},")
        out.append("            new[]")
        out.append("            {")
        for spk, state, script_id, image in s["lines"]:
            out.append(f"                new StoryLine({spk}, {cs(state)}, {script_id}, {cs(image)}),")
        out.append("            },")
        if s["choices"]:
            items = ", ".join(f"new StoryChoice(StoryEnding.{cid.capitalize()}, {sid})" for cid, sid in s["choices"])
            out.append(f"            new[] {{ {items} }},")
        else:
            out.append("            null,")
        if s["cues"]:
            items = ", ".join(f"new StoryCue({n}, StoryCueKind.{k}, {cs(a)})" for n, k, a in s["cues"])
            out.append(f"            new[] {{ {items} }}),")
        else:
            out.append("            null),")
    out += ["    };", ""]

    floors = range(0, gc.TOTAL_FLOORS + 1)

    def array(ctype, name, values, comment):
        out.append(f"    // {comment}")
        out.append(f"    public static readonly {ctype}[] {name} =")
        out.append("    {")
        values = list(values)
        for i in range(0, len(values), 20):
            out.append("        " + ", ".join(values[i:i + 20]) + ",")
        out.append("    };")

    out.append("    // 층별 사실 — 인덱스는 층 번호(1~100). 0 번과 손수 만든 1~4층은 비어 있다.")
    array("sbyte", "FloorTypes", (str(facts[f]["type"]) if f else "-1" for f in floors),
          "generate_content.floor_type: 0 기본 1 인색 2 관문 3 넉넉 4 보물")
    array("int", "VaultDoors", (str(facts[f]["vault"]) if f else "-1" for f in floors),
          "금고 문(네 번째 문)의 Door._doorIndex_forActive, 없으면 -1")
    array("int", "SpareKeys", (str(facts[f]["spare"]) if f else "-1" for f in floors),
          "여분 열쇠의 ConsumableItem._itemIndex_forActive, 없으면 -1")
    array("bool", "ChoiceFloors", (("true" if facts[f]["choice"] else "false") if f else "false" for f in floors),
          "둘 중 하나 보상(칸 끝의 ~)이 있는 층")
    array("int", "FloorBarks", (str(v) for v in plan),
          "들어설 때 뜨는 층 유형 바크(Scenes 의 번호), 없으면 -1 — story_gen.bark_schedule (바이블 R13·R15)")
    out.append("    // 마검이 값만 말하는 층 — 죽음 바크는 데미안 것만 (바이블 R13)")
    out.append(f"    public const int SwordQuietFrom = {SWORD_QUIET[0]}, SwordQuietTo = {SWORD_QUIET[1]};")
    bosses = [facts[20 * (c + 1)]["boss"] for c in range(CHAPTERS)]
    firsts = [chapter_first_floor(c) for c in range(CHAPTERS)]
    out.append("    // 챕터 c 의 보스(MonsterData id)와 첫 생성 층, 챕터 카드 문구")
    out.append(f"    public static readonly int[] BossIds = {{ {', '.join(str(b) for b in bosses)} }};")
    out.append(f"    public static readonly int[] ChapterFirstFloors = {{ {', '.join(map(str, firsts))} }};")
    out.append(f"    public static readonly int[] ChapterNameIds = {{ {', '.join(str(CHAPTER_NAME_BASE + c) for c in range(CHAPTERS))} }};")
    out.append(f"    public static readonly int[] ChapterSubtitleIds = {{ {', '.join(str(CHAPTER_SUB_BASE + c) for c in range(CHAPTERS))} }};")
    out.append("}")
    with open(OUT_CS, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(out) + "\n")


def emit_scripts(book, current_rows):
    """ScriptData.json(원본)과 .csv 를 generate_content 와 같은 모양으로 쓴다."""
    owned = lambda sid: OWNED[0] <= sid <= OWNED[1]
    rows = []
    for row in current_rows:
        sid = row["id"]
        if sid in book.rows:
            fresh = book.rows.pop(sid)
            row = dict(row)
            row.update({col: fresh[col] for col in COLUMNS})
            rows.append(row)
        elif not owned(sid):
            rows.append(row)
    rows += book.rows.values()
    rows.sort(key=lambda r: r["id"])
    gc.write_json(SCRIPT_JSON, "scripts", rows)
    header = ["id", "ScriptKr", "ScriptEn", "ScriptJp", "ScriptCn"]
    gc.write_csv(SCRIPT_CSV, header, [[row.get(k, "") for k in header] for row in rows])
    return rows


# ---------------------------------------------------------------- 실행

def coverage(scenes):
    counts = {}
    for s in scenes:
        counts[s["trigger"]] = counts.get(s["trigger"], 0) + 1
    return ", ".join(f"{k} {v}" for k, v in sorted(counts.items()))


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--check", action="store_true", help="쓰지 않고 검사만")
    args = ap.parse_args()

    story, source = load_story()
    translations = load_translations()
    current_rows = _read(SCRIPT_JSON)["scripts"]
    current = {r["id"]: r for r in current_rows}
    facts, fact_problems = floor_facts()

    checks = {"errors": [], "warnings": []}
    book, speaker_rows, scenes = build(story, translations, current, facts, checks)
    plan = bark_schedule(scenes, speaker_rows, facts)
    check(scenes, speaker_rows, facts, fact_problems, checks, plan)

    story_check = run_story_check()

    print(f"[story_gen] 대본: {source} — 장면 {len(scenes)}, 대사·이름 {len(book.rows)}행")
    print(f"            트리거: {coverage(scenes)}")
    print(f"            층 사실: 금고 {sum(f['vault'] >= 0 for f in facts.values())}층, "
          f"여분 열쇠 {sum(f['spare'] >= 0 for f in facts.values())}층, "
          f"둘 중 하나 {sum(f['choice'] for f in facts.values())}층, "
          f"층 바크 {sum(v >= 0 for v in plan)}층")
    have = [lang for lang in LANGS[1:] if translations[lang]]
    print(f"            번역 파일: {', '.join(have) or '없음'} — 빠진 칸 "
          + ", ".join(f"{lang} {n}" for lang, n in book.missing.items())
          + (f" (개작 {book.kept}행은 옛 영어를 둠)" if book.kept else "") + " → 영어, 없으면 한국어로 채움")
    if story_check is None:
        print("            check_story.py 없음 — 스토리 팀 검사는 건너뜀")
    else:
        code, text = story_check
        print(f"            check_story.py: {'통과' if code == 0 else f'실패 (코드 {code})'}")
        if code != 0:
            checks["errors"].append("check_story.py 가 실패했다:\n" + text[-2000:])
    for w in checks["warnings"]:
        print(f"  [경고] {w}")
    for e in checks["errors"]:
        print(f"  [실패] {e}")
    if checks["errors"]:
        print(f"[story_gen] 실패 {len(checks['errors'])}건 — 아무것도 쓰지 않았다")
        return 1
    if args.check:
        print("[story_gen] 검사 통과 (--check 이라 쓰지 않음)")
        return 0

    emit_scripts(book, current_rows)
    emit_cs(scenes, speaker_rows, facts, source, plan)
    print(f"[story_gen] 썼다: {os.path.relpath(SCRIPT_JSON, ROOT)}, {os.path.relpath(SCRIPT_CSV, ROOT)}, "
          f"{os.path.relpath(OUT_CS, ROOT)}")
    return 0


def _self_check():
    """손으로 잡은 규칙 몇 개가 그대로인지 (python Tools/story_gen.py --selftest)."""
    assert sheet("a, b\nc") == "a^ b\\nc"
    assert parse_trigger("floor_first:31", []) == ("FloorFirst", 31)
    assert parse_trigger("trait_first:armor", []) == ("TraitFirst", 8)
    assert parse_trigger("ending:seal", ["seal", "hold"]) == ("Ending", 1)
    assert parse_trigger("mechanic_first:spare_key", []) == ("Mechanic", 4)
    for bad in ("floor_first:4", "chapter_start:5", "ending:seal", "mechanic_first:nope", "village:1"):
        try:
            parse_trigger(bad, [])
        except ValueError:
            continue
        raise AssertionError(bad)
    assert gc.floor_type(5) == 0 and gc.floor_type(9) == 4 and gc.floor_type(20) == 0   # FLOOR_TYPES 순서
    book = Book({"en": {"lines": {"x.01": "Hi, you"}}, "jp": {}, "cn": {}}, {})
    book.put(301000, "안녕, 너", lambda t: _get(t, "lines", "x.01"))
    row = book.rows[301000]
    assert row["ScriptKr"] == "안녕^ 너" and row["ScriptEn"] == "Hi^ you" and row["ScriptJp"] == "Hi^ you"
    assert book.missing == {"en": 0, "jp": 1, "cn": 1}
    # 층 바크: 변주(마검·데미안·마검)가 다 뜨고, 보스층·81~89층의 마검 바크는 없다 (R13·R15)
    rows = [("damian", 6, "Damian", 0), ("sword", 4018, "Sword", 0)]
    fake = [dict(trigger="FloorType", arg=0, lines=[(spk, None, 0, None)]) for spk in (1, 0, 1)]
    fake_facts = {f: dict(type=0 if f > gc.HANDMADE_FLOORS else -1, traits=0) for f in range(1, gc.TOTAL_FLOORS + 1)}
    plan = bark_schedule(fake, rows, fake_facts)
    assert {0, 1, 2} <= set(plan), plan
    assert all(plan[f] == -1 for f in (20, 40, 60, 80, 100))
    assert all(plan[f] in (-1, 1) for f in range(SWORD_QUIET[0], SWORD_QUIET[1] + 1))
    print("story_gen 자체 점검 통과")


if __name__ == "__main__":
    if "--selftest" in sys.argv:
        _self_check()
        sys.exit(0)
    sys.exit(main())
