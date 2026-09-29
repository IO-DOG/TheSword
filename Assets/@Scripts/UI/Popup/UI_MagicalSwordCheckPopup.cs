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

        Managers.Game.OnConversation = true;
        _locked = true;

        YesPointerExit();

        return true;
    }

    // Esc 로는 닫히지 않는다. 계약(O)을 눌러야 연출이 이어진다.
    public override bool OnEscape() => true;

    // 이 창이 건 대화 잠금. 어떤 길로 사라지든 풀고 간다.
    bool _locked;

    void OnDestroy()
    {
        if (_locked)
            Managers.Game.OnConversation = false;
    }

    #region Pointer Interaction
    void YesPointerEnter()
    {
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Choice_SFX");

        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("OnlyYesMouseOver");
    }

    void YesClick()
    {
        Managers.Game.OnDirect = false;
        Managers.Sound.Play(Define.Sound.Effect, "ButtonUI_Ok_SFX");

        Managers.Game.OnConversation = false;
        _locked = false;
        Managers.Directing.Events.CoStartContractSword();
        ClosePopupUI();
    }

    void YesPointerExit()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("OnlyYesIdle");
    }
    #endregion
}
