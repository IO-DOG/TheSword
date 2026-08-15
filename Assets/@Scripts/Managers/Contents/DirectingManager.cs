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

        // 계약이 끝나면 3층에 열쇠가 열린다. 이 열쇠가 있어야 2층의 보스방 구역 문이 열리고,
        // 그래야 킹슬라임에게 갈 수 있다 — 여기서 실패하면 게임을 더 진행할 수가 없다.
        // 예전에는 이름으로 맵을 찾고(Instantiate 이름이 다르면 null) SaveGame 이 먼저라
        // 그 사이 어디서든 예외가 나면 열쇠가 영영 안 나왔다.
        EnableMagicSwordKey();

        Managers.Game.SaveGame();
        Managers.Sound.FadeAndPlayBGM("Chapter0_BGM", 0.8f);
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
        if (_kingSlime != null)
        {
            _kingSlime.transform.localScale = new Vector3(1f, 2f, 1f);
            _kingSlime.transform.localPosition = new Vector3(3.84f, 3f, -5.5f);
            _kingSlime.SetActive(true);
            MakeBossReachable(_kingSlime);
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

        // 콜라이더가 스프라이트에 맞춰 붙어 있어서 제 칸을 벗어나 있다.
        // 실측: 보스 칸은 (12,-17) 인데 콜라이더 중심은 z 로 1.4칸 북쪽,
        // 높이는 6유닛(18칸)짜리 거대한 상자였다. 그래서 플레이어가 보스 칸을
        // 그냥 통과했고 전투가 열리지 않았다.
        // 다른 몬스터처럼 제 칸 한 칸만 차지하게 맞춘다.
        Vector3 scale = boss.transform.lossyScale;
        Vector3 before = col.bounds.center;
        col.center = Vector3.zero;
        col.size = new Vector3(
            Define.TILE_SIZE / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            Define.TILE_SIZE / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            Define.TILE_SIZE / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
        Debug.Log($"[Directing] 보스 콜라이더를 제 칸에 맞췄다 — 중심 {before} -> {col.bounds.center}");

        // 싸울 수 있는 몬스터는 예외 없이 바닥(y=0)에 선다. 킹슬라임만 등장 연출
        // 때문에 y=3 에 뜬 채로 남아서, 플레이어의 충돌 판정 높이에 아예 걸리지
        // 않는다. 같은 바닥으로 내린다.
        float groundY = Managers.Game.Player.transform.position.y;
        Vector3 p = boss.transform.position;
        if (Mathf.Abs(p.y - groundY) > 0.01f)
        {
            boss.transform.position = new Vector3(p.x, groundY, p.z);
            Debug.Log($"[Directing] 보스를 몬스터와 같은 바닥(y={groundY:0.00})으로 내렸다 " +
                      $"— 원래 y={p.y:0.00}");
        }
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
