using Coffee.UIEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class UI_InvenPopup : UI_Popup
{
    #region Enum
    enum Images
    {
        necklace,
        Inventory_accessory_necklace_Get,
        Inventory_accessory_necklace_On,
        ring,
        Inventory_accessory_ring_Get,
        Inventory_accessory_ring_On,
        shoes,
        Inventory_accessory_shoes_Get,
        Inventory_accessory_shoes_On,
        book,
        Inventory_accessory_book_Get,
        Inventory_accessory_book_On,
        sword,
        Inventory_Sword_Get,
        Inventory_Sword_On,
        Class,
        shield,
        Inventory_Shield_Get,
        Inventory_Shield_On,
        Inventory_InfoFrame,
        Inventory_MyInfo,
        Inventory_MyInfo_On,
        Inventory_EquipList,
        EquipList1,
        Inventory_EquipList1_Get,
        Inventory_EquipList1_On,
        EquipList2,
        Inventory_EquipList2_Get,
        Inventory_EquipList2_On,
        EquipList3,
        Inventory_EquipList3_Get,
        Inventory_EquipList3_On,
        EquipList4,
        Inventory_EquipList4_Get,
        Inventory_EquipList4_On,
        EquipList5,
        Inventory_EquipList5_Get,
        Inventory_EquipList5_On,
        EquipList6,
        Inventory_EquipList6_Get,
        Inventory_EquipList6_On,
        EquipList7,
        Inventory_EquipList7_Get,
        Inventory_EquipList7_On,
        EquipList8,
        Inventory_EquipList8_Get,
        Inventory_EquipList8_On,
        EquipList9,
        Inventory_EquipList9_Get,
        Inventory_EquipList9_On,
        EquipList10,
        Inventory_EquipList10_Get,
        Inventory_EquipList10_On,
        ATKInfo,
        DEFInfo,
        HPInfo,
        CRIInfo,
        CRIATKInfo,
        LVInfo,
        ATKSPEEDInfo,
        DEFSPEEDInfo,
        MOVESPEEDInfo,
        ATKInfoImage,
        DEFInfoImage,
        HPInfoImage,
        CRIInfoImage,
        CRIATKInfoImage,
        LVInfoImage,
        ATKSPEEDInfoImage,
        DEFSPEEDInfoImage,
        MOVESPEEDInfoImage,
        IllustBG,
        Illust,
        IllustFX,
    }

    enum Texts
    {
        TotalATK,
        AddATK,
        BaseATK,
        TotalDEF,
        AddDEF,
        BaseDEF,
        TotalHP,
        AddHP,
        BaseHP,
        TotalCRI,
        AddCRI,
        BaseCRI,
        TotalCRIATK,
        AddCRIATK,
        BaseCRIATK,
        TotalLV,
        AddLV,
        BaseLV,
        TotalATKSPEED,
        AddATKSPEED,
        BaseATKSPEED,
        TotalDEFSPEED,
        AddDEFSPEED,
        BaseDEFSPEED,
        TotalMOVESPEED,
        AddMOVESPEED,
        BaseMOVESPEED,
        EquipName,
        InfoText,
        ATKInfoText,
        DEFInfoText,
        HPInfoText,
        CRIInfoText,
        CRIATKInfoText,
        LVInfoText,
        ATKSPEEDInfoText,
        DEFSPEEDInfoText,
        MOVESPEEDInfoText,
        // 능력치 칸 이름과 플레이어 이름. 프리팹에 영어로 박혀 있어 Init 이 언어대로 다시 쓴다.
        // 아래 아홉 줄은 StatLabelTextIds 와 같은 순서다.
        ATKText,
        DEFText,
        HPText,
        CRIText,
        CRIATKText,
        LVText,
        ATKSPEEDText,
        DEFSPEEDText,
        MOVESPEEDText,
        UserName,
    }

    // 능력치 칸 이름 (Tools/ui_text_parts/ui.py). 칸이 좁아 짧은 이름을 따로 둔다 — 100~108 은 긴 이름이라
    // 마우스를 올렸을 때 뜨는 설명(…InfoText)이 쓴다. 띄어쓰기는 두 줄 칸(ATK SPEED 등)의 줄바꿈이다.
    public static readonly int[] StatLabelTextIds = { 194, 195, 196, 197, 198, 199, 200, 201, 202 };
    // 장비 능력치 줄(PrintEquipAbilityAndDesc 의 순서: 공·방·체·공속·방속·치명·치명공격·이동속도) → 위 표의 자리.
    static readonly int[] EquipStatLabel = { 0, 1, 2, 6, 7, 3, 4, 8 };

    enum GameObjects
    {
        EquipInfo,
        StatusInfoList,
        EquipIllust,
    }

    #endregion

    public bool _isInventory_MyInfo_On = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));
        #endregion
        OnPointerEnterImage();
        OnPointerExitImage();

        SetPlayerStatusInfo();
        GetImage((int)Images.Inventory_MyInfo_On).gameObject.BindEvent(OnClickInventory_MyInfo_On);
        GetImage((int)Images.sword).gameObject.BindEvent(OnClickSword);
        GetImage((int)Images.shield).gameObject.BindEvent(OnClickShield);
        GetImage((int)Images.necklace).gameObject.BindEvent(OnClickNecklace);
        GetImage((int)Images.ring).gameObject.BindEvent(OnClickRing);
        GetImage((int)Images.shoes).gameObject.BindEvent(OnClickShoes);
        GetImage((int)Images.book).gameObject.BindEvent(OnClickBook);
        GetImage((int)Images.EquipList1).gameObject.BindEvent(() => { OnClickEquipList(1); });
        GetImage((int)Images.EquipList2).gameObject.BindEvent(() => { OnClickEquipList(2); });
        GetImage((int)Images.EquipList3).gameObject.BindEvent(() => { OnClickEquipList(3); });
        GetImage((int)Images.EquipList4).gameObject.BindEvent(() => { OnClickEquipList(4); });
        GetImage((int)Images.EquipList5).gameObject.BindEvent(() => { OnClickEquipList(5); });
        GetImage((int)Images.EquipList6).gameObject.BindEvent(() => { OnClickEquipList(6); });
        GetImage((int)Images.EquipList7).gameObject.BindEvent(() => { OnClickEquipList(7); });
        GetImage((int)Images.EquipList8).gameObject.BindEvent(() => { OnClickEquipList(8); });
        GetImage((int)Images.EquipList9).gameObject.BindEvent(() => { OnClickEquipList(9); });
        GetImage((int)Images.EquipList10).gameObject.BindEvent(() => { OnClickEquipList(10); });
        int i = 0;
        GetText((int)Texts.ATKInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.DEFInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.HPInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.CRIInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.CRIATKInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.LVInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.ATKSPEEDInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.DEFSPEEDInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);
        GetText((int)Texts.MOVESPEEDInfoText).text = Managers.GetString(Define.STAT_INFO_SCRIPT + i++);

        // 한국어로 해도 "Demian", "ATK" 가 떴다. 두 줄 칸은 프리팹이 줄 간격을 좁혀 두어 줄바꿈만 넣으면 제자리다.
        // 이름표 하나를 못 찾았다고 창(장비 칸·목록)이 통째로 안 서면 안 된다 — 없으면 건너뛴다.
        // 문구가 표에 없으면(빈 문자열) 프리팹 글자를 둔다. 빈칸보다 영어가 낫다.
        // 칸 폭(17)이 한 글자 폭이라, 자동 줄바꿈이 켜진 칸(공격·방어·체력·치명·레벨)은 한글·한자가 한 자씩 세로로
        // 쌓였고 "치명 공격" 은 네 줄이 되어 창 밑으로 삐져나갔다. 줄은 위에서 넣는 줄바꿈으로만 나눈다.
        void Label(Texts slot, string text)
        {
            var label = GetText((int)slot);
            if (label == null || text.Length == 0)
                return;
            label.text = text;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        }
        Label(Texts.UserName, Managers.GetString(Define.USER_NAME_INDEX));
        for (int k = 0; k < StatLabelTextIds.Length; k++)
            Label(Texts.ATKText + k, Managers.GetString(StatLabelTextIds[k]).Replace(' ', '\n'));

        OnClickInventory_MyInfo_On();
        SortInven();
        Refresh();

        return true;
    }

    // Esc 는 게임 화면(UI_GameScene)이 맨 위 창에게만 넘긴다. 예전에는 여기서도 따로 받아서,
    // 한 번 누른 Esc 에 인벤토리가 닫히고 곧바로 메뉴가 열렸다.
    public override bool OnEscape()
    {
        Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
        ClosePopupUI();
        return true;
    }

    /// <summary>
    /// 장비 그림을 칸에 넣는다. 그림이 어드레서블에 없으면(부츠·목걸이·반지 대부분) 칸을 비운다 —
    /// 예전에는 sprite 가 null 인 채 불투명하게 그려져 흰 네모가 떴다.
    /// </summary>
    static void ShowIcon(Image image, string spriteKey)
    {
        Sprite sprite = Managers.Resource.Load<Sprite>(spriteKey);
        image.sprite = sprite;
        image.color = new Color(1, 1, 1, sprite != null ? 1 : 0);
    }

    void SortInven()
    {
        Managers.Game.PlayerData.Inventory[(int)Define.Types.Necklace].Sort();
        Managers.Game.PlayerData.Inventory[(int)Define.Types.Ring].Sort();
        Managers.Game.PlayerData.Inventory[(int)Define.Types.Shoes].Sort();
        Managers.Game.PlayerData.Inventory[(int)Define.Types.Book].Sort();
        Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Sort();
        Managers.Game.PlayerData.Inventory[(int)Define.Types.Shield].Sort();
    }

    void Refresh()
    {
        GetImage((int)Images.Inventory_accessory_necklace_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_accessory_necklace_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_accessory_ring_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_accessory_ring_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_accessory_shoes_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_accessory_shoes_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_accessory_book_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_accessory_book_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_Sword_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_Sword_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_Shield_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_Shield_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList1_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList1_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList2_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList2_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList3_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList3_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList4_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList4_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList5_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList5_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList6_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList6_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList7_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList7_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList8_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList8_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList9_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList9_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList10_Get).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList10_On).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(false);

        if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Necklace].Count == 0)
            GetImage((int)Images.necklace).color = new Color(1, 1, 1, 0);
        else
        {
            int idx = Managers.Game.PlayerData.Inventory[(int)Define.Types.Necklace][Managers.Game.PlayerData.Inventory[(int)Define.Types.Necklace].Count - 1];
            ShowIcon(GetImage((int)Images.necklace), Managers.Data.EquipDic[idx].ImageName);
        }

        if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Ring].Count == 0)
            GetImage((int)Images.ring).color = new Color(1, 1, 1, 0);
        else
        {
            int idx = Managers.Game.PlayerData.Inventory[(int)Define.Types.Ring][Managers.Game.PlayerData.Inventory[(int)Define.Types.Ring].Count - 1];
            ShowIcon(GetImage((int)Images.ring), Managers.Data.EquipDic[idx].ImageName);
        }

        if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Shoes].Count == 0)
            GetImage((int)Images.shoes).color = new Color(1, 1, 1, 0);
        else
        {
            int idx = Managers.Game.PlayerData.Inventory[(int)Define.Types.Shoes][Managers.Game.PlayerData.Inventory[(int)Define.Types.Shoes].Count - 1];
            ShowIcon(GetImage((int)Images.shoes), Managers.Data.EquipDic[idx].ImageName);
        }

        if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Book].Count == 0)
            GetImage((int)Images.book).color = new Color(1, 1, 1, 0);
        else
        {
            int idx = Managers.Game.PlayerData.Inventory[(int)Define.Types.Book][Managers.Game.PlayerData.Inventory[(int)Define.Types.Book].Count - 1];
            ShowIcon(GetImage((int)Images.book), Managers.Data.EquipDic[idx].ImageName);
        }

        // 이 칸이 그리는 것은 "가진 것"이 아니라 "낀 것"이다. 둘이 어긋나면
        // EquipDic[0] 을 그리는데, 0 번은 이름이 "-" 인 빈 칸이면서 그림만
        // Equip_Ring_00 이라 검 칸에 반지가 뜬다. 안 낀 상태는 비워 둔다.
        if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Count == 0
            || Managers.Game.PlayerData.CurSword == Define.NOT_EQUIP)
            GetImage((int)Images.sword).color = new Color(1, 1, 1, 0);
        else
            ShowIcon(GetImage((int)Images.sword), Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ImageName);

        // to check player class
        GetImage((int)Images.Class).color = new Color(1, 1, 1, 0);

        // 검 칸과 같은 이유로 낀 것이 없으면 비운다.
        if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Shield].Count == 0
            || Managers.Game.PlayerData.CurShield == Define.NOT_EQUIP)
            GetImage((int)Images.shield).color = new Color(1, 1, 1, 0);
        else
            ShowIcon(GetImage((int)Images.shield), Managers.Data.EquipDic[Managers.Game.PlayerData.CurShield].ImageName);

        GetImage((int)Images.EquipList1).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList2).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList3).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList4).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList5).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList6).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList7).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList8).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList9).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.EquipList10).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.ATKInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.DEFInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.HPInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.CRIInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.CRIATKInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.LVInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.ATKSPEEDInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.DEFSPEEDInfoImage).gameObject.SetActive(false);
        GetImage((int)Images.MOVESPEEDInfoImage).gameObject.SetActive(false);

        GetObject((int)GameObjects.EquipIllust).gameObject.SetActive(false);
    }

    void OnClickInventory_MyInfo_On()
    {
        if (_isInventory_MyInfo_On == false)
        {
            _isInventory_MyInfo_On = true;
            GetImage((int)Images.Inventory_MyInfo_On).color = new Color(1, 1, 1, 1);
            GetImage((int)Images.Inventory_InfoFrame).gameObject.SetActive(true);
            GetImage((int)Images.Inventory_MyInfo).gameObject.SetActive(true);
            GetObject((int)GameObjects.EquipInfo).gameObject.SetActive(false);
            // 숫자는 Init 에서 한 번만 찍혔다. 팝업을 연 채 장비를 갈아입으면
            // 옛 값이 그대로 남아서, 켤 때마다 지금 값으로 다시 그린다.
            SetPlayerStatusInfo();
        }
        else
        {
            _isInventory_MyInfo_On = false;
            GetImage((int)Images.Inventory_MyInfo_On).color = new Color(1, 1, 1, 0);
            GetImage((int)Images.Inventory_InfoFrame).gameObject.SetActive(false);
            GetImage((int)Images.Inventory_MyInfo).gameObject.SetActive(false);
            GetObject((int)GameObjects.EquipInfo).gameObject.SetActive(true);
        }
    }

    void OffInventory_MyInfo_On()
    {
        _isInventory_MyInfo_On = false;
        GetImage((int)Images.Inventory_MyInfo_On).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.Inventory_InfoFrame).gameObject.SetActive(false);
        GetImage((int)Images.Inventory_MyInfo).gameObject.SetActive(false);
        GetObject((int)GameObjects.EquipInfo).gameObject.SetActive(true);
    }

    /// <summary>지금 목록에 띄운 부위. 목록을 클릭하면 이 부위를 갈아입는다.
    ///
    /// 예전에는 검이냐 방패냐로만 갈라져 있어서, 목걸이·반지·부츠·책은 칸을 눌러도
    /// 목록이 열리지 않았다 — 바꿀 방법이 아예 없었다.</summary>
    Define.Types _listType = Define.Types.Sword;

    void OnClickSword()
    {
        _listType = Define.Types.Sword;
        Refresh();
        OffInventory_MyInfo_On();
        SetSwordListImage();
        PrintEquipAbilityAndDesc(Managers.Game.PlayerData.CurSword);

        GetImage((int)Images.Inventory_Sword_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_Sword_On).gameObject.SetActive(true);

        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);

        ShowSwordIllust();
    }

    void SetSwordListImage()
    {
        UnityEngine.UI.Image[] equipList =
            { GetImage((int)Images.EquipList1), GetImage((int)Images.EquipList2), GetImage((int)Images.EquipList3), GetImage((int)Images.EquipList4)
        , GetImage((int)Images.EquipList5), GetImage((int)Images.EquipList6), GetImage((int)Images.EquipList7), GetImage((int)Images.EquipList8), GetImage((int)Images.EquipList9)
        , GetImage((int)Images.EquipList10)};
        UnityEngine.UI.Image[] inventory_equipList_get =
            { GetImage((int)Images.Inventory_EquipList1_Get), GetImage((int)Images.Inventory_EquipList2_Get), GetImage((int)Images.Inventory_EquipList3_Get), GetImage((int)Images.Inventory_EquipList4_Get)
        , GetImage((int)Images.Inventory_EquipList5_Get), GetImage((int)Images.Inventory_EquipList6_Get), GetImage((int)Images.Inventory_EquipList7_Get), GetImage((int)Images.Inventory_EquipList8_Get)
        , GetImage((int)Images.Inventory_EquipList9_Get), GetImage((int)Images.Inventory_EquipList10_Get)};

        for (int i = 1; i <= 10; ++i)
        {
            if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Count >= i)
            {
                int idx = Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword][i - 1];
                ShowIcon(equipList[i - 1], Managers.Data.EquipDic[idx].ImageName);
                inventory_equipList_get[i - 1].gameObject.SetActive(true);
            }
        }
    }

    private Coroutine illustCoroutine;
    void ShowSwordIllust()
    {
        GetObject((int)GameObjects.EquipIllust).gameObject.SetActive(true);

        int curSwordIdx = Managers.Game.PlayerData.CurSword;

        ShowIcon(GetImage((int)Images.IllustBG), Managers.Data.EquipDic[curSwordIdx].IllustBG);
        ShowIcon(GetImage((int)Images.Illust), Managers.Data.EquipDic[curSwordIdx].Illust);

        // 이미 일러스트 코루틴이 실행 중이라면 중단한다.
        if (illustCoroutine != null)
        {
            StopCoroutine(illustCoroutine);
            illustCoroutine = null;
        }
        illustCoroutine = StartCoroutine(CoIllustUIEffect());
        PlayIllustFX(Managers.Data.EquipDic[curSwordIdx].IllustFX);
    }

    /// <summary>
    /// 칼 그림 위의 번쩍임. 데이터는 "Illust_SwordFX_01" 로 적는데 컨트롤러의 상태는 "SwordIllust1FX" 다 —
    /// 이름이 달라 여태 한 번도 재생되지 않았다. 번호로 옮겨 부르고, 클립이 없는 칼(지금은 01·02 만 있다)은
    /// FX 칸을 끈다. 없는 상태를 부르면 기본 상태, 곧 다른 칼의 번쩍임이 나온다.
    /// </summary>
    void PlayIllustFX(string data)
    {
        Animator fx = GetImage((int)Images.IllustFX).GetComponent<Animator>();
        if (fx == null)
            return;

        int number;
        string state = null;
        int underscore = string.IsNullOrEmpty(data) ? -1 : data.LastIndexOf('_');
        if (underscore >= 0 && int.TryParse(data.Substring(underscore + 1), out number))
            state = $"SwordIllust{number}FX";

        fx.gameObject.SetActive(true);   // 꺼진 애니메이터에는 상태를 물을 수 없다
        if (state != null && fx.HasState(0, Animator.StringToHash(state)))
            fx.Play(state);
        else
            fx.gameObject.SetActive(false);
    }

    IEnumerator CoIllustUIEffect()
    {
        GetImage((int)Images.Illust).gameObject.GetOrAddComponent<UIShiny>().effectFactor = 0;
        WaitForSeconds delay = new WaitForSeconds(0.02f);
        while (true)
        {
            if (GetImage((int)Images.Illust).gameObject.GetOrAddComponent<UIShiny>().effectFactor >= 0.98f)
                GetImage((int)Images.Illust).gameObject.GetOrAddComponent<UIShiny>().effectFactor = 0;
            yield return delay;
            GetImage((int)Images.Illust).gameObject.GetOrAddComponent<UIShiny>().effectFactor += 0.02f;
        }
    }

    void OnClickShield()
    {
        _listType = Define.Types.Shield;
        Refresh();
        OffInventory_MyInfo_On();
        SetShieldListImage();
        PrintEquipAbilityAndDesc(Managers.Game.PlayerData.CurShield);

        GetImage((int)Images.Inventory_Shield_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_Shield_On).gameObject.SetActive(true);

        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);

        // TODO 여섯 부위(검·방패·목걸이·반지·신발·책) 핸들러가 전부 _Get 을 무조건
        // 켠다. 이것이 "보유 표시"인지 "선택된 탭 표시"인지 프리팹에서 확인한 뒤
        // 여섯 곳을 함께 고쳐야 한다. 한 곳만 바꾸면 부위마다 규칙이 갈라진다.
    }

    void SetShieldListImage()
    {
        UnityEngine.UI.Image[] equipList =
            { GetImage((int)Images.EquipList1), GetImage((int)Images.EquipList2), GetImage((int)Images.EquipList3), GetImage((int)Images.EquipList4)
        , GetImage((int)Images.EquipList5), GetImage((int)Images.EquipList6), GetImage((int)Images.EquipList7), GetImage((int)Images.EquipList8), GetImage((int)Images.EquipList9)
        , GetImage((int)Images.EquipList10)};
        UnityEngine.UI.Image[] inventory_equipList_get =
            { GetImage((int)Images.Inventory_EquipList1_Get), GetImage((int)Images.Inventory_EquipList2_Get), GetImage((int)Images.Inventory_EquipList3_Get), GetImage((int)Images.Inventory_EquipList4_Get)
        , GetImage((int)Images.Inventory_EquipList5_Get), GetImage((int)Images.Inventory_EquipList6_Get), GetImage((int)Images.Inventory_EquipList7_Get), GetImage((int)Images.Inventory_EquipList8_Get)
        , GetImage((int)Images.Inventory_EquipList9_Get), GetImage((int)Images.Inventory_EquipList10_Get)};

        for (int i = 1; i <= 10; ++i)
        {
            if (Managers.Game.PlayerData.Inventory[(int)Define.Types.Shield].Count >= i)
            {
                int idx = Managers.Game.PlayerData.Inventory[(int)Define.Types.Shield][i - 1];
                ShowIcon(equipList[i - 1], Managers.Data.EquipDic[idx].ImageName);
                inventory_equipList_get[i - 1].gameObject.SetActive(true);
            }
        }
    }

    void OnClickNecklace()
    {
        _listType = Define.Types.Necklace;
        Refresh();
        OffInventory_MyInfo_On();
        GetImage((int)Images.Inventory_accessory_necklace_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_necklace_On).gameObject.SetActive(true);

        int idx = Managers.Game.PlayerData.CurNecklace;
        PrintEquipAbilityAndDesc(idx);

        SetTypeListImage(_listType);
        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);
    }

    void OnClickRing()
    {
        _listType = Define.Types.Ring;
        Refresh();
        OffInventory_MyInfo_On();
        GetImage((int)Images.Inventory_accessory_ring_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_ring_On).gameObject.SetActive(true);

        int idx = Managers.Game.PlayerData.CurRing;
        PrintEquipAbilityAndDesc(idx);

        SetTypeListImage(_listType);
        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);
    }

    void OnClickShoes()
    {
        _listType = Define.Types.Shoes;
        Refresh();
        OffInventory_MyInfo_On();
        GetImage((int)Images.Inventory_accessory_shoes_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_shoes_On).gameObject.SetActive(true);

        int idx = Managers.Game.PlayerData.CurShoes;
        PrintEquipAbilityAndDesc(idx);

        SetTypeListImage(_listType);
        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);
    }

    void OnClickBook()
    {
        _listType = Define.Types.Book;
        Refresh();
        OffInventory_MyInfo_On();
        GetImage((int)Images.Inventory_accessory_book_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_book_On).gameObject.SetActive(true);

        int idx = Managers.Game.PlayerData.CurBook;
        PrintEquipAbilityAndDesc(idx);

        SetTypeListImage(_listType);
        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);
    }


    /// <summary>그 부위의 인벤토리를 목록 칸에 그린다. 검·방패 전용이던 것을 부위 공통으로.</summary>
    void SetTypeListImage(Define.Types type)
    {
        UnityEngine.UI.Image[] equipList =
        {
            GetImage((int)Images.EquipList1), GetImage((int)Images.EquipList2), GetImage((int)Images.EquipList3),
            GetImage((int)Images.EquipList4), GetImage((int)Images.EquipList5), GetImage((int)Images.EquipList6),
            GetImage((int)Images.EquipList7), GetImage((int)Images.EquipList8), GetImage((int)Images.EquipList9),
            GetImage((int)Images.EquipList10)
        };
        UnityEngine.UI.Image[] getMark =
        {
            GetImage((int)Images.Inventory_EquipList1_Get), GetImage((int)Images.Inventory_EquipList2_Get),
            GetImage((int)Images.Inventory_EquipList3_Get), GetImage((int)Images.Inventory_EquipList4_Get),
            GetImage((int)Images.Inventory_EquipList5_Get), GetImage((int)Images.Inventory_EquipList6_Get),
            GetImage((int)Images.Inventory_EquipList7_Get), GetImage((int)Images.Inventory_EquipList8_Get),
            GetImage((int)Images.Inventory_EquipList9_Get), GetImage((int)Images.Inventory_EquipList10_Get)
        };

        System.Collections.Generic.List<int> owned = OwnedOf(type);
        for (int i = 0; i < equipList.Length; ++i)
        {
            bool has = i < owned.Count;
            getMark[i].gameObject.SetActive(has);
            if (has == false)
            {
                equipList[i].color = new Color(1, 1, 1, 0);
                continue;
            }
            ShowIcon(equipList[i], Managers.Data.EquipDic[owned[i]].ImageName);
        }
    }

    /// <summary>그 부위로 가진 장비 목록. 범위를 벗어나면 빈 목록을 준다.</summary>
    System.Collections.Generic.List<int> OwnedOf(Define.Types type)
    {
        System.Collections.Generic.List<System.Collections.Generic.List<int>> inv =
            Managers.Game.PlayerData.Inventory;
        int i = (int)type;
        if (inv == null || i < 0 || i >= inv.Count || inv[i] == null)
            return new System.Collections.Generic.List<int>();
        return inv[i];
    }

    void OnClickEquipList(int idx)
    {
        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);

        UnityEngine.UI.Image[] onMark =
        {
            GetImage((int)Images.Inventory_EquipList1_On), GetImage((int)Images.Inventory_EquipList2_On),
            GetImage((int)Images.Inventory_EquipList3_On), GetImage((int)Images.Inventory_EquipList4_On),
            GetImage((int)Images.Inventory_EquipList5_On), GetImage((int)Images.Inventory_EquipList6_On),
            GetImage((int)Images.Inventory_EquipList7_On), GetImage((int)Images.Inventory_EquipList8_On),
            GetImage((int)Images.Inventory_EquipList9_On), GetImage((int)Images.Inventory_EquipList10_On)
        };
        for (int i = 0; i < onMark.Length; ++i)
            onMark[i].gameObject.SetActive(false);

        System.Collections.Generic.List<int> owned = OwnedOf(_listType);
        int slot = idx - 1;                       // 목록 칸은 1 부터 온다
        if (slot < 0 || slot >= owned.Count)
            return;

        int equipId = owned[slot];
        onMark[slot].gameObject.SetActive(true);
        PrintEquipAbilityAndDesc(equipId);

        // 스탯 더하고 빼기와 슬롯 기록을 한 곳에서 한다. 예전에는 여기서 따로
        // 계산하고 CurSword 를 직접 대입해서, 부위마다 규칙이 갈라져 있었다.
        Managers.Game.SwapEquip(equipId);

        SetTypeListImage(_listType);
        RefreshSlotIcon(_listType, equipId);
        if (_listType == Define.Types.Sword)
            ShowSwordIllust();
        Refresh();
    }

    /// <summary>부위 칸의 그림을 지금 낀 것으로 바꾼다.</summary>
    void RefreshSlotIcon(Define.Types type, int equipId)
    {
        Sprite sprite = Managers.Resource.Load<Sprite>($"{Managers.Data.EquipDic[equipId].ImageName}");
        if (sprite == null)
            return;

        switch (type)
        {
            case Define.Types.Sword: GetImage((int)Images.sword).sprite = sprite; break;
            case Define.Types.Shield: GetImage((int)Images.shield).sprite = sprite; break;
            case Define.Types.Necklace: GetImage((int)Images.necklace).sprite = sprite; break;
            case Define.Types.Ring: GetImage((int)Images.ring).sprite = sprite; break;
            case Define.Types.Shoes: GetImage((int)Images.shoes).sprite = sprite; break;
            case Define.Types.Book: GetImage((int)Images.book).sprite = sprite; break;
        }
    }

    /// <summary>
    /// 인덱스에 맞는 장비에 대한 능력치와 설명을 보여준다.
    ///         public float ATK { get; set; }
    ///         public float DEF { get; set; }
    ///         public float HP { get; set; }
    ///         public float ASPD { get; set; }
    ///         public float DSPD { get; set; }
    ///         public float CRI { get; set; }
    ///         public float CRIATK { get; set; }
    ///         public float MSPD { get; set; }
    ///         이 순서로 seq 값이 씌워짐. seq는 0부터.
    /// </summary>
    /// <param name="idx"> 
    /// 장비의 인덱스 
    /// </param>
    void PrintEquipAbilityAndDesc(int equipId)
    {
        GetImage((int)Images.Inventory_MyInfo).gameObject.SetActive(false);
        GetObject((int)GameObjects.EquipInfo).gameObject.SetActive(true);

        int nameId = Managers.Data.EquipDic[equipId].NameId;
        GetText((int)Texts.EquipName).text = Managers.GetString(nameId);

        int descId = Managers.Data.EquipDic[equipId].DescId;
        GetText((int)Texts.InfoText).text = Managers.GetString(descId);

        GetObject((int)GameObjects.StatusInfoList).gameObject.SetActive(true);

        float[] statList =
        {
            Managers.Data.EquipDic[equipId].ATK, Managers.Data.EquipDic[equipId].DEF, Managers.Data.EquipDic[equipId].HP, Managers.Data.EquipDic[equipId].ASPD,
            Managers.Data.EquipDic[equipId].DSPD, Managers.Data.EquipDic[equipId].CRI, Managers.Data.EquipDic[equipId].CRIATK, Managers.Data.EquipDic[equipId].MSPD
        };

        // Status InfoList 정리
        Transform[] transforms = GetObject((int)GameObjects.StatusInfoList).GetComponentsInChildren<Transform>();
        if (transforms.Length > 1)
        {
            for (int i = 1; i < transforms.Length; i++)
            {
                Destroy(transforms[i].gameObject);
            }
        }

        for (int i = 0; i < statList.Length; i++)
        {
            if (statList[i] != 0)
            {
                UI_StatusInfo statusInfo = Managers.UI.MakeSubItem<UI_StatusInfo>(GetObject((int)GameObjects.StatusInfoList).transform);
                statusInfo.Init();
                statusInfo.Refresh(equipId, i);
                // 이름은 UI_StatusInfo 가 영어로 박아 쓴다("ATK", "DEF \n SPEED") — 한국어로 해도 장비 칸만 영어였다.
                // 왼쪽 능력치 칸과 같은 문구로 덮는다.
                statusInfo._statNameText.text = Managers.GetString(StatLabelTextIds[EquipStatLabel[i]]).Replace(' ', '\n');
            }
        }
    }

    /// <summary>
    /// 줄마다 위 = 합, 아래 = 맨몸 + 낀 장비가 더한 몫(초록). 예전에는 셋 다 합을 찍어 "228 + 228" 위에 228 이 섰다.
    /// 장비 몫은 SwapEquip 이 더하는 표 값의 합이다. 이동 속도만은 부츠가 배수로 정해서(EquipUtility.Apply) 기준 1 을 뺀
    /// 나머지다. 체력·레벨 줄은 "지금 / 최대" 라 그대로 둔다.
    /// </summary>
    void SetPlayerStatusInfo()
    {
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        Split(Texts.TotalATK, Texts.BaseATK, Texts.AddATK, p.Attack, Equipped(e => e.ATK));
        Split(Texts.TotalDEF, Texts.BaseDEF, Texts.AddDEF, p.Defence, Equipped(e => e.DEF));
        Split(Texts.TotalCRI, Texts.BaseCRI, Texts.AddCRI, p.Critical, Equipped(e => e.CRI));
        Split(Texts.TotalCRIATK, Texts.BaseCRIATK, Texts.AddCRIATK, p.CriticalAttack, Equipped(e => e.CRIATK));
        Split(Texts.TotalATKSPEED, Texts.BaseATKSPEED, Texts.AddATKSPEED, p.AttackSpeed, Equipped(e => e.ASPD));
        Split(Texts.TotalDEFSPEED, Texts.BaseDEFSPEED, Texts.AddDEFSPEED, p.DefenceSpeed, Equipped(e => e.DSPD));
        Split(Texts.TotalMOVESPEED, Texts.BaseMOVESPEED, Texts.AddMOVESPEED, p.MoveSpeed, p.MoveSpeed - 1f);

        GetText((int)Texts.TotalHP).text = p.MaxHP.ToString();
        GetText((int)Texts.AddHP).text = p.MaxHP.ToString();
        GetText((int)Texts.BaseHP).text = p.CurHP.ToString();

        GetText((int)Texts.TotalLV).text = p.Level.ToString();
        GetText((int)Texts.AddLV).text = Managers.Data.PlayerDic.TryGetValue(p.Level + 1, out var nextLevel)
            ? nextLevel.NeedExp.ToString() : "-";
        GetText((int)Texts.BaseLV).text = p.CurExp.ToString();
    }

    void Split(Texts total, Texts own, Texts add, float value, float fromEquip)
    {
        GetText((int)total).text = Num(value);
        GetText((int)own).text = Num(value - fromEquip);
        GetText((int)add).text = Num(fromEquip);
    }

    // 뺄셈이 2.2 - 0.5 = 1.7000001 로 찍히지 않게 둘째 자리에서 자른다.
    static string Num(float v) => (Mathf.Round(v * 100f) / 100f).ToString();

    /// <summary>지금 낀 장비(검·방패·목걸이·반지·부츠·책)의 그 능력치 합.</summary>
    static float Equipped(Func<Data.EquipData, float> stat)
    {
        GameManager.CurPlayerData p = Managers.Game.PlayerData;
        float sum = 0f;
        foreach (int id in new[] { p.CurSword, p.CurShield, p.CurNecklace, p.CurRing, p.CurShoes, p.CurBook })
        {
            Data.EquipData eq;
            if (id != Define.NOT_EQUIP && Managers.Data.EquipDic.TryGetValue(id, out eq))
                sum += stat(eq);
        }
        return sum;
    }

    void OnPointerEnterImage()
    {
        GetImage((int)Images.ATKInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.ATKInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.DEFInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.DEFInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.HPInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.HPInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.CRIInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.CRIInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.CRIATKInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.CRIATKInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.LVInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.LVInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.ATKSPEEDInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.ATKSPEEDInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.DEFSPEEDInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.DEFSPEEDInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MOVESPEEDInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.MOVESPEEDInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);

    }

    void OnPointerExitImage()
    {
        GetImage((int)Images.ATKInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.ATKInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.DEFInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.DEFInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.HPInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.HPInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.CRIInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.CRIInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.CRIATKInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.CRIATKInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.LVInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.LVInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.ATKSPEEDInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.ATKSPEEDInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.DEFSPEEDInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.DEFSPEEDInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.MOVESPEEDInfo).gameObject.BindEvent(() =>
        {
            GetImage((int)Images.MOVESPEEDInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
    }



}
