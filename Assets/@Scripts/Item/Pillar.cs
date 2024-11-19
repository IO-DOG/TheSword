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
    }

    IEnumerator WaitAndOpen(float time)
    {
        Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_XDamping = 1f;
        Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_YDamping = 1f;
        Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().Follow = transform;
        yield return new WaitForSeconds(time);
        _pillar = gameObject.GetComponentInChildren<Animator>().gameObject;
        _pillar.GetComponent<Animator>().Play("Pillar");
        yield return new WaitForSeconds(2f);
        _pillar.SetActive(false);
        Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().Follow = Managers.Game.Player.transform;
        yield return new WaitForSeconds(2f);
        Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_XDamping = 0f;
        Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_YDamping = 0f;
    }
}
