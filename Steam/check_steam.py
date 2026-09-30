"""Steam/ 폴더가 게임과 어긋나지 않았는지 본다. 표준 라이브러리만 쓴다.

    python Steam/check_steam.py

- Rich Presence 의 챕터·숲 이름이 ScriptData(300100~300104, 5000~5003)와 같은가 (번체는 게임에 아직 없어 개수만)
- 다섯 언어 파일의 토큰이 같고, {#X_%key%} 가 가리키는 토큰이 있는가
- achievements.csv 의 칸이 다 찼고 API 이름이 겹치지 않는가
- 스토어 짧은 설명이 300자 이하인가
- SteamHooks.cs 가 생기면: 코드가 부르는 업적·통계·토큰 이름이 여기 것과 같은가
FAIL 이 하나라도 있으면 종료 코드 1.
"""
import csv, json, re, sys
from pathlib import Path

sys.stdout.reconfigure(errors="replace")      # cp949 콘솔에서 중국어·일본어 값을 찍다 죽지 않게
HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
STATS = {"MAX_FLOOR", "ENDINGS_SEEN", "FIGHTS_WON", "DEATHS"}          # stats.md 와 같아야 한다
LANGS = {"koreana": "ScriptKr", "english": "ScriptEn", "japanese": "ScriptJp", "schinese": "ScriptCn", "tchinese": None}
NAME_IDS = {**{f"#C_{i}": 300100 + i for i in range(5)}, **{f"#F_{i + 1}": 5000 + i for i in range(4)}}
fails = []


def fail(msg): fails.append(msg); print("FAIL", msg)


def tokens(path):
    return dict(re.findall(r'"(#[^"]+)"\s+"([^"]*)"', path.read_text(encoding="utf-8")))


# ---------------------------------------------------------------- Rich Presence
script = {r["id"]: r for r in json.loads((ROOT / "Assets/@Resources/Data/JsonData/ScriptData.json")
                                         .read_text(encoding="utf-8"))["scripts"]}
rp = {lang: tokens(HERE / "rich_presence" / f"{lang}.vdf") for lang in LANGS}
before = len(fails)
keys = set(rp["english"])
for lang, column in LANGS.items():
    t = rp[lang]
    if set(t) != keys:
        fail(f"rich_presence/{lang}.vdf 토큰이 english 와 다르다: {sorted(set(t) ^ keys)}")
    for value in t.values():
        for prefix in re.findall(r"\{(#\w+?_)%\w+%\}", value):
            if not any(k.startswith(prefix) for k in t):
                fail(f"rich_presence/{lang}.vdf: {{{prefix}...}} 가 가리키는 토큰이 없다")
    if column:
        for token, sid in NAME_IDS.items():
            if t.get(token) != script[sid][column]:
                fail(f"rich_presence/{lang}.vdf {token}='{t.get(token)}' != ScriptData {sid} '{script[sid][column]}'")
if len(fails) == before:
    print("OK  rich_presence", len(keys), "tokens x", len(LANGS), "languages")

# ---------------------------------------------------------------- achievements
with open(HERE / "achievements.csv", encoding="utf-8-sig", newline="") as f:
    rows = list(csv.DictReader(f))
apis = [r["api_name"] for r in rows]
before = len(fails)
if len(set(apis)) != len(apis):
    fail("achievements.csv: API 이름이 겹친다")
for r in rows:
    api = r["api_name"]
    if not re.fullmatch(r"ACH_[A-Z0-9_]+", api) or r["wired"] not in ("yes", "planned") or r["hidden"] not in ("0", "1"):
        fail(f"achievements.csv {api}: api_name/wired/hidden 값")
    if r["progress_stat"] and r["progress_stat"] not in STATS:
        fail(f"achievements.csv {api}: progress_stat {r['progress_stat']} 는 stats.md 에 없다")
    if r["icon_achieved"] != f"icons/{api}.png" or r["icon_unachieved"] != f"icons/{api}_locked.png":
        fail(f"achievements.csv {api}: 아이콘 파일 이름")
    empty = [k for k in r if k.startswith(("name_", "desc_")) and not r[k].strip()]
    if empty:
        fail(f"achievements.csv {api}: 빈 칸 {empty}")
