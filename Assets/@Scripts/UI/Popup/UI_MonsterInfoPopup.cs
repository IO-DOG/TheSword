using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_MonsterInfoPopup : UI_Popup
{
    #region Enum
    enum Images
    {
        BGImage,
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
            GetComponentsInChildren<UnityEngine.UI.Image>()[0].GetComponent<RectTransform>().anchoredPosition = _position;
            //GetImage((int)Images.BGImage).gameObject.GetComponent<RectTransform>().anchoredPosition = Input.mousePosition;
        }
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        #endregion

        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopupUI();
        }
    }
}
