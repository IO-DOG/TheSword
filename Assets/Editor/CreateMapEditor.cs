using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CreateMapEditor : MonoBehaviour
{

//#if UNITY_EDITOR
//    // % (Ctrl), # (Shift), & (Alt)
//    [MenuItem("Tools/CreateMap %#c")]
//    private static void CreateMap()
//    {
//        #region ExcelData
//        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/MapData.csv").Split("\n");

//        GameObject parent = GameObject.Find("Parent");
//        if (parent == null)
//            parent = new GameObject { name = "Parent" };
//        else
//            Debug.Log("Parent ������Ʈ ���� �� ���� �ٶ�.");
//        GameObject monsters = GameObject.Find("Monsters");
//        if (monsters == null)
//            monsters = new GameObject { name = "Monsters" };
//        else
//            Debug.Log("Monsters ������Ʈ ���� �� ���� �ٶ�.");
//        GameObject items = GameObject.Find("Items");
//        if (items == null)
//            items = new GameObject { name = "Items" };
//        else
//            Debug.Log("Items ������Ʈ ���� �� ���� �ٶ�.");

//        GameObject bossMonsters = GameObject.Find("BossMonsters");

//        float coX = 0, coY = 0, coZ = 0;
//        float toAdd = 0;
//        float addToFloorY = 1.5f;
//        int monsterIndex = 0;
//        int bossMonsterIndex = 0;
//        int itemIndex = 0;
//        int doorIndex = 0;

//        for (int y = 0; y < lines.Length; y++)
//        {
//            string[] row = lines[y].Replace("\r", "").Split(',');

//            if (row.Length == 0)
//                continue;
//            if (string.IsNullOrEmpty(row[0]))
//                continue;

//            for (int x = 0; x < row.Length; ++x)
//            {
//                string block = row[x];

//                if (block == "0")
//                {
//                    GameObject voidObject = Resources.Load<GameObject>($"Tilemap_0");
//                    toAdd = voidObject.GetComponentInChildren<BoxCollider>().size.x;
//                    UnityEngine.Object.Instantiate(voidObject, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
//                }
//                else if (block[0] == 'I') // �������� ���
//                {
//                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
//                    toAdd = floor.GetComponentInChildren<BoxCollider>().size.x;
//                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
//                    //floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);

//                    // TODO ������ ����
//                    GameObject item = Resources.Load<GameObject>($"Item");
//                    UnityEngine.Object.Instantiate(item, new Vector3(coX, coY + 3f, coZ), Quaternion.identity, items.transform);
//                    //item.transform.localScale = new Vector3(1f, 1.4f, 1.4f);
//                    item.GetComponent<Item>().id = block[2] - '0';
//                    item.name = $"item{itemIndex}";
//                    item.GetComponent<Item>()._itemIndex_forActive = itemIndex++;
//                }
//                else if (block[0] == 'M') // ������ ���
//                {
//                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
//                    toAdd = floor.GetComponentInChildren<BoxCollider>().size.x;
//                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
//                    //floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);

//                    // TODO ���� ����
//                    GameObject monster = Resources.Load<GameObject>($"Monster");
//                    UnityEngine.Object.Instantiate(monster, new Vector3(coX, coY + 4.5f, coZ - 1f), Quaternion.identity, monsters.transform);
//                    //monster.transform.localScale = new Vector3(2f, 4f, 4f);
//                    monster.GetComponent<MonsterController>().id = block[2] - '0';
//                    monster.name = $"monster{monsterIndex}";
//                    monster.GetComponent<MonsterController>()._monsterIndex_forActive = monsterIndex++;
//                }
//                else if (block[0] == 'B') // ���� ������ ���
//                {
//                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
//                    toAdd = floor.GetComponentInChildren<BoxCollider>().size.x;
//                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
//                    //floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);

//                    // TODO ���� ���� ����
//                    //GameObject bossMonster = Resources.Load<GameObject>($"BossMonster");
//                    //UnityEngine.Object.Instantiate(bossMonster, new Vector3(coX, coY + 1f, coZ), Quaternion.identity, bossMonsters.transform);
//                    //bossMonster.transform.localScale = new Vector3(1f, 1.4f, 1.4f);
//                    //bossMonster.GetComponent<BossMonsterController>().id = block[2] - '0';
//                    //bossMonster.name = $"monster{bossMonsterIndex}";
//                    //bossMonster.GetComponent<BossMonsterController>()._index_forActive = bossMonsterIndex++;
//                }
//                else
//                {
//                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
//                    toAdd = floor.GetComponentInChildren<BoxCollider>().size.x;
//                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
//                    //floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);
//                    // TODO Ÿ�� ����
//                    if (block != "1") // �ٴ� Ÿ���� �ƴҰ��.
//                    {
//                        GameObject tile = Resources.Load<GameObject>($"Tilemap_{block}");
//                        UnityEngine.Object.Instantiate(tile, new Vector3(coX, coY, coZ), Quaternion.identity, parent.transform);
//                        //tile.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);

//                        if (block[0] >= '3' && block[0] <= '8')
//                        {
//                            tile.name = $"door{doorIndex}";
//                            tile.GetComponentInChildren<Door>()._doorIndex_forActive = doorIndex++;
//                        }
//                    }
//                }
//                coX += toAdd;
//            }
//            coZ += toAdd;
//            coX = 0;
//        }

//        #endregion
//    }

    private static void CreateMap_Test()
    {
        GameObject parent = GameObject.Find("Parent");
        if (parent == null)
            Object.Instantiate(parent);

        string[] lines = File.ReadAllText("Assets/@Resources/Map/output.txt").Split("\n");
        float x = 0, y = 0, z = 0;
        float toAdd = 3.2f;
        for (int lineY = 4; lineY < lines.Length; lineY++)
        {
            string[] row = lines[lineY].Split(' ');
            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            for (int lineX = 0; lineX < row.Length; lineX++)
            {
                if (row[lineX] == "0" || row[lineX] == " " || row[lineX] == "" || row[lineX] == null)
                    continue;

                GameObject go = Resources.Load<GameObject>($"{row[lineX]}"); // todo  Tile_Chapter01_0 -> row[lineX]
                if (go == null)
                    continue;
                if (row[lineX] == "Tile_Chapter01_0")
                    y = 1.55f;
                else
                    y = 0;
                Object.Instantiate(go, new Vector3(x, y, z), Quaternion.identity, parent.transform);
                x += toAdd;
            }
            z += toAdd;
            x = 0;
        }
    }

}
