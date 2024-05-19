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
        SHJTestScene,
        InputTestScene,
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
    }

    public enum Layer
    {
        Wall = 6,
        Key = 7,
        Player = 8,
        Door = 9,
    }

    public enum MoveDir
    {
        None,
        Up,
        Down,
        Left,
        Right,
    }

    public enum ItemType
    {
        Unknown,
        Key,
        Potion,
        Equipment,
    }

    public enum KeyColor
    {
        Red = 0,
        Yellow = 1,
        Green = 2,
    }

    #endregion
}
