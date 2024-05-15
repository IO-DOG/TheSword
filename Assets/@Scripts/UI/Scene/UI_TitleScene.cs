using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_TitleScene : UI_Scene
{
    #region Enum

    enum Objects
    {
        Slider,
    }

    enum Buttons
    {
        TestButton,
        GameSpeedButton,
        InputManagerTestButton,
    }

    #endregion

    bool isPreload = false;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        #region Bind
        BindObject(typeof(Objects));
        BindButton(typeof(Buttons));
        #endregion

        GetObject((int)Objects.Slider).GetComponent<Slider>().value = 0;
        // 테스트용
        GetButton((int)Buttons.TestButton).gameObject.BindEvent(() =>
        {
            if (isPreload)
                Managers.Scene.LoadScene(Define.Scene.SHJTestScene, transform);
        });
        GetButton((int)Buttons.TestButton).gameObject.SetActive(false);
        GetButton((int)Buttons.GameSpeedButton).gameObject.BindEvent(() => {
            if (Managers.Game.GameSpeed == 1)
                Managers.Game.GameSpeed = 2;
            else if (Managers.Game.GameSpeed == 2)
                Managers.Game.GameSpeed = 4;
            else
                Managers.Game.GameSpeed = 1;
        });
        GetButton((int)Buttons.InputManagerTestButton).gameObject.BindEvent(() =>
        {
            if (isPreload)
                Managers.Scene.LoadScene(Define.Scene.InputTestScene, transform);
        });
        GetButton((int)Buttons.InputManagerTestButton).gameObject.SetActive(false);

        return true;
    }

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        Managers.Resource.LoadAllAsync<Object>("PreLoad", (key, count, totalCount) =>
        {
            GetObject((int)Objects.Slider).GetComponent<Slider>().value = (float)count / totalCount;
            if (count == totalCount)
            {
                isPreload = true;
                GetButton((int)Buttons.TestButton).gameObject.SetActive(true);
                GetButton((int)Buttons.InputManagerTestButton).gameObject.SetActive(true);
                Managers.Data.Init();
                Managers.Game.Init();
                // continueData로 플레이어 적용시키기. TODO
            }
        });
    }

}
