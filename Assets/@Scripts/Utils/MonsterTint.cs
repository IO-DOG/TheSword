using UnityEngine;

/// <summary>
/// 몬스터 색 변형.
///
/// 이 프로젝트에 실재하는 몬스터 그림은 몹 8종 + 보스 4종이 전부다. 100층을
/// 그것만으로 채우면 같은 그림이 계속 나온다. 매직 타워가 쓰던 방법 그대로,
/// 같은 그림을 색만 바꿔 다른 몬스터로 쓴다.
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

    /// <summary>그 몬스터가 입을 색. chapter 는 0~4, order 는 층 안 서열.</summary>
    public static Color For(int chapter, int order)
    {
        Color baseColor = Chapter[Mathf.Clamp(chapter, 0, Chapter.Length - 1)];
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

        int order = monsterId % 8;
        return For(md.Chapter, order);
    }
}
