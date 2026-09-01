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

        // 화면 전체에 곱해지는 색 (URP ColorAdjustments). 해의 색과 같은 방향이라야
        // 조명과 후처리가 서로 다른 시간대를 말하지 않는다.
        public Color ColorFilter;

        // 공중에 떠도는 것. 벽 아트가 한 세트뿐이라, 방에 들어섰을 때
        // "여기는 다른 곳이다" 를 가장 먼저 말해 주는 것이 이 알갱이들이다.
        // 같은 파티클(FallingLeaves)을 색·중력·크기·빈도만 바꿔 다르게 쓴다.
        public Color AirColor;
        public float AirGravity;    // 음수면 떠오른다 (불티·티끌)
        public float AirSize;
        public float AirSpeed;      // 파티클 시뮬레이션 배속
        public Vector2 AirInterval; // 하나 내보내는 간격의 최소~최대(초)
        public Color DustColor;     // 카메라 주변을 떠다니는 먼지
        public Color FogBase;       // VFX Graph 안개의 두 색
        public Color FogSecondary;

        // 챕터마다 같은 곡을 다른 조·속도로 쓴다. 곡이 챕터 00 것 하나뿐이라
        // 100층 내내 같은 소리가 나는 것을 이걸로 덜어 둔다 — 새 곡이 들어오면
        // 1 로 두고 StageInfoData 의 BGM 키만 채우면 된다.
        public float BgmPitch;
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
            ColorFilter = new Color(1.00f, 0.91f, 0.81f),
            AirColor = new Color(0.72f, 0.82f, 0.52f), AirGravity = 0.06f,
            AirSize = 1.00f, AirSpeed = 1.00f, AirInterval = new Vector2(2f, 5f),
            DustColor = new Color(0.85f, 0.86f, 0.72f),
            FogBase = new Color(0.62f, 0.68f, 0.55f), FogSecondary = Color.white,
            BgmPitch = 1.00f,
        },
        // 01 무너진 수로 — 습기 찬 구름 낀 낮 / 보통 안개
        new Theme
        {
            Light = new Color(0.62f, 0.80f, 1.00f), Intensity = 0.85f,
            SunAngle = new Vector3(38f, 20f, 0f),
            Ambient = new Color(0.30f, 0.38f, 0.46f),
            Fog = true, FogColor = new Color(0.42f, 0.55f, 0.66f), FogDensity = 0.030f,
            ColorFilter = new Color(0.78f, 0.90f, 1.00f),
            AirColor = new Color(0.62f, 0.86f, 1.00f), AirGravity = 0.55f,
            AirSize = 0.45f, AirSpeed = 1.70f, AirInterval = new Vector2(0.5f, 1.4f),
            DustColor = new Color(0.66f, 0.82f, 0.92f),
            FogBase = new Color(0.40f, 0.58f, 0.72f), FogSecondary = new Color(0.72f, 0.88f, 1.00f),
            BgmPitch = 0.95f,
        },
        // 02 잿빛 용광로 — 열기로 흐린 한낮 / 붉은 재 안개
        new Theme
        {
            Light = new Color(1.00f, 0.68f, 0.45f), Intensity = 1.05f,
            SunAngle = new Vector3(62f, -10f, 0f),
            Ambient = new Color(0.44f, 0.28f, 0.20f),
            Fog = true, FogColor = new Color(0.52f, 0.30f, 0.22f), FogDensity = 0.026f,
            ColorFilter = new Color(1.00f, 0.80f, 0.62f),
            AirColor = new Color(1.00f, 0.55f, 0.18f), AirGravity = -0.34f,
            AirSize = 0.55f, AirSpeed = 1.25f, AirInterval = new Vector2(0.7f, 1.8f),
            DustColor = new Color(0.95f, 0.62f, 0.38f),
            FogBase = new Color(0.52f, 0.28f, 0.18f), FogSecondary = new Color(1.00f, 0.62f, 0.30f),
            BgmPitch = 1.06f,
        },
        // 03 얼어붙은 심층 — 신비스러운 밝은 저녁 / 약한 안개
        new Theme
        {
            Light = new Color(0.75f, 0.92f, 1.00f), Intensity = 0.80f,
            SunAngle = new Vector3(22f, 40f, 0f),
            Ambient = new Color(0.32f, 0.40f, 0.48f),
            Fog = true, FogColor = new Color(0.60f, 0.72f, 0.82f), FogDensity = 0.020f,
            ColorFilter = new Color(0.86f, 0.94f, 1.00f),
            AirColor = new Color(0.95f, 0.99f, 1.00f), AirGravity = 0.10f,
            AirSize = 0.70f, AirSpeed = 0.55f, AirInterval = new Vector2(0.4f, 1.1f),
            DustColor = new Color(0.90f, 0.95f, 1.00f),
            FogBase = new Color(0.66f, 0.78f, 0.88f), FogSecondary = new Color(0.88f, 0.96f, 1.00f),
            BgmPitch = 0.92f,
        },
        // 04 왕좌의 균열 — 스산한 저녁 / 짙은 보랏빛 안개
        new Theme
        {
            Light = new Color(0.62f, 0.50f, 0.88f), Intensity = 0.70f,
            SunAngle = new Vector3(14f, 70f, 0f),
            Ambient = new Color(0.26f, 0.22f, 0.34f),
            Fog = true, FogColor = new Color(0.28f, 0.22f, 0.38f), FogDensity = 0.042f,
            ColorFilter = new Color(0.82f, 0.72f, 1.00f),
            AirColor = new Color(0.72f, 0.48f, 1.00f), AirGravity = -0.10f,
            AirSize = 0.85f, AirSpeed = 0.70f, AirInterval = new Vector2(1.2f, 3.0f),
            DustColor = new Color(0.72f, 0.62f, 0.92f),
            FogBase = new Color(0.26f, 0.20f, 0.36f), FogSecondary = new Color(0.66f, 0.42f, 0.95f),
            BgmPitch = 0.90f,
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
