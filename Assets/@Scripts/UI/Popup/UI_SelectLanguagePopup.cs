using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SelectLanguagePopup : UI_Popup
{

    #region Enum
    enum Buttons
    {
        Koarean,
        English,
        japan,
        China,
    }

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButton(typeof(Buttons));

        GetButton((int)Buttons.Koarean).gameObject.BindEvent(OnClickKorean);
        GetButton((int)Buttons.English).gameObject.BindEvent(OnClickEnglish);
        GetButton((int)Buttons.japan).gameObject.BindEvent(OnClickJapan);
        GetButton((int)Buttons.China).gameObject.BindEvent(OnClickChina);

        return true;
    }

    void OnClickKorean()
    {
        Managers.Game.ScriptType = Define.ScriptType.Kr;

        ClosePopupUI();
    }

    void OnClickEnglish()
    {
        Managers.Game.ScriptType = Define.ScriptType.En;

        ClosePopupUI();
    }

    void OnClickJapan()
    {
        Managers.Game.ScriptType = Define.ScriptType.Jp;

        ClosePopupUI();
    }

    void OnClickChina()
    {
        Managers.Game.ScriptType = Define.ScriptType.Cn;

        ClosePopupUI();
    }
}
