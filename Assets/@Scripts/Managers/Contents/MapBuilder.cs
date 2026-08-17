using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CSV 에서 파싱한 MapData 로 던전 한 층을 런타임에 조립한다.
///
/// 손수 만든 Dungeon_CC_FFF 프리팹이 있으면 GameManager 가 그것을 그대로 쓰고,
/// 프리팹이 없는 층(100층 대부분)만 이쪽으로 넘어온다.
///
/// GameManager.RefreshMap 이 "Monsters" / "Items" / "Doors" / "Pillars" / "Levers"
/// 컨테이너를 이름으로 찾으므로, 비어 있더라도 항상 만들어 둔다.
/// </summary>
public static class MapBuilder
{
    // 벽 아트는 챕터 00 세트만 존재한다. 챕터 분위기는 아래 틴트로 낸다.
    const string WALL_PREFAB_FORMAT = "Tilemap_C00_W{0:00}";
    const string WALL_PREFAB_FALLBACK = "Tilemap_C00_W02";

    static readonly Color[] ChapterTint =
    {
        new Color(1.00f, 1.00f, 1.00f), // 00 이끼 낀 지하 묘소 - 원본
        new Color(0.72f, 0.86f, 1.00f), // 01 무너진 수로     - 푸른 물빛
        new Color(1.00f, 0.72f, 0.55f), // 02 잿빛 용광로     - 붉은 열기
        new Color(0.78f, 0.94f, 1.00f), // 03 얼어붙은 심층   - 창백한 냉기
        new Color(0.70f, 0.62f, 0.90f), // 04 왕좌의 균열     - 보랏빛 심연
    };

    static readonly Color[] ChapterLight =
    {
        new Color(1.00f, 0.96f, 0.88f),
        new Color(0.62f, 0.80f, 1.00f),
        new Color(1.00f, 0.68f, 0.45f),
        new Color(0.75f, 0.92f, 1.00f),
        new Color(0.62f, 0.50f, 0.88f),
    };

    public static Color GetChapterTint(int chapter)
    {
        return ChapterTint[Mathf.Clamp(chapter, 0, ChapterTint.Length - 1)];
    }

    public static Color GetChapterLight(int chapter)
    {
        return ChapterLight[Mathf.Clamp(chapter, 0, ChapterLight.Length - 1)];
    }

    // 손수 만든 도입부. 여기만 프리팹을 그대로 쓴다 (Tools/generate_content.py 의
    // HANDMADE_FLOORS 와 같은 목록이어야 한다 — 한쪽만 늘리면 인트로가 깨진다).
    static readonly HashSet<string> HandAuthored = new HashSet<string>
    {
        "00_000", "00_001", "00_002", "00_003",
    };

    public static bool IsHandAuthored(string dungeonId)
    {
        return HandAuthored.Contains(dungeonId);
    }

    public static int GetChapter(int mapId)
    {
        Data.StageInfoData info;
        if (Managers.Data.StageInfoDic.TryGetValue(mapId, out info) == false)
            return 0;
        int chapter;
        if (int.TryParse(info.DungeonID.Substring(0, 2), out chapter) == false)
            return 0;
        return chapter;
    }

