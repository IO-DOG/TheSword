using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StageNamePopup : UI_Popup
{
    float _duration = 3f;

    enum Texts
    {
        StageNameText,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindText(typeof(Texts));
        #endregion

        StartCoroutine(PlayAndDestory());
        GetText((int)Texts.StageNameText).text = Managers.GetString(Managers.Data.ScriptDic[(int)Define.STAGE_NAME + Managers.Game.CurPlayerData.CurStageid].id);

        return true;
    }

    IEnumerator PlayAndDestory()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(_duration);

        yield return waitForSeconds;

        gameObject.GetComponentInChildren<TypewriterByCharacter>().StartDisappearingText();
    }
}
