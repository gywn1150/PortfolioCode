/*===========================================================================================
*
*   Rest API 통신 클래스
*                                         ┌  HttpServer[자식]
*   HttpServerBase[부모] (현재 스크립트) ◁-
*                                         └  HttpServer_MultiPartForm[자식]
*                          
*   - Rest API 비동기 호출 클래스
*   - 호출 단계에 따라 별개의 가상 함수를 호출하도록 함. 자식 클래스에서 확장하여 사용 
* 
===========================================================================================*/

using CryptoWebRequestSample;
using Cysharp.Threading.Tasks;
using FrameWork.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

#region Http 통신 데이터 클래스
public class SendRequestDataBase
{
    public eHttpVerb httpVerb = eHttpVerb.GET;
    public string mainUrl = string.Empty;
    public string subUrl = string.Empty;
    public object packet = null;
    public bool dim = true; // Dim Panel을 활성화 할 것인지
    public bool isErrorPopup = true; // Error Response 시 팝업을 띄우게 할 것인지

    protected Dictionary<string, string> _headerDic = new Dictionary<string, string>();

    public SendRequestDataBase(eHttpVerb httpVerb, string mainUrl, string subUrl, object packet = null, bool dim = true, bool isErrorPopup = true)
    {
        this.httpVerb = httpVerb;
        this.mainUrl = mainUrl;
        this.subUrl = subUrl;
        this.packet = packet;
        this.dim = dim;
        this.isErrorPopup = isErrorPopup;
    }

    public void SetHeader(string key, string value)
    {
        if (!_headerDic.ContainsKey(key))
        {
            _headerDic.Add(key, value);
        }
    }

    public string GetURL() => mainUrl + subUrl;
    public string GetMehtod() => httpVerb.ToString();
    public Dictionary<string, string> GetHeader() { return _headerDic; }
    public virtual byte[] GetUploadData() { return null; }
}
#endregion

public abstract class HttpServerBase
{
    protected int timeout_Count = 10; // 기본 Timeout 10초
    private CancellationTokenSource _cancelToken = new CancellationTokenSource();

    /// <summary>
    /// Rest API 비동기 호출 - Request & Response (Core method)
    /// Error 시 오류 팝업 자동 호출 처리 추가
    /// </summary>
    protected async UniTask HttpSendRequest<T>(SendRequestDataBase data, Action<T> _res, Action<DefaultPacketRes> _error = null)
    {
        if (data.dim) Single.Scene.SetDimOn(1f);
        SetIsErrorPopup(data.subUrl, data.isErrorPopup);

        using (var uwr = new UnityWebRequest(data.GetURL(), data.GetMehtod()))
        {
            // Timeout 설정
            // UnityWebRequest의 timeout 사용 시 ConnectionError로 통합되어 오므로 별개의 오류로 받기 위함
            _cancelToken.CancelAfterSlim(TimeSpan.FromSeconds(timeout_Count));

            uwr.uploadHandler = new UploadHandlerRaw(data.GetUploadData());
            uwr.downloadHandler = new DownloadHandlerBuffer();

            // Header 설정
            var headerDic = data.GetHeader();
            if (headerDic != null && headerDic.Count > 0)
            {
                foreach (var kvp in headerDic)
                {
                    uwr.SetRequestHeader(kvp.Key, kvp.Value);
                }
            }

            try // 정상 처리
            {
                await uwr.SendWebRequest().WithCancellation(_cancelToken.Token);

                var resultHendler = SuccessProcess<T>(uwr.downloadHandler.text);
                if (resultHendler != null)
                {
                    _res.Invoke(resultHendler);
                }

                DEBUG.LOG(Util.Beautify(uwr.downloadHandler.text), eColorManager.Web_Response);
            }
            catch (OperationCanceledException timeout_exception) // Timeout 처리
            {
                TimeoutErrorHendler(timeout_exception);

                DEBUG.LOG("Timeout", eColorManager.Web_Response);
            }
            catch (UnityWebRequestException exception) // 에러 처리
            {
                var errorHendler = ResponseErrorHendler(exception, uwr.downloadHandler.text);
                if (errorHendler != null)
                {
                    if (GetIsErrorPopup(data.subUrl))
                    {
                        // 오류 팝업 호출
                        SceneLogic.instance.GetPopup<Popup_Basic>().CheckResponseError(errorHendler.error);
                    }

                    _error?.Invoke(errorHendler);
                }

                DEBUG.LOG(data.GetURL() + " " + exception.Error, eColorManager.Web_Response);
                DEBUG.LOG(exception.Text, eColorManager.Web_Response);
            }
            finally // 최종 처리
            {
                if (data.dim) Single.Scene.SetDimOff(1f);

                FinalProcess();
            }
        }
    }

