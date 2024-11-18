using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    [HideInInspector]
    public int id = 0;
    [HideInInspector]
    public int _monsterIndex_forActive = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Player")
        {
            Managers.Game.MonsterData.Clear();

            Managers.Game.MonsterData.Add(new GameManager.CurMonsterData());


            Managers.Game.MonsterData[0].id = Managers.Data.MonsterDic[id].id;
            Managers.Game.MonsterData[0].Chapter = Managers.Data.MonsterDic[id].Chapter;
            Managers.Game.MonsterData[0].Class = Managers.Data.MonsterDic[id].Class;
            Managers.Game.MonsterData[0].Name = Managers.Data.MonsterDic[id].Name;
            Managers.Game.MonsterData[0].Feature = Managers.Data.MonsterDic[id].Feature;
            Managers.Game.MonsterData[0].MaxHP = Managers.Data.MonsterDic[id].MaxHP;
            Managers.Game.MonsterData[0].CurHP = Managers.Data.MonsterDic[id].MaxHP;
            Managers.Game.MonsterData[0].Attack = Managers.Data.MonsterDic[id].Attack;
            Managers.Game.MonsterData[0].Defence = Managers.Data.MonsterDic[id].Defence;
            Managers.Game.MonsterData[0].AttackSpeed = Managers.Data.MonsterDic[id].AttackSpeed;
            Managers.Game.MonsterData[0].DefenceSpeed = Managers.Data.MonsterDic[id].DefenceSpeed;
            Managers.Game.MonsterData[0].Critical = Managers.Data.MonsterDic[id].Critical;
            Managers.Game.MonsterData[0].CriticalAttack = Managers.Data.MonsterDic[id].CriticalAttack;
            Managers.Game.MonsterData[0].RewardExp = Managers.Data.MonsterDic[id].RewardExp;
            Managers.Game.MonsterData[0].RewardItem = Managers.Data.MonsterDic[id].RewardItem;
            Managers.Game.MonsterData[0].IdleAnimStr = Managers.Data.MonsterDic[id].IdleAnimStr;
            Managers.Game.MonsterData[0].AttackAnimStr = Managers.Data.MonsterDic[id].AttackAnimStr;
            Managers.Game.MonsterData[0].BattleParticleAttack = Managers.Data.MonsterDic[id].BattleParticleAttack;
            Managers.Game.MonsterData[0].BattleParticleHit = Managers.Data.MonsterDic[id].BattleParticleHit;
            Managers.Game.MonsterData[0].MonsterNameId = Managers.Data.MonsterDic[id].MonsterNameId;
            Managers.Game.MonsterData[0].MonsterDescId = Managers.Data.MonsterDic[id].MonsterDescId;
            Managers.Game.MonsterData[0].IsDefence = false;
            Managers.Game.MonsterData[0].IsActiveIndex = _monsterIndex_forActive;
            //Managers.Game.MonsterData.Image = Managers.Data.MonsterDic[id].Image;

            Managers.Game.Monster = this;
            //Util.Screenshot((screenShot) => {Managers.Game._screenShot = screenShot; });
            StartCoroutine(Util.Screenshot2((screenShot) =>
            {
                Managers.Game._screenShot2 = screenShot;
                Managers.UI.ShowPopupUI<UI_BattlePopup>();
            }));
            //Util.Screenshot2((screenShot) => {Managers.Game._screenShot2 = screenShot; });
        }
    }

    private void Start()
    {
        GetComponent<Animator>().Play($"{Managers.Data.MonsterDic[id].IdleAnimStr}");
        GetComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>(Managers.Data.MonsterDic[id].Shadow);

        //id = 1;
    }
}
