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

    #endregion
}
