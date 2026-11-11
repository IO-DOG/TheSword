using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

// 일본어·중국어가 두부(□)로 나오지 않게 TMP 전역 폴백을 채운다.
//
// 두 글꼴(Silver SDF, DNFBitBitv2 SDF)은 정적 아틀라스라 구울 때 넣은 글자만 그린다.
// ScriptData 네 언어가 쓰는 글자와 대 보면 Silver 는 히라가나 31자와 、。，：▶◀ 가,
// DNF 는 한자 전부와 히라가나 일부(일본어 58자·중국어 54자)가 빠져 있다.
// 원본 Silver.ttf 는 그 글자를 전부 갖고 있다 — 그걸로 동적 아틀라스를 만들어 맨 앞에 두고,
// 그래도 없는 글자(앞으로 쓸 대사의 드문 한자 등)는 윈도에 늘 있는 글꼴이 받는다.
//
// 에셋은 건드리지 않는다. 에디터에서 플레이 중에 폴백 표를 고치거나 동적 아틀라스에 글자를
// 넣으면 그대로 에셋에 남는다 — 만드는 것은 전부 런타임 인스턴스이고, 전역 목록
// (TMP Settings 에셋)에 넣은 것은 플레이가 끝날 때 도로 뺀다.
public static class FontFallback
{
    // 어드레서블 주소 — Assets/@Resources/Font/Silver.ttf. 등록이 안 돼 있으면 윈도 글꼴만 쓴다.
    const string PixelFontKey = "Silver";

    // 윈도 10/11 이 언어팩 없이도 갖고 있는 글꼴. 한자는 나라마다 자형이 달라 고른 언어 것을 앞에 둔다.
    static readonly (Define.ScriptType lang, string family)[] SystemFonts =
    {
        (Define.ScriptType.Kr, "Malgun Gothic"),
        (Define.ScriptType.Jp, "Yu Gothic"),
        (Define.ScriptType.Cn, "Microsoft YaHei"),
    };

    // HUD·숫자 글꼴(DNF)에는 한자가 없어 Silver 로 넘어가는데, 같은 크기에서 Silver 한자는 DNF 글자 키의 0.7 배라
    // "距暴击 6次"·"基准" 의 한자만 쪼그라들었다(중·일 HUD, 2026-11-03 스크린샷). 중·일일 때만 DNF 의 제 폴백 표에
    // 키운 Silver 를 건다 — 이야기 글꼴(Silver SDF)은 전역 폴백(제 크기) 그대로, 한·영은 아무것도 안 바뀐다.
    const float NumberCjkScale = 1.3f;

    static TMP_FontAsset s_pixel;
    static TMP_FontAsset s_pixelCjk;
    static readonly List<(Define.ScriptType lang, TMP_FontAsset font)> s_system = new List<(Define.ScriptType, TMP_FontAsset)>();
    static bool s_installed;
    static Define.ScriptType? s_attachedFor;    // 마지막으로 건 언어 (OnSettingsChanged)

