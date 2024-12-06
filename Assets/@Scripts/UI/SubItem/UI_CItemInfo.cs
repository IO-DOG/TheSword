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
    #endregion

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
            GetComponentsInChildren<UnityEngine.UI.Image>()[0].GetComponent<RectTransform>().anchoredPosition = _position +
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
        #endregion

        SetInfo();

        return true;
    }

    void SetInfo()
    {
        int id = gameObject.transform.parent.GetComponent<ConsumableItem>().id;
        GetText((int)Texts.MonsterNameText).text = Managers.GetString(Managers.Data.ConsumableItemDic[id].ScriptNameId);
        GetText((int)Texts.MonsterAttackText).text = "0";
        GetText((int)Texts.MonsterDefenseText).text = "0";
        GetText((int)Texts.MonsterHPText).text = "0";
        GetText((int)Texts.MonsterDescText).text = Managers.GetString(Managers.Data.ConsumableItemDic[id].ScriptDescriptionId);
    }

    private void Update()
    {
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            Destroy(gameObject);
        }
    }

    //private ScrollRect mScrollRect;  // 스크롤 영역
    //private Coroutine? mCoAutoScroll; // 자동 스크롤 코루틴
    //private bool mIsPointerEnter = false; // 현재 포인터가 영역에 들어왔는가?

    //private void Awake()
    //{
    //    mScrollRect = GetComponent<ScrollRect>();
    //}

    //private void OnEnable()
    //{
    //    ToggleAutoScroll(true);
    //}

    //private void OnDisable()
    //{
    //    ToggleAutoScroll(false);
    //}

    //private void ToggleAutoScroll(bool isEnable)
    //{
    //    mScrollRect.velocity = Vector2.zero;

    //    if (mCoAutoScroll is not null)
    //        StopCoroutine(mCoAutoScroll);

    //    if (isEnable)
    //    {
    //        mCoAutoScroll = StartCoroutine(CoAutoScroll());
    //        mIsPointerEnter = false;
    //    }
    //}

    //private IEnumerator CoAutoScroll()
    //{
    //    yield return new WaitForSecondsRealtime(mScrollDelay);

    //    while (true)
    //    {
    //        // 스크롤 방향에 따라 계산
    //        if (mIsVerticalScroll)
    //            mScrollRect.verticalNormalizedPosition -= mScrollSpeed * Time.deltaTime / mScrollRectTransform.sizeDelta.y;
    //        else
    //            mScrollRect.horizontalNormalizedPosition -= mScrollSpeed * Time.deltaTime / mScrollRectTransform.sizeDelta.x;

    //        // 스크롤의 끝 영역에 도달했다면 방향을 반전
    //        if ((mIsVerticalScroll ? mScrollRect.verticalNormalizedPosition : mScrollRect.horizontalNormalizedPosition) <= 0f || (mIsVerticalScroll ? mScrollRect.verticalNormalizedPosition : mScrollRect.horizontalNormalizedPosition) >= 1f)
    //            mScrollSpeed = -mScrollSpeed;

    //        // 영역에 포인터가 진입한 상태에서 클릭 또는 드래그를 했다면?
    //        if (mIsPointerEnter && (Input.GetMouseButton(0) || Input.GetAxis("Mouse ScrollWheel") > 0))
    //            ToggleAutoScroll(false);

    //        yield return null;
    //    }
    //}

    //public void OnPointerEnter(PointerEventData eventData)
    //{
    //    mIsPointerEnter = true;
    //}

    //public void OnPointerExit(PointerEventData eventData)
    //{
    //    mIsPointerEnter = false;
    //    ToggleAutoScroll(true);
    //}
}
