using DG.Tweening;
using Febucci.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_StageNamePopup : UI_Popup
{
    float _duration = Define.STAGE_NAME_DURATION;

    // 흰 글자만 맵 한가운데에 떠서 밝은 바닥에서는 희미했다 — 뒤에 반투명 검은 띠를 깐다. 글자·장식선과 함께 오르내린다.
    // 띠는 글자(약 -36~+17)와 장식선(약 -64~-44)을 덮는 화면 폭 전체 (캔버스 단위, 가운데 기준).
    const float BandAlpha = 0.55f;
    const float BandY = -20f;
    const float BandHeight = 112f;
    // 짧게 뜨고 짧게 진다. 예전에는 1초에 걸쳐 올라와 다 보이자마자 1초에 걸쳐 내려갔고, 그 뒤 duration 을 한 번 더
    // 기다려서 계단 하나에 2초(연출 층은 3초) 동안 화면 가운데를 가렸다. 글자가 사라지기 시작하는 때(duration)는 그대로다.
    const float ShowSeconds = 0.3f;
    const float HideSeconds = 0.6f;
    // 글자도 늘 반투명이었다: Text Animator 의 기본 fade 가 한 글자에 1초라, 다 나타나기 전에 사라지기 시작했다.
    // 한 글자 0.3초로 줄이고 사라지는 차례도 세 배 빠르게 — 서른 글자 이름도 HideSeconds 안에 다 사라진다.
    const string TextFade = "fade d=0.3";
    const float VanishSpeed = 3f;
    Image _band;

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

        _band = CodeUI.NewImage(transform, "StageNameBand", null, new Color(0f, 0f, 0f, 0f));
        RectTransform band = _band.rectTransform;
        band.anchorMin = new Vector2(0f, 0.5f);
        band.anchorMax = new Vector2(1f, 0.5f);
        band.anchoredPosition = new Vector2(0f, BandY);
        band.sizeDelta = new Vector2(0f, BandHeight);
        band.SetAsFirstSibling();      // 글자와 장식선 뒤

        TextAnimator_TMP animator = GetText((int)Texts.StageNameText).GetComponent<TextAnimator_TMP>();
        if (animator != null)
        {
            animator.DefaultAppearancesTags = new[] { TextFade };
            animator.DefaultDisappearancesTags = new[] { TextFade };
        }
        TypewriterByCharacter writer = GetComponentInChildren<TypewriterByCharacter>();
        if (writer != null)
            writer.disappearanceSpeedMultiplier = VanishSpeed;
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
        FadeDeco(1f, ShowSeconds);
        yield return new WaitForSeconds(duration);

        // && 가 아니라 || 여야 한다. this 가 이미 파괴됐는데 gameObject 를 만지면
        // MissingReferenceException 이 나고, 이 코루틴을 돌리던 워프가 같이 죽는다.
        if (this == null)
            yield break;

        TypewriterByCharacter writer = gameObject.GetComponentInChildren<TypewriterByCharacter>();
        if (writer != null)
            writer.StartDisappearingText();
        FadeDeco(0f, HideSeconds);

        yield return new WaitForSeconds(HideSeconds);
        if (this == null)
            yield break;

        // 제 자신을 닫는다. 그 사이 다른 창(전투창 등)이 위에 올라와 있어도 닫힌다 —
        // 예전에는 맨 위가 아니면 조용히 실패해서 보이지 않는 창이 스택에 남아 휠 줌을 막았다.
        Managers.UI.ClosePopupUI(this);
        if (Managers.UI.StageNamePopup == this)
            Managers.UI.StageNamePopup = null;
    }

    // 장식선 셋과 띠를 함께 올리거나 내린다. 앞의 트윈은 지운다 — 계단을 빨리 넘어 다시 뜨면
    // 내려가던 트윈과 올라오는 트윈이 같은 그림을 두고 다툰다.
    void FadeDeco(float alpha, float seconds)
    {
        foreach (Images part in new[] { Images.StageNameStart, Images.StageNameLine, Images.StageNameEnd })
        {
            Image image = GetImage((int)part);
            image.DOKill();
            image.DOFade(alpha, seconds).SetLink(gameObject);
        }
        // 띠는 질 때 늦게 걷힌다(InQuad). 기본(OutQuad)이면 0.3초 만에 1/4 로 옅어져 아직 사라지는 글자가 밝은 바닥에 떴다.
        _band.DOKill();
        _band.DOFade(alpha * BandAlpha, seconds).SetEase(alpha > 0f ? Ease.OutQuad : Ease.InQuad).SetLink(gameObject);
    }
}
