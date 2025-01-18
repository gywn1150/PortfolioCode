using FrameWork.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

#region 로컬라이징 이벤트 스크립트

[System.Serializable]
public class MasterLocalData
{
    public string id;
    public object[] args;

    public MasterLocalData() { }

    public MasterLocalData(string id, params object[] args)
    {
        this.id = id;
        this.args = args;
    }
}

/*===========================================================================================
*
*   - Text Component에 부착되는 Localization 스크립트
*   - UI Framework(UIbase.cs)로 Text 참조 시 자동 부착 
*   - MainLogic(SceneLogic.cs)의 delegate에 등록 (언어 교체 시 즉시 변경)
* 
===========================================================================================*/

/// <summary>
/// Text Component에 Add되는 스크립트
/// </summary>
public class LocalizingEvent : MonoBehaviour
{
    [SerializeField] private MasterLocalData masterLocalData = null;

    private TMP_Text _txtmp;
    private string _str = null;
    private bool isNormalString = false;

    #region 핸들러 등록, 삭제
    private void Awake()
    {
        _txtmp = TryGetComponent<TMP_Text>(out var txtmp) ? txtmp : gameObject.AddComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        SceneLogic.instance.localizingEventHandler += OnLocalizingText;

        if (masterLocalData != null || _str != null) OnLocalizingText();
    }

    private void OnDisable()
    {
        SceneLogic.instance.localizingEventHandler -= OnLocalizingText;
    }
    #endregion

    /// <summary>
    /// MasterLocalData 입력
    /// </summary>
    public void SetMasterLocalizing(MasterLocalData masterLocalData)
    {
        if (masterLocalData == null) return;

        isNormalString = false;

        this.masterLocalData = masterLocalData;
        OnLocalizingText();
    }

    /// <summary>
    /// String 입력
    /// String 입력 시 초기화 해야 할 경우 필요
    /// </summary>
    public void SetString(string str)
    {
        isNormalString = true;

        this._str = str;
        OnLocalizingText();
    }

    private void OnLocalizingText()
    {
        if (_txtmp == null) return;

        _txtmp.text = isNormalString ? _str : Util.GetMasterLocalizing(masterLocalData);
    }
}

#endregion

/*===========================================================================================
*
*   - Util.cs => 공통 기능 메서드를 위한 static class
*   - UI Framework(UIbase.cs)로 Text 호출 시, 아래 로컬라이징 메서드 호출
* 
===========================================================================================*/
#region Util 로컬라이징
/// <summary>
/// 마스터로컬라이징 세팅
/// </summary>
public static void SetMasterLocalizing(MonoBehaviour mono, object data)
{
    if (mono == null) return;

    if (!mono.TryGetComponent(out LocalizingEvent localizingEvent))
    {
        localizingEvent = mono.gameObject.AddComponent<LocalizingEvent>();
    }

    if (data is string str) localizingEvent.SetString(str);
    else if (data is MasterLocalData masterLocalData) localizingEvent.SetMasterLocalizing(masterLocalData);
}

/// <summary>
/// 마스터 데이터에서 다국어 가져오기
/// </summary>
public static string GetMasterLocalizing(MasterLocalData masterLocalData)
{
    return GetMasterLocalizing(masterLocalData.id, masterLocalData.args);
}

public static string GetMasterLocalizing(string id, params object[] args)
{
    if (string.IsNullOrEmpty(id)) return "";

    var localizeData = Single.MasterData.dataLocalization.GetData(id);

    string result;
    string language = string.Empty;
    try
    {
        switch (AppGlobalSettings.Instance.language)
        {
            case Language.Korean: language = localizeData.kor; break;
            case Language.English: language = localizeData.eng; break;
            default: language = localizeData.eng; break;
        };

        result = args == null || args.Length == 0 ? language : string.Format(language, args);
    }
    catch
    {
        result = id;
    }
    return result;
}

#endregion