#!/usr/bin/env python3
"""스토리 데이터 검사기 (표준 라이브러리만). 형식은 FORMAT.md.

    python check_story.py              story_kr.json 검사
    python check_story.py en           + story_en.json 이 kr 의 모든 키를 덮는지 (jp, cn 도 같다)
    python check_story.py --selftest   검사기가 고장 난 데이터를 실제로 잡는지

오류가 하나라도 있으면 종료 코드 1. 경고(한 줄 26자 초과 같은 것)는 종료 코드에 영향이 없다.
"""
import copy
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))

KINDS = {"dialogue", "card", "bark", "choice", "credits"}
EMOTIONS = {
    "damian": set("Normal AHA Confusion Panic Question Silence Surprise Thinking".split()),
    "sword": set("Normal Angry Madness Nerve Panic Question Silence Sleep Smile Surprise Treasure".split()),
}
IMAGES = {"Intro01", "Intro02", "Intro03", "Intro04", "Intro05", "TowerArrival", "TowerGraves",
          "Parchment", "ForestLine", "ForestColor", "GameOver1", "Ending"} | {f"Loading{i}" for i in range(1, 7)}
ID = r"[a-z][a-z0-9_]*"
_ONE = lambda words: "(" + "|".join(words.split()) + ")"
TRIGGERS = [                                      # (패턴, 인자 검사) — FORMAT 의 트리거 표
    (r"prologue:" + _ONE("contract_after kingslime_reveal kingslime_clear"), None),
    (r"village", None),
    (r"chapter_start:([0-4])", None),
    (r"floor_first:([1-9][0-9]*)", lambda n: 5 <= int(n) <= 100),
    (r"trait_first:" + _ONE("beast magic guardian immortal knight titan assassin armor"), None),
    (r"floor_type:" + _ONE("basic stingy gate plenty treasure"), None),
    (r"mechanic_first:" + _ONE("forecast fatal overflow vault spare_key choice rune nokey warp death crit levelup"), None),
    (r"death", None),
    (r"boss_intro:([0-4])", None),
    (r"boss_defeat:([0-4])", None),
    (r"ending:(" + ID + ")", None),
    (r"epilogue:(" + ID + ")", None),
    (r"credits", None),
]
BARK_ONLY = ("floor_type:", "death")               # FORMAT: bark 전용
CHAPTER_FIRST = {5, 21, 41, 61, 81}               # 이 층은 chapter_start 를 쓴다
MAX_LINES, SOFT_WIDTH = 3, 26
TYPOS = ("외뢰서", "버텨기", "있을꺼야", "받친다")    # BRIEF 4절
ITEMS = ("monster_book", "warp_ring", "key", "potion", "rune")
# 도감 설명은 한 벌씩 따로 쓴다(STORY_BIBLE 13절). 예전 mob_desc 는 특성 문장 틀을 되풀이해 기계가 쓴 것처럼
# 읽혔다 — 간혹 40번, 그중에는 40번. 그리고 같은 챕터·같은 그림도 띠마다 특성이 달라서
# (generate_content.BAND_TRAITS) 설명에 규칙을 적으면 바로 위 특성 줄과 어긋난다. 규칙은 특성 줄이 말한다.
DESC_TEMPLATES = ("간혹", "그중에는", "개중에는", "덩치 큰 놈", "셋이 함께", "셋이 설 때", "셋으로 나오는", "세 마리가 한 층")
DESC_RULE_WORDS = ("치명타", "%", "예상 피해", "강타", "흡혈", "철벽", "방어력", "체력")
SCENE_FIELDS = {"id", "kind", "trigger", "staging", "lines", "choices"}
LINE_FIELDS = {"key", "speaker", "emotion", "image", "kr"}


def _nlines(text):
    return text.count("\n") + 1


def _text(v):
    return isinstance(v, str) and v.strip() != ""


def repeated_sentences(texts):
    """{자리: 문구} 에서 두 자리가 같이 쓰는 문장. 도감을 이어 읽으면 같은 문장이 틀로 보인다."""
    seen, out = {}, []
    for where, text in texts.items():
        for s in re.split(r"(?<=[.!?。！？])\s*|\n", text if isinstance(text, str) else ""):
            s = s.strip()
            if s and seen.setdefault(s, where) != where:
                out.append(f"'{s}' 가 {seen[s]} 와 {where} 에 같이 있다")
    return out


