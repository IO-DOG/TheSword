using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WallData", menuName = "SaveData/WallData")]
public class WallData : ScriptableObject
{
    public List<GameObject> wallPrefabs = new List<GameObject>();
}
