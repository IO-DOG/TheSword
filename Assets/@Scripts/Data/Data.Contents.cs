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

    #region Consumable Item Data
    [Serializable]
    public class ConsumableItemData
    {
        public int id { get; set; }
        public string Name { get; set; }
        public float Heal { get; set; }
        public float AttackUp { get; set; }
        public float DefenceUp { get; set; }
        public float HPUp { get; set; }
        public string Description { get; set; }
    }

    [Serializable]
    public class ConsumableItemDataLoader : ILoader<int, ConsumableItemData>
    {
        public List<ConsumableItemData> consumableItems = new List<ConsumableItemData>();
        public Dictionary<int, ConsumableItemData> MakeDict()
        {
            Dictionary<int, ConsumableItemData> dict = new Dictionary<int, ConsumableItemData>();
            foreach (ConsumableItemData consumableItem in consumableItems)
                dict.Add(consumableItem.id, consumableItem);
            return dict;
        }
    }
    #endregion

    #region MapData

    [Serializable]
    public class MapData
    {
        public string Key { get; set; }
        public List<Data.Tile> Tile { get; set; }
    }

    [Serializable]
    public class Tile
    {
        public int ID;
        public Position Position { get; set; }
        public Occupied Occupied { get; set; }
    }

    [Serializable]
    public class Occupied
    {
        public string Type { get; set; }
        public int Index { get; set; }
        public int TotalIndex { get; set; }
        public bool IsActive { get; set; }
    }

    [Serializable]
    public class Position
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }


    [Serializable]
    public class MapDataLoader : ILoader<string, MapData>
    {
        public List<MapData> maps = new List<MapData>();
        public Dictionary<string, MapData> MakeDict()
        {
            Dictionary<string, MapData> dict = new Dictionary<string, MapData>();
            foreach (MapData map in maps)
                dict.Add(map.Key, map);
            return dict;
        }
    }
    #endregion

    #region MonsterClassData
    [Serializable]
    public class MonsterClassData
    {
        public int id { get; set; }
        public string ClassName { get; set; }
        public string ClassDesc { get; set; }
        public string Image { get; set; }
        public string AttackFX { get; set; }
        public int ClassId { get; set; }
        public int EffectDescId { get; set; }
    }

    [Serializable]
    public class MonsterClassDataLoader : ILoader<int, MonsterClassData>
    {
        public List<MonsterClassData> monsterClasses = new List<MonsterClassData>();
        public Dictionary<int, MonsterClassData> MakeDict()
        {
            Dictionary<int, MonsterClassData> dict = new Dictionary<int, MonsterClassData>();
            foreach (MonsterClassData monsterClass in monsterClasses)
                dict.Add(monsterClass.id, monsterClass);
            return dict;
        }
    }
    #endregion

    #region EquipData
    [Serializable]
    public class EquipData
    {
        public int id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public float ATK { get; set; }
        public float DEF { get; set; }
        public float HP { get; set; }
        public float ASPD { get; set; }
        public float DSPD { get; set; }
        public float CRI { get; set; }
        public float CRIATK { get; set; }
        public float MSPD { get; set; }
        public int AbilityId { get; set; }
        public string ImageName { get; set; }
        public int ScriptId { get; set; }
    }

    [Serializable]
    public class EquipDataLoader : ILoader<int, EquipData>
    {
        public List<EquipData> equips = new List<EquipData>();
        public Dictionary<int, EquipData> MakeDict()
        {
            Dictionary<int, EquipData> dict = new Dictionary<int, EquipData>();
            foreach (EquipData equip in equips)
                dict.Add(equip.id, equip);
            return dict;
        }
    }
    #endregion
}