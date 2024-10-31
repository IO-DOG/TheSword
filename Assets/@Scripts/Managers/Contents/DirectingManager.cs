using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class DirectingManager
{
    public Action PopupAction;
    public Events Events = new Events();

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
}

public class Events
{
    bool _coroutineCompleted;
    void StartCoPlayEmoji(string EmojiName, Transform transform)
    {
        _coroutineCompleted = false;
        CoroutineManager.StartCoroutine(PlayEmoji(EmojiName, transform));
    }
    IEnumerator PlayEmoji(string EmojiName, Transform transform)
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
            float originalSpeed = Managers.Game.CurPlayerData.MoveSpeed;
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
        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
    }
    #endregion

    #region Contract Sword
    public void CoStartContractSword()
    {
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

        Managers.Game.CurPlayerData.IsContractedSword = true;
        Managers.Game.Player.SetState(Define.PlayerState.IdleFront);
        Managers.Game.Player._moveDir = Define.MoveDir.Down;
        Managers.Game.Player._isEquiptWeapon = true;
        Managers.Game.Player._isEquiptShield = true;
        Managers.Game.CurPlayerData.CurSword = Define.EQUIP_SOWRD_FIRST + 1;
        Managers.Game.OnDirect = false;
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
        if (_clearKingSlime == false && Managers.Game.Player.gameObject.transform.position.x > 303.5f && Managers.Game.Player.gameObject.transform.position.x < 304.2f)
        {
            _clearKingSlime = true;
            _kingSlime = GameObject.Find("bossMonster0");
            _kingSlime.gameObject.SetActive(false);
            //_kingSlime.GetOrAddComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        }

        if (Managers.Game.Player.gameObject.transform.position.x < 303.5f || Managers.Game.Player.gameObject.transform.position.x > 304.2f
            || Managers.Game.Player.gameObject.transform.position.z < -7f) // 일단 하드코딩 특정 자리가 아니면 리턴함.
            return;
        // x : 303.52 ~ 304.16
        // y : -7.04
        Debug.Log("킹 슬라임을 만났다!!");

        Managers.Game.OnMeetKingSlime = true;
        Managers.Game.OnDirect = true;

        // 주인공을 길 중간 위치로 이동
        // 주인공이 정면을 바라보도록
        // 카메라 워킹 및 UI사라짐
        // 카메라 흔들림 등의 연출 효과
        // 연출이 끝나면 UI 활성화

        GameObject tutorialSene = GameObject.Find("UI_TutorialScene");
        if (tutorialSene != null)
        {
            // UI Off
            RectTransform[] rectTransforms = tutorialSene.gameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 1; i < rectTransforms.Length; i++)
            {
                //rectTransforms[i].gameObject.SetActive(false);
                Image image = rectTransforms[i].gameObject.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(1, 1, 1, 0);
                }
                TMP_Text tMP_Text = rectTransforms[i].gameObject.GetComponent<TMP_Text>();
                if (tMP_Text != null)
                {
                    tMP_Text.color = new Color(1, 1, 1, 0);
                }
            }


            // maybe.. coroutine?
            //CoroutineManager.StartCoroutine(CoDirectingKingSlime()); 
            // or dotween?
            // todo directing action

            DG.Tweening.Sequence sequence = DOTween.Sequence();
            Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
            Vector3 target = new Vector3(0f, 16.5f, -5f); ;
            float moveTime = 2f;

            OnMeetKingSlime -= KingSlimeAction;
            OnMeetKingSlime += KingSlimeAction;

            CoroutineManager.StartCoroutine(CoVirtualCameraMove(original, target, moveTime));


            //sequence.Append(() => { Debug.Log("슬라임이 채워진다..."); })
            //    .OnComplete(() => { _kingSlime = GameObject.Find("bossMonster0"); _kingSlime.SetActive(false); })
            //    .OnComplete(() => { Debug.Log("슬라임이 채워진다..."); })
            //    .OnComplete(() => { Debug.Log("슬라임이 한곳으로 모여 합쳐진다.."); })
            //    .OnComplete(() => { AfterMeetKingSlime(); });
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
        yield return new WaitForSeconds(2.5f);
        GameObject parent = GameObject.Find("Dungeon_00_003");
        GameObject midlePos = GameObject.Find("SpawnKingSlime");
        Vector3 pos = new Vector3(4.056f, 1.514f, -1.776f);
        GameObject scoutSlime = Managers.Resource.Instantiate("BossScene_C0_000", parent.transform);
        scoutSlime.transform.localPosition = pos;
        scoutSlime.GetComponent<Animator>().Play("bossScene_C0_000");
        DG.Tweening.Sequence sequence = DOTween.Sequence();
        yield return new WaitForSeconds(2f);
        GameObject slimeSpawner = Managers.Resource.Instantiate("BossScene_C0_006", parent.transform);
        slimeSpawner.transform.localPosition = new Vector3(4.056f, 0.474f, -2.79f);
        slimeSpawner.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        CoroutineManager.StartCoroutine(CoBright(slimeSpawner, 2f));


        GameObject.Find("SlimeFall1").GetComponent<ParticleSystem>().Play();
        GameObject.Find("SlimeFall2").GetComponent<ParticleSystem>().Play();
        GameObject.Find("SlimeFall3").GetComponent<ParticleSystem>().Play();
        GameObject.Find("SlimeFall4").GetComponent<ParticleSystem>().Play();
        GameObject.Find("SlimeFall5").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < 10; ++i)
        {
            GameObject slime1 = Managers.Resource.Instantiate("BossScene_C0_003", parent.transform);
            slime1.transform.localPosition = new Vector3(UnityEngine.Random.Range(3.5f, 4), 0.7f, -3.045f);
            GameObject slime2 = Managers.Resource.Instantiate("BossScene_C0_004", parent.transform);
            slime2.transform.localPosition = new Vector3(UnityEngine.Random.Range(3.5f, 4), 0.7f, -3.045f);
            GameObject slime3 = Managers.Resource.Instantiate("BossScene_C0_005", parent.transform);
            slime3.transform.localPosition = new Vector3(UnityEngine.Random.Range(4f, 4.5f), 0.7f, -3.045f);

            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime1, midlePos.transform.localPosition, UnityEngine.Random.Range(1, 4)));
            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime2, midlePos.transform.localPosition, UnityEngine.Random.Range(1, 4)));
            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime3, midlePos.transform.localPosition, UnityEngine.Random.Range(1, 4)));
        }
        

        

        yield return new WaitForSeconds(4.2f);

        CoroutineManager.StartCoroutine(CoBlack(slimeSpawner, 2f));


        _kingSlime = GameObject.Find("bossMonster0");
        //_kingSlime.SetActive(false);
        Debug.Log("슬라임이 채워진다...");
        Debug.Log("슬라임이 한곳으로 모여 합쳐진다..");
        yield return new WaitForSeconds(2.2f);
        AfterMeetKingSlime();
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

        while (totalTime <= time)
        {
            float delta = totalTime / time;
            float x = original.transform.localPosition.x + (target.x - original.transform.localPosition.x) * delta;
            float y = original.transform.localPosition.y + (target.y - original.transform.localPosition.y) * delta;
            float z = original.transform.localPosition.z + (target.z - original.transform.localPosition.z) * delta;
            original.transform.localPosition = new Vector3(x, y, z);
            totalTime += Time.deltaTime;
            yield return null;
        }

        original.SetActive(false);
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

    public void AfterMeetKingSlime()
    {
        Debug.Log("킹 슬라임이 울부짖었다!!!");
        Debug.Log("이 함수가 실행되었다!!!!!!!!!!!!");

        Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target = new Vector3(0f, 10f, -5f); ;
        float moveTime = 2f;
        CoroutineManager.StartCoroutine(CoVirtualCameraMove(original, target, moveTime));

        if (_kingSlime != null)
        {
            _kingSlime.SetActive(true);
            _kingSlime.gameObject.GetOrAddComponent<Animator>().Play("Boss_C0_I000");
        }
        //_kingSlime.GetOrAddComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        GameObject tutorialSene = GameObject.Find("UI_TutorialScene");
        if (tutorialSene != null)
        {
            // UI On
            RectTransform[] rectTransforms = tutorialSene.gameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 1; i < rectTransforms.Length; i++)
            {
                //rectTransforms[i].gameObject.SetActive(false);
                Image image = rectTransforms[i].gameObject.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(1, 1, 1, 1);
                }
                TMP_Text tMP_Text = rectTransforms[i].gameObject.GetComponent<TMP_Text>();
                if (tMP_Text != null)
                {
                    tMP_Text.color = new Color(1, 1, 1, 1);
                }
            }
        }

        Managers.Game.OnDirect = false;
    }

    IEnumerator CoDirectingKingSlime()
    {
        yield return null;
        Debug.Log("킹 슬라임이 울부짖었다!!!");
    }

    #endregion
}