    /// <summary>지정한 맵 ID 의 층을 만들어 반환한다. 데이터가 없으면 null.</summary>
    public static GameObject Build(int mapId, Transform parent)
    {
        Data.MapData mapData;
        // 데이터가 없으면 조용히 null. 호출부가 프리팹 폴백으로 넘어간다.
        if (Managers.Data.MapDic.TryGetValue(mapId, out mapData) == false
            || mapData.Objects == null || mapData.Objects.Count == 0)
            return null;

        Data.StageInfoData info;
        string dungeonId = Managers.Data.StageInfoDic.TryGetValue(mapId, out info)
            ? info.DungeonID : mapId.ToString();
        int chapter = GetChapter(mapId);
        Color tint = GetChapterTint(chapter);

        GameObject root = new GameObject($"Dungeon_{dungeonId}");
        root.transform.SetParent(parent, false);

        Transform tiles = NewContainer(root, "Tiles");
        Transform monsters = NewContainer(root, "Monsters");
        Transform bossMonsters = NewContainer(root, "BossMonsters");
        Transform items = NewContainer(root, "Items");
        Transform doors = NewContainer(root, "Doors");
        Transform pillars = NewContainer(root, "Pillars");
        Transform levers = NewContainer(root, "Levers");

        PlaceFloorField(root, mapData, tint);

        int doorCount = 0;

        foreach (Data.ObjectData obj in mapData.Objects)
        {
            Vector3 pos = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);

            switch ((Define.ObjectType)obj.ObjectType)
            {
                case Define.ObjectType.Floor:
                    Place("Tilemap_1", tiles, pos, tint);
                    break;

                case Define.ObjectType.Wall:
                    {
                        string key = string.Format(WALL_PREFAB_FORMAT, obj.Id);
                        GameObject wall = Place(key, tiles, pos, tint);
                        if (wall == null)
                            Place(WALL_PREFAB_FALLBACK, tiles, pos, tint);
                        break;
                    }

                case Define.ObjectType.Monster:
                    {
                        GameObject go = Place("Monster", monsters, pos, Color.white);
                        if (go != null)
                        {
                            MonsterController mc = Bind<MonsterController>(go);
                            mc.id = obj.Id;
                            mc._monsterIndex_forActive = obj.Count;
                            SetupLook(go, obj.Id);
                        }
                        break;
                    }

                case Define.ObjectType.BossMonster:
                    {
                        // 보스는 "Monsters" 밑에 둔다. RefreshMap 이 그 컨테이너만 훑으면서
                        // MonsterActiveDic 로 죽은 놈을 끄기 때문에, BossMonsters 에 두면
                        // 잡은 보스가 재입장/로드 때마다 되살아난다.
                        GameObject go = Place("BossMonster", monsters, pos, Color.white);
                        if (go != null)
                        {
                            go.tag = "Boss";
                            MonsterController mc = Bind<MonsterController>(go);
                            mc.id = obj.Id;
                            mc._monsterIndex_forActive = obj.Count;
                            SetupLook(go, obj.Id);
                            FitColliderToCell(go);
                        }
                        break;
                    }

                case Define.ObjectType.CItem:
                    {
                        GameObject go = Place("ConsumableItem", items, pos, Color.white);
                        if (go != null)
                        {
                            ConsumableItem ci = Bind<ConsumableItem>(go);
                            ci.id = obj.Id;
                            ci._itemIndex_forActive = obj.Count;
                        }
                        break;
                    }

                case Define.ObjectType.Eitem:
                    {
                        GameObject go = Place("EquipItem", items, pos, Color.white);
                        if (go != null)
                        {
                            Equip eq = Bind<Equip>(go);
                            eq._id = obj.Id;
                            eq._itemIndex_forActive = obj.Count;
                        }
                        break;
                    }

                case Define.ObjectType.Door:
                    BuildDoor(obj, doors, pos, tint, doorCount++);
                    break;

                case Define.ObjectType.Pillar:
                    {
                        GameObject holder = new GameObject($"pillar{obj.Count}");
                        holder.transform.SetParent(pillars, false);
                        holder.transform.localPosition = pos;
                        GameObject go = Place("Tilemap_13", holder.transform, Vector3.zero, tint);
                        if (go != null)
                        {
                            Pillar p = Bind<Pillar>(go);
                            p._pillarIndex_forActive = obj.Count;
                        }
                        break;
                    }

                case Define.ObjectType.Lever:
                    {
                        GameObject holder = new GameObject($"lever{obj.Count}");
                        holder.transform.SetParent(levers, false);
                        holder.transform.localPosition = pos;
                        GameObject go = Place("Tilemap_12", holder.transform, Vector3.zero, tint);
                        if (go != null)
                        {
                            Lever l = Bind<Lever>(go);
                            l._leverIndex_forActive = obj.Count;
                        }
                        break;
                    }

                case Define.ObjectType.SpawnPoint:
                    {
                        GameObject go = new GameObject("SpawnPoint");
                        go.tag = "SpawnPoint";
                        go.transform.SetParent(tiles, false);
                        go.transform.localPosition = pos;
                        break;
                    }

                case Define.ObjectType.Portal:
                    BuildPortal(obj, tiles, pos, tint, mapId);
                    break;
            }
        }

