using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    static TMP_FontAsset s_pixel;
    static readonly List<(Define.ScriptType lang, TMP_FontAsset font)> s_system = new List<(Define.ScriptType, TMP_FontAsset)>();
    static bool s_installed;

    // 에디터는 도메인 리로드를 끄고 돈다(Enter Play Mode 옵션). 만든 글꼴은 플레이가 끝날 때 부서지는데 이 값들은
    // 남아서, 두 번째 플레이부터 죽은 글꼴을 다시 걸어 ◀▶·일본어·중국어가 □ 로 나왔다. 플레이마다 새로 만든다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_pixel = null;
        s_system.Clear();
        s_installed = false;
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
            GameSettings.Changed -= Attach;
            GameSettings.Changed += Attach;
            Application.quitting -= Detach;
            Application.quitting += Detach;

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
                if (Application.isPlaying)
                    Attach();
            });
        }
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
        foreach ((Define.ScriptType lang, TMP_FontAsset font) in s_system)
            if (lang == chosen)
                global.Add(font);
        foreach ((Define.ScriptType lang, TMP_FontAsset font) in s_system)
            if (lang != chosen)
                global.Add(font);
    }

    static void Detach()
    {
        // 부서진 글꼴(유니티 null)도 같이 걷는다. 지난 플레이의 것이 전역 목록(TMP Settings 에셋)에 남아 플레이마다 늘었다.
        TMP_Settings.fallbackFontAssets.RemoveAll(f => f == null || f == s_pixel || s_system.Exists(s => s.font == f));
    }
}
