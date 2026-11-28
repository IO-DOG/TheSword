using DG.Tweening;
using UnityEngine;

/// <summary>
/// 챕터 보스 다섯(id 900~904, 20·40·60·80·100층)의 한 장짜리 그림(`Sprites/Boss/BossCh{챕터}.png`, 172칸, 점 크기는 몹과 같다).
/// 몹 시트처럼 애니메이터로 돌지 않는다 — 그림을 넣는 곳(맵·전투 카드·대화 초상·도감)에서 이것이 그림을 주면
/// 애니메이터를 끄고 이 스프라이트를 건다. 공격은 `Lunge` 로 몸을 내민다. 그림이 없으면(어드레서블에 빠졌다) null 이라
/// 부르는 쪽이 예전 몹 시트를 그대로 튼다.
/// 그림은 이미 챕터 색이라 `MonsterTint` 를 곱하지 않는다(`MonsterTint.Of` 가 이 그림이 있으면 흰색을 준다).
/// </summary>
public static class BossArt
{
    public const int FirstId = 900;

    public static bool IsChapterBoss(int id) => id >= FirstId && id < FirstId + 5;

    // ponytail: 매번 Load — ResourceManager 가 이미 캐시를 쥐고 있다.
    public static Sprite Of(int id) =>
        IsChapterBoss(id) ? Managers.Resource.Load<Sprite>($"BossCh{id - FirstId}") : null;

    /// <summary>애니메이터 대신 정지 그림을 건다. 걸었으면 true.</summary>
    public static bool Apply(int id, Animator anim, SpriteRenderer sr)
    {
        Sprite s = Of(id);
        if (s == null || sr == null)
            return false;
        if (anim != null)
            anim.enabled = false;
        sr.sprite = s;
        Breathe(id, sr, s);
        return true;
    }

    /// <summary>
    /// 맵에 선 챕터 보스에 빛 하나(11-28 오너 "종뿔사슴은 잘 안 보이고"). 보스 그림은 조명을 받는 재질이라 챕터의 어두운 조명 아래에서
    /// 벽에 묻혔다 — 몸이 가늘고 잿빛인 종뿔사슴은 거의 안 보였다. 몸 앞(카메라 쪽) 위에서 비춘다: 0.4 앞·세기 2.5 는 하얗게 날아갔고
    /// 세기 0.55 는 티가 안 났다. 보스 밑에 달아 쓰러지면 같이 꺼진다. 층마다 하나뿐이라(보스 층 다섯) 값이 싸다.
    /// 트랜스폼이 다 선 뒤(MonsterController.Init)에 부른다 — 자리를 월드 기준으로 잡는다(보스는 세로 배율이 2 다).
    /// </summary>
    public static void Glow(Transform boss)
    {
        if (boss == null || boss.Find("BossGlow") != null)
            return;
        GameObject go = new GameObject("BossGlow");
        go.transform.SetParent(boss, false);
        go.transform.position = boss.position + new Vector3(0f, 0.7f, -1.0f);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 2.8f;
        light.intensity = 1.4f;
        light.color = new Color(1f, 0.94f, 0.82f);
        light.shadows = LightShadows.None;
    }

    const float BreathSeconds = 0.6f;
    static System.Collections.Generic.Dictionary<int, Sprite> s_raised;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => s_raised = null;

    /// <summary>맵의 대기 동작 — 같은 그림을 한 점 위에 그린 판과 번갈아 건다(몹 시트의 들썩임 자리).
    /// 트랜스폼을 옮기지 않아 콜라이더·SitOnFloor·예측 숫자 자리가 그대로고, 한 점씩이라 도트가 번지지 않는다.
    /// 아래끝은 바닥 그대로(올라갔다 돌아온다).</summary>
    static void Breathe(int id, SpriteRenderer sr, Sprite s)
    {
        s_raised ??= new System.Collections.Generic.Dictionary<int, Sprite>();
        if (s_raised.TryGetValue(id, out Sprite up) == false || up == null)
        {
            Rect r = s.rect;
            Vector2 pivot = new Vector2(s.pivot.x / r.width, (s.pivot.y - 1f) / r.height);   // 피벗을 한 점 내리면 그림이 한 점 뜬다
            up = Sprite.Create(s.texture, r, pivot, s.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            up.name = s.name + "_up";
            s_raised[id] = up;
        }
        DOTween.Kill(sr);
        DOTween.Sequence()
            .AppendInterval(BreathSeconds).AppendCallback(() => { if (sr != null && sr.sprite == s) sr.sprite = up; })
            .AppendInterval(BreathSeconds).AppendCallback(() => { if (sr != null && sr.sprite == up) sr.sprite = s; })
            .SetLoops(-1).SetId(sr).SetLink(sr.gameObject);
    }

    /// <summary>UI 판. 이미지의 애니메이터를 끄고 그림을 건다.</summary>
    public static bool Apply(int id, UnityEngine.UI.Image img)
    {
        Sprite s = Of(id);
        if (s == null || img == null)
            return false;
        Animator anim = img.GetComponent<Animator>();
        if (anim != null)
            anim.enabled = false;
        img.sprite = s;
        return true;
    }

    /// <summary>공격 한 번 — 상대 쪽(왼쪽)으로 몸을 내밀었다 돌아온다. 몹 시트의 공격 동작 자리를 대신한다.</summary>
    public static void Lunge(RectTransform rt)
    {
        if (rt == null)
            return;
        string key = "BossLunge" + rt.GetInstanceID();   // 보스 등장 밀기 등 같은 판의 다른 트윈은 건드리지 않는다
        DOTween.Complete(key);
        rt.DOPunchAnchorPos(new Vector2(-24f, 0f), 0.35f, 1, 0f).SetId(key).SetLink(rt.gameObject);
    }
}
