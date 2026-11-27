"""완주 녹화에서 이야기 구간만 모은 영상을 만들고, 영상 설명에 넣을 챕터 표를 뽑는다.

  python Tools/story_cut.py Recordings/X.mp4 --log Recordings/X.log --list        # 구간과 합친 길이만
  python Tools/story_cut.py Recordings/X.mp4 --log Recordings/X.log               # Recordings/X_story.mp4
  python Tools/story_cut.py Recordings/X.mp4 --log Recordings/X.log --chapters    # 유튜브 챕터 줄(완주 영상용)

녹화는 `THESWORD_RECORD_READ=1 … -executeMethod PlaythroughRecorder.Record` 로 한다 — 대사가 읽힐 속도로 돌고
(StoryUI.ReadPace) 에디터 로그에 이런 줄이 남는다. 시각은 영상 시각(녹화 프레임 / fps)이다.

  [Playthrough] 이야기 시작 t=22.97 scene 1층        (intro · scene · bark · credits · ending)
  [Playthrough] 이야기 끝 t=26.27 scene
  [Playthrough] 층 t=95.20 2층

에디터를 다시 켜면 Editor.log 가 지워지니 녹화가 끝나자마자 복사해 둔다. 걸으며 한마디(bark)는 걷는 화면 위에 뜨는
한두 줄이라 기본으로는 뺀다(--barks 로 넣는다). ffmpeg 는 PATH 에 없으면 imageio_ffmpeg 것을 쓴다.
Recordings/ 는 커밋하지 않는다 — 이 도구만 커밋한다.
"""
import argparse
import os
import re
import subprocess
import sys
import tempfile

START = re.compile(r"\[Playthrough\] 이야기 시작 t=([\d.]+) (\w+) (\d+)층")
END = re.compile(r"\[Playthrough\] 이야기 끝 t=([\d.]+) (\w+)")
FLOOR = re.compile(r"\[Playthrough\] 층 t=([\d.]+) (\d+)층")
PAD = 0.7          # 구간 앞뒤로 더 담는 초 — 대화창이 뜨고 걷히는 모습까지
JOIN = 2.5         # 이보다 가까운 구간은 하나로 잇는다(끊겼다 이어지는 장면이 깜빡이지 않게)
CHAPTER_NAMES = {1: "이끼 낀 지하 묘소", 21: "무너진 수로", 41: "잿빛 용광로", 61: "얼어붙은 심층", 81: "왕좌의 균열"}
BOSS_NAMES = {20: "종뿔사슴", 40: "수문을 진 소라", 60: "잿불풀무나방", 80: "빙성고래", 100: "검은 태양"}


def ffmpeg():
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError:
        return "ffmpeg"


def duration(path):
    out = subprocess.run([ffmpeg(), "-hide_banner", "-i", path], capture_output=True, text=True, encoding="utf-8", errors="replace").stderr
    m = re.search(r"Duration: (\d+):(\d+):([\d.]+)", out)
    if not m:
        sys.exit(f"{path} 의 길이를 못 읽었다")
    return int(m.group(1)) * 3600 + int(m.group(2)) * 60 + float(m.group(3))


def read_log(path):
    """[(kind, start, end, floor)], [(t, floor)] — 끝이 없는 구간은 end 가 None."""
    spans, floors, open_span = [], [], None
    for line in open(path, encoding="utf-8", errors="replace"):
        m = START.search(line)
        if m:
            if open_span:                   # 끝 줄 없이 다음이 시작했다 — 그 시각에 닫는다
                spans.append(open_span + (float(m.group(1)),))
            open_span = (m.group(2), float(m.group(1)), int(m.group(3)))
            continue
        m = END.search(line)
        if m and open_span:
            spans.append(open_span + (float(m.group(1)),))
            open_span = None
            continue
        m = FLOOR.search(line)
        if m:
            floors.append((float(m.group(1)), int(m.group(2))))
    if open_span:
        spans.append(open_span + (None,))
    return [(k, a, b, f) for (k, a, f, b) in spans], floors


def merge(spans, total, barks):
    keep = sorted((max(0.0, a - PAD), min(total, (b if b is not None else total) + PAD))
                  for k, a, b, _ in spans if barks or k != "bark")
    out = []
    for a, b in keep:
        if b - a < 0.2:
            continue
        if out and a - out[-1][1] <= JOIN:
            out[-1][1] = max(out[-1][1], b)
        else:
            out.append([a, b])
    return out


def stamp(t):
    t = int(t)
    return f"{t // 3600}:{t % 3600 // 60:02d}:{t % 60:02d}" if t >= 3600 else f"{t // 60}:{t % 60:02d}"


