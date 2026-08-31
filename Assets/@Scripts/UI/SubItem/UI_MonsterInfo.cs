using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UI_MonsterInfo : UI_Base
{
    #region Enum
    enum Images
    {
        BGImage,
    }

    enum Texts
    {
        MonsterNameText,
        //MonsterClassText,
        MonsterAttackText,
        MonsterDefenseText,
        MonsterHPText,
        MonsterDescText,
    }

    enum Objects
    {
        ScrollView,
        Content,
    }
    #endregion

    float mScrollSpeed = 10.1f;  // 스크롤 속도
    float mScrollDelay = 1f;  // 자동 스크롤 시작 딜레이
    int _mask = (1 << (int)Define.Layer.Monster | 1 << (int)Define.Layer.CItem | 1 << (int)Define.Layer.Wall | 1 << (int)Define.Layer.Default);

    public Vector3 _position;
    public Vector3 Position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
            if (_position.x < (Input.mousePosition.x - Screen.width / 2) / 2)
                GetComponentsInChildren<UnityEngine.UI.Image>()[0].GetComponent<RectTransform>().anchoredPosition = _position +
                    new Vector3((float)(GetComponentsInChildren<BoxCollider>()[0].bounds.max.x - GetComponentsInChildren<BoxCollider>()[0].bounds.min.x) / 2 + 50, 0, 0);
            else if (_position.x > (Input.mousePosition.x - Screen.width / 2) / 2)
                GetComponentsInChildren<UnityEngine.UI.Image>()[0].GetComponent<RectTransform>().anchoredPosition = _position -
                    new Vector3((float)(GetComponentsInChildren<BoxCollider>()[0].bounds.max.x - GetComponentsInChildren<BoxCollider>()[0].bounds.min.x) / 2 + 50, 0, 0);

            //GetImage((int)Images.BGImage).gameObject.GetComponent<RectTransform>().anchoredPosition = Input.mousePosition;
        }
    }

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(Objects));
        #endregion

        SetInfo();

        GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().velocity = Vector2.zero;
        StartCoroutine(CoAutoScroll());

        return true;
    }

    void SetInfo()
    {
        int id = gameObject.transform.parent.GetComponent<MonsterController>().id;
        int stageId = Managers.Game.PlayerData.CurStageid;

        //Debug.Log(Managers.Data.MonsterDic[id].MonsterNameId);
        GetText((int)Texts.MonsterNameText).text = Managers.GetString(Managers.Data.MonsterDic[id].MonsterNameId);
        //GetText((int)Texts.MonsterClassText).text = "특성 : " + Managers.Data.MonsterClassDic[Managers.Data.MonsterDic[id].Feature].ClassName;
        GetText((int)Texts.MonsterAttackText).text = (Managers.Data.StageInfoDic[stageId].ATK * Managers.Data.MonsterDic[id].Attack).ToString();
        GetText((int)Texts.MonsterDefenseText).text = (Managers.Data.StageInfoDic[stageId].DEF * Managers.Data.MonsterDic[id].Defence).ToString();
        GetText((int)Texts.MonsterHPText).text = Managers.Data.MonsterDic[id].MaxHP.ToString();
        GetText((int)Texts.MonsterDescText).text = Managers.GetString(Managers.Data.MonsterDic[id].MonsterDescId);

        ShowForecast(id, stageId);
    }

    #region 전투 비용
    // 아직 ScriptData 에 없는 줄들. 표에 들어오면 자동으로 번역이 쓰인다.
    // (133 "예상 피해" / 134 "쓰러진다" / 135 "못 이긴다". 102 "체력" 은 이미 있다.)
    const int SCRIPT_COST = 133;
    const int SCRIPT_DIE = 134;
    const int SCRIPT_NO_WIN = 135;
    const int SCRIPT_HP = 102;

    /// <summary>
    /// "이놈을 잡으면 체력이 얼마 줄어드는가" 한 줄. 이 창이 있어야 하는 이유다.
    ///
    /// 공격력·방어력·체력만 보여 주는 것으로는 판단할 수 없다 — 게이지식 전투라
    /// 공격 주기와 특성이 얽히기 때문이다. 그래서 숫자를 늘어놓는 대신 결과를 적는다.
    /// 셈은 BattleForecast 가 진짜 전투 코드를 그대로 돌려서 한다.
    ///
    /// 프리팹은 에디터에서만 고칠 수 있으니 새 오브젝트를 만들지 않고, 특성 표시를
    /// 접으면서 꺼 둔 채 남아 있던 MonsterClassText 를 되살려 쓴다(위 Texts enum 의
    /// 주석 처리된 줄이 그것이다). 자리는 설명 칸을 그만큼 줄여서 낸다.
    /// </summary>
    void ShowForecast(int id, int stageId)
    {
        TMP_Text line = null;
        // 꺼져 있는 오브젝트라 Util.FindChild 로는 못 찾는다(비활성 자식을 훑지 않는다).
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == "MonsterClassText")
            {
                line = text;
                break;
            }
        }

        if (line == null)
        {
            Debug.LogWarning("[MonsterInfo] 전투 비용을 적을 자리(MonsterClassText)가 없다");
            return;
        }

        BattleForecast.Result forecast = BattleForecast.Of(id, stageId);
        if (forecast.Ok == false)
            return;

        const float lineHeight = 22f;
        RectTransform scroll = GetObject((int)Objects.ScrollView).GetComponent<RectTransform>();
        scroll.sizeDelta = new Vector2(scroll.sizeDelta.x, scroll.sizeDelta.y - lineHeight);

        RectTransform rt = line.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // 창 아래쪽, 설명 칸 밑
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, lineHeight * 0.7f);
        rt.sizeDelta = new Vector2(scroll.sizeDelta.x, lineHeight);

        string cost = Str(SCRIPT_COST, "예상 피해");
        string hp = Str(SCRIPT_HP, "체력");
        if (forecast.Win)
        {
            line.text = $"{cost} -{forecast.Damage} ▶ {hp} {forecast.RemainHP}";
            // 이기긴 하는데 남는 게 1/4 도 안 되면 그것도 알려야 한다. 다음 층이 있다.
            float left = Managers.Game.PlayerData.MaxHP > 0f
                ? forecast.RemainHP / Managers.Game.PlayerData.MaxHP : 1f;
            line.color = (left <= 0.25f) ? new Color32(255, 190, 60, 255) : new Color32(160, 230, 160, 255);
        }
        else
        {
            // 죽는 싸움. 이 표시의 존재 이유이므로 눈에 띄어야 한다.
            string end = (forecast.RemainHP <= 0) ? Str(SCRIPT_DIE, "쓰러진다") : Str(SCRIPT_NO_WIN, "못 이긴다");
            line.text = $"{cost} -{forecast.Damage} ▶ {end}";
            line.color = new Color32(255, 70, 70, 255);
            line.fontStyle = FontStyles.Bold;
        }

        // 창 폭이 200 이라 긴 숫자는 넘친다. 이름표(UI_BaseCard.SetName)와 같은 방식으로
        // 한 줄에 맞춰 줄인다 — 접히면 아래 칸으로 넘쳐 나간다.
        line.alignment = TextAlignmentOptions.Center;
        line.enableWordWrapping = false;
        line.overflowMode = TextOverflowModes.Ellipsis;
        line.fontSizeMin = 7f;
        line.fontSizeMax = line.fontSize;
        line.enableAutoSizing = true;
        line.gameObject.SetActive(true);
    }

    /// <summary>
    /// ScriptData 에 줄이 있으면 그 번역을, 아직 없으면 한국어를 쓴다.
    /// 표에 넣는 것은 데이터 쪽 일이고, 그때까지 이 줄이 비어 있으면 안 된다.
    /// </summary>
    static string Str(int id, string kr)
    {
        string script = Managers.Data.ScriptDic.ContainsKey(id) ? Managers.GetString(id) : "";
        return string.IsNullOrEmpty(script) ? kr : script;
    }
    #endregion

    private void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f, _mask);

        if (raycastHit)
        {
            //Debug.Log(hit.collider.gameObject.layer);
            if (hit.collider.gameObject.layer != (int)Define.Layer.Monster)
            {
                Managers.Game.GameScene.isOpenInfoPopup = false;
                Destroy(gameObject);
            }
        }
        else
        {
            Managers.Game.GameScene.isOpenInfoPopup = false;
            Destroy(gameObject);
        }
    }

    private IEnumerator CoAutoScroll()
    {
        yield return new WaitForSecondsRealtime(mScrollDelay);

        while (true)
        {
            GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().verticalNormalizedPosition -= 10f * Time.deltaTime / GetObject((int)Objects.Content).GetComponent<RectTransform>().sizeDelta.y;

            //스크롤의 끝 영역에 도달했다면 방향을 반전
            if (GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().verticalNormalizedPosition <= 0f || GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().verticalNormalizedPosition >= 1f)
                mScrollSpeed = -mScrollSpeed;
            //if ((GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().verticalNormalizedPosition : GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().horizontalNormalizedPosition) <= 0f || (mIsVerticalScroll ? GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().verticalNormalizedPosition : GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().horizontalNormalizedPosition) >= 1f)
            //    mScrollSpeed = -mScrollSpeed;


            yield return null;
        }
    }

}
