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
    }

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

        OnClickInventory_MyInfo_On();
        SortInven();
        Refresh();

        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            ClosePopupUI();
        }
    }

    void SortInven()
    {
        Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Necklace].Sort();
        Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Ring].Sort();
        Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shoes].Sort();
        Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Book].Sort();
        Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Sort();
        Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shield].Sort();
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

        if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Necklace].Count == 0)
            GetImage((int)Images.necklace).color = new Color(1, 1, 1, 0);
        else
        {
            GetImage((int)Images.necklace).color = new Color(1, 1, 1, 1);
            int idx = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Necklace][Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Necklace].Count - 1];
            GetImage((int)Images.necklace).sprite = Managers.Resource.Load<Sprite>(
                $"{Managers.Data.EquipDic[idx].ImageName}");
        }

        if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Ring].Count == 0)
            GetImage((int)Images.ring).color = new Color(1, 1, 1, 0);
        else
        {
            GetImage((int)Images.ring).color = new Color(1, 1, 1, 1);
            int idx = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Ring][Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Ring].Count - 1];
            GetImage((int)Images.ring).sprite = Managers.Resource.Load<Sprite>(
                $"{Managers.Data.EquipDic[idx].ImageName}");
        }

        if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shoes].Count == 0)
            GetImage((int)Images.shoes).color = new Color(1, 1, 1, 0);
        else
        {
            GetImage((int)Images.shoes).color = new Color(1, 1, 1, 1);
            int idx = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shoes][Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shoes].Count - 1];
            GetImage((int)Images.shoes).sprite = Managers.Resource.Load<Sprite>(
                $"{Managers.Data.EquipDic[idx].ImageName}");
        }

        if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Book].Count == 0)
            GetImage((int)Images.book).color = new Color(1, 1, 1, 0);
        else
        {
            GetImage((int)Images.book).color = new Color(1, 1, 1, 1);
            int idx = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Book][Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Book].Count - 1];
            GetImage((int)Images.book).sprite = Managers.Resource.Load<Sprite>(
                $"{Managers.Data.EquipDic[idx].ImageName}");
        }

        // todo check curplayer's sowrd
        if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Count == 0)
            GetImage((int)Images.sword).color = new Color(1, 1, 1, 0);
        else
        {
            GetImage((int)Images.sword).color = new Color(1, 1, 1, 1);
            int idx = Managers.Game.CurPlayerData.CurSword;
            GetImage((int)Images.sword).sprite = Managers.Resource.Load<Sprite>(
                $"{Managers.Data.EquipDic[idx].ImageName}");
        }

        // to check player class
        GetImage((int)Images.Class).color = new Color(1, 1, 1, 0);

        if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shield].Count == 0)
            GetImage((int)Images.shield).color = new Color(1, 1, 1, 0);
        else
        {
            GetImage((int)Images.shield).color = new Color(1, 1, 1, 1);
            int idx = Managers.Game.CurPlayerData.CurShield;
            GetImage((int)Images.shield).sprite = Managers.Resource.Load<Sprite>(
                $"{Managers.Data.EquipDic[idx].ImageName}");
        }

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
            // TODO Add status
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

    void OnClickSword()
    {
        Refresh();
        SetSwordListImage();
        PrintEquipAbilityAndDesc(Managers.Game.CurPlayerData.CurSword);

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
            if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Count >= i)
            {
                equipList[i - 1].color = Color.white;
                int idx = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword][i - 1];
                equipList[i - 1].sprite = Managers.Resource.Load<Sprite>($"{Managers.Data.EquipDic[idx].ImageName}");
                inventory_equipList_get[i - 1].gameObject.SetActive(true);
            }
        }
    }

    void ShowSwordIllust()
    {
        GetObject((int)GameObjects.EquipIllust).gameObject.SetActive(true);

        int curSwordIdx = Managers.Game.CurPlayerData.CurSword;
        int idx = curSwordIdx - 8;

        GetImage((int)Images.IllustBG).sprite = Managers.Resource.Load<Sprite>($"{Managers.Data.EquipDic[curSwordIdx].IllustBG}");
        GetImage((int)Images.Illust).sprite = Managers.Resource.Load<Sprite>($"{Managers.Data.EquipDic[curSwordIdx].Illust}");
        StartCoroutine(CoIllustUIEffect());
        GetImage((int)Images.IllustFX).GetComponent<Animator>().Play($"{Managers.Data.EquipDic[curSwordIdx].IllustFX}");
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
        Refresh();
        SetShieldListImage();
        PrintEquipAbilityAndDesc(Managers.Game.CurPlayerData.CurShield);

        GetImage((int)Images.Inventory_Shield_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_Shield_On).gameObject.SetActive(true);

        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);

        // TODO Check get shield
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
            if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shield].Count >= i)
            {
                equipList[i - 1].color = Color.white;
                int idx = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shield][i - 1];
                equipList[i - 1].sprite = Managers.Resource.Load<Sprite>($"{Managers.Data.EquipDic[idx].ImageName}");
                inventory_equipList_get[i - 1].gameObject.SetActive(true);
            }
        }
    }

    void OnClickNecklace()
    {
        Refresh();
        GetImage((int)Images.Inventory_accessory_necklace_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_necklace_On).gameObject.SetActive(true);

        int idx = Managers.Game.CurPlayerData.CurNecklace;
        PrintEquipAbilityAndDesc(idx);
    }

    void OnClickRing()
    {
        Refresh();
        GetImage((int)Images.Inventory_accessory_ring_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_ring_On).gameObject.SetActive(true);

        int idx = Managers.Game.CurPlayerData.CurRing;
        PrintEquipAbilityAndDesc(idx);
    }

    void OnClickShoes()
    {
        Refresh();
        GetImage((int)Images.Inventory_accessory_shoes_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_shoes_On).gameObject.SetActive(true);

        int idx = Managers.Game.CurPlayerData.CurShoes;
        PrintEquipAbilityAndDesc(idx);
    }

    void OnClickBook()
    {
        Refresh();
        GetImage((int)Images.Inventory_accessory_book_Get).gameObject.SetActive(true);
        GetImage((int)Images.Inventory_accessory_book_On).gameObject.SetActive(true);

        int idx = Managers.Game.CurPlayerData.CurBook;
        PrintEquipAbilityAndDesc(idx);
    }

    void OnClickEquipList(int idx)
    {
        Debug.Log($"OnClickEquipList 실행. idx : {idx}");

        UnityEngine.UI.Image[] equipList =
        {
            GetImage((int)Images.EquipList1), GetImage((int)Images.EquipList2), GetImage((int)Images.EquipList3), GetImage((int)Images.EquipList4), GetImage((int)Images.EquipList5),
            GetImage((int)Images.EquipList6), GetImage((int)Images.EquipList7), GetImage((int)Images.EquipList8), GetImage((int)Images.EquipList9), GetImage((int)Images.EquipList10)
        };
        UnityEngine.UI.Image[] inventory_equipList_get =
        {
            GetImage((int)Images.Inventory_EquipList1_Get), GetImage((int)Images.Inventory_EquipList2_Get), GetImage((int)Images.Inventory_EquipList3_Get), GetImage((int)Images.Inventory_EquipList4_Get),
            GetImage((int)Images.Inventory_EquipList5_Get), GetImage((int)Images.Inventory_EquipList6_Get), GetImage((int)Images.Inventory_EquipList7_Get), GetImage((int)Images.Inventory_EquipList8_Get),
            GetImage((int)Images.Inventory_EquipList9_Get), GetImage((int)Images.Inventory_EquipList10_Get)
        };
        UnityEngine.UI.Image[] inventory_equipList_on =
        {
            GetImage((int)Images.Inventory_EquipList1_On), GetImage((int)Images.Inventory_EquipList2_On), GetImage((int)Images.Inventory_EquipList3_On), GetImage((int)Images.Inventory_EquipList4_On),
            GetImage((int)Images.Inventory_EquipList5_On), GetImage((int)Images.Inventory_EquipList6_On), GetImage((int)Images.Inventory_EquipList7_On), GetImage((int)Images.Inventory_EquipList8_On),
            GetImage((int)Images.Inventory_EquipList9_On), GetImage((int)Images.Inventory_EquipList10_On)
        };

        //Refresh();
        GetImage((int)Images.Inventory_EquipList).gameObject.SetActive(true);
        int equipIdx = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword][idx - 1];
        // 검 리스트를 보여줘야 할 때
        if (equipIdx >= Define.EQUIP_SOWRD_FIRST && equipIdx <= Define.EQUIP_SOWRD_END)
        {
            //Refresh();

            for (int i = 0; i < inventory_equipList_on.Length; ++i)
            {
                inventory_equipList_on[i].gameObject.SetActive(false);
            }

            Debug.Log($"검 인덱스");
            SetSwordListImage();
            if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword].Count >= idx)
            {
                inventory_equipList_get[idx - 1].gameObject.SetActive(true);
                inventory_equipList_on[idx - 1].gameObject.SetActive(true);
                int temp = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Sword][idx - 1];
                Debug.Log($"검 인덱스 {temp}");
                PrintEquipAbilityAndDesc(temp);
                int curSwordIdx = Managers.Game.CurPlayerData.CurSword;
                SwapSword(curSwordIdx, temp);
                Managers.Game.CurPlayerData.CurSword = temp;
                ShowSwordIllust();
                GetImage((int)Images.sword).sprite = Managers.Resource.Load<Sprite>($"{Managers.Data.EquipDic[temp].ImageName}");
            }
        }
        else // 방패 리스트를 보여줘야 할 때
        {
            //Refresh();

            if (Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shield].Count >= idx)
            {
                inventory_equipList_get[idx].gameObject.SetActive(true);
                inventory_equipList_on[idx].gameObject.SetActive(true);
                int temp = Managers.Game.CurPlayerData.Inventory[(int)Define.Types.Shield][idx];
                PrintEquipAbilityAndDesc(temp);
                int curShieldIdx = Managers.Game.CurPlayerData.CurShield;
                SwapShield(curShieldIdx, temp);
                Managers.Game.CurPlayerData.CurShield = temp;
                GetImage((int)Images.shield).sprite = Managers.Resource.Load<Sprite>($"{Managers.Data.EquipDic[temp].ImageName}");
            }
        }
    }

    void SwapSword(int curSwordIdx, int swordIdx)
    {
        if (curSwordIdx == 0)
        {
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[swordIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[swordIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[swordIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[swordIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[swordIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[swordIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[swordIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[swordIdx].MSPD;
            return;
        }
        else
        {
            Managers.Game.CurPlayerData.Attack -= Managers.Data.EquipDic[curSwordIdx].ATK;
            Managers.Game.CurPlayerData.Defence -= Managers.Data.EquipDic[curSwordIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP -= Managers.Data.EquipDic[curSwordIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed -= Managers.Data.EquipDic[curSwordIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed -= Managers.Data.EquipDic[curSwordIdx].DSPD;
            Managers.Game.CurPlayerData.Critical -= Managers.Data.EquipDic[curSwordIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack -= Managers.Data.EquipDic[curSwordIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed -= Managers.Data.EquipDic[curSwordIdx].MSPD;

            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[swordIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[swordIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[swordIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[swordIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[swordIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[swordIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[swordIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[swordIdx].MSPD;
        }

        Managers.Game.GameScene.Refresh();
    }

    void SwapShield(int curShieldIdx, int shieldIdx)
    {
        if (curShieldIdx == 0)
        {
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[shieldIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[shieldIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[shieldIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[shieldIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[shieldIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[shieldIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[shieldIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[shieldIdx].MSPD;
            return;
        }
        else
        {
            Managers.Game.CurPlayerData.Attack -= Managers.Data.EquipDic[curShieldIdx].ATK;
            Managers.Game.CurPlayerData.Defence -= Managers.Data.EquipDic[curShieldIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP -= Managers.Data.EquipDic[curShieldIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed -= Managers.Data.EquipDic[curShieldIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed -= Managers.Data.EquipDic[curShieldIdx].DSPD;
            Managers.Game.CurPlayerData.Critical -= Managers.Data.EquipDic[curShieldIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack -= Managers.Data.EquipDic[curShieldIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed -= Managers.Data.EquipDic[curShieldIdx].MSPD;

            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[shieldIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[shieldIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[shieldIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[shieldIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[shieldIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[shieldIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[shieldIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[shieldIdx].MSPD;
        }

        Managers.Game.GameScene.Refresh();
    }

    public void SwapEquip(int curIdx, int idx)
    {
        if (curIdx == 0)
        {
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[curIdx].MSPD;
            return;
        }
        else
        {
            Managers.Game.CurPlayerData.Attack -= Managers.Data.EquipDic[curIdx].ATK;
            Managers.Game.CurPlayerData.Defence -= Managers.Data.EquipDic[curIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP -= Managers.Data.EquipDic[curIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed -= Managers.Data.EquipDic[curIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed -= Managers.Data.EquipDic[curIdx].DSPD;
            Managers.Game.CurPlayerData.Critical -= Managers.Data.EquipDic[curIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack -= Managers.Data.EquipDic[curIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed -= Managers.Data.EquipDic[curIdx].MSPD;

            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[idx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[idx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[idx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[idx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[idx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[idx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[idx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[idx].MSPD;
        }

        Managers.Game.GameScene.Refresh();
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

        // todo script dic to script
        //GetText((int)Texts.EquipInfo).text = Managers.Data.ScriptDic[descId].ToString();

        // todo for scriptDic
        //GetText((int)Texts.EquipName).text = Managers.Data.EquipDic[equipId].NameId.ToString();
        int nameId = Managers.Data.EquipDic[equipId].NameId;
        Debug.Log(nameId);
        GetText((int)Texts.EquipName).text = Managers.GetString(nameId);

        // todo for scriptDic
        //GetText((int)Texts.InfoText).text = Managers.Data.EquipDic[equipId].DescId.ToString();
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
                Debug.Log($"equipId : {equipId}, i : {i}");
                statusInfo.Refresh(equipId, i);
            }
        }
    }

    void SetPlayerStatusInfo()
    {
        GetText((int)Texts.TotalATK).text = Managers.Game.CurPlayerData.Attack.ToString();
        GetText((int)Texts.AddATK).text = Managers.Game.CurPlayerData.Attack.ToString();
        GetText((int)Texts.BaseATK).text = Managers.Game.CurPlayerData.Attack.ToString();

        GetText((int)Texts.TotalDEF).text = Managers.Game.CurPlayerData.Defence.ToString();
        GetText((int)Texts.AddDEF).text = Managers.Game.CurPlayerData.Defence.ToString();
        GetText((int)Texts.BaseDEF).text = Managers.Game.CurPlayerData.Defence.ToString();

        GetText((int)Texts.TotalHP).text = Managers.Game.CurPlayerData.MaxHP.ToString();
        GetText((int)Texts.AddHP).text = Managers.Game.CurPlayerData.MaxHP.ToString();
        GetText((int)Texts.BaseHP).text = Managers.Game.CurPlayerData.CurHP.ToString();

        GetText((int)Texts.TotalCRI).text = Managers.Game.CurPlayerData.Critical.ToString();
        GetText((int)Texts.AddCRI).text = Managers.Game.CurPlayerData.Critical.ToString();
        GetText((int)Texts.BaseCRI).text = Managers.Game.CurPlayerData.Critical.ToString();

        GetText((int)Texts.TotalCRIATK).text = Managers.Game.CurPlayerData.CriticalAttack.ToString();
        GetText((int)Texts.AddCRIATK).text = Managers.Game.CurPlayerData.CriticalAttack.ToString();
        GetText((int)Texts.BaseCRIATK).text = Managers.Game.CurPlayerData.CriticalAttack.ToString();

        GetText((int)Texts.TotalLV).text = Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].TotalExp.ToString();
        GetText((int)Texts.AddLV).text = Managers.Game.CurPlayerData.CurExp.ToString();
        GetText((int)Texts.BaseLV).text = Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].NeedExp.ToString();

        GetText((int)Texts.TotalATKSPEED).text = Managers.Game.CurPlayerData.AttackSpeed.ToString();
        GetText((int)Texts.AddATKSPEED).text = Managers.Game.CurPlayerData.AttackSpeed.ToString();
        GetText((int)Texts.BaseATKSPEED).text = Managers.Game.CurPlayerData.AttackSpeed.ToString();

        GetText((int)Texts.TotalDEFSPEED).text = Managers.Game.CurPlayerData.DefenceSpeed.ToString();
        GetText((int)Texts.AddDEFSPEED).text = Managers.Game.CurPlayerData.DefenceSpeed.ToString();
        GetText((int)Texts.BaseDEFSPEED).text = Managers.Game.CurPlayerData.DefenceSpeed.ToString();

        GetText((int)Texts.TotalMOVESPEED).text = Managers.Game.CurPlayerData.MoveSpeed.ToString();
        GetText((int)Texts.AddMOVESPEED).text = Managers.Game.CurPlayerData.MoveSpeed.ToString();
        GetText((int)Texts.BaseMOVESPEED).text = Managers.Game.CurPlayerData.MoveSpeed.ToString();
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

    public void SetPlayerStatByEquip()
    {
        if (Managers.Game.CurPlayerData.CurSword != 0)
        {
            int swordIdx = Managers.Game.CurPlayerData.CurSword;
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[swordIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[swordIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[swordIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[swordIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[swordIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[swordIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[swordIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[swordIdx].MSPD;
            // TODO ADD AbilityID
        }
        if (Managers.Game.CurPlayerData.CurShield != 0)
        {
            int shieldIdx = Managers.Game.CurPlayerData.CurShield;
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[shieldIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[shieldIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[shieldIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[shieldIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[shieldIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[shieldIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[shieldIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[shieldIdx].MSPD;
            // TODO ADD AbilityID
        }
        if (Managers.Game.CurPlayerData.CurShoes != 0)
        {
            int shoesIdx = Managers.Game.CurPlayerData.CurShoes;
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[shoesIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[shoesIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[shoesIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[shoesIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[shoesIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[shoesIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[shoesIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[shoesIdx].MSPD;
            // TODO ADD AbilityID
        }
        if (Managers.Game.CurPlayerData.CurNecklace != 0)
        {
            int necklaceIdx = Managers.Game.CurPlayerData.CurNecklace;
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[necklaceIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[necklaceIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[necklaceIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[necklaceIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[necklaceIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[necklaceIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[necklaceIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[necklaceIdx].MSPD;
            // TODO ADD AbilityID
        }
        if (Managers.Game.CurPlayerData.CurRing != 0)
        {
            int ringIdx = Managers.Game.CurPlayerData.CurRing;
            Managers.Game.CurPlayerData.Attack += Managers.Data.EquipDic[ringIdx].ATK;
            Managers.Game.CurPlayerData.Defence += Managers.Data.EquipDic[ringIdx].DEF;
            Managers.Game.CurPlayerData.MaxHP += Managers.Data.EquipDic[ringIdx].HP;
            Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.EquipDic[ringIdx].ASPD;
            Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.EquipDic[ringIdx].DSPD;
            Managers.Game.CurPlayerData.Critical += Managers.Data.EquipDic[ringIdx].CRI;
            Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.EquipDic[ringIdx].CRIATK;
            Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.EquipDic[ringIdx].MSPD;
            // TODO ADD AbilityID
        }
    }


}
