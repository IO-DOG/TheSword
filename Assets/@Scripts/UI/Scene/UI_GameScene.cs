using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GameScene : UI_Scene
{
    enum Buttons
    {
        //ToTitleButton,
    }

    enum GameObjects
    {
        KeyInventory,
    }

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
}
