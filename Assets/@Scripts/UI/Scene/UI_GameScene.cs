using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
{
    #region Enum
    //enum Buttons
    //{
    //    //ToTitleButton,
    //    PlayConversation,
    //}

    enum GameObjects
    {
        KeyInventory,
        GreenKey,
        YellowKey,
        RedKey,
    }

    enum Texts
    {
        //PlayerNameText,
        PlayerHPText,
        PlayerAttackText,
        PlayerDefenseText,
        PlayerLevelText,
        MainUIMapNameText,
    }

    enum Images
    {
        MainUIEXPGaugeImage,
        MainUIAuxiliaryHPGaugeImage,
        MainUIOptionAImage,
        MainUIOptionBImage,
        MainUIInventoryAImage,
        MainUIInventoryBImage,
        MainUISwordAImage,
        MainUISwordBImage,
        MainUIWarpAImage,
        MainUIWarpBImage,
        MainUIStatusHPImage,
        LoadingIllustImage,
    }

    #endregion

    public bool isOpenMenuPopup = false;
    public bool isOpenInfoPopup = false;
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        //BindButton(typeof(Buttons));
        BindObject(typeof(GameObjects));
        BindText(typeof(Texts));
        BindImage(typeof(Images));
        #endregion

        Managers.UI.UI_GameScene = this;
        #region PointerEnter&PointerExit
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUISwordAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);

        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(true); }, null, Define.UIEvent.PointerEnter);
        GetImage((int)Images.MainUIWarpAImage).gameObject.BindEvent(() =>
        { GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false); }, null, Define.UIEvent.PointerExit);
        #endregion

        #region OffBImage
        GetImage((int)Images.MainUIOptionBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false);
        #endregion

        Managers.Game.GenerateMap(Managers.Game.PlayerData.CurStageid);
        Managers.Game.PlayerData.MoveSpeed = 1f;
        Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();

        Managers.Sound.Play(Define.Sound.Effect, "MapTransition_SFX");

        //Managers.Game.PlayerData.CurSword = Define.EQUIP_SOWRD_FIRST;
        //Managers.Game.PlayerData.CurShield = 0;

        Managers.Game.Player._keyInventory = GetObject((int)GameObjects.KeyInventory);

        if (PlayerPrefs.GetInt("ISOPENGREENKEY") == 0)
            GetObject((int)GameObjects.GreenKey).SetActive(false);
        if (PlayerPrefs.GetInt("ISOPENYELLOWKEY") == 0)
            GetObject((int)GameObjects.YellowKey).SetActive(false);
        if (PlayerPrefs.GetInt("ISOPENREDKEY") == 0)
            GetObject((int)GameObjects.RedKey).SetActive(false);

        // UI 활성화 여부 체크
        if (PlayerPrefs.GetInt("ISOPENINVENUI") == 0) // 인벤 활성화 x
            OffUIInventory();
        if (PlayerPrefs.GetInt("ISOPENWARPUI") == 0)
            OffUIWarp();
        if (PlayerPrefs.GetInt("ISOPENCLASSUI") == 0)
            OffUISword();

        GetImage((int)Images.MainUIOptionAImage).gameObject.BindEvent(() =>
        {
            GameObject go = GameObject.Find("UI_MenuPopup");
            if (go == null)
            {
                isOpenMenuPopup = true;
                Managers.UI.ShowPopupUI<UI_MenuPopup>();
            }
            else
                go.GetComponent<UI_MenuPopup>().OpenOtherUI();
        });
        GetImage((int)Images.MainUIInventoryAImage).gameObject.BindEvent(OnClickMainUIInventoryAImage);

        //GetButton((int)Buttons.PlayConversation).gameObject.BindEvent(() =>
        //{
        //    if (!Managers.Game.OnBattle)
        //    {
        //        Managers.UI.ShowPopupUI<UI_ConversationPopup>();
        //        Managers.Game.CurEventID = Define.EVENT_SWORD_FIRST;
        //    }
        //});

        Refresh();
        Data.MyVector3 loadPos = Managers.Game.PlayerData.CurPosition;

        //최초 실행 시 스폰 포인트 못 찾는 문제 예외 처리
        if (loadPos.X == 0 && loadPos.Z == 0)
            Managers.Game.Player.SetPlayerPosition(Managers.Game.SpawnPoints[0].position);
        else
        {
            Vector3 playerPos = new Vector3(loadPos.X, loadPos.Y, loadPos.Z);
            Managers.Game.Player.SetPlayerPosition(playerPos);
        }

        //#region Test
        //Managers.Game.Player.SetPlayerPosition(Managers.Game.SpawnPoints[1].position);
        //Managers.Game.PlayerData.CurStageid = 3;
        //Managers.Game.MainCamera.GetComponentInChildren<CameraController>().SetupCameraConfiner();
        //#endregion

        if (PlayerPrefs.GetInt("ISOPENSWORD") == 0)
            GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(false);
        if (PlayerPrefs.GetInt("ISOPENPORTAL") == 0)
            GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(false);

        Managers.Game.OnFadeAction.Invoke(1f);


        if (PlayerPrefs.GetInt("ISFIRST", 1) == 1)
            Managers.Directing.Events.CoPlayTutorial_1();
        else
        {
            Managers.UI.ShowStageNamePopup(1f);
            // 하드코딩
            Managers.Sound.Play(Define.Sound.Bgm, "Chapter0_BGM");
            Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1));
        }

        // 보스를 만나지 않았을 때만 키를 다시 생성
        // 보스를 만나면, 세이브 지점이 달라지므로 키를 다시 생성할 필요가 없다. 
        if (PlayerPrefs.GetInt("ISMEETSWORD") == 1 && PlayerPrefs.GetInt("ISMEETBOSS") == 0)
        {
            // 검 먹고 남은 자리에 있는 key 활성화
            GameObject map = GameObject.Find("Dungeon_00_002");
            GameObject key = map.transform.Find("Items/CItem13").gameObject;
            key.GetComponent<SpriteRenderer>().enabled = true;
            key.GetComponent<BoxCollider>().enabled = true;
            key.SetActive(true);
        }
        return true;
    }

    public void Refresh()
    {
        GetText((int)Texts.MainUIMapNameText).text = Managers.GetString(Managers.Data.StageInfoDic[Managers.Game.PlayerData.CurStageid].DungeonNameScriptID);
        GetText((int)Texts.PlayerLevelText).text = Managers.Game.PlayerData.Level.ToString();
        int level = Managers.Game.PlayerData.Level;
        Managers.Game.PlayerData.Level = Mathf.Max(level, 1);
        level = Mathf.Max(level, 1);

        GetImage((int)Images.MainUIEXPGaugeImage).fillAmount = Managers.Game.PlayerData.CurExp / Managers.Data.PlayerDic[level + 1].NeedExp;
        float hpRatio = Managers.Game.PlayerData.CurHP / Managers.Game.PlayerData.MaxHP;
        //GetImage((int)Images.MainUIAuxiliaryHPGaugeImage).fillAmount = hpRatio;
        GameObject.Find("PlayerHPBarGauge").GetComponent<Image>().fillAmount = hpRatio;
        Managers.Game.KeyInventory.ShowKeySlot(Managers.Game.Player._keyInventory);
        SetPlayerInfo();

        Debug.Log($"{Managers.Game.PlayerData.CurExp} , {Managers.Data.PlayerDic[level + 1].NeedExp}");
        Debug.Log($"ATK : {Managers.Game.PlayerData.Attack} , total ATK : {Managers.Game.PlayerData.Attack + Managers.Data.EquipDic[Managers.Game.PlayerData.CurSword].ATK}, cursword : {Managers.Game.PlayerData.CurSword}");
    }

    int _mask = (1 << (int)Define.Layer.Monster | 1 << (int)Define.Layer.CItem);

    private void Update()
    {
        ShowInfo();

        // ESC 설정창
        OnClickESC();

        // Timer
        StartTimer();

        #region for_test
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Managers.Game.PlayerData.CurExp += 10;
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Managers.Game.PlayerData.Attack += 10000;
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Managers.Game.PlayerData.CurHP -= 10000;
            Refresh();

        }
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Managers.Game.PlayerData.MaxHP += 10000;
            Refresh();
        }
        if (Input.GetKeyDown(KeyCode.F5))
        {
            switch (Managers.Game.PlayerData.MoveSpeed)
            {
                case 1f:
                    Managers.Game.PlayerData.MoveSpeed = 1.5f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
                case 1.5f:
                    Managers.Game.PlayerData.MoveSpeed = 2f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
                case 2f:
                    Managers.Game.PlayerData.MoveSpeed = 1f;
                    Managers.Game.Player.Speed = Managers.Game.PlayerData.MoveSpeed * 5;
                    break;
            }

            Debug.Log($"Managers.Game.CurPlayerData.MoveSpeed : {Managers.Game.PlayerData.MoveSpeed}");
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            GameObject monsters = GameObject.Find("Monsters");
            if (monsters != null) monsters.gameObject.SetActive(false);
            GameObject pillars = GameObject.Find("Pillars");
            if (pillars != null) pillars.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.F7))
        {
            if (Managers.Game.PlayerData.CurSword != Define.EQUIP_SOWRD_FIRST)
                Managers.Game.SwapEquip(Define.EQUIP_SOWRD_FIRST);
            else
                Managers.Game.SwapEquip(Define.EQUIP_SOWRD_FIRST + 1);

            Refresh();
            //Managers.Game.SaveGame();
        }
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.Log("move count : " + PlayerPrefs.GetInt("MOVECOUNT"));
            Debug.Log("death count : " + PlayerPrefs.GetInt("DEATHCOUNT"));
            Debug.Log("paly time : " + PlayerPrefs.GetFloat("PLAYTIME"));
        }
        #endregion
    }

    void ShowInfo()
    {
        if (isOpenMenuPopup)
            return;
        if (isOpenInfoPopup)
            return;

        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 100.0f, _mask);
        Debug.DrawRay(Camera.main.transform.position, ray.direction * 100.0f, Color.red, 1.0f);

        if (raycastHit)
        {
            if (hit.collider.gameObject.layer == (int)Define.Layer.Monster && Managers.Cursor._cursor == CursorType.Search)
            {
                MonsterController monster = hit.collider.gameObject.GetComponent<MonsterController>();
                int id = monster.id;

                UI_MonsterInfo monsterInfo = Managers.UI.MakeSubItem<UI_MonsterInfo>(monster.transform);
                Vector3 monsterInfoPos = monsterInfo.gameObject.transform.position;

                isOpenInfoPopup = true;
                monsterInfo.Position = Util.ScreenToWorldCood(Input.mousePosition);

            }
            else if (hit.collider.gameObject.layer == (int)Define.Layer.CItem && Managers.Cursor._cursor == CursorType.Search)
            {
                ConsumableItem cItem = hit.collider.gameObject.GetComponent<ConsumableItem>();
                int id = cItem.id;

                UI_CItemInfo cItemInfo = Managers.UI.MakeSubItem<UI_CItemInfo>(cItem.transform);
                isOpenInfoPopup = true;
                cItemInfo.Position = Util.ScreenToWorldCood(Input.mousePosition);

            }
        }
    }

    void OnClickESC()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !Managers.Game.OnBattle)
        {
            GameObject go = GameObject.Find("UI_MenuPopup");
            if (Managers.UI.GetPopupCount() > 0 && go == null)
            {
                //go.GetComponent<UI_MenuPopup>().OpenOtherUI();
                Managers.UI.ClosePopupUI();
                Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
            }
            else if (go != null)
            {
                go.GetComponent<UI_MenuPopup>().OpenOtherUI();
                Managers.Sound.Play(Define.Sound.Effect, "SettingMenuUI_Back_SFX");
            }
            else
            {
                isOpenMenuPopup = true;
                Managers.UI.ShowPopupUI<UI_MenuPopup>();
            }
        }
    }

    /// <summary>
    /// �������� �÷��̾� ������ �����ϴ� �Լ�
    /// �÷��̾� ������ �߰��Ǹ� ���Լ� ���� �߰��Ǿ����.
    /// </summary>
    public void SetPlayerInfo()
    {
        //GetText((int)Texts.PlayerNameText).text = "PlayerName";
        GetText((int)Texts.PlayerHPText).text = $"{Managers.Game.PlayerData.CurHP} / {Managers.Game.PlayerData.MaxHP}";
        GetText((int)Texts.PlayerAttackText).text = $"{Managers.Game.PlayerData.Attack}";
        GetText((int)Texts.PlayerDefenseText).text = $"{Managers.Game.PlayerData.Defence}";

        float textWidth = GetText((int)Texts.PlayerHPText).preferredWidth;

        // 이미지의 RectTransform 가져오기
        RectTransform imageRect = GetImage((int)Images.MainUIStatusHPImage).GetComponent<RectTransform>();
        // 이미지의 가로 크기를 텍스트 너비 + 여백으로 설정
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + 30f);

    }

    public void OnClickMainUIInventoryAImage()
    {
        if (GameObject.Find("UI_InvenPopup") == null)
            Managers.UI.ShowPopupUI<UI_InvenPopup>();
        else
            Managers.UI.ClosePopupUI();
    }

    public void OffUIInventory()
    {
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
    }

    public void OnUIInventory()
    {
        PlayerPrefs.SetInt("ISOPENINVENUI", 1);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIInventoryBImage).gameObject.SetActive(false);
    }

    public void OffUISword()
    {
        GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUISwordBImage).gameObject.SetActive(false);
    }

    public void OffUIWarp()
    {
        GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(false);
        GetImage((int)Images.MainUIWarpBImage).gameObject.SetActive(false);
    }

    public void OffUI()
    {
        OffUIInventory();
        OffUISword();
        OffUIWarp();
    }

    public void OnUI()
    {
        GetImage((int)Images.MainUIOptionAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIInventoryAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUISwordAImage).gameObject.SetActive(true);
        GetImage((int)Images.MainUIWarpAImage).gameObject.SetActive(true);
    }

    public void StartTimer()
    {
        float playTime = PlayerPrefs.GetFloat("PLAYTIME", 0);
        playTime += Time.deltaTime;
        PlayerPrefs.SetFloat("PLAYTIME", playTime);
    }

    /// <summary>
    /// 가짜 로딩창 만드는 함수
    /// </summary>
    /// <returns></returns>
    public IEnumerator CoShowLoadingIllust()
    {
        yield return null;
        Managers.Game.OnInputLock = true;

        // 일러스트 표현
        if (Managers.Game.GameScene == null)
            yield return null;

        int randValue = UnityEngine.Random.Range(1, 7);
        GetImage((int)Images.LoadingIllustImage).color = new Color(0, 0, 0, 1);
        GetImage((int)Images.LoadingIllustImage).sprite = Managers.Resource.Load<Sprite>($"LoadingIllust{randValue}");
        float timer = 0f;
        float duration = 2f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            GetImage((int)Images.LoadingIllustImage).color = new Color(t, t, t, 1);
            //// 소리끄기
            //Managers.Sound.SetVolume(0);
            Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1) - t);
            Managers.Sound.SetEffectVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1) - t);

            yield return null;
        }

        //Image image = Managers.Game.GameScene.ShowLoadingIllust(randValue);
        yield return new WaitForSeconds(UnityEngine.Random.Range(1, 2));

        timer = 0f;
        while (timer < duration / 2)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / (duration / 2));
            GetImage((int)Images.LoadingIllustImage).color = new Color(1 - t, 1 - t, 1 - t, 1);

            yield return null;
        }

        Managers.Game.OnInputLock = false;

        // 일러스트 끄기
        GetImage((int)Images.LoadingIllustImage).color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// 마검방 들어갈때 삽화 애니 연출
    /// </summary>
    /// <returns></returns>
    public IEnumerator CoShowMagicSwordAni()
    {
        yield return null;
        Managers.Game.OnInputLock = true;
        // 소리끄기
        //Managers.Sound.SetVolume(0);
        // 일러스트 표현
        if (Managers.Game.GameScene == null)
            yield return null;

        GetImage((int)Images.LoadingIllustImage).color = new Color(0, 0, 0, 1);
        GetImage((int)Images.LoadingIllustImage).sprite = Managers.Resource.Load<Sprite>($"ForestIllust");
        GameObject UI_LoadingIllustImage = Managers.Resource.Instantiate($"UI_LoadingIllustImage", gameObject.transform);
        UI_LoadingIllustImage.GetComponent<Image>().sprite = Managers.Resource.Load<Sprite>($"ForestIllust");

        float timer1 = 0f;
        while (timer1 < 2)
        {
            timer1 += Time.deltaTime;
            float t = Mathf.Clamp01(timer1 / 1);
            UI_LoadingIllustImage.GetComponent<Image>().color = new Color(t, t, t, 1);
            //// 소리끄기
            //Managers.Sound.SetVolume(0);
            Managers.Sound.SetBGMVolume(PlayerPrefs.GetFloat("CURBGMSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1) - t);
            Managers.Sound.SetEffectVolume(PlayerPrefs.GetFloat("CUREFFECTSOUND", 1) * PlayerPrefs.GetFloat("SAVESOUND", 1) - t);

            yield return null;
        }

        GameObject go = Managers.Resource.Instantiate($"UI_LoadingIllustImage", gameObject.transform);
        go.GetComponent<Image>().sprite = Managers.Resource.Load<Sprite>($"ForestColorIllust");
        go.GetComponent<Image>().color = Color.white;

        float timer = 0f;
        float duration = 4f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // imageA는 점점 투명해지고, imageB는 점점 불투명해짐
            SetImageAlpha(UI_LoadingIllustImage.GetComponent<Image>(), 1f - t);
            SetImageAlpha(go.GetComponent<Image>(), t);

            yield return null;
        }
        //Image image = Managers.Game.GameScene.ShowLoadingIllust(randValue);
        yield return new WaitForSeconds(1);

        timer = 0f;
        while (timer < duration / 2)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / (duration / 2));
            go.GetComponent<Image>().color = new Color(1 - t, 1 - t, 1 - t, 1);

            yield return null;
        }

        Managers.Game.OnInputLock = false;

        // 일러스트 끄기
        GetImage((int)Images.LoadingIllustImage).color = new Color(1, 1, 1, 0);
        Destroy(go);
        Destroy(UI_LoadingIllustImage);
    }

    /// <summary>
    /// Image의 색상 알파값을 설정하는 헬퍼 함수
    /// </summary>
    private void SetImageAlpha(Image img, float alpha)
    {
        if (img != null)
        {
            Color col = img.color;
            col.a = alpha;
            img.color = col;
        }
    }
}