def chapters(floors, spans, total):
    """유튜브 챕터: 0:00 부터, 챕터 첫 층과 보스 층. 10초보다 가까운 것은 뒤의 것을 뺀다."""
    marks = [(0.0, "1층 — 프롤로그, 마검과의 계약")]
    seen = set()
    for t, floor in floors:
        if floor in seen:
            continue
        seen.add(floor)
        if floor in CHAPTER_NAMES and floor > 1:
            marks.append((t, f"{CHAPTER_NAMES[floor]} ({floor}층~)"))
        elif floor == 5:
            marks.append((t, f"{CHAPTER_NAMES[1]} (5층~)"))
        elif floor in BOSS_NAMES:
            marks.append((t, f"{floor}층 보스 — {BOSS_NAMES[floor]}"))
    for k, a, _, _ in spans:
        if k == "ending":
            marks.append((a, "결말"))
            break
    out = []
    for t, name in sorted(marks):
        if not out or t - out[-1][0] >= 10:
            out.append((t, name))
    return [f"{stamp(t)} {name}" for t, name in out if t < total]


def cut(video, keep, out):
    expr = "+".join(f"between(t,{a:.3f},{b:.3f})" for a, b in keep)
    graph = (f"[0:v]select='{expr}',setpts=N/FRAME_RATE/TB[v];"
             f"[0:a]aselect='{expr}',asetpts=N/SR/TB[a]")
    with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False, encoding="utf-8") as f:
        f.write(graph)
    try:
        r = subprocess.run([ffmpeg(), "-hide_banner", "-loglevel", "error", "-y", "-i", video, "-filter_complex_script", f.name,
                            "-map", "[v]", "-map", "[a]", "-c:v", "libx264", "-preset", "veryfast", "-crf", "22", "-pix_fmt", "yuv420p",
                            "-c:a", "aac", "-b:a", "128k", "-movflags", "+faststart", out])
    finally:
        os.unlink(f.name)
    if r.returncode != 0 or not os.path.exists(out):
        sys.exit("ffmpeg 가 실패했다")


def self_check():
    spans = [("intro", 0.0, 21.3, 1), ("scene", 23.0, 26.3, 1), ("bark", 40.0, 43.0, 2), ("scene", 100.0, None, 3)]
    assert merge(spans, 120.0, False) == [[0.0, 27.0], [99.3, 120.0]], merge(spans, 120.0, False)
    assert merge(spans, 120.0, True) == [[0.0, 27.0], [39.3, 43.7], [99.3, 120.0]]
    assert stamp(59) == "0:59" and stamp(3723) == "1:02:03"
    marks = chapters([(22.0, 1), (300.0, 5), (301.0, 5), (900.0, 20), (1000.0, 21)], [("ending", 2000.0, None, 100)], 2100.0)
    assert marks == ["0:00 1층 — 프롤로그, 마검과의 계약", "5:00 이끼 낀 지하 묘소 (5층~)", "15:00 20층 보스 — 종뿔사슴",
                     "16:40 무너진 수로 (21층~)", "33:20 결말"], marks
    print("story_cut 자기검사 통과")


if __name__ == "__main__":
    if len(sys.argv) == 1:
        self_check()
        sys.exit(0)
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("video")
    ap.add_argument("--log", required=True)
    ap.add_argument("--out")
    ap.add_argument("--barks", action="store_true", help="걸으며 한마디도 넣는다")
    ap.add_argument("--list", action="store_true", help="구간만 찍는다")
    ap.add_argument("--chapters", action="store_true", help="완주 영상의 유튜브 챕터 줄")
    a = ap.parse_args()
    total = duration(a.video)
    spans, floors = read_log(a.log)
    if a.chapters:
        print("\n".join(chapters(floors, spans, total)))
        sys.exit(0)
    keep = merge(spans, total, a.barks)
    if not keep:
        sys.exit("이야기 구간이 없다 — THESWORD_RECORD_READ=1 로 찍은 로그인가")
    length = sum(b - a_ for a_, b in keep)
    kinds = {}
    for k, s, e, _ in spans:
        kinds[k] = kinds.get(k, 0) + 1
    print(f"구간 {len(keep)}개, 합쳐서 {stamp(length)} (영상 {stamp(total)}) — " + " ".join(f"{k} {n}" for k, n in sorted(kinds.items())))
    if a.list:
        for s, e in keep:
            print(f"  {stamp(s)} ~ {stamp(e)}  ({e - s:.1f}초)")
        sys.exit(0)
    out = a.out or os.path.splitext(a.video)[0] + "_story.mp4"
    cut(a.video, keep, out)
    print(f"{out} — {os.path.getsize(out) / 2**20:.0f}MB")
