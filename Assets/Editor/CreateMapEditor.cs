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
