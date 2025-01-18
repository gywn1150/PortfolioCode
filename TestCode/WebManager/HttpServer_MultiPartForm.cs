/*===========================================================================================
*
*   Rest API 통신 클래스
*                          ┌  HttpServer[자식]
*   HttpServerBase[부모] ◁-
*                          └  HttpServer_MultiPartForm[자식] (현재 스크립트)
*                          
*   - MultiPartForm을 위한 Header 및 데이터 클래스 형식 확장
* 
===========================================================================================*/

using FrameWork.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#region Http 통신 데이터 클래스
/// <summary>
/// 멀티 폼 데이터용 클래스
/// </summary>
public class SendRequestData_MultiPartForm : SendRequestDataBase
{
    public string thumbnailPath = "";
    public Texture2D iosTex = null;

    private byte[] _boundary = null;
    private byte[] _uploadData = null;

    public SendRequestData_MultiPartForm(eHttpVerb httpVerb, string mainUrl, string subUrl, object packet = null, bool dim = true, bool isErrorPopup = true, string thumbnailPath = "", Texture2D iosTex = null)
        : base(httpVerb, mainUrl, subUrl, packet, dim, isErrorPopup)
    {
        this.thumbnailPath = thumbnailPath;
        this.iosTex = iosTex;
    }

    private void SetUploadData()
    {
        if (packet != null)
        {
            var formData = new List<IMultipartFormSection>();

            if (thumbnailPath != "")
            {
#if UNITY_IOS
                byte[] image = iosTex.EncodeToPNG();
                string fileName = Util.ConvertIOSExtension(Uri.EscapeUriString(Path.GetFileName(thumbnailPath)));
#else
                byte[] image = File.ReadAllBytes(thumbnailPath);
                string fileName = Uri.EscapeUriString(Path.GetFileName(thumbnailPath));
#endif
                string extension = "image/" + Path.GetExtension(fileName).Split('.').Last();
                formData.Add(new MultipartFormFileSection("image", image, fileName, extension));
            }

            string requestJson = JsonConvert.SerializeObject(packet, Formatting.Indented);

            var jsonDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestJson);
            foreach (var item in jsonDic)
            {
                formData.Add(new MultipartFormDataSection(item.Key, item.Value));
            }

            _boundary = UnityWebRequest.GenerateBoundary();
            _uploadData = UnityWebRequest.SerializeFormSections(formData, _boundary);
        }
    }

    public override byte[] GetUploadData()
    {
        if (_uploadData == null) SetUploadData();
        return _uploadData;
    }

    public string GetBoundaryStr()
    {
        if (_boundary == null) SetUploadData();
        return Encoding.UTF8.GetString(_boundary);
    }
}
#endregion

public class HttpServer_MultiPartForm : HttpServerBase
{
    /// <summary>
    /// Data Request (Core)
    /// </summary>
    public async void SendRequest<T>(SendRequestData_MultiPartForm data, Action<T> _res, Action<DefaultPacketRes> _error = null)
    {
        SetHeader(data);
        await HttpSendRequest(data, _res, _error);
    }

    /// <summary>
    /// UnityWebRequest Header 세팅
    /// </summary>
    private void SetHeader(SendRequestData_MultiPartForm data)
    {
        SetHeader_Defalut(data);
        data.SetHeader("Content-Type", $"multipart/form-data; boundary={data.GetBoundaryStr()}");
    }

    protected override T SuccessProcess<T>(string res)
    {
        T resObj = JsonConvert.DeserializeObject<T>(res);

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