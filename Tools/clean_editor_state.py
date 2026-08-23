"""에디터를 켜기 전/끈 뒤에 남은 찌꺼기를 지운다.

왜 필요한가
-----------
Unity 는 도는 동안 열린 씬의 백업을 Temp/__Backupscenes 에 쓴다. 정상 종료하면
스스로 지우지만, 강제 종료되면 그대로 남는다. 다음에 켤 때 그 파일을 보고

    "Recovering Scene Backups — 복구본을 Assets/_Recovery 에 보관할까요?"

를 묻는다. Yes 를 누를 때마다 Assets/_Recovery 에 복사본이 하나씩 쌓인다.

즉 이 창은 고장이 아니라 <b>지난번에 에디터가 제대로 안 닫혔다는 표시</b>다.
자동 검증 실행이 시간 제한에 걸려 죽는 일이 잦아서 계속 뜬다.

    python Tools/clean_editor_state.py

에디터가 떠 있을 때는 아무것도 지우지 않는다 — 도는 중인 백업을 지우면
그 세션이 진짜로 복구 불가능해진다.
"""
import os
import shutil
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def editor_running():
    """이 프로젝트를 열고 있는 에디터가 있는지. (unity mcp 서버는 에디터가 아니다)"""
    try:
        out = subprocess.run(
            ["powershell", "-NoProfile", "-Command",
             "@(Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" | "
             "Where-Object { $_.CommandLine -like '*-projectPath*' }).Count"],
            capture_output=True, text=True, timeout=60)
        return out.stdout.strip() not in ("0", "")
    except Exception:
        return True   # 확인 못 하면 지우지 않는다


def main():
    if editor_running():
        print("에디터가 실행 중이다. 아무것도 지우지 않는다.")
        return 1

    removed = []

    backups = os.path.join(ROOT, "Temp", "__Backupscenes")
    if os.path.isdir(backups):
        shutil.rmtree(backups, ignore_errors=True)
        removed.append("Temp/__Backupscenes  (이게 있으면 복구 창이 뜬다)")

    lock = os.path.join(ROOT, "Temp", "UnityLockfile")
    if os.path.isfile(lock):
        try:
            os.remove(lock)
            removed.append("Temp/UnityLockfile  (비정상 종료 흔적)")
        except OSError:
            pass

    # 이미 쌓인 복구본. 놀이에도 빌드에도 쓰이지 않는다.
    recovery = os.path.join(ROOT, "Assets", "_Recovery")
    if os.path.isdir(recovery):
        count = len([f for f in os.listdir(recovery) if f.endswith(".unity")])
        if count:
            shutil.rmtree(recovery, ignore_errors=True)
            meta = recovery + ".meta"
            if os.path.isfile(meta):
                os.remove(meta)
            removed.append(f"Assets/_Recovery  (쌓인 복구본 {count}개)")

    if removed:
        print("지웠다:")
        for r in removed:
            print("  -", r)
    else:
        print("지울 것이 없다. 깨끗하다.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