def _descs(b):
    """도감에서 이어 읽히는 설명 — 몬스터(챕터:그림)와 보스."""
    out = {f"mob_desc['{k}']": v for k, v in (b.get("mob_desc") or {}).items()}
    out.update({f"bosses[chapter {r.get('chapter')}].desc": r.get("desc") for r in b.get("bosses") or []})
    return out


def trigger_error(trigger):
    """맞으면 None, 아니면 이유."""
    for pat, arg_ok in TRIGGERS:
        m = re.fullmatch(pat, trigger)
        if m:
            if arg_ok and not arg_ok(m.group(1)):
                return f"트리거 '{trigger}' 의 값이 범위 밖"
            return None
    return f"모르는 트리거 '{trigger}'"


def existing_prologue(path):
    """기존 프롤로그의 ScriptData id -> 줄 수. 기존 데이터의 줄바꿈은 글자 그대로의 \\n 이다."""
    with open(path, encoding="utf-8") as f:
        old = json.load(f)
    out = {}
    for row in old.get("intro_cards", []) + old.get("events", []):
        if "script" in row:
            out[str(row["script"])] = row["kr"].count("\\n") + 1
    for key, text in old.get("other", {}).items():         # contract_text_8 -> 8
        out[re.search(r"(\d+)$", key).group(1)] = text.count("\\n") + 1
    return out


