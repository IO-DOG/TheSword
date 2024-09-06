using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UI_StatInfo : UI_Base
{
    #region Enum
    enum Texts
    {
        StatDescText,
    }
    #endregion

    public Vector3 _position;
    public Vector3 Position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            Debug.Log("change monster info position");
            GetComponentsInChildren<UnityEngine.UI.Image>()[0].GetComponent<RectTransform>().anchoredPosition = _position +
                new Vector3((float)(GetComponentsInChildren<BoxCollider>()[0].bounds.max.x - GetComponentsInChildren<BoxCollider>()[0].bounds.min.x) / 2 + 50, 0, 0);
            Debug.Log($"GetComponentsInChildren<BoxCollider>()[0].bounds.max.x : {GetComponentsInChildren<BoxCollider>()[0].bounds.max.x}");
            Debug.Log($"GetComponentsInChildren<BoxCollider>()[0].bounds.min.x : {GetComponentsInChildren<BoxCollider>()[0].bounds.min.x}");
            //GetImage((int)Images.BGImage).gameObject.GetComponent<RectTransform>().anchoredPosition = Input.mousePosition;
        }
    }


    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindText(typeof(Texts));

        return true;
    }
}
