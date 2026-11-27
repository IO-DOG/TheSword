using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마검의 장부 (11-28) — 메뉴에서 여는 이번 판의 중간 결산. 판 카드(UI_EndingScene)는 결말에서야 뜨고 띠 결산(UI_TallyPopup)은
/// 띠를 닫는 그 순간뿐이라, 판 도중에는 지금까지 어떻게 치렀는지 볼 곳이 없었다. 지금까지의 예언·치름·기준과 점수, 제단·넘친 회복,
/// 걸린 시간·쓰러진 횟수, 챕터마다 네 띠의 별(이번 판)과 그 띠에서 받은 가장 많은 별(Records)을 보인다. 읽기만 한다 — 아무것도 바꾸지 않는다.
/// 문구는 판 카드와 같은 것(ledger.py 500~505)에 "이번 판"·"최고" 둘(506·507)을 더했다.
/// </summary>
public class UI_LedgerPopup : UI_Popup
{
    const int CardTitle = 500, CardExtra = 501, CardScore = 502, CardLower = 503, CardChapter = 504, CardTime = 505;
    const int ThisRun = 506, Best = 507;
    const int ContinueKey = 481;

    const float Width = 1000f, Pad = 32f;
    const float InputDelay = 0.15f;         // 메뉴 줄을 누른 그 입력으로 닫히지 않게
    const int BandsPerChapter = 4;

    static readonly Color Gold = new Color32(240, 210, 138, 255);
    static readonly Color Soft = new Color32(174, 182, 200, 255);
    static readonly Color Ink = new Color32(236, 236, 242, 255);

    float _opened;
    bool _closing;

    public static UI_LedgerPopup Show()
    {
        UI_LedgerPopup popup = new GameObject(nameof(UI_LedgerPopup), typeof(RectTransform)).AddComponent<UI_LedgerPopup>();
        Managers.UI.PushPopup(popup);
        popup.Init();
        popup.Build();
        return popup;
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;
        // 결산 창과 같다 — 높이를 1080 에 맞춰 어느 화면비에서도 창이 세로로 들어가게 한다.
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        scaler.enabled = false;
        scaler.enabled = true;
        return true;
    }

    void Build()
    {
        _opened = Time.unscaledTime;
        LedgerState run = SwordLedger.State;
        GameManager.CurPlayerData player = Managers.Game.PlayerData;
        TMP_FontAsset prose = CodeUI.ProseFont, number = CodeUI.NumberFont;

        Image dim = CodeUI.NewImage(transform, "Dim", null, new Color(0f, 0f, 0f, 0.55f));
        CodeUI.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        dim.gameObject.BindEvent(() =>
        {
            if (Time.unscaledTime - _opened >= 0.4f)
                Close();
        });

        Sprite frame = CodeUI.PrefabSprite("UI_InvenPopup", "Inventory_Popup32");
        Image panel = CodeUI.NewImage(transform, "Panel", frame, frame != null ? Color.white : new Color(0.07f, 0.08f, 0.12f, 0.97f), true);
        panel.raycastTarget = true;
        RectTransform root = panel.rectTransform;
        float y = -Pad;

        Line(root, prose, 44f, Gold, ref y, 60f).text = Managers.GetString(CardTitle);
        if (player.Mode == GameMode.Tower)
            Line(root, prose, 28f, new Color32(255, 128, 104, 255), ref y, 38f).text = Managers.GetString(UI_GameScene.TowerText);

        TextMeshProUGUI totals = Line(root, number, 30f, Ink, ref y, 44f);
        totals.fontSharedMaterial = CodeUI.Outlined(number, 0.2f);
        totals.text = string.Format(Managers.GetString(UI_TallyPopup.TotalsText), run.Foretold, run.Paid, run.Par);
        int score = SwordLedger.Score(run);
        if (score > 0)
            Line(root, number, 26f, Ink, ref y, 38f).text =
                string.Format(Managers.GetString(CardScore), score) + "  <color=#AEB6C8>" + Managers.GetString(CardLower) + "</color>";
        Line(root, prose, 26f, Soft, ref y, 36f).text = string.Format(Managers.GetString(CardExtra), run.AltarPaid, run.Spill);
        int seconds = Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(Managers.Game.PlayTime, PlayerPrefs.GetFloat("PLAYTIME", 0f))));
        Line(root, prose, 26f, Soft, ref y, 36f).text = string.Format(Managers.GetString(CardTime),
            $"{seconds / 3600}:{seconds % 3600 / 60:00}:{seconds % 60:00}", PlayerPrefs.GetInt("DEATHCOUNT", 0));

        // 별 합계: 이번 판(닫은 띠) / 지금까지의 최고(규칙마다)
        int sum = 0, max = 0;
        foreach (int s in run.BandStars)
        {
            sum += s;
            max += s > 0 ? 3 : 0;
        }
        y -= 10f;
        Line(root, number, 26f, Ink, ref y, 38f).text =
            $"{Managers.GetString(ThisRun)}  <color=#F0D28A>★ {sum}/{max}</color>      " +
            $"{Managers.GetString(Best)}  <color=#F0D28A>★ {Records.StarsTotal(player.Mode)}/{SwordLedger.BandCount * 3}</color>";

        y -= 6f;
        for (int chapter = 0; chapter * BandsPerChapter < SwordLedger.BandCount; chapter++)
        {
            string mine = "", best = "";
            for (int band = chapter * BandsPerChapter; band < (chapter + 1) * BandsPerChapter; band++)
            {
                // 아직 닫지 않은 띠는 빈 별 셋(어둡게), 기준이 없던 띠는 "-"(StarText)
                // 두 줄의 글자 크기가 달라도 띠마다 같은 자리에 오게 가로 자리를 박는다(TMP <pos>)
                string at = $"<pos={150 + (band - chapter * BandsPerChapter) * 190}>";
                mine += at + (band < run.BandStars.Count ? UI_TallyPopup.StarText(run.BandStars[band]) : "<color=#3A4050>☆☆☆</color>");
                best += at + UI_TallyPopup.StarText(Records.BandStarsBest(player.Mode, band));
            }
            Line(root, number, 34f, Ink, ref y, 46f).text = string.Format(Managers.GetString(CardChapter), chapter + 1) + mine;
            Line(root, number, 20f, Soft, ref y, 30f).text = Managers.GetString(Best) + best;
        }

        y -= 10f;
        TextMeshProUGUI keys = Line(root, prose, 26f, Soft, ref y, 34f, TextAlignmentOptions.Center);
        PromptText.Set(keys, ContinueKey);
        y -= Pad;

        CodeUI.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Width, -y));
        root.localScale = Vector3.one * CodeUI.PanelScale(new Vector2(Width, -y));
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_SFX");
    }

    TextMeshProUGUI Line(Transform root, TMP_FontAsset font, float size, Color color, ref float y, float height,
                         TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI text = CodeUI.NewText(root, "Line", font, size, color, align);
        CodeUI.Place(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Width - Pad * 2f, height));
        y -= height;
        return CodeUI.Fit(text, size * 0.5f);
    }

    void Update()
    {
        if (_closing || Managers.UI.TopPopup != this || Managers.UI.ClosedThisFrame || Time.unscaledTime - _opened < InputDelay)
            return;
        if (GameKeys.Down(KeyCode.Return) || GameKeys.Down(KeyCode.KeypadEnter) || GameKeys.Down(KeyCode.Space))
            Close();
    }

    public override bool OnEscape()
    {
        Close();
        return true;
    }

    void Close()
    {
        if (_closing)
            return;
        _closing = true;
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
    }
}
