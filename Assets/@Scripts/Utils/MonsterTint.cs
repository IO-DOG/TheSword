using UnityEngine;

/// <summary>
/// 몬스터 색 변형.
///
/// 생성 층이 쓸 수 있는 몬스터 그림은 몹 8종이 전부다 (Boss_C0_* 는 킹 슬라임과
/// 분열 3종 연출 전용이라 다른 층에 내보내지 않는다). 100층을 그것만으로 채우면
/// 같은 그림이 계속 나온다. 매직 타워가 쓰던 방법 그대로, 같은 그림을 색만 바꿔
/// 다른 몬스터로 쓴다 — 8종 x 5챕터 = 40가지.
/// 정예와 보스는 여기에 몸집이 더해진다 (MapBuilder.MonsterBulk).
///
/// 색은 장식이 아니라 정보다.
///   * 색조(hue)   = 챕터. 층이 스무 개 지날 때마다 분위기가 바뀐다.
///   * 진하기      = 그 층에서의 서열. 옅으면 약하고 진하면 세다.
/// 그래서 방에 들어선 순간 "저건 지금 잡을 놈인가" 가 눈으로 읽힌다.
/// </summary>
public static class MonsterTint
{
    // 챕터별 색조. MapBuilder 의 벽 틴트와 같은 계열로 맞춰 층 전체가 겉돌지 않게 한다.
    static readonly Color[] Chapter =
    {
        new Color(0.70f, 1.00f, 0.70f), // 00 이끼 낀 지하 묘소 - 초록
        new Color(0.65f, 0.90f, 1.00f), // 01 무너진 수로       - 물빛
        new Color(1.00f, 0.72f, 0.55f), // 02 잿빛 용광로       - 잿불
        new Color(0.80f, 0.95f, 1.00f), // 03 얼어붙은 심층     - 서리
        new Color(0.82f, 0.70f, 1.00f), // 04 왕좌의 균열       - 보랏빛
    };

    // 층 안에서의 서열(0=제일 약함). 뒤로 갈수록 진해진다.
    static readonly float[] Depth = { 1.15f, 1.05f, 0.95f, 0.85f, 0.72f };

    // 챕터 안에서 다섯 층마다 색을 조금씩 돌린다.
    //
    // 한 챕터가 20층인데 색이 하나면, 같은 그림이 스무 층 내내 똑같은 놈으로 보인다.
    // 챕터를 알아볼 수 있을 만큼만 돌려서 "같은 계열의 다른 종" 으로 읽히게 한다.
    static readonly float[] VariantHue = { 0f, 0.055f, -0.055f, 0.11f };
    static readonly float[] VariantSat = { 1.00f, 1.18f, 0.82f, 1.08f };

    static Color Variant(Color baseColor, int variant)
    {
        int i = ((variant % VariantHue.Length) + VariantHue.Length) % VariantHue.Length;
        if (i == 0)
            return baseColor;

        float h, sat, v;
        Color.RGBToHSV(baseColor, out h, out sat, out v);
        h = Mathf.Repeat(h + VariantHue[i], 1f);
        sat = Mathf.Clamp01(sat * VariantSat[i]);
        return Color.HSVToRGB(h, sat, v);
    }

    /// <summary>그 몬스터가 입을 색. chapter 는 0~4, order 는 층 안 서열,
    /// variant 는 챕터 안에서 몇 번째 색 갈래인가.</summary>
    public static Color For(int chapter, int order, int variant = 0)
    {
        Color baseColor = Variant(Chapter[Mathf.Clamp(chapter, 0, Chapter.Length - 1)], variant);
        float k = Depth[Mathf.Clamp(order, 0, Depth.Length - 1)];

        // 곱하기만 하면 어두워지기만 한다. 1 보다 큰 값은 흰 쪽으로 끌어올린다.
        if (k >= 1f)
            return Color.Lerp(baseColor, Color.white, (k - 1f) / 0.2f);
        return baseColor * k;
    }

    /// <summary>보스용. 서열 대신 챕터 색을 진하게 쓴다.</summary>
    public static Color ForBoss(int chapter)
    {
        return Chapter[Mathf.Clamp(chapter, 0, Chapter.Length - 1)] * 0.80f;
    }

    /// <summary>몬스터 데이터에서 색을 정한다. 맵과 전투창이 같은 값을 쓰게.</summary>
    public static Color Of(int monsterId)
    {
        Data.MonsterData md;
        if (Managers.Data.MonsterDic.TryGetValue(monsterId, out md) == false)
            return Color.white;

        // 손수 만든 1~4층(0~16)은 원본 색 그대로 둔다.
        if (monsterId < 100)
            return Color.white;

        // 생성 몬스터 id = MOB_ID_BASE + 층*8 + 서열, 보스 = BOSS_ID_BASE + 챕터
        if (monsterId >= 900 && monsterId < 1000)
            return ForBoss(md.Chapter);

        // 생성 몬스터 id = 1000 + 층*8 + 서열. 층을 되짚어 색 갈래를 정한다.
        int order = monsterId % 8;
        int floor = (monsterId - 1000) / 8;
        int variant = (floor / 5) % VariantHue.Length;   // 다섯 층마다 갈린다
        return For(md.Chapter, order, variant);
    }
}
