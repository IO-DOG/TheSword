using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{

    #region PlayerData
    [Serializable]
    public class PlayerData
    {
        public int id { get; set; } // Lv
        public float NeedExp { get; set; }
        public float TotalExp { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public float MaxHP { get; set; }
        public float AttackSpeed { get; set; }
        public float DefenceSpeed { get; set; }
        public float Critical { get; set; }
        public float CriticalAttack { get; set; }
        public float MoveSpeed { get; set; }
    }

    [Serializable]
    public class PlayerDataLoader : ILoader<int, PlayerData>
    {
        public List<PlayerData> creatures = new List<PlayerData>();
        public Dictionary<int, PlayerData> MakeDict()
        {
            Dictionary<int, PlayerData> dict = new Dictionary<int, PlayerData>();
            foreach (PlayerData creature in creatures)
                dict.Add(creature.id, creature);
            return dict;
        }
    }
    #endregion


    #region MonsterData
    [Serializable]
    public class MonsterData
    {
        public int id { get; set; }
        public int Chapter { get; set; }
        public string Class { get; set; }
        public int Feature { get; set; }
        public string Name { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public float MaxHP { get; set; }
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
    }

    [Serializable]
    public class MonsterDataLoader : ILoader<int, MonsterData>
    {
        public List<MonsterData> creatures = new List<MonsterData>();
        public Dictionary<int, MonsterData> MakeDict()
        {
            Dictionary<int, MonsterData> dict = new Dictionary<int, MonsterData>();
            foreach (MonsterData creature in creatures)
                dict.Add(creature.id, creature);
            return dict;
        }
    }
    #endregion


    #region MapData

    [Serializable]
    public class MapData
    {
        public int id;
    }

    [Serializable]
    public class MapDataLoader : ILoader<int, MapData>
    {
        public List<MapData> data = new List<MapData>();
        public Dictionary<int, MapData> MakeDict()
        {
            Dictionary<int, MapData> dict = new Dictionary<int, MapData>();
            foreach (MapData creature in data)
                dict.Add(creature.id, creature);
            return dict;
        }
    }

    #endregion
}