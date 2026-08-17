using UnityEngine;

/// <summary>
/// 챕터(20층)마다 바뀌는 배경 분위기.
///
/// 기획서 12쪽이 방향을 못박아 두었다 — "현재 시스템을 유지 ▶ 환경을 변화".
/// 전투 규칙을 늘리는 대신 환경을 바꿔서 층마다 다른 곳에 온 느낌을 낸다.
/// 118~123쪽은 그 환경을 두 가지로 정의한다: <b>시간대</b>와 <b>안개</b>.
///
///   1. 밝은 낮        / 안개 없음
///   2. 구름 낀 낮     / 보통 안개
///   3. 밝은 저녁      / 약한 안개
///   4. 스산한 저녁    / 초록빛이 도는 강한 안개
///
/// 그래서 여기서도 색만 갈아입히지 않고, 해의 각도와 세기(시간대)와 안개를 같이
/// 움직인다. 벽 아트는 챕터 00 세트뿐이라 색만 바꾸면 "같은 곳에 필터를 씌운 것"
/// 으로 보이는데, 그림자 방향과 안개가 같이 바뀌면 다른 장소로 읽힌다.
/// </summary>
public static class ChapterTheme
{
    public struct Theme
    {
        public Color Light;        // 해의 색
        public float Intensity;    // 해의 세기
        public Vector3 SunAngle;   // 해의 각도 = 시간대. x 가 작을수록 해가 낮다(저녁)
        public Color Ambient;      // 그늘의 색
        public bool Fog;
        public Color FogColor;
        public float FogDensity;
    }

    static readonly Theme[] Themes =
    {
        // 00 이끼 낀 지하 묘소 — 밝은 낮 / 안개 없음
        new Theme
        {
            Light = new Color(1.00f, 0.96f, 0.88f), Intensity = 1.10f,
            SunAngle = new Vector3(50f, -30f, 0f),
            Ambient = new Color(0.42f, 0.44f, 0.40f),
            Fog = false, FogColor = new Color(0.55f, 0.58f, 0.52f), FogDensity = 0f,
        },
        // 01 무너진 수로 — 습기 찬 구름 낀 낮 / 보통 안개
        new Theme
        {
            Light = new Color(0.62f, 0.80f, 1.00f), Intensity = 0.85f,
            SunAngle = new Vector3(38f, 20f, 0f),
            Ambient = new Color(0.30f, 0.38f, 0.46f),
            Fog = true, FogColor = new Color(0.42f, 0.55f, 0.66f), FogDensity = 0.030f,
        },
        // 02 잿빛 용광로 — 열기로 흐린 한낮 / 붉은 재 안개
        new Theme
        {
            Light = new Color(1.00f, 0.68f, 0.45f), Intensity = 1.05f,
            SunAngle = new Vector3(62f, -10f, 0f),
            Ambient = new Color(0.44f, 0.28f, 0.20f),
            Fog = true, FogColor = new Color(0.52f, 0.30f, 0.22f), FogDensity = 0.026f,
        },
        // 03 얼어붙은 심층 — 신비스러운 밝은 저녁 / 약한 안개
        new Theme
        {
            Light = new Color(0.75f, 0.92f, 1.00f), Intensity = 0.80f,
            SunAngle = new Vector3(22f, 40f, 0f),
            Ambient = new Color(0.32f, 0.40f, 0.48f),
            Fog = true, FogColor = new Color(0.60f, 0.72f, 0.82f), FogDensity = 0.020f,
        },
        // 04 왕좌의 균열 — 스산한 저녁 / 짙은 보랏빛 안개
        new Theme
        {
            Light = new Color(0.62f, 0.50f, 0.88f), Intensity = 0.70f,
            SunAngle = new Vector3(14f, 70f, 0f),
            Ambient = new Color(0.26f, 0.22f, 0.34f),
            Fog = true, FogColor = new Color(0.28f, 0.22f, 0.38f), FogDensity = 0.042f,
        },
    };

    public static Theme Get(int chapter)
    {
        return Themes[Mathf.Clamp(chapter, 0, Themes.Length - 1)];
    }

    /// <summary>그 층의 챕터 분위기를 씬에 입힌다.</summary>
    public static void Apply(int chapter, Light sun)
    {
        Theme t = Get(chapter);

        if (sun != null)
        {
            sun.color = t.Light;
            sun.intensity = t.Intensity;
            sun.transform.rotation = Quaternion.Euler(t.SunAngle);
        }

        RenderSettings.ambientLight = t.Ambient;
        RenderSettings.fog = t.Fog;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = t.FogColor;
        RenderSettings.fogDensity = t.FogDensity;
    }
}
