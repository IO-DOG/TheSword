using Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

public class GameManager
{
    public bool OnBattle = false;

    public PlayerController Player; // 전투후 삭제를 위해
    public MonsterController Monster; // 전투후 삭제를 위해
    public CurMonsterData MonsterData = new CurMonsterData(); // 실제 사용될 몬스터 정보
    public ContinueData CurPlayerData = new ContinueData(); // 실제 사용될 플레이어 정보
    public CurConsumableItemData ConsumableItemData = new CurConsumableItemData(); // Current Consumable Item Data
    public Inventory Inventory = new Inventory(); //Inventory

    public Action OnBattleAction;
    public Action OnBattleDataRefreshAction;
    public Action OnBattleCreatureDefeceAction;
    public Action OnBattlePlayerDefeceAction;


    #region CurPlayerData
    public class ContinueData
    {
        public int Level { get; set; } // Lv
        public float curExp;
        public float CurExp
        {
            get
            {
                return curExp;
            }
            set
            {
                curExp = value;

                float needExp = Managers.Data.PlayerDic[Level + 1].NeedExp;
                Debug.Log($"CurExp : {CurExp}");
                Debug.Log($"NeedExp : {Managers.Data.PlayerDic[Level + 1].NeedExp}");

                if (curExp >= needExp)
                {
                    curExp = curExp - needExp;
                    Level++;
                    Debug.Log("Level UP!!");
                    LevelUp();
                }
            }
        }
        public float MaxHP { get; set; }
        public float CurHP { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public float AttackSpeed { get; set; }
        public float DefenceSpeed { get; set; }
        public float Critical { get; set; }
        public float CriticalAttack { get; set; }
        public float MoveSpeed { get; set; }
        public bool IsDefence { get; set; }
    }
    #endregion

    #region CurMonsterData
    public class CurMonsterData
    {
        public int id { get; set; }
        public int Chapter { get; set; }
        public string Class { get; set; }
        public string Name { get; set; }
        public int Feature { get; set; }
        public string Image { get; set; }
        public float MaxHP { get; set; }
        public float CurHP { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public float AttackSpeed { get; set; }
        public float DefenceSpeed { get; set; }
        public float Critical { get; set; }
        public float CriticalAttack { get; set; }
        public float RewardExp { get; set; }
        public int RewardItem { get; set; }
        public string IdleAnimStr { get; set; }
        public string AttackAnimStr { get; set; }
        public string DefenceAnimStr { get; set; }
        public string HitAnimStr { get; set; }
        public bool IsDefence { get; set; }
        public int IsActiveIndex { get; set; }
    }
    #endregion

    #region CurConsumableItemData
    public class CurConsumableItemData
    {
        public int id { get; set; }
        public string Name { get; set; }
        public float Heal { get; set; }
        public float AttackUp { get; set; }
        public float DefenceUp { get; set; }
        public float HPUp { get; set; }
        public string Description { get; set; }
        public int IsActiveIndex { get; set; }
    }
    #endregion

    #region InGame
    public int GameSpeed = 1;

