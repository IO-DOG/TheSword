using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>
/// 챕터별 3D 그림을 들여온다 (Documents/TheSword_Meshy_Prompts.md 의 표).
///
///   1. 표의 이름 그대로 넣는다 — Wall_C1_Base.glb, Prop_C3_Light.glb …
///        Assets/@Resources/Art/Meshy/       소유자가 Meshy 로 뽑은 그림
///        Assets/@Resources/Art/ChapterKit/  임시 복셀 그림(MagicaVoxel OBJ + 256x1 팔레트)
///      같은 이름이 둘 다 있으면 Meshy 가 이긴다 — 진짜 그림이다.
///   2. 메뉴 TheSword/Import Meshy Art → Assets/@Resources/Art/Built/{이름}.prefab
///   3. 소품(Prop_*)이 새로 들어왔으면 메뉴 TheSword/Build Map Decorations. 벽은 MapBuilder 가 런타임에 고른다.
///
/// 크기·높이·피벗은 <b>지금 그림을 재서</b> 맞춘다 — 손으로 y 를 옮기거나 배율을 만질 일이 없다.
///   벽·큰 소품  옛 벽 셋(W01~W03)이 차지하던 칸을 채우고 그 바닥면(y 0.2)에 앉는다
///   벽에 거는 것  카메라가 보는 남쪽 벽면에 등을 붙인다 — 횃불·사슬 크기 안으로
///   바닥에 서는 것  (바닥 불 포함) 화로 발자국 안, 발목 높이(0.08) 안, 바닥(y 0) — 걷는 칸에 서기 때문이다
/// 고르게 키우거나 줄이기만 한다. 찌그러뜨리지 않는다. 복셀 그림은 이미 옛 그림의 단위(한 칸 = 3.2)로
/// 그렸으니 배율을 옛 장식과 같은 0.1 로 두고 자리만 붙인다 — 칸보다 클 때만 줄인다(복셀 크기가 달라져 알린다).
///
/// 새 벽은 Tilemap_C00_W02 의 콜라이더·레이어를 그대로 받는다 — 길찾기·충돌·카메라 범위가 그것을 잰다.
/// 소품에는 콜라이더가 없다. 재질은 옛 벽 재질을 복사해 바탕색 그림만 바꾼다(같은 셰이더라야 챕터 해를
/// 같게 받는다). 그림은 Built/ 에 점 필터로 둔다. 메시는 모델 파일 것을 그대로 쓰므로 넣은 모델은 지우지 않는다.
///
/// .glb/.gltf 는 Assets/UniGLTF 의 임포터가 읽는다(기본 축 Z 반전 — glTF 의 앞(+Z)이 카메라 쪽(-Z)이 된다).
/// FBX·OBJ 도 받지만 앞이 어느 쪽인지는 내보낸 프로그램 몫이다. OBJ 는 Unity 가 X 를 뒤집는다(Z 는 그대로).
///
///   MeshyArtImport.SelfTest()   // 가짜 상자로 맞춤·피벗·콜라이더와 벽 변형 비율을 재 본다
/// </summary>
public static class MeshyArtImport
{
    const string MeshyDir = "Assets/@Resources/Art/Meshy";
    const string KitDir = "Assets/@Resources/Art/ChapterKit";
    const string OutDir = "Assets/@Resources/Art/Built";
    const string WallKey = "Tilemap_C00_W02";   // 새 벽이 콜라이더·재질을 물려받는 옛 기본형

    const int MaxTexture = 512;
    const float AnkleHeight = 0.08f;   // 걷는 칸 위 소품의 높이 한계 — 8 복셀 (Art_Music_Prompts §4.2.1)
    const float TallCap = 1.5f;        // 벽·큰 소품은 옛 벽 높이의 1.5 배까지 (굴뚝·가시·떠오른 조각)
    const float EmitIntensity = 1.5f;  // 빛나는 칸의 밝기(선형). 블룸 문턱 1.1(GameSceneProfile)을 가장 밝은 칸만 조금 넘는다
    const int WallTris = 3000;
    const int PropTris = 2000;

    static readonly string[] ModelExtensions = { ".glb", ".gltf", ".fbx", ".obj" };
    static readonly Regex Pattern = new Regex(@"^(Wall|Prop)_C([0-4])_([A-Za-z0-9]+)$", RegexOptions.IgnoreCase);

    /// <summary>옛 그림에서 잰 자리(칸 가운데, 맵 바닥 y 0 기준). 숫자를 박아 두지 않는다.</summary>
    class Targets
    {
        public Bounds Wall;                // 옛 벽 셋을 합친 자리 — 새 벽·큰 소품이 채운다
        public Bounds Collider;            // 옛 기본형의 벽 콜라이더 — 새 벽이 그대로 갖는다
        public bool HasCollider;
        public float FaceZ, FaceY;         // 카메라 쪽(남쪽) 벽면과 그 가운데 높이
        public Vector2 Torch;              // 벽에 거는 불의 가로·세로 (불꽃 그림까지)
        public float Hang;                 // 벽에 거는 소품의 가장 긴 변 (사슬)
        public float Bowl, FloorY;         // 바닥 화로의 발자국과 바닥 높이 — 바닥에 서는 것은 이 안에
        public Material Template;          // 옛 벽의 재질
        public ShadowCastingMode Shadows;
        public bool ReceiveShadows;
    }

