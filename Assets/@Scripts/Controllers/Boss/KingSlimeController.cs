using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KingSlimeController : BossMonsterController
{
    protected override void Init()
    {
        base.Init();
        SetDeadEvent();
    }

    public override void OnDeadEvent()
    {
        Managers.Directing.BossOnDeadAction.Invoke();
    }


    public override void SetDeadEvent()
    {
        Managers.Directing.BossOnDeadAction = null;
        Managers.Directing.BossOnDeadAction += Managers.Directing.Events.CoStartKingSlimeDead;
    }
}
