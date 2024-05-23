using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UI_MonsterInfo : UI_Base
{
    #region Enum
    enum Images
    {
        BGImage,
    }

    enum Texts
    {
        MonsterNameText,
        MonsterAttackText,
        MonsterDefenseText,
        MonsterHPText,
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

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        SetInfo();

        return true;
    }

    void SetInfo()
    {
        int id = gameObject.transform.parent.GetComponent<MonsterController>().id;
        GetText((int)Texts.MonsterNameText).text = "Name : " + Managers.Data.MonsterDic[id].Name;
        GetText((int)Texts.MonsterAttackText).text = "Attack : " + Managers.Data.MonsterDic[id].Attack.ToString();
        GetText((int)Texts.MonsterDefenseText).text = "Defense : " + Managers.Data.MonsterDic[id].Defence.ToString();
        GetText((int)Texts.MonsterHPText).text = "HP : " + Managers.Data.MonsterDic[id].MaxHP.ToString();
    }

    private void Update()
    {
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            Destroy(gameObject);
        }
    }
}