    [MenuItem("TheSword/Import Meshy Art")]
    public static void Import()
    {
        Directory.CreateDirectory(MeshyDir);
        Directory.CreateDirectory(KitDir);
        Directory.CreateDirectory(OutDir);
        AssetDatabase.Refresh();

        // 이름 → 모델 파일. 복셀 키트를 먼저 담고 Meshy 로 덮는다 — 같은 이름이면 진짜 그림이 이긴다.
        var notes = new List<string>();
        var picked = new Dictionary<string, string>();
        foreach (string dir in new[] { KitDir, MeshyDir })
        {
            foreach (string found in Directory.GetFiles(dir))
            {
                string file = found.Replace('\\', '/');
                if (Array.IndexOf(ModelExtensions, Path.GetExtension(file).ToLowerInvariant()) < 0)
                    continue;
                string name = Canonical(Path.GetFileNameWithoutExtension(file), out _, out _, out _);
                if (name == null)
                {
                    notes.Add($"{file}: 표에 없는 이름이라 건너뛴다 — " +
                              $"Wall_C0~4_{{{string.Join("|", MapBuilder.WallVariants)}}} 또는 " +
                              $"Prop_C0~4_{{{string.Join("|", MapDecoSetup.PropSlots)}}}");
                    continue;
                }
                string before;
                if (picked.TryGetValue(name, out before))
                    notes.Add($"{name}: {before} 대신 {file} 를 쓴다");
                picked[name] = file;
            }
        }
        if (picked.Count == 0)
        {
            Debug.LogWarning($"[MeshyArt] 넣을 모델이 없다 — {MeshyDir}/ 에 Wall_C1_Base.glb 처럼 표의 이름으로 넣는다");
            return;
        }

        var errors = new List<string>();
        Targets t = Measure(errors);
        if (t == null)
        {
            Debug.LogError("[MeshyArt] 옛 그림을 못 재서 멈춘다\n" + string.Join("\n", errors));
            return;
        }

        var addresses = new List<KeyValuePair<string, string>>();
        foreach (KeyValuePair<string, string> pick in picked)
        {
            string file = pick.Value;
            string kind, slot;
            int chapter;
            Canonical(Path.GetFileNameWithoutExtension(file), out kind, out chapter, out slot);

            GameObject model = AssetDatabase.LoadMainAssetAtPath(file) as GameObject;
            if (model == null)
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                notes.Add(ext == ".glb" || ext == ".gltf"
                    ? $"{Path.GetFileName(file)}: 모델로 읽히지 않았다 — .glb 는 Assets/UniGLTF 의 임포터가 읽는다(없거나 실패). FBX 로 내보내 다시 넣는다"
                    : $"{Path.GetFileName(file)}: 모델로 읽히지 않았다 — 콘솔의 임포트 오류를 본다");
                continue;
            }

            bool voxel = file.StartsWith(KitDir + "/", StringComparison.OrdinalIgnoreCase);
            string path = BuildPrefab(model, pick.Key, kind, chapter, slot, voxel, t, OutDir, notes);
            if (path != null)
                addresses.Add(new KeyValuePair<string, string>(pick.Key, path));
        }

