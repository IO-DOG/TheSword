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
        public string BattleParticleAttack { get; set; }
        public string BattleParticleHit { get; set; }
        public string Shadow { get; set; }
        public int MonsterNameId { get; set; }
        public int MonsterDescId { get; set; }
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
        public float Heal { get; set; }
        public float AttackUp { get; set; }
        public float DefenceUp { get; set; }
        public float HPUp { get; set; }
        public string Img { get; set; }
        public string PrefabName {get;set;}
        public string Shadow {get;set;}
        public int ScriptNameId {get;set;}
        public int ScriptDescriptionId {get;set;}
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
        public List<Data.TileData> Tiles { get; set; }
    }

    [Serializable]
    public class TileData
    {
        public int PrefabID { get; set; }
        public MyVector3 Position { get; set; }
        public int TileType { get; set; }
    }

    [Serializable]
    public class StairsData : TileData
    {
        public int Floor { get; set; }
        public int StairsType { get; set; }
    }

    [Serializable]
    public class DoorData : TileData
    {
        public int TotalCount { get; set; }
        public bool IsActive { get; set; }
    }

    [Serializable]
    public class PillarData : TileData
    {
        public int TotalCount { get; set; }
        public bool IsActive { get; set; }
    }

    [Serializable]
    public class LeverData : TileData
    {
        public int TotalCount { get; set; }
        public bool IsActive { get; set; }
    }


    [Serializable]
    public class Occupied : TileData
    {
        public int Type { get; set; }
        public int Index { get; set; }
        public int TotalCount { get; set; }
        public bool IsActive { get; set; }
    }

    [Serializable]
    public class MyVector3
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

    [Serializable]
    public class DungeonDecoData
    {
        public string DGName { get; set; }
        public List<DecoData> DecoData { get; set; }
    }

    public class DecoData
    {
        public int LightType { get; set; }
        public MyVector3 Position { get; set; }
        public MyVector3 Scale { get; set; }
        public MyVector3 Rotation { get; set;}
    }

    [Serializable]
    public class DungeonDecoDataLoader : ILoader<string, DungeonDecoData>
    {
        public List<DungeonDecoData> lights = new List<DungeonDecoData>();
        public Dictionary<string, DungeonDecoData> MakeDict()
        {
            Dictionary<string, DungeonDecoData> dict = new Dictionary<string, DungeonDecoData>();
            foreach (DungeonDecoData light in lights)
                dict.Add(light.DGName, light);
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
        public string AttackFX { get; set; }
        public string HitFX { get; set; }
        public string Shadow { get; set; }
        public string IllustFX { get; set; }
        public string Illust { get; set; }
        public string IllustBG { get; set; }
        public int NameId { get; set; }
        public int DescId { get; set; }
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

    #region ScriptData
    [Serializable]
    public class ScriptData
    {
        public int id;
        public string ScriptKr;
        public string ScriptEn;
        public string ScriptJp;
        public string ScriptCn;
    }

    [Serializable]
    public class ScriptDataLoader : ILoader<int, ScriptData>
    {
        public List<ScriptData> scripts = new List<ScriptData>();
        public Dictionary<int, ScriptData> MakeDict()
        {
            Dictionary<int, ScriptData> dict = new Dictionary<int, ScriptData>();
            foreach (ScriptData script in scripts)
                dict.Add(script.id, script);
            return dict;
        }
    }
    #endregion

    #region StageInfoData
    [Serializable]
    public class StageInfoData
    {
        public int id;
        public string DungeonID;
        public Define.DungeonType Type;
        public string UpStage;
        public string DownStage;
        public string BossRoom;
        public int ATK;
        public int DEF;
        public int EXP;
        public string BGM;
    }

    [Serializable]
    public class StageInfoDataLoader : ILoader<int, StageInfoData>
    {
        public List<StageInfoData> stageInfos = new List<StageInfoData>();
        public Dictionary<int, StageInfoData> MakeDict()
        {
            Dictionary<int, StageInfoData> dict = new Dictionary<int, StageInfoData>();
            foreach (StageInfoData script in stageInfos)
                dict.Add(script.id, script);
            return dict;
        }
    }
    #endregion

    #region EventData
    [Serializable]
    public class EventData
    {
        public int id;
        public string IllustLeft;
        public string IllustRight;
        public string HeroEmoji;
        public string OtherEmoji;
        public int ScriptID;
        public int Class;
        public float Delay;
    }

    [Serializable]
    public class EventDataLoader : ILoader<int, EventData>
    {
        public List<EventData> events = new List<EventData>();
        public Dictionary<int, EventData> MakeDict()
        {
            Dictionary<int, EventData> dict = new Dictionary<int, EventData>();
            foreach (EventData script in events)
                dict.Add(script.id, script);
            return dict;
        }
    }
    #endregion
}