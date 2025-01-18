/*===========================================================================================
*
*   UIBase 사용 예시 
* 
*                   ┌ PanelBase(예시) ◁- Panel_Setting(예시)
*   UIBase[부모]  ◁-
*                   └ PopupBase(예시) ◁- Popup_ExpositionFileUpload (예시) (현재 스크립트)
*                                 
*   - Popup 생성 시 UIBase-PopupBase를 상속받아 사용한다
*   - SetMemberUI()를 Override하여 캐싱된 UI를 활용한다
* 
===========================================================================================*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Popup_ExpositionFileUpload : PopupBase
{
    #region 변수
    private Button btn_Registration;
    private CanvasGroup cg_Registration;
    private ToggleGroup togg_Icon;
    private TMP_InputField input_FileName;
    private TMP_InputField input_Link;

    private CSAF_FILEBOX_TYPE curFileType;
    private List<TogglePlus> toggleList = new List<TogglePlus>();
    private Item_ExpositionFileData data;

    private bool isEdit = false; // true: 기존 데이터 수정, false: 신규 데이터 생성
    #endregion

    protected override void SetMemberUI()
    {
        base.SetMemberUI();

        #region TMP_Text
        GetUI_TxtmpMasterLocalizing("txtmp_Title", new MasterLocalData("office_filebox_upload"));
        GetUI_TxtmpMasterLocalizing("txtmp_SelectIcon", new MasterLocalData("office_filebox_icon"));
        GetUI_TxtmpMasterLocalizing("txtmp_FileName", new MasterLocalData("office_filebox_filename"));
        GetUI_TxtmpMasterLocalizing("txtmp_Link", new MasterLocalData("office_filebox_link"));
        GetUI_TxtmpMasterLocalizing("txtmp_Registration", new MasterLocalData("office_filebox_save"));
        GetUI_TxtmpMasterLocalizing("txtmp_Cloud", new MasterLocalData("office_filebox_cloud"));
        GetUI_TxtmpMasterLocalizing("txtmp_File", new MasterLocalData("office_filebox_document"));
        GetUI_TxtmpMasterLocalizing("txtmp_Video", new MasterLocalData("office_filebox_video"));
        #endregion

        #region Button
        btn_Registration = GetUI_Button(nameof(btn_Registration), OnClick_Registration);

        GetUI_Button("btn_PopupExit", Back);
        GetUI_Button("btn_Exit", Back);
        #endregion

        #region ToggleGroup
        togg_Icon = GetUI<ToggleGroup>(nameof(togg_Icon));
        if (togg_Icon != null)
        {
            var togList = togg_Icon.GetComponentsInChildren<TogglePlus>();
            int count = togList.Length;
            for (int i = 0; i < count; i++)
            {
                int capture = i + 1;
                togList[i].SetToggleOnAction(() =>
                {
                    curFileType = (CSAF_FILEBOX_TYPE)capture;
                    if (togg_Icon.allowSwitchOff) togg_Icon.allowSwitchOff = false;
                    ActiveRegisterInteract();
                });
                toggleList.Add(togList[i]);
            }
        }
        #endregion

        #region TMP_InputField
        input_FileName = GetUI_TMPInputField(nameof(input_FileName), OnValueChanged_FileName);
        input_Link = GetUI_TMPInputField(nameof(input_Link), OnValueChanged_Link);
        #endregion

        #region etc
        cg_Registration = GetChildGObject("go_Registration").GetComponent<CanvasGroup>();
        if (cg_Registration != null)
        {
            cg_Registration.interactable = false;
        }
        #endregion
    }

    #region Init
    /// <summary>
    /// 데이터가 없을 시(신규 업로드)일 때도 해당 메소드 호출 필요
    /// </summary>
    public void SetData(Item_ExpositionFileData _data = null)
    {
        isEdit = _data != null;
        data = _data ?? new Item_ExpositionFileData()
        {
            boothId = (SceneLogic.instance as Scene_Room_Exposition_Booth).GetBooth().id
        };

        SetPopupUI();
    }

    private void SetPopupUI()
    {
        if (togg_Icon != null)
        {
            curFileType = (CSAF_FILEBOX_TYPE)data.fileBoxType;
            var isNone = curFileType == CSAF_FILEBOX_TYPE.None;

            togg_Icon.SetAllTogglesOff();
            togg_Icon.allowSwitchOff = isNone;
            if (!isNone)
            {
                toggleList[data.fileBoxType - 1].SetToggleIsOnWithoutNotify(true);
            }
        }

        if (input_FileName != null)
        {
            input_FileName.text = data.fileName;
            Util.SetMasterLocalizing(input_FileName.placeholder, new MasterLocalData("office_filebox_name_input"));
        }

        if (input_Link != null)
        {
            input_Link.text = data.link;
            Util.SetMasterLocalizing(input_Link.placeholder, new MasterLocalData("office_filebox_link_input"));
        }
    }
    #endregion

    /// <summary>
    /// 파일함 등록 및 수정
    /// </summary>
    private void OnClick_Registration()
    {
        if (!ActiveRegisterInteract()) return;

        if (!isEdit)
        {
            Single.Web.RegisterCSAFFileBox(data.boothId, (int)curFileType, input_FileName.text, ContainsHttpCheck(input_Link.text), FileBoxResponse); // 신규 등록
        }
        else
        {
            Single.Web.EditCSAFFilebox(data.boothId, data.id, (int)curFileType, input_FileName.text, ContainsHttpCheck(input_Link.text), FileBoxResponse); // 수정
        }
    }

    private string ContainsHttpCheck(string text) => !text.Contains("http") ? $"https://{text}" : text;

    private void FileBoxResponse(CSAFFilebox res)
    {
        PopPopup();
        SceneLogic.instance.GetPanel<Panel_ExpositionFileBox>().LoadData();
    }

    /// <summary>
    /// 파일명 실시간 제한
    /// 한글, 영문, 숫자, 띄어쓰기 포함 20자 이상 제한
    /// </summary>
    private string oriStr;
    private void OnValueChanged_FileName(string str)
    {
        if (str.Length > 20)
        {
            input_FileName.text = oriStr;
        }
        oriStr = str;

        ActiveRegisterInteract();
    }

    /// <summary>
    /// 링크 실시간 제한
    /// 별도 제한 없음
    /// </summary>
    private void OnValueChanged_Link(string str)
    {
        ActiveRegisterInteract();
    }

    /// <summary>
    /// 제한에 따른 등록 버튼 비/활성화
    /// </summary>
    private bool ActiveRegisterInteract()
    {
        var b = Util.FileNameRestriction(input_FileName.text) &&
                !string.IsNullOrEmpty(input_Link.text) &&
                curFileType != CSAF_FILEBOX_TYPE.None;

        return cg_Registration.interactable = b;
    }
}
