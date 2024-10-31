using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class legacy__MapEditor : MonoBehaviour
{
#if UNITY_EDITOR

    // % (Ctrl), # (Shift), & (Alt)
    [MenuItem("Tools/GenerateMap %#g")]
    private static void GenerateMap()
    {
        GameObject go = GameObject.Find("Map");

        Tilemap tm = Util.FindChild<Tilemap>(go, "Tilemap_Collision", true);

        using (var writer = File.CreateText("Assets/@Resources/Map/output.txt"))
        {
            writer.WriteLine(tm.cellBounds.xMin);
            writer.WriteLine(tm.cellBounds.xMax);
            writer.WriteLine(tm.cellBounds.yMin);
            writer.WriteLine(tm.cellBounds.yMax);

            for (int y = tm.cellBounds.yMax; y >= tm.cellBounds.yMin; y--)
            {
                for (int x = tm.cellBounds.xMin; x <= tm.cellBounds.xMax; x++)
                {
                    TileBase tile = tm.GetTile(new Vector3Int(x, y, 0));
                    if (tile != null)
                    {
                        writer.Write($"{tile.name} ");
                    }
                    else
                        writer.Write("0 ");
                }
                writer.WriteLine();
            }
        }

        Debug.Log("Generate Map Finish!");
    }

#endif
}