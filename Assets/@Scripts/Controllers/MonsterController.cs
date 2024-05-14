using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public int id = 0;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Player")
        {
            //전투 구현
            Managers.Game.MonsterData.id = Managers.Data.MonsterDic[id].id;
            Managers.Game.MonsterData.Chapter = Managers.Data.MonsterDic[id].Chapter;
            Managers.Game.MonsterData.Class = Managers.Data.MonsterDic[id].Class;
            Managers.Game.MonsterData.Name = Managers.Data.MonsterDic[id].Name;
            Managers.Game.MonsterData.Feature = Managers.Data.MonsterDic[id].Feature;
            Managers.Game.MonsterData.MaxHP = Managers.Data.MonsterDic[id].MaxHP;
            Managers.Game.MonsterData.CurHP = Managers.Data.MonsterDic[id].MaxHP;
            Managers.Game.MonsterData.Attack = Managers.Data.MonsterDic[id].Attack;
            Managers.Game.MonsterData.Defence = Managers.Data.MonsterDic[id].Defence;
            Managers.Game.MonsterData.AttackSpeed = Managers.Data.MonsterDic[id].AttackSpeed;
            Managers.Game.MonsterData.DefenceSpeed = Managers.Data.MonsterDic[id].DefenceSpeed;
            Managers.Game.MonsterData.RewardExp = Managers.Data.MonsterDic[id].Exp;
            Managers.Game.MonsterData.IsDefence = false;
            //Managers.Game.MonsterData.Image = Managers.Data.MonsterDic[id].Image;

            Managers.Game.Monster = this;
            Managers.UI.ShowPopupUI<UI_BattlePopup>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Player")
        {
            //전투 구현
            Managers.Game.MonsterData.id = Managers.Data.MonsterDic[id].id;
            Managers.Game.MonsterData.Chapter = Managers.Data.MonsterDic[id].Chapter;
            Managers.Game.MonsterData.Class = Managers.Data.MonsterDic[id].Class;
            Managers.Game.MonsterData.Name = Managers.Data.MonsterDic[id].Name;
            Managers.Game.MonsterData.Feature = Managers.Data.MonsterDic[id].Feature;
            Managers.Game.MonsterData.MaxHP = Managers.Data.MonsterDic[id].MaxHP;
            Managers.Game.MonsterData.CurHP = Managers.Data.MonsterDic[id].MaxHP;
            Managers.Game.MonsterData.Attack = Managers.Data.MonsterDic[id].Attack;
            Managers.Game.MonsterData.Defence = Managers.Data.MonsterDic[id].Defence;
            Managers.Game.MonsterData.AttackSpeed = Managers.Data.MonsterDic[id].AttackSpeed;
            Managers.Game.MonsterData.DefenceSpeed = Managers.Data.MonsterDic[id].DefenceSpeed;
            Managers.Game.MonsterData.RewardExp = Managers.Data.MonsterDic[id].Exp;
            Managers.Game.MonsterData.IsDefence = false;
            //Managers.Game.MonsterData.Image = Managers.Data.MonsterDic[id].Image;

            Managers.Game.Monster = this;
            Managers.UI.ShowPopupUI<UI_BattlePopup>();
        }
    }

    private void Start()
    {
        //id = 1;
    }
}
