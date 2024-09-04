using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_MagicalSwordCheckPopup : UI_Popup
{
    #region Enum
    enum Images
    {
        MagicalSwordCheckBox,
    }

    enum Texts
    {
        MagicalSwordCheckText,
    }

    enum Buttons
    {
        YesBtn,
    }

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindButton(typeof(Buttons));
        #endregion

        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesPointerEnter, type: Define.UIEvent.PointerEnter);
        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesClick, type: Define.UIEvent.Click);
        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesPointerExit, type: Define.UIEvent.PointerExit);

        GetText((int)Texts.MagicalSwordCheckText).text = Managers.GetString(Managers.Data.ScriptDic[Define.SWOARD_ALERT].id);

        Managers.Game.OnDirect = true;

        YesPointerExit();

        return true;
    }

    #region Pointer Interaction
    void YesPointerEnter()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("OnlyYesMouseOver");
    }

    void YesClick()
    {
        Managers.Game.OnDirect = false;

        Managers.Directing.Events.CoStartContractSword();
        ClosePopupUI();
    }

    void YesPointerExit()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("OnlyYesIdle");
    }
    #endregion
}
