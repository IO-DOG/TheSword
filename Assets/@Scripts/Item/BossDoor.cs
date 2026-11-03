using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class BossDoor : PortalController
{
   public void CoStartPlayEffect()
    {
        Managers.Game.SaveGame();

        StartCoroutine(PlayEffect());
    }

    IEnumerator PlayEffect()
    {
        Managers.Game.OnDirect = true;
        Color originLightColor = Managers.Game.DirectionalLight.color;
        Managers.Game.DirectionalLight.DOColor(Define.BossLight, 1f);

        yield return new WaitForSeconds(0.3f);

        // Sound
        Managers.Sound.Play(Define.Sound.Effect, "BossGate_Event");

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
        Managers.Game.Player.gameObject.SetActive(false);
        UsePortal();


        yield return new WaitForSeconds(0.2f);
        Managers.Game.Player.transform.localScale = originScale;
        Managers.Game.Player.SetIdleState(Define.MoveDir.Up);
        Managers.Game.Player.gameObject.SetActive(true);
        Managers.Game.OnDirect = false; 
        Managers.Game.DirectionalLight.color = originLightColor;

        // Sound — 킹 슬라임이 이미 쓰러졌으면(워프로 돌아와 다시 들어간다) 보스곡을 틀지 않는다. 같은 챕터 계단은 곡을 되돌리지 않아
        // 그 곡이 20층까지 돌았다. 이 문은 손수 만든 2층에만 있다(보스 포탈 셀 16 을 쓰는 층이 그 하나다).
        if (Managers.Data.MonsterActiveDic.TryGetValue(GameManager.KingSlimeActiveIndex, out bool kingAlive) == false || kingAlive)
            Managers.Sound.Play(Define.Sound.Bgm, "Boss_Entry_BGM");

        yield return new WaitForSeconds(5f);

        // 보스방으로 옮기는 포탈 코루틴(삽화 포함)도 이 문 위에서 돈다. 삽화는 2 + 1~2 + 1초라 느린 프레임이면
        // 5.2초를 넘기는데, 그 전에 끄면 같이 죽어 방에 못 들어가고 OnInteract·OnInputLock 이 켜진 채 굳었다.
        // 그 코루틴이 UsePortal 에서 켜고 LoadingAndWarp 에서 끄는 OnInteract 가 내려갈 때까지 기다린다.
        yield return new WaitWhile(() => Managers.Game.OnInteract);

        gameObject.SetActive(false);
    }
}
