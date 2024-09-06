using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SHJTestScene : UI_Scene
{
    #region Enum

    enum Buttons
    {
        ToTitleButton,
    }

    enum Objects
    {

    }

    enum Images
    {

    }

    enum Texts
    {

    }

    #endregion

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindButton(typeof(Buttons));
        #endregion

        GetButton((int)Buttons.ToTitleButton).gameObject.BindEvent(() => Managers.Scene.LoadScene(Define.Scene.TitleScene));
        CheckMonster();

        return true;
    }

    void CheckMonster()
    {
        //GameObject go = GameObject.Find("Monsters");
        //MonsterController[] monsters = go.GetComponentsInChildren<MonsterController>();

        //foreach (MonsterController monster in monsters)
        //{
        //    if (Managers.Data.MonsterActiveDic[monster._monsterIndex_forActive] == false)
        //    {
        //        monster.gameObject.SetActive(false);
        //        continue;
        //    }

        //    int id = monster.id;
        //    monster.GetComponent<Animator>().Play($"{Managers.Data.MonsterDic[id].IdleAnimStr}");
        //    Debug.Log($"{Managers.Data.MonsterDic[id].IdleAnimStr}");
        //}
    }
}
