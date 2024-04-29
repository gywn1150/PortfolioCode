using Assets._Launching.DEV.Script.Framework.Network.WebPacket;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Ocsp;
using CryptoWebRequestSample;
using Cysharp.Threading.Tasks;
using FrameWork.UI;
using MEC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TreeEditor;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.Events;
using UnityEngine.Networking;


#region 웹매니저 데이터 클래스
/// <summary>
/// UnityWebRequest Method
/// </summary>
public enum eHttpVerb
{
    GET,
    POST,
    PUT,
    DELETE,
}

/// <summary>
/// 헤더 세팅
/// </summary>
public enum AuthHeader
{
    None, // 로그인 이전
    Access, // 로그인 이후 모든 API 호출 시 
    Refresh // Access Token 만료 시
}

public class SendRequestData
{
    #region 변수
    // 필수 입력 변수
    public eHttpVerb HttpVerb { get; private set; } = eHttpVerb.GET;
    public string MainUrl { get; private set; } = string.Empty;
    public string SubUrl { get; private set; } = string.Empty;
    public object Packet { get; private set; } = null;

    // 선택 입력 변수
    public AuthHeader HeaderType { get; private set; } = AuthHeader.Access;
    public Dictionary<string, string> Headers { get; private set; } = null;
    public bool IsDim { get; private set; } = true;
    public bool IsErrorPopup { get; private set; } = true;

    // 멀티데이터폼 용 변수
    private bool isMultipartForm = false;
    public string thumbnailPath { get; private set; } = string.Empty;
    public Texture2D IOSTex { get; private set; } = null;
    #endregion

    public SendRequestData(eHttpVerb httpVerb, string mainUrl, string subUrl, object packet = null)
    {
        this.HttpVerb = httpVerb;
        this.MainUrl = mainUrl;
        this.SubUrl = subUrl;
        this.Packet = packet;
    }

    #region Set Option Method

    /// <summary>
    /// Auth 헤더
    /// None : 로그인 이전 요청하는 API에 사용
    /// Access : 로그인 후에 요청하는 모든 API에 사용
    /// Refresh : AccessToken 및 RefreshToken이 만료됐을 시 사용
    /// </summary>
    /// <param name="_headerType"></param>
    /// <returns></returns>
    public SendRequestData SetAuthHeader(AuthHeader _headerType)
    {
        HeaderType = _headerType;
        return this;
    }

    /// <summary>
    /// Dim Panel을 활성화 할 것인지
    /// </summary>
    /// <param name="_isDim"></param>
    /// <returns></returns>
    public SendRequestData SetIsDim(bool _isDim)
    {
        IsDim = _isDim;
        return this;
    }

    /// <summary>
    /// 에러 리스폰스 시 팝업을 띄우게 할 것인지
    /// </summary>
    /// <param name="_isErrorPopup"></param>
    /// <returns></returns>
    public SendRequestData SetIsErrorPopup(bool _isErrorPopup)
    {
        IsErrorPopup = _isErrorPopup;
        return this;
    }

    /// <summary>
    /// 추가적인 헤더 세팅
    /// </summary>
    /// <param name="key"></param>
    /// <param name="vaule"></param>
    public SendRequestData SetCustomHeader(string key, string vaule)
    {
        if (!string.IsNullOrEmpty(key))
        {
            Headers ??= new();
            Headers.Add(key, vaule);
        }
        return this;
    }

    /// <summary>
    /// 썸네일패스 세팅 (멀티 폼 데이터용)
    /// </summary>
    /// <param name="_thumbnailPath"></param>
    /// <returns></returns>
    public SendRequestData SetThumbnailPath(string _thumbnailPath)
    {
        thumbnailPath = _thumbnailPath;
        return this;
    }

    /// <summary>
    /// IOS용 썸네일 텍스쳐 세팅 (멀티 폼 데이터용)
    /// </summary>
    /// <param name="_IOSTex"></param>
    /// <returns></returns>
    public SendRequestData SetIOSTex(Texture2D _IOSTex)
    {
        IOSTex = _IOSTex;
        return this;
    }