    // 에디터는 도메인 리로드를 끄고 돈다(Enter Play Mode 옵션). 만든 글꼴은 플레이가 끝날 때 부서지는데 이 값들은
    // 남아서, 두 번째 플레이부터 죽은 글꼴을 다시 걸어 ◀▶·일본어·중국어가 □ 로 나왔다. 플레이마다 새로 만든다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_pixel = null;
        s_pixelCjk = null;
        s_system.Clear();
        s_installed = false;
        s_attachedFor = null;
    }

    // 켜자마자 한 번 (Managers).
    public static void Install()
    {
        if (s_installed == false)
        {
            s_installed = true;
            foreach ((Define.ScriptType lang, string family) in SystemFonts)
            {
                TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(family, "Regular");
                if (font != null)
                    s_system.Add((lang, font));
            }
            // 정적 이벤트도 플레이를 넘어 남는다 — 빼고 건다.
            GameSettings.Changed -= OnSettingsChanged;
            GameSettings.Changed += OnSettingsChanged;
            Application.quitting -= Detach;
            Application.quitting += Detach;
            // DNF 는 장면과 어드레서블 묶음에 따로 실려 메모리에 둘일 수 있다 — 새로 읽힌 것에도 건다(선로드 뒤는 Refresh).
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            // 주소도 키라서 라벨 자리에 넣어도 된다. 등록이 안 돼 있으면 예외 없이 오류 문자열만 온다.
            Managers.Resource.LoadAllAsync<Font>(PixelFontKey, null, error =>
            {
                Font source = error == null ? Managers.Resource.Load<Font>(PixelFontKey) : null;
                if (source == null)
                {
                    Debug.LogWarning($"[Font] 어드레서블에 '{PixelFontKey}' 가 없다 — 빠진 글자는 윈도 글꼴로 그린다");
                    return;
                }
                // Silver SDF 와 같은 크기·여백으로 굽는다. 한 줄에 섞여 찍혀도 굵기가 같다.
                s_pixel = TMP_FontAsset.CreateFontAsset(source, 47, 5, GlyphRenderMode.SDFAA, 1024, 1024);
                s_pixelCjk = TMP_FontAsset.CreateFontAsset(source, 47, 5, GlyphRenderMode.SDFAA, 1024, 1024);
                FaceInfo face = s_pixelCjk.faceInfo;
                face.scale = NumberCjkScale;
                s_pixelCjk.faceInfo = face;
                if (Application.isPlaying)
                    Attach();
            });
        }
        Attach();
    }

    /// <summary>어드레서블 선로드 뒤 한 번 — 프리팹이 물고 온 DNF 에도 건다.</summary>
    public static void Refresh() => Attach();

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Attach();

    // 설정은 소리 막대 한 칸·V(맵 숫자)·흔들림마다 Changed 를 쏜다. 글꼴은 언어만 따르는데 그때마다 모든 글꼴·글을 훑어 덱에서
    // 한 번씩 끊겼다 — 언어가 바뀐 때만 다시 건다.
    static void OnSettingsChanged()
    {
        if (s_attachedFor != GameSettings.Language)
            Attach();
    }

    // 픽셀 글꼴 → 고른 언어의 윈도 글꼴 → 나머지 윈도 글꼴.
    static void Attach()
    {
        Detach();
        List<TMP_FontAsset> global = TMP_Settings.fallbackFontAssets;
        if (s_pixel != null)
            global.Add(s_pixel);
        Define.ScriptType chosen = GameSettings.Language;
        s_attachedFor = chosen;
        foreach ((Define.ScriptType lang, TMP_FontAsset font) in s_system)
            if (lang == chosen)
                global.Add(font);
        foreach ((Define.ScriptType lang, TMP_FontAsset font) in s_system)
            if (lang != chosen)
                global.Add(font);

        bool cjk = s_pixelCjk != null && (chosen == Define.ScriptType.Cn || chosen == Define.ScriptType.Jp);
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            bool number = font.name == CodeUI.NumberFontName;
            if (number == false && font.name != CodeUI.ProseFontName)
                continue;
            ForgetFallbacks(font);
            if (number && cjk)
            {
                MatchLines(s_pixelCjk, font);
                font.fallbackFontAssetTable.Insert(0, s_pixelCjk);
            }
        }
        // 이미 짠 글은 옛 폴백 글자를 들고 있다 — 다음 그리기에 다시 짠다. 꺼진 글은 켤 때 다시 짠다.
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
            text.havePropertiesChanged = true;
    }

    // TMP 는 폴백에서 찾은 글자를 그 글을 쓴 글꼴의 조회표에 적어 두고 다시 찾지 않는다(TMP_Text.GetTextElement — 먼저 찾은 것이
    // 이긴다). Silver.ttf 가 도착하기 전에 짜인 글의 가나·한자는 윈도 글꼴로 적혀, 픽셀 글꼴을 건 뒤에도 그 실행 내내 그 글자만
    // 윈도 글꼴로 나왔다 — 크기도 달라 히라가나만 커지고 도감 설명이 칸을 넘쳤다(2026-11-11 일본어). 폴백을 다시 걸 때마다
    // 남의 글꼴에서 빌려 적어 둔 글자를 지운다. 제 글자(textAsset 이 자신)는 그대로다.
    static void ForgetFallbacks(TMP_FontAsset font)
    {
        Dictionary<uint, TMP_Character> table = font.characterLookupTable;
        var borrowed = new List<uint>();    // 폴백을 다시 걸 때(언어 바꿈·씬)만 돈다
        foreach (KeyValuePair<uint, TMP_Character> kv in table)
            if (kv.Value.textAsset != font)
                borrowed.Add(kv.Key);
        foreach (uint key in borrowed)
            table.Remove(key);
    }

    // 줄의 윗선·아랫선은 DNF 와 같게. Silver 의 깊은 아랫선(0.47em)을 1.3 배로 키운 채 두면 한자 한 글자가 줄을 늘려,
    // 높이에 맞춰 줄이는 글(전투 라벨)이 숫자까지 17pt 로 작아졌다. 글자 그림의 자리는 그대로다.
    static void MatchLines(TMP_FontAsset fallback, TMP_FontAsset number)
    {
        FaceInfo face = fallback.faceInfo, to = number.faceInfo;
        float k = face.pointSize / (to.pointSize * face.scale);   // DNF 단위 -> 이 글꼴 단위
        face.ascentLine = to.ascentLine * k;
        face.descentLine = to.descentLine * k;
        face.lineHeight = to.lineHeight * k;
        fallback.faceInfo = face;
    }

    static void Detach()
    {
        // 부서진 글꼴(유니티 null)도 같이 걷는다. 지난 플레이의 것이 전역 목록(TMP Settings 에셋)에 남아 플레이마다 늘었다.
        TMP_Settings.fallbackFontAssets.RemoveAll(f => f == null || f == s_pixel || s_system.Exists(s => s.font == f));
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (font.name == CodeUI.NumberFontName)
                font.fallbackFontAssetTable?.RemoveAll(f => f == null || f == s_pixelCjk);
    }
}
