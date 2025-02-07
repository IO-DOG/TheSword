using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Animations;
using UnityEngine;

public class SplitSlimeController : BossMonsterController
{
    protected override void Init()
    {
        base.Init();
        SetDeadEvent();
    }

    public override void OnDeadEvent()
    {
        Managers.Game.TotalKillSplitSlime++;
        if(Managers.Game.TotalKillSplitSlime == 3)
            Managers.Directing.BossOnDeadAction.Invoke();

        Managers.Directing.Events.StartBossDeathEffect(this.gameObject);
    }

    public override void SetDeadEvent()
    {
        Managers.Directing.BossOnDeadAction = null;
        Managers.Directing.BossOnDeadAction += Managers.Directing.Events.CoStartUnLock4Floor;
    }

    public override void SetAppearEvent()
    {
        
    }

    public override void OnAppearEvent()
    {
        
    }

}
