
/*===========================================================================================
*
*   하이라키 UI 캐싱
*                                 ┌ PanelBase(예시) ◁- Panel_Setting(예시)
*   UIBase[부모] (현재 스크립트) ◁-
*                                 └ PopupBase(예시) ◁- Popup_ExpositionFileUpload (예시)
*                                 
*   - 해당 스크립트가 부착된 오브젝트 기준 
*     하위 자식들을 순회하며 규정된 Header가 어미에 붙은 오브젝트를 캐싱
*   - UI 캐싱이 필요할 시 상속
*     SetMemberUI() 가상 메서드를 override하여 캐싱된 데이터 활용
*   - 오브젝트 비/활성화 시의 처리를 위한 Action 등록 메서드,
*     Back 버튼 기능 메서드
* 
===========================================================================================*/

using FrameWork.UI;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum UI_HEADER
{
    tap, // Tap
    txtmp, // TextMeshPro
    img, // Image
    btn, // Button
    tog, // Toggle
    togg, // ToggleGroup
    sld, // Slider
    input, // TMP_InputField
    sview, // ScrollView (ScrollRect)
    sbar, // Scrollbar
    drop, // TMP_Dropdown
    go, // GameObject
}

public class UIBase : MonoBehaviour
{
    private bool _isInit = false;

    protected GameObject _myGameObject = default;
    protected string _myUIName;

    public bool dontSetActiveFalse = false;
    [HideInInspector] public CanvasGroup canvasGroup;
    public Action BackAction_Custom { get; set; } // 뒤로가기 시 실행 Action

    private Dictionary<string, MonoBehaviour> _dicUI = new Dictionary<string, MonoBehaviour>(); // 자식 하이라키 UI 캐싱
    private Dictionary<string, GameObject> _dicGO = new Dictionary<string, GameObject>(); // 자식 하이라키 GameObject 캐싱 : "go_" 로 시작하는 GameObject

    #region Init
    protected virtual void Awake()
    {
        Initialize();
    }

