using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 크레딧 뒤의 엔딩 씬 (바이블 6.7, R5). "Thank You For Playing" 그림(Ending)은 새벽(dawn) 결말 뒤에만 한 번 띄운다.
/// 봉인·동반 결말 뒤에는 까만 화면에 크레딧의 마지막 인사만 몇 초. 그다음 타이틀로 — Managers.Scene 을 거쳐야
/// Managers.Clear 가 돌아 지난 판의 창·소리가 남지 않는다.
/// </summary>
public class UI_EndingScene : UI_Scene
{
    enum Images
    {
        Fade,
        Image,
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindImage(typeof(Images));
        // 프리팹 캔버스는 기준(SetCanvas 가 넣는다)을 가로로만 맞춘다 — 다른 창처럼 가로세로 반씩.
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
            scaler.matchWidthOrHeight = 0.5f;

        StartCoroutine(StartDirecting());

        return true;
    }

    IEnumerator StartDirecting()
    {
        StoryEnding ending = StoryDirector.LastEnding;
        Image fade = GetImage((int)Images.Fade);
        Image art = GetImage((int)Images.Image);
        StoryUI.Stretch(fade.rectTransform);
        fade.color = Color.black;
        StoryUI.Stretch(art.rectTransform);
        art.preserveAspect = true;

        // 씬을 옮기며 소리가 다 멎었다. 크레딧의 곡을 잇는다 (dawn·seal 타이틀곡, hold 마검 조우곡).
        Managers.Sound.FadeAndPlayBGM(ending == StoryEnding.Hold ? "EgoSword_Encounter_Event" : "MainTitle_BGM", 2f);

        if (ending == StoryEnding.Dawn)
        {
            // 크레딧의 마지막 인사와 같은 인사가 그림에 박혀 있다 — 잠깐 암전을 둔 뒤 띄운다.
            art.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            yield return fade.DOFade(0f, 2f).SetLink(gameObject).WaitForCompletion();
            yield return CoHold(8f);
            yield return fade.DOFade(1f, 2f).SetLink(gameObject).WaitForCompletion();
        }
        else
        {
            art.gameObject.SetActive(false);
            TMP_Text line = StoryUI.NewText(transform, "LastLine", StoryUI.ProseFont, 60f, TextAlignmentOptions.Center);
            StoryUI.Stretch(line.rectTransform);
            int id = StoryDirector.LastCreditsLineId();
            line.text = id != 0 ? Managers.GetString(id) : "";
            line.color = new Color(1f, 1f, 1f, 0f);
            yield return line.DOFade(1f, 1.5f).SetLink(gameObject).WaitForCompletion();
            yield return CoHold(4f);
            yield return line.DOFade(0f, 1.5f).SetLink(gameObject).WaitForCompletion();
        }

        Managers.Scene.LoadScene(Define.Scene.TitleScene);
    }

    // 기다린다. 2초가 지나면 아무 키로나 넘길 수 있다.
    static IEnumerator CoHold(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            if (t > 2f && (Input.anyKeyDown || StoryUI.Auto))
                yield break;
            yield return null;
        }
    }
}
