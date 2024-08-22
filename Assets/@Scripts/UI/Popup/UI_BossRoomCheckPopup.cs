using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_BossRoomCheckPopup : UI_Popup
{
    #region Enum
    enum Images
    {
        BossRoomCheckBox,
    }

    enum Texts
    {
        BossRoomCheckText,
    }

    enum Buttons
    {
        YesBtn,
        NoBtn,
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
        GetButton((int)Buttons.NoBtn).gameObject.BindEvent(NoPointerEnter, type: Define.UIEvent.PointerEnter);


        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesPointerExit, type: Define.UIEvent.PointerExit);
        GetButton((int)Buttons.NoBtn).gameObject.BindEvent(NoPointerExit, type: Define.UIEvent.PointerExit);

        YesPointerExit();
        NoPointerExit();

        return true;
    }

    void YesPointerEnter()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("YesMouseOver");
    }

    void NoPointerEnter()
    {
        GetButton((int)Buttons.NoBtn).gameObject.GetComponent<Animator>().Play("NoMouseOver");
    }
    void YesPointerExit()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("YesIdle");
    }

    void NoPointerExit()
    {
        GetButton((int)Buttons.NoBtn).gameObject.GetComponent<Animator>().Play("NoIdle");
    }
}