        AssetDatabase.SaveAssets();
        MapDecoSetup.Register(addresses);
        Debug.Log($"[MeshyArt] 프리팹 {addresses.Count}개 → {OutDir} (어드레서블 PreLoad). " +
                  "소품이 새로 들어왔으면 TheSword/Build Map Decorations\n" + string.Join("\n", notes));
    }

    /// <summary>파일 이름을 표의 이름으로 맞춘다(대소문자). 표에 없으면 null.</summary>
    static string Canonical(string file, out string kind, out int chapter, out string slot)
    {
        kind = null;
        slot = null;
        chapter = 0;
        Match m = Pattern.Match(file);
        if (m.Success == false)
            return null;

        kind = m.Groups[1].Value.Equals("wall", StringComparison.OrdinalIgnoreCase) ? "Wall" : "Prop";
        chapter = m.Groups[2].Value[0] - '0';
        foreach (string s in kind == "Wall" ? MapBuilder.WallVariants : MapDecoSetup.PropSlots)
            if (s.Equals(m.Groups[3].Value, StringComparison.OrdinalIgnoreCase))
                slot = s;
        return slot == null ? null : $"{kind}_C{chapter}_{slot}";
    }

    /// <summary>
    /// 자리마다 맞추는 법. max 는 축마다 넘지 않을 크기(0 이면 자유), back 이면 뒷면(+z) 가운데를,
    /// 아니면 바닥 가운데를 at 에 붙인다. 프리팹의 피벗은 늘 칸 가운데(맵 바닥 y 0)다.
    /// </summary>
    static bool Rule(string kind, int chapter, string slot, Targets t, out Vector3 max, out bool back, out Vector3 at)
    {
        float cell = Mathf.Max(t.Wall.size.x, t.Wall.size.z);
        back = false;
        at = new Vector3(0f, t.FloorY, 0f);

        if (kind == "Wall" || slot == "Big")
        {
            // 벽 한 칸을 채운다. 큰 소품은 MapBuilder 가 그림을 끈 벽 칸에 선다
            max = new Vector3(cell, t.Wall.size.y * TallCap, cell);
            at = new Vector3(t.Wall.center.x, t.Wall.min.y, t.Wall.center.z);
        }
        else if (slot == "Wall" || (slot == "Light" && MapDecoSetup.LightOnFloor(chapter) == false))
        {
            // 벽 칸 가운데에 놓이고 그림은 남쪽 면으로 비켜 선다 — 옛 횃불처럼 벽 속에 묻히지 않게
            max = slot == "Wall" ? new Vector3(t.Hang, t.Hang, 0f) : new Vector3(t.Torch.x, t.Torch.y, 0f);
            back = true;
            at = new Vector3(0f, t.FaceY, t.FaceZ);
        }
        else if (slot == "Light" || slot == "Light2" || slot == "Floor" || slot == "Extra")
        {
            // 걷는 칸에 선다(콜라이더 없음) — 길을 막는 것처럼 보이면 퍼즐이 거짓말을 한다. 그래서 발목 높이다.
            // 옛 화로(높이 0.14 + 불꽃)는 이 규칙보다 먼저 놓였다.
            max = new Vector3(t.Bowl, AnkleHeight, t.Bowl);
        }
        else
        {
            max = Vector3.zero;
            return false;
        }
        return true;
    }

    /// <summary>그림 경계 src 를 고르게 키우거나 줄여 축마다 max 안에 넣고(0 이면 자유) 붙이는 점을 at 에 맞춘다.
    /// 배율은 most 를 넘지 않는다(복셀 그림은 옛 장식 배율 0.1 — 키우지 않는다). 원점에 놓인 그림에 scale 을
    /// 곱한 뒤 offset 만큼 옮기면 된다.</summary>
    static void Fit(Bounds src, Vector3 max, bool back, Vector3 at, float most, out float scale, out Vector3 offset)
    {
        scale = most;
        for (int a = 0; a < 3; a++)
            if (max[a] > 0f && src.size[a] > 1e-5f)
                scale = Mathf.Min(scale, max[a] / src.size[a]);
        if (scale == float.MaxValue)
            scale = 1f;

        Vector3 pivot = back ? new Vector3(src.center.x, src.center.y, src.max.z)
                             : new Vector3(src.center.x, src.min.y, src.center.z);
        offset = at - pivot * scale;
    }

    /// <summary>모델 하나로 프리팹을 만든다. 경로를 돌려주고, 못 만들면 null.
    /// voxel 이면 옛 그림 단위로 그린 복셀 그림이다 — 배율은 0.1 그대로, 자리만 붙인다.
    ///
    /// 이미 있으면 뿌리는 그대로 두고 속만 바꾼다 — 층 장식(Deco_*)이 이 뿌리를 가리켜 자리·이름을
    /// 덮어쓰고 있다. 새 뿌리로 갈아 끼우면 그 덮어쓰기가 끊겨 소품이 맵 원점에 몰린다.</summary>
    static string BuildPrefab(GameObject model, string name, string kind, int chapter, string slot, bool voxel,
                              Targets t, string outDir, List<string> notes)
    {
        Vector3 max, at;
        bool back;
        if (Rule(kind, chapter, slot, t, out max, out back, out at) == false)
        {
            notes.Add($"{name}: 모르는 자리 '{slot}'");
            return null;
        }

        string path = $"{outDir}/{name}.prefab";
        bool existed = File.Exists(path);
        GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // 아래 경계를 뿌리 기준으로 잰다
        root.transform.localScale = Vector3.one;
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        foreach (Collider old in root.GetComponents<Collider>())
            Object.DestroyImmediate(old);

        GameObject art = Object.Instantiate(model, root.transform, false);
        art.name = "Art";
        art.transform.localPosition = Vector3.zero;

        // 그림만 남긴다 — 콜라이더·빛·카메라·애니메이션은 놀이나 빛을 바꾼다.
        foreach (Component c in art.GetComponentsInChildren<Component>(true))
            if (c != null && c is Transform == false && c is MeshFilter == false && c is Renderer == false)
                Object.DestroyImmediate(c);

        var made = new Dictionary<Material, Material>();
        int tris = 0;
        foreach (Renderer r in art.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material key = mats[i] != null ? mats[i] : t.Template;
                Material mine;
                if (made.TryGetValue(key, out mine) == false)
                {
                    mine = MakeMaterial(mats[i], t.Template, $"{outDir}/{name}_{made.Count}");
                    made[key] = mine;
                }
                mats[i] = mine;
            }
            r.sharedMaterials = mats;
            r.shadowCastingMode = t.Shadows;
            r.receiveShadows = t.ReceiveShadows;
            tris += Triangles(r);
        }

        Bounds b;
        if (Box(art, false, out b) == false)
        {
            notes.Add($"{name}: 메시가 없다");
            if (existed) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
            return null;
        }
        float scale;
        Vector3 offset;
        Fit(b, max, back, at, voxel ? MapDecoSetup.TileScale : float.MaxValue, out scale, out offset);
        art.transform.localScale *= scale;
        art.transform.localPosition = offset;
        if (voxel && scale < MapDecoSetup.TileScale * 0.999f)
            notes.Add($"{name}: 복셀 그림이 자리보다 커서 {scale / MapDecoSetup.TileScale:P0} 로 줄였다 — 복셀 크기가 옛 그림과 달라진다");

        int layer = 0;
        if (kind == "Wall")
        {
            // 놀이는 옛 벽 그대로다 — 같은 상자, 같은 레이어(길찾기·충돌·카메라 범위가 이것을 잰다).
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = t.Collider.center;
            box.size = t.Collider.size;
            layer = (int)Define.Layer.Wall;
        }
        foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
            tr.gameObject.layer = layer;

        Box(art, false, out b);
        int budget = kind == "Wall" ? WallTris : PropTris;
        notes.Add($"{name}: {b.size.x:0.000} x {b.size.y:0.000} x {b.size.z:0.000}, 삼각형 {tris:N0}" +
                  (tris > budget ? $" — 기준 {budget:N0} 을 넘는다(Meshy Remesh)" : ""));

        PrefabUtility.SaveAsPrefabAsset(root, path);
        if (existed)
            PrefabUtility.UnloadPrefabContents(root);
        else
            Object.DestroyImmediate(root);
        return path;
    }

    /// <summary>옛 벽 재질을 복사해 바탕색 그림·색만 바꾼다 — 셰이더와 매끈함 같은 설정이 같아야 같은 빛을 받는다.
    /// Meshy 의 노멀·거칠기 맵은 쓰지 않는다(명암은 챕터 해가 넣는다).</summary>
    static Material MakeMaterial(Material src, Material template, string pathNoExt)
    {
        var m = new Material(template) { name = Path.GetFileName(pathNoExt) };
        Color color = Color.white;
        Texture tex = null, glow = null;
        if (src != null)
        {
            color = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                  : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            Texture baseMap = BaseMap(src);
            tex = Bake(baseMap, pathNoExt + ".png");
            glow = Bake(EmitMap(baseMap), pathNoExt + "_Emit.png");
        }
        foreach (string p in new[] { "_BaseMap", "_MainTex" })
            if (m.HasProperty(p))
                m.SetTexture(p, tex);
        foreach (string p in new[] { "_BaseColor", "_Color" })
            if (m.HasProperty(p))
                m.SetColor(p, color);

        // 빛나는 칸(불꽃·숯·등 유리·수정·빛 균열)은 챕터가 어두워도 제 색으로 보인다. 빛 그림이 없으면 옛 벽 재질 그대로다.
        // SetColor 는 선형 공간 프로젝트에서 감마 값을 받아 선형으로 바꾼다 — 선형 밝기 EmitIntensity 가 되게 되돌려 넘긴다.
        if (glow != null && m.HasProperty("_EmissionMap"))
        {
            float k = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? Mathf.LinearToGammaSpace(EmitIntensity) : EmitIntensity;
            m.SetTexture("_EmissionMap", glow);
            m.SetColor("_EmissionColor", new Color(k, k, k, 1f));
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        AssetDatabase.CreateAsset(m, pathNoExt + ".mat");
        return m;
    }

    /// <summary>바탕색 그림 옆의 {이름}_Emit.png — 같은 배치에서 빛나는 칸만 켜고 나머지는 검정인 그림
    /// (복셀 키트가 팔레트와 같이 낸다). 그림 파일이 없으면(GLB 속 그림) null.</summary>
    static Texture EmitMap(Texture baseMap)
    {
        string file = baseMap != null ? AssetDatabase.GetAssetPath(baseMap) : "";
        if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == false)
            return null;
        return AssetDatabase.LoadAssetAtPath<Texture2D>(file.Substring(0, file.Length - 4) + "_Emit.png");
    }

    static Texture BaseMap(Material m)
    {
        if (m.HasProperty("_BaseMap"))
            return m.GetTexture("_BaseMap");   // URP (UniGLTF·FBX 가 만드는 재질)
        if (m.HasProperty("_MainTex"))
            return m.GetTexture("_MainTex");
        return null;
    }

    /// <summary>바탕색 그림을 Built/ 에 PNG 로 둔다. 파일로 있는 PNG(복셀 팔레트, FBX·OBJ 옆 그림)는 바이트째
    /// 복사한다 — 다시 굽지 않으니 잃는 것이 없다. 모델 속에 든 그림(GLB)은 대개 읽기 금지라 GPU 로 한 번 옮겨
    /// 512 이하로 읽는다. 들일 때 512 로 줄이고 점 필터를 건다(도트 몬스터와 섞이게).
    /// 팔레트 띠(MagicaVoxel 256x1)는 압축·밉맵을 끈다 — 압축은 4칸씩 묶어 색을 섞고, 밉맵은 이웃 칸을 평균한다.
    /// 그림은 밉맵을 켠다 — 카메라에서 한 칸에 텍셀 수십 개가 접히니 없으면 걸을 때 반짝인다.</summary>
    static Texture2D Bake(Texture src, string path)
    {
        if (src == null)
            return null;

        string file = AssetDatabase.GetAssetPath(src);
        if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(file, path, true);
        }
        else
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogError($"[MeshyArt] {path}: 그래픽 장치가 없어(-nographics) 모델 속 그림을 못 읽는다 — 에디터 창에서 메뉴로 돌린다");
                return null;
            }
            float k = Mathf.Min(1f, (float)MaxTexture / Mathf.Max(src.width, src.height));
            int w = Mathf.Max(1, Mathf.RoundToInt(src.width * k));
            int h = Mathf.Max(1, Mathf.RoundToInt(src.height * k));
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture before = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            copy.Apply();
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);
        }

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        bool palette = src.height <= 4;
        importer.maxTextureSize = MaxTexture;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = palette == false;
        importer.textureCompression = palette ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static int Triangles(Renderer r)
    {
        Mesh mesh = null;
        if (r is SkinnedMeshRenderer skin)
            mesh = skin.sharedMesh;
        else if (r.TryGetComponent(out MeshFilter mf))
            mesh = mf.sharedMesh;
        if (mesh == null)
            return 0;
        int n = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
            n += (int)mesh.GetIndexCount(i) / 3;
        return n;
    }

    // ------------------------------------------------------------------ 옛 그림 재기

    static Targets Measure(List<string> errors)
    {
        var t = new Targets();
        bool walls = false;
        foreach (string key in new[] { "Tilemap_C00_W01", WallKey, "Tilemap_C00_W03" })
        {
            GameObject go = Open(key, 0f, errors);
            if (go == null)
                continue;
            Bounds b;
            if (Box(go, false, out b))
            {
                // 벽면은 셋 중 가장 안쪽(W02·W03 이 W01 보다 한 복셀 얇다) — 거는 소품이 벽에서 뜨지 않게
                t.FaceZ = walls ? Mathf.Max(t.FaceZ, b.min.z) : b.min.z;
                if (walls) t.Wall.Encapsulate(b);
                else t.Wall = b;
                walls = true;
            }
            if (key == WallKey)
            {
                t.FaceY = b.center.y;
                MeshRenderer r = go.GetComponentInChildren<MeshRenderer>(true);
                if (r != null)
                {
                    t.Template = r.sharedMaterial;
                    t.Shadows = r.shadowCastingMode;
                    t.ReceiveShadows = r.receiveShadows;
                }
                foreach (BoxCollider c in go.GetComponentsInChildren<BoxCollider>(true))
                {
                    // 벽 콜라이더는 돌아 있지 않다 — 크기에 배율만 곱하면 월드 상자다
                    Vector3 size = Vector3.Scale(c.size, c.transform.lossyScale);
                    t.Collider = new Bounds(c.transform.TransformPoint(c.center),
                                            new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
                    t.HasCollider = true;
                }
            }
            PrefabUtility.UnloadPrefabContents(go);
        }

        Bounds torch, cuff, bowl;
        bool props = Measure("Deco_Torch", true, errors, out torch)
                     & Measure("Deco_Handcuff", false, errors, out cuff)
                     & Measure("Deco_FireBowl", false, errors, out bowl);
        t.Torch = new Vector2(torch.size.x, torch.size.y);
        t.Hang = Mathf.Max(cuff.size.x, Mathf.Max(cuff.size.y, cuff.size.z));
        t.Bowl = Mathf.Max(bowl.size.x, bowl.size.z);
        t.FloorY = bowl.min.y;

        if (walls == false) errors.Add("옛 벽(Tilemap_C00_W01~03)을 하나도 못 쟀다");
        if (t.HasCollider == false) errors.Add($"{WallKey} 에 BoxCollider 가 없다");
        if (t.Template == null) errors.Add($"{WallKey} 에 재질이 없다");
        return walls && props && t.HasCollider && t.Template != null ? t : null;
    }

    /// <summary>옛 장식 하나를 MapDecoSetup 이 놓는 배율로 잰다.</summary>
    static bool Measure(string address, bool sprites, List<string> errors, out Bounds box)
    {
        box = default;
        GameObject go = Open(address, MapDecoSetup.TileScale, errors);
        if (go == null)
            return false;
        bool found = Box(go, sprites, out box);
        PrefabUtility.UnloadPrefabContents(go);
        if (found == false)
            errors.Add($"옛 장식 '{address}' 에 그림이 없다");
        return found;
    }

    /// <summary>옛 프리팹을 따로 연다(열린 장면을 건드리지 않는다). scale 이 0 보다 크면 그 배율로 놓는다.</summary>
    static GameObject Open(string address, float scale, List<string> errors)
    {
        GameObject prefab = MapDecoSetup.FindByAddress(address, true);
        if (prefab == null)
        {
            errors.Add($"옛 그림 '{address}' 이 어드레서블에 없다");
            return null;
        }
        GameObject go = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        if (scale > 0f)
            go.transform.localScale = Vector3.one * scale;
        return go;
    }

    /// <summary>그려지는 것의 경계(월드). 메시는 메시의 경계로, sprites 면 SpriteRenderer 그림까지.
    /// 렌더러의 bounds 는 쓰지 않는다 — 미리보기 장면에서 늦게 갱신될 수 있다.</summary>
    static bool Box(GameObject go, bool sprites, out Bounds box)
    {
        box = default;
        bool found = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Bounds local;
            if (r is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                local = skin.sharedMesh.bounds;
            else if (r is MeshRenderer && r.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                local = mf.sharedMesh.bounds;
            else if (sprites && r is SpriteRenderer sr && sr.sprite != null)
                local = sr.sprite.bounds;
            else
                continue;

            Matrix4x4 m = r.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = m.MultiplyPoint3x4(new Vector3((i & 1) != 0 ? local.max.x : local.min.x,
                                                           (i & 2) != 0 ? local.max.y : local.min.y,
                                                           (i & 4) != 0 ? local.max.z : local.min.z));
                if (found)
                {
                    box.Encapsulate(p);
                }
                else
                {
                    box = new Bounds(p, Vector3.zero);
                    found = true;
                }
            }
        }
        return found;
    }

    // ------------------------------------------------------------------ 자체 검사

    /// <summary>
    /// 원점에서 먼 4x6x2 상자(Meshy 가 아무렇게나 내보낸 크기·피벗)를 자리마다 맞춰 보고, 한계·비율·붙는 점·
    /// 콜라이더·레이어를 잰다. 다시 들일 때 프리팹 뿌리가 그대로인지, 벽 변형의 비율과 이웃 규칙도 본다.
    /// 임시 폴더에 만들고 지운다. GPU 는 쓰지 않는다(그림 없는 재질).
    /// </summary>
    public static string SelfTest()
    {
        var fails = new List<string>();
        var sb = new StringBuilder("[MeshyArt] 자체 검사\n");
        Targets t = Measure(fails);
        if (t == null)
            return sb.Append("  실패: 옛 그림을 못 쟀다\n    ").Append(string.Join("\n    ", fails)).ToString();
        sb.AppendLine($"  옛 벽 {t.Wall.size.x:0.000} x {t.Wall.size.y:0.000} x {t.Wall.size.z:0.000} " +
                      $"(y {t.Wall.min.y:0.000}~{t.Wall.max.y:0.000}), 벽면 z {t.FaceZ:0.000} · y {t.FaceY:0.000}, " +
                      $"콜라이더 {t.Collider.size.x:0.000} x {t.Collider.size.y:0.000} x {t.Collider.size.z:0.000} @ y {t.Collider.center.y:0.000}");
        sb.AppendLine($"  횃불 {t.Torch.x:0.000} x {t.Torch.y:0.000} · 사슬 {t.Hang:0.000} · 화로 발자국 {t.Bowl:0.000} " +
                      $"· 바닥 y {t.FloorY:0.000}");

        const string dir = "Assets/__MeshySelfTest";
        AssetDatabase.DeleteAsset(dir);
        AssetDatabase.CreateFolder("Assets", "__MeshySelfTest");
        var models = new List<GameObject>();
        Material plain = null;
        try
        {
            plain = new Material(t.Template.shader);
            GameObject model = BoxModel("Dummy", new Vector3(3f, 7f, -4f), new Vector3(7f, 13f, -2f), plain, dir, models);

            var notes = new List<string>();
            Check(fails, model, "Wall", 1, "Base", t, dir, notes);
            Check(fails, model, "Prop", 0, "Light", t, dir, notes);   // 챕터 0 — 벽에 건다
            Check(fails, model, "Prop", 2, "Light", t, dir, notes);   // 챕터 2 — 바닥 화로
            Check(fails, model, "Prop", 3, "Light2", t, dir, notes);
            Check(fails, model, "Prop", 1, "Wall", t, dir, notes);
            Check(fails, model, "Prop", 3, "Floor", t, dir, notes);
            Check(fails, model, "Prop", 0, "Big", t, dir, notes);

            // 다시 들여도 뿌리가 같아야 층 장식의 자리 덮어쓰기가 살아남는다
            string again = dir + "/Wall_C1_Base.prefab";
            long before = RootId(again);
            BuildPrefab(model, "Wall_C1_Base", "Wall", 1, "Base", false, t, dir, notes);
            if (before == 0 || RootId(again) != before)
                fails.Add("다시 들이니 프리팹 뿌리가 바뀌었다 — 층 장식의 자리가 끊긴다");

            // 복셀 키트는 옛 그림 단위로 그린다 — 칸을 채우는 32 복셀 벽(바닥면 y 2.0)은 배율 0.1 그대로 옛 벽 자리에 선다
            GameObject voxelWall = BoxModel("VoxelWall", new Vector3(-1.6f, 2f, -1.6f), new Vector3(1.6f, 5f, 1.6f), plain, dir, models);
            CheckVoxel(fails, voxelWall, "Wall", 2, "A", t, dir, notes, new Vector3(0.32f, 0.3f, 0.32f));
            // 걷는 칸에 10 복셀(발목 8 복셀보다 크다) — 그때만 줄인다
            GameObject voxelTall = BoxModel("VoxelTall", new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 1f, 0.5f), plain, dir, models);
            CheckVoxel(fails, voxelTall, "Prop", 2, "Floor", t, dir, notes, new Vector3(AnkleHeight, AnkleHeight, AnkleHeight));
            CheckGlow(fails, t, dir);
            sb.AppendLine("  " + string.Join("\n  ", notes));
        }
        finally
        {
            foreach (GameObject go in models)
                Object.DestroyImmediate(go);
            if (plain != null)
                Object.DestroyImmediate(plain);
            AssetDatabase.DeleteAsset(dir);
        }

        CheckVariants(fails, sb);
        sb.AppendLine(fails.Count == 0 ? "  통과" : "  실패:\n    " + string.Join("\n    ", fails));
        return sb.ToString();
    }

    /// <summary>lo~hi 상자 하나짜리 가짜 모델. 메시는 에셋으로 남긴다(프리팹이 가리킬 수 있게).</summary>
    static GameObject BoxModel(string name, Vector3 lo, Vector3 hi, Material material, string dir, List<GameObject> made)
    {
        var mesh = new Mesh { name = name };
        var v = new Vector3[8];
        for (int i = 0; i < 8; i++)
            v[i] = new Vector3((i & 1) != 0 ? hi.x : lo.x, (i & 2) != 0 ? hi.y : lo.y, (i & 4) != 0 ? hi.z : lo.z);
        mesh.vertices = v;
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 4, 5, 6, 5, 7, 6, 0, 1, 4, 1, 5, 4,
                                 2, 6, 3, 3, 6, 7, 0, 4, 2, 2, 4, 6, 1, 3, 5, 3, 7, 5 };
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, $"{dir}/{name}.asset");

        var go = new GameObject(name);
        made.Add(go);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        go.AddComponent<BoxCollider>();   // 지워져야 한다
        return go;
    }

    /// <summary>팔레트 옆에 {이름}_Emit.png 가 있으면 재질이 빛나고(빛 그림·_EMISSION), 없으면 옛 벽 재질과 같은가.
    /// 4x1 팔레트 둘을 CPU 로 써서 본다 — GPU 가 없어도 돈다.</summary>
    static void CheckGlow(List<string> fails, Targets t, string dir)
    {
        var palette = new Texture2D(4, 1, TextureFormat.RGBA32, false);
        var lit = new Color32[] { new Color32(80, 60, 40, 255), new Color32(255, 180, 60, 255),
                                  new Color32(90, 90, 90, 255), new Color32(30, 30, 30, 255) };
        palette.SetPixels32(lit);
        byte[] colors = palette.EncodeToPNG();
        lit[0] = lit[2] = lit[3] = new Color32(0, 0, 0, 255);   // 불꽃 칸(1)만 남긴다
        palette.SetPixels32(lit);
        File.WriteAllBytes(dir + "/Glow.png", colors);
        File.WriteAllBytes(dir + "/Glow_Emit.png", palette.EncodeToPNG());
        File.WriteAllBytes(dir + "/Plain.png", colors);
        Object.DestroyImmediate(palette);
        foreach (string file in new[] { "Glow.png", "Glow_Emit.png", "Plain.png" })
            AssetDatabase.ImportAsset($"{dir}/{file}");

        foreach (string name in new[] { "Glow", "Plain" })
        {
            var src = new Material(t.Template.shader) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/{name}.png") };
            Material m = MakeMaterial(src, t.Template, $"{dir}/{name}Mat");
            Object.DestroyImmediate(src);

            bool glows = m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionMap") && m.GetTexture("_EmissionMap") != null;
            if (name == "Glow" && glows == false)
                fails.Add("빛 그림(_Emit)이 있는데 재질이 빛나지 않는다");
            if (name == "Plain" && m.IsKeywordEnabled("_EMISSION") != t.Template.IsKeywordEnabled("_EMISSION"))
                fails.Add("빛 그림이 없는데 재질의 빛 설정이 옛 벽 재질과 다르다");
        }
    }

    /// <summary>복셀 그림 — 배율 0.1(넘치면 그만큼 줄여서) 들어가 크기가 want 이고 자리에 붙었는가.</summary>
    static void CheckVoxel(List<string> fails, GameObject model, string kind, int chapter, string slot,
                           Targets t, string dir, List<string> notes, Vector3 want)
    {
        string name = $"{kind}_C{chapter}_{slot}";
        string path = BuildPrefab(model, name, kind, chapter, slot, true, t, dir, notes);
        if (path == null)
        {
            fails.Add($"{name}(복셀): 프리팹을 못 만들었다");
            return;
        }

        Vector3 at;
        Rule(kind, chapter, slot, t, out _, out _, out at);
        GameObject go = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Bounds b;
            Box(go, false, out b);
            if ((b.size - want).magnitude > 1e-4f)
                fails.Add($"{name}(복셀): 크기 {b.size:F4} 가 {want:F4} 가 아니다");
            if ((new Vector3(b.center.x, b.min.y, b.center.z) - at).magnitude > 1e-4f)
                fails.Add($"{name}(복셀): 바닥 가운데가 자리 {at:F4} 에 붙지 않았다");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(go);
        }
    }

    static void Check(List<string> fails, GameObject model, string kind, int chapter, string slot,
                      Targets t, string dir, List<string> notes)
    {
        string name = $"{kind}_C{chapter}_{slot}";
        string path = BuildPrefab(model, name, kind, chapter, slot, false, t, dir, notes);
        if (path == null)
        {
            fails.Add($"{name}: 프리팹을 못 만들었다");
            return;
        }

        Vector3 max, at;
        bool back;
        Rule(kind, chapter, slot, t, out max, out back, out at);
        GameObject go = PrefabUtility.LoadPrefabContents(path);
        try
        {
            const float eps = 1e-4f;
            Bounds b;
            Box(go, false, out b);

            bool touches = false;
            for (int a = 0; a < 3; a++)
            {
                if (max[a] <= 0f)
                    continue;
                if (b.size[a] > max[a] + eps)
                    fails.Add($"{name}: 축 {a} 크기 {b.size[a]:0.0000} 가 한계 {max[a]:0.0000} 를 넘는다");
                touches |= Mathf.Abs(b.size[a] - max[a]) < eps;
            }
            if (touches == false)
                fails.Add($"{name}: 어느 축도 한계에 닿지 않는다 — 덜 키웠다");
            if (Mathf.Abs(b.size.y / b.size.x - 1.5f) > 1e-3f || Mathf.Abs(b.size.z / b.size.x - 0.5f) > 1e-3f)
                fails.Add($"{name}: 비율이 4:6:2 에서 어긋났다 — 찌그러졌다");

            Vector3 pivot = back ? new Vector3(b.center.x, b.center.y, b.max.z)
                                 : new Vector3(b.center.x, b.min.y, b.center.z);
            if ((pivot - at).magnitude > eps)
                fails.Add($"{name}: 붙는 점 {pivot:F4} 이 자리 {at:F4} 가 아니다");

            Collider[] cols = go.GetComponentsInChildren<Collider>(true);
            if (kind == "Wall")
            {
                BoxCollider box = go.GetComponent<BoxCollider>();
                if (cols.Length != 1 || box == null
                    || (box.center - t.Collider.center).magnitude > eps || (box.size - t.Collider.size).magnitude > eps)
                    fails.Add($"{name}: 콜라이더가 옛 벽({WallKey})과 다르다");
                foreach (Transform tr in go.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.gameObject.layer == (int)Define.Layer.Wall)
                        continue;
                    fails.Add($"{name}: {tr.name} 의 레이어가 벽이 아니다");
                    break;
                }
            }
            else if (cols.Length != 0)
            {
                fails.Add($"{name}: 소품에 콜라이더가 남았다");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(go);
        }
    }

    static long RootId(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        string guid;
        long id;
        return prefab != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out guid, out id) ? id : 0;
    }

    /// <summary>MapBuilder.WallVariant — 비율(기본형 약 80%, 나머지 셋 약 7%)과 같은 변형이 이웃하지 않는지.</summary>
    static void CheckVariants(List<string> fails, StringBuilder sb)
    {
        const int n = 200;
        var count = new int[MapBuilder.WallVariants.Length];
        bool adjacent = false;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                int v = MapBuilder.WallVariant(57, new Vector2Int(x, y));
                count[v]++;
                if (v != 0 && adjacent == false
                    && (v == MapBuilder.WallVariant(57, new Vector2Int(x - 1, y))
                        || v == MapBuilder.WallVariant(57, new Vector2Int(x, y - 1))))
                {
                    fails.Add($"벽 변형 {MapBuilder.WallVariants[v]} 가 ({x},{y}) 에서 이웃과 붙었다");
                    adjacent = true;
                }
            }
        }

        var share = new string[count.Length];
        for (int i = 0; i < count.Length; i++)
        {
            float p = count[i] / (float)(n * n);
            share[i] = $"{MapBuilder.WallVariants[i]} {p * 100f:0.0}%";
            bool ok = i == 0 ? p > 0.74f && p < 0.86f : p > 0.05f && p < 0.09f;
            if (ok == false)
                fails.Add($"벽 변형 {MapBuilder.WallVariants[i]} 비율 {p * 100f:0.0}% 가 어긋났다");
        }
        sb.AppendLine("  벽 변형 " + string.Join(" · ", share));
    }
}
