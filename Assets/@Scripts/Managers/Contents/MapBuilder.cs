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
        PlaceDeco(root, dungeonId);

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
                            StretchBillboard(go);   // 그대로 두면 눌려 보인다
                            SitOnFloor(go);
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
                            StretchBillboard(go);
                            SitOnFloor(go);
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

    /// <summary>그 층의 장식 프리팹을 붙인다 (Deco_CC_FFF).
    ///
    /// 장식은 맵 데이터에 넣지 않는다. 놀이에 영향이 없는 것이 데이터에 섞이면
    /// 밸런스·경로 검증에까지 딸려 다니고, 미술을 손보려면 생성기를 다시 돌려야 한다.
    /// 층 이름과 짝지은 프리팹 하나만 열면 되게 둔다 —
    /// 손수 만든 1~4층이 이미 그 규칙(Deco_00_000)을 쓰고 있다.
    /// 없으면 조용히 넘어간다. 장식이 없다고 층이 안 만들어질 이유는 없다.</summary>
    static void PlaceDeco(GameObject root, string dungeonId)
    {
        GameObject prefab = Managers.Resource.Load<GameObject>($"Deco_{dungeonId}");
        if (prefab == null)
            return;

        // 바닥 필드가 이미 만든 "Decos" 를 같이 쓴다. 따로 만들면 같은 이름이 둘이 되고,
        // CameraController 가 Find("Decos/BG") 로 찾는 바닥을 놓친다.
        Transform decos = root.transform.Find("Decos");
        if (decos == null)
            decos = NewContainer(root, "Decos");

        GameObject go = Object.Instantiate(prefab, decos);
        go.name = prefab.name;
        go.transform.localPosition = Vector3.zero;
    }

    static void BuildDoor(Data.ObjectData obj, Transform doors, Vector3 pos, Color tint, int index)
    {
        // Door.Start() 가 transform.parent.GetChild(1) 을 자물쇠 위치로 쓴다.
        // 따라서 [0]=문, [1]=자물쇠위치 순서의 부모가 반드시 필요하다.
        GameObject holder = new GameObject($"door{index}");
        holder.transform.SetParent(doors, false);
        holder.transform.localPosition = pos;

        // 문 셀 id 는 색과 방향 두 가지로 갈린다.
        //   3/4/5 가로문(좌우가 벽), 6/7/8 세로문(위아래가 벽)
        // 색은 3 으로 나눈 나머지다 — 3·6 초록, 4·7 노랑, 5·8 빨강.
        // 예전에는 Clamp(id-3) 이라 6·7·8 이 전부 빨강을 달라고 했다.
        int keyIndex = (obj.Id - 3) % ConsumableItem.NUM_OF_KEYS;
        bool vertical = obj.Id >= 3 + ConsumableItem.NUM_OF_KEYS;

        // 프리팹 이름은 셀 코드와 순서가 다르다 — <b>색이 먼저, 방향이 나중</b>이다.
        //   Tilemap_3/4 초록, 5/6 노랑, 7/8 빨강. 짝의 뒤쪽(4·6·8)만 Y 90° 로 돌아 있다.
        // 회전은 프리팹에 구워져 있고 여기서는 주지 않으므로, 이름을 그대로 쓰면
        // (셀 4 -> Tilemap_4 처럼) 그림이 통째로 90° 어긋난다. 매 층 두 번째 문이
        // 그랬다. 번역은 여기와 layout_gen.door_art 두 곳뿐이고 서로 같아야 한다.
        int artId = 3 + keyIndex * 2 + (vertical ? 1 : 0);

        GameObject go = Place($"Tilemap_{artId}", holder.transform, Vector3.zero, tint);
        if (go == null)
            go = Place(vertical ? "Tilemap_4" : "Tilemap_3", holder.transform, Vector3.zero, tint);

        GameObject lockPos = new GameObject("DoorLockPos");
        lockPos.transform.SetParent(holder.transform, false);

        if (go == null)
            return;

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
    /// 손으로 만든 층은 층마다 그린 FloorField 통그림을 깔아 두는데, 100층을 손으로
    /// 그릴 수 없어 **한 칸짜리 바닥 타일을 층 크기만큼 타일링**한다.
    ///
    /// 예전에는 32x32 자리표(FloorField_99_999)를 층 크기로 늘려 깔았다. 그 그림이
    /// 순수 검정이라 생성 층은 바닥이 통째로 검은 사각형이었다 — 셰이더도 정렬순서도
    /// 아니고 그냥 깔 그림이 없었던 것이다.
    ///
    /// 칸마다 타일 오브젝트를 놓지 않는 이유: 생성 층은 걸어다니는 칸이 층당 340개라
    /// (23x27 격자 중 벽이 아닌 칸) 그만큼 오브젝트가 늘고, 100층 녹화 시간에 그대로
    /// 얹힌다. 타일링은 오브젝트가 0개 늘고 드로우콜도 하나다.
    /// 격자에 빈 칸이 없어서(모든 칸이 벽 아니면 바닥) 통째로 깔아도 남는 데가 없고,
    /// 벽 프리팹이 제 칸을 정확히 덮으므로 벽 밑에 깔린 바닥은 보이지 않는다.
    ///
    /// **여기서 축척은 1 이다.** 벽·장식이 0.1 인 것은 3.2유닛으로 그린 3D 모델이라
    /// 그렇고, 이 타일은 32px / PPU 100 = 0.32유닛 = Define.TILE_SIZE 로 이미 한 칸이다.
    /// 게다가 SpriteDrawMode.Tiled 는 넓이를 localScale 이 아니라 sr.size(월드 단위)로
    /// 받는다 — 스케일을 건드리면 층이 아니라 타일 한 칸이 커지거나 작아진다.
    ///
    /// BG 를 없애면 안 된다 — CameraController.SetupCameraConfiner 가 Decos/BG 로
    /// 카메라 범위를 잡는다. 그림을 못 찾으면 BG 도 만들지 않는데, 그건 의도한 것이다.
    /// 스프라이트 없는 SpriteRenderer 는 bounds 가 0 이라 카메라가 한 점에 갇힌다.
    /// 없으면 카메라가 벽으로 범위를 재는 폴백(TryWallBounds)으로 넘어간다.
    /// </summary>
    static void PlaceFloorField(GameObject root, Data.MapData mapData, Color tint)
    {
        Sprite sprite = Resources.Load<Sprite>("Sprites/00/FloorTile_99_999");
        if (sprite == null)
        {
            Debug.LogWarning("[MapBuilder] 공용 바닥 타일을 못 찾았다");
            return;
        }

        // 층이 차지하는 칸 범위를 실제 오브젝트에서 잰다.
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (Data.ObjectData obj in mapData.Objects)
        {
            // 빈 칸(Void)은 층에 속하지 않는다. CSV 끝의 빈 줄이 24번째 행에
            // Void 하나를 만드는데, 그걸 세면 격자가 23행인 층에 바닥 한 줄이
            // 벽 바깥으로 삐져나온다 (검은 자리표일 때는 안 보였다).
            if ((Define.ObjectType)obj.ObjectType == Define.ObjectType.Void)
                continue;

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

        // 한 칸 그림을 층 크기만큼 반복해서 채운다. 층은 항상 칸 수의 정수배라
        // (23 x 27 칸) 잘리는 타일 없이 딱 맞는다.
        // 타일 스프라이트는 Full Rect 여야 한다 — Tight 메시면 Unity 가 경고를 찍고
        // Simple 로 되돌려서 그림 한 장이 늘어난다(예전 모습). 임포트 설정에
        // spriteMeshType: 0 을 박아 두었다.
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(width, height);
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
    /// 카메라가 내려다보는 만큼 세로가 눌린다. 그만큼 늘려 세운다.
    ///
    /// 그림은 전부 X회전 0 으로 <b>서 있다</b>. 카메라가 각도 t 로 내려다보면 그 세로가
    /// cos(t) 로 줄어드니 1/cos(t) 를 곱해야 제 비율로 보인다.
    /// 60도면 2.00 인데 <b>이 게임의 카메라는 50도</b>라 1.56 이다 — 2.0 을 박아 두었더니
    /// 물약이 29% 길쭉했다 (화면에서 잰 세로/가로 1.17~1.29, 제 비율이면 1.00).
    /// </summary>
    static float BillboardStretch()
    {
        Camera cam = Managers.Game != null ? Managers.Game.MainCamera : null;
        if (cam == null)
            cam = Camera.main;

        float pitch = cam != null ? cam.transform.eulerAngles.x : 50f;
        pitch = Mathf.Repeat(pitch, 360f);
        if (pitch > 180f)
            pitch = 360f - pitch;
        pitch = Mathf.Clamp(pitch, 1f, 89f);
        return 1f / Mathf.Cos(pitch * Mathf.Deg2Rad);
    }

    /// <summary>
    /// 바닥에 세워 두는 2D 그림의 비율과 크기를 맞춘다.
    ///
    /// 프리팹마다 이미 늘어난 정도가 다르다 — 몬스터는 (1,1,1) 인데 보스는 자식이
    /// (1,2,1) 이다. 그래서 무조건 곱하지 않고, <b>합쳐서</b> 목표 배율이 되게 맞춘다.
    /// 예전에 그냥 곱했더니 보스가 네 배가 되어 아래쪽 절반이 바닥에 묻혔다.
    /// </summary>
    static void StretchBillboard(GameObject go, float bulk = 1f)
    {
        if (go == null)
            return;

        float ratio = 1f;
        foreach (Transform tr in go.GetComponentsInChildren<Transform>(true))
        {
            if (tr == go.transform || tr.localScale.x <= 0.0001f)
                continue;
            ratio = Mathf.Max(ratio, tr.localScale.y / tr.localScale.x);
        }

        float stretch = BillboardStretch() / ratio;
        go.transform.localScale = new Vector3(bulk, bulk * stretch, 1f);
    }

    /// <summary>
    /// 그림의 아래끝을 바닥에 앉힌다.
    ///
    /// 스프라이트의 피벗이 가운데라 그대로 놓으면 절반이 바닥에 묻힌다. 그림마다
    /// 세로가 달라(잿빛 파수꾼 48x36, 심연의 거수 86x68) 같은 값으로 올릴 수 없으니
    /// 실제 스프라이트 아래끝을 재서 그만큼 올린다.
    ///
    /// 애니메이터는 Play 만으로는 그 프레임에 그림을 넣지 않는다. Update(0) 로
    /// 한 번 돌려야 bounds 가 실제 그림을 가리킨다 — 전투창에서 같은 함정을 겪었다.
    /// </summary>
    static void SitOnFloor(GameObject go)
    {
        if (go == null)
            return;

        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim != null && anim.isActiveAndEnabled)
            anim.Update(0f);

        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return;

        float floor = go.transform.position.y;
        float bottom = sr.bounds.min.y;
        if (bottom < floor - 0.0001f)
            go.transform.position += new Vector3(0f, floor - bottom, 0f);
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
        // 보스와 정예는 몸집으로 구분한다.
        //
        // 킹 슬라임과 분열 3종의 그림(Boss_C0_*)은 도입부 연출 전용이라 생성 층에
        // 내보내지 않는다. 그래서 쓸 수 있는 그림은 몹 여덟 종뿐이고, 우두머리를
        // 우두머리로 보이게 할 방법이 크기와 색밖에 없다.
        float bulk = MonsterBulk(id);
        StretchBillboard(go, bulk);

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

        // 그림을 정한 뒤라야 아래끝을 잴 수 있다. 여기서 바닥에 앉힌다.
        SitOnFloor(go);
    }

    /// <summary>그 몬스터가 차지하는 몸집. 1 이 보통 몹이다.
    ///
    /// 한 칸을 넘기지는 않는다 — 넘기면 옆 칸까지 콜라이더가 걸쳐서 통로를 막는다.
    /// (FitColliderToCell 이 콜라이더는 한 칸으로 다시 맞춘다.)</summary>
    static float MonsterBulk(int id)
    {
        if (id >= 900 && id < 1000)
            return 1.45f;                  // 챕터 보스

        // 생성 몬스터 id = 1000 + 층*8 + 서열. 층에서 가장 센 놈이 정예다.
        if (id >= 1000 && id % 8 == 4)
            return 1.2f;

        return 1f;
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
