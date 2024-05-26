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
        PlayerNameText,
        PlayerHPText,
        PlayerAttackText,
        PlayerDefenseText,
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
        #endregion

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);
        //GetButton((int)Buttons.ToTitleButton).gameObject.BindEvent(() => Managers.Scene.LoadScene(Define.Scene.TitleScene));
        CheckMonster();
        CheckItem();
        CheckDoor();
        SetPlayerInfo();

        return true;
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
        GetText((int)Texts.PlayerNameText).text = "PlayerName";
        GetText((int)Texts.PlayerHPText).text = $"HP : {Managers.Game.CurPlayerData.MaxHP} / {Managers.Game.CurPlayerData.CurHP}";
        GetText((int)Texts.PlayerAttackText).text = $"Attack : {Managers.Game.CurPlayerData.Attack}";
        GetText((int)Texts.PlayerDefenseText).text = $"Defense : {Managers.Game.CurPlayerData.Defence}";
    }
}
