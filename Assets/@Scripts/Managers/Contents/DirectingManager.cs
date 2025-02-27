using Cinemachine;
using DG.Tweening;
using Febucci.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Define;
using static UnityEngine.UI.Image;

public class DirectingManager
{
    public Action BossOnAppearAction;
    public Action BossOnDeadAction;
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

public class Events : MonoBehaviour
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

        // Sound
        Managers.Sound.FadeAndPlayBGM("EgoSword_Encounter_Event", 0.8f);

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
            Managers.Game.Player.Moving(Define.MoveDir.Up, true);
            yield return new WaitForSeconds(0.2f);
            Managers.Game.Player.SetState(Define.PlayerState.DrawSword);
            yield return new WaitForSeconds(1f);
            Managers.Game.Player.Moving(Define.MoveDir.Back, true);
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

        yield return new WaitForSeconds(3f);

        GameObject fireflies = GameObject.Find("MagicalSwordRoomFireflies");
        if (fireflies != null)
        {
            fireflies.SetActive(false);
        }

        GameObject godray = GameObject.Find("MagicalSwordRoomGodray");
        if (godray != null)
        {
            godray.GetComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>("Godray3");
        }

        Volume postProcessingVolume = Managers.Game.MainCamera.GetComponent<Volume>();
        ColorAdjustments colorAdjustments;

        if (postProcessingVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments.colorFilter.Override(new Color(255 / 255f, 231 / 255f, 206 / 255f));
        }

        Managers.Game.DirectionalLight.color = new Color(255 / 255f, 244 / 255f, 214 / 255f);
        // Sound

        Managers.Game.DirectionalLight.DOIntensity(1.5f, 1f);

        Managers.Resource.Destroy(go1);
        Managers.Resource.Destroy(go2);

        yield return new WaitForSeconds(1.5f);

        Managers.Game.PlayerData.IsContractedSword = true;
        Managers.Game.Player.SetState(Define.PlayerState.IdleFront);
        Managers.Game.Player._moveDir = Define.MoveDir.Down;
        Managers.Game.Player._isEquiptWeapon = true;
        Managers.Game.Player._isEquiptShield = true;

        // 인벤토리에 마검 추가
        Managers.Game.PlayerData.Inventory[(int)Define.Types.Sword].Add(10);

        // 현재 검 변경
        Managers.Game.SwapEquip(Define.EQUIP_SOWRD_FIRST + 1);

        //Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST + 1;
        Managers.Game.OnDirect = false;
        Managers.Game.GameScene?.OnUIInventory();
        Managers.UI.ShowGameSceneUI();
        Managers.Game.SaveGame();