def check(doc, old_prologue):
    """story_kr.json 한 벌. (오류, 경고) 목록."""
    err, warn = [], []
    for k in ("version", "speakers", "scenes", "prologue_rewrites", "bestiary"):
        if k not in doc:
            err.append(f"최상위 키 '{k}' 가 없다")
    if not isinstance(doc.get("version"), int):
        err.append("version 은 정수여야 한다")

    speakers = doc.get("speakers") or {}
    for sp, v in speakers.items():
        if not isinstance(v, dict) or not isinstance(v.get("kr"), str):
            err.append(f"speakers.{sp}: {{\"kr\": \"이름\"}} 모양이어야 한다")
        elif not v["kr"].strip() and sp != "narration":
            err.append(f"speakers.{sp}: 화면 이름이 비었다")

    scenes = doc.get("scenes") or []
    ids, keys, by_trigger, ending_ids, epilogue_ids, choice_ids = set(), set(), {}, set(), set(), []
    for n, s in enumerate(scenes):
        sid = s.get("id", f"#{n}")
        where = f"장면 {sid}"
        for k in ("id", "kind", "trigger", "lines"):
            if k not in s:
                err.append(f"{where}: '{k}' 가 없다")
        for k in set(s) - SCENE_FIELDS:
            warn.append(f"{where}: 모르는 필드 '{k}'")
        if not re.fullmatch(ID, str(sid)):
            err.append(f"{where}: id 는 영문 snake_case")
        if sid in ids:
            err.append(f"{where}: id 가 겹친다")
        ids.add(sid)
        if not _text(s.get("staging")):
            warn.append(f"{where}: staging 이 비었다")

        kind, trigger = s.get("kind"), str(s.get("trigger", ""))
        if kind not in KINDS:
            err.append(f"{where}: 모르는 kind '{kind}'")
        why = trigger_error(trigger)
        if why:
            err.append(f"{where}: {why}")
        m = re.fullmatch(r"floor_first:(\d+)", trigger)
        if m and int(m.group(1)) in CHAPTER_FIRST:
            warn.append(f"{where}: {m.group(1)}층은 챕터 첫 층이라 chapter_start 를 쓴다")
        if trigger.startswith(BARK_ONLY) and kind != "bark":
            err.append(f"{where}: '{trigger}' 는 bark 전용")
        if (trigger == "credits") != (kind == "credits"):
            err.append(f"{where}: credits 트리거와 credits kind 는 짝이다")
        if trigger == "ending:choice" and kind != "choice":
            err.append(f"{where}: ending:choice 장면은 kind choice 여야 한다")
        if trigger.startswith("ending:") and trigger != "ending:choice":
            ending_ids.add(trigger.split(":", 1)[1])
        if trigger.startswith("epilogue:"):
            epilogue_ids.add((sid, trigger.split(":", 1)[1]))
        by_trigger.setdefault(trigger, []).append(kind)

        choices = s.get("choices")
        if kind == "choice":
            if not isinstance(choices, list) or len(choices) < 2:
                err.append(f"{where}: choice 장면에는 선택지가 둘 이상 있어야 한다")
            else:
                seen = set()
                for c in choices:
                    cid = c.get("id") if isinstance(c, dict) else None
                    if not cid or not re.fullmatch(ID, cid) or cid in seen:
                        err.append(f"{where}: 선택지 id '{cid}' 가 잘못됐거나 겹친다")
                    elif not _text(c.get("kr")):
                        err.append(f"{where}: 선택지 '{cid}' 문구가 비었다")
                    seen.add(cid)
                    choice_ids.append((sid, cid))
        elif choices is not None:
            err.append(f"{where}: choices 는 choice 장면에만 둔다")

        lines = s.get("lines")
        if not isinstance(lines, list) or not lines:
            err.append(f"{where}: lines 가 비었다")
            continue
        if kind == "bark" and len(lines) != 1:
            err.append(f"{where}: bark 는 한 줄이다 (지금 {len(lines)})")
        for i, l in enumerate(lines, 1):
            key = l.get("key")
            at = f"{where} {key or '#' + str(i)}"
            for k in set(l) - LINE_FIELDS:
                warn.append(f"{at}: 모르는 필드 '{k}'")
            if key != f"{sid}.{i:02d}":
                err.append(f"{at}: key 는 '{sid}.{i:02d}' 여야 한다 (장면 id + 두 자리 순번)")
            if key in keys:
                err.append(f"{at}: key 가 겹친다")
            keys.add(key)

            sp = l.get("speaker")
            if sp not in speakers:
                err.append(f"{at}: speakers 에 없는 화자 '{sp}'")
            if sp in EMOTIONS:
                if l.get("emotion") not in EMOTIONS[sp]:
                    err.append(f"{at}: {sp} 의 감정 '{l.get('emotion')}' 은 쓸 수 없다")
            elif "emotion" in l:
                err.append(f"{at}: emotion 은 damian·sword 만 쓴다")
            if kind in ("card", "credits") and sp != "narration":
                err.append(f"{at}: {kind} 의 화자는 narration")
            if "image" in l and (kind != "card" or l["image"] not in IMAGES):
                err.append(f"{at}: image '{l['image']}' 는 card 에서 FORMAT 목록의 그림만")

            kr = l.get("kr")
            if not _text(kr):
                err.append(f"{at}: kr 이 비었다")
                continue
            if "\\n" in kr:
                err.append(f"{at}: 줄바꿈은 JSON 의 \\n (글자 그대로의 백슬래시가 들어 있다)")
            if _nlines(kr) > MAX_LINES:
                err.append(f"{at}: {_nlines(kr)}줄 (최대 {MAX_LINES})")
            if kind == "bark" and "\n" in kr:
                err.append(f"{at}: bark 는 한 줄이다")
            if kind != "credits":
                for row in kr.split("\n"):
                    if len(row) > SOFT_WIDTH:
                        warn.append(f"{at}: {len(row)}자 '{row}'")
            for t in TYPOS:
                if t in kr:
                    err.append(f"{at}: 오탈자 '{t}'")

    for sid, cid in choice_ids:
        if cid not in ending_ids:
            err.append(f"장면 {sid}: 선택지 '{cid}' 로 이어질 ending:{cid} 장면이 없다")
    for sid, eid in epilogue_ids:
        if eid not in ending_ids:
            err.append(f"장면 {sid}: epilogue:{eid} 앞에 올 ending:{eid} 장면이 없다")
    for trigger, kinds in by_trigger.items():
        if "bark" in kinds and len(set(kinds)) > 1:
            warn.append(f"트리거 {trigger}: bark 와 다른 kind 가 섞였다")
        if "bark" not in kinds and len([k for k in kinds if k != "card"]) > 1:
            warn.append(f"트리거 {trigger}: 카드가 아닌 장면이 여럿이다 (파일 순서대로 잇는다, R1)")

    # 프롤로그 개작: 기존 대사 슬롯마다 하나, 줄 수 유지 (BRIEF 4절)
    rewrites = doc.get("prologue_rewrites") or {}
    for sid, n in sorted(old_prologue.items(), key=lambda kv: int(kv[0])):
        v = rewrites.get(sid)
        if not _text(v):
            err.append(f"prologue_rewrites.{sid}: 없다")
        elif _nlines(v) != n:
            err.append(f"prologue_rewrites.{sid}: {_nlines(v)}줄 — 기존 {n}줄을 지켜야 한다")
    for sid, v in rewrites.items():
        if sid not in old_prologue:
            err.append(f"prologue_rewrites.{sid}: 기존 프롤로그에 없는 id")
        if isinstance(v, str):
            if "\\n" in v:
                err.append(f"prologue_rewrites.{sid}: 줄바꿈은 JSON 의 \\n")
            for t in TYPOS:
                if t in v:
                    err.append(f"prologue_rewrites.{sid}: 오탈자 '{t}'")

    b_err, b_warn = check_bestiary(doc.get("bestiary") or {}, speakers)
    return err + b_err, warn + b_warn


