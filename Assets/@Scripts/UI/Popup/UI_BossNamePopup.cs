using DG.Tweening;
using Febucci.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_BossNamePopup : UI_Popup
{
    enum Images
    {
        BossNameStart,
        BossNameLine,
        BossNameEnd,
    }

    enum Texts
    {
        BossNameText,
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

    public void SetBossName()
    {
        int bossId = Managers.Game.GetBoss().GetComponent<MonsterController>().id;
        GetText((int)Texts.BossNameText).text = Managers.GetString(Managers.Data.ScriptDic[Managers.Data.MonsterDic[bossId].MonsterNameId].id);
    }

    public IEnumerator HideBossNamePopup(float duration)
    {
        GetImage((int)Images.BossNameStart).DOFade(1f, 1f);
        GetImage((int)Images.BossNameLine).DOFade(1f, 1f);
        GetImage((int)Images.BossNameEnd).DOFade(1f, 1f);
        yield return new WaitForSeconds(duration);

        if (this == null && gameObject == null)
        {

        }
        else
        {
            gameObject.GetComponentInChildren<TypewriterByCharacter>().StartDisappearingText();
            GetImage((int)Images.BossNameStart).DOFade(0f, 1f);
            GetImage((int)Images.BossNameLine).DOFade(0f, 1f);
            GetImage((int)Images.BossNameEnd).DOFade(0f, 1f);

            yield return new WaitForSeconds(duration);

            if (Managers.UI.BossNamePopup != null)
            {
                Managers.UI.ClosePopupUI(Managers.UI.BossNamePopup);
                Managers.UI.BossNamePopup = null;
            }
        }
        
    }
}
