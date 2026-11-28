using UnityEngine;

/// <summary>
/// 챕터 보스 그림(172칸)은 화면에서 다섯 칸 높이라 북쪽에 선 것을 통째로 가린다 — 40층은 보스로 가는 외길(룬·몬스터·큰 물약)과
/// 그 길을 걷는 주인공이 보스 그림 뒤로 사라졌고, 60·80·100층도 보스 북쪽 서너 칸이 가렸다(11-28). 주인공이 그 뒤(북쪽)에
/// 들어서면 보스를 비쳐 보이게 한다. 몬스터 재질(HalfSpriteShadow)은 알파를 자르기만 해서, 비치는 동안은 기본 스프라이트
/// 재질로 바꿔 걸고 다 돌아오면 제 재질을 되돌린다.
/// </summary>
public class BossSeeThrough : MonoBehaviour
{
    const float GhostAlpha = 0.35f;
    static float Margin => Define.TILE_SIZE;    // 그림 둘레로 한 칸 — 들어서기 조금 전부터 비친다
    const float Speed = 4f;                     // 초당 알파 변화

    static Material s_ghost;

    SpriteRenderer _sr;
    Material _solid;
    float _alpha = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => s_ghost = null;

    public static void Attach(GameObject boss)
    {
        if (boss.GetComponent<BossSeeThrough>() == null)
            boss.AddComponent<BossSeeThrough>();
    }

    void Awake() => _sr = GetComponent<SpriteRenderer>();

    /// <summary>
    /// 그만둔다 — 제 재질·알파로 되돌리고 끈다. 죽는 연출(UI_MonsterCard.CoDead)이 하얀 재질을 걸고 통째로 복사하기 전에 부른다:
    /// 꺼 두지 않으면 복사본이 하얀 재질을 "제 재질" 로 쥐고 비치는 재질로 갈아, 하얀 번쩍임이 반투명하게 튀었다(꺼진 채로 복사된다).
    /// </summary>
    public static void Stop(GameObject boss)
    {
        BossSeeThrough see = boss != null ? boss.GetComponent<BossSeeThrough>() : null;
        if (see == null)
            return;
        see.Restore();
        see.enabled = false;
    }

    void Restore()
    {
        if (_sr == null)
            return;
        if (_solid != null)
            _sr.sharedMaterial = _solid;
        _solid = null;
        _alpha = 1f;
        Color c = _sr.color;
        c.a = 1f;
        _sr.color = c;
    }

    void LateUpdate()
    {
        if (_sr == null || _sr.sprite == null)
            return;
        float target = Showing() == false && PlayerBehind() ? GhostAlpha : 1f;
        if (Mathf.Approximately(_alpha, target))
            return;

        if (_solid == null)
        {
            // 비치기 시작: 제 재질을 쥐어 두고 알파를 받는 재질로 바꾼다.
            if (s_ghost == null)
                s_ghost = new Material(Shader.Find("Sprites/Default"));
            _solid = _sr.sharedMaterial;
            _sr.sharedMaterial = s_ghost;
        }
        _alpha = Mathf.MoveTowards(_alpha, target, Speed * Time.unscaledDeltaTime);
        Color c = _sr.color;
        c.a = _alpha;
        _sr.color = c;
        if (_alpha >= 1f)
        {
            _sr.sharedMaterial = _solid;
            _solid = null;
        }
    }

    /// <summary>보스를 보여 줘야 하는 때 — 등장 대화·연출·전투 중에는 비치지 않는다(40층은 늘 북쪽에서 다가가 등장 장면이 반투명했다).</summary>
    static bool Showing()
    {
        if (Managers.IsAlive == false)
            return true;
        GameManager g = Managers.Game;
        return g.OnBattle || g.OnConversation || g.OnDirect;
    }

    /// <summary>
    /// 주인공이 이 그림에 가리는 자리에 있는가 — 보스보다 북쪽이고, 그림의 폭과 (화면에서의) 높이 안.
    /// 카메라가 60도로 내려다보니 높이 y 는 바닥에서 y/√3 만큼 북쪽을 가린다.
    /// </summary>
    bool PlayerBehind()
    {
        if (Managers.IsAlive == false || Managers.Game.Player == null)
            return false;
        Vector3 p = Managers.Game.Player.transform.position, at = transform.position;
        Bounds b = _sr.bounds;
        float north = p.z - at.z;
        float reach = b.max.z - at.z + (b.max.y - at.y) / Mathf.Sqrt(3f);
        return north > Define.TILE_SIZE * 0.5f && north < reach + Margin && Mathf.Abs(p.x - b.center.x) < b.extents.x + Margin;
    }
}
