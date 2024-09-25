using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pillar : MonoBehaviour
{
    public int _pillarIndex_forActive = 0;

    public GameObject _pillar;

    public void Open()
    {
        Managers.Data.PillarActiveDic[_pillarIndex_forActive] = false;
        Debug.Log("Open");

        _pillar = gameObject.GetComponentInChildren<Animator>().gameObject;
        _pillar.GetComponent<Animator>().Play("Pillar");
        StartCoroutine(SetActiveFalse());
    }

    IEnumerator SetActiveFalse()
    {
        yield return new WaitForSeconds(2f);
        _pillar.SetActive(false);
    }
}