        return root;
    }

    static void BuildDoor(Data.ObjectData obj, Transform doors, Vector3 pos, Color tint, int index)
    {
        // Door.Start() 가 transform.parent.GetChild(1) 을 자물쇠 위치로 쓴다.
        // 따라서 [0]=문, [1]=자물쇠위치 순서의 부모가 반드시 필요하다.
        GameObject holder = new GameObject($"door{index}");
        holder.transform.SetParent(doors, false);
        holder.transform.localPosition = pos;

        GameObject go = Place($"Tilemap_{obj.Id}", holder.transform, Vector3.zero, tint);
        if (go == null)
            go = Place("Tilemap_3", holder.transform, Vector3.zero, tint);

        GameObject lockPos = new GameObject("DoorLockPos");
        lockPos.transform.SetParent(holder.transform, false);

        if (go == null)
            return;

        // 문 셀 id 3/4/5 -> 초록/노랑/빨강 열쇠. 그 외(6~8)는 초록으로 둔다.
        int keyIndex = Mathf.Clamp(obj.Id - 3, 0, ConsumableItem.NUM_OF_KEYS - 1);
        foreach (Door door in Components<Door>(go))
        {
            door._doorIndex_forActive = obj.Count;
            door._keyIndex = keyIndex;
        }
    }

    static void BuildPortal(Data.ObjectData obj, Transform parent, Vector3 pos, Color tint, int mapId)
    {
        // 포탈은 각자 홀더를 갖는다. GameManager 가 보스 포탈을 숨길 때
        // transform.parent 를 끄기 때문에, 홀더가 없으면 층 전체가 꺼져 버린다.
        GameObject holder = new GameObject($"stairs_{obj.Id}_{mapId}");
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = pos;

        GameObject go = Place($"Tilemap_{obj.Id}", holder.transform, Vector3.zero, tint);
        if (go == null)
            return;

        // 14 = 위층 계단, 15 = 아래층 계단, 16 = 보스방
        PortalController.Type type = obj.Id == 14 ? PortalController.Type.UpStairs
                                   : obj.Id == 15 ? PortalController.Type.DownStairs
                                   : PortalController.Type.Boss;
        foreach (PortalController portal in Components<PortalController>(go))
        {
            portal._mapId = mapId;
            portal._portalType = type;
        }
    }

    /// <summary>
    /// 프리팹에 이미 붙어 있는 컴포넌트를 전부 돌려준다. 하나도 없으면 루트에 붙인다.
    ///
    /// 타일 프리팹(Tilemap_3, Tilemap_14 …)은 자식에도 Door/PortalController 를 갖고 있다.
    /// 루트에만 값을 넣으면 자식 쪽이 기본값(_mapId=0, _doorIndex=0)으로 남아
    /// SearchPortal 이 엉뚱한 계단을 찾거나 다른 문의 잠금이 풀린다.
    /// </summary>
    static T[] Components<T>(GameObject go) where T : Component
    {
        T[] found = go.GetComponentsInChildren<T>(true);
        if (found.Length > 0)
            return found;
        return new[] { go.AddComponent<T>() };
    }

    /// <summary>컴포넌트가 하나뿐인 프리팹(Monster, ConsumableItem …)용 단축형.</summary>
    static T Bind<T>(GameObject go) where T : Component
    {
        return Components<T>(go)[0];
    }

    /// <summary>
    /// 바닥 그림(Decos/BG).
    ///
    /// 손으로 만든 층은 층마다 그린 FloorField 스프라이트를 깔아 두는데,
    /// 100층을 손으로 그릴 수 없어 공용 그림 하나를 층 크기에 맞춰 깐다.
    /// 없으면 두 가지가 같이 망가진다 —
    ///   1) 바닥이 비어 보인다.
    ///   2) CameraController.SetupCameraConfiner 가 Decos/BG 로 범위를 잡는데
    ///      그게 없으면 이전 층 경계가 남아 카메라가 플레이어를 못 따라간다.
    /// </summary>
    static void PlaceFloorField(GameObject root, Data.MapData mapData, Color tint)
    {
        Sprite sprite = Resources.Load<Sprite>("Sprites/00/FloorField_99_999");
        if (sprite == null)
        {
            Debug.LogWarning("[MapBuilder] 공용 바닥 그림을 못 찾았다");
            return;
        }

        // 층이 차지하는 칸 범위를 실제 오브젝트에서 잰다.
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (Data.ObjectData obj in mapData.Objects)
        {
            if (obj.Position.X < minX) minX = obj.Position.X;
            if (obj.Position.X > maxX) maxX = obj.Position.X;
            if (obj.Position.Z < minZ) minZ = obj.Position.Z;
            if (obj.Position.Z > maxZ) maxZ = obj.Position.Z;
        }
        if (minX > maxX)
            return;

        float width = (maxX - minX) + Define.TILE_SIZE;
        float height = (maxZ - minZ) + Define.TILE_SIZE;

        Transform decos = NewContainer(root, "Decos");
        GameObject bg = new GameObject("BG");
        bg.tag = "BG";
        bg.transform.SetParent(decos, false);
        bg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // 눕혀서 바닥에 깐다
        bg.transform.localPosition = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = -100;   // 타일보다 아래

        Vector2 native = sprite.bounds.size;
        if (native.x > 0.0001f && native.y > 0.0001f)
            bg.transform.localScale = new Vector3(width / native.x, height / native.y, 1f);
    }

    /// <summary>
    /// 보스 콜라이더를 제 칸 하나에 맞춘다.
    ///
    /// 보스는 스프라이트가 커서 콜라이더도 제 칸을 벗어나 여러 칸에 걸치고,
    /// 중심마저 어긋나 있다. 그러면 플레이어가 제 키높이로 쏘는 얇은 충돌
    /// 광선이 보스를 스치지 못해서, 바로 옆에서 밀어도 전투가 열리지 않는다.
    /// 킹슬라임이 같은 이유로 안 잡혔고, 챕터 보스도 마찬가지였다.
    /// </summary>
    static void FitColliderToCell(GameObject go)
    {
        BoxCollider col = go.GetComponentInChildren<BoxCollider>();
        if (col == null)
            return;

        // 가로/세로는 제 칸 하나. 높이는 넉넉히 준다.
        // 플레이어의 충돌 광선은 정확히 키높이(TILE/2)에서 나가는데, 높이도
        // 한 칸으로 맞추면 콜라이더 위끝이 딱 그 높이라 광선이 경계에 걸쳐
        // 빗나간다 — 20층 보스를 바로 옆에서 밀어도 "광선 빈손" 이었다.
        Vector3 scale = col.transform.lossyScale;
        col.center = Vector3.zero;
        col.size = new Vector3(
            Define.TILE_SIZE / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            Define.TILE_SIZE * 16f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            Define.TILE_SIZE / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
    }

    /// <summary>
    /// 몬스터의 겉모습을 그 몬스터 데이터에 맞춘다.
    ///
    ///  - 세로 비율: 플레이어와 보스 프리팹은 (1,2,1) 인데 일반 몬스터 프리팹만
    ///    (1,1,1) 이라 화면에서 납작하게 눌려 보였다.
    ///  - 그림: 프리팹 하나를 모든 몬스터가 함께 쓰기 때문에, 무엇을 그릴지는
    ///    데이터의 대기 애니메이션(Mob_C0_I003 …)이 정한다. 이걸 지정하지 않으면
    ///    전부 애니메이터 기본 상태로 나와서 이름과 그림이 따로 놀았고,
    ///    보스는 컨트롤러에 없는 상태(Boss_C1_I000)를 물고 있어 엉뚱하게 보였다.
    /// </summary>
    static void SetupLook(GameObject go, int id)
    {
        go.transform.localScale = new Vector3(1f, 2f, 1f);

        Data.MonsterData md;
        if (Managers.Data.MonsterDic.TryGetValue(id, out md) == false)
        {
            Debug.LogWarning($"[MapBuilder] 몬스터 {id} 데이터가 없다");
            return;
        }
        if (string.IsNullOrEmpty(md.IdleAnimStr))
            return;

        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim != null)
            anim.Play(md.IdleAnimStr);

        // 그림은 열두 종뿐이라 100층을 채우면 계속 같은 놈이 나온다.
        // 색을 바꿔 다른 몬스터로 쓴다 — 챕터가 색조, 층 안 서열이 진하기다.
        Color tint = MonsterTint.Of(id);
        if (tint != Color.white)
        {
            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                sr.color = tint;
        }
    }

    static Transform NewContainer(GameObject root, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        return go.transform;
    }

    static GameObject Place(string key, Transform parent, Vector3 localPos, Color tint)
    {
        GameObject prefab = Managers.Resource.Load<GameObject>(key);
        if (prefab == null)
            return null;

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = prefab.name;
        go.transform.localPosition = localPos;

        if (tint != Color.white)
            ApplyTint(go, tint);

        return go;
    }

    static readonly List<Renderer> _renderers = new List<Renderer>();
    static MaterialPropertyBlock _tintBlock;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    static void ApplyTint(GameObject go, Color tint)
    {
        // renderer.material 을 건드리면 렌더러마다 머티리얼 사본이 생긴다.
        // 한 챕터에 타일이 수만 개라 그대로 두면 메모리가 감당이 안 된다.
        // MaterialPropertyBlock 은 사본 없이 색만 덮어쓴다.
        if (_tintBlock == null)
            _tintBlock = new MaterialPropertyBlock();

        go.GetComponentsInChildren(true, _renderers);
        for (int i = 0; i < _renderers.Count; i++)
        {
            SpriteRenderer sr = _renderers[i] as SpriteRenderer;
            if (sr != null)
            {
                sr.color = tint;
                continue;
            }

            Renderer r = _renderers[i];
            r.GetPropertyBlock(_tintBlock);
            _tintBlock.SetColor(BaseColorId, tint);  // URP
            _tintBlock.SetColor(ColorId, tint);      // 빌트인/일부 커스텀 셰이더
            r.SetPropertyBlock(_tintBlock);
        }
    }
}
