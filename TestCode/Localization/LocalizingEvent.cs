using FrameWork.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

#region ���ö���¡ �̺�Ʈ ��ũ��Ʈ

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
*   - Text Component�� �����Ǵ� Localization ��ũ��Ʈ
*   - UI Framework(UIbase.cs)�� Text ���� �� �ڵ� ���� 
*   - MainLogic(SceneLogic.cs)�� delegate�� ��� (��� ��ü �� ��� ����)
* 
===========================================================================================*/

/// <summary>
/// Text Component�� Add�Ǵ� ��ũ��Ʈ
/// </summary>
public class LocalizingEvent : MonoBehaviour
{
    [SerializeField] private MasterLocalData masterLocalData = null;

    private TMP_Text _txtmp;
    private string _str = null;
    private bool isNormalString = false;

    #region �ڵ鷯 ���, ����
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
    /// MasterLocalData �Է�
    /// </summary>
    public void SetMasterLocalizing(MasterLocalData masterLocalData)
    {
        if (masterLocalData == null) return;

        isNormalString = false;

        this.masterLocalData = masterLocalData;
        OnLocalizingText();
    }

    /// <summary>
    /// String �Է�
    /// String �Է� �� �ʱ�ȭ �ؾ� �� ��� �ʿ�
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
*   - Util.cs => ���� ��� �޼��带 ���� static class
*   - UI Framework(UIbase.cs)�� Text ȣ�� ��, �Ʒ� ���ö���¡ �޼��� ȣ��
* 
===========================================================================================*/
#region Util ���ö���¡
/// <summary>
/// �����ͷ��ö���¡ ����
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
/// ������ �����Ϳ��� �ٱ��� ��������
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