using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class MonsterController : MonoBehaviour
{
    public int id = 0;
    public int _monsterIndex_forActive = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Player")
        {
            //���� ����
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
            Managers.Game.MonsterData.RewardExp = Managers.Data.MonsterDic[id].RewardExp;
            Managers.Game.MonsterData.RewardItem = Managers.Data.MonsterDic[id].RewardItem;
            Managers.Game.MonsterData.IdleAnimStr = Managers.Data.MonsterDic[id].IdleAnimStr;
            Managers.Game.MonsterData.AttackAnimStr = Managers.Data.MonsterDic[id].AttackAnimStr;
            Managers.Game.MonsterData.BattleParticleAttack = Managers.Data.MonsterDic[id].BattleParticleAttack;
            Managers.Game.MonsterData.BattleParticleHit = Managers.Data.MonsterDic[id].BattleParticleHit;
            Managers.Game.MonsterData.MonsterNameId = Managers.Data.MonsterDic[id].MonsterNameId;
            Managers.Game.MonsterData.MonsterDescId = Managers.Data.MonsterDic[id].MonsterDescId;
            Managers.Game.MonsterData.IsDefence = false;
            Managers.Game.MonsterData.IsActiveIndex = _monsterIndex_forActive;
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