        GameObject curMap = GameObject.Find("Dungeon_00_002");
        GameObject key = curMap.transform.Find("Items/CItem13").gameObject;
        key.GetComponent<SpriteRenderer>().enabled = true;
        key.GetComponent<BoxCollider>().enabled = true;
        key.SetActive(true);
        Managers.Sound.FadeAndPlayBGM("Chapter0_BGM", 0.8f);
    }

    #endregion

    #region KingSlimeDirecting
    public GameObject _kingSlime;

    bool _clearKingSlime = false;

    public void MeetKingSlime()
    {
        if (_clearKingSlime == false)
        {
            _clearKingSlime = true;
            _kingSlime = GameObject.Find("bossMonster0");

            if (_kingSlime != null)
            {
                _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = false;
                _kingSlime.GetOrAddComponent<BoxCollider>().enabled = false;
            }
        }

        CoroutineManager.StartCoroutine(CoKingSlimeAction());
    }

    IEnumerator CoKingSlimeAction()
    {
        Managers.Game.OnDirect = true;
        yield return new WaitForSeconds(0.4f);
        Managers.Game.OnStaticResolution = true;
        Managers.UI.CloseGameSceneUI();
        Managers.Directing.PlayLetterBox();

        Managers.Game.Player.SetIdleState(Define.MoveDir.Up);

        Vector3 original0 = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target0 = new Vector3(0f, 20f, -5f); ;
        float moveTime0 = 2f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().StartCoVirtualCameraMove(original0, target0, moveTime0);

        GameObject parent0 = GameObject.Find("Dungeon_00_003");
        GameObject scoutSlime0 = Managers.Resource.Instantiate("BossScene_C0_000", parent0.transform);
        Vector3 pos0 = new Vector3(3.845f, 1.47f, -1.408f);

        scoutSlime0.transform.localPosition = pos0;

        Managers.Sound.FadeAndPlayBGM("Chapter0_Boss_Event", 0.5f);
  
        yield return new WaitForSeconds(1.75f);
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
        Vector3 target = new Vector3(0f, 14.5f, -5f); ;
        float moveTime = 3f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().StartCoVirtualCameraMove(original, target, moveTime);
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

        slimes.GetComponent<ParticleSystem>().Stop();
        slimesCore.GetComponent<ParticleSystem>().Stop();
        yield return new WaitForSeconds(2.1f);
        CoroutineManager.StartCoroutine(CameraController.WhiteBang(0.1f));
        Managers.Resource.Destroy(kingSlimeActionFront);

        _kingSlime.transform.localPosition = new Vector3(3.84f, 3f, -5.5f);
        _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = true;
        _kingSlime.GetOrAddComponent<BoxCollider>().enabled = true;

        _kingSlime.GetComponent<Animator>().speed = 0f;
        _kingSlime.transform.DOLocalMoveZ(_kingSlime.transform.localPosition.z - 0.5f, 0.2f);
        _kingSlime.transform.DOScaleY(0.5f, 0.15f);
        _kingSlime.GetComponent<KingSlimeController>()._sr.material = Managers.Resource.Load<Material>("PaintWhiteMat");
        _kingSlime.GetComponent<KingSlimeController>()._sr.color = Color.white;
        yield return new WaitForSeconds(0.15f);

        _kingSlime.GetComponent<Animator>().speed = 1f;
        _kingSlime.transform.DOLocalMoveZ(_kingSlime.transform.localPosition.z + 0.5f, 0.1f);
        _kingSlime.transform.DOScaleY(2f, 0.15f);

        _kingSlime.GetComponent<KingSlimeController>()._sr.material = Managers.Resource.Load<Material>("HalfSpriteShadow");
        Managers.Resource.Instantiate("KingSlimeInstantiateEffect", GameObject.Find("Actions").transform);
        CoroutineManager.StartCoroutine(CameraController.CoShakeCamera(0.7f, 0.7f));

        //kingSlimeActionFront.SetActive(false);
        kingSlimeActionBack.SetActive(false);
        //_kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = true;
        GameObject.Find("Effects_00")?.SetActive(false);
        CoroutineManager.StartCoroutine(AfterMeetKingSlime());
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

    public IEnumerator AfterMeetKingSlime()
    {
        if (_kingSlime != null)
        {
            _kingSlime.transform.localScale = new Vector3(1f, 2f, 1f);
            _kingSlime.transform.localPosition = new Vector3(3.84f, 3f, -5.5f);
            _kingSlime.SetActive(true);
            //if (_kingSlime != null)
            //    //_kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = true;
            //_kingSlime.gameObject.GetOrAddComponent<Animator>().Play("Boss_C0_I000");
        }

        Managers.UI.ShowBossNamePopup(1.5f);

        yield return new WaitForSeconds(2f);

        Vector3 original = CameraController._transposer.m_FollowOffset;
        Vector3 target = new Vector3(0f, 10f, -5f); ;
        float moveTime = 2f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().StartCoVirtualCameraMove(original, target, moveTime);
        yield return new WaitForSeconds(1f);
        Managers.Directing.CloseLetterBox();
        yield return new WaitForSeconds(1f);
        Managers.Game.OnStaticResolution = false;
        Managers.Game.OnDirect = false;
        Managers.UI.ShowGameSceneUI();
        Managers.Sound.FadeAndPlayBGM("Chapter0_Boss_BGM", 1f);
    }

    public void CoStartUnLock4Floor()
    {
        CoroutineManager.StartCoroutine(Unlock4Floor());
    }

    IEnumerator Unlock4Floor()
    {
        Managers.Game.Portals[Managers.Game.Portals.Length - 1].transform.parent.gameObject.SetActive(true);
        yield return new WaitForSeconds(10f);
        // todo
        // path particle
        // open 4floor
        GameObject parent = GameObject.Find("Dungeon_00_003");
        GameObject go = Managers.Resource.Instantiate("FX_BossClearLine", parent.transform);
        go.transform.localPosition = new Vector3(3.83f, 0.033f, -3.032f);
        go.transform.localScale = new Vector3(0.3f, 0.4f, 0.4f);
    }

    #endregion

    #region KingSlimeDead
    public void CoStartKingSlimeDead()
    {
        if(!Managers.Game.IsPlayerDead)
            CoroutineManager.StartCoroutine(StartKingSlimeDead());
    }

    IEnumerator StartKingSlimeDead()
    {
        yield return new WaitForSeconds(0.4f);
        Managers.UI.CloseGameSceneUI();
        Managers.Directing.PlayLetterBox();
        Managers.Game.OnDirect = true;
        GameObject greenSmoke = GameObject.Find("SmokeFlatWhiteGreen");
        greenSmoke.GetComponent<ParticleSystem>().Stop();

        yield return new WaitForSeconds(2f);

        #region Slime orbs event
        Transform orbsSpawnPos = GameObject.Find("OrbsSpawnPos").transform;
        GameObject slimeOrb = Managers.Resource.Instantiate("SlimeOrb", orbsSpawnPos);
        //slimeOrb.transform.position = new Vector3(kingSlime.transform.position.x, kingSlime.transform.position.y, kingSlime.transform.position.z);
        Managers.Sound.Play(Define.Sound.Effect, "Chapter0_Boss_Event2");
        yield return new WaitForSeconds(0.5f);
        Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target = new Vector3(0f, 18f, -5f); ;
        float moveTime = 1f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().StartCoVirtualCameraMove(original, target, moveTime);
        slimeOrb.transform.DOLocalMoveZ(2f, 1f);

        yield return new WaitForSeconds(1f);

        slimeOrb.transform.GetChild(0).DOLocalMoveX(-2.24f, 0.5f);
        slimeOrb.transform.GetChild(2).DOLocalMoveX(2.24f, 0.5f);
        yield return new WaitForSeconds(0.5f);

        slimeOrb.transform.GetChild(0).DOLocalMoveZ(-1f, 0.25f);
        slimeOrb.transform.GetChild(2).DOLocalMoveZ(-1f, 0.25f);
        yield return new WaitForSeconds(0.5f);

        Vector3 original2 = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target2 = new Vector3(0f, 16f, -7f); ;
        float moveTime2 = 0.5f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().StartCoVirtualCameraMove(original2, target2, moveTime2);

        yield return new WaitForSeconds(0.1f);

        Sequence seq = DOTween.Sequence();

        // Yellow Down
        Sequence yellow = DOTween.Sequence();
        yellow.Append(slimeOrb.transform.GetChild(0).DOLocalMoveZ(-3f, 0.5f));
        yellow.Append(slimeOrb.transform.GetChild(0).DOScale(5f, 0.5f));

        // Red Down
        Sequence red = DOTween.Sequence();
        red.Append(slimeOrb.transform.GetChild(1).DOLocalMoveZ(-1.8f, 0.5f));
        red.Append(slimeOrb.transform.GetChild(1).DOScale(5f, 0.5f));

        // Blue Down
        Sequence blue = DOTween.Sequence();
        blue.Append(slimeOrb.transform.GetChild(2).DOLocalMoveZ(-3f, 0.5f));
        blue.Append(slimeOrb.transform.GetChild(2).DOScale(5f, 0.5f));


        seq.Append(yellow).Join(red).Join(blue).Play().OnComplete(() =>
        {
            Managers.Resource.Destroy(slimeOrb);
        });

        yield return new WaitForSeconds(0.6f);
        #endregion

        #region FlashBang Effect
        // FlashBang Effect

        float whiteTime = 0.5f;
        float defaultTime = 0.2f;
        CoroutineManager.StartCoroutine(CameraController.CoExposure(whiteTime, CameraController.Exposure.White));
        yield return new WaitForSeconds(whiteTime);

        CoroutineManager.StartCoroutine(CameraController.CoExposure(defaultTime, CameraController.Exposure.Default));
        yield return new WaitForSeconds(defaultTime);
        #endregion

        #region Instantiate 3 Slimes

        GameObject map = GameObject.Find("Dungeon_00_003");

        GameObject yellowSlime = Managers.Resource.Instantiate("BossMonster_3Slimes", map.transform);
        yellowSlime.transform.position = GameObject.Find("YellowSlimePos").transform.position;
        yellowSlime.GetComponent<MonsterController>().id = 7;
        GameObject jumpCloud0 = Managers.Resource.Instantiate("JumpCloud", yellowSlime.transform);
        jumpCloud0.transform.localScale = new Vector3(jumpCloud0.transform.localScale.x * 1.5f, jumpCloud0.transform.localScale.y * 1.5f, jumpCloud0.transform.localScale.z * 1.5f);
        GameObject explosion0 = Managers.Resource.Instantiate("PoisonExplosionYellow", yellowSlime.transform);
        GameObject smoke0 = Managers.Resource.Instantiate("SmokeFlatBlack", yellowSlime.transform);
        smoke0.transform.localPosition = new Vector3(0f, -0.8f, 0.5f);
        smoke0.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        CoroutineManager.StartCoroutine(CameraController.CoShakeCamera(0.3f, 0.4f));
        Managers.Resource.Instantiate("Stones", map.transform);

        yield return new WaitForSeconds(0.2f);

        GameObject redSlime = Managers.Resource.Instantiate("BossMonster_3Slimes", map.transform);
        redSlime.transform.position = GameObject.Find("RedSlimePos").transform.position;
        redSlime.GetComponent<MonsterController>().id = 6;
        GameObject jumpCloud1 = Managers.Resource.Instantiate("JumpCloud", redSlime.transform);
        jumpCloud1.transform.localScale = new Vector3(jumpCloud0.transform.localScale.x * 1.5f, jumpCloud0.transform.localScale.y * 1.5f, jumpCloud0.transform.localScale.z * 1.5f);
        GameObject explosion1 = Managers.Resource.Instantiate("PoisonExplosionRed", redSlime.transform);
        GameObject smoke1 = Managers.Resource.Instantiate("SmokeFlatBlack", redSlime.transform);
        smoke1.transform.localPosition = new Vector3(0f, -0.8f, 0.5f);
        smoke1.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        CoroutineManager.StartCoroutine(CameraController.CoShakeCamera(0.3f, 0.4f));
        Managers.Resource.Instantiate("Stones", map.transform);

        yield return new WaitForSeconds(0.1f);

        GameObject blueSlime = Managers.Resource.Instantiate("BossMonster_3Slimes", map.transform);
        blueSlime.transform.position = GameObject.Find("BlueSlimePos").transform.position;
        blueSlime.GetComponent<MonsterController>().id = 8;
        GameObject jumpCloud2 = Managers.Resource.Instantiate("JumpCloud", blueSlime.transform);
        jumpCloud2.transform.localScale = new Vector3(jumpCloud0.transform.localScale.x * 1.5f, jumpCloud0.transform.localScale.y * 1.5f, jumpCloud0.transform.localScale.z * 1.5f);
        GameObject explosion2 = Managers.Resource.Instantiate("PoisonExplosionBlue", blueSlime.transform);
        GameObject smoke2 = Managers.Resource.Instantiate("SmokeFlatBlack", blueSlime.transform);
        smoke2.transform.localPosition = new Vector3(0f, -0.8f, 0.5f);
        smoke2.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        CoroutineManager.StartCoroutine(CameraController.CoShakeCamera(0.3f, 0.4f));
        Managers.Resource.Instantiate("Stones", map.transform);

        #endregion

        yield return new WaitForSeconds(1f);

        // yellow Slime Potion
        GameObject potion = Managers.Resource.Instantiate("ConsumableItem");
        potion.GetComponent<ConsumableItem>().id = 7;
        potion.transform.position = new Vector3(yellowSlime.transform.position.x, 0f, -4.05f);
        potion.transform.localScale = new Vector3(1f, 2f, 1f);
       
        Vector3 original3 = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target3 = Define.DEFALUT_CAMERA_OFFSET;
        float moveTime3 = 1f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().StartCoVirtualCameraMove(original3, target3, moveTime3);

        yield return new WaitForSeconds(1);

        Managers.UI.ShowGameSceneUI();
        Managers.Directing.CloseLetterBox();

        yield return new WaitForSeconds(1f);
        Managers.Game.OnDirect = false;
        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        Managers.Game.CurEventID = Define.EVENT_KINGSLIME_DEAD;
    }
    #endregion

    #region Tutorial
    public void CoPlayTutorial_1()
    {
        Managers.Sound.Play(Define.Sound.Bgm, "Chapter0_BGM");
        Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));
        CoroutineManager.StartCoroutine(PlayTutorial_1());
        Managers.UI.CloseGameSceneUI();
        Managers.Game.Player._isEquiptWeapon = false;
    }

    // 마검 만남
    IEnumerator PlayTutorial_1()
    {
        Managers.Game.CurEventID = 0;
        // Set Player Dir
        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);

        yield return new WaitForSeconds(0.1f);

        Managers.Game.OnDirect = true;

        // Player Movement
        float originalSpeed = Managers.Game.PlayerData.MoveSpeed;
        Managers.Game.Player.Speed = 1f;
        Managers.Game.Player.Moving(Define.MoveDir.Up, true);
        Managers.Game.Player._back.GetComponent<Animator>().Play("Tutorial_First_Run");

        yield return new WaitForSeconds(0.5f);
        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);
        Managers.Game.Player._back.GetComponent<Animator>().Play("Tutorial_First_Idle");

        yield return new WaitForSeconds(1f);
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
                break;
            }

            prevConvsersationState = currentConversationState;

            yield return null;
        }
        #endregion

        Managers.Sound.Play(Sound.Effect, "HeroReady_SFX");
        Managers.Game.Player.SetState(PlayerState.TutorialFirst_Ready);
        Managers.Game.Player._back.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        Managers.Game.Player.SetState(Define.PlayerState.IdleBack);
        Managers.Game.Player._isEquiptWeapon = true;
        Managers.Game.Player._weapon.SetActive(true);

        PlayerPrefs.SetInt("ISFIRST", 0);
        Managers.UI.ShowGameSceneUI();
        Managers.UI.ShowStageNamePopup(Define.STAGE_NAME_DURATION);
        Managers.Game.OnDirect = false;
        Managers.Game.SaveGame();
    }
    #endregion

    public void StartBossDeathEffect(GameObject boss)
    {
        CoroutineManager.StartCoroutine(BossDeadEffect(boss));
    }

    IEnumerator BossDeadEffect(GameObject boss)
    {
        Managers.Sound.Play(Define.Sound.Effect, "BossDeath_SFX");
        Vector3 bossPos = boss.transform.position;

        boss.GetComponent<SpriteRenderer>().enabled = true;
        SpriteRenderer sr = boss.GetOrAddComponent<SpriteRenderer>();
        sr.color = Util.DamagedColor();
        boss.GetComponent<Animator>().speed = 0f;
        yield return new WaitForSeconds(0.1f);

        GameObject boom = Managers.Resource.Instantiate("BossDeathBoom");
        boom.transform.position = boss.transform.position;
        boss.GetComponent<Collider>().enabled = false;
        boom.GetComponent<BossBoom>().StartCoBossBoom(boss);

        yield return new WaitForSeconds(0.5f);

        // 하얗게
        boss.GetOrAddComponent<SpriteRenderer>().material = Managers.Resource.Load<Material>("PaintWhiteMat");
        sr.DOColor(Color.white, 2f);

        yield return new WaitForSeconds(0.5f);

        GameObject light = Managers.Resource.Instantiate("BossDeathLight");
        light.transform.position = boss.transform.position;
        light.transform.localScale = new Vector3(1f, 2f, 1f);
        yield return new WaitForSeconds(1f);


        Managers.Resource.Destroy(light);
        Managers.Resource.Destroy(boss);
        GameObject poofCloudArcs = Managers.Resource.Instantiate("PoofCloudArcs");
        poofCloudArcs.transform.position = bossPos;

        GameObject poofCloudNova = Managers.Resource.Instantiate("PoofCloudNova");
        poofCloudNova.transform.position = bossPos;
    }

    public void CoStartEndingScene()
    {
        CoroutineManager.StartCoroutine(StartEndingScene());
    }
    IEnumerator StartEndingScene()
    {
        Managers.UI.CloseGameSceneUI();
        Managers.Game.OnDirect = true;

        Managers.Sound.FadeAndStopBGM(1.5f);

        CoroutineManager.StartCoroutine(CameraController.CoExposure(1.5f, CameraController.Exposure.Black));
        yield return new WaitForSeconds(1.5f);

        Managers.Scene.LoadScene(Define.Scene.EndingScene);
        Managers.Game.OnDirect = false;
    }
}
