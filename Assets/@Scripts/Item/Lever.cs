using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
    public GameObject On;
    public GameObject Off;
    public GameObject lever;

    public bool _IsActive = false;

    void Start()
    {
        if(_IsActive == false)
        {
            On.SetActive(false);
        }
        else
        {
            transform.parent.gameObject.layer = (int)Define.Layer.Wall;
            foreach (Transform component in transform.parent.gameObject.transform)
            {
                component.gameObject.layer = (int)Define.Layer.Wall;
            }
        }
    }

    public Tween Play(float time)
    {
        Vector3 rotateAngle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z + 25f);
        Quaternion targetRotation = Quaternion.Euler(rotateAngle);

        Tween tween = lever.transform.DORotateQuaternion(targetRotation, time);
        return tween;
    }

    public void SetActiveLight()
    {
        On.SetActive(true);
        Off.SetActive(false);
        _IsActive = true;
    }

    public void Open()
    {
        GameObject stage = gameObject.transform.parent.parent.parent.gameObject;
        Debug.Log(stage.name);
        foreach (Transform child in stage.transform)
        {
            if(child.GetComponentInChildren<Pillar>() != null)
            {
                child.GetComponentInChildren<Pillar>().Open();
                Managers.Data.LeverActiveOff();
            }
        }
    }
}
