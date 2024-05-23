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
    #endregion

    int _mask = (1 << (int)Define.Layer.Monster);

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        #endregion

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);
        //GetButton((int)Buttons.ToTitleButton).gameObject.BindEvent(() => Managers.Scene.LoadScene(Define.Scene.TitleScene));
        CheckMonster();
        CheckItem();

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
}