    /// <summary>
    /// 멀티파트폼 데이터인지 아닌지
    /// </summary>
    /// <param name="_isMultipartForm"></param>
    /// <returns></returns>
    public SendRequestData SetIsMultipartForm(bool _isMultipartForm)
    {
        isMultipartForm = _isMultipartForm;
        return this;
    }

    #endregion

    #region Get Method
    private byte[] boundary = null;

    /// <summary>
    /// 웹리퀘스트 시 보내는 최종 URL
    /// </summary>
    /// <returns></returns>
    public string GetURL() => new Uri(new Uri(MainUrl), SubUrl).ToString();

    /// <summary>
    /// Content-Type 헤더 추가
    /// 멀티파트 폼 데이터 / 일반
    /// </summary>
    /// <returns></returns>
    public string GetContentTypeHeader()
    {
        return isMultipartForm && boundary != null ? $"multipart/form-data; boundary={Encoding.UTF8.GetString(boundary)}" : "application/json";
    }

    /// <summary>
    /// 웹 리퀘스트 데이터 패킷 추가
    /// </summary>
    /// <returns></returns>
    public byte[] GetJsonBytes()
    {
        string requestJson = null;

        if (Packet != null) requestJson = JsonConvert.SerializeObject(Packet, Formatting.Indented);

        if (isMultipartForm)
        {
            List<IMultipartFormSection> formData = new();

            if (!string.IsNullOrEmpty(requestJson))
            {
                var jsonDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestJson);
                foreach (var item in jsonDic)
                {
                    formData.Add(new MultipartFormDataSection(item.Key, item.Value));
                }
            }

            var thumnailFormData = ThumnailFormDataSetting();
            if (thumnailFormData != null)
            {
                formData.Add(thumnailFormData);
            }

            boundary = UnityWebRequest.GenerateBoundary();
            return UnityWebRequest.SerializeFormSections(formData, boundary);//3. 폼데이터에 맞게 업로드 핸들러
        }

        if (!string.IsNullOrEmpty(requestJson))
        {
            return Encoding.UTF8.GetBytes(requestJson);
        }

        return null;
    }

    /// <summary>
    /// 썸네일 멀티파트 폼 데이터 추가 메소드
    /// </summary>
    /// <returns></returns>
    private MultipartFormFileSection ThumnailFormDataSetting()
    {
        byte[] image = null;
        string fileName = null;

#if UNITY_IOS
            if (IOSTex != null)
            {
                image = IOSTex.EncodeToPNG();
            }
            if (!string.IsNullOrEmpty(thumbnailPath))
            {
                fileName = Util.ConvertIOSExtension(Uri.EscapeUriString(Path.GetFileName(thumbnailPath)));
            }
#else
        if (!string.IsNullOrEmpty(thumbnailPath))
        {
            image = File.ReadAllBytes(thumbnailPath);
            fileName = Uri.EscapeUriString(Path.GetFileName(thumbnailPath));
        }
#endif
        string extension = "image/" + Path.GetExtension(fileName).Split('.').Last();

        if (image == null || string.IsNullOrEmpty(fileName))
        {
            DEBUG.LOG("Webmanager.cs ThumnailFormDataSetting() 데이터 세팅이 제대로 되지 않았습니다.");
            return null;
        }

        return new MultipartFormFileSection("image", image, fileName, extension);
    }

    #endregion
}

public class RequestDataClass<T_Res, T_Error>
{
    public SendRequestData data;
    public Action<T_Res> res;
    public Action<T_Error> error;

    public RequestDataClass(SendRequestData data, Action<T_Res> res, Action<T_Error> error)
    {
        this.data = data;
        this.res = res;
        this.error = error;
    }
}

#endregion

namespace FrameWork.Network
{
    public partial class WebManager : Singleton<WebManager>
    {
        #region Unity 기본 메소드

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            Timing.KillCoroutines();
            StopAllCoroutines();
            Resources.UnloadUnusedAssets();
        }

