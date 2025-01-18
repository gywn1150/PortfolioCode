/*===========================================================================================
*
*   UIBase 사용 예시
*   
*                   ┌ PanelBase(예시) (현재 스크립트) ◁- Panel_Setting(예시)
*   UIBase[부모]  ◁-
*                   └ PopupBase(예시) ◁- Popup_ExpositionFileUpload (예시)
*                                 
*   - Panel UI의 Base가 되는 클래스. Open & Close 기능 추가
*   - Panel : 화면 전체를 가리는 UI, 1개만 활성화 됨, Popup보다 늘 뒤에 뜸
* 
===========================================================================================*/

using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PanelBase : UIBase
{
    protected override void SetMemberUI()
    {
        base.SetMemberUI();
    }

    #region Panel Open, Close
    /// <summary>
    /// Panel 활성화 로직
    /// </summary>
    public IEnumerator<float> Co_OpenPanel(bool isShowAnimation = true)
    {
        transform.SetAsLastSibling();

        OpenStartAct();
        yield return Co_OpenStartAct().WaitUntilDone();

        if (isShowAnimation)
        {
            yield return Single.Scene.Co_FadeIn(2.5f).WaitUntilDone();
        }

        OpenEndAct();
        yield return Co_OpenEndAct().WaitUntilDone();
    }

    /// <summary>
    /// Panel 비활성화 로직
    /// </summary>
    public IEnumerator<float> Co_ClosePanel(bool isShowAnimation = true)
    {
        if (!gameObject.activeSelf)
        {
            yield break;
        }

        CloseStartAct();
        yield return Co_SetCloseStartAct().WaitUntilDone();

        if (isShowAnimation)
        {
            yield return Single.Scene.Co_FadeOut(2.5f).WaitUntilDone();
        }

        CloseEndAct();
        yield return Co_SetCloseEndAct().WaitUntilDone();
    }
    #endregion
}

