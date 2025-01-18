/*===========================================================================================
*
*   Rest API 통신 클래스
*   
*                                ┌> [참조] HttpServer
*   WebManager (현재 스크립트) ---
*                                └> [참조] HttpServer_MultiPartForm
*                          
*   - API 호출 메서드 작성용 매니저
*   - 앱 시작 시 Gateway 호출 및 관련 데이터 캐싱 관리
* 
===========================================================================================*/

using Assets._Launching.DEV.Script.Framework.Network.WebPacket;
using CryptoWebRequestSample;
using Cysharp.Threading.Tasks;
using FrameWork.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using UnityEngine.Events;

public partial class WebManager : Singleton<WebManager>
{
    #region Unity Method
    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }
    #endregion

    #region CoreMethod
    private HttpServer _requestHttp = new HttpServer();
    private HttpServer_MultiPartForm _requestHttp_Multi = new HttpServer_MultiPartForm();

    /// <summary>
    /// 기본 Http Request
    /// </summary>
    private void SendRequest<T>(SendRequestData data, Action<T> _res, Action<DefaultPacketRes> _error = null)
    {
        _requestHttp.SendRequest(data, _res, _error);
    }

    /// <summary>
    /// MultiPartForm용 Http Request
    /// </summary>
    private void SendRequest<T>(SendRequestData_MultiPartForm data, Action<T> _res, Action<DefaultPacketRes> _error = null)
    {
        _requestHttp_Multi.SendRequest(data, _res, _error);
    }
    #endregion
}

#region Server URL 캐싱
public partial class WebManager : Singleton<WebManager>
{
    public GatewayInfo GatewayInfo { get; private set; }

    private ServerInfo ServerInfo => GatewayInfo.Gateway.ServerType.ServerInfo;

    public string AccountServerUrl => ServerInfo.accountServerUrl;
    public string PCAccountServerUrl => ServerInfo.pcAccountServerUrl;
    public string AgoraServerUrl => ServerInfo.agoraServerUrl;
    public string ContentServerUrl => ServerInfo.contentServerUrl;
    public string LobbyServerUrl => ServerInfo.lobbyServerUrl;
    public int GameServerPort => ServerInfo.gameServerPort;
    public string StorageUrl => ServerInfo.storageUrl;
    public string HomepageUrl => ServerInfo.homepageUrl;
    public string WebviewUrl => ServerInfo.webviewUrl;
    public string LinuxServerIp => ServerInfo.linuxServerIp;
    public int LinuxServerPort => ServerInfo.linuxServerPort;
    public int LinuxHttpPort => ServerInfo.linuxHttpPort;
    public string HomepageBackendUrl => ServerInfo.homepageBackendUrl;
    public string WebSocketUrl => ServerInfo.webSocketUrl;
    public string YoutubeDlUrl => ServerInfo.youtubeDlUrl;

    // Make 요청할 때 사용하는 Url,Port
    // 점핑매칭
    public string MatchingServerUrl => $"{ServerInfo.matchingServerUrl}:{MatchingServerPort}{Cons.RequestMakeRoomStr}";
    public int MatchingServerPort => ServerInfo.matchingServerPort;

    // OX 퀴즈
    public string OXServerUrl => $"{ServerInfo.OXServerUrl}:{MatchingServerPort}{Cons.RequestMakeRoomStr}";
    public int OXServerPort => ServerInfo.OXServerPort;

    // 오피스
    public string MeetingRoomServerUrl => $"{ServerInfo.meetingRoomServerUrl}:{MeetingRoomServerport}{Cons.RequestMakeRoomStr}";
    public int MeetingRoomServerport => ServerInfo.meetingRoomServerPort;

    // 상담실
    public string MedicineRoomServerUrl => $"{ServerInfo.medicineUrl}:{MedicineRoomServerport}{Cons.RequestMakeRoomStr}";
    public int MedicineRoomServerport => ServerInfo.medicinePort;

    // 마이룸
    public string MyRoomServerUrl => $"{ServerInfo.myRoomServerUrl}:{MyRoomServerport}{Cons.RequestMakeRoomStr}";
    public int MyRoomServerport => ServerInfo.myRoomServerPort;
}
#endregion

#region Gateway
public partial class WebManager : Singleton<WebManager>
{
    public bool IsGatewayInfoResDone { get; private set; }

    private string GATEWAY_URL = "https://arzmetasta.blob.core.windows.net/arzmeta-container/gateway/gatewayInfo.txt";

