using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class DirectingManager
{
    public Action PopupAction;
    public Events Events = new Events();
    public UI_LetterBox letterBox;
    public void PlayDirecting(int eventId)
    {
        switch (eventId)
        {
            case 1:
                Events.CoStartEvent_1();
                PopupAction += (() => Managers.UI.ShowPopupUI<UI_MagicalSwordCheckPopup>());
                break;
        }
    }

    public void PlayLetterBox()
    {
        letterBox = Managers.UI.ShowPopupUI<UI_LetterBox>();
        letterBox.Init();
        letterBox.StartLetterBox();

    }

    public void CloseLetterBox()
    {
        Managers.Directing.letterBox.StopLetterBox();
        Managers.Directing.letterBox = null;
    }
}

public class Events
{
    bool _coroutineCompleted;
    void StartCoPlayEmoji(string EmojiName, UnityEngine.Transform transform)
    {
        _coroutineCompleted = false;
        CoroutineManager.StartCoroutine(PlayEmoji(EmojiName, transform));
    }
    IEnumerator PlayEmoji(string EmojiName, UnityEngine.Transform transform)
    {
        GameObject go = Managers.Resource.Instantiate("Emoji", transform);
        go.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        go.transform.localPosition = new Vector3(0.2f, 0.8f, -0.1f);
        go.GetComponent<Animator>().Play(EmojiName);
        float delay = go.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(delay);
        Managers.Resource.Destroy(go);
        yield return new WaitForSeconds(1f);
        _coroutineCompleted = true;
    }

    #region EVENT_1
    public void CoStartEvent_1()
    {
        CoroutineManager.StartCoroutine(EVENT_1());
    }
    IEnumerator EVENT_1()
    {
        Managers.UI.CloseGameSceneUI();
        Managers.Game.OnDirect = true;
        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);

