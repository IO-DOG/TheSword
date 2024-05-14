using Data;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;

public class GameManager
{
    public bool OnBattle = false;

    public PlayerController Player; // 전투후 삭제를 위해
    public MonsterController Monster; // 전투후 삭제를 위해
    public CurMonsterData MonsterData = new CurMonsterData(); // 실제 사용될 몬스터 정보
    public ContinueData CurPlayerData = new ContinueData(); // 실제 사용될 플레이어 정보

    public Action OnBattleAction;
    public Action OnBattleDataRefreshAction;
    public Action OnBattleCreatureDefeceAction;
    public Action OnBattlePlayerDefeceAction;

    #region CurPlayerData
    public class ContinueData
    {
        public int Level { get; set; } // Lv
        public float CurExp { get; set; }
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
        public float RewardItem { get; set; }
        public bool IsDefence { get; set; }
    }
    #endregion

    #region InGame
    public int GameSpeed = 1;
    #endregion

    #region Save&Load

    string _path;

    public void SaveGame()
    {
        string jsonStr = JsonConvert.SerializeObject(CurPlayerData);
        File.WriteAllText(_path, jsonStr);
    }

    public bool LoadGame()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
        {
            string path = Application.dataPath + "/@Resources/Data/SaveData.json";
            if (File.Exists(path))
                File.Delete(path);
            return false;
        }

        if (File.Exists(_path) == false)
            return false;

        string fileStr = File.ReadAllText(_path);
        ContinueData data = JsonConvert.DeserializeObject<ContinueData>(fileStr);
        if (data != null)
            CurPlayerData = data;

        return true;
    }

    #endregion

    public void Init()
    {
        _path = Application.dataPath + "/@Resources/Data/SaveData.json";

        if (LoadGame())
            return;

        PlayerPrefs.SetInt("ISFIRST", 1);

        int level = 0;
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

        SaveGame();
    }

}
