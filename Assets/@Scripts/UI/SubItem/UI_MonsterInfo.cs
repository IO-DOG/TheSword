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
        //MonsterClassText,
        MonsterAttackText,
        MonsterDefenseText,
        MonsterHPText,
        MonsterDescText,
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
            GetComponentsInChildren<UnityEngine.UI.Image>()[0].GetComponent<RectTransform>().anchoredPosition = _position +
                new Vector3((float)(GetComponentsInChildren<BoxCollider>()[0].bounds.max.x - GetComponentsInChildren<BoxCollider>()[0].bounds.min.x) / 2 + 50, 0, 0);
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
        Debug.Log(Managers.Data.MonsterDic[id].MonsterNameId);
        GetText((int)Texts.MonsterNameText).text = Managers.GetString(Managers.Data.MonsterDic[id].MonsterNameId);
        //GetText((int)Texts.MonsterClassText).text = "특성 : " + Managers.Data.MonsterClassDic[Managers.Data.MonsterDic[id].Feature].ClassName;
        GetText((int)Texts.MonsterAttackText).text = Managers.Data.MonsterDic[id].Attack.ToString();
        GetText((int)Texts.MonsterDefenseText).text = Managers.Data.MonsterDic[id].Defence.ToString();
        GetText((int)Texts.MonsterHPText).text = Managers.Data.MonsterDic[id].MaxHP.ToString();
        GetText((int)Texts.MonsterDescText).text = Managers.GetString(Managers.Data.MonsterDic[id].MonsterDescId);
    }

    private void Update()
    {
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            Destroy(gameObject);
        }
    }
}