    /// <summary>
    ///  UI 최초 캐싱
    /// </summary>
    public virtual void Initialize()
    {
        if (_isInit)
        {
            return;
        }
        _isInit = true;

        if (!GetComponent<CanvasGroup>())
        {
            gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup = GetComponent<CanvasGroup>();

        AddUI(transform); // 캐싱
        SetMemberUI();

        _myUIName = gameObject.name.Replace("(Clone)", "");

        _myGameObject = gameObject;
        if (!dontSetActiveFalse)
        {
            _myGameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 자식 클래스에서 override 하여 캐싱 데이터 활용
    /// </summary>
    protected virtual void SetMemberUI() { }

    protected virtual void Start() { }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable() { }

    private void OnDestroy()
    {
        RemoveDropDownLocalizeHandler();
    }
    #endregion

    #region Tap
    /// <summary>
    /// Tap : 한 개의 UI 내부에서 계층의 Stack 없이 변경되는 자식 UI 단위
    /// </summary>

    private List<UIBase> tapList = new List<UIBase>();
    private string curTapName = string.Empty;
    private UIBase curActiveTap = null;

    /// <summary>
    /// 지정한 Tap으로 변경
    /// </summary>
    public UIBase ChangeTap(string changeTapName = "")
    {
        return ChangeTap<UIBase>(changeTapName);
    }

    public virtual T ChangeTap<T>() where T : UIBase
    {
        // JIT 컴파일러 검색
        return ChangeTap<T>(typeof(T).Name);
    }

    private T ChangeTap<T>(string changeTapName) where T : UIBase
    {
        T t = default;
        string oldTapName = curTapName;

        for (int i = 0; i < tapList.Count; i++)
        {
            UIBase tap = tapList[i];

            if (tap.name == oldTapName) // 현재 Tap일 시 비활성화
            {
                tap.gameObject.SetActive(false);
            }

            if (tap.name == changeTapName) // 변경 타깃 Tap일 시
            {
                tap.TryGetComponent(out t);
                tap.gameObject.SetActive(true);

                curActiveTap = tap;
                curTapName = changeTapName;
                continue;
            }
        }
        return t;
    }

    /// <summary>
    /// Tap 가져오기
    /// </summary>
    public GameObject GetTap(string tapName)
    {
        return GetTap<UIBase>(tapName).gameObject;
    }

    public T GetTap<T>()
    {
        return GetTap<T>(typeof(T).Name);
    }

    public T GetTap<T>(string tapName)
    {
        for (int i = 0; i < tapList.Count; i++)
        {
            if (tapName.ToLower() == tapList[i].name.ToLower())
            {
                return tapList[i].GetComponent<T>();
            }
        }
        return default;
    }

    /// <summary>
    /// 현재 활성화된 Tap 반환
    /// </summary>
    public UIBase GetActiveTap()
    {
        return curActiveTap;
    }
    #endregion

    #region Set UI
    /// <summary>
    /// 하위 하이라키를 탐색하면서, 사전 합의된 UI 이름과 비교해 UI Component 찾기
    /// </summary>
    private void AddUI(Transform parent)
    {
        for (int i = 0; i < parent.childCount; ++i)
        {
            Transform child = parent.GetChild(i);
            string[] splits = child.name.Split('_');

            bool isContinue = SetUI(splits[0], child); //코어코드
            if (isContinue && child.childCount > 0)
            {
                AddUI(child);
            }
        }
    }

    /// <summary>
    /// UI 딕셔너리에 캐싱
    /// </summary>
    protected virtual bool SetUI(string header, Transform child)
    {
        MonoBehaviour _childUI = null;

        if (Enum.TryParse<UI_HEADER>(header, true, out var _header))
        {
            switch (_header)
            {
                case UI_HEADER.txtmp: _childUI = child.GetComponent<TMP_Text>(); break;
                case UI_HEADER.img: _childUI = child.GetComponent<Image>(); break;
                case UI_HEADER.btn: _childUI = child.GetComponent<Button>(); break;
                case UI_HEADER.tog: _childUI = child.GetComponent<Toggle>(); break;
                case UI_HEADER.togg: _childUI = child.GetComponent<ToggleGroup>(); break;
                case UI_HEADER.sld: _childUI = child.GetComponent<Slider>(); break;
                case UI_HEADER.input: _childUI = child.GetComponent<TMP_InputField>(); break;
                case UI_HEADER.sview: _childUI = child.GetComponent<ScrollRect>(); break;
                case UI_HEADER.sbar: _childUI = child.GetComponent<Scrollbar>(); break;
                case UI_HEADER.drop: _childUI = child.GetComponent<TMP_Dropdown>(); break;
                case UI_HEADER.go:
                    {
                        GameObject _go = child.gameObject;

                        if (!_dicGO.ContainsKey(child.name))
                        {
                            _dicGO.Add(child.name, _go);
                        }
                    }
                    break;
                case UI_HEADER.tap:
                    {
                        UIBase _tap = child.GetComponent<UIBase>();

                        _tap.Initialize();
                        tapList.Add(_tap);
                    }
                    return false;
            }

            if (_childUI)
            {
                if (!_dicUI.ContainsKey(child.name))
                {
                    _dicUI.Add(child.name, _childUI);
                }
            }
        }
        return true;
    }
    #endregion

    #region Get UI
    /// <summary>
    /// 자식 하이라키에서 이름으로 UGUI Object 얻기
    /// </summary>
    /// <typeparam name="T"> UGUI UI 타입지정 : Image, Button, etc...</typeparam>
    /// <param name="hierachyName"> 씬 하이라키 이름 </param>
    /// <returns></returns>
    public T GetUI<T>(string hierachyName) where T : MonoBehaviour
    {
        if (!_dicUI.TryGetValue(hierachyName, out MonoBehaviour childUI))
        {
            DEBUG.LOGWARNING($"GetUI() : 해당 UI가 존재하지 않습니다. - {hierachyName}", eColorManager.UI);
            return null;
        }

        return childUI as T;
    }


    /// <summary>
    /// Image 캐싱, sprite 설정 가능
    /// </summary>
    public Image GetUI_Img(string hierachyName, string spriteName = "")
    {
        var img = GetUI<Image>(hierachyName);

        if (img)
        {
            if (!string.IsNullOrEmpty(spriteName))
            {
                img.sprite = Single.Resources.Load<Sprite>(Cons.Path_Image + spriteName);
            }
        }

        return img;
    }

    /// <summary>
    /// TMP_Text 캐싱, 일반 텍스트 언어 설정 가능
    /// </summary>
    public TMP_Text GetUI_TxtmpMasterLocalizing(string hierachyName, string str = "")
    {
        var txtmp = GetUI<TMP_Text>(hierachyName);

        if (txtmp)
        {
            if (!string.IsNullOrEmpty(str))
            {
                Util.SetMasterLocalizing(txtmp, str);
            }
        }

        return txtmp;
    }

    /// <summary>
    /// TMP_Text 캐싱, 로컬라이징 키값 설정 가능
    /// </summary>
    public TMP_Text GetUI_TxtmpMasterLocalizing(string hierachyName, MasterLocalData masterLocalData)
    {
        var txtmp = GetUI<TMP_Text>(hierachyName);

        if (txtmp)
        {
            if (masterLocalData)
            {
                Util.SetMasterLocalizing(txtmp, masterLocalData);
            }
        }

        return txtmp;
    }

    /// <summary>
    /// Button 캐싱, onClickAction 설정 가능
    /// </summary>
    public Button GetUI_Button(string hierachyName, UnityAction unityAction = null, string soundName = "")
    {
        var btn = GetUI<Button>(hierachyName);

        if (btn)
        {
            btn.onClick.RemoveAllListeners();

            btn.onClick.AddListener(() => Single.Sound.PlayEffect(soundName ?? Cons.click));
            if (unityAction)
            {
                btn.onClick.AddListener(unityAction);
            }
        }

        return btn;
    }

    /// <summary>
    /// TMP_InputField 캐싱, valueChanged와 submit 및 placeholder 설정 가능
    /// </summary>
    public TMP_InputField GetUI_TMPInputField(string hierachyName, UnityAction<string> valueChangedAction = null, UnityAction<string> submitAction = null, MasterLocalData masterLocalData = null)
    {
        var input = GetUI<TMP_InputField>(hierachyName);

        if (input)
        {
            if (valueChangedAction != null)
            {
                input.onValueChanged.AddListener(valueChangedAction);
            }
            if (submitAction != null)
            {
                input.onSubmit.AddListener(submitAction);
            }
            if (masterLocalData != null)
            {
                Util.SetMasterLocalizing(input.placeholder, masterLocalData);
            }
        }

        return input;
    }

    /// <summary>
    /// 자식 하이라키에서 이름으로 GameObject 찾기
    /// </summary>
    public GameObject GetChildGObject(string hierachyName)
    {
        if (!_dicGO.TryGetValue(hierachyName, out GameObject goFind))
        {
            DEBUG.LOGWARNING($"GetGameObject() : GameObject를 찾지 못했습니다. - {hierachyName}", eColorManager.UI);
            return null;
        }

        return goFind;
    }

    #region Dropdown
    private TMP_Dropdown dropdown;
    private List<MasterLocalData> masterLocalData;
    /// <summary>
    /// TMP_Dropdown 캐싱, valueChanged 및 로컬라이징 키값 설정 가능
    /// </summary>
    public TMP_Dropdown GetUI_TMPDropdown(string hierachyName, UnityAction<int> valueChangedAction = null, List<MasterLocalData> masterLocalData = null)
    {
        dropdown = GetUI<TMP_Dropdown>(hierachyName);

        if (dropdown)
        {
            dropdown.onValueChanged.AddListener((idx) => Single.Sound.PlayEffect(Cons.click));

            if (valueChangedAction != null)
            {
                dropdown.onValueChanged.AddListener((idx) => valueChangedAction?.Invoke(idx));
            }

            if (masterLocalData != null)
            {
                this.masterLocalData = masterLocalData;
                DropDownLocalizeHandler();
                AddDropDownLocalizeHandler();
            }
        }

        return dropdown;
    }

    private void DropDownLocalizeHandler()
    {
        Localizing_TMPDropdownOption(dropdown, masterLocalData);
    }

    private void AddDropDownLocalizeHandler() => AppGlobalSettings.Instance.OnChangeLanguage.AddListener(DropDownLocalizeHandler);
    private void RemoveDropDownLocalizeHandler() => AppGlobalSettings.Instance.OnChangeLanguage.RemoveListener(DropDownLocalizeHandler);

    /// <summary>
    /// 드롭다운 로컬라이징 (MasterLocalData)
    /// </summary>
    private void Localizing_TMPDropdownOption(TMP_Dropdown dropdown, List<MasterLocalData> masterLocalData = null)
    {
        if (!dropdown ||
            masterLocalData == null ||
            masterLocalData.Count == 0) return;

        int selectValue = dropdown.value;

        dropdown.Hide();
        dropdown.ClearOptions();
        int count = masterLocalData.Count;
        for (int i = 0; i < count; i++)
        {
            var newData = new TMP_Dropdown.OptionData { text = Util.GetMasterLocalizing(masterLocalData[i]) };
            dropdown.options.Add(newData);
        }
        dropdown.value = selectValue;

        if (dropdown.IsExpanded)
        {
            dropdown.Show();
        }

        Util.SetMasterLocalizing(dropdown.captionText, masterLocalData[dropdown.value]);
    }
    #endregion

    #region Toggle
    /// <summary>
    /// Toggle 캐싱, toggleAction 설정 가능 (on / off 별도)
    /// </summary>
    public Toggle GetUI_Toggle(string togName, UnityAction toggleOnAction = null, UnityAction toggleOffAction = null)
    {
        var tog = GetUI<Toggle>(togName);

        if (tog)
        {
            tog.onValueChanged.AddListener((b) =>
            {
                if (b)
                {
                    Single.Sound.PlayEffect(Cons.click);

                    toggleOnAction?.Invoke();
                }
                else
                {
                    toggleOffAction?.Invoke();
                }
            });
        }

        return tog;
    }

    /// <summary>
    /// Toggle 캐싱, toggleAction 설정 가능 (on / off 통합)
    /// </summary>
    public Toggle GetUI_Toggle(string togName, UnityAction<bool> toggleAction)
    {
        var tog = GetUI<Toggle>(togName);

        if (tog)
        {
            tog.onValueChanged.AddListener((b) =>
            {
                if (b)
                {
                    Single.Sound.PlayEffect(Cons.click);
                }
            });

            if (toggleAction != null)
            {
                tog.onValueChanged.AddListener(toggleAction);
            }
        }

        return tog;
    }
    #endregion
    #endregion

    #region Callback Action
    private Action _openStartCallback = default;
    private Action _openEndCallback = default;
    private Action _closeStartCallback = default;
    private Action _closeEndCallback = default;

    #region UI 비/활성화 시 Action 등록
    /// <summary>
    /// UI 활성화 애니메이션 시작 시 Action 등록
    /// </summary>
    public UIBase SetOpenStartCallback(Action callback)
    {
        _openStartCallback = callback;
        return this;
    }

    /// <summary>
    /// UI 활성화 애니메이션 종료 시 Action 등록
    /// </summary>
    /// <param name="callback"></param>
    public UIBase SetOpenEndCallback(Action callback)
    {
        _openEndCallback = callback;
        return this;
    }

    /// <summary>
    /// UI 비활성화 애니메이션 시작 시 Action 등록
    /// </summary>
    public UIBase SetCloseStartCallback(Action callback)
    {
        _closeStartCallback = callback;
        return this;
    }

    /// <summary>
    /// UI 비활성화 애니메이션 종료 시 Action 등록
    /// </summary>
    /// <param name="callback"></param>
    public UIBase SetCloseEndCallback(Action callback)
    {
        _closeEndCallback = callback;
        return this;
    }
    #endregion

    #region UI 비/활성화 시 Action 실행
    /// <summary>
    /// UI 활성화 애니메이션 시작 시 Action 실행
    /// </summary>
    public virtual void OpenStartAct()
    {
        _openStartCallback?.Invoke();
        _openStartCallback = null;
    }

    /// <summary>
    /// UI 활성화 애니메이션 종료 시 Action 실행
    /// </summary>
    public virtual void OpenEndAct()
    {
        _openEndCallback?.Invoke();
        _openEndCallback = null;
    }

    /// <summary>
    /// UI 비활성화 애니메이션 시작 시 Action 실행
    /// </summary>
    public virtual void CloseStartAct()
    {
        _closeStartCallback?.Invoke();
        _closeStartCallback = null;
    }

    /// <summary>
    /// UI 비활성화 애니메이션 종료 시 Action 실행
    /// </summary>
    public virtual void CloseEndAct()
    {
        _closeEndCallback?.Invoke();
        _closeEndCallback = null;
    }
    #endregion

    #region 비/활성화 애니메니션 시 추가 처리용 가상 함수
    /// <summary>
    /// 활성화 시작 시
    /// </summary>
    protected virtual IEnumerator<float> Co_OpenStartAct()
    {
        yield return Timing.WaitForOneFrame;
    }

    /// <summary>
    /// 활성화 종료 시
    /// </summary>
    protected virtual IEnumerator<float> Co_OpenEndAct()
    {
        yield return Timing.WaitForOneFrame;
    }

    /// <summary>
    /// 비활성화 시작 시
    /// </summary>
    protected virtual IEnumerator<float> Co_SetCloseStartAct()
    {
        yield return Timing.WaitForOneFrame;
    }

    /// <summary>
    /// 비활성화 종료 시
    /// </summary>
    protected virtual IEnumerator<float> Co_SetCloseEndAct()
    {
        yield return Timing.WaitForOneFrame;
    }
    #endregion

    /// <summary>
    /// Back 버튼 Override
    /// </summary>
    protected virtual void Back()
    {
        if (BackAction_Custom != null)
        {
            BackAction_Custom();
            BackAction_Custom = null;
        }
        else
        {
            SceneLogic.instance.Back();
        }
    }
    #endregion

}