        #endregion

        #region 변수

        public bool IsGateWayInfoResDone { get; private set; }

        private string GatewayInfoURL = "https://devmoasisstorage.blob.core.windows.net/dev-moasis-container/gateway/gatewayInfo.txt";

        #region GateWayData
        public GatewayInfo GatewayInfo { get; private set; }

        private ServerInfo ServerInfo => GatewayInfo.Gateway.ServerType.ServerInfo;
        // Url
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
        public string MatchingServerUrl => ServerInfo.matchingServerUrl + ":" + MatchingServerPort + Cons.RequestMakeRoomStr;
        public int MatchingServerPort => ServerInfo.matchingServerPort;

        // OX 퀴즈
        public string OXServerUrl => ServerInfo.OXServerUrl + ":" + MatchingServerPort + Cons.RequestMakeRoomStr;
        public int OXServerPort => ServerInfo.OXServerPort;

        // 오피스
        public string MeetingRoomServerUrl => ServerInfo.meetingRoomServerUrl + ":" + MeetingRoomServerport + Cons.RequestMakeRoomStr;
        public int MeetingRoomServerport => ServerInfo.meetingRoomServerPort;

        // 상담실
        public string MedicineRoomServerUrl => ServerInfo.medicineUrl + ":" + MedicineRoomServerport + Cons.RequestMakeRoomStr;
        public int MedicineRoomServerport => ServerInfo.medicinePort;

        // 마이룸
        public string MyRoomServerUrl => ServerInfo.myRoomServerUrl + ":" + MyRoomServerport + Cons.RequestMakeRoomStr;
        public int MyRoomServerport => ServerInfo.myRoomServerPort;
        #endregion

        #endregion

        #region 초기화

        public void Initialize() => SetUpGateWayInfo();

        /// <summary>
        /// 게이트웨이 데이터 가져오는 메소드. 맨 처음 호출해줘야함
        /// </summary>
        private async void SetUpGateWayInfo()
        {
            IsGateWayInfoResDone = false;
            var mainURL = string.Empty;

            var dataGetMainUrl = new SendRequestData(eHttpVerb.GET, GatewayInfoURL, string.Empty).SetAuthHeader(AuthHeader.None);
            SendRequest<string>(dataGetMainUrl, (res) => mainURL = SetMainUrl(res));

            await UniTask.WaitUntil(() => !string.IsNullOrEmpty(mainURL));

            var packet = new
            {
                osType = (int)GetOsType(),
                appVersion = UnityEngine.Application.version,
            };

            var dataGetGatewayInfo = new SendRequestData(eHttpVerb.POST, mainURL, string.Empty, packet).SetAuthHeader(AuthHeader.None);
            SendRequest<string>(dataGetGatewayInfo, SerializeGatewayInfo);
        }

        /// <summary>
        /// 게이트웨이 데이터 파싱 및 상태에 따른 처리
        /// </summary>
        /// <param name="_result"></param>
        private void SerializeGatewayInfo(string _result)
        {
            if (string.IsNullOrEmpty(_result))
            {
                OpenReturnToLogoPopup();
                return;
            }

            var jobj = (JObject)JsonConvert.DeserializeObject(_result);
            foreach (var x in jobj)
            {
                switch (x.Key)
                {
                    case "Gateway":
                        GatewayInfo = JsonConvert.DeserializeObject<GatewayInfo>(_result);

                        CheckAppVersionDataReset(GatewayInfo.Gateway.appVersion);

                        switch ((SERVER_STATE)GatewayInfo.Gateway.ServerState.state)
                        {
                            case SERVER_STATE.ACTIVATE: IsGateWayInfoResDone = true; break;
                            case SERVER_STATE.INACTIVATE: SetPopup(GatewayInfo.Gateway.StateMessage.message, () => Util.QuitApplication()); break;
                            case SERVER_STATE.TEST: Debug.Log("Gateway: 테스트 수락된 유저만 진입할 수 있습니다. 본 기능은 아직 개발되지 않았습니다."); break;
                            case SERVER_STATE.NEED_UPDATE: SetPopup(GatewayInfo.Gateway.StateMessage.message, () => UpdateVersion(GatewayInfo.Gateway.OsType.storeUrl)); break;
                            default: break;
                        }
                        break;
                    default:
                        GatewayInfo_Update gatewayInfo_Update = JsonConvert.DeserializeObject<GatewayInfo_Update>(_result);
                        SetPopup(gatewayInfo_Update.StateMessage.message, () => UpdateVersion(gatewayInfo_Update.OsType.storeUrl));
                        break;
                }
                break;
            }
        }

