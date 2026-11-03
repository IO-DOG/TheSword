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
            if (File.Exists(path) && TryReadFile(path, out value, out error, validateContent))
                return true;
        }
        return false;
    }

    /// <summary>파일 하나를 읽는다. 층별 사본(History)을 고를 때도 이 길로 온다.</summary>
    public static bool TryReadFile(string path, out Snapshot value, out string error,
        Action<Snapshot> validateContent = null)
    {
        value = null;
        error = null;
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
            return false;
        }
    }

    // ---------------------------------------------------------------- 층별 사본
    // 층에 들어설 때마다 방금 쓴 체크포인트를 그 층 이름으로 한 벌 더 둔다. 같은 층에
    // 다시 오면 덮어쓴다 — 오르내리기만 해도 목록이 같은 두 층으로 차 버리면 쓸모가 없다.
    // 체크포인트 하나로는 "피가 바닥인 채 저장된 층" 에 갇힐 수 있어서, 몇 층 앞으로
    // 되돌아갈 길을 남긴다.

    public struct CheckpointInfo
    {
        public string File;        // RestartFromCheckpoint 에 그대로 넘긴다
        public int Stage;          // 0 부터 (화면에는 +1 층)
        public int Level;
        public int Hp;
        public int MaxHp;
        public DateTime SavedAt;
    }

    const string HistoryPrefix = "Floor_";
    public const int HistoryLimit = 10;

    static IEnumerable<string> HistoryFiles(string directory) => Directory.Exists(directory)
        ? Directory.GetFiles(directory, HistoryPrefix + "*.json").OrderByDescending(f => File.GetLastWriteTimeUtc(f))
        : Enumerable.Empty<string>();

    /// <summary>지금의 Checkpoint.json 을 그 층의 사본으로 복사하고, 오래된 것은 지운다. 방금 쓴 것은 지우지 않는다 — 시계가 뒤로
    /// 가면(CMOS·덱의 틀린 시각) 그것이 가장 옛것으로 보여 바로 지워졌다.</summary>
    public static void WriteHistory(string directory, int stage)
    {
        string copy = Path.Combine(directory, $"{HistoryPrefix}{stage:000}.json");
        File.Copy(Path.Combine(directory, FileName), copy, true);
        foreach (string old in HistoryFiles(directory)
                     .Where(f => string.Equals(Path.GetFullPath(f), Path.GetFullPath(copy), StringComparison.OrdinalIgnoreCase) == false)
                     .Skip(HistoryLimit - 1).ToList())
            File.Delete(old);
    }

    /// <summary>층별 사본, 최근 것부터 HistoryLimit 개. 읽히지 않는 것은 뺀다. 순서는 플레이 시간(줄지 않는다)이다 — 파일 시각은
    /// 시계가 뒤로 가거나 Steam 클라우드에서 내려받으면 어긋난다.</summary>
    public static List<CheckpointInfo> History(string directory)
    {
        var found = new List<(float time, CheckpointInfo info)>();
        foreach (string path in HistoryFiles(directory))
        {
            if (!TryReadFile(path, out Snapshot s, out _))
                continue;
            found.Add((s.PlayTime, new CheckpointInfo {
                File = path, Stage = s.Player.CurStageid, Level = s.Player.Level,
                Hp = Mathf.CeilToInt(s.Player.CurHP), MaxHp = Mathf.CeilToInt(s.Player.MaxHP),
                SavedAt = File.GetLastWriteTime(path)
            }));
        }
        return found.OrderByDescending(f => f.time).Take(HistoryLimit).Select(f => f.info).ToList();
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

    /// <summary>새 게임. 이 빌드가 읽지 못하는 저장(keepAside)은 지우지 않고 Unreadable/<시각>/ 으로 옮긴다 — 타이틀이 "원본은
    /// 보존됩니다" 라고 약속했고, 지도 보관 파일이 빠진 패치(K1) 뒤라면 다음 패치가 그것을 살릴 수 있다. Steam 클라우드는 맨 위
    /// 폴더의 이름 적힌 파일만 올린다. 읽기 전용 파일도 지운다(지우다 던지면 새 게임이 검은 화면에서 멈췄다).</summary>
    public static void Delete(string directory, bool keepAside = false)
    {
        var files = new List<string>();
        foreach (var suffix in new[] { "", ".bak", ".tmp" })
            files.Add(Path.Combine(directory, FileName + suffix));
        // 새 게임에 지난 판의 층별 사본이 남아 있으면 "되돌아가기" 가 옛 판으로 간다.
        files.AddRange(HistoryFiles(directory));
        string aside = keepAside ? Path.Combine(directory, "Unreadable", DateTime.Now.ToString("yyyyMMdd-HHmmss")) : null;
        foreach (string file in files)
        {
            if (File.Exists(file) == false)
                continue;
            File.SetAttributes(file, FileAttributes.Normal);
            if (aside == null)
            {
                File.Delete(file);
                continue;
            }
            Directory.CreateDirectory(aside);
            File.Move(file, Path.Combine(aside, Path.GetFileName(file)));
        }
    }

    /// <summary>작은 기록 파일(Records.json·StorySeen.json)을 체크포인트처럼 쓴다: 임시 파일에 써서 바꿔 끼우고 앞의 것은 .bak.
    /// 예전에는 .bak 없이 덮어써서, 싸움 직후 전원이 나가 NUL 로 찬 파일이 다음 판에 빈 기록으로 덮였다. durable 이면 디스크까지
    /// 기다린다(fsync) — 싸움마다 쓰는 기록에는 끈다: 덱의 microSD 에서 수십 ms 가 걸리고, 깨져도 .bak(오래전에 디스크에 내려간 앞 판)이 남는다.</summary>
    public static void WriteText(string path, string text, bool durable = true)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.Write(text);
            writer.Flush();
            stream.Flush(durable);
        }
        if (File.Exists(path))
            File.Replace(temp, path, path + ".bak");
        else
            File.Move(temp, path);
    }

    /// <summary>WriteText 로 쓴 파일을 읽는다. 본 파일이 깨졌으면 .corrupt 로 비켜 두고 .bak 을 읽는다 — 깨진 것을 .bak 으로 밀어
    /// 좋은 사본을 덮거나, 그 위에 빈 기록을 쓰지 않는다. 없거나 둘 다 못 읽으면 null.</summary>
    public static T ReadText<T>(string path, Func<string, T> parse) where T : class
    {
        if (TryParse(path, parse, out T value))
            return value;
        if (File.Exists(path))
        {
            try
            {
                File.Copy(path, path + ".corrupt", true);
                File.Delete(path);
            }
            catch (Exception ex) when (IsSaveError(ex))
            {
                Debug.LogWarning($"[Save] {Path.GetFileName(path)} 를 비켜 두지 못했다: {ex.Message}");
            }
        }
        return TryParse(path + ".bak", parse, out value) ? value : null;
    }

    static bool TryParse<T>(string path, Func<string, T> parse, out T value) where T : class
    {
        value = null;
        if (File.Exists(path) == false)
            return false;
        try { value = parse(File.ReadAllText(path)); }
        catch (Exception ex) when (IsSaveError(ex)) { Debug.LogWarning($"[Save] {Path.GetFileName(path)} 를 읽지 못했다: {ex.Message}"); }
        return value != null;
    }
}
