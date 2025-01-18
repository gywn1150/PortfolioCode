
/*===========================================================================================
*
*   UIBase 사용 예시 
* 
*                   ┌ PanelBase(예시) ◁- Panel_Setting(예시)
*   UIBase[부모]  ◁-
*                   └ PopupBase(예시) (현재 스크립트) ◁- Popup_ExpositionFileUpload (예시)
*                                 
*   - Popup UI의 Base가 되는 클래스. Open & Close 기능, 확인 & 취소 메서드 추가
*   - Popup : 화면의 일부만 가리는 UI, 여러 개가 동시에 뜰 수 있음, Panel보다 위에 뜸
* 
===========================================================================================*/
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Assets._Launching.DEV.Script.Framework.Network.WebPacket;
using UnityEngine.Localization.Components;
using System.Collections.Generic;
using MEC;
using System;

public class PopupBase : UIBase
{
    #region 변수
    protected Button btn_PopupExit;
    protected Animator popupAnimator;

    private readonly string _popupOpen = "PopupOpen";
    private readonly string _popupClose = "PopupClose";
    private readonly string PATH_ANIM = "BuildAssetBundle/UI/Animation/PopupCommon";
    #endregion

    protected override void SetMemberUI()
    {
        btn_PopupExit = GetUI_Button(nameof(btn_PopupExit), SceneLogic.instance.Back);

        popupAnimator = GetComponent<Animator>();
        if (popupAnimator == null)
        {
            popupAnimator = gameObject.AddComponent<Animator>();
            popupAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(PATH_ANIM);
        }
    }

    #region Popup Open, Close, Button Callback
    /// <summary>
    /// Popup 활성화 로직
    /// </summary>
    public IEnumerator<float> Co_OpenPopup()
    {
        transform.SetAsLastSibling();

        OpenStartAct();
        yield return Co_OpenStartAct().WaitUntilDone();

        popupAnimator.enabled = true;
        popupAnimator.Play(_popupOpen);
        yield return Timing.WaitForSeconds(.55f);
        popupAnimator.enabled = false;

        OpenEndAct();
        yield return Co_OpenEndAct().WaitUntilDone();
    }

    /// <summary>
    /// Popup 비활성화 로직
    /// </summary>
    public IEnumerator<float> Co_ClosePopup()
    {
        CloseStartAct();
        yield return Co_SetCloseStartAct().WaitUntilDone();

        popupAnimator.enabled = true;
        popupAnimator.Play(_popupClose);
        yield return Timing.WaitForSeconds(.375f);
        popupAnimator.enabled = false;

        CloseEndAct();
        yield return Co_SetCloseEndAct().WaitUntilDone();
    }

    /// <summary>
    /// 확인(Confirm)
    /// </summary>
    protected virtual void OnConfirm()
    {
        SceneLogic.instance.PopPopup();
    }

    /// <summary>
    /// 취소(Cancel)
    /// </summary>
    protected virtual void OnCancel()
    {
        SceneLogic.instance.PopPopup();
    }
    #endregion
}
