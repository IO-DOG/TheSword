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

        Managers.Game.PlayerData.IsContractedSword = true;
        Managers.Game.Player.SetState(Define.PlayerState.IdleFront);
        Managers.Game.Player._moveDir = Define.MoveDir.Down;
        Managers.Game.Player._isEquiptWeapon = true;
        Managers.Game.Player._isEquiptShield = true;
        Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST + 1;
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
        if (_clearKingSlime == false)
        {
            _clearKingSlime = true;
            _kingSlime = GameObject.Find("bossMonster0");
            _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = false;
        }

        if (Managers.Game.Player.gameObject.transform.position.x < 303.5f || Managers.Game.Player.gameObject.transform.position.x > 304.2f
            || Managers.Game.Player.gameObject.transform.position.z < -7f) // 하드코딩. 일단 놔두자.
            return;

        Managers.Game.OnMeetKingSlime = true;
        Managers.Game.OnDirect = true;

        // 주인공을 길 중간 위치로 이동
        // 주인공이 정면을 바라보도록
        // 카메라 워킹 및 UI사라짐
        // 카메라 흔들림 등의 연출 효과
        // 연출이 끝나면 UI 활성화
        GameObject gameScene = Managers.Game.GameScene.gameObject;
        if (gameScene != null)
        {
            // UI Off
            RectTransform[] rectTransforms = gameScene.gameObject.GetComponentsInChildren<RectTransform>();
            for (int i = 1; i < rectTransforms.Length; i++)
            {
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

            DG.Tweening.Sequence sequence = DOTween.Sequence();
            Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
            Vector3 target = new Vector3(0f, 17.5f, -5f); ;
            float moveTime = 2f;

            GameObject parent = GameObject.Find("Dungeon_00_003");
            GameObject scoutSlime = Managers.Resource.Instantiate("BossScene_C0_000", parent.transform);
            Vector3 pos = new Vector3(3.547f, 3.123f, -2f);
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
        Vector3 pos = new Vector3(3.547f, 3.123f, -2f);
        GameObject scoutSlime = GameObject.Find("BossScene_C0_000");
        Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(0f, 17.5f, -5f);
        scoutSlime.transform.localPosition = pos;


        Vector3 scoutSlimeMoveDest = new Vector3(pos.x + 0.5f, pos.y, pos.z);
        CoroutineManager.StartCoroutine(CoMoveToDest(scoutSlime, scoutSlimeMoveDest, 2.5f));


        yield return new WaitForSeconds(2.5f);

        scoutSlime.GetComponent<Animator>().Play("bossScene_C0_001");

        yield return new WaitForSeconds(2f);

        // camera slow down
        Vector3 original = Camera.main.GetComponentInChildren<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineTransposer>().m_FollowOffset;
        Vector3 target = new Vector3(0f, 14.5f, -5f); ;
        float moveTime = 10f;
        CoroutineManager.StartCoroutine(CoVirtualCameraMove(original, target, moveTime));

        GameObject.Find("SlimeFall4").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 1.5f));
        GameObject.Find("SlimeFall2").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 1.5f));
        GameObject.Find("SlimeFall3").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1f));
        GameObject.Find("SlimeFall1").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.5f));
        GameObject.Find("SlimeFall5").GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(2f);

        GameObject slimeSpawner = Managers.Resource.Instantiate("BossScene_C0_006", parent.transform);
        slimeSpawner.transform.localPosition = new Vector3(3.94f, 0.474f, -2.43f);
        slimeSpawner.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        GameObject slimeSpawner2 = Managers.Resource.Instantiate("BossScene_C0_006", parent.transform);
        slimeSpawner2.transform.localPosition = new Vector3(5.26f, 0.41f, -3f);
        slimeSpawner2.transform.rotation = Quaternion.Euler(6.73f, 39.188f, 0.649f);
        slimeSpawner2.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        GameObject slimeSpawner3 = Managers.Resource.Instantiate("BossScene_C0_006", parent.transform);
        slimeSpawner3.transform.localPosition = new Vector3(2.5f, 0.48f, -3f);
        slimeSpawner3.transform.rotation = Quaternion.Euler(14.75f, -53.521f, -7.414f);
        slimeSpawner3.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        GameObject slimeSpawner4 = Managers.Resource.Instantiate("BossScene_C0_006", parent.transform);
        slimeSpawner4.transform.localPosition = new Vector3(2f, 0.7f, -4.6f);
        slimeSpawner4.transform.rotation = Quaternion.Euler(34.123f, -90, 0);
        slimeSpawner4.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        GameObject slimeSpawner5 = Managers.Resource.Instantiate("BossScene_C0_006", parent.transform);
        slimeSpawner5.transform.localPosition = new Vector3(5.8f, 0.6f, -4.5f);
        slimeSpawner5.transform.rotation = Quaternion.Euler(35.874f, 90, 0);
        slimeSpawner5.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        CoroutineManager.StartCoroutine(CoBright(slimeSpawner, 3f));
        CoroutineManager.StartCoroutine(CoBright(slimeSpawner2, 3f));
        CoroutineManager.StartCoroutine(CoBright(slimeSpawner3, 3f));
        CoroutineManager.StartCoroutine(CoBright(slimeSpawner4, 3f));
        CoroutineManager.StartCoroutine(CoBright(slimeSpawner5, 3f));

        // create fog


        for (int i = 0; i < 7; ++i)
        {
            float randValue = UnityEngine.Random.Range(0f, 0.5f);
            WaitForSeconds delay = new WaitForSeconds(randValue);
            GameObject slime1 = Managers.Resource.Instantiate("BossScene_C0_003", parent.transform);
            slime1.transform.localPosition = new Vector3(UnityEngine.Random.Range(3f, 4.5f), 0.7f, -2.5f);
            yield return delay;

            GameObject slime2 = Managers.Resource.Instantiate("BossScene_C0_004", parent.transform);
            slime2.transform.localPosition = new Vector3(UnityEngine.Random.Range(4.6f, 5.5f), 0.7f, UnityEngine.Random.Range(-2.7f, -3.3f));

            GameObject slime3 = Managers.Resource.Instantiate("BossScene_C0_005", parent.transform);
            slime3.transform.localPosition = new Vector3(UnityEngine.Random.Range(2.2f, 3.0f), 0.7f, UnityEngine.Random.Range(-3.6f, -2.6f));
            yield return delay;

            GameObject slime4 = Managers.Resource.Instantiate("BossScene_C0_004", parent.transform);
            slime4.transform.localPosition = new Vector3(2.1f, 0.7f, UnityEngine.Random.Range(-3.6f, -5f));

            GameObject slime5 = Managers.Resource.Instantiate("BossScene_C0_003", parent.transform);
            slime5.transform.localPosition = new Vector3(5.5f, 0.7f, UnityEngine.Random.Range(-3.6f, -5f));
            yield return delay;

            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime1, midlePos.transform.localPosition, 4));
            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime2, midlePos.transform.localPosition, 4));
            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime3, midlePos.transform.localPosition, 4));
            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime4, midlePos.transform.localPosition, 4));
            CoroutineManager.StartCoroutine(CoMoveToKingSlimeMidlePos(slime5, midlePos.transform.localPosition, 4));
        }

        CoroutineManager.StartCoroutine(CoBlack(slimeSpawner, 3f));
        CoroutineManager.StartCoroutine(CoBlack(slimeSpawner2, 3f));
        CoroutineManager.StartCoroutine(CoBlack(slimeSpawner3, 3f));
        CoroutineManager.StartCoroutine(CoBlack(slimeSpawner4, 3f));
        CoroutineManager.StartCoroutine(CoBlack(slimeSpawner5, 3f));

        _kingSlime = GameObject.Find("bossMonster0");
        GameObject kingSlimeAction = GameObject.Find("KingSlimeAction");
        kingSlimeAction.GetComponent<Animator>().Play("KingSlimeAction");

        {
            WaitForSeconds delay = new WaitForSeconds(0.5f);
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

        // flash bang
        CoroutineManager.StartCoroutine(CoFlashBang(1f));
        yield return new WaitForSeconds(0.6f);
        GameObject image = new GameObject();
        image.AddComponent<Image>();
        GameObject gameScene = Managers.Game.GameScene.gameObject;
        image.transform.parent = gameScene.transform;
        RectTransform parentRect = image.transform.parent.GetComponent<RectTransform>();
        // 앵커를 부모의 전체 크기에 맞게 설정
        RectTransform rectTransform = image.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        // 오프셋을 0으로 설정하여 부모에 맞게 확장
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
        image.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        Util.TriggerFlash(image.GetComponent<Image>(), 1, 1.2f);

        yield return new WaitForSeconds(1.6f);

        image.SetActive(false);
        // create boss
        kingSlimeAction.SetActive(false);
        _kingSlime.GetOrAddComponent<SpriteRenderer>().enabled = true;
        image.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        GameObject.Find("Effects_00")?.SetActive(false);

        AfterMeetKingSlime();
    }

    public IEnumerator CoFlashBang(float time)
    {
        GameObject go = GameObject.Find("Directional Light");
        Light light = go.GetComponent<Light>();
        float start = light.intensity;
        float totalTime = 0;
        while (totalTime <= time)
        {
            float delta = totalTime / time;
            light.intensity = 100 * delta;
            totalTime += Time.deltaTime;
            yield return null;
        }
        light.intensity = 0f;
        yield return new WaitForSeconds(1f);
        light.intensity = start;
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

    public void AfterMeetKingSlime()
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
        go.transform.position = new Vector3(3.83f, 0.66f, -3.75f);
        go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

    }

    #endregion
}
