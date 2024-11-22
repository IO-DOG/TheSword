using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScene : BaseScene
{
    List<GameObject> _directingObjects = new List<GameObject>();
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.GameScene;
        Managers.Game.GameScene = Managers.UI.ShowSceneUI<UI_GameScene>();

        Managers.Game.DirectionalLight = GameObject.Find("Directional Light").GetComponent<Light>();

        foreach(Transform child in GameObject.Find("DirectingObjects").transform)
        {
            _directingObjects.Add(child.gameObject);
        }

        Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST;
        Managers.Game.PlayerData.CurShield = 0;
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
            Managers.Directing.Events.CoPlayTutorial_1();
    }

    public override void Clear()
    {

    }

}
