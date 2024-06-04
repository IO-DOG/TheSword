using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

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
    }

    enum Texts
    {
        InfoText,
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
    }

    enum GameObjects
    {

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

        OnClickInventory_MyInfo_On();

        Refresh();

        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ClosePopupUI();
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
        GetImage((int)Images.necklace).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.ring).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.shoes).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.book).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.sword).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.Class).color = new Color(1, 1, 1, 0);
        GetImage((int)Images.shield).color = new Color(1, 1, 1, 0);
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
    }

    void OnClickInventory_MyInfo_On()
    {
        if (_isInventory_MyInfo_On == false)
        {
            _isInventory_MyInfo_On = true;
            GetImage((int)Images.Inventory_MyInfo_On).color = new Color(1, 1, 1, 1);
            GetImage((int)Images.Inventory_InfoFrame).gameObject.SetActive(true);
            GetImage((int)Images.Inventory_MyInfo).gameObject.SetActive(true);
            GetText((int)Texts.InfoText).text = "";
            GetText((int)Texts.InfoText).gameObject.SetActive(false);
            // TODO Add status
        }
        else
        {
            _isInventory_MyInfo_On = false;
            GetImage((int)Images.Inventory_MyInfo_On).color = new Color(1, 1, 1, 0);
            GetImage((int)Images.Inventory_InfoFrame).gameObject.SetActive(false);
            GetImage((int)Images.Inventory_MyInfo).gameObject.SetActive(false);
            GetText((int)Texts.InfoText).gameObject.SetActive(true);
            GetText((int)Texts.InfoText).text = "";
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
        GetImage((int)Images.ATKInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.ATKInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.DEFInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.DEFInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.HPInfo).gameObject.BindEvent(() => { 
            GetImage((int)Images.HPInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.CRIInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.CRIInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.CRIATKInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.CRIATKInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.LVInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.LVInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.ATKSPEEDInfo).gameObject.BindEvent(() => { 
            GetImage((int)Images.ATKSPEEDInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.DEFSPEEDInfo).gameObject.BindEvent(() => { 
            GetImage((int)Images.DEFSPEEDInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MOVESPEEDInfo).gameObject.BindEvent(() => { 
            GetImage((int)Images.MOVESPEEDInfoImage).gameObject.SetActive(true);
        }, null, Define.UIEvent.PointerEnter);

    }

    void OnPointerExitImage()
    {
        GetImage((int)Images.ATKInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.ATKInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.DEFInfo).gameObject.BindEvent(() => { 
            GetImage((int)Images.DEFInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.HPInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.HPInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.CRIInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.CRIInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.CRIATKInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.CRIATKInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.LVInfo).gameObject.BindEvent(() => { 
            GetImage((int)Images.LVInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.ATKSPEEDInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.ATKSPEEDInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.DEFSPEEDInfo).gameObject.BindEvent(() => { 
            GetImage((int)Images.DEFSPEEDInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
        GetImage((int)Images.MOVESPEEDInfo).gameObject.BindEvent(() => {
            GetImage((int)Images.MOVESPEEDInfoImage).gameObject.SetActive(false);
        }, null, Define.UIEvent.PointerExit);
    }
}
