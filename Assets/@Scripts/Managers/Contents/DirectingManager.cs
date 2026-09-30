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
    // Events 는 MonoBehaviour 라 new 로 만들 수 없다(그렇게 하면 null 이 되어
    // 엔딩 연출 CoStartEndingScene 등이 전부 터진다). @Managers 에 컴포넌트로 붙인다.
    Events _events;
    public Events Events
    {
        get
        {
            if (_events == null)
            {
                GameObject go = GameObject.Find("@Managers");
                if (go == null)
                    go = new GameObject { name = "@Managers" };
                _events = Util.GetOrAddComponent<Events>(go);
            }
            return _events;
        }
    }
    public UI_LetterBox letterBox;
    public void PlayDirecting(int eventId)
    {
        switch (eventId)
        {
            case 1:
                Events.CoStartEvent_1();
                // += 로만 쌓으면 예전 구독이 남아, 엉뚱한 대화가 끝날 때 마검 계약
                // 팝업이 튀어나온다 (4층 분열 슬라임과 싸우는 중에 떴다).
                // 매번 새로 건다. 그리고 이미 계약했으면 띄우지 않는다.
                PopupAction = null;
                PopupAction += (() =>
                {
                    if (Managers.Game.PlayerData.IsContractedSword)
                        return;
                    Managers.UI.ShowPopupUI<UI_MagicalSwordCheckPopup>();
                });
                break;
        }
    }

    public void PlayLetterBox()
    {
        // 이미 내려와 있으면 그대로 쓴다. 두 번 띄우면 앞의 것은 아무도 걷지 않아 화면에 남는다.
        if (letterBox != null)
            return;
        letterBox = Managers.UI.ShowPopupUI<UI_LetterBox>();
        letterBox.Init();
        letterBox.StartLetterBox();

    }

    public void CloseLetterBox()
    {
        // 레터박스가 이미 닫혔거나 애초에 안 열렸을 수 있다. 그때 여기서 터지면
        // 부르는 쪽 연출이 통째로 죽는다 — 킹슬라임 사망 연출이 그랬다.
        if (Managers.Directing.letterBox != null)
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
        // 기다리는 쪽(EVENT_1)이 _coroutineCompleted 만 본다. 이모티콘이 없어도 끝났다고는 알린다.
        try
        {
            GameObject go = transform != null ? Managers.Resource.Instantiate("Emoji", transform) : null;
            if (go != null)
            {
                go.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                go.transform.localPosition = new Vector3(0.2f, 0.8f, -0.1f);
                Animator animator = go.GetComponent<Animator>();
                float delay = 1f;
                if (animator != null && string.IsNullOrEmpty(EmojiName) == false)
                {
                    animator.Play(EmojiName);
                    // Play 는 다음 갱신에서야 상태를 바꾼다. 곧바로 재면 기본 상태(Surprise)의 길이가 나온다.
                    yield return null;
                    if (animator != null)
                        delay = animator.GetCurrentAnimatorStateInfo(0).length;
                }
                yield return new WaitForSeconds(delay);
                Managers.Resource.Destroy(go);
            }
            yield return new WaitForSeconds(1f);
        }
        finally
        {
            _coroutineCompleted = true;
        }
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
    /// <summary>계약 때 함께 받는 몬스터 도감 (EquipData 31, 책 칸). 마검의 기억 = 도감 (바이블 2.3).</summary>
    const int MonsterBookId = 31;

    public void CoStartContractSword()
    {
        CoroutineManager.StartCoroutine(ContractSword());
    }

    IEnumerator ContractSword()
    {
        Managers.Game.OnDirect = true;
        bool contracted = false;
        try
        {
            if (Managers.Game.DirectionalLight != null)
                Managers.Game.DirectionalLight.DOIntensity(0.05f, 0.5f);

            Managers.Game.Player.SetState(Define.PlayerState.ContractSword);

            if (Managers.Game.CurInteractObject != null)
                Managers.Game.CurInteractObject.transform.gameObject.SetActive(false);

            yield return new WaitForSeconds(1f);

            GameObject go1 = Managers.Resource.Instantiate("FX_ContractSwordEffect", Managers.Game.Player.transform);
            if (go1 != null)
            {
                go1.transform.localPosition = Vector3.zero;
                go1.transform.localScale = new Vector3(0.3f, 0.3f, 0.15f);
            }

            GameObject go2 = Managers.Resource.Instantiate("FX_PowerWave", Managers.Game.Player.transform);
            if (go2 != null)
            {
                go2.transform.localPosition = Vector3.zero;
                go2.transform.localScale = new Vector3(0.2f, 0.2f, 0.1f);
            }

            yield return new WaitForSeconds(3f);

            GameObject fireflies = GameObject.Find("MagicalSwordRoomFireflies");
            if (fireflies != null)
            {
                fireflies.SetActive(false);
            }

            GameObject godray = GameObject.Find("MagicalSwordRoomGodray");
            SpriteRenderer godraySprite = godray != null ? godray.GetComponent<SpriteRenderer>() : null;
            if (godraySprite != null)
            {
                godraySprite.material = Managers.Resource.Load<Material>("Godray3");
            }

            Volume postProcessingVolume = Managers.Game.MainCamera != null ? Managers.Game.MainCamera.GetComponent<Volume>() : null;
            if (postProcessingVolume != null && postProcessingVolume.profile.TryGet<ColorAdjustments>(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments.colorFilter.Override(new Color(255 / 255f, 231 / 255f, 206 / 255f));
            }

            if (Managers.Game.DirectionalLight != null)
            {
                Managers.Game.DirectionalLight.color = new Color(255 / 255f, 244 / 255f, 214 / 255f);
                Managers.Game.DirectionalLight.DOIntensity(1.5f, 1f);
            }

            Managers.Resource.Destroy(go1);
            Managers.Resource.Destroy(go2);

            yield return new WaitForSeconds(1.5f);

            FinishContract();
            contracted = true;
        }
        finally
        {
            // 연출이 어디서 끊겨도 계약의 결과는 남긴다. 마검(+10 공격)과 3층 열쇠가 없으면
            // 그 뒤를 진행할 수 없다 — 예전에는 자동 플레이 봇이 이 결과를 손으로 대신 채웠다.
            if (contracted == false)
            {
                Debug.LogWarning("[Directing] 마검 계약 연출이 끊겼다 — 결과만 남긴다");
                FinishContract();
                Managers.Game.OnDirect = false;
                Managers.UI.ShowGameSceneUI();
            }
        }

        // 외눈이 뜨인다 → 첫 예측 (바이블 9.1). 레터박스 없이 이 자리에서 대화만.
        yield return StoryDirector.CoPrologue(StoryPrologue.ContractAfter);

        Managers.Game.OnDirect = false;
        Managers.UI.ShowGameSceneUI();
        Managers.Sound.FadeAndPlayBGM("Chapter0_BGM", 0.8f);
    }

    /// <summary>계약의 결과: 마검을 쥐고, 도감을 받고, 3층 열쇠가 열리고, 체크포인트를 쓴다. 두 번 불러도 한 번 한 것과 같다.</summary>
    static void FinishContract()
    {
        GameManager game = Managers.Game;
        game.PlayerData.IsContractedSword = true;
        if (game.Player != null)
        {
            game.Player.SetState(Define.PlayerState.IdleFront);
            game.Player._moveDir = Define.MoveDir.Down;
            game.Player._isEquiptWeapon = true;
            game.Player._isEquiptShield = true;
        }

        // 인벤토리에 마검 추가, 현재 검 변경
        int sword = Define.EQUIP_SOWRD_FIRST + 1;
        List<int> swords = game.PlayerData.Inventory[(int)Define.Types.Sword];
        if (swords.Contains(sword) == false)
            swords.Add(sword);
        if (game.PlayerData.CurSword != sword)
            game.SwapEquip(sword);

        // 몬스터 도감 — 인벤토리의 책 칸에 뜬다.
        List<int> books = game.PlayerData.Inventory[(int)Define.Types.Book];
        if (Managers.Data.EquipDic.ContainsKey(MonsterBookId) && books.Contains(MonsterBookId) == false)
        {
            books.Add(MonsterBookId);
            game.EquipIfBetter(MonsterBookId);
        }

        // 계약이 끝나면 3층에 열쇠가 열린다. 이 열쇠가 있어야 2층의 보스방 구역 문이 열리고,
        // 그래야 킹슬라임에게 갈 수 있다 — 여기서 실패하면 게임을 더 진행할 수가 없다.
        // 예전에는 이름으로 맵을 찾고(Instantiate 이름이 다르면 null) SaveGame 이 먼저라
        // 그 사이 어디서든 예외가 나면 열쇠가 영영 안 나왔다.
        EnableMagicSwordKey();

        // 만났다는 표시는 계약이 끝난 뒤에 남긴다. 예전에는 연출을 시작할 때 남겨서, 연출이 죽으면
        // 검 없이 열쇠만 되살아났다 (UI_GameScene.RestoreMagicSwordKey 가 이것을 본다).
        PlayerPrefs.SetInt("ISMEETSWORD", 1);
        // 인벤토리 단추(ISOPENINVENUI)도 체크포인트보다 먼저 연다. 예전에는 계약 뒤 대화가 끝난 다음에 열어서, 그 사이에
        // 저장된 체크포인트가 0 을 쥐었다 — 다음 계단 전에 불러오면 단추가 판 끝까지 사라졌다. 연출이 끊긴 길(finally)도 같다.
        // HUD 는 아직 숨겨져 있어 단추는 연출이 끝나 HUD 가 돌아올 때 보인다.
        if (game.GameScene != null)
            game.GameScene.OnUIInventory();
        game.SaveGame();
    }

    /// <summary>마검 계약 뒤에 열리는 3층 열쇠(Items/CItem13)를 켠다.</summary>
    static void EnableMagicSwordKey()
    {
        GameObject curMap = null;

        // 이름으로 찾는 대신 실제로 생성된 맵에서 집는다.
        foreach (KeyValuePair<int, GameObject> pair in Managers.Game.Maps)
        {
            Data.StageInfoData info;
            if (Managers.Data.StageInfoDic.TryGetValue(pair.Key, out info) == false)
                continue;
            if (info.DungeonID != "00_002")
                continue;
            curMap = pair.Value;
            break;
        }
        if (curMap == null)
            curMap = GameObject.Find("Dungeon_00_002");
        if (curMap == null)
        {
            Debug.LogError("[Directing] 3층 맵을 못 찾아 마검 열쇠를 켜지 못했다");
            return;
        }

        Transform key = curMap.transform.Find("Items/CItem13");
        if (key == null)
        {
            Debug.LogError("[Directing] Items/CItem13 이 없어 마검 열쇠를 켜지 못했다");
            return;
        }

        key.gameObject.SetActive(true);
        SpriteRenderer sr = key.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
        BoxCollider col = key.GetComponent<BoxCollider>();
        if (col != null) col.enabled = true;

        // 이 열쇠는 프리팹에 _itemIndex_forActive = 13 이 구워져 있는데,
        // 2층의 포션(Dungeon_00_001/Items/CItem13)도 같은 13 을 쓴다.
        // 그래서 2층 포션을 먹는 순간 CItemActiveDic[13] 이 false 가 되고,
        // 다음 RefreshMap 에서 이 열쇠가 같이 꺼져 버렸다 —
        // 열쇠가 없으면 2층 (15,-10) 문이 안 열리고 보스방에 영영 못 간다.
        // 아무도 안 쓰는 번호로 옮겨서 충돌을 끊는다.
        ConsumableItem ci = key.GetComponent<ConsumableItem>();
        if (ci != null)
        {
            ci._itemIndex_forActive = Define.MAGICAL_SWORD_KEY_INDEX;
            Managers.Data.CItemActiveDic[Define.MAGICAL_SWORD_KEY_INDEX] = true;
        }
    }

    #endregion

    #region KingSlimeDirecting
    public GameObject _kingSlime;

    public void MeetKingSlime()
    {
        // 등장 연출이 끝에서 보스를 다시 켠다. 그래서 매번 먼저 숨긴다 — 예전에는 한 판에 한 번만 숨겨서
        // (그 표시가 되돌려지지 않았다) 죽고 다시 온 두 번째 등장에서는 보스가 처음부터 보인 채로 연출이 돌았다.
        _kingSlime = GameObject.Find("bossMonster0");
        if (_kingSlime != null)
        {
            _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = false;
            _kingSlime.GetOrAddComponent<BoxCollider>().enabled = false;
        }

        CoroutineManager.StartCoroutine(CoKingSlimeAction());
    }

    /// <summary>
    /// 킹슬라임 등장 연출.
    ///
    /// 여기서 쓰는 오브젝트는 전부 Dungeon_00_003 프리팹 안의 것을 이름으로 찾는다.
    /// 하나라도 못 찾으면 예전에는 코루틴이 그 자리에서 죽었고 OnDirect 가 켜진 채 남아
    /// 게임이 3층에서 영영 멈췄다 — 보스방 문은 이 연출의 끝에서만 열리기 때문이다.
    /// 그래서 모든 Find 를 널 안전하게 바꾸고, 어떤 경로로 빠져나가든
    /// AfterMeetKingSlime 이 반드시 불리도록 finally 로 묶었다.
    /// </summary>
    IEnumerator CoKingSlimeAction()
    {
        bool handedOff = false;
        try
        {
            Managers.Game.OnDirect = true;
            yield return new WaitForSeconds(0.4f);
            Managers.Game.OnStaticResolution = true;
            Managers.UI.CloseGameSceneUI();
            Managers.Directing.PlayLetterBox();

            Managers.Game.Player.SetIdleState(Define.MoveDir.Up);

            CameraController cam = Managers.Game.MainCamera != null
                ? Managers.Game.MainCamera.GetComponentInChildren<CameraController>() : null;
            CinemachineVirtualCamera vcam = Camera.main != null
                ? Camera.main.GetComponentInChildren<CinemachineVirtualCamera>() : null;
            CinemachineTransposer transposer = vcam != null
                ? vcam.GetCinemachineComponent<CinemachineTransposer>() : null;

            Vector3 original0 = transposer != null ? transposer.m_FollowOffset : Vector3.zero;
            if (cam != null)
                cam.StartCoVirtualCameraMove(original0, new Vector3(0f, 20f, -5f), 2f);

            GameObject parent0 = GameObject.Find("Dungeon_00_003");
            Vector3 pos = new Vector3(3.845f, 1.47f, -1.408f);
            GameObject scoutSlime = parent0 != null
                ? Managers.Resource.Instantiate("BossScene_C0_000", parent0.transform) : null;
            if (scoutSlime != null)
                scoutSlime.transform.localPosition = pos;

            Managers.Sound.FadeAndPlayBGM("Chapter0_Boss_Event", 0.5f);

            yield return new WaitForSeconds(1.75f);

            if (transposer != null)
                transposer.m_FollowOffset = new Vector3(0f, 20f, -5f);

            if (scoutSlime != null)
            {
                scoutSlime.transform.localPosition = pos;
                CoroutineManager.StartCoroutine(
                    CoMoveToDest(scoutSlime, new Vector3(pos.x, pos.y, pos.z - 0.3f), 2.5f));
            }
            yield return new WaitForSeconds(2.5f);

            PlayAnim(scoutSlime, "bossScene_C0_001");
            yield return new WaitForSeconds(1f);

            if (scoutSlime != null)
                scoutSlime.transform.DOLocalMoveZ(-0.3f, 1f);
            yield return new WaitForSeconds(0.5f);

            // 카메라 감속
            if (cam != null)
            {
                Vector3 from = transposer != null ? transposer.m_FollowOffset : Vector3.zero;
                cam.StartCoVirtualCameraMove(from, new Vector3(0f, 14.5f, -5f), 3f);
            }
            yield return new WaitForSeconds(0.5f);

            SetSlimeFall(true);
            yield return new WaitForSeconds(0.5f);

            GameObject slimesPos = GameObject.Find("SlimesPos");
            GameObject slimes = slimesPos != null
                ? Managers.Resource.Instantiate("Slimes", slimesPos.transform) : null;
            GameObject slimesCore = slimesPos != null
                ? Managers.Resource.Instantiate("SlimesCore", slimesPos.transform) : null;
            if (slimesCore != null)
                slimesCore.transform.DOScale(Vector3.one * 2f, 1f);

            _kingSlime = GameObject.Find("bossMonster0");

            GameObject actionFront = GameObject.Find("KingSlimeActionFront");
            PlayAnim(actionFront, "NewKingSlimeActionFront");
            GameObject actionBack = GameObject.Find("KingSlimeActionBack");
            PlayAnim(actionBack, "NewKingSlimeActionBack");

            {
                WaitForSeconds delay = new WaitForSeconds(0.4f);
                for (int i = 1; i <= 5; i++)
                {
                    yield return delay;
                    StopParticle(GameObject.Find("SlimeFall" + i));
                }
            }

            StopParticle(slimes);
            StopParticle(slimesCore);
            yield return new WaitForSeconds(2.1f);
            CoroutineManager.StartCoroutine(CameraController.WhiteBang(0.1f));
            if (actionFront != null)
                Managers.Resource.Destroy(actionFront);

            if (_kingSlime != null)
            {
                _kingSlime.transform.localPosition = new Vector3(3.84f, 3f, -5.5f);
                _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = true;
                _kingSlime.GetOrAddComponent<BoxCollider>().enabled = true;

                Animator anim = _kingSlime.GetComponent<Animator>();
                if (anim != null) anim.speed = 0f;
                _kingSlime.transform.DOLocalMoveZ(_kingSlime.transform.localPosition.z - 0.5f, 0.2f);
                _kingSlime.transform.DOScaleY(0.5f, 0.15f);

                KingSlimeController ks = _kingSlime.GetComponent<KingSlimeController>();
                if (ks != null && ks._sr != null)
                {
                    ks._sr.material = Managers.Resource.Load<Material>("PaintWhiteMat");
                    ks._sr.color = Color.white;
                }

                yield return new WaitForSeconds(0.15f);

                if (anim != null) anim.speed = 1f;
                _kingSlime.transform.DOLocalMoveZ(_kingSlime.transform.localPosition.z + 0.5f, 0.1f);
                _kingSlime.transform.DOScaleY(2f, 0.15f);
                if (ks != null && ks._sr != null)
                    ks._sr.material = Managers.Resource.Load<Material>("HalfSpriteShadow");
            }
            else
            {
                yield return new WaitForSeconds(0.15f);
            }

            GameObject actions = GameObject.Find("Actions");
            if (actions != null)
                Managers.Resource.Instantiate("KingSlimeInstantiateEffect", actions.transform);
            CoroutineManager.StartCoroutine(CameraController.CoShakeCamera(0.7f, 0.7f));

            if (actionBack != null)
                actionBack.SetActive(false);
            GameObject effects = GameObject.Find("Effects_00");
            if (effects != null)
                effects.SetActive(false);

            handedOff = true;
            CoroutineManager.StartCoroutine(AfterMeetKingSlime());
        }
        finally
        {
            // 중간에 무슨 일이 있어도 마무리는 반드시 돌린다.
            // 이게 없으면 OnDirect 가 켜진 채 남아 3층에서 진행이 끊긴다.
            if (handedOff == false)
            {
                Debug.LogWarning("[Directing] 킹슬라임 연출이 중단됐다 — 마무리만 진행한다");
                CoroutineManager.StartCoroutine(AfterMeetKingSlime());
            }
        }
    }

    /// <summary>이름으로 찾은 오브젝트의 애니메이터를 재생한다. 없으면 조용히 넘어간다.</summary>
    static void PlayAnim(GameObject go, string clip)
    {
        if (go == null)
            return;
        Animator anim = go.GetComponent<Animator>();
        if (anim != null)
            anim.Play(clip);
    }

    static void SetSlimeFall(bool play)
    {
        for (int i = 1; i <= 5; i++)
        {
            GameObject go = GameObject.Find("SlimeFall" + i);
            if (go == null)
                continue;
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                continue;
            if (play) ps.Play();
            else ps.Stop();
        }
    }

    static void StopParticle(GameObject go)
    {
        if (go == null)
            return;
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Stop();
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
        try
        {
            if (_kingSlime != null)
            {
                _kingSlime.transform.localScale = new Vector3(1f, 2f, 1f);
                _kingSlime.transform.localPosition = new Vector3(3.84f, 3f, -5.5f);
                _kingSlime.SetActive(true);
                MakeBossReachable(_kingSlime);
            }

            Managers.UI.ShowBossNamePopup(1.5f);
            Managers.Sound.FadeAndPlayBGM("Chapter0_Boss_BGM", 1f);

            yield return new WaitForSeconds(2f);

            // "킹? 왕은 이몸 하나다!" — 레터박스를 내린 채, 카메라가 킹 슬라임을 잡고 있을 때 (바이블 10절)
            yield return StoryDirector.CoPrologue(StoryPrologue.KingslimeReveal);

            CameraController cam = Managers.Game.MainCamera != null ? Managers.Game.MainCamera.GetComponentInChildren<CameraController>() : null;
            if (cam != null && CameraController._transposer != null)
                cam.StartCoVirtualCameraMove(CameraController._transposer.m_FollowOffset, Define.DEFALUT_CAMERA_OFFSET, 2f);
            yield return new WaitForSeconds(1f);
            Managers.Directing.CloseLetterBox();
            yield return new WaitForSeconds(1f);
        }
        finally
        {
            // 어디서 끊겨도 보스방에서 움직일 수 있게 돌려놓는다 (레터박스가 이미 걷혔으면 아무 일도 없다).
            Managers.Directing.CloseLetterBox();
            Managers.Game.OnStaticResolution = false;
            Managers.Game.OnDirect = false;
            Managers.UI.ShowGameSceneUI();
        }
    }

    /// <summary>
    /// 킹슬라임은 스프라이트를 띄워 놓으려고 바닥보다 한참 위(localY=3)에 선다.
    /// 그런데 전투는 PlayerController 가 제 키높이로 쏘는 얇은 광선이 몬스터
    /// 콜라이더에 닿아야 시작된다 — 그 높이에서는 보스를 스치지도 못해서
    /// 밀어도 아무 일이 없고 그대로 통과했다. 보스방에서 싸움 자체가 안 열린다.
    ///
    /// 보이는 위치는 그대로 두고 콜라이더만 플레이어 키높이까지 늘린다.
    /// </summary>
    static void MakeBossReachable(GameObject boss)
    {
        if (boss == null || Managers.Game.Player == null)
            return;

        if (boss.layer != (int)Define.Layer.Monster)
        {
            Debug.LogWarning($"[Directing] 보스가 Monster 레이어가 아니다 ({boss.layer}) — 옮긴다");
            boss.layer = (int)Define.Layer.Monster;
        }

        BoxCollider col = boss.GetComponent<BoxCollider>();
        if (col == null)
            return;

        // 연출이 중간에 갈리면 콜라이더가 꺼진 채로 남는다. 꺼져 있으면
        // 크기를 아무리 맞춰도 물리 판정에 안 잡히고, 보스는 유령이 된다.
        if (col.enabled == false)
        {
            col.enabled = true;
            Debug.Log("[Directing] 보스 콜라이더가 꺼져 있어 켰다");
        }

        // 보스는 스프라이트를 세워 보이려고 바닥보다 높이(y=3) 서 있다.
        // 그 자체를 바닥으로 내리면 스프라이트 아랫부분이 바닥 이미지에 잘려
        // 절반만 보인다 — 보이는 자리는 그대로 두고 콜라이더만 옮긴다.
        //
        // 콜라이더는 스프라이트에 맞춰 붙어 있어서 제 칸도 벗어나 있었다.
        // 실측: 보스 칸은 (12,-17) 인데 콜라이더 중심은 z 로 1.4칸 북쪽,
        // 높이는 6유닛(18칸)짜리 상자였다. 플레이어의 충돌 판정은 제 키높이로
        // 나가는 얇은 광선이라 거기에 걸리지 않았고, 보스를 그냥 통과했다.
        // 제 칸 한 칸만, 플레이어 키높이에 놓는다.
        Vector3 scale = boss.transform.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        float sz = Mathf.Max(0.0001f, Mathf.Abs(scale.z));

        Vector3 before = col.bounds.center;
        float groundY = Managers.Game.Player.transform.position.y;

        // 높이는 넉넉히. 한 칸으로 맞추면 광선이 위끝에 걸쳐 빗나간다.
        col.size = new Vector3(Define.TILE_SIZE / sx, Define.TILE_SIZE * 16f / sy, Define.TILE_SIZE / sz);
        col.center = new Vector3(0f, (groundY - boss.transform.position.y) / sy, 0f);

        Debug.Log($"[Directing] 보스 콜라이더를 제 칸/바닥높이({groundY:0.00})에 맞췄다 " +
                  $"— 중심 {before} -> {col.bounds.center} (보스는 y={boss.transform.position.y:0.00} 그대로)");
    }

    public void CoStartUnLock4Floor()
    {
        CoroutineManager.StartCoroutine(Unlock4Floor());
    }

    IEnumerator Unlock4Floor()
    {
        // 예전엔 여기서 Portals 배열의 "마지막"을 켰다. 챕터 0 이 5개 맵에서 20개로
        // 늘어난 뒤로는 그 마지막이 20층 계단이라, 엉뚱한 층의 관문이 열렸다.
        // 보스 층 잠금은 GameManager.RefreshBossGates 가 층별로 처리한다.
        Managers.Game.RefreshBossGates();
        // "노랑부터 베니까 산 거다." — 마지막 분열 슬라임의 전투창이 닫혀 한가해지면 뜬다.
        StoryDirector.QueuePrologue(StoryPrologue.KingslimeClear);
        yield return new WaitForSeconds(10f);
        // 보스를 잡은 뒤 계단까지 이어지는 빛줄기. 관문 자체는 위의 RefreshBossGates
        // 가 이미 열었으니, 층을 못 찾거나 이펙트가 없으면 조용히 건너뛴다.
        GameObject parent = GameObject.Find("Dungeon_00_003");
        if (parent == null)
            yield break;

        GameObject go = Managers.Resource.Instantiate("FX_BossClearLine", parent.transform);
        if (go == null)
            yield break;

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
        CameraController cam = Managers.Game.MainCamera != null ? Managers.Game.MainCamera.GetComponentInChildren<CameraController>() : null;
        GameObject map = GameObject.Find("Dungeon_00_003");
        List<GameObject> slimes = new List<GameObject>();
        try
        {
            StopParticle(GameObject.Find("SmokeFlatWhiteGreen"));

            yield return new WaitForSeconds(2f);

            #region Slime orbs event
            GameObject orbsSpawnPos = GameObject.Find("OrbsSpawnPos");
            GameObject slimeOrb = orbsSpawnPos != null ? Managers.Resource.Instantiate("SlimeOrb", orbsSpawnPos.transform) : null;
            Managers.Sound.Play(Define.Sound.Effect, "Chapter0_Boss_Event2");
            yield return new WaitForSeconds(0.5f);
            MoveCamera(cam, new Vector3(0f, 18f, -5f), 1f);
            bool orbs = slimeOrb != null && slimeOrb.transform.childCount >= 3;
            if (orbs)
                slimeOrb.transform.DOLocalMoveZ(2f, 1f).SetLink(slimeOrb);

            yield return new WaitForSeconds(1f);

            if (orbs)
            {
                slimeOrb.transform.GetChild(0).DOLocalMoveX(-2.24f, 0.5f).SetLink(slimeOrb);
                slimeOrb.transform.GetChild(2).DOLocalMoveX(2.24f, 0.5f).SetLink(slimeOrb);
            }
            yield return new WaitForSeconds(0.5f);

            if (orbs)
            {
                slimeOrb.transform.GetChild(0).DOLocalMoveZ(-1f, 0.25f).SetLink(slimeOrb);
                slimeOrb.transform.GetChild(2).DOLocalMoveZ(-1f, 0.25f).SetLink(slimeOrb);
            }
            yield return new WaitForSeconds(0.5f);

            MoveCamera(cam, new Vector3(0f, 16f, -7f), 0.5f);

            yield return new WaitForSeconds(0.1f);

            if (orbs)
            {
                // 노랑·빨강·파랑이 떨어지며 커진다
                Sequence yellow = DOTween.Sequence();
                yellow.Append(slimeOrb.transform.GetChild(0).DOLocalMoveZ(-3f, 0.5f));
                yellow.Append(slimeOrb.transform.GetChild(0).DOScale(5f, 0.5f));
                Sequence red = DOTween.Sequence();
                red.Append(slimeOrb.transform.GetChild(1).DOLocalMoveZ(-1.8f, 0.5f));
                red.Append(slimeOrb.transform.GetChild(1).DOScale(5f, 0.5f));
                Sequence blue = DOTween.Sequence();
                blue.Append(slimeOrb.transform.GetChild(2).DOLocalMoveZ(-3f, 0.5f));
                blue.Append(slimeOrb.transform.GetChild(2).DOScale(5f, 0.5f));
                DOTween.Sequence().Append(yellow).Join(red).Join(blue).SetLink(slimeOrb).Play()
                    .OnComplete(() => Managers.Resource.Destroy(slimeOrb));
            }
            else if (slimeOrb != null)
            {
                Managers.Resource.Destroy(slimeOrb);
            }

            yield return new WaitForSeconds(0.6f);
            #endregion

            #region FlashBang Effect
            float whiteTime = 0.5f;
            float defaultTime = 0.2f;
            CoroutineManager.StartCoroutine(CameraController.CoExposure(whiteTime, CameraController.Exposure.White));
            yield return new WaitForSeconds(whiteTime);

            CoroutineManager.StartCoroutine(CameraController.CoExposure(defaultTime, CameraController.Exposure.Default));
            yield return new WaitForSeconds(defaultTime);
            #endregion

            #region Instantiate 3 Slimes
            slimes.Add(SpawnSplitSlime(map, 0, true));
            yield return new WaitForSeconds(0.2f);
            slimes.Add(SpawnSplitSlime(map, 1, true));
            yield return new WaitForSeconds(0.1f);
            slimes.Add(SpawnSplitSlime(map, 2, true));
            #endregion

            yield return new WaitForSeconds(1f);

            SpawnSplitPotion(slimes[0]);
            MoveCamera(cam, Define.DEFALUT_CAMERA_OFFSET, 1f);

            yield return new WaitForSeconds(1);
        }
        finally
        {
            // 분열 슬라임 셋을 다 잡아야 4층 출구가 열린다. 연출이 어디서 끊겨도 셋(과 노랑의 물약)은 세운다 —
            // 예전에는 연기 하나를 못 찾으면 여기서 멈춰 보스방에 갇혔다.
            if (slimes.Count < SplitSlimes.Length)
            {
                Debug.LogWarning("[Directing] 킹 슬라임 분열 연출이 끊겼다 — 분열 슬라임만 세운다");
                while (slimes.Count < SplitSlimes.Length)
                    slimes.Add(SpawnSplitSlime(map, slimes.Count, false));
                SpawnSplitPotion(slimes[0]);
                MoveCamera(cam, Define.DEFALUT_CAMERA_OFFSET, 0.5f);
            }
            // 킹 슬라임이 쓰러진 순간(UI_MonsterCard.Dead)의 관문 셈에는 아직 분열 슬라임이 없어 출구가 열렸다.
            // 셋이 섰으니 다시 잰다 — 셋을 다 잡으면 마지막 전투의 Dead 가 연다.
            Managers.Game.RefreshBossGates();
            Managers.UI.ShowGameSceneUI();
            Managers.Directing.CloseLetterBox();
        }

        yield return new WaitForSeconds(1f);
        Managers.Game.OnDirect = false;
        Managers.Game.CurEventID = Define.EVENT_KINGSLIME_DEAD;
        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
    }

    // 분열 슬라임: 자리, MonsterData id, 튀는 독 (노랑 대거·빨강 메이스·파랑 방패)
    static readonly (string pos, int id, string fx)[] SplitSlimes =
    {
        ("YellowSlimePos", 7, "PoisonExplosionYellow"),
        ("RedSlimePos", 6, "PoisonExplosionRed"),
        ("BlueSlimePos", 8, "PoisonExplosionBlue"),
    };

    static GameObject SpawnSplitSlime(GameObject map, int index, bool effects)
    {
        (string posName, int id, string fx) = SplitSlimes[index];
        GameObject slime = Managers.Resource.Instantiate("BossMonster_3Slimes", map != null ? map.transform : null);
        if (slime == null)
            return null;
        GameObject pos = GameObject.Find(posName);
        if (pos != null)
            slime.transform.position = pos.transform.position;
        MonsterController monster = slime.GetComponent<MonsterController>();
        if (monster != null)
        {
            monster.id = id;
            // 맵 데이터에 없는 몬스터다. 프리팹의 0 을 그대로 두면 잡을 때 1층 0번 몬스터가 죽은 것으로 저장됐다.
            monster._monsterIndex_forActive = GameManager.SplitSlimeActiveIndex + index;
        }
        if (effects == false)
            return slime;

        GameObject jumpCloud = Managers.Resource.Instantiate("JumpCloud", slime.transform);
        if (jumpCloud != null)      // 빨강·파랑의 구름은 예전부터 노랑 것(1.5배)의 1.5배였다 — 그림을 그대로 둔다
            jumpCloud.transform.localScale *= index == 0 ? 1.5f : 2.25f;
        Managers.Resource.Instantiate(fx, slime.transform);
        GameObject smoke = Managers.Resource.Instantiate("SmokeFlatBlack", slime.transform);
        if (smoke != null)
        {
            smoke.transform.localPosition = new Vector3(0f, -0.8f, 0.5f);
            smoke.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        }
        CoroutineManager.StartCoroutine(CameraController.CoShakeCamera(0.3f, 0.4f));
        if (map != null)
            Managers.Resource.Instantiate("Stones", map.transform);
        return slime;
    }

    // 노랑 슬라임(대거)은 몸속에 물약을 품었다 — 그 자리 앞에 떨어진다.
    static void SpawnSplitPotion(GameObject yellowSlime)
    {
        GameObject potion = Managers.Resource.Instantiate("ConsumableItem");
        ConsumableItem item = potion != null ? potion.GetComponent<ConsumableItem>() : null;
        if (item == null)
            return;
        item.id = 7;
        // 맵 데이터에 없는 물약이다. 프리팹의 0 을 그대로 두면 마실 때 1층 0번 물약이 먹은 것으로 저장됐다.
        item._itemIndex_forActive = GameManager.SplitPotionActiveIndex;
        float x = yellowSlime != null ? yellowSlime.transform.position.x : potion.transform.position.x;
        potion.transform.position = new Vector3(x, 0f, -4.05f);
        potion.transform.localScale = new Vector3(1f, 2f, 1f);
    }

    static void MoveCamera(CameraController cam, Vector3 offset, float time)
    {
        if (cam != null && CameraController._transposer != null)
            cam.StartCoVirtualCameraMove(CameraController._transposer.m_FollowOffset, offset, time);
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
        if (boss == null)
            yield break;
        Managers.Sound.Play(Define.Sound.Effect, "BossDeath_SFX");
        Vector3 bossPos = boss.transform.position;
        GameObject light = null;
        try
        {
            SpriteRenderer sr = boss.GetOrAddComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.color = Util.DamagedColor();
            Animator animator = boss.GetComponent<Animator>();
            if (animator != null)
                animator.speed = 0f;
            // 콜라이더는 자식에 붙어 있기도 하다 — 쓰러진 보스와 다시 부딪히지 않게 전부 끈다.
            foreach (Collider col in boss.GetComponentsInChildren<Collider>())
                col.enabled = false;
            yield return new WaitForSeconds(0.1f);

            GameObject boom = Managers.Resource.Instantiate("BossDeathBoom");
            BossBoom bossBoom = boom != null ? boom.GetComponent<BossBoom>() : null;
            if (bossBoom != null)
            {
                boom.transform.position = bossPos;
                bossBoom.StartCoBossBoom(boss);
            }

            yield return new WaitForSeconds(0.5f);

            // 하얗게
            if (sr != null)
            {
                sr.material = Managers.Resource.Load<Material>("PaintWhiteMat");
                // 보스가 사라진 뒤에도 트윈이 살아 스프라이트 색을 만지면 널참조가 난다.
                sr.DOColor(Color.white, 2f).SetLink(sr.gameObject);
            }

            yield return new WaitForSeconds(0.5f);

            light = Managers.Resource.Instantiate("BossDeathLight");
            if (light != null)
            {
                light.transform.position = bossPos;
                light.transform.localScale = new Vector3(1f, 2f, 1f);
            }
            yield return new WaitForSeconds(1f);
        }
        finally
        {
            // 연출이 어디서 끊겨도 쓰러진 보스는 치운다.
            Managers.Resource.Destroy(light);
            Managers.Resource.Destroy(boss);
        }

        GameObject poofCloudArcs = Managers.Resource.Instantiate("PoofCloudArcs");
        if (poofCloudArcs != null)
            poofCloudArcs.transform.position = bossPos;

        GameObject poofCloudNova = Managers.Resource.Instantiate("PoofCloudNova");
        if (poofCloudNova != null)
            poofCloudNova.transform.position = bossPos;
    }

    /// <summary>100층 위층 계단(다음 층이 없다). 결말 흐름으로 간다 — 이미 봤으면 크레딧과 엔딩 씬만.</summary>
    public void CoStartEndingScene()
    {
        StoryDirector.OnFinalStairs();
    }
}
