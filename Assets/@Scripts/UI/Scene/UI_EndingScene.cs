using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 크레딧 뒤의 엔딩 씬 (바이블 6.7, R5). "Thank You For Playing" 그림(Ending)은 새벽(dawn) 결말 뒤에만 한 번 띄운다.
/// 봉인·동반 결말 뒤에는 까만 화면에 크레딧의 마지막 인사만 몇 초. 그다음 타이틀로 — 창·소리·풀을 비우고 간다
/// (Managers.Clear 와 같은 일. 이 씬에는 BaseScene 이 없어 그것을 그대로 부를 수 없다).
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
        art.rectTransform.localScale = Vector3.one;     // 프리팹은 0.5 — 화면에 맞춘 그림이 반으로 줄어 가운데에 작게 떴다
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

        yield return CoRunCard();

        // 엔딩 씬 파일에는 BaseScene(EndingScene)이 없고 이 UI 만 놓여 있다. Managers.Scene.LoadScene 과 Managers.Clear 는
        // 지금 씬(CurrentScene)부터 찾아서 여기서는 null 예외로 멈췄고, 결말 뒤 까만 화면에서 영영 못 나갔다.
        // 씬 없이 같은 것을 비운다.
        Managers.Sound.Clear();
        Managers.UI.Clear();
        Managers.Pool.Clear();
        SceneManager.LoadScene(nameof(Define.Scene.TitleScene));
    }

    // 판 카드 문구 (Tools/ui_text_parts/ledger.py)
    const int CardTitle = 500;      // 마검의 장부
    const int CardExtra = 501;      // 제단 {0}  넘친 회복 {1}
    const int CardScore = 502;      // 점수 {0}
    const int CardLower = 503;      // 낮을수록 좋다
    const int CardChapter = 504;    // {0}장
    const int AnyKey = 143;         // 아무 키나 누르세요 (Tools/ui_text.py)

    /// <summary>
    /// 판 카드 (기획 L2) — 끝낸 판의 장부: 예언·치름·기준, 제단에 바친 것과 넘친 회복, 점수(1000·치름/기준, 낮을수록 좋다),
    /// 챕터마다 네 띠의 별. 100층 계단으로 판을 끝낸 때만 뜬다(SwordLedger.TakeFinishedRun) — DebugPlay 로 튼 결말에는 없다.
    /// 기준을 매긴 싸움이 하나도 없으면(ParLoss 가 빈 자료) 적을 것이 없어 띄우지 않는다.
    /// 까만 화면 위에 떴다가 2초 뒤부터 아무 키로 넘긴다. 점수를 알리는 것(RunScored)은 계단이 이미 했다.
    /// </summary>
    IEnumerator CoRunCard()
    {
        LedgerState run = SwordLedger.TakeFinishedRun();
        if (run == null || run.Par <= 0)
            yield break;

        RectTransform box = CodeUI.Stretch(CodeUI.NewRect(transform, "RunCard"));
        CanvasGroup card = box.gameObject.AddComponent<CanvasGroup>();
        card.alpha = 0f;
        TMP_FontAsset prose = StoryUI.ProseFont, number = CodeUI.NumberFont;
        Color gold = new Color32(240, 210, 138, 255), soft = new Color32(174, 182, 200, 255), ink = new Color32(236, 236, 242, 255);

        float y = 380f;
        CardLine(box, prose, 72f, gold, ref y, 96f).text = Managers.GetString(CardTitle);
        if (Managers.Game.PlayerData.Mode == GameMode.Tower)
            CardLine(box, prose, 36f, new Color32(255, 128, 104, 255), ref y, 50f).text = Managers.GetString(UI_GameScene.TowerText);
        CardLine(box, number, 44f, ink, ref y, 64f).text =
            string.Format(Managers.GetString(UI_TallyPopup.TotalsText), run.Foretold, run.Paid, run.Par);
        CardLine(box, prose, 30f, soft, ref y, 46f).text = string.Format(Managers.GetString(CardExtra), run.AltarPaid, run.Spill);

        int score = SwordLedger.Score(run);
        if (score > 0)
        {
            y -= 16f;
            CardLine(box, number, 84f, gold, ref y, 104f).text = string.Format(Managers.GetString(CardScore), score);
            CardLine(box, prose, 28f, soft, ref y, 40f).text = Managers.GetString(CardLower);
        }

        y -= 24f;
        const int BandsPerChapter = 4;      // 챕터 20층 = 띠 넷
        for (int chapter = 0; chapter * BandsPerChapter < run.BandStars.Count; chapter++)
        {
            string stars = "";
            for (int b = chapter * BandsPerChapter; b < (chapter + 1) * BandsPerChapter && b < run.BandStars.Count; b++)
                stars += "   " + UI_TallyPopup.StarText(run.BandStars[b]);
            CardLine(box, number, 40f, ink, ref y, 56f).text = string.Format(Managers.GetString(CardChapter), chapter + 1) + stars;
        }

        TextMeshProUGUI hint = CodeUI.NewText(box, "AnyKey", prose, 26f, soft, TextAlignmentOptions.Center);
        CodeUI.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(1600f, 40f));
        hint.text = Managers.GetString(AnyKey);

        yield return card.DOFade(1f, 1f).SetLink(box.gameObject).WaitForCompletion();
        yield return CoHold(20f);
        yield return card.DOFade(0f, 1f).SetLink(box.gameObject).WaitForCompletion();
    }

    static TextMeshProUGUI CardLine(RectTransform box, TMP_FontAsset font, float size, Color color, ref float y, float height)
    {
        TextMeshProUGUI text = CodeUI.NewText(box, "Line", font, size, color, TextAlignmentOptions.Center);
        CodeUI.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1600f, height));
        y -= height;
        return CodeUI.Fit(text, size * 0.5f);
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
