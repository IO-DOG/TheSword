using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameScene : UI_Scene
{
    #region Enum
    enum Buttons
    {
        //ToTitleButton,
    }

    enum GameObjects
    {
        KeyInventory,
    }

    enum Texts
    {
        //PlayerNameText,
        PlayerHPText,
        PlayerAttackText,
        PlayerDefenseText,
        PlayerLevelText,
    }

    enum Images
    {
        MainUIEXPGaugeImage,
        MainUIAuxiliaryHPGaugeImage,
        MainUIOptionAImage,
        MainUIInventoryAImage,
        MainUISwordAImage,
        MainUIWarpAImage,
    }

    #endregion

    int _mask = (1 << (int)Define.Layer.Monster);

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);
        //GetButton((int)Buttons.ToTitleButton).gameObject.BindEvent(() => Managers.Scene.LoadScene(Define.Scene.TitleScene));

        #region PointerEnter&PointerExit
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Option_B); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Option_A); ; }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Inventory_B); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Inventory_A); ; }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Sword_B); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Sword_A); ; }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Warp_B); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpAImage).sprite = Managers.Resource.Load<Sprite>(Define.MainUI_Warp_A); ; }, null, Define.UIEvent.PointerExit);
        #endregion


        CheckMonster();
        CheckItem();
        CheckDoor();
        SetPlayerInfo();
        Refresh();

        return true;
    }

    public void Refresh()
    {
        GetText((int)Texts.PlayerLevelText).text = Managers.Game.CurPlayerData.Level.ToString();
        int level = Managers.Game.CurPlayerData.Level;
        Debug.Log($"{Managers.Game.CurPlayerData.CurExp} , {Managers.Data.PlayerDic[level].NeedExp}");
        //GetImage((int)Images.MainUIEXPGaugeImage).fillAmount = Managers.Game.CurPlayerData.CurExp / Managers.Data.PlayerDic[level].NeedExp;
        GetImage((int)Images.MainUIAuxiliaryHPGaugeImage).fillAmount = Managers.Game.CurPlayerData.CurHP / Managers.Game.CurPlayerData.MaxHP;
        Managers.Game.Inventory.ShowKeySlot(Managers.Game.Player._keyInventory);
    }

    void CheckMonster()
    {
        GameObject go = GameObject.Find("Monsters");
        MonsterController[] monsters = go.GetComponentsInChildren<MonsterController>();

        foreach (MonsterController monster in monsters)
        {
            if (Managers.Data.MonsterActiveDic[monster._monsterIndex_forActive] == false)
            {
                monster.gameObject.SetActive(false);
                continue;
            }

            int id = monster.id;
            monster.GetComponent<Animator>().Play($"{Managers.Data.MonsterDic[id].IdleAnimStr}");
        }
    }
    
    void CheckItem()
    {
        GameObject go = GameObject.Find("Items");
        Item[] items = go.GetComponentsInChildren<Item>();

        foreach (Item item in items)
        {
            if (Managers.Data.ItemActiveDic[item._itemIndex_forActive] == false)
            {
                item.gameObject.SetActive(false);
                continue;
            }

            int id = item.id;
            item.GetComponent<Animator>().Play($"ConsumableItem_{id}");
        }
    }

    void CheckDoor()
    {
        GameObject go = GameObject.Find("Parent");
        Door[] doors = go.GetComponentsInChildren<Door>();

        foreach (Door door in doors)
        {
            if (Managers.Data.ItemActiveDic[door._doorIndex_forActive] == false)
            {
                door.gameObject.transform.parent.gameObject.SetActive(false);
                continue;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Managers.Game.CurPlayerData.CurExp += 10;
        }
    }

    /// <summary>
    /// �������� �÷��̾� ������ �����ϴ� �Լ�
    /// �÷��̾� ������ �߰��Ǹ� ���Լ� ���� �߰��Ǿ����.
    /// </summary>
    public void SetPlayerInfo()
    {
        //GetText((int)Texts.PlayerNameText).text = "PlayerName";
        GetText((int)Texts.PlayerHPText).text = $"HP : {Managers.Game.CurPlayerData.MaxHP} / {Managers.Game.CurPlayerData.CurHP}";
        GetText((int)Texts.PlayerAttackText).text = $"Attack : {Managers.Game.CurPlayerData.Attack}";
        GetText((int)Texts.PlayerDefenseText).text = $"Defense : {Managers.Game.CurPlayerData.Defence}";
    }
}