    public static void LevelUp()
    {
        Managers.Game.CurPlayerData.MaxHP += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].MaxHP;
        Managers.Game.CurPlayerData.CurHP += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].MaxHP;
        Managers.Game.CurPlayerData.Attack += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].Attack;
        Managers.Game.CurPlayerData.Defence += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].Defence;
        Managers.Game.CurPlayerData.AttackSpeed += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].AttackSpeed;
        Managers.Game.CurPlayerData.DefenceSpeed += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].DefenceSpeed;
        Managers.Game.CurPlayerData.Critical += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].Critical;
        Managers.Game.CurPlayerData.CriticalAttack += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].CriticalAttack;
        Managers.Game.CurPlayerData.MoveSpeed += Managers.Data.PlayerDic[Managers.Game.CurPlayerData.Level].MoveSpeed;
    }
    #endregion

    #region Save&Load

    string _path;

    public void SaveGame()
    {
        string jsonStr = JsonConvert.SerializeObject(CurPlayerData, Formatting.Indented);
        File.WriteAllText(_path, jsonStr);

        string monsterActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.MonsterActiveDic, Formatting.Indented);
        File.WriteAllText(Application.dataPath + "/@Resources/Data/SaveMonsterActiveData.json", monsterActiveDicJsonStr);
        string bossMonsterActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.BossMonsterActiveDic, Formatting.Indented);
        File.WriteAllText(Application.dataPath + "/@Resources/Data/SaveBossMonsterActiveData.json", bossMonsterActiveDicJsonStr);
        string itemActiveDicJsonStr = JsonConvert.SerializeObject(Managers.Data.ItemActiveDic, Formatting.Indented);
        File.WriteAllText(Application.dataPath + "/@Resources/Data/SaveItemActiveData.json", itemActiveDicJsonStr);
    }

    public bool LoadGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            string path = Application.dataPath + "/@Resources/Data/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);

            int level = 1;
            Managers.Game.CurPlayerData.Level = Managers.Data.PlayerDic[level].id;
            Managers.Game.CurPlayerData.CurExp = 0;
            Managers.Game.CurPlayerData.MaxHP = Managers.Data.PlayerDic[level].MaxHP;
            Managers.Game.CurPlayerData.CurHP = Managers.Data.PlayerDic[level].MaxHP;
            Managers.Game.CurPlayerData.Attack = Managers.Data.PlayerDic[level].Attack;
            Managers.Game.CurPlayerData.Defence = Managers.Data.PlayerDic[level].Defence;
            Managers.Game.CurPlayerData.AttackSpeed = Managers.Data.PlayerDic[level].AttackSpeed;
            Managers.Game.CurPlayerData.DefenceSpeed = Managers.Data.PlayerDic[level].DefenceSpeed;
            Managers.Game.CurPlayerData.Critical = Managers.Data.PlayerDic[level].Critical;
            Managers.Game.CurPlayerData.CriticalAttack = Managers.Data.PlayerDic[level].CriticalAttack;
            Managers.Game.CurPlayerData.MoveSpeed = Managers.Data.PlayerDic[level].MoveSpeed;
            Managers.Game.CurPlayerData.IsDefence = false;

            return false;
        }

        if (File.Exists(_path) == false)
        {
            Debug.Log("플레이어 데이터 로딩 실패");
            return false;
        }

        string fileStr = File.ReadAllText(_path);
        ContinueData data = JsonConvert.DeserializeObject<ContinueData>(fileStr);
        if (data != null)
        {
            CurPlayerData = data;
            Debug.Log("플레이어 데이터 로딩 완료");
        }

        string monsterActiveDicFile = File.ReadAllText(Application.dataPath + "/@Resources/Data/SaveMonsterActiveData.json");
        Dictionary<int, bool> monsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(monsterActiveDicFile);
        Managers.Data.MonsterActiveDic = monsterActiveDic;
        string bossMonsterActiveDicFile = File.ReadAllText(Application.dataPath + "/@Resources/Data/SaveBossMonsterActiveData.json");
        Dictionary<int, bool> bossMonsterActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(bossMonsterActiveDicFile);
        Managers.Data.BossMonsterActiveDic = bossMonsterActiveDic;
        string itemActiveDicFile = File.ReadAllText(Application.dataPath + "/@Resources/Data/SaveItemActiveData.json");
        Dictionary<int, bool> itemActiveDic = JsonConvert.DeserializeObject<Dictionary<int, bool>>(itemActiveDicFile);
        Managers.Data.ItemActiveDic = itemActiveDic;

        return true;
    }

    #endregion

    public void Init()
    {
        _path = Application.dataPath + "/@Resources/Data/SaveData.json";

        if (LoadGame())
            return;

        PlayerPrefs.SetInt("ISFIRST", 0);

        SaveGame();
    }

}
