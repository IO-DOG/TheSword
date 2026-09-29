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
        // 보스 등장 연출이 부른다. 여기서 터지면 그 연출이 통째로 끊긴다 — 이름만 비우고 넘어간다.
        MonsterController boss = Managers.Game.GetBoss();
        Data.MonsterData data;
        if (boss != null && Managers.Data.MonsterDic.TryGetValue(boss.id, out data))
            GetText((int)Texts.BossNameText).text = Managers.GetString(data.MonsterNameId);
    }

    public IEnumerator HideBossNamePopup(float duration)
    {
        GetImage((int)Images.BossNameStart).DOFade(1f, 1f).SetLink(gameObject);
        GetImage((int)Images.BossNameLine).DOFade(1f, 1f).SetLink(gameObject);
        GetImage((int)Images.BossNameEnd).DOFade(1f, 1f).SetLink(gameObject);
        yield return new WaitForSeconds(duration);

        // && 가 아니라 || 여야 한다. this 가 이미 파괴됐는데 gameObject 를 만지면
        // MissingReferenceException 이 나고, 이 코루틴을 돌리던 쪽이 같이 죽는다.
        if (this == null || gameObject == null)
        {

        }
        else
        {
            TypewriterByCharacter writer = gameObject.GetComponentInChildren<TypewriterByCharacter>();
            if (writer != null)
                writer.StartDisappearingText();
            GetImage((int)Images.BossNameStart).DOFade(0f, 1f).SetLink(gameObject);
            GetImage((int)Images.BossNameLine).DOFade(0f, 1f).SetLink(gameObject);
            GetImage((int)Images.BossNameEnd).DOFade(0f, 1f).SetLink(gameObject);

            yield return new WaitForSeconds(duration);

            // 맨 위가 아니어도 제 자신을 닫는다 (UI_StageNamePopup 과 같다).
            Managers.UI.ClosePopupUI(this);
            if (Managers.UI.BossNamePopup == this)
                Managers.UI.BossNamePopup = null;
        }

    }
}
