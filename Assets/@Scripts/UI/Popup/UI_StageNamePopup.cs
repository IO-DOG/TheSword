using DG.Tweening;
using Febucci.UI;
using TMPro;
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
        // 층 이름이 없어도 여기서 터지면 안 된다 — 이 팝업을 띄우는 쪽이
        // 포탈 워프 코루틴이라, 같이 죽으면 다음 층으로 넘어가지 못한다.
        //
        // HUD(UI_GameScene.Refresh)와 같은 이름을 쓴다. 예전에는 5000+층 을 따로 읽어서
        // 5층이 HUD 에서는 "이끼 낀 지하 묘소 5층", 여기서는 "가브 마을" 로 서로 달랐다.
        TMP_Text text = GetText((int)Texts.StageNameText);
        Data.StageInfoData info;
        if (text != null && Managers.Data.StageInfoDic.TryGetValue(Managers.Game.PlayerData.CurStageid, out info))
            text.text = Managers.GetString(info.DungeonNameScriptID);
    }

    public IEnumerator HideStageNamePopup(float duration)
    {
        GetImage((int)Images.StageNameStart).DOFade(1f, 1f).SetLink(gameObject);
        GetImage((int)Images.StageNameLine).DOFade(1f, 1f).SetLink(gameObject);
        GetImage((int)Images.StageNameEnd).DOFade(1f, 1f).SetLink(gameObject);
        yield return new WaitForSeconds(duration);

        // && 가 아니라 || 여야 한다. this 가 이미 파괴됐는데 gameObject 를 만지면
        // MissingReferenceException 이 나고, 이 코루틴을 돌리던 워프가 같이 죽는다.
        if (this == null)
        {

        }
        else
        {
            TypewriterByCharacter writer = gameObject.GetComponentInChildren<TypewriterByCharacter>();
            if (writer != null)
                writer.StartDisappearingText();
            GetImage((int)Images.StageNameStart).DOFade(0f, 1f).SetLink(gameObject);
            GetImage((int)Images.StageNameLine).DOFade(0f, 1f).SetLink(gameObject);
            GetImage((int)Images.StageNameEnd).DOFade(0f, 1f).SetLink(gameObject);

            yield return new WaitForSeconds(duration);

            // 제 자신을 닫는다. 그 사이 다른 창(전투창 등)이 위에 올라와 있어도 닫힌다 —
            // 예전에는 맨 위가 아니면 조용히 실패해서 보이지 않는 창이 스택에 남아 휠 줌을 막았다.
            Managers.UI.ClosePopupUI(this);
            if (Managers.UI.StageNamePopup == this)
                Managers.UI.StageNamePopup = null;
        }

    }
}