    public void Initialize()
    {
        SetUpGateWayInfo();
    }

    /// <summary>
    /// 게이트웨이 데이터 가져오는 메소드
    /// 최초 호출 필요
    /// </summary>
    private async void SetUpGateWayInfo()
    {
        IsGatewayInfoResDone = false;
        var mainURL = string.Empty;

        SendRequest<string>(new SendRequestData(eHttpVerb.GET, GATEWAY_URL, string.Empty), (res) => mainURL = SetMainUrl(res));

        await UniTask.WaitUntil(() => !string.IsNullOrEmpty(mainURL));

        var packet = new
        {
            osType = GetOsType(),
            appVersion = Application.version,
        };

        SendRequest<string>(new SendRequestData(eHttpVerb.POST, mainURL, string.Empty, packet), SerializeGatewayInfo);
    }

    /// <summary>
    /// 현재 플랫폼에 따른 OsType 리턴 
    /// </summary>
    /// <returns></returns>
    private OS_TYPE GetOsType()
    {
#if UNITY_EDITOR || !UNITY_ANDROID && !UNITY_IOS
        return OS_TYPE.Window;
#elif UNITY_ANDROID
        return OS_TYPE.Android;
#elif UNITY_IOS
        return OS_TYPE.IOS;
#endif
    }

    /// <summary>
    /// mainURL 세팅
    /// </summary>
    /// <param name="result"></param>
    private string SetMainUrl(string result)
    {
        if (string.IsNullOrEmpty(result))
        {
            Util.OpenReturnToLogoPopup();
            return null;
        }

        return WebPacket.Gateway(ClsCrypto.DecryptByAES(result));
    }

    /// <summary>
    /// 게이트웨이 데이터 파싱 및 상태에 따른 처리
    /// </summary>
    /// <param name="result"></param>
    private void SerializeGatewayInfo(string result)
    {
        if (string.IsNullOrEmpty(result))
        {
            Util.OpenReturnToLogoPopup();
            return;
        }

        var jobj = (JObject)JsonConvert.DeserializeObject(result);
        foreach (var x in jobj)
        {
            switch (x.Key)
            {
                case "Gateway":
                    GatewayInfo = JsonConvert.DeserializeObject<GatewayInfo>(result);

                    CheckAppVersionDataReset(GatewayInfo.Gateway.appVersion);

                    switch ((SERVER_STATE)GatewayInfo.Gateway.ServerState.state)
                    {
                        case SERVER_STATE.ACTIVATE: IsGatewayInfoResDone = true; break;
                        case SERVER_STATE.INACTIVATE: SetGatewayPopup(GatewayInfo.Gateway.StateMessage.message, () => Util.QuitApplication()); break;
                        case SERVER_STATE.TEST: Debug.Log("Gateway: 테스트 수락된 유저만 진입할 수 있습니다. 본 기능은 아직 개발되지 않았습니다."); break;
                        case SERVER_STATE.NEED_UPDATE: SetGatewayPopup(GatewayInfo.Gateway.StateMessage.message, () => UpdateVersion(GatewayInfo.Gateway.OsType.storeUrl)); break;
                        default: break;
                    }
                    break;
                default:
                    GatewayInfo_Update gatewayInfo_Update = JsonConvert.DeserializeObject<GatewayInfo_Update>(result);
                    SetGatewayPopup(gatewayInfo_Update.StateMessage.message, () => UpdateVersion(gatewayInfo_Update.OsType.storeUrl));
                    break;
            }
            break;
        }
    }

    /// <summary>
    /// 앱버전이 설치되어있던 것과 다를 시 로그아웃
    /// </summary>
    private void CheckAppVersionDataReset(string appVersion)
    {
        if (PlayerPrefs.GetString("AppVersion", "0.0.0") != appVersion)
        {
            LocalPlayerData.ResetData();
        }
        PlayerPrefs.SetString("AppVersion", appVersion);
    }

    private void SetGatewayPopup(string message, UnityAction action = null)
    {
        SceneLogic.instance.PushPopup<Popup_Basic>()
                 .ChainPopupData(new PopupData(POPUPICON.NONE, BTN_TYPE.Confirm, masterLocalDesc: new MasterLocalData(message)))
                 .ChainPopupAction(new PopupAction(action));
    }

    /// <summary>
    /// 업데이트 => 스토어 이동
    /// </summary>
    public void UpdateVersion(string storeUrl)
    {
        Application.OpenURL(storeUrl);
        Application.Quit();
    }
}
#endregion
