/*===========================================================================================
*
*   UIBase 사용 예시 
* 
*                   ┌ PanelBase(예시) ◁- Panel_Setting(예시) (현재 스크립트) 
*   UIBase[부모]  ◁-
*                   └ PopupBase(예시) ◁- Popup_ExpositionFileUpload (예시)
*                                 
*   - Panel 생성 시 UIBase-PanelBase를 상속받아 사용한다
*   - SetMemberUI()를 Override하여 캐싱된 UI를 활용한다
* 
===========================================================================================*/

using FrameWork.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class Panel_Setting : PanelBase
{
    #region 변수
    public string forceTap = Cons.Tap_System;

    private ToggleGroup togg_Tab;
    private List<TogglePlus> toggleList = new List<TogglePlus>();
    #endregion

    protected override void SetMemberUI()
    {
        base.SetMemberUI();

        #region Button
        GetUI_Button("btn_Back", Back);
        #endregion

        #region TMP_Text
        GetUI_TxtmpMasterLocalizing("txtmp_SettingTitle", new MasterLocalData("9000"));
        GetUI_TxtmpMasterLocalizing("txtmp_System", new MasterLocalData("9004"));
        GetUI_TxtmpMasterLocalizing("txtmp_Account", new MasterLocalData("9001"));
        #endregion

        #region ToggleGroup
        togg_Tab = GetUI<ToggleGroup>("togg_Tab");
        if (togg_Tab != null)
        {
            togg_Tab.enabled = false;
            int count = togg_Tab.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                TogglePlus tog = togg_Tab.transform.GetChild(i).GetComponent<TogglePlus>();
                string tapName = tog.name.Replace("togplus", "Tap");
                tog.SetToggleOnAction(() => { ChangeTap(tapName); });
                toggleList.Add(tog);
            }
            togg_Tab.enabled = true;
        }
        #endregion

        #region etc
        var go_PcManual = toggleList.FirstOrDefault(x => x.name == "togplus_PcManual").gameObject;
        var tap_PcManual = GetTap<Tap_PcManual>().gameObject;

#if UNITY_STANDALONE || UNITY_EDITOR
            if (tap_PcManual != null) tap_PcManual.SetActive(true);
            if (go_PcManual != null) go_PcManual.SetActive(true);

            GetUI_TxtmpMasterLocalizing("txtmp_PcManual", new MasterLocalData("arzphone_setting_title_shortcutkey"));
#elif UNITY_ANDROID || UNITY_IOS
            if (tap_PcManual != null) tap_PcManual.SetActive(false);
            if (go_PcManual != null) go_PcManual.SetActive(false);
#endif
        #endregion
    }

    #region 초기화
    protected override void OnEnable()
    {
        // default : Tap_System
        SetOpenStartCallback(() =>
        {
            string togName = forceTap.Replace("Tap", "togplus");
            TogglePlus toggle = toggleList.FirstOrDefault(x => x.name == togName);
            if (toggle != null)
            {
                toggle.SetToggleIsOn(true);
            }
            forceTap = Cons.Tap_System;
        });
    }
    #endregion
}
