using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class BossDoor : MonoBehaviour
{
   public void CoStartPlayEffect()
    {
        StartCoroutine(PlayEffect());
    }

    IEnumerator PlayEffect()
    {
        Managers.Game.OnDirect = true;
        Color originLightColor = Managers.Game.DirectionalLight.color;
        Managers.Game.DirectionalLight.DOColor(Define.BossLight, 1f);

        yield return new WaitForSeconds(0.3f);

        gameObject.GetComponentInChildren<Animator>().Play("BossPortal_Activation");
        yield return new WaitForSeconds(1f);

        Managers.Resource.Instantiate("FX_BossPortal_A", transform);

        yield return new WaitForSeconds(2f);

        Managers.Resource.Instantiate("FX_BossPortal_B", transform);

        yield return new WaitForSeconds(1f);

        Vector3 originScale = Managers.Game.Player.transform.localScale;
        transform.GetChild(0).gameObject.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        Managers.Game.Player.transform.DOMoveY(0.8f, 0.2f).SetEase(Ease.Linear);

        yield return new WaitForSeconds(0.15f);

        Managers.Game.Player.gameObject.SetActive(false);

        yield return new WaitForSeconds(1.5f);

        Managers.Game.BossRoom.GetComponent<PortalController>().UsePortal();

        //Managers.Game.DirectionalLight.color= originLightColor;
        yield return new WaitForSeconds(0.2f);
        Managers.Game.Player.transform.localScale = originScale;
        Managers.Game.Player.SetIdleState(Define.MoveDir.Up);
        Managers.Game.Player.gameObject.SetActive(true);
        Managers.Game.OnDirect = false;
        Managers.Resource.Destroy(gameObject);
    }
}
