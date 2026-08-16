using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossMonsterController : MonsterController
{
    /// <summary>
    /// 사망 연출을 이미 돌렸는가.
    ///
    /// 킹슬라임이 죽을 때 분열 연출이 두 번 나오면서 슬라임이 3마리가 아니라
    /// 6마리로 나왔다. UI_MonsterCard.Dead() 가 한 번 더 불리면 OnDeadEvent 도
    /// 다시 돌고, 그때마다 SlimeOrb 를 새로 만들기 때문이다.
    /// 죽음은 한 번뿐이니 여기서 막는다.
    /// </summary>
    protected bool _deadHandled;

    /// <summary>이미 처리한 죽음이면 false. 각 보스의 OnDeadEvent 첫 줄에서 쓴다.</summary>
    protected bool MarkDeadOnce()
    {
        if (_deadHandled)
        {
            Debug.LogWarning($"[Boss] {name} 사망 연출이 중복 호출됐다 — 무시한다");
            return false;
        }
        _deadHandled = true;
        return true;
    }

    public abstract void SetAppearEvent();
    public abstract void OnAppearEvent();
    public abstract void SetDeadEvent();
    public abstract void OnDeadEvent();
}
