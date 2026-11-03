"""게임 문자열의 글자가 픽셀 글꼴(Silver)에 다 있는지 본다.

    python Tools/glyph_check.py

없는 글자는 FontFallback 이 Windows 글꼴(맑은 고딕·Yu Gothic·YaHei)로 그린다. Steam Deck(Proton)에는 그 글꼴이
없어서 □ 로 나온다 — 그래서 대본·도감·UI 문구를 고쳐 Silver 에 있는 글자로 쓴다(2026-11-03: 叼咂唧拄槛洇洼镶鹫).
Silver 에 없으면 1 로 끝난다. Pillow 가 필요하다.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
FONT = ROOT / "Assets/@Resources/Font/Silver.ttf"
SCRIPTS = ROOT / "Assets/@Resources/Data/JsonData/ScriptData.json"


def main():
    font = ImageFont.truetype(str(FONT), 19)

    def bitmap(ch):
        im = Image.new("L", (24, 24), 0)
        ImageDraw.Draw(im).text((2, 0), ch, font=font, fill=255)
        return im.tobytes()

    blank = bitmap("￿")
    data = json.loads(SCRIPTS.read_text(encoding="utf-8"))
    rows = data[next(iter(data))] if isinstance(data, dict) else data
    bad = 0
    for col in ("ScriptKr", "ScriptEn", "ScriptJp", "ScriptCn"):
        where = {}
        for row in rows:
            for ch in row.get(col) or "":
                if ord(ch) > 0x7F and not ch.isspace():
                    where.setdefault(ch, row["id"])
        missing = sorted(ch for ch in where if bitmap(ch) == blank)
        print(f"{col}: 글자 {len(where)}, Silver 에 없음 {len(missing)}"
              + ("" if not missing else " — " + " ".join(f"{ch}(id {where[ch]})" for ch in missing)))
        bad += len(missing)
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
