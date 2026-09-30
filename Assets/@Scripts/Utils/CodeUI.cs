using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프리팹 없이 코드로 세우는 창(전투 예측 표시·몬스터 도감·워프)이 같이 쓰는 도구.
///
/// 빌드에서 불러올 수 있는 것은 어드레서블 "PreLoad" 와 그 프리팹이 물고 있는 것뿐이다. 글꼴(TMP)과
/// 창 그림 대부분은 어드레서블이 아니라서, 이미 있는 프리팹에서 이름으로 빌려 온다 — 그래야 그림과
/// 글꼴이 게임의 다른 창과 똑같다. 못 찾으면 null 이고, 부르는 쪽은 그림 없이도 돌아가야 한다.
/// </summary>
public static class CodeUI
{
    public const string NumberFontName = "DNFBitBitv2 SDF";   // HUD·숫자
    public const string ProseFontName = "Silver SDF";         // 이름·설명

    // 글꼴과 창 그림을 물고 있는 프리팹. HUD 에 두 글꼴이 다 있다.
    static readonly string[] FontSources = { "UI_GameScene", "UI_MenuPopup" };

    static readonly Dictionary<string, Object> s_cache = new Dictionary<string, Object>();

    public static TMP_FontAsset NumberFont => Font(NumberFontName);
    public static TMP_FontAsset ProseFont => Font(ProseFontName);

    public static TMP_FontAsset Font(string fontName)
    {
        if (Cached(fontName, out TMP_FontAsset font))
            return font;
        foreach (string key in FontSources)
        {
            GameObject prefab = Managers.Resource.Load<GameObject>(key);
            if (prefab == null)
                continue;
            foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != null && text.font.name == fontName)
                    return Remember(fontName, text.font);
            }
        }
        return TMP_Settings.defaultFontAsset;
    }

    /// <summary>프리팹 안의 Image 가 쓰는 스프라이트를 이름으로 빌려 온다.</summary>
    public static Sprite PrefabSprite(string prefabKey, string spriteName)
    {
        string id = prefabKey + "/" + spriteName;
        if (Cached(id, out Sprite sprite))
            return sprite;
        GameObject prefab = Managers.Resource.Load<GameObject>(prefabKey);
        if (prefab == null)
            return null;
        foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite != null && image.sprite.name == spriteName)
                return Remember(id, image.sprite);
        }
        return null;
    }

    /// <summary>
    /// 9분할 테두리를 붙인 사본. 메뉴 버튼(240x48) 같은 그림은 테두리가 없어서 늘리면 양 끝의 모서리까지
    /// 늘어난다 — 같은 텍스처에 테두리만 정해 새로 만든다(텍스처는 복사하지 않는다).
    /// </summary>
    public static Sprite Sliced(Sprite source, Vector4 border)
    {
        if (source == null)
            return null;
        string id = source.GetInstanceID() + "/" + border;
        if (Cached(id, out Sprite sprite))
            return sprite;
        sprite = Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit,
                               0, SpriteMeshType.FullRect, border);
        sprite.name = source.name + "_Sliced";
        return Remember(id, sprite);
    }

    /// <summary>검은 테두리를 두른 글자 재질. 맵 위에서 읽히게 한다. 글꼴마다 하나를 나눠 쓴다(배칭이 깨지지 않게).</summary>
    public static Material Outlined(TMP_FontAsset font, float width)
    {
        string id = "outline/" + font.GetInstanceID() + "/" + width;
        if (Cached(id, out Material material))
            return material;
        material = new Material(font.material) { name = font.name + " Outline" };
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.95f));
        return Remember(id, material);
    }

    // 에디터는 도메인 리로드 없이 플레이를 되풀이한다 — 지난 플레이에서 받아 둔 것은 이미 부서졌을 수 있다.
    static bool Cached<T>(string id, out T value) where T : Object
    {
        value = s_cache.TryGetValue(id, out Object found) ? found as T : null;
        return value != null;
    }

    static T Remember<T>(string id, T value) where T : Object
    {
        s_cache[id] = value;
        return value;
    }

    public static RectTransform NewRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>부모 기준 앵커 한 점에 붙인다. pos 는 앵커에서의 거리.</summary>
    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static TextMeshProUGUI NewText(Transform parent, string name, TMP_FontAsset font, float size, Color color,
                                       TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI text = NewRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    /// <summary>칸에 넣고 넘치면 글자를 줄인다 (UI_BaseCard.SetName 과 같은 방식).</summary>
    public static TextMeshProUGUI Fit(TextMeshProUGUI text, float minSize)
    {
        text.fontSizeMax = text.fontSize;
        text.fontSizeMin = minSize;
        text.enableAutoSizing = true;
        return text;
    }

    /// <summary>그림 한 장. sliced 면 9분할 테두리를 2배 픽셀로 그린다 — HUD 가 같은 그림을 2배로 쓴다.</summary>
    public static Image NewImage(Transform parent, string name, Sprite sprite, Color color, bool sliced = false)
    {
        Image image = NewRect(parent, name).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        if (sliced && sprite != null && sprite.border != Vector4.zero)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
        }
        return image;
    }
}
