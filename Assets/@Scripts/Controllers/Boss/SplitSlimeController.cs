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
        // 같은 슬라임의 죽음이 두 번 세어지면 세 마리를 다 잡아도 계단이 안 열린다.
        if (MarkDeadOnce() == false)
            return;

        Managers.Game.TotalKillSplitSlime++;
        if (Managers.Game.TotalKillSplitSlime >= 3 && Managers.Directing.BossOnDeadAction != null)
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
