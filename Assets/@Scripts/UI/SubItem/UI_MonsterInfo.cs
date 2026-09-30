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
        // 값은 화면 가운데에서 마우스까지(픽셀, Util.ScreenToWorldCood). 마우스 옆 화면 가운데 쪽에, 화면 안으로 붙잡아 세운다.
        set
        {
            _position = value;
            CodeUI.PlaceBeside(GetComponentsInChildren<UnityEngine.UI.Image>()[0].rectTransform, value, 80f);
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
        _owner = gameObject.transform.parent.GetComponent<MonsterController>();
        int id = _owner.id;
        int stageId = Managers.Game.PlayerData.CurStageid;

        //Debug.Log(Managers.Data.MonsterDic[id].MonsterNameId);
        GetText((int)Texts.MonsterNameText).text = Managers.GetString(Managers.Data.MonsterDic[id].MonsterNameId);
        //GetText((int)Texts.MonsterClassText).text = "특성 : " + Managers.Data.MonsterClassDic[Managers.Data.MonsterDic[id].Feature].ClassName;
        GetText((int)Texts.MonsterAttackText).text = (Managers.Data.StageInfoDic[stageId].ATK * Managers.Data.MonsterDic[id].Attack).ToString();
        GetText((int)Texts.MonsterDefenseText).text = (Managers.Data.StageInfoDic[stageId].DEF * Managers.Data.MonsterDic[id].Defence).ToString();
        GetText((int)Texts.MonsterHPText).text = Managers.Data.MonsterDic[id].MaxHP.ToString();

        // 특성 이름을 설명 맨 앞에 붙인다. 설명은 저절로 흐르는 칸이라 첫 줄이 먼저 보인다.
        Data.MonsterClassData trait;
        string traitLine = Managers.Data.MonsterClassDic.TryGetValue(Managers.Data.MonsterDic[id].Ability, out trait)
            ? $"<color=#E8C170>{Managers.GetString(trait.ClassName)}</color>\n" : "";
        GetText((int)Texts.MonsterDescText).text = traitLine + Managers.GetString(Managers.Data.MonsterDic[id].MonsterDescId);

        // 예측은 마검의 눈이다 — 계약 전에는 보이지 않는다.
        if (Managers.Game.PlayerData.IsContractedSword)
            ShowForecast(id, stageId);
    }

    #region 전투 비용
    /// <summary>
    /// "이놈을 잡으면 체력이 얼마 줄어드는가" 와 그 곁의 한 줄(맞는 횟수·첫 타 치명·경험치). 이 창이 있어야 하는 이유다.
    ///
    /// 공격력·방어력·체력만 보여 주는 것으로는 판단할 수 없다 — 게이지식 전투라
    /// 공격 주기와 특성이 얽히기 때문이다. 그래서 숫자를 늘어놓는 대신 결과를 적는다.
    /// 셈은 BattleForecast 가 진짜 전투 코드를 그대로 돌려서 한다. 더 자세한 것(임계)은 도감(M)에 있다.
    ///
    /// 셋째 줄은 다음 임계 — "공격 +3이면 -31→-24". 공격 +10 안에 값이 내려가는 수가 있을 때만 나온다.
    /// 마우스를 옮길 때마다 창이 새로 뜨니 셈은 ForecastUI.Forecast 가 기억해 둔 것을 쓴다.
    ///
    /// 프리팹은 에디터에서만 고칠 수 있으니 새 오브젝트를 만들지 않고, 특성 표시를
    /// 접으면서 꺼 둔 채 남아 있던 MonsterClassText 를 되살려 쓴다(위 Texts enum 의
    /// 주석 처리된 줄이 그것이다). 자리는 설명 칸을 그 줄 수만큼 줄여서 낸다.
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

        BattleForecast.Result forecast = ForecastUI.Forecast(id, stageId);
        if (forecast.Ok == false)
            return;

        BattleForecast.Result then;
        int step = ForecastUI.AttackStep(id, stageId, forecast, out then);
        int lines = step > 0 ? 3 : 2;

        const float lineHeight = 22f;
        RectTransform scroll = GetObject((int)Objects.ScrollView).GetComponent<RectTransform>();
        scroll.sizeDelta = new Vector2(scroll.sizeDelta.x, scroll.sizeDelta.y - lineHeight * 2f);
        // 셋째 줄(임계)은 설명 칸을 더 줄이지 않고 틀을 늘려 낸다. 설명은 위에, 이 줄은 아래에 붙어 있어 사이가 그만큼 벌어진다.
        if (lines > 2)
        {
            GetImage((int)Images.BGImage).rectTransform.sizeDelta += new Vector2(0f, lineHeight);
            Position = _position;   // 늘어난 높이로 화면 안에 다시 붙잡는다
        }

        RectTransform rt = line.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // 창 아래쪽, 설명 칸 밑
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, lineHeight * (0.2f + lines * 0.5f));
        rt.sizeDelta = new Vector2(scroll.sizeDelta.x, lineHeight * lines);

        // 첫 줄은 결과, 둘째 줄은 곁의 것. 색은 맵 위 숫자와 같은 눈금이다(지금 체력에 대한 몫).
        // 화살표는 → 다. 두 글꼴 모두 ▶ 가 없어 네모(□)로 나왔다. ✖ 도 없어서 X 를 쓴다.
        string cost = Managers.GetString(ForecastUI.Cost);
        string result = forecast.Win
            ? $"{cost} -{forecast.Damage} → {Managers.GetString(ForecastUI.Hp)} {forecast.RemainHP}"
            : forecast.Kills
                ? $"X {cost} -{forecast.Damage} → {Managers.GetString(ForecastUI.Falls)}"
                : $"X {Managers.GetString(ForecastUI.CannotWin)}";

        string detail = forecast.Win ? string.Format(Managers.GetString(ForecastUI.HitsTaken), forecast.HitsTaken) : "";
        if (forecast.FirstStrikeCrit)
            detail += "  " + Managers.GetString(ForecastUI.FirstCrit);
        detail += "  " + string.Format(Managers.GetString(ForecastUI.Exp), forecast.Exp);
        if (forecast.LevelUp)
            detail += " " + ForecastUI.LevelUpMark;

        // 곁의 두 줄은 결과 줄과 같은 크기로, 색으로만 가른다. 80% 로 줄였더니 1280x800(스팀 덱)에서 8~9px 이라
        // "9px 아래 글자 없음" 기준에 걸렸다.
        line.text = result + "\n<color=#D8DCE6>" + detail.Trim() + "</color>";
        if (step > 0)
        {
            string atk = string.Format(Managers.GetString(ForecastUI.AtkUp), step);
            line.text += "\n<color=#96E68C>" + string.Format(Managers.GetString(ForecastUI.NextStep), atk,
                ForecastUI.Price(forecast), ForecastUI.Price(then)) + "</color>";
        }
        line.color = ForecastUI.Tone(forecast, Managers.Game.PlayerData.CurHP);
        line.fontStyle = forecast.Win ? FontStyles.Normal : FontStyles.Bold;

        // 창 폭이 200 이라 긴 숫자는 넘친다. 이름표(UI_BaseCard.SetName)와 같은 방식으로
        // 줄마다 한 줄에 맞춰 줄인다 — 접히면 아래 칸으로 넘쳐 나간다.
        line.alignment = TextAlignmentOptions.Center;
        line.textWrappingMode = TextWrappingModes.NoWrap;
        line.overflowMode = TextOverflowModes.Ellipsis;
        line.fontSizeMin = 7f;
        // 설명(13)과 같은 크기까지. 프리팹의 12 로는 라틴 글자(한글보다 낮다)가 1280x800 에서 9px 를 못 넘는다.
        // 가장 긴 줄(영어 "N hits taken  First hit crits  EXP N ★")도 13 에서 폭 200 안에 든다.
        line.fontSizeMax = 13f;
        line.enableAutoSizing = true;
        line.gameObject.SetActive(true);
    }
    #endregion

    // 이 창을 띄운 몬스터(ShowInfo 가 Util.Find 로 찾아 부모로 삼았다)와, 광선이 마지막으로 맞힌 것이 그놈인가.
    MonsterController _owner;
    Collider _lastHit;
    bool _lastHitIsOwner;

    // 마우스가 이 몬스터를 벗어나면 닫는다 — 옆 칸 몬스터로 곧장 옮겨 가도. 몬스터 콜라이더는 한 칸 폭에
    // 16칸 높이라(MapBuilder.FitColliderToCell) 바닥을 안 거치고 넘어가서, A 의 이름·예측이 B 를 가리키는
    // 채 남았다(못 이기는 B 위에 A 의 "-12" 가). 닫으면 UI_GameScene.ShowInfo 가 B 의 창을 새로 띄운다.
    // 메뉴·전투·연출·대화나 다른 창이 맵을 덮어도 닫는다(UI_GameScene.CanShowTooltip).
    // LateUpdate 에서, 끄고 나서 부순다: 부딪혀 전투가 열린 그 프레임 끝에 전투창이 화면을 찍는다(MonsterController.SetMonster).
    // Update 는 부딪힘(PlayerController.Update)보다 먼저 돌 수 있어서, 그러면 이 창이 한 프레임 남아 전투 배경에 박혔다.
    private void LateUpdate()
    {
        if (_init == false)
            return;
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (UI_GameScene.CanShowTooltip() && Physics.Raycast(ray, out hit, 1000.0f, _mask) && IsOwner(hit.collider))
            return;

        Release();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    // 같은 콜라이더면 다시 찾지 않는다. Util.Find 는 자식을 훑을 때 배열을 만든다(마우스를 올린 동안 매 프레임).
    bool IsOwner(Collider hitCollider)
    {
        if (hitCollider != _lastHit)
        {
            _lastHit = hitCollider;
            _lastHitIsOwner = hitCollider.gameObject.layer == (int)Define.Layer.Monster
                && Util.Find<MonsterController>(hitCollider.gameObject) == _owner;
        }
        return _lastHitIsOwner;
    }

    // 이 창은 몬스터의 자식이다. 마우스를 올린 채 그 몬스터와 싸우면 몬스터가 통째로 꺼지거나 부서져
    // 위의 Update 가 돌지 않았고, "정보 창이 떠 있다" 표시가 켜진 채 남아 그 뒤로 전투 예측이 영영 안 떴다.
    // 표시는 한 번만 내린다 — 부서질 때도 OnDisable 이 오는데, 그 사이 새로 뜬 창의 표시를 지우면 안 된다.
    // 플레이를 끄는 중이면(매니저가 먼저 사라졌으면) 건드리지 않는다 — 부르면 @Managers 를 새로 세운다.
    bool _released;

    void Release()
    {
        if (_released)
            return;
        _released = true;
        if (Managers.IsAlive && Managers.Game.GameScene != null)
            Managers.Game.GameScene.isOpenInfoPopup = false;
    }

    void OnDisable()
    {
        Release();
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