    /// <summary>
    /// API 호출 성공 시
    /// </summary>
    protected virtual T SuccessProcess<T>(string res) { return default; }

    /// <summary>
    /// Http 오류 / 서버와 합의 된 오류 발생 시
    /// </summary>
    protected virtual DefaultPacketRes ResponseErrorHendler(UnityWebRequestException exception, string response)
    {
        try
        {
            // 합의된 에러일 시
            var responseError = JsonConvert.DeserializeObject<DefaultPacketRes>(response);
            return responseError;

        }
        catch (JsonSerializationException)
        {
            if (exception.Result == UnityWebRequest.Result.ConnectionError ||
                exception.Result == UnityWebRequest.Result.ProtocolError ||
                exception.Result == UnityWebRequest.Result.DataProcessingError)
            {
                if (SceneLogic.instance.GetPopup<Popup_Basic>().CheckHttpResponseError(exception.Error))// http 서버 에러
                {
                    Single.Web.StopHeartBeat();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 시간 초과 시 
    /// </summary>
    protected virtual void TimeoutErrorHendler(OperationCanceledException timeout_exception)
    {
        if (timeout_exception.CancellationToken == cancelToken.Token)
        {
            Debug.Log("Request timeout");
            SceneLogic.instance.GetPopup<Popup_Basic>().CheckHttpResponseError("Request timeout");
        }
    }

    /// <summary>
    /// 프로세스 종료 후 호출되어야 할 코드 작성
    /// </summary>
    protected virtual void FinalProcess() { }

    /// <summary>
    /// API 사용 시 헤더 추가
    /// 로그인 이전에 호출되는 API 외에 대부분 해당 헤더가 사용된다
    /// </summary>
    protected void SetHeader_Defalut(SendRequestDataBase data)
    {
        if (!string.IsNullOrEmpty(LocalPlayerData.JwtAccessToken))
            data.SetHeader("jwtAccessToken", ClsCrypto.EncryptByAES(LocalPlayerData.JwtAccessToken));
        if (!string.IsNullOrEmpty(LocalPlayerData.SessionID))
            data.SetHeader("sessionId", ClsCrypto.EncryptByAES(LocalPlayerData.SessionID));
    }

    #region IsErrorPopup
    /// <summary>
    /// 에러 시 자동으로 뜨는 팝업을 비/활성화 할 수 있는 옵션
    /// API 호출 시 isErrorPopup 파라미터를 작성하면 된다
    /// true: 활성화 (기본값), false: 비활성화
    /// </summary>

    private Dictionary<string, bool> _IsErrorPopupDic = new Dictionary<string, bool>();

    public void SetIsErrorPopup(string key, bool isErrorPopup)
    {
        if (!_IsErrorPopupDic.ContainsKey(key))
        {
            _IsErrorPopupDic.Add(key, default);
        }
        _IsErrorPopupDic[key] = isErrorPopup;
    }

    private bool GetIsErrorPopup(string key)
    {
        if (_IsErrorPopupDic.ContainsKey(key))
        {
            return _IsErrorPopupDic[key];
        }
        return true;
    }
    #endregion
}
