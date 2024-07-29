using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialScene : BaseScene
{
    List<GameObject> _directingObjects = new List<GameObject>();
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.TutorialScene;
        Managers.UI.ShowSceneUI<UI_TutorialScene>();

        foreach(Transform child in GameObject.Find("DirectingObjects").transform)
        {
            _directingObjects.Add(child.gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(PlayTutorial_1());
    }

    public override void Clear()
    {

    }

    IEnumerator PlayTutorial_1()
    {
        // Set Player Dir
        Managers.Game.Player.SetState(Define.PlayerState.IdleUp);

        yield return new WaitForSeconds(0.5f);

        Managers.Game.OnDirect = true;

        // Set Camera Position
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetCameraTarget(_directingObjects[0]);

        // Player Movement
        float originalSpeed = Managers.Game.CurPlayerData.MoveSpeed;
        Managers.Game.Player.Speed = 2f;
        Managers.Game.Player.Moving(Define.MoveDir.Up);

        yield return new WaitForSeconds(0.5f);
        Managers.Game.Player.SetState(Define.PlayerState.IdleUp);

        // Show Stage Name
        Managers.UI.ShowPopupUI<UI_StageNamePopup>();

        yield return new WaitForSeconds(Define.STAGE_NAME_DURATION);

        UI_ConversationPopup conversation = Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        conversation._scriptCode = Define.TUTORIAL_SCRIPT;

        // Reset Player Stat
        Managers.Game.Player.Speed = originalSpeed;

        bool prevConvsersationState = Managers.Game.OnConversation;

        while (true) 
        {
            bool currentConversationState = Managers.Game.OnConversation;
            if (prevConvsersationState && !currentConversationState)
            {
                Managers.Game.OnDirect = false;
                Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetCameraTarget(Managers.Game.Player.gameObject);
            }

            prevConvsersationState = currentConversationState;

            yield return null;
        }
    }
}
