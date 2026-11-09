#!/usr/bin/env python3
"""CLAUDE.md 규칙 가운데 코드에서 세어 볼 수 있는 것을 한 번에 본다. 실패 0 이어야 한다.

    python Tools/rules_check.py

규칙을 바꾸거나 예외를 늘리면 CLAUDE.md 의 그 문단도 같이 고친다. 예외에는 까닭을 적는다.
문법을 읽지 않고 글자로 재는 검사라, 새 코드가 모양을 바꿔 빠져나갈 수는 있다 — 그래도 같은 사고가
같은 모양으로 다시 나는 것은 막는다.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(ROOT, "Assets", "@Scripts")
GROUPS = os.path.join(ROOT, "Assets", "AddressableAssetsData", "AssetGroups")

errors = []


def fail(rel, line, msg):
    errors.append(f"{rel}:{line}: {msg}")


def read(path):
    with open(path, encoding="utf-8-sig") as f:
        return f.read()


# ---------------------------------------------------------------- C# 읽기

def strip_comments(text):
    """주석을 공백으로 지운다(줄 수는 그대로). 문자열은 남긴다 — 리소스 키를 읽어야 한다."""
    out, i, n = [], 0, len(text)
    while i < n:
        c, nxt = text[i], text[i + 1:i + 2]
        if c == "/" and nxt == "/":
            j = text.find("\n", i)
            j = n if j < 0 else j
            out.append(" " * (j - i))
            i = j
        elif c == "/" and nxt == "*":
            j = text.find("*/", i + 2)
            j = n if j < 0 else j + 2
            out.append(re.sub(r"[^\n]", " ", text[i:j]))
            i = j
        elif c == '"' or (c in "@$" and re.match(r"[@$]{1,2}\"", text[i:i + 3])):
            start, verbatim = i, False
            while text[i] in "@$":
                verbatim |= text[i] == "@"
                i += 1
            i += 1
            while i < n:
                ch = text[i]
                if verbatim and ch == '"':
                    if text[i + 1:i + 2] == '"':
                        i += 2
                        continue
                    i += 1
                    break
                if not verbatim and ch == "\\":
                    i += 2
                    continue
                if not verbatim and ch in '"\n':
                    i += ch == '"'
                    break
                i += 1
            out.append(text[start:i])
        elif c == "'":
            m = re.match(r"'(\\.|[^'\\])'", text[i:i + 4])
            span = m.end() if m else 1
            out.append(text[i:i + span])
            i += span
        else:
            out.append(c)
            i += 1
    return "".join(out)


def guards(lines):
    """줄마다 (개발 빌드까지인가, 에디터 전용인가). `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 는 앞의 것만.
    `||` 로 묶인 항이 하나라도 출시 빌드에서 참일 수 있으면(UNITY_STANDALONE 등) 개발 전용이 아니다."""
    stack, out = [], []

    def kind(cond):
        cond = cond.replace(" ", "").replace("(", "").replace(")", "")
        terms = [t.split("&&") for t in cond.split("||")]
        dev = bool(cond) and all(any(c in ("UNITY_EDITOR", "DEVELOPMENT_BUILD") for c in t) for t in terms)
        editor = bool(cond) and all("UNITY_EDITOR" in t for t in terms)
        return dev, editor

    for line in lines:
        m = re.match(r"\s*#\s*(if|elif|else|endif)\b(.*)", line)
        if m:
            kw, cond = m.groups()
            if kw == "if":
                stack.append(kind(cond) + (cond.replace(" ", ""),))
            elif stack and kw == "elif":
                stack[-1] = kind(cond) + (cond.replace(" ", ""),)
            elif stack and kw == "else":
                editor = stack[-1][2] == "!UNITY_EDITOR"      # #if !UNITY_EDITOR 의 #else 는 에디터 전용
                stack[-1] = (editor, editor, "")
            elif stack:
                stack.pop()
        out.append((any(s[0] for s in stack), any(s[1] for s in stack)))
    return out


def sources():
    """(상대 경로, 에디터 코드인가, 주석 뺀 전문, 줄, 줄마다 가드)."""
    for dirpath, _, files in os.walk(SCRIPTS):
        for fn in sorted(files):
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(dirpath, fn)
            rel = os.path.relpath(path, ROOT).replace("\\", "/")
            editor = "/Editor/" in "/" + os.path.relpath(path, SCRIPTS).replace("\\", "/")
            code = strip_comments(read(path))
            lines = code.split("\n")
            yield rel, editor, code, lines, guards(lines)


def line_of(code, pos):
    return code.count("\n", 0, pos) + 1


# ---------------------------------------------------------------- 규칙

KEY_READ = re.compile(r"\bInput\.(GetKey|GetKeyDown|GetKeyUp|GetButton|GetButtonDown|GetButtonUp|GetAxis|GetAxisRaw"
                      r"|anyKey|anyKeyDown)\b(?:\s*\(\s*\"([^\"]*)\")?")


def check_keys(rel, lines, guard):
    """키는 GameKeys 로 읽는다 — 그래야 패드가 같이 돈다. 마우스 축과 개발용 치트 키(#if)만 예외.
    ESC 를 누르고 떼는 것은 씬 UI 만 받는다(누르고 있는 동안 감기 — 크레딧 — 는 창을 닫지 않아 괜찮다)."""
    raw_ok = rel.endswith("/GameKeys.cs")
    for no, (line, (dev, _)) in enumerate(zip(lines, guard), 1):
        for m in KEY_READ.finditer(line):
            if raw_ok or (m.group(2) or "").startswith("Mouse") or dev:
                continue
            fail(rel, no, f"Input.{m.group(1)} 를 직접 읽는다 — GameKeys 로")
        if re.search(r"KeyCode\.(LeftShift|RightShift)|\"(left|right) shift\"", line):
            fail(rel, no, "Shift 를 키로 쓴다 — Shift+Tab 은 Steam 오버레이다(가속은 Space)")
        if re.search(r"GameKeys\.(Down|Up)\(\s*KeyCode\.Escape\s*\)", line) and not re.search(
                r"/UI_(GameScene|TitleScene|IntroScene)\.cs$", rel):
            fail(rel, no, "ESC 를 여기서 받는다 — 씬 UI 한 곳이 맨 위 팝업의 OnEscape() 에 묻는다")


def check_editor_api(rel, lines, guard):
    """런타임 코드의 UnityEditor 는 #if UNITY_EDITOR 안에만 — 빌드가 깨지고 Safe Mode 로 떨어진다."""
    for no, (line, (_, editor_only)) in enumerate(zip(lines, guard), 1):
        if re.search(r"\bUnityEditor\b", line) and not editor_only:
            fail(rel, no, "런타임 코드에 가드 없는 UnityEditor")


# 커서는 PreLoad 를 다 읽기 전, 타이틀 로딩부터 보여야 한다. 바닥 타일은 층마다 한 장을 SpriteRenderer 로 깐다.
RESOURCES_OK = {
    "Assets/@Scripts/Managers/Core/CursorManager.cs",
    "Assets/@Scripts/Managers/Contents/MapBuilder.cs",
}


def resources_paths():
    """Assets 아래 모든 Resources 폴더의 (확장자 뺀) 상대 경로 — Resources.Load 가 받는 이름."""
    found = set()
    for dirpath, _, files in os.walk(os.path.join(ROOT, "Assets")):
        parts = dirpath.replace("\\", "/").split("/")
        if "Resources" not in parts:
            continue
        sub = "/".join(parts[len(parts) - parts[::-1].index("Resources"):])
        for fn in files:
            if not fn.endswith(".meta"):
                found.add((sub + "/" if sub else "") + os.path.splitext(fn)[0])
    return found


def check_loading(rel, code, res_paths):
    """리소스는 Managers.Resource 로만 읽는다. 예외 파일의 Resources.Load 는 그 파일이 실제로 있어야 한다."""
    if rel.endswith("/ResourceManager.cs"):
        return
    for m in re.finditer(r"\bAddressables\.", code):
        fail(rel, line_of(code, m.start()), "Addressables 를 직접 부른다 — Managers.Resource 로")
    for m in re.finditer(r"\bResources\.Load\w*(?:<\w+>)?\s*\(\s*(\"[^\"]*\")?", code):
        no = line_of(code, m.start())
        if rel not in RESOURCES_OK:
            fail(rel, no, "Resources.Load 를 직접 부른다 — Managers.Resource 로")
        elif not m.group(1):
            fail(rel, no, "Resources.Load 의 경로가 글자 그대로가 아니다 — 있는지 잴 수 없다")
        elif m.group(1).strip('"') not in res_paths:
            fail(rel, no, f"Resources 에 없는 경로 {m.group(1)}")


def preload_keys():
    """어드레서블 그룹 전부에서 PreLoad 라벨이 붙은 주소 — 타이틀이 선로드해 Managers.Resource 가 아는 키."""
    keys = set()
    for fn in os.listdir(GROUPS):
        if fn.endswith(".asset"):
            for m in re.finditer(r"m_Address: (.+?)\s*\n\s*m_ReadOnly: \d+\s*\n\s*m_SerializedLabels:\s*\n((?:\s*- \S+\s*\n)*)",
                                 read(os.path.join(GROUPS, fn))):
                if re.search(r"- PreLoad\b", m.group(2)):
                    keys.add(m.group(1).strip())
    return keys


INSTANTIATE = re.compile(r"Managers\.Resource\.Instantiate\(\s*")
NULL_CHECK = r"(==|!=)\s*null|\bis\s+(not\s+)?null|\?\.|\?\?"
# UI 창은 타입 이름이 키다 — 부르는 쪽의 <T> 를 PreLoad 와 대조한다(아래). 없는 창은 null 로 넘겨도 부른 쪽이 죽는다.
INSTANTIATE_OK = {"Assets/@Scripts/Managers/Core/UIManager.cs"}


def scan_call(code, i):
    """여는 괄호 바로 뒤 i 부터 맞는 닫는 괄호까지 — (최상위 인자들, 닫는 괄호 자리)."""
    depth, start, args = 0, i, []
    while i < len(code):
        c = code[i]
        if c in "([{":                  # 꺾쇠는 비교·람다와 못 가른다 — 인자 안의 제네릭 쉼표는 드물어 안 센다
            depth += 1
        elif c in ")]}":
            if depth == 0:
                break
            depth -= 1
        elif c == "," and depth == 0:
            args.append(code[start:i].strip())
            start = i + 1
        elif c == '"':
            i = code.find('"', i + 1)
            if i < 0:
                i = len(code)
                break
        i += 1
    last = code[start:i].strip()
    return (args + [last] if last else args), i


def call_args(code, i):
    return scan_call(code, i)[0]


def check_resource_keys(rel, code, keys):
    """글자 그대로 쓴 키는 PreLoad 에 있어야 한다. 데이터에서 온 키로 만든 것은 쓰기 전에 null 을 본다.

    Instantiate 는 없는 키에 null 을 준다. 그대로 .transform 을 만지면 부른 쪽이 통째로 죽는다 — 무기 FX 는 그 전투의
    공격 코루틴을, 룬 FX 는 줍기를 죽였다. 글자 그대로 쓴 키(문 자물쇠 "DoorLock" 등)는 PreLoad 에 있는지만 잰다 —
    있으면 null 이 오지 않는다. 글자로 재는 검사라 흐름은 못 본다: null 을 보고도 그냥 지나가는 줄은 잡지 못한다.
    """
    for m in re.finditer(r"Managers\.Resource\.(Instantiate|Load<(\w+)>)\(\s*\$?\"([^\"{}]*)\"", code):
        key, kind = m.group(3), m.group(2)
        ok = key in keys or (kind == "Sprite" and key + ".sprite" in keys)
        if not ok:
            fail(rel, line_of(code, m.start()), f"PreLoad 어드레서블에 없는 키 \"{key}\"")
    # UI 프리팹 이름은 따로 주지 않으면 타입 이름이다. Show*UI 는 첫 인자가, Make* 는 둘째 인자가 이름이다.
    for m in re.finditer(r"Managers\.UI\.(ShowPopupUI|ShowSceneUI|MakeSubItem|MakeWorldSpaceUI)<(\w+)>\(", code):
        args = call_args(code, m.end())
        named = any(a.startswith("name:") for a in args) or len(args) > (0 if m.group(1).startswith("Show") else 1)
        if not named and m.group(2) not in keys:
            fail(rel, line_of(code, m.start()), f"UI 프리팹 \"{m.group(2)}\" 가 PreLoad 어드레서블에 없다")

    if rel in INSTANTIATE_OK:
        return
    for m in INSTANTIATE.finditer(code):
        if re.match(r"\$?\"[^\"{}]*\"", code[m.end():]):
            continue                                            # 위에서 키가 있는지 쟀다
        _, close = scan_call(code, m.end())
        if re.match(r"\s*\.", code[close + 1:]):               # Instantiate(키).GetComponent… — 같은 문장에서 바로 쓴다
            fail(rel, line_of(code, m.start()), "Instantiate 결과를 null 확인 없이 바로 쓴다")
            continue
        start = max(code.rfind(";", 0, m.start()), code.rfind("{", 0, m.start()), code.rfind("}", 0, m.start())) + 1
        a = re.search(r"\b(\w+)\s*=(?!=)", code[start:m.start()])
        if a is None:
            continue                                            # 돌려주거나 넘긴다 — 받는 쪽이 본다
        end = code.find(";", close)
        var = a.group(1)
        use = re.search(r"\b" + re.escape(var) + r"\b[^;\n]*", code[end + 1:])
        if use and not re.search(r"^" + re.escape(var) + r"\s*(" + NULL_CHECK + r")", use.group(0)) \
                and not re.search(r"(if|return|while)\s*\(?[^;]*\b" + re.escape(var) + r"\b\s*(" + NULL_CHECK + ")",
                                  code[end + 1:end + 1 + use.end()]):
            fail(rel, line_of(code, m.start()), f"Instantiate 결과 {var} 를 null 확인 없이 쓴다(키가 데이터에서 온다)")


STATIC_FIELD = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|internal|protected|new)\s+)*static\s+"
                          r"(?!class\b|readonly\b|void\b|extern\b|partial\b)(?:event\s+)?[\w<>\[\],\.\? ]+?\s+(\w+)\s*"
                          r"(?:=(?!>)[^;]*)?;", re.M)
STATIC_PROP = re.compile(r"^\s*(?:(?:public|private|internal|protected)\s+)*static\s+[\w<>\[\],\.\? ]+?\s+(\w+)\s*"
                         r"\{\s*(?:(?:public|private|internal|protected)\s+)?get;\s*(?:(?:private|internal|protected)\s+)?set;",
                         re.M)
STATIC_TABLE = re.compile(r"^\s*(?:(?:public|private|internal|protected)\s+)*static\s+readonly\s+"
                          r"(?:List|Dictionary|HashSet|Queue|Stack)<[^;=]*>\s+(\w+)", re.M)

# 플레이 사이에 남아도 되는 정적 값 — 까닭과 함께.
STATIC_OK = {
    # 지난 플레이의 것은 부서진 오브젝트라 == null 이 참이고, Show 가 새로 만든다.
    "Assets/@Scripts/UI/Popup/UI_StoryBark.cs",
    # 상수 표다. 값을 바꾸는 줄이 없는지는 check_define_constants 가 잰다.
    "Assets/@Scripts/Utils/Define.cs",
}
STATIC_KEEP = {
    ("MapBuilder.cs", "_tintBlock"): "속성 블록 하나를 돌려 쓴다 — 쓸 때마다 GetPropertyBlock 이 덮는다",
    ("StoryDirector.cs", "s_epoch"): "늘기만 하는 세대 번호 — 같은지만 본다",
    ("GameKeys.cs", "_hasStick"): "_probed 를 비우면 다음 Poll 이 다시 묻는다",
    ("GameKeys.cs", "_hasDPad"): "_probed 를 비우면 다음 Poll 이 다시 묻는다",
    ("GameKeys.cs", "_hasRT"): "_probed 를 비우면 다음 Poll 이 다시 묻는다",
    ("GameKeys.cs", "_hasZoom"): "_probed 를 비우면 다음 Poll 이 다시 묻는다",
    ("GameKeys.cs", "_hasMouse"): "_probed 를 비우면 다음 Poll 이 다시 묻는다",
    ("GameKeys.cs", "_usingPad"): "마지막에 쓴 장치 — 다음 입력이 다시 정한다",
    ("StoryUI.cs", "s_hintLang"): "s_hintText 를 비우면 안내를 새로 만들며 다시 쓴다",
    ("StoryUI.cs", "s_prose"): "글꼴 에셋 캐시 — 에셋은 플레이 사이에도 산다(!= null 로 다시 찾는다)",
    ("StoryUI.cs", "s_pixel"): "글꼴 에셋 캐시 — 에셋은 플레이 사이에도 산다(!= null 로 다시 찾는다)",
}
RESET_METHOD = re.compile(r"\[RuntimeInitializeOnLoadMethod[^\]]*\]\s*(?:(?:public|private|internal|static)\s+)*\w+\s+\w+\s*\(\s*\)\s*(=>|\{)")


def static_names(code):
    names = []
    for m in STATIC_FIELD.finditer(code):
        decl = m.group(0)
        if "=" in decl or "<" in decl or "," not in decl:
            names.append(m.group(1))
        else:                                                   # static bool a, b, c;
            names += [t.split()[-1] for t in decl.rstrip(";").split(",")]
    names += [m.group(1) for m in STATIC_PROP.finditer(code)]
    names += [n for n in (m.group(1) for m in STATIC_TABLE.finditer(code))
              if re.search(r"\b" + n + r"\s*(\.(Add|Clear|Remove|Enqueue|Push|TryAdd)\w*\(|\[[^\]]+\]\s*=(?!=))", code)]
    return names


def reset_bodies(code):
    """RuntimeInitializeOnLoadMethod 가 붙은 메서드 몸통(플레이마다 도는 것 — 리셋이든 다시 세우기든)."""
    out = []
    for m in RESET_METHOD.finditer(code):
        if m.group(1) == "=>":
            out.append(code[m.end():code.find(";", m.end())])
        else:
            out.append(code[m.end():scan_call(code, m.end())[1]])
    return "\n".join(out)


def check_statics(rel, code):
    """도메인 리로드를 끈 에디터에서 정적 값은 지난 플레이에서 넘어온다 — SubsystemRegistration 에서 비운다.
    필드마다 잰다: 리셋 메서드가 있는 파일에 새 정적 값을 더하고 안 비우는 것이 가장 흔한 실수다.
    (CoroutineManager 처럼 플레이마다 다시 세우는 RuntimeInitializeOnLoadMethod 도 받는다.)"""
    if rel in STATIC_OK:
        return
    base, body = rel.rsplit("/", 1)[-1], reset_bodies(code)
    left = [n for n in dict.fromkeys(static_names(code))
            if (base, n) not in STATIC_KEEP and not re.search(r"\b" + re.escape(n) + r"\b", body)]
    if left:
        fail(rel, 1, f"정적 상태({', '.join(left)})를 SubsystemRegistration 에서 비우지 않는다 — 남아도 되면 STATIC_KEEP 에 까닭과 함께")


def check_define_constants(rel, code):
    """Define 의 public static 은 상수로 쓴다 — 바꾸면 플레이 사이에 남는다(위 STATIC_OK)."""
    for m in re.finditer(r"\bDefine\.(\w+)\s*(=(?!=)|\+=|-=|\+\+|--)", code):
        fail(rel, line_of(code, m.start()), f"Define.{m.group(1)} 의 값을 바꾼다")


def floors(n):
    return {f"00_{i:03d}" for i in range(n)}


def check_hand_authored():
    """손수 만든 1~4층 목록은 세 곳(+ 두 도구)에 있고 전부 같아야 한다 — 하나만 늘면 인트로가 깨진다."""
    lists = {}
    for rel in ("Tools/generate_content.py", "Tools/mapdata_gen.py"):
        m = re.search(r"^HANDMADE_FLOORS\s*=\s*(\d+)", read(os.path.join(ROOT, rel)), re.M)
        lists[rel] = floors(int(m.group(1))) if m else None
    for rel, pat in (("Tools/validate_content.py", r"^HAND_AUTHORED\s*=\s*[\{\(]([^\}\)]*)"),
                     ("Tools/layout_gen.py", r"^HAND_AUTHORED\s*=\s*[\{\(]([^\}\)]*)"),
                     ("Assets/@Scripts/Managers/Contents/MapBuilder.cs", r"HandAuthored\s*=\s*new HashSet<string>\s*\{([^}]*)")):
        m = re.search(pat, read(os.path.join(ROOT, rel)), re.M)
        lists[rel] = set(re.findall(r"\"(\d\d_\d{3})\"", m.group(1))) if m else None
    want = lists["Assets/@Scripts/Managers/Contents/MapBuilder.cs"]
    for rel, got in lists.items():
        if not got or got != want:
            fail(rel, 1, f"손수 만든 층 목록 {sorted(got or [])} 이 MapBuilder.HandAuthored {sorted(want or [])} 와 다르다")


# ---------------------------------------------------------------- 자기 검사

def _self_test():
    code = strip_comments('a = "http://x"; // b\n/* c\n d */ e = \'"\'; f = @"g "" // h";\n')
    assert '"http://x"' in code and "b" not in code.split("\n")[0][16:], code
    assert code.count("\n") == 3 and "c" not in code and " d " not in code and "// h" in code, code
    g = guards(["#if UNITY_EDITOR || DEVELOPMENT_BUILD", "x", "#else", "y", "#endif", "#if UNITY_EDITOR", "z", "#endif"])
    assert [t for t in g] == [(True, False), (True, False), (False, False), (False, False), (False, False),
                              (True, True), (True, True), (False, False)], g
    errors_before = len(errors)
    check_resource_keys("t.cs", 'GameObject go = Managers.Resource.Instantiate(key, p);\ngo.transform.x = 1;', set())
    check_resource_keys("t.cs", 'GameObject go = Managers.Resource.Instantiate(key, p);\nif (go == null) return;', set())
    check_resource_keys("t.cs", 'var go = Managers.Resource.Instantiate("A");', {"A"})
    assert len(errors) == errors_before + 1, errors[errors_before:]
    del errors[errors_before:]


def main():
    _self_test()
    keys = preload_keys()
    res_paths = resources_paths()
    count = 0
    for rel, editor, code, lines, guard in sources():
        count += 1
        if editor:
            continue
        check_keys(rel, lines, guard)
        check_editor_api(rel, lines, guard)
        check_loading(rel, code, res_paths)
        check_resource_keys(rel, code, keys)
        check_statics(rel, code)
        check_define_constants(rel, code)
    check_hand_authored()

    print(f"rules_check: C# {count}개, PreLoad 주소 {len(keys)}개")
    for e in errors:
        print("  " + e)
    print(f"위반 {len(errors)}건" + (" — 통과" if not errors else ""))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
