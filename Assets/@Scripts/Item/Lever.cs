using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
    GameObject On;
    GameObject Off;
    GameObject lever;

    void Start()
    {
        On = GameObject.Find("Tilemap_IronLever_ON");
        Off = GameObject.Find("Tilemap_IronLever_OFF");
        lever = GameObject.Find("lever");
        On.SetActive(false);
    }

    public void Play()
    {
        Vector3 rotateAngle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z + 25f);
        Quaternion targetRotation = Quaternion.Euler(rotateAngle);

        lever.transform.DORotateQuaternion(targetRotation, 1.0f).OnComplete(()=>
        {
            On.SetActive(true);
            Off.SetActive(false);
        });
    }
}
