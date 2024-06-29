using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    #region Enum

    public enum Scene
    {
        Unknown,
        TitleScene,
        IntroScene,
        GameScene,
        TutorialScene,
    }

    public enum Sound
    {
        Bgm,
        SubBgm,
        Effect,
        Max,
    }

    public enum UIEvent
    {
        Click,
        Preseed,
        PointerDown,
        PointerUp,
        BeginDrag,
        Drag,
        EndDrag,
        PointerEnter,
        PointerExit,
    }

    public enum Layer
    {
        Wall = 6,
        CItem = 7,
        Player = 8,
        Door = 9,
        Monster = 10,
        Portal = 11,
        EItem = 12,
        Lever = 14,
    }

    public enum MoveDir
    {
        None,
        Up,
        Down,
        Left,
        Right,
    }

    public enum Types
    {
        None = 0,
        Sword = 1,
        Shield = 2,
        Necklace = 3,
        Ring = 4,
        Shoes = 5,
        Book = 6,
    }
    
    public enum ScriptType
    {
        None = 0,
        Kr = 1,
        En = 2,
        Jp = 3,
        Cn = 4,
    }

    public enum DungeonType
    {
        Common,
        Special,
        Boss,
    }
    #endregion

    #region Map
    public static float TILE_SIZE = 3.2f;
    public static float CAMERA_ANGLE = 60;

    public enum TileType
    {
        Void = 0,
        Floor,
        Wall,
        Door,
        Stairs,
        SpawnPoint = 11,
    }

    public enum OccupiedType
    {
        None = 0,
        Monster,
        CItem,
        EItem,
        Boss,
    }

    public enum Stairs
    {
        None = -1,
        Upstairs = 0,
        Downstairs = 1,
    }

    public enum DecoType
    {
        Torch = 0,
        FireBowl = 1,
        GodRay = 2,
        PointLight = 3,
        Handcuff = 4,
    }

    public enum PlayerState
    {
        Left,
        Right,
        Up,
        Down,
        IdleUp,
        IdleDown,
        OnLever,
    }
    #endregion

    public static string MainUI_Inventory_A = "MainUI_Inventory_A.sprite";
    public static string MainUI_Inventory_B = "MainUI_Inventory_B.sprite";
    public static string MainUI_Option_A = "MainUI_Option_A.sprite";
    public static string MainUI_Option_B = "MainUI_Option_B.sprite";
    public static string MainUI_Sword_A = "MainUI_Sword_A.sprite";
    public static string MainUI_Sword_B = "MainUI_Sword_B.sprite";
    public static string MainUI_Warp_A = "MainUI_Warp_A.sprite";
    public static string MainUI_Warp_B = "MainUI_Warp_B.sprite";

    #region Script Data
    public static int TITLE_MENU = 0;
    public static int INTRO_STORY = 10000;
    public static int STAGE_NAME = 30000;
    public static int PLAYER_DEFAULT_NAME = 10;
    public static int TUTORIAL_SCRIPT = 11000;

    #endregion
}
