using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class BossDoor : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(PlayEffect());
    }

    IEnumerator PlayEffect()
    {
        Managers.Game.OnDirect = true;

        yield return new WaitForSeconds(1.5f);

        Managers.Resource.Instantiate("FX_BossPortal_B", transform);

        yield return new WaitForSeconds(1f);

        float originalSpeed = Managers.Game.CurPlayerData.MoveSpeed;
        Managers.Game.Player.Speed = 2f;
        Managers.Game.Player.Moving(Define.MoveDir.Up);

        yield return new WaitForSeconds(0.5f);

        Managers.Game.Player.gameObject.SetActive(false);

        yield return new WaitForSeconds(2f);

        Managers.Game.Player.gameObject.SetActive(true);
        Managers.Game.Player.SetIdleState(Define.MoveDir.Up);
        Managers.Game.Player.Speed = originalSpeed;
        Managers.Game.OnFadeAction.Invoke();
        Managers.Game.OnEnterBossRoomAction.Invoke();
        Managers.Game.OnDirect = false;
        Managers.Resource.Destroy(gameObject);
    }
}
