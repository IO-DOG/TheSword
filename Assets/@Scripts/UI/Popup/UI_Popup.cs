using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Popup : UI_Base
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        Managers.UI.SetCanvas(gameObject, true);
        return true;
    }

    public virtual void ClosePopupUI()
    {
        Managers.UI.ClosePopupUI(this);
    }

    /// <summary>
    /// Esc 가 눌렸고 이 창이 맨 위다. Esc 를 받는 곳은 씬마다 하나뿐이고(UI_GameScene, 타이틀은
    /// UI_TitleScene), 그곳이 맨 위 창에게만 이것을 묻는다.
    ///   true  — 처리했다. 제 닫는 길로 닫았거나, Esc 로 닫히면 안 되는 창이라 삼켰다.
    ///   false — Esc 를 쓰지 않는 창이다(전투창·층 이름처럼 저절로 사라지는 것). 씬이 그 위에 메뉴를 열 수 있다.
    ///
    /// 예전에는 씬 UI 가 맨 위 창을 그냥 걷어냈다. 가이드·보스방 확인·대화·게임오버 창이 잠금 플래그를
    /// 켠 채로 사라져서 플레이어가 영영 움직이지 못했다. 창을 닫는 것은 그 창의 일이다.
    /// </summary>
    public virtual bool OnEscape() => false;
}