wired = {r["api_name"] for r in rows if r["wired"] == "yes"}
if len(fails) == before:
    print("OK  achievements", len(rows), "rows,", len(wired), "wired")

# ---------------------------------------------------------------- store 짧은 설명 (## 짧은 설명 뒤 첫 ``` 블록)
for md in sorted((HERE / "store").glob("*.md")):
    m = re.search(r"^## 짧은 설명.*?```[^\n]*\n(.*?)\n```", md.read_text(encoding="utf-8"), re.S | re.M)
    if not m:
        fail(f"store/{md.name}: '## 짧은 설명' 블록이 없다")
    elif len(m.group(1).strip()) > 300:
        fail(f"store/{md.name}: 짧은 설명 {len(m.group(1).strip())}자 > 300")
    else:
        print(f"OK  store/{md.name} 짧은 설명 {len(m.group(1).strip())}자")

# ---------------------------------------------------------------- SteamPipe 스크립트
for vdf in sorted((HERE / "scripts").glob("app_build_*.vdf")):
    for depot in re.findall(r'"(depot_build_[^"]+\.vdf)"', vdf.read_text(encoding="utf-8")):
        if not (vdf.parent / depot).exists():
            fail(f"scripts/{vdf.name} 가 없는 {depot} 를 가리킨다")
print("OK  scripts")

# ---------------------------------------------------------------- 코드와 맞추기 (다른 레인이 쓰는 파일 — 읽기만)
hooks = list((ROOT / "Assets/@Scripts").rglob("SteamHooks.cs"))
if not hooks:
    print("WARN SteamHooks.cs 가 아직 없다 — 코드 대조를 건너뛴다")
else:
    code = hooks[0].read_text(encoding="utf-8")
    used = set(re.findall(r'"(ACH_[A-Z0-9_]+)"', code))
    full = used & set(apis)
    # "ACH_BOSS" + floor 처럼 이어 붙여 만드는 이름은 앞부분만 글자로 남는다 — 그 앞부분으로 시작하는 업적은 부른 것으로 친다
    prefixes = {u for u in used - full if any(a.startswith(u) for a in apis)}
    for api in sorted(used - full - prefixes):
        fail(f"SteamHooks.cs 가 부르는 {api} 가 achievements.csv 에 없다")
    for api in sorted(full - wired):
        fail(f"SteamHooks.cs 가 {api} 를 부른다 — achievements.csv 의 wired 를 yes 로")
    for api in sorted(wired - full):
        if not any(api.startswith(p) for p in prefixes):
            print(f"WARN {api} 는 wired=yes 인데 SteamHooks.cs 에 그 이름도, 그 앞부분도 없다")
    for stat in sorted(STATS - set(re.findall(r'"([A-Z_]+)"', code))):
        print(f"WARN 통계 {stat} 가 SteamHooks.cs 에 없다")
    for token in sorted(set(re.findall(r'"(#[A-Za-z0-9_]+)"', code)) - keys):
        fail(f"SteamHooks.cs 의 Rich Presence 토큰 {token} 가 rich_presence 에 없다")
    print("OK  SteamHooks.cs 대조 끝")

manager = list((ROOT / "Assets/@Scripts").rglob("SteamManager.cs"))
if manager:
    m = re.search(r"AppId\s*=\s*(\d+)", manager[0].read_text(encoding="utf-8"))
    app = m.group(1) if m else "?"
    print(("WARN SteamManager.AppId 가 아직 " + app + " — README 1절") if app in ("0", "480", "?")
          else "OK  SteamManager.AppId " + app)

sys.exit(1 if fails else 0)
