"""체험판 플레이테스트 zip 을 묶는다 — Build/WindowsDemo + 테스터 안내(문서 2절 그대로).

  python Tools/playtest_zip.py            # Build/Playtest/TheSword_Demo_<오늘>.zip
  python Tools/playtest_zip.py --check Build/Playtest/TheSword_Demo_20261127.zip   # 그 zip 의 안내가 지금 문서와 같은가

안내 글은 Documents/TheSword_플레이테스트_안내.md 의 "## 2." 절 ``` 블록이 원본이다 — zip 에 손으로 쓰지 않는다
(11-27 손으로 묶을 때 "아래 설문" 의 물음이 zip 에 빠져 나갔다). 메모장이 읽게 BOM + CRLF.
Build/ 는 커밋하지 않는다(공개 저장소) — 이 도구만 커밋한다.
"""
import datetime
import os
import re
import sys
import zipfile

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
DOC = os.path.join(ROOT, "Documents", "TheSword_플레이테스트_안내.md")
SRC = os.path.join(ROOT, "Build", "WindowsDemo")
TOP = "TheSword_Demo"
GUIDE = f"{TOP}/테스터_안내.txt"


def guide_bytes():
    doc = open(DOC, encoding="utf-8").read()
    m = re.search(r"^## 2\..*?```\n(.*?)```", doc, re.S | re.M)
    assert m, "문서에 '## 2.' 절의 ``` 블록이 없다"
    body = m.group(1).strip()
    assert "설문" not in body or re.search(r"^\s*1\. ", body, re.M), "안내가 설문을 말하는데 물음이 없다"
    return ("﻿" + body.replace("\n", "\r\n") + "\r\n").encode("utf-8")


def build(out):
    exe = os.path.join(SRC, "TheSword.exe")
    assert os.path.exists(exe), f"{exe} 가 없다 — 에디터를 닫고 GameBuild.Prepare → GameBuild.WindowsDemo 부터"
    aa = os.path.join(SRC, "TheSword_Data", "StreamingAssets", "aa")
    assert os.path.isdir(aa), "어드레서블 번들이 없다 — 타이틀에서 못 나간다(CLAUDE.md 빌드 절)"
    os.makedirs(os.path.dirname(out), exist_ok=True)
    tmp = out + ".tmp"
    n = 0
    with zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        z.writestr(GUIDE, guide_bytes())
        for dirpath, _, files in os.walk(SRC):
            for name in sorted(files):
                path = os.path.join(dirpath, name)
                z.write(path, f"{TOP}/" + os.path.relpath(path, SRC).replace(os.sep, "/"))
                n += 1
    with zipfile.ZipFile(tmp) as z:
        assert z.testzip() is None
    os.replace(tmp, out)
    print(f"{out} — 파일 {n} + 안내, {os.path.getsize(out) / 2**20:.0f}MB")


def check(path):
    with zipfile.ZipFile(path) as z:
        got = z.read(GUIDE)
        assert z.testzip() is None, "zip 이 깨졌다"
        assert f"{TOP}/TheSword.exe" in z.namelist(), "실행 파일이 없다"
    if got != guide_bytes():
        print(f"다르다: {path} 의 안내가 지금 문서와 다르다 — 다시 묶는다")
        return 1
    print(f"같다: {path}")
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--check":
        sys.exit(check(sys.argv[2]))
    build(os.path.join(ROOT, "Build", "Playtest", f"TheSword_Demo_{datetime.date.today():%Y%m%d}.zip"))
