"""Save failures and lost-progress guards shown to the player (ids 150-179, core/save block).

C# constants: Define.SAVE_FAILED_PLAY / SAVE_DELETE_FAILED / VAULT_KEY_NEEDED. Same rules as battle.py: no newlines, ASCII commas or '^';
Korean and English stay inside the glyphs of both pixel fonts.
"""

TEXT = {
    # GameManager.EnterFloor: the checkpoint could not be written (disk full, read-only file). Shown once per session.
    150: ("저장하지 못했습니다. 디스크 공간과 저장 폴더를 확인해 주세요. 지금 끄면 마지막 저장 뒤의 진행이 사라집니다.",
          "Could not save. Check your free disk space and the save folder. Quitting now loses progress since the last save.",
          "セーブできませんでした。ディスクの空き容量とセーブフォルダを確認してください。今終了すると最後のセーブ以降の進行が失われます。",
          "无法保存。请检查磁盘剩余空间和存档文件夹。现在退出将丢失上次保存之后的进度。"),
    # UI_TitleScene new game: the old save could not be cleared (read-only or in use). The title stays usable.
    151: ("지난 저장을 지우지 못했습니다. 파일이 읽기 전용이거나 다른 프로그램이 쓰고 있습니다.",
          "Could not clear the old save. The files may be read-only or in use by another program.",
          "前のセーブを削除できませんでした。ファイルが読み取り専用か別のプログラムが使用中です。",
          "无法删除旧存档。文件可能为只读或正被其他程序占用。"),
    # KeyInventory.TryUseKey: a vault on a lower floor would eat a key the highest floor's main door still needs (that floor
    # could never be passed again and every checkpoint would keep the stuck state). {0} = that floor's number.
    152: ("이 열쇠를 쓰면 {0}층 문을 열 수 없습니다. 금고는 남는 열쇠로 여세요.",
          "Using this key would leave a door on floor {0} locked for good. Open vaults with spare keys.",
          "この鍵を使うと{0}階の扉が開けられなくなります。金庫は余った鍵で開けてください。",
          "用掉这把钥匙就打不开第{0}层的门了。金库请用多余的钥匙开。"),
}


if __name__ == "__main__":
    import re
    from pathlib import Path
    font_dir = Path(__file__).resolve().parents[2] / "Assets/@Resources/Font"

    def glyphs(name):
        text = (font_dir / f"{name}.asset").read_text(encoding="utf-8", errors="replace")
        table = text.split("  m_CharacterTable:", 1)[1].split("\n  m_", 1)[0]
        return {int(u) for u in re.findall(r"m_Unicode: (\d+)", table)}

    fonts = {name: glyphs(name) for name in ("Silver SDF", "DNFBitBitv2 SDF")}
    for sid, values in TEXT.items():
        assert 150 <= sid < 180 and len(values) == 4, sid
        for value in values:
            assert value and not set(value) & set(",^\n"), (sid, value)
        for value in values[:2]:
            for name, have in fonts.items():
                missing = sorted({c for c in value if ord(c) not in have})
                assert not missing, (sid, name, missing, value)
    print(f"save.py: {len(TEXT)} strings OK")
