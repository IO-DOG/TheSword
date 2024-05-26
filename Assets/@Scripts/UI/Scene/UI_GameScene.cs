using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Managers.Game.CurPlayerData.CurExp += 10;
        }

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 100.0f, _mask);
            Debug.DrawRay(Camera.main.transform.position, ray.direction * 100.0f, Color.red, 1.0f);

            if (raycastHit)
            {
                if (hit.collider.gameObject.layer == (int)Define.Layer.Monster)
                {
                    Debug.Log("몬스터 맞음");
                    MonsterController monster = hit.collider.gameObject.GetComponent<MonsterController>();
                    int id = monster.id;
                    Debug.Log($"MonsterName : {Managers.Data.MonsterDic[id].Name}");
                    Debug.Log($"MonsterImage : {Managers.Data.MonsterDic[id].IdleAnimStr}");
                    Debug.Log($"MonsterImage : {Managers.Data.MonsterDic[id].IdleAnimStr}");

                    UI_MonsterInfo monsterInfo = Managers.UI.MakeSubItem<UI_MonsterInfo>(monster.transform);
                    //monsterInfo.Position = monster.gameObject.transform.localToWorldMatrix.GetPosition();
                    monsterInfo.Position = Util.ScreenToWorldCood(Input.mousePosition);
                    Debug.Log($"{Input.mousePosition.x},{Input.mousePosition.y},{Input.mousePosition.z}");
                    Debug.Log($"Monster Position X : {monster.gameObject.transform.position.x}, Monster Position Y : {monster.gameObject.transform.position.y}, Monster Position Z : {monster.gameObject.transform.position.z}");
                }
            }
        }
    }

    /// <summary>
    /// 보여지는 플레이어 정보를 갱신하는 함수
    /// 플레이어 정보가 추가되면 이함수 역시 추가되어야함.
    /// </summary>
    public void SetPlayerInfo()
    {
        //GetText((int)Texts.PlayerNameText).text = "PlayerName";
        GetText((int)Texts.PlayerHPText).text = $"HP : {Managers.Game.CurPlayerData.MaxHP} / {Managers.Game.CurPlayerData.CurHP}";
        GetText((int)Texts.PlayerAttackText).text = $"Attack : {Managers.Game.CurPlayerData.Attack}";
        GetText((int)Texts.PlayerDefenseText).text = $"Defense : {Managers.Game.CurPlayerData.Defence}";
    }
}
