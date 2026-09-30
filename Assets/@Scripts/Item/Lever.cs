using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
    public GameObject On;
    public GameObject Off;
    public GameObject lever;

    public int _leverIndex_forActive = 0;
    public bool _IsActive;

    public Tween Play(float time)
    {
        Vector3 rotateAngle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z + 25f);
        Quaternion targetRotation = Quaternion.Euler(rotateAngle);

        Tween tween = lever.transform.DORotateQuaternion(targetRotation, time);
        return tween;
    }

    public void SetInActive()
    {
        On.SetActive(false);
        Off.SetActive(true);
    }

    public void SetActive()
    {
        On.SetActive(true);
        Off.SetActive(false);
        _IsActive = true;
        transform.parent.gameObject.layer = (int)Define.Layer.Wall;
        foreach (Transform component in transform.parent.gameObject.transform)
        {
            component.gameObject.layer = (int)Define.Layer.Wall;
        }
    }

    public void Open()
    {
        // 당겼다는 표시를 먼저 남긴다. 체크포인트는 Pillar.Open 안에서 쓰이는데, 예전에는 그 뒤에 표시해서
        // 기둥은 열리고 레버는 안 당긴 채로 저장됐다 — 죽거나 불러오면 레버가 도로 서서 기둥 연출이 다시 돌았다.
        Managers.Data.LeverActiveDic[_leverIndex_forActive] = false;

        GameObject stage = gameObject.transform.parent.parent.parent.gameObject;
        Debug.Log(stage.name);
        foreach (Transform child in stage.transform)
        {
            if(child.GetComponentInChildren<Pillar>() != null)
            {
                child.GetComponentInChildren<Pillar>().Open(1.0f);
                StartCoroutine(Managers.Sound.CoPlay(Define.Sound.Effect, "Gimic_leverDown_SFX", 1, 0.2f));
            }
        }
    }
}
