using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class PortalController : MonoBehaviour
{
    public int _stairs = (int)Define.Stairs.None;

    public void UsePortal()
    {
        if (_stairs == (int)Define.Stairs.Upstairs)
        {
            string nextStageName = Managers.Data.StageInfoDic[Managers.Game.CurPlayerData.CurStageid].UpStage;
            Managers.Game.CurPlayerData.CurStageid = Managers.Data.FindKeyByValue_StageInfoData(nextStageName);

            Vector3 nextPos = GetNextPos(nextStageName);
            Managers.Game.Player.transform.position = nextPos;
        }
        else if (_stairs == (int)Define.Stairs.Downstairs)
        {
            string nextStageName = Managers.Data.StageInfoDic[Managers.Game.CurPlayerData.CurStageid].DownStage;
            Managers.Game.CurPlayerData.CurStageid = Managers.Data.FindKeyByValue_StageInfoData(nextStageName);

            Vector3 nextPos = GetNextPos(nextStageName);
            Managers.Game.Player.transform.position = nextPos;
        }
    }

    Vector3 GetNextPos(string nextStageName)
    {
        List<Data.TileData> tiles = Managers.Data.MapDic["Dungeon_" + nextStageName].Tiles;

        string chapterName = nextStageName.Substring(0, 2);
        int totalCurrentStageCount = Managers.Data.GetChapterCount(chapterName);
        int startStageID = Managers.Data.FindKeyByValue_StageInfoData(chapterName + "_000");
        int nextStageID = Managers.Game.CurPlayerData.CurStageid;
        int endStageID = startStageID + totalCurrentStageCount - 1;

        int offset = 0;
        int index = GetNextPortalTileIndex(nextStageName);

        if (endStageID == nextStageID)
        {
            return Managers.Game.Player.transform.position;
        }
        else
        {
            offset = (nextStageID - startStageID) * 100;
        }

        Vector3 nextPos = new Vector3(tiles[index].Position.X * 0.33f + offset, Managers.Game.Player.transform.position.y, tiles[index].Position.Z * 0.33f);
        return nextPos;
    }

    int GetNextPortalTileIndex(string nextStageName)
    {
        List<Data.TileData> tiles = Managers.Data.MapDic["Dungeon_" + nextStageName].Tiles;
        int index = -1;

        if (_stairs == (int)Define.Stairs.Upstairs)
        {
            index = tiles.FindIndex(tile =>
            {
                if (tile.TileType == (int)Define.TileType.Portal)
                {
                    if (tile is StairsData stairsTile)
                    {
                        return stairsTile.StairsType == (int)Define.Stairs.Downstairs;
                    }
                }
                return false;
            });
        }
        else
        {
            index = tiles.FindIndex(tile =>
            {
                if (tile.TileType == (int)Define.TileType.Portal)
                {
                    if (tile is StairsData stairsTile)
                    {
                        return stairsTile.StairsType == (int)Define.Stairs.Upstairs;
                    }
                }
                return false;
            });
        }

        return index;
    }
}
