using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossMonsterController : MonsterController
{
    public abstract void SetAppearEvent();
    public abstract void OnAppearEvent();
    public abstract void SetDeadEvent();
    public abstract void OnDeadEvent();
}