        #endregion

        #region [코어 메소드] Request&Response

        /// <summary>
        /// 데이터 리퀘스트
        /// </summary>
        public void SendRequest<T_Res>(SendRequestData _data, Action<T_Res> _res, Action<DefaultPacketRes> _error = null)
        {
            SendRequest(new RequestDataClass<T_Res, DefaultPacketRes>(_data, _res, _error ?? Error_Res));
        }

        public void SendRequest<T_Res, T_Error>(SendRequestData _data, Action<T_Res> _res, Action<T_Error> _error = null)
        {
            SendRequest(new RequestDataClass<T_Res, T_Error>(_data, _res, _error));
        }

        public async void SendRequest<T_Res, T_Error>(RequestDataClass<T_Res, T_Error> _reqData)
        {
            await Co_SendWebRequest(_reqData);
        }

        /// <summary>
        /// 코어 메소드
        /// </summary>
        public async Task Co_SendWebRequest<T_Res, T_Error>(RequestDataClass<T_Res, T_Error> _reqData)
        {
            string responseJson = string.Empty;

            if (_reqData.data.IsDim) Single.Scene.SetDimOn(1f);

            if (!string.IsNullOrEmpty(_reqData.data.SubUrl))
            {
                SetIsErrorPopup(_reqData.data.SubUrl, _reqData.data.IsErrorPopup);
            }

            #region 데이터 패킹 및 세팅
            var uwr = new UnityWebRequest(_reqData.data.GetURL(), _reqData.data.HttpVerb.ToString())
            {
                uploadHandler = new UploadHandlerRaw(_reqData.data.GetJsonBytes()),
                downloadHandler = new DownloadHandlerBuffer()
            };

            uwr.SetRequestHeader("Content-Type", _reqData.data.GetContentTypeHeader());
            SetRequestHeader_Default(uwr, _reqData.data.HeaderType);
            SetRequestHeader_Custom(uwr, _reqData.data.Headers);

            var cts = new CancellationTokenSource();
            cts.CancelAfterSlim(TimeSpan.FromSeconds(10)); // 타임아웃 10초
            #endregion

            #region 데이터 샌드 및 리스폰스
            try
            {
                await uwr.SendWebRequest().WithCancellation(cts.Token);

                responseJson = Util.Beautify(uwr.downloadHandler.text);
                DEBUG.LOG(_reqData.data.SubUrl + "\n" + responseJson + "\n", eColorManager.Web_Response);

                var resObj = (typeof(T_Res) == typeof(string)) ? (T_Res)(object)responseJson : JsonConvert.DeserializeObject<T_Res>(responseJson);

                if (resObj != null)
                {
                    _reqData.res?.Invoke(resObj);
                }
                else
                {
                    SetPopup("common_error_network_01");
                }
            }
            catch (UnityWebRequestException exception)
            {
                if (exception.Result == UnityWebRequest.Result.ConnectionError ||
                                       exception.Result == UnityWebRequest.Result.ProtocolError ||
                                       exception.Result == UnityWebRequest.Result.DataProcessingError)
                {
                    DEBUG.LOG(_reqData.data.SubUrl + " " + exception.Error, eColorManager.Web_Response);
                    DEBUG.LOG(exception.Text, eColorManager.Web_Response);

                    CheckTokenError(exception.Error, _reqData);

                    if (GetIsErrorPopup(_reqData.data.SubUrl))
                    {
                        CheckHttpResponseError(exception.Error);
                        HandleError(exception.Text, _reqData.error);
                    }
                }
            }
            catch (OperationCanceledException exception)
            {
                // 작업 취소 시 타임 아웃 예외 처리
                if (exception.CancellationToken == cts.Token)
                {
                    CheckHttpResponseError("Request timeout");
                }
            }
            finally
            {
                await UniTask.WaitUntil(() => uwr.isDone);

                if (_reqData.data.IsDim) Single.Scene.SetDimOff(1f);

                uwr.Dispose();
            }
            #endregion
        }