def check_bestiary(b, speakers):
    err, warn = [], []

    def need(obj, fields, where):
        for f in fields:
            if not _text((obj or {}).get(f)):
                err.append(f"bestiary.{where}.{f}: 없다")
            elif _nlines(obj[f]) > MAX_LINES:
                warn.append(f"bestiary.{where}.{f}: {_nlines(obj[f])}줄")

    chapters = b.get("chapters") or []
    if len(chapters) != 5:
        err.append(f"bestiary.chapters: 5개여야 한다 (지금 {len(chapters)})")
    for c, row in enumerate(chapters):
        need(row, ("name", "subtitle", "mob_prefix"), f"chapters[{c}]")

    species = b.get("species") or []
    if [r.get("art") for r in species] != list(range(10)):
        err.append("bestiary.species: art 0~9 가 순서대로 하나씩 있어야 한다")
    for a, row in enumerate(species):
        need(row, ("name",), f"species[{a}]")

    mob = b.get("mob_desc") or {}
    want = {f"{c}:{a}" for c in range(5) for a in range(10)}
    for k in sorted(want - set(mob)):
        err.append(f"bestiary.mob_desc['{k}']: 없다")
    for k in sorted(set(mob) - want):
        err.append(f"bestiary.mob_desc['{k}']: 모르는 키 (챕터 0~4 : 그림 0~9)")
    for k in sorted(want & set(mob)):
        need(mob, (k,), "mob_desc")
        if not _text(mob[k]):
            continue
        for w in DESC_TEMPLATES:
            if w in mob[k]:
                err.append(f"bestiary.mob_desc['{k}']: 틀 문구 '{w}' — 설명은 그 종만의 것으로 쓴다")
        for w in DESC_RULE_WORDS:
            if w in mob[k]:
                err.append(f"bestiary.mob_desc['{k}']: 규칙 낱말 '{w}' — 규칙은 특성 줄이 말한다 (띠마다 특성이 다르다)")
        if _nlines(mob[k]) > 2:
            warn.append(f"bestiary.mob_desc['{k}']: {_nlines(mob[k])}줄 — 한두 문장")

    bosses = b.get("bosses") or []
    if sorted(r.get("chapter") for r in bosses) != list(range(5)):
        err.append("bestiary.bosses: chapter 0~4 가 하나씩 있어야 한다")
    for row in bosses:
        c = row.get("chapter")
        need(row, ("name", "title", "desc"), f"bosses[chapter {c}]")
        spoken = (speakers.get(f"boss{c}") or {}).get("kr")
        if spoken and row.get("name") and spoken != row["name"]:
            warn.append(f"bestiary.bosses[chapter {c}]: 이름 '{row['name']}' 이 화자 이름 '{spoken}' 과 다르다")
    err += [f"bestiary: {x}" for x in repeated_sentences(_descs(b))]

    traits = b.get("traits") or []
    if [r.get("id") for r in traits] != list(range(9)):
        err.append("bestiary.traits: id 0~8 이 순서대로 하나씩 있어야 한다")
    for row in traits:
        need(row, ("name", "desc"), f"traits[{row.get('id')}]")

    items = b.get("items") or {}
    for k in ITEMS:
        need(items.get(k), ("name", "desc"), f"items.{k}")

    # 몬스터 이름은 "{접두어} {종}" (FORMAT). 겹치거나 어색하게 되풀이되면 안 된다.
    if len(chapters) == 5 and len(species) == 10:
        names = {}
        for c, ch in enumerate(chapters):
            for a, sp in enumerate(species):
                prefix, kind = ch.get("mob_prefix", ""), sp.get("name", "")
                if prefix and prefix in kind:
                    warn.append(f"bestiary: 접두어 '{prefix}' 가 종 이름 '{kind}' 에 겹친다")
                name = f"{prefix} {kind}"
                if name in names:
                    err.append(f"bestiary: 몬스터 이름 '{name}' 이 {names[name]} 와 겹친다")
                names[name] = f"{c}:{a}"
        for row in bosses:
            if row.get("name") in names:
                err.append(f"bestiary: 보스 이름 '{row['name']}' 이 일반 몬스터 이름과 겹친다")
    return err, warn


