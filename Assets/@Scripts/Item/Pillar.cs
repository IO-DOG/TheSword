using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class Pillar : MonoBehaviour
{
    public int _pillarIndex_forActive = 0;

    public GameObject _pillar;

    public void Open(float time)
    {
        Managers.Data.PillarActiveDic[_pillarIndex_forActive] = false;
        Debug.Log("Open");
        StartCoroutine(WaitAndOpen(time));

        // 레버를 당긴 것은 되돌릴 수 없는 진행이다. 바로 남긴다.
        // 안 남기면 다음에 불러올 때 기둥이 도로 서 있고,
        // 레버는 이미 벽이 되어 있어서 보스방에 영영 못 들어간다.
        Managers.Game.SaveGame();
    }

    public void SetInActive()
    {
        if (_pillar != null)
            _pillar.SetActive(false);

        // 콜라이더가 _pillar 바깥에 붙어 있으면 보이지만 않을 뿐 길은 여전히 막힌다.
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }

    IEnumerator WaitAndOpen(float time)
    {
        // 이 연출이 중간에 죽으면 기둥이 선 채로 남고 OnDirect 도 켜진 채 남는다.
        // 그러면 보스방으로 가는 유일한 길이 막혀 게임이 진행 불가가 된다.
        // 카메라 참조는 하나라도 없을 수 있으니 전부 널 안전하게 잡는다.
        CinemachineVirtualCamera vcam = Managers.Game.MainCamera != null
            ? Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>() : null;
        CinemachineTransposer transposer = vcam != null
            ? vcam.GetCinemachineComponent<CinemachineTransposer>() : null;

        Managers.Game.OnDirect = true;

        if (transposer != null)
        {
            transposer.m_XDamping = 0.3f;
            transposer.m_YDamping = 0.3f;
        }
        if (vcam != null)
            vcam.Follow = transform;

        yield return new WaitForSeconds(time);

        Animator anim = gameObject.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            _pillar = anim.gameObject;
            anim.Play("Pillar");
        }
        yield return new WaitForSeconds(0.7f);

        // 애니메이터를 못 찾았어도 기둥은 반드시 치운다 — 길이 먼저다.
        SetInActive();

        if (vcam != null && Managers.Game.Player != null)
            vcam.Follow = Managers.Game.Player.transform;
        yield return new WaitForSeconds(1f);

        if (transposer != null)
        {
            transposer.m_XDamping = 0f;
            transposer.m_YDamping = 0f;
        }
        Managers.Game.OnDirect = false;
    }
}