        /// <summary>
        /// 에러 발생 시 처리
        /// </summary>
        private void HandleError<T_Error>(string _response, Action<T_Error> _error)
        {
            if (string.IsNullOrEmpty(_response))
            {
                OpenReturnToLogoPopup();
                return;
            }

            var responseError = JsonConvert.DeserializeObject<T_Error>(_response);

            if (responseError != null)
            {
                _error?.Invoke(responseError);

                if (responseError is DefaultPacketRes errorPacketRes)
                {
                    SceneLogic.instance.GetPopup<Popup_Basic>().CheckResponseError(errorPacketRes.error);
                }
            }
        }

        private Stack<(object data, Type type_Res, Type type_Error)> expiredReqStack = new();

        /// <summary>
        /// 토큰 만료 시 재발급 로직
        /// </summary>
        /// <typeparam name="T_Res"></typeparam>
        /// <param name="_errorMessage"></param>
        /// <param name="_reqData"></param>
        private void CheckTokenError<T_Res, T_Error>(string _errorMessage, RequestDataClass<T_Res, T_Error> _reqData)
        {
            if (!_errorMessage.Contains("Unauthorized")) return;

            switch (_reqData.data.HeaderType)
            {
                case AuthHeader.Access: // 일반 API 호출했다가 AccessToken 만료
                    {
                        expiredReqStack.Push((_reqData, typeof(T_Res), typeof(T_Error)));

                        AccessToken(() =>
                        {
                            int stackCount = expiredReqStack.Count();
                            if (stackCount > 0)
                            {
                                for (int i = 0; i < stackCount; i++)
                                {
                                    var (data, type_Res, type_Error) = expiredReqStack.Pop();
                                    var instance = (dynamic)data;
                                    var constructedType = typeof(RequestDataClass<,>).MakeGenericType(type_Res, type_Error);
                                    var newInstance = Activator.CreateInstance(constructedType, instance.data, instance.res, instance.error);

                                    SendRequest(newInstance);
                                }
                            }
                        });
                    }
                    break;
                case AuthHeader.Refresh: // AccessToken 만료나서 호출했다가 RefreshToken 만료
                    {
                        SetPopup("자동로그인 유효 기한이 만료되어 로그아웃 합니다.", Util.ReturnToLogo);
                    }
                    break;
                default: break;
            }
        }

        private void CheckHttpResponseError(string _errorMessage) => SceneLogic.instance.GetPopup<Popup_Basic>().CheckHttpResponseError(_errorMessage);

        /// <summary>
        /// 기본 Error Action
        /// </summary>
        /// <param name="_res"></param>
        public void Error_Res(DefaultPacketRes _res)
        {
            if (Single.Scene.isDim) Single.Scene.SetDimOff();

            Debug.Log("errorCode : " + _res.error + ", errorMessage : " + _res.errorMessage);
        }

        /// <summary>
        /// 헤더 추가
        /// </summary>
        private void SetRequestHeader_Default(UnityWebRequest _uwr, AuthHeader _headerType)
        {
            string value = string.Empty;

            switch (_headerType)
            {
                case AuthHeader.Access: value = LocalPlayerData.GetBearerAccessToken(); break;
                case AuthHeader.Refresh: value = LocalPlayerData.GetBearerRefreshToken(); break;
                case AuthHeader.None:
                default: break;
            }

            if (!string.IsNullOrEmpty(value))
            {
                _uwr.SetRequestHeader("authorization", value);
            }
        }

