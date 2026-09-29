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
        var item = gameObject.transform.parent.GetComponent<ConsumableItem>();
        int id = item.id;
        GetText((int)Texts.MonsterNameText).text = Managers.GetString(Managers.Data.ConsumableItemDic[id].ScriptNameId);
        GetText((int)Texts.MonsterAttackText).text = Managers.Data.ConsumableItemDic[id].AttackUp.ToString();
        GetText((int)Texts.MonsterDefenseText).text = Managers.Data.ConsumableItemDic[id].DefenceUp.ToString();
        float heal = Managers.Data.ConsumableItemDic[id].Heal * Managers.Game.PlayerData.MaxHP / 100;
        heal = Mathf.Round(heal) + Managers.Data.ConsumableItemDic[id].HPUp;
        GetText((int)Texts.MonsterHPText).text = $"{heal}";
        GetText((int)Texts.MonsterDescText).text = (item.ChoicePartner != null
            ? Managers.GetString(Define.REWARD_CHOICE) + "\n\n" : "") +
            Managers.GetString(Managers.Data.ConsumableItemDic[id].ScriptDescriptionId);
    }

    private void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f, _mask);

        if (raycastHit)
        {
            //Debug.Log(hit.collider.gameObject.layer);
            if (hit.collider.gameObject.layer != (int)Define.Layer.CItem)
            {
                Release();
                Destroy(gameObject);
            }
        }
        else
        {
            Release();
            Destroy(gameObject);
        }
    }

    // 이 창은 아이템의 자식이다. 마우스를 올린 채 그 아이템을 밟으면 아이템이 꺼져 위의 Update 가 돌지
    // 않았고, "정보 창이 떠 있다" 표시가 켜진 채 남아 그 뒤로 전투 예측과 아이템 설명이 영영 안 떴다.
    // 표시는 한 번만 내린다 — 부서질 때도 OnDisable 이 오는데, 그 사이 새로 뜬 창의 표시를 지우면 안 된다.
    bool _released;

    void Release()
    {
        if (_released)
            return;
        _released = true;
        if (Managers.Game.GameScene != null)
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
