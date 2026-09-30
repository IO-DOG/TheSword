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
        public int Ability { get; set; }
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
        // 기준 값(par): 설계한 플레이어(그 층 목표 레벨, 치명 횟수 0, 체력 가득)가 이 몬스터에게
        // 잃는 HP, 절대값. 생성 몬스터만 있고 1~4층 몬스터는 0 이다 (generate_content.build_monsters).
        public float ParLoss { get; set; }
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
        public int Key { get; set; }
        public List<Data.ObjectData> Objects { get; set; }
    }

    [Serializable]
    public class ObjectData
    {
        public int ChoiceGroup { get; set; }
        public int Id { get; set; }
        public int Count { get; set; }
        public int ObjectType { get; set; }
        public MyVector3 Position { get; set; }
    }
   
    [Serializable]
    public class MyVector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    [Serializable]
    public class MapDataLoader : ILoader<int, MapData>
    {
        public List<MapData> maps = new List<MapData>();
        public Dictionary<int, MapData> MakeDict()
        {
            Dictionary<int, MapData> dict = new Dictionary<int, MapData>();
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
        public int ClassName { get; set; }
        public int ClassDesc { get; set; }
        public string AbilityImage { get; set; }
        public string BattleBGImage { get; set; }
        public string Weapon { get; set; }
        public string Shield { get; set; }
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
        public int DungeonNameScriptID;
        // 기준 레벨: 정답 경로가 이 층에 들어설 때·떠날 때의 레벨 (1~4층은 0).
        public int ParLevelIn;
        public int ParLevelOut;
        // 계단의 제단 (띠의 마지막 층에만, 없으면 전부 0). 한 번에 공격 AltarAtk 또는 방어 AltarDef.
        // n 번째 구매의 값 = AltarPrice + AltarPriceStep·n(n−1) (최대 HP 의 %),
        // 내고 나서 AltarReserve % (탑의 법이면 AltarReserveTower %)가 안 남으면 팔지 않는다
        // (generate_content.altar_sells). 문턱은 층마다 다르다 — 그 HP 로 다음 층 입구(체크포인트)에
        // 서도 100층까지 갈 수 있는 값이다(altar_reserves). 낮춰 쓰면 그 입구에서 영영 갇힌다.
        public float AltarAtk;
        public float AltarDef;
        public float AltarPrice;
        public float AltarPriceStep;
        public float AltarReserve;
        public float AltarReserveTower;
    }

    [Serializable]
    public class StageInfoDataLoader : ILoader<int, StageInfoData>
    {
        public List<StageInfoData> stageInfos = new List<StageInfoData>();
        public Dictionary<int, StageInfoData> MakeDict()
        {
            Dictionary<int, StageInfoData> dict = new Dictionary<int, StageInfoData>();
            foreach (StageInfoData stageInfo in stageInfos)
                dict.Add(stageInfo.id, stageInfo);
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