"""Compile-check the game's C# without the Unity editor.

Unity only regenerates Assembly-CSharp.csproj when the editor refreshes, so a
script added from outside is missing from it and `dotnet build` would skip it.
This rewrites the two Unity projects into throwaway copies whose Compile list is
whatever is on disk right now, references the already-built package DLLs in
Library/ScriptAssemblies instead of their projects, and builds them.

    python Tools/cscheck.py            # errors only, exit 1 if any
    python Tools/cscheck.py --warn     # also warnings from our own scripts
    python Tools/cscheck.py --tag lane # own csproj copies and obj folder, so parallel runs don't collide

Needs the editor to have compiled once (Library/ScriptAssemblies) and the
generated Assembly-CSharp*.csproj files to exist. The _check_*.csproj copies are
gitignored like every other .csproj.
"""
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TAG = next((a.split("=", 1)[1] for a in sys.argv if a.startswith("--tag=")), "")
if "--tag" in sys.argv and sys.argv.index("--tag") + 1 < len(sys.argv):
    TAG = sys.argv[sys.argv.index("--tag") + 1]
SUFFIX = f"_{TAG}" if TAG else ""
OUT = f"obj\\cscheck{SUFFIX}"
OUR = "Assets\\@Scripts\\"


def _on_disk(editor):
    base = os.path.join(ROOT, "Assets", "@Scripts")
    found = []
    for d, _, files in os.walk(base):
        parts = os.path.relpath(d, base).replace("/", "\\").split("\\")
        if ("Editor" in parts) != editor:
            continue
        found += [os.path.relpath(os.path.join(d, f), ROOT).replace("/", "\\")
                  for f in files if f.endswith(".cs")]
    return sorted(found)


def _rewrite(src, dst, editor, runtime_dll=None):
    text = open(os.path.join(ROOT, src), encoding="utf-8-sig").read()
    # our scripts: take the disk, not the list Unity wrote at its last refresh
    text = re.sub(r'\s*<Compile Include="Assets\\@Scripts\\[^"]*"\s*/>', "", text)
    items = "".join(f'\n    <Compile Include="{p}" />' for p in _on_disk(editor))
    text = text.replace("<ItemGroup>", "<ItemGroup>" + items, 1)

    def ref(m):  # project references -> the DLLs the editor already built
        name = m.group(1)
        path = runtime_dll if (name == "Assembly-CSharp" and runtime_dll) \
            else f"Library\\ScriptAssemblies\\{name}.dll"
        return f'<Reference Include="{name}"><HintPath>{path}</HintPath></Reference>'

    text = re.sub(r'<ProjectReference Include="([^"]+)\.csproj">.*?</ProjectReference>', ref, text, flags=re.S)
    tag = os.path.splitext(os.path.basename(dst))[0]
    text = re.sub(r"<OutputPath>[^<]*</OutputPath>",
                  lambda m: f"<OutputPath>{OUT}\\bin\\{tag}\\</OutputPath>", text)
    text = text.replace("<BaseDirectory>.</BaseDirectory>",
                        f"<BaseDirectory>.</BaseDirectory>"
                        f"<BaseIntermediateOutputPath>{OUT}\\obj\\{tag}\\</BaseIntermediateOutputPath>")
    # the IDE analyzer only adds noise here
    text = re.sub(r'\s*<Analyzer Include="[^"]*Microsoft\.Unity\.Analyzers\.dll"\s*/>', "", text)
    open(os.path.join(ROOT, dst), "w", encoding="utf-8").write(text)
    return os.path.join(OUT, "bin", tag, "Assembly-CSharp.dll")


def _build(proj, warn):
    r = subprocess.run(["dotnet", "build", proj, "-nologo", "-v", "q", "-clp:NoSummary"],
                       cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    seen = []
    for line in (r.stdout + r.stderr).splitlines():
        if ": error " in line or (warn and ": warning " in line and OUR in line):
            key = re.sub(r"\s*\[[^\]]*\]$", "", line.strip())  # msbuild repeats each diagnostic
            if key not in seen:
                seen.append(key)
    return r.returncode, seen


def main():
    warn = "--warn" in sys.argv
    runtime, editor = f"_check_runtime{SUFFIX}.csproj", f"_check_editor{SUFFIX}.csproj"
    dll = _rewrite("Assembly-CSharp.csproj", runtime, editor=False)
    code, out = _build(runtime, warn)
    if code == 0:
        _rewrite("Assembly-CSharp-Editor.csproj", editor, editor=True, runtime_dll=dll)
        code, more = _build(editor, warn)
        out += more
    else:
        out.append("(editor scripts not checked: runtime assembly failed)")
    for line in out:
        print(line)
    print(f"cscheck: {sum(': error ' in l for l in out)} error(s)")
    return 1 if code else 0


if __name__ == "__main__":
    sys.exit(main())
