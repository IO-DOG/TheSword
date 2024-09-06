using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_StatusInfo : UI_Base
{
    #region Enum
    enum Images
    {

    }

    enum Texts
    {
        StatNameText,
        StatValueText,
    }
    #endregion

    public TMP_Text _statNameText;
    public TMP_Text _statValueText;
    public int _equipId, _seq;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindText(typeof(Texts));
        #endregion

        _statNameText = GetText((int)Texts.StatNameText);
        _statValueText = GetText((int)Texts.StatValueText);

        return true;
    }


    /// <summary>
    ///         public float ATK { get; set; }
    ///         public float DEF { get; set; }
    ///         public float HP { get; set; }
    ///         public float ASPD { get; set; }
    ///         public float DSPD { get; set; }
    ///         public float CRI { get; set; }
    ///         public float CRIATK { get; set; }
    ///         public float MSPD { get; set; }
    ///         이 순서로 seq 값이 씌워짐. seq는 0부터.
    /// </summary>
    /// <param name="equipId"></param>
    /// <param name="seq"></param>
    public void Refresh(int equipId, int seq)
    {
        switch (seq)
        {
            case 0:
                _statNameText.text = "ATK";
                _statValueText.text = Managers.Data.EquipDic[equipId].ATK.ToString();
                break;
            case 1:
                _statNameText.text = "DEF";
                _statValueText.text = Managers.Data.EquipDic[equipId].DEF.ToString();
                break;
            case 2:
                _statNameText.text = "HP";
                _statValueText.text = Managers.Data.EquipDic[equipId].HP.ToString();
                break;
            case 3:
                _statNameText.text = "ATK \n SPEED";
                _statValueText.text = Managers.Data.EquipDic[equipId].ASPD.ToString();
                break;
            case 4:
                _statNameText.text = "DEF \n SPEED";
                _statValueText.text = Managers.Data.EquipDic[equipId].DSPD.ToString();
                break;
            case 5:
                _statNameText.text = "CRI";
                _statValueText.text = Managers.Data.EquipDic[equipId].CRI.ToString();
                break;
            case 6:
                _statNameText.text = "CRI \n ATK";
                _statValueText.text = Managers.Data.EquipDic[equipId].CRIATK.ToString();
                break;
            case 7:
                _statNameText.text = "MSPD";
                _statValueText.text = Managers.Data.EquipDic[equipId].MSPD.ToString();
                break;
            default:
                break;
        }
    }
}
