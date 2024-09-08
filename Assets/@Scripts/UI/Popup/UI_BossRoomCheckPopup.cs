using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
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

        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesClick, type: Define.UIEvent.Click);
        GetButton((int)Buttons.NoBtn).gameObject.BindEvent(NoClick, type: Define.UIEvent.Click);

        GetButton((int)Buttons.YesBtn).gameObject.BindEvent(YesPointerExit, type: Define.UIEvent.PointerExit);
        GetButton((int)Buttons.NoBtn).gameObject.BindEvent(NoPointerExit, type: Define.UIEvent.PointerExit);

        #region Popup init
        GetImage((int)Images.BossRoomCheckBox).color = new Color(1f, 1f, 1f, 0f);
        GetText((int)Texts.BossRoomCheckText).gameObject.SetActive(false);
        GetButton((int)Buttons.YesBtn).gameObject.SetActive(false);
        GetButton((int)Buttons.NoBtn).gameObject.SetActive(false);
        GetText((int)Texts.BossRoomCheckText).text = Managers.GetString(Managers.Data.ScriptDic[Define.BOSS_ALERT].id);
        #endregion

        StartCoroutine(PopupAnimation());

        Managers.Game.OnDirect = true;

        return true;
    }

    IEnumerator PopupAnimation()
    {
        GetImage((int)Images.BossRoomCheckBox).DOFade(1f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        GetText((int)Texts.BossRoomCheckText).gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        GetButton((int)Buttons.YesBtn).gameObject.SetActive(true);
        GetButton((int)Buttons.NoBtn).gameObject.SetActive(true);

        YesPointerExit();
        NoPointerExit();
    }

    #region Pointer interaction
    void YesPointerEnter()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("YesMouseOver");
    }

    void NoPointerEnter()
    {
        GetButton((int)Buttons.NoBtn).gameObject.GetComponent<Animator>().Play("NoMouseOver");
    }

    void YesClick()
    {
        Managers.Game.BossRoom.parent.GetComponentInChildren<BossDoor>().CoStartPlayEffect();
        ClosePopupUI();
    }

    void NoClick()
    {
        Managers.Game.OnDirect = false;
        ClosePopupUI();
    }

    void YesPointerExit()
    {
        GetButton((int)Buttons.YesBtn).gameObject.GetComponent<Animator>().Play("YesIdle");
    }

    void NoPointerExit()
    {
        GetButton((int)Buttons.NoBtn).gameObject.GetComponent<Animator>().Play("NoIdle");
    }
    #endregion
}
