using DG.Tweening;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StageNamePopup : UI_Popup
{
    float _duration = Define.STAGE_NAME_DURATION;

    enum Images
    {
        StageNameStart,
        StageNameLine,
        StageNameEnd,
    }

    enum Texts
    {
        StageNameText,
    }

    private void Awake()
    {
        #region Bind
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;


        //StartCoroutine(PlayAndDestory());
        //GetText((int)Texts.StageNameText).text = Managers.GetString(Managers.Data.ScriptDic[(int)Define.STAGE_NAME + Managers.Game.PlayerData.CurStageid].id);

        return true;
    }

    public void SetStageName()
    {
        GetText((int)Texts.StageNameText).text = Managers.GetString(Managers.Data.ScriptDic[(int)Define.STAGE_NAME + Managers.Game.PlayerData.CurStageid].id);
    }

    public IEnumerator HideStageNamePopup(float duration)
    {
        GetImage((int)Images.StageNameStart).DOFade(1f, 1f);
        GetImage((int)Images.StageNameLine).DOFade(1f, 1f);
        GetImage((int)Images.StageNameEnd).DOFade(1f, 1f);
        yield return new WaitForSeconds(duration);

        if (this == null && gameObject == null)
        {

        }
        else
        {
            gameObject.GetComponentInChildren<TypewriterByCharacter>().StartDisappearingText();
            GetImage((int)Images.StageNameStart).DOFade(0f, 1f);
            GetImage((int)Images.StageNameLine).DOFade(0f, 1f);
            GetImage((int)Images.StageNameEnd).DOFade(0f, 1f);

            yield return new WaitForSeconds(duration);

            if (Managers.UI.StageNamePopup != null)
            {
                Managers.UI.ClosePopupUI(Managers.UI.StageNamePopup);
                Managers.UI.StageNamePopup = null;
            }
        }
        
    }
}
