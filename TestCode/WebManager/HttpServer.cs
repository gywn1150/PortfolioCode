/*===========================================================================================
*
*   Rest API 통신 클래스
*                          ┌  HttpServer[자식] (현재 스크립트)
*   HttpServerBase[부모] ◁-
*                          └  HttpServer_MultiPartForm[자식]
*                          
*   - 기본 Header 및 데이터 송신 시 사용
* 
===========================================================================================*/

using FrameWork.UI;
using Newtonsoft.Json;
using System;
using System.Text;

#region Http 통신 데이터 클래스
public class SendRequestData : SendRequestDataBase
{
    public SendRequestData(eHttpVerb httpVerb, string mainUrl, string subUrl, object packet = null, bool dim = true, bool isErrorPopup = true)
        : base(httpVerb, mainUrl, subUrl, packet, dim, isErrorPopup) { }

    public override byte[] GetUploadData()
    {
        if (packet != null)
        {
            var requestJson = JsonConvert.SerializeObject(packet, Formatting.Indented);
            return Encoding.UTF8.GetBytes(requestJson);
        }

        return null;
    }
}
#endregion

public class HttpServer : HttpServerBase
{
    /// <summary>
    /// Data Request (Core)
    /// </summary>
    public async void SendRequest<T>(SendRequestData data, Action<T> _res, Action<DefaultPacketRes> _error = null)
    {
        SetHeader(data);
        await HttpSendRequest(data, _res, _error);
    }

    /// <summary>
    /// UnityWebRequest Header 세팅
    /// </summary>
    private void SetHeader(SendRequestData data)
    {
        SetHeader_Defalut(data);
        data.SetHeader("Content-Type", "application/json");
    }

    protected override T SuccessProcess<T>(string res)
    {
        T resObj = (typeof(T) == typeof(string)) ? (T)(object)res : JsonConvert.DeserializeObject<T>(res);

        if (resObj != null)
        {
            return resObj;
        }
        else
        {
            SceneLogic.instance.PushPopup<Popup_Basic>()
                .ChainPopupData(new PopupData(POPUPICON.NONE, BTN_TYPE.Confirm, masterLocalDesc: new MasterLocalData("common_error_network_01")));
        }

        return base.SuccessProcess<T>(res);
    }
}
