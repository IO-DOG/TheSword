using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lever : MonoBehaviour
{
    GameObject On;
    GameObject Off;
    GameObject lever;

    public bool _IsActive = false;

    void Start()
    {
        On = GameObject.Find("Tilemap_IronLever_ON");
        Off = GameObject.Find("Tilemap_IronLever_OFF");
        lever = GameObject.Find("lever");
        On.SetActive(false);
    }

    public Tween Play()
    {
        Vector3 rotateAngle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z + 25f);
        Quaternion targetRotation = Quaternion.Euler(rotateAngle);

        Tween tween = lever.transform.DORotateQuaternion(targetRotation, 1.0f);
        return tween;
    }

    public void SetActiveLight()
    {
        On.SetActive(true);
        Off.SetActive(false);
        _IsActive = true;
    }
}
