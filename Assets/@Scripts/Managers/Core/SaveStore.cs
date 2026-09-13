using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

/// <summary>A complete checkpoint. UI delegates, traits and property setters are not save data.</summary>
public static class SaveStore
{
    public const int Version = 1;
    public const string FileName = "Checkpoint.json";
    public static string DirectoryPath => Application.persistentDataPath;
    public static readonly string[] ProgressKeys = {
        "ISFIRST", "ISFIRSTBATTLE", "ISFIRSTLEVER", "ISFIRSTRECOVERY", "ISFIRSTKEY",
        "ISOPENSWORD", "ISOPENPORTAL", "ISOPENINVENUI", "ISOPENWARPUI", "ISOPENCLASSUI",
        "ISMEETSWORD", "ISMEETBOSS", "ISOPENGREENKEY", "ISOPENYELLOWKEY", "ISOPENREDKEY",
        "DEATHCOUNT", "MOVECOUNT"
    };

    public class ActiveState
    {
        public Dictionary<int, bool> Monsters, Bosses, Items, Equipment, Doors, Pillars, Levers;
        public bool IsValid => Monsters != null && Bosses != null && Items != null &&
            Equipment != null && Doors != null && Pillars != null && Levers != null;
    }

    public class Snapshot
    {
        public int Version;
        public string ContentHash;
        public GameManager.CurPlayerData Player;
        public ActiveState Active;
        public Dictionary<string, int> Progress;
        public float PlayTime;
        public int AttackCount;
        public float DefenceCoolTime;
    }

    sealed class SaveContract : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization mode)
        {
            var property = base.CreateProperty(member, mode);
            // curExp is the backing field; setting CurExp can level up and spawn FX while loading.
            if (member.Name == "Trait" || member.Name == "CurExp") property.Ignored = true;
            return property;
        }
    }

    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings {
        TypeNameHandling = TypeNameHandling.None, ContractResolver = new SaveContract(),
        FloatParseHandling = FloatParseHandling.Double
    };

    public static string Hash(string content)
    {
        using (var sha = SHA256.Create())
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(content)));
    }

    public static bool Exists(string directory) => File.Exists(Path.Combine(directory, FileName)) ||
        File.Exists(Path.Combine(directory, FileName + ".bak"));

    public static void Validate(Snapshot value)
    {
        if (value == null || value.Version != Version)
            throw new InvalidDataException("Unsupported checkpoint version.");
        if (string.IsNullOrEmpty(value.ContentHash) || value.Active == null || !value.Active.IsValid ||
            value.Progress == null || value.Player == null || value.Player.Level < 1 ||
            value.Player.CurStageid < 0 || value.Player.CurPosition == null ||
            value.Player.Inventory == null || value.Player.Inventory.Count < 10 ||
            value.Player.Inventory.Any(x => x == null) || value.Player.KeyInventory == null ||
            value.Player.FirstEnterMapCheck == null || value.Player.FirstEnterMapCheck.Count <= value.Player.CurStageid ||
            !Finite(value.Player.MaxHP) || value.Player.MaxHP <= 0 || !Finite(value.Player.CurHP) ||
            !Finite(value.Player.AttackSpeed) || value.Player.AttackSpeed <= 0 ||
            !Finite(value.PlayTime) || !Finite(value.DefenceCoolTime))
            throw new InvalidDataException("Incomplete checkpoint.");
    }

    /// <summary>세이브가 읽히지 않거나 이 던전의 것이 아닐 때 나는 예외들. 이것만 받아 넘긴다.
    /// InvalidDataException 은 IOException 이 아니다 — 빠뜨리면 검증에 걸린 세이브가
    /// 거부되는 대신 LoadGame 밖으로 튀어 나가 이어하기가 멈춘다.</summary>
    public static bool IsSaveError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException ||
        ex is JsonException || ex is ArgumentException || ex is InvalidDataException;

    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public static bool TryRead(string directory, out Snapshot value, out string error,
        Action<Snapshot> validateContent = null)
    {
        value = null;
        error = null;
        foreach (var suffix in new[] { "", ".bak" })
        {
            string path = Path.Combine(directory, FileName + suffix);
            if (!File.Exists(path)) continue;
            try
            {
                var candidate = JsonConvert.DeserializeObject<Snapshot>(File.ReadAllText(path), Settings);
                Validate(candidate);
                validateContent?.Invoke(candidate);
                value = candidate;
                return true;
            }
            catch (Exception ex) when (IsSaveError(ex))
            {
                error = ex.Message;
            }
        }
        return false;
    }

    public static void Write(string directory, Snapshot value)
    {
        Validate(value);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, FileName);
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.Write(JsonConvert.SerializeObject(value, Formatting.Indented, Settings));
            writer.Flush();
            stream.Flush(true);
        }
        if (File.Exists(path))
        {
            // A corrupt primary must never replace the last working backup.
            bool validPrimary;
            try { Validate(JsonConvert.DeserializeObject<Snapshot>(File.ReadAllText(path), Settings)); validPrimary = true; }
            catch (Exception ex) when (IsSaveError(ex)) { validPrimary = false; }
            File.Replace(temp, path, validPrimary ? path + ".bak" : null);
        }
        else File.Move(temp, path);
    }

    public static void Delete(string directory)
    {
        foreach (var suffix in new[] { "", ".bak", ".tmp" })
            File.Delete(Path.Combine(directory, FileName + suffix));
    }
}
