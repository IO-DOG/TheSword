using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Pillar : MonoBehaviour
{
    public int _pillarIndex_forActive = 0;

    GameObject _pillar;

    public void Open()
    {
        Managers.Data.PillarActiveOff(_pillarIndex_forActive);
        Managers.Game.SaveGame();
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