def check_translation(kr, tr, lang):
    """번역 파일이 story_kr.json 의 모든 키를 덮는가 (FORMAT '번역 파일')."""
    err, warn = [], []
    if tr.get("lang") != lang:
        warn.append(f"story_{lang}.json: lang 이 '{tr.get('lang')}'")

    def cover(section, want, allow_empty=()):
        have = tr.get(section) or {}
        for k, src in want.items():
            v = have.get(k)
            if not isinstance(v, str) or (not v.strip() and k not in allow_empty):
                err.append(f"{lang}.{section}['{k}']: 없다")
            elif section != "speakers" and _nlines(v) > MAX_LINES and _nlines(src) <= MAX_LINES:
                warn.append(f"{lang}.{section}['{k}']: {_nlines(v)}줄")
        for k in sorted(set(have) - set(want)):
            err.append(f"{lang}.{section}['{k}']: kr 에 없는 키")

    sp = {k: v.get("kr", "") for k, v in (kr.get("speakers") or {}).items()}
    cover("speakers", sp, allow_empty={k for k, v in sp.items() if not v})
    cover("lines", {l["key"]: l["kr"] for s in kr["scenes"] for l in s["lines"]})
    cover("choices", {f"{s['id']}.{c['id']}": c["kr"] for s in kr["scenes"] for c in s.get("choices", [])})
    rewrites = kr.get("prologue_rewrites") or {}
    cover("prologue_rewrites", rewrites)
    for k, v in (tr.get("prologue_rewrites") or {}).items():
        if k in rewrites and isinstance(v, str) and _nlines(v) != _nlines(rewrites[k]):
            warn.append(f"{lang}.prologue_rewrites['{k}']: {_nlines(v)}줄 — kr 은 {_nlines(rewrites[k])}줄")

    kb, tb = kr.get("bestiary") or {}, tr.get("bestiary") or {}

    def field(path, value):
        if not _text(value):
            err.append(f"{lang}.bestiary.{path}: 없다")

    for c, row in enumerate(kb.get("chapters", [])):
        t = (tb.get("chapters") or [])[c:c + 1]
        for f in ("name", "subtitle", "mob_prefix"):
            field(f"chapters[{c}].{f}", t[0].get(f) if t else None)
    t_species = {r.get("art"): r for r in tb.get("species") or []}
    for row in kb.get("species", []):
        field(f"species[{row['art']}].name", t_species.get(row["art"], {}).get("name"))
    for k in kb.get("mob_desc", {}):
        field(f"mob_desc['{k}']", (tb.get("mob_desc") or {}).get(k))
    t_bosses = {r.get("chapter"): r for r in tb.get("bosses") or []}
    for row in kb.get("bosses", []):
        for f in ("name", "title", "desc"):
            field(f"bosses[chapter {row['chapter']}].{f}", t_bosses.get(row["chapter"], {}).get(f))
    t_traits = {r.get("id"): r for r in tb.get("traits") or []}
    for row in kb.get("traits", []):
        for f in ("name", "desc"):
            field(f"traits[{row['id']}].{f}", t_traits.get(row["id"], {}).get(f))
    for k in kb.get("items", {}):
        for f in ("name", "desc"):
            field(f"items.{k}.{f}", ((tb.get("items") or {}).get(k) or {}).get(f))
    err += [f"{lang}.bestiary: {x}" for x in repeated_sentences(_descs(tb))]
    return err, warn