        #region #1
        {
            StartCoPlayEmoji(Managers.Data.EventDic[Managers.Game.CurEventID].HeroEmoji, Managers.Game.Player.transform);
            yield return new WaitUntil(() => _coroutineCompleted);

            Managers.Game.CurEventID++;
        }
        #endregion
        #region #2
        {
            Managers.Game.CurInteractObject.layer = (int)Define.Layer.Default;
            float originalSpeed = Managers.Game.PlayerData.MoveSpeed;
            Managers.Game.Player.Moving(Define.MoveDir.Up);
            yield return new WaitForSeconds(0.2f);
            Managers.Game.Player.SetState(Define.PlayerState.DrawSword);
            yield return new WaitForSeconds(1f);
            Managers.Game.Player.Moving(Define.MoveDir.Back);
            yield return new WaitForSeconds(0.2f);
            Managers.Game.Player.SetState(Define.PlayerState.IdleBack);
            yield return new WaitForSeconds(1f);

            StartCoPlayEmoji(Managers.Data.EventDic[Managers.Game.CurEventID].HeroEmoji, Managers.Game.Player.transform);
            yield return new WaitUntil(() => _coroutineCompleted);

            Managers.Game.CurEventID++;
        }
        #endregion
        #region #3
        {
            Managers.Game.CurInteractObject.layer = (int)Define.Layer.InteractObjects;

            StartCoPlayEmoji(Managers.Data.EventDic[Managers.Game.CurEventID].OtherEmoji, Managers.Game.CurInteractObject.transform);
            yield return new WaitUntil(() => _coroutineCompleted);

            Managers.Game.CurEventID++;
        }
        #endregion
        #region #4
        {
            StartCoPlayEmoji(Managers.Data.EventDic[Managers.Game.CurEventID].HeroEmoji, Managers.Game.Player.transform);
            yield return new WaitUntil(() => _coroutineCompleted);

            Managers.Game.CurEventID++;
        }
        #endregion
        Managers.Game.OnDirect = false;
        Managers.UI.CloseGameSceneUI();
        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
    }
    #endregion

    #region Contract Sword
    public void CoStartContractSword()
    {
        PlayerPrefs.SetInt("ISMEETSWORD", 1);
        CoroutineManager.StartCoroutine(ContractSword());
    }

    IEnumerator ContractSword()
    {
        Managers.Game.OnDirect = true;

        Managers.Game.DirectionalLight.DOIntensity(0.05f, 0.5f);

        Managers.Game.Player.SetState(Define.PlayerState.ContractSword);

        Vector3 swordPos = Managers.Game.CurInteractObject.transform.position;
        Managers.Game.CurInteractObject.transform.gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        GameObject go1 = Managers.Resource.Instantiate("FX_ContractSwordEffect", Managers.Game.Player.transform);
        go1.transform.localPosition = Vector3.zero;
        go1.transform.localScale = new Vector3(0.3f, 0.3f, 0.15f);

        GameObject go2 = Managers.Resource.Instantiate("FX_PowerWave", Managers.Game.Player.transform);
        go2.transform.localPosition = Vector3.zero;
        go2.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);

        yield return new WaitForSeconds(4f);

        Managers.Game.DirectionalLight.DOIntensity(1f, 1f);

        Managers.Resource.Destroy(go1);
        Managers.Resource.Destroy(go2);

        GameObject key = Managers.Resource.Instantiate("ConsumableItem");
        key.transform.position = swordPos;
        key.transform.localScale = new Vector3(1f, 2f, 1f);
        key.GetComponent<ConsumableItem>().id = 1;

        yield return new WaitForSeconds(1.5f);

        Managers.Game.PlayerData.IsContractedSword = true;
        Managers.Game.Player.SetState(Define.PlayerState.IdleFront);
        Managers.Game.Player._moveDir = Define.MoveDir.Down;
        Managers.Game.Player._isEquiptWeapon = true;
        Managers.Game.Player._isEquiptShield = true;
        Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST + 1;
        Managers.Game.OnDirect = false;
        Managers.UI.OpenGameSceneUI();
        Managers.Game.SaveGame();
    }

    #endregion

    #region KingSlimeDirecting
    public GameObject _kingSlime;

    bool _clearKingSlime = false;
    public Action OnMeetKingSlime = null;

    public void MeetKingSlime()
    {
        if (Managers.Game.OnMeetKingSlime)
            return;
        if (_clearKingSlime == false)
        {
            _clearKingSlime = true;
            _kingSlime = GameObject.Find("bossMonster0");
            if (_kingSlime != null)
                _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = false;
        }

        if (Managers.Game.Player.gameObject != null && Managers.Game.Player.gameObject.transform.position.x < 303.5f || Managers.Game.Player.gameObject.transform.position.x > 304.2f
            || Managers.Game.Player.gameObject.transform.position.z < -7f) // 하드코딩. 일단 놔두자.
            return;

        Managers.Game.OnMeetKingSlime = true;
        // 주인공을 길 중간 위치로 이동
        // 주인공이 정면을 바라보도록
        // 카메라 워킹 및 UI사라짐
        // 카메라 흔들림 등의 연출 효과
        // 연출이 끝나면 UI 활성화
        GameObject gameScene = Managers.Game.GameScene.gameObject;
        if (gameScene != null)
        {
            Managers.UI.CloseGameSceneUI();
            Managers.Directing.PlayLetterBox();

            Managers.Game.Player.SetIdleState(Managers.Game.Player._moveDir);
            Managers.Game.OnStaticResolution = true;
            Managers.Game.OnDirect = true;

            Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
            Vector3 target = new Vector3(0f, 20f, -5f); ;
            float moveTime = 2f;

            GameObject parent = GameObject.Find("Dungeon_00_003");
            GameObject scoutSlime = Managers.Resource.Instantiate("BossScene_C0_000", parent.transform);
            Vector3 pos = new Vector3(3.845f, 1.47f, -1.408f);

            scoutSlime.transform.localPosition = pos;

            OnMeetKingSlime -= KingSlimeAction;
            OnMeetKingSlime += KingSlimeAction;

            CoroutineManager.StartCoroutine(CoVirtualCameraMove(original, target, moveTime));
        }
    }

    void KingSlimeAction()
    {
        OnMeetKingSlime = null;
        CoroutineManager.StartCoroutine(CoKingSlimeAction());
    }

    IEnumerator CoKingSlimeAction()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(1f);
        GameObject parent = GameObject.Find("Dungeon_00_003");
        GameObject midlePos = GameObject.Find("SpawnKingSlime");
        Vector3 pos = new Vector3(3.845f, 1.47f, -1.408f);
        GameObject scoutSlime = GameObject.Find("BossScene_C0_000");
        Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(0f, 20f, -5f);
        scoutSlime.transform.localPosition = pos;


        Vector3 scoutSlimeMoveDest = new Vector3(pos.x, pos.y, pos.z - 0.3f);
        CoroutineManager.StartCoroutine(CoMoveToDest(scoutSlime, scoutSlimeMoveDest, 2.5f));


        yield return new WaitForSeconds(2.5f);

        scoutSlime.GetComponent<Animator>().Play("bossScene_C0_001");

        yield return new WaitForSeconds(1f);

        scoutSlime.transform.DOLocalMoveZ(-0.3f, 1f);


        yield return new WaitForSeconds(0.5f);

        // camera slow down
        Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target = new Vector3(0f, 14.5f, -6f); ;
        float moveTime = 3f;
        CoroutineManager.StartCoroutine(CoVirtualCameraMove(original, target, moveTime));

        yield return new WaitForSeconds(0.5f);

        GameObject.Find("SlimeFall4").GetComponent<ParticleSystem>().Play();
        //yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 1.5f));
        GameObject.Find("SlimeFall2").GetComponent<ParticleSystem>().Play();
        //yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 1.5f));
        GameObject.Find("SlimeFall3").GetComponent<ParticleSystem>().Play();
        //yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1f));
        GameObject.Find("SlimeFall1").GetComponent<ParticleSystem>().Play();
        //yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.5f));
        GameObject.Find("SlimeFall5").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(0.5f);

        GameObject slimesPos = GameObject.Find("SlimesPos");
        GameObject slimes = Managers.Resource.Instantiate("Slimes", slimesPos.transform);
        GameObject slimesCore = Managers.Resource.Instantiate("SlimesCore", slimesPos.transform);
        slimesCore.transform.DOScale(Vector3.one * 2f, 1f);

        _kingSlime = GameObject.Find("bossMonster0");
        GameObject kingSlimeActionFront = GameObject.Find("KingSlimeActionFront");
        kingSlimeActionFront.GetComponent<Animator>().Play("NewKingSlimeActionFront");
        GameObject kingSlimeActionBack = GameObject.Find("KingSlimeActionBack");
        kingSlimeActionBack.GetComponent<Animator>().Play("NewKingSlimeActionBack");

        {
            WaitForSeconds delay = new WaitForSeconds(0.4f);
            yield return delay;
            GameObject.Find("SlimeFall1").GetComponent<ParticleSystem>().Stop();
            yield return delay;
            GameObject.Find("SlimeFall2").GetComponent<ParticleSystem>().Stop();
            yield return delay;
            GameObject.Find("SlimeFall3").GetComponent<ParticleSystem>().Stop();
            yield return delay;
            GameObject.Find("SlimeFall4").GetComponent<ParticleSystem>().Stop();
            yield return delay;
            GameObject.Find("SlimeFall5").GetComponent<ParticleSystem>().Stop();
        }

        //kingSlimeAction.transform.DOScaleY(2f, 0.5f).OnComplete(()=>
        //{
        //    kingSlimeAction.transform.DOScaleY(1f, 0.5f);
        //});

        slimes.GetComponent<ParticleSystem>().Stop();
        slimesCore.GetComponent<ParticleSystem>().Stop();
        yield return new WaitForSeconds(1f);

        // flash bang
        CoroutineManager.StartCoroutine(CoFlashBang());
        CoroutineManager.StartCoroutine(CoShakeCamera());

        yield return new WaitForSeconds(0.9f);
        Managers.Resource.Instantiate("Stones", GameObject.Find("Actions").transform);

        kingSlimeActionFront.SetActive(false);
        kingSlimeActionBack.SetActive(false);
        _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = true;
        GameObject.Find("Effects_00")?.SetActive(false);

        yield return new WaitForSeconds(0.5f);
        CoroutineManager.StartCoroutine(AfterMeetKingSlime());
    }

    public IEnumerator CoShakeCamera()
    {
        yield return new WaitForSeconds(0.8f);
        var noise = Managers.Game.MainCamera.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        noise.m_NoiseProfile = Managers.Resource.Load<NoiseSettings>("6D Shake");
        noise.enabled = true;
        noise.m_AmplitudeGain = 5f;

        yield return new WaitForSeconds(0.5f);
        noise.enabled = false;
    }

    public IEnumerator CoFlashBang()
    {
        yield return new WaitForSeconds(0.3f);
        GameObject go = GameObject.Find("Directional Light");
        Light light = go.GetComponent<Light>();
        float start = light.intensity;
        light.DOIntensity(0, 0.5f);
        yield return new WaitForSeconds(0.5f);
        light.DOIntensity(50f, 0.1f);
        yield return new WaitForSeconds(0.15f);
        light.DOIntensity(start, 0.5f);
        light.color = new Color(1, 244 / 255f, 214 / 255f, 1);
    }

    public IEnumerator CoBright(GameObject go, float time)
    {
        yield return null;

        float totalTime = 0f;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        while (totalTime <= time)
        {
            float delta = totalTime / time;
            sr.color = new Color(1, 1, 1, delta);
            totalTime += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator CoBlack(GameObject go, float time)
    {
        yield return null;

        float totalTime = 0f;
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        while (totalTime <= time)
        {
            float delta = totalTime / time;
            sr.color = new Color(1, 1, 1, 1 - delta);
            totalTime += Time.deltaTime;
            yield return null;
        }

        sr.color = new Color(1, 1, 1, 0);
    }

    public IEnumerator CoMoveToKingSlimeMidlePos(GameObject original, Vector3 target, float time)
    {
        yield return null;

        float totalTime = 0f;

        float originalX = original.transform.localPosition.x;
        float originalY = original.transform.localPosition.y;
        float originalZ = original.transform.localPosition.z;

        while (totalTime <= time)
        {
            float delta = totalTime / time;
            float x = originalX + (target.x - originalX) * delta;
            float y = originalY + (target.y - originalY) * delta;
            float z = originalZ + (target.z - originalZ) * delta;
            original.transform.localPosition = new Vector3(x, original.transform.position.y, z);
            totalTime += Time.deltaTime;
            yield return null;
        }

        original.SetActive(false);
    }

    public IEnumerator CoMoveToDest(GameObject original, Vector3 target, float time)
    {
        yield return null;

        float totalTime = 0f;

        float originalX = original.transform.localPosition.x;
        float originalY = original.transform.localPosition.y;
        float originalZ = original.transform.localPosition.z;

        while (totalTime <= time)
        {
            float delta = totalTime / time;
            float x = originalX + (target.x - originalX) * delta;
            float y = originalY + (target.y - originalY) * delta;
            float z = originalZ + (target.z - originalZ) * delta;
            original.transform.localPosition = new Vector3(x, original.transform.position.y, z);
            totalTime += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator CoVirtualCameraMove(Vector3 original, Vector3 target, float time)
    {
        yield return null;

        float totalTime = 0f;

        while (totalTime <= time)
        {
            float delta = totalTime / time;
            float x = original.x + (target.x - original.x) * delta;
            float y = original.y + (target.y - original.y) * delta;
            float z = original.z + (target.z - original.z) * delta;
            Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(x, y, z);
            totalTime += Time.deltaTime;
            yield return null;
        }

        if (OnMeetKingSlime != null)
            OnMeetKingSlime?.Invoke();
    }

    public IEnumerator AfterMeetKingSlime()
    {
        Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target = new Vector3(0f, 10f, -5f); ;
        float moveTime = 2f;
        CoroutineManager.StartCoroutine(CoVirtualCameraMove(original, target, moveTime));

        if (_kingSlime != null)
        {
            _kingSlime.transform.localScale = new Vector3(1f, 2f, 1f);
            _kingSlime.transform.localPosition = new Vector3(3.84f, 3f, -6f);
            _kingSlime.SetActive(true);
            _kingSlime.gameObject.GetOrAddComponent<Animator>().Play("Boss_C0_I000");
        }

        yield return new WaitForSeconds(2f);
        Managers.Game.OnStaticResolution = false;
        Managers.Game.OnDirect = false;
        Managers.Directing.CloseLetterBox();
        Managers.UI.OpenGameSceneUI();
        Managers.Game.OnKingSlimeDeadAction -= Unlock4Floor;
        Managers.Game.OnKingSlimeDeadAction += Unlock4Floor;
    }

    public void Unlock4Floor()
    {
        // todo
        // path particle
        // open 4floor
        GameObject parent = GameObject.Find("Dungeon_00_003");
        GameObject go = Managers.Resource.Instantiate("FX_BossClearLine", parent.transform);
        go.transform.localPosition = new Vector3(3.83f, 0.66f, -3.75f);
        go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

    }

    #endregion

    #region Tutorial
    public void CoPlayTutorial_1()
    {
        CoroutineManager.StartCoroutine(PlayTutorial_1());
        Managers.Sound.Play(Define.Sound.Bgm, "Chapter0_BGM");
        GameObject.Find("UI_PlayerHPBar").GetComponent<Image>().color = new Color(1, 1, 1, 0);
        GameObject.Find("PlayerHPBarGauge").GetComponent<Image>().color = new Color(1, 1, 1, 0);
        Managers.Game.Player._isEquiptWeapon = false;
        Managers.Game.Player._weapon.SetActive(false);
        Managers.Game.SaveGame();
    }

    IEnumerator PlayTutorial_1()
    {
        Managers.Game.CurEventID = 0;
        // Set Player Dir
        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);

        yield return new WaitForSeconds(0.5f);

        Managers.Game.OnDirect = true;

        // Player Movement
        float originalSpeed = Managers.Game.PlayerData.MoveSpeed;
        Managers.Game.Player.Speed = 1f;
        Managers.Game.Player.Moving(Define.MoveDir.Up);

        yield return new WaitForSeconds(0.5f);
        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);

        yield return new WaitForSeconds(Define.STAGE_NAME_DURATION * 2.2f);

        Managers.Game.OnDirect = false;

        Managers.UI.CloseGameSceneUI();
        UI_ConversationPopup conversation = Managers.UI.ShowPopupUI<UI_ConversationPopup>();

        // Reset Player Stat
        Managers.Game.Player.Speed = originalSpeed;

        #region 테스트 후 다시 활성화해야 함
        bool prevConvsersationState = Managers.Game.OnConversation;

        while (true)
        {
            bool currentConversationState = Managers.Game.OnConversation;
            if (prevConvsersationState && !currentConversationState)
            {
                Managers.Game.OnDirect = false;
                break;
            }

            prevConvsersationState = currentConversationState;

            yield return null;
        }
        #endregion

        Managers.Game.Player._isEquiptWeapon = true;
        Managers.Game.Player._weapon.SetActive(true);

        PlayerPrefs.SetInt("ISFIRST", 0);
        //Managers.Game.SaveGame();
    }
    #endregion
}
