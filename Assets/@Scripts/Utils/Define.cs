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

    public enum StageName
    {
        None = -1,
        Dungeon_00_000,
        Dungeon_00_001,
        Dungeon_00_002,
        Dungeon_00_003,
    }

    public enum ScreenType
    {
        None = 0,
        Full,
        Window,
        FullWindow,
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

    public enum FadeEvent
    {
        LeftToCenter,
        CenterToRight,
        FadnIn,
        FadeOut,
    }

    public enum Layer
    {
        Default = 0,
        Wall = 6,
        CItem = 7,
        Player = 8,
        Door = 9,
        Monster = 10,
        Portal = 11,
        EItem = 12,
        Lever = 14,
        InteractObjects = 15,
        BossDoor = 16,
    }

    public enum MoveDir
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Back,
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
    public static float TILE_SIZE = 0.32f;
    public static float CAMERA_ANGLE = 60;
    public static int PPU = 100;

    public enum TileType
    {
        ObjectTile = -2,
        VoidTile = -1,
        Void = 0,
        Floor,
        Wall,
        Door,
        Portal,
        SpawnPoint = 11,
        Lever = 12,
        Pillar = 13,
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
        BossRoom = 2,
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
        IdleBack,
        IdleFront,
        IdleLeft,
        IdleRight,
        OnLever,
        DrawSword,
        ContractSword,
        BackStep,
    }

    public enum EventClass
    {
        Start = 0,
        InProgress = 1,
        End = 2,
    }
    #endregion

    #region BossMonsterId
    public const int KingSlime = 5;
    #endregion

    public static string MainUI_Inventory_A = "MainUI_Inventory_A.sprite";
    public static string MainUI_Inventory_B = "MainUI_Inventory_B.sprite";
    public static string MainUI_Option_A = "MainUI_Option_A.sprite";
    public static string MainUI_Option_B = "MainUI_Option_B.sprite";
    public static string MainUI_Sword_A = "MainUI_Sword_A.sprite";
    public static string MainUI_Sword_B = "MainUI_Sword_B.sprite";
    public static string MainUI_Warp_A = "MainUI_Warp_A.sprite";
    public static string MainUI_Warp_B = "MainUI_Warp_B.sprite";

    public static Color BossLight = new Color(1f, 0, 0);

    #region Script Data
    public static int TITLE_MENU = 0;
    public static int INTRO_STORY = 900000;
    public static int STAGE_NAME = 5000;
    public static int PLAYER_DEFAULT_NAME = 6;
    public static int SWORD_DEFAULT_NAME = 4018;
    public static int BOSS_ALERT = 7;
    public static int SWOARD_ALERT = 8;
    public static int TUTORIAL_SCRIPT = 100006;
    public static int STAT_INFO_SCRIPT = 100;
    public static int CONTINUE = 9;
    public static int SETTING = 10;
    public static int LANGUAGE = 11;
    public static int QUIT_GAME = 12;
    public static int SCREEN = 13;
    public static int FULL_SCREEN = 14;
    public static int WINDOW_SCREEN = 15;
    public static int FULL_WINDOW_SCREEN = 16;
    public static int SOUND = 17;
    public static int TOTAL_SOUND = 18;
    public static int BGM = 19;
    public static int EFFECT = 20;
    #endregion

    #region Event ID
    public static int EVENT_TUTORIAL = 0;
    public static int EVENT_SWORD_FIRST = 1;
    #endregion

    public static float FADE_DURATION = 2f;
    public static float STAGE_NAME_DURATION = 1.5f;

    #region EquipDataForInven
    public static int EQUIP_SOWRD_FIRST = 10;
    public static int EQUIP_SOWRD_END = 19;

    #endregion
}
