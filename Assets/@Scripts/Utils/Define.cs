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
        GameScene,
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
        Item = 7,
        Player = 8,
        Door = 9,
        Monster = 10,
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
        ring = 4,
        Shoes = 5,
        Book = 6,
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
}
