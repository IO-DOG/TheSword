using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KingSlimeController : BossMonsterController
{
    public SpriteRenderer _sr;
    protected override void Init()
    {
        base.Init();
        SetDeadEvent();
        SetAppearEvent();
    }

    public override void SetAppearEvent()
    {
        Managers.Directing.BossOnAppearAction = null;
        Managers.Directing.BossOnAppearAction += Managers.Directing.Events.MeetKingSlime;
    }

    public override void OnAppearEvent()
    {
        Managers.Directing.BossOnAppearAction.Invoke();
    }
    public override void OnDeadEvent()
    {
        Managers.Directing.Events.StartBossDeathEffect(this.gameObject);
        Managers.Directing.BossOnDeadAction.Invoke();
    }


    public override void SetDeadEvent()
    {
        Managers.Directing.BossOnDeadAction = null;
        Managers.Directing.BossOnDeadAction += Managers.Directing.Events.CoStartKingSlimeDead;
    }
}