        /// <summary>
        /// 커스텀 헤더 추가
        /// </summary>
        /// <param name="uwr"></param>
        /// <param name="headers"></param>
        private void SetRequestHeader_Custom(UnityWebRequest _uwr, Dictionary<string, string> _headers)
        {
            if (_headers == null || _headers.Count == 0) return;

            foreach (var header in _headers)
            {
                _uwr.SetRequestHeader(header.Key, header.Value);
            }
        }

        #region IsErrorPopup
        // 에러 시 자동으로 뜨는 팝업을 비/활성화 할 수 있는 옵션.
        // API 호출 시 isErrorPopup 파라미터를 작성하면 된다. true: 활성화, false: 비활성화
        // true가 기본값

        private Dictionary<string, bool> IsErrorPopupDic = new Dictionary<string, bool>();

        public void SetIsErrorPopup(string _key, bool _isErrorPopup)
        {
            if (!IsErrorPopupDic.ContainsKey(_key))
            {
                IsErrorPopupDic.Add(_key, default);
            }
            IsErrorPopupDic[_key] = _isErrorPopup;
        }

        private bool GetIsErrorPopup(string _key)
        {
            if (IsErrorPopupDic.ContainsKey(_key))
            {
                return IsErrorPopupDic[_key];
            }
            return true;
        }
        #endregion

        #endregion

        #region 기능 메소드

        /// <summary>
        /// mainURL 세팅
        /// </summary>
        /// <param name="result"></param>
        private string SetMainUrl(string _result)
        {
            if (string.IsNullOrEmpty(_result))
            {
                OpenReturnToLogoPopup();
                return null;
            }

            return WebPacket.Gateway(ClsCrypto.DecryptByAES(_result));
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
        /// 앱버전이 설치되어있던 것과 다를 시 로그아웃
        /// </summary>
        private void CheckAppVersionDataReset(string _appVersion)
        {
            if (PlayerPrefs.GetString("AppVersion", "0.0.0") != _appVersion)
            {
                LocalPlayerData.ResetData();
            }
            PlayerPrefs.SetString("AppVersion", _appVersion);
        }

        /// <summary>
        /// BasicPopup 세팅
        /// </summary>
        /// <param name="message"></param>
        /// <param name="action"></param>
        private void SetPopup(string _message, UnityAction _action = null)
        {
            SceneLogic.instance.PushPopup<Popup_Basic>()
                     .ChainPopupData(new PopupData(POPUPICON.NONE, BTN_TYPE.Confirm, masterLocalDesc: new MasterLocalData(_message)))
                     .ChainPopupAction(new PopupAction(_action));
        }

        /// <summary>
        /// 업데이트 시 Action
        /// </summary>
        /// <param name="storeUrl"></param>
        public void UpdateVersion(string _storeUrl)
        {
            UnityEngine.Application.OpenURL(_storeUrl);
            UnityEngine.Application.Quit();
        }

        /// <summary>
        /// 가입하는 플랫폼에 따른 RegPathType 리턴 
        /// </summary>
        /// <returns></returns>
        public int GetRegPathType()
        {
#if UNITY_EDITOR || !UNITY_ANDROID && !UNITY_IOS
            return (int)REGPATH_TYPE.Etc;
#elif UNITY_ANDROID
            return (int)REGPATH_TYPE.Android;
#elif UNITY_IOS
            return (int)REGPATH_TYPE.IOS;
#endif
        }

        /// <summary>
        /// 로그아웃 없이 소켓만 종료하여 로고씬으로 재 로드
        /// </summary>
        private void OpenReturnToLogoPopup()
        {
            SceneLogic.instance.PushPopup<Popup_Basic>()
                .ChainPopupData(new PopupData(POPUPICON.WARNING, BTN_TYPE.Confirm, masterLocalDesc: new MasterLocalData(Cons.Local_Arzmeta, "common_error_server_02")))
                .ChainPopupAction(new PopupAction(Util.ReturnToLogo));
        }
        #endregion
    }
}
