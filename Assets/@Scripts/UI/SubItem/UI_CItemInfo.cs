using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_CItemInfo : UI_Base
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
        // 몬스터 정보 창(UI_MonsterInfo)과 같다. 틀은 SetInfo 가 글 길이에 맞춰 늘린 뒤 한 번 더 세운다.
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
        var item = gameObject.transform.parent.GetComponent<ConsumableItem>();
        _owner = item;
        int id = item.id;
        GetText((int)Texts.MonsterNameText).text = Managers.GetString(Managers.Data.ConsumableItemDic[id].ScriptNameId);
        GetText((int)Texts.MonsterAttackText).text = Managers.Data.ConsumableItemDic[id].AttackUp.ToString();
        GetText((int)Texts.MonsterDefenseText).text = Managers.Data.ConsumableItemDic[id].DefenceUp.ToString();
        float heal = Managers.Data.ConsumableItemDic[id].Heal * Managers.Game.PlayerData.MaxHP / 100;
        heal = Mathf.Round(heal) + Managers.Data.ConsumableItemDic[id].HPUp;
        GetText((int)Texts.MonsterHPText).text = $"{heal}";
        GetText((int)Texts.MonsterDescText).text = (item.ChoicePartner != null
            ? Managers.GetString(Define.REWARD_CHOICE) + "\n\n" : "") + Overflow(id, heal) +
            WhatIf(Managers.Data.ConsumableItemDic[id]) +
            Managers.GetString(Managers.Data.ConsumableItemDic[id].ScriptDescriptionId);
        FitFrame();
    }

    /// <summary>
    /// 공격·방어가 오르는 아이템(룬)을 주우면 이 층에 남은 몬스터의 값이 어떻게 바뀌는가 — 임계가 보이는 곳이다.
    /// "공격 +1 → 늑대 -31→-26" 을 셋까지. 같은 종(이름이 같다 — 생성기가 종마다 이름을 따로 준다)은 한 줄로 묶고,
    /// 이 층에서 아끼는 체력(한 판 × 마릿수)이 큰 것부터, 지다가 이기게 되는 것이 맨 앞이다. 바뀌는 게 없으면
    /// "변화 없음" 한 줄. 셈은 BattleForecast 의 가정(공격·방어 +n) 그대로라 주운 뒤 맵 위 숫자와 같다.
    /// 예측은 마검의 눈이다 — 계약 전에는 없다.
    /// </summary>
    static string WhatIf(Data.ConsumableItemData row)
    {
        int atk = Mathf.RoundToInt(row.AttackUp), def = Mathf.RoundToInt(row.DefenceUp);
        GameManager g = Managers.Game;
        if (atk <= 0 && def <= 0 || g.PlayerData.IsContractedSword == false)
            return "";

        string stat = string.Join(" ",
            atk > 0 ? string.Format(Managers.GetString(ForecastUI.AtkUp), atk) : null,
            def > 0 ? string.Format(Managers.GetString(ForecastUI.DefUp), def) : null).Trim();
        int stage = g.PlayerData.CurStageid;

        Dictionary<string, Change> species = new Dictionary<string, Change>();
        GameObject map;
        if (g.Maps.TryGetValue(stage, out map) && map != null)
        {
            foreach (MonsterController mc in map.GetComponentsInChildren<MonsterController>(false))
            {
                Data.MonsterData md;
                if (ForecastUI.IsStanding(mc) == false || Managers.Data.MonsterDic.TryGetValue(mc.id, out md) == false)
                    continue;
                string name = Managers.GetString(md.MonsterNameId);
                Change c;
                if (species.TryGetValue(name, out c))
                {
                    c.Count++;
                    continue;
                }
                species[name] = new Change { Name = name, Count = 1, Now = ForecastUI.Forecast(mc.id, stage),
                                             Then = ForecastUI.Forecast(mc.id, stage, atk, def) };
            }
        }

        List<Change> changed = new List<Change>();
        foreach (Change c in species.Values)
        {
            if (c.Now.Ok && ForecastUI.Price(c.Now) != ForecastUI.Price(c.Then))
                changed.Add(c);
        }
        if (changed.Count == 0)
            return $"<color=#9AA4B8>{stat} → {Managers.GetString(ForecastUI.NoChange)}</color>\n\n";

        changed.Sort((a, b) => b.Saved.CompareTo(a.Saved));
        string lines = "";
        for (int i = 0; i < changed.Count && i < 3; i++)
            lines += $"{stat} → {changed[i].Name} {ForecastUI.Price(changed[i].Now)}→{ForecastUI.Price(changed[i].Then)}\n";
        return $"<color=#96E68C>{lines}</color>\n";
    }

    class Change
    {
        public string Name;
        public int Count;
        public BattleForecast.Result Now, Then;
        // 이 층에서 아끼는 체력. 지다가 이기게 되면 어떤 값보다 크다(BattleForecast 의 값은 1e7 아래다).
        public float Saved => (Now.Win ? Now.Damage - Then.Damage : 1e7f) * Count;
    }

    /// <summary>
    /// 틀(BGImage)이 설명 칸보다 작다 — 프리팹의 보기 창(167)이 틀(140) 밑으로 삐져 있어서, 넘침 한 줄이나 "둘 중 하나"
    /// 안내가 붙으면 설명 끝줄이 틀 밖 바닥에 그려졌다(가득 찬 체력의 사과는 한 줄, 선택 보상은 두세 줄). 글이 끝나는
    /// 자리까지 틀을 늘린다. 틀은 가운데 기준이라 위아래로 반씩 늘고, 위에 붙은 글도 같이 올라가서 늘린 만큼 다 번다.
    /// </summary>
    void FitFrame()
    {
        const float Bottom = 8f;    // 틀 아래 테두리 안쪽 여백 (틀 단위)
        TMPro.TMP_Text desc = GetText((int)Texts.MonsterDescText);
        RectTransform frame = GetImage((int)Images.BGImage).rectTransform;
        desc.ForceMeshUpdate();
        float textBottom = frame.InverseTransformPoint(desc.transform.TransformPoint(desc.textBounds.min)).y;

        // 보기 창(마스크)도 글 끝까지 내린다 — 룬의 "주우면" 줄에 "둘 중 하나" 안내까지 붙으면 열 줄을 넘어 끝줄이 마스크에
        // 잘리고, 틀만 늘어 빈 띠가 남았다. 창만 늘리고 내용(Content)은 그대로 둔다: 글이 Content 가운데에 붙어 있어서
        // Content 를 늘리면 늘린 몫의 반만큼 글이 따라 내려가 다시 잘렸다. 창보다 짧은 Content 는 ScrollRect 가 창 크기로
        // 셈하므로(AdjustBounds) 저절로 흐르는 스크롤(CoAutoScroll)도 글을 움직이지 않는다.
        RectTransform view = GetObject((int)Objects.ScrollView).GetComponent<RectTransform>();
        float viewBottom = frame.InverseTransformPoint(view.TransformPoint(new Vector3(0f, view.rect.yMin, 0f))).y;
        if (viewBottom > textBottom)
            view.sizeDelta += new Vector2(0f, viewBottom - textBottom);

        float lack = frame.rect.yMin + Bottom - textBottom;
        if (lack > 0f)
        {
            frame.sizeDelta += new Vector2(0f, lack);
            Position = _position;   // 늘어난 높이로 화면 안에 다시 붙잡는다
        }
    }

    /// <summary>
    /// 물약이 최대 체력을 넘겨 버려질 몫 (ConsumableItem.PickUp 이 최대치에서 자른다). 넘치면 주황 한 줄,
    /// 아니면 빈 문자열. 물약은 층의 예산이라 "지금 마실까 아껴 둘까" 가 판단거리다.
    /// </summary>
    static string Overflow(int id, float heal)
    {
        if (id < ConsumableItem.NUM_OF_KEYS || id >= ConsumableItem.NUM_OF_POTIONS)
            return "";
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        int wasted = Mathf.RoundToInt(p.CurHP + heal - p.MaxHP);
        if (wasted <= 0)
            return "";
        return $"<color=#FFA040>{string.Format(Managers.GetString(ForecastUI.HealOverflow), Mathf.RoundToInt(heal), wasted)}</color>\n\n";
    }

    // 이 창을 띄운 아이템(ShowInfo 가 Util.Find 로 찾아 부모로 삼았다)과, 광선이 마지막으로 맞힌 것이 그것인가.
    ConsumableItem _owner;
    Collider _lastHit;
    bool _lastHitIsOwner;

    // 마우스가 이 아이템을 벗어나면 닫는다 — 옆 칸 아이템으로 곧장 옮겨 가도(UI_MonsterInfo 와 같다).
    // 닫으면 UI_GameScene.ShowInfo 가 새 아이템의 창을 띄운다. 맵이 덮여도 닫는다(UI_GameScene.CanShowTooltip).
    // LateUpdate 에서, 끄고 나서 부순다 — 메뉴·전투가 열린 그 프레임에 그려지지 않게(UI_MonsterInfo 와 같다).
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

    // 같은 콜라이더면 다시 찾지 않는다. Util.Find 는 자식을 훑을 때 배열을 만든다.
    bool IsOwner(Collider hitCollider)
    {
        if (hitCollider != _lastHit)
        {
            _lastHit = hitCollider;
            _lastHitIsOwner = hitCollider.gameObject.layer == (int)Define.Layer.CItem
                && Util.Find<ConsumableItem>(hitCollider.gameObject) == _owner;
        }
        return _lastHitIsOwner;
    }

    // 이 창은 아이템의 자식이다. 마우스를 올린 채 그 아이템을 밟으면 아이템이 꺼져 위의 Update 가 돌지
    // 않았고, "정보 창이 떠 있다" 표시가 켜진 채 남아 그 뒤로 전투 예측과 아이템 설명이 영영 안 떴다.
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
            if (GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().horizontalNormalizedPosition <= 0f || GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().horizontalNormalizedPosition >= 1f)
                mScrollSpeed = -mScrollSpeed;
            //if ((GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().verticalNormalizedPosition : GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().horizontalNormalizedPosition) <= 0f || (mIsVerticalScroll ? GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().verticalNormalizedPosition : GetObject((int)Objects.ScrollView).GetComponent<ScrollRect>().horizontalNormalizedPosition) >= 1f)
            //    mScrollSpeed = -mScrollSpeed;


            yield return null;
        }
    }
}
