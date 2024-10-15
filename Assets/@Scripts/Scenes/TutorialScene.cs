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

        Managers.Game.DirectionalLight = GameObject.Find("Directional Light").GetComponent<Light>();

        foreach(Transform child in GameObject.Find("DirectingObjects").transform)
        {
            _directingObjects.Add(child.gameObject);
        }

        Managers.Game.CurPlayerData.CurSword = Define.EQUIP_SOWRD_FIRST;
        Managers.Game.CurPlayerData.CurShield = 0;
    }

    private void Start()
    {
        if(PlayerPrefs.GetInt("ISFIRST", 1) == 1)
            StartCoroutine(PlayTutorial_1());
    }

    public override void Clear()
    {

    }

    IEnumerator PlayTutorial_1()
    {
        // Set Player Dir
        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);

        yield return new WaitForSeconds(0.5f);

        Managers.Game.OnDirect = true;

        // Set Camera Position
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetCameraTarget(Managers.Game.Player.gameObject);
        //Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetCameraTarget(_directingObjects[0]);

        // Player Movement
        float originalSpeed = Managers.Game.CurPlayerData.MoveSpeed;
        Managers.Game.Player.Speed = 2f;
        Managers.Game.Player.Moving(Define.MoveDir.Up);

        yield return new WaitForSeconds(0.5f);
        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);

        // Show Stage Name
        Managers.UI.ShowPopupUI<UI_StageNamePopup>();

        yield return new WaitForSeconds(Define.STAGE_NAME_DURATION * 2.2f);

        Managers.Game.OnDirect = false;

        //UI_ConversationPopup conversation = Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        //conversation._eventID = Define.EVENT_TUTORIAL;

        // Reset Player Stat
        Managers.Game.Player.Speed = originalSpeed;

        #region 테스트 후 다시 활성화해야 함
        //bool prevConvsersationState = Managers.Game.OnConversation;

        //while (true) 
        //{
        //    bool currentConversationState = Managers.Game.OnConversation;
        //    if (prevConvsersationState && !currentConversationState)
        //    {
        //        Managers.Game.OnDirect = false;
        //        //Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetCameraTarget(Managers.Game.Player.gameObject);
        //    }

        //    prevConvsersationState = currentConversationState;

        //    yield return null;
        //}
        #endregion

        PlayerPrefs.SetInt("ISFIRST", 0);
        Managers.Game.SaveGame();
    }
}
