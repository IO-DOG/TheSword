using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CreateMapEditor : MonoBehaviour
{

#if UNITY_EDITOR
    // % (Ctrl), # (Shift), & (Alt)
    [MenuItem("Tools/CreateMap %#c")]
    private static void CreateMap()
    {
        #region ExcelData
        string[] lines = File.ReadAllText($"{Application.dataPath}/@Resources/Data/Excel/MapData.csv").Split("\n");

        GameObject parent = GameObject.Find("Parent");
        GameObject monsters = GameObject.Find("Monsters");
        GameObject items = GameObject.Find("Items");
        float coX = 0, coY = 0, coZ = 0;
        float toAdd = 1f;
        float addToFloorY = 0.465f;

        for (int y = 0; y < lines.Length; y++)
        {
            string[] row = lines[y].Replace("\r", "").Split(',');

            if (row.Length == 0)
                continue;
            if (string.IsNullOrEmpty(row[0]))
                continue;

            for (int x = 0; x < row.Length; ++x)
            {
                string block = row[x];

                if (block == "0")
                {
                    GameObject voidObject = Resources.Load<GameObject>($"Tilemap_0");
                    UnityEngine.Object.Instantiate(voidObject, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
                }
                else if (block[0] == 'I') // 아이템일 경우
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
                    floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);

                    // TODO 아이템 생성

                }
                else if (block[0] == 'M') // 몬스터일 경우
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
                    floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);

                    // TODO 몬스터 생성
                    GameObject monster = Resources.Load<GameObject>($"Monster");
                    UnityEngine.Object.Instantiate(monster, new Vector3(coX, coY + 1f, coZ), Quaternion.identity, monsters.transform);
                    monster.transform.localScale = new Vector3(1f, 1.4f, 1.4f);
                    monster.GetComponent<MonsterController>().id = block[2] - '0';
                }
                else if (block[0] == 'B') // 보스 몬스터일 경우
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
                    floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);

                    // TODO 보스 몬스터 생성
                }
                else
                {
                    GameObject floor = Resources.Load<GameObject>($"Tilemap_1");
                    UnityEngine.Object.Instantiate(floor, new Vector3(coX, coY + addToFloorY, coZ), Quaternion.identity, parent.transform);
                    floor.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);
                    // TODO 타일 생성
                    if (block != "1")
                    {
                        GameObject tile = Resources.Load<GameObject>($"Tilemap_{block}");
                        UnityEngine.Object.Instantiate(tile, new Vector3(coX, coY, coZ), Quaternion.identity, parent.transform);
                        tile.transform.localScale = new Vector3(0.312f, 0.312f, 0.312f);
                    }
                }
                coX += toAdd;
            }
            coZ += toAdd;
            coX = 0;
        }

        #endregion
    }

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

#endif

}