def summary(doc):
    scenes = doc.get("scenes") or []
    kinds = {}
    for s in scenes:
        kinds[s.get("kind")] = kinds.get(s.get("kind"), 0) + 1
    lines = sum(len(s.get("lines") or []) for s in scenes)
    return (f"장면 {len(scenes)} · 대사 {lines} · "
            + " ".join(f"{k} {v}" for k, v in sorted(kinds.items(), key=lambda kv: str(kv[0])))
            + f" · 개작 {len(doc.get('prologue_rewrites') or {})}")


def load(name):
    with open(os.path.join(HERE, name), encoding="utf-8") as f:
        return json.load(f)


def selftest():
    """고장 낸 사본마다 오류가 적어도 하나 나와야 한다."""
    doc, old = load("story_kr.json"), existing_prologue(os.path.join(HERE, "existing_prologue.json"))
    base, _ = check(doc, old)

    def broken(fn):
        d = copy.deepcopy(doc)
        fn(d)
        e, _ = check(d, old)
        return len(e) > len(base)

    first = lambda d: d["scenes"][0]
    choice = lambda d: next(s for s in d["scenes"] if s["kind"] == "choice")
    cases = {
        "모르는 트리거": lambda d: first(d).update(trigger="floor_first:4"),
        "겹친 key": lambda d: first(d)["lines"][1].update(key=first(d)["lines"][0]["key"]),
        "목록 밖 감정": lambda d: first(d)["lines"][0].update(emotion="Bad"),
        "네 줄": lambda d: first(d)["lines"][0].update(kr="가\n나\n다\n라"),
        "없는 화자": lambda d: first(d)["lines"][0].update(speaker="nobody"),
        "도감 빈칸": lambda d: d["bestiary"]["mob_desc"].pop("3:7"),
        "도감 틀 문구": lambda d: d["bestiary"]["mob_desc"].update({"0:0": "간혹 이런 놈이 섞여 있다."}),
        "도감 규칙 낱말": lambda d: d["bestiary"]["mob_desc"].update({"0:0": "평타는 20%만 들어간다."}),
        "도감 같은 문장": lambda d: d["bestiary"]["mob_desc"].update({"0:1": d["bestiary"]["mob_desc"]["0:0"]}),
        "개작 빠짐": lambda d: d["prologue_rewrites"].pop("100031"),
        "개작 줄 수": lambda d: d["prologue_rewrites"].update({"100011": "뭐야?!\n뭐야?!"}),
        "결말 없는 선택지": lambda d: choice(d)["choices"].append({"id": "flee", "kr": "도망친다"}),
    }
    failed = [name for name, fn in cases.items() if not broken(fn)]
    tr = {"lang": "en", "speakers": {}, "lines": {}, "choices": {}, "prologue_rewrites": {}, "bestiary": {}}
    e, _ = check_translation(doc, tr, "en")
    if not any("lines" in x for x in e):
        failed.append("번역 빈칸")
    en = load("story_en.json")
    twin = copy.deepcopy(en)
    twin["bestiary"]["mob_desc"]["0:1"] = twin["bestiary"]["mob_desc"]["0:0"]
    if len(check_translation(doc, twin, "en")[0]) <= len(check_translation(doc, en, "en")[0]):
        failed.append("번역 도감 같은 문장")
    assert not failed, f"검사기가 못 잡았다: {failed}"
    print(f"selftest 통과 — {len(cases) + 2}가지 고장을 모두 잡았다")


def main(argv):
    if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if "--selftest" in argv:
        selftest()
        return 0
    doc = load("story_kr.json")
    err, warn = check(doc, existing_prologue(os.path.join(HERE, "existing_prologue.json")))
    for lang in argv:
        path = os.path.join(HERE, f"story_{lang}.json")
        if not os.path.exists(path):
            err.append(f"story_{lang}.json 이 없다")
            continue
        e, w = check_translation(doc, load(f"story_{lang}.json"), lang)
        err += e
        warn += w
    for w in warn:
        print("경고:", w)
    for e in err:
        print("오류:", e)
    print(summary(doc))
    print(f"오류 {len(err)} · 경고 {len(warn)}" + (" — 통과" if not err else ""))
    return 1 if err else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
