using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GuidePopup : UI_Popup
{
    #region Enum
    enum Images
    {
        GuideImage,
    }

    enum Texts
    {
        GuideText
    }
    #endregion

    public Animator anim = null;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        #endregion

        anim = GetImage((int)Images.GuideImage).gameObject.GetComponent<Animator>();

        return true;
    }

    public void SetInfo(int index)
    {
        GetText((int)Texts.GuideText).text = Managers.GetString(index);

        if (index == Define.GUIDE_BATTLE)
            anim.Play("UI_GuideBattle");
        else if (index == Define.GUIDE_RECOVERY)
            anim.Play("UI_GuideRecovery");
        else if (index == Define.GUIDE_LEVER)
            anim.Play("UI_GuideLever");
        else if (index == Define.GUIDE_KEY)
            anim.Play("UI_GuideKey");
    }
}
