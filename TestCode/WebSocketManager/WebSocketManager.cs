using BestHTTP.SocketIO3;
using BestHTTP.SocketIO3.Events;
using CryptoWebRequestSample;
using Cysharp.Threading.Tasks;
using FrameWork.UI;
using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FrameWork.Socket
{
    public partial class WebSocketManager : Singleton<WebSocketManager>
    {
        [Header("디버그 전용")]
        public bool isDebug = true;

        public SocketManager socketManager { get; private set; }

        private void Awake()
        {
            BestHTTP.HTTPManager.Setup();
        }

        /// <summary>
        /// 최초 로그인 시, 1회 연결
        /// </summary>
        public async void SocketIO3Connect()
        {
            await ConnectSocket();

            AddListener();
        }

        private async UniTask ConnectSocket()
        {
            SocketOptions socketOptions = new()
            {
                ConnectWith = BestHTTP.SocketIO3.Transports.TransportTypes.WebSocket,
                AutoConnect = true,

                Auth = (manager, socket) => new { accessToken = LocalPlayerData.AccessToken }
            };

            socketManager = new SocketManager(new Uri(Single.Web.WebSocketUrl), socketOptions);

            await UniTask.SwitchToThreadPool();
            if (socketManager.State == SocketManager.States.Open)
                socketManager.Close();

            socketManager.Open();
            await UniTask.SwitchToMainThread();
        }

        private void AddListener()
        {
            #region Basic Response
            socketManager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnect);
            socketManager.Socket.On<ConnectResponse>(SocketIOEventTypes.Disconnect, OnDisconnect);
            socketManager.Socket.On<object>(SocketIOEventTypes.Error, OnError);
            #endregion

            AddListener_Basic();
            AddListener_Player();
            AddListener_Interaction();
            AddListener_Common();
            AddListener_Chatting();
            AddListener_ScreenBanner();
            AddListener_Friend();
            AddListener_NFTAvatar();
            AddListener_MyRoom();
        }

        #region Basic Response 
        private bool isConnected = false;

        private void OnConnect(ConnectResponse obj)
        {
            isConnected = true;

            DEBUGLOG(nameof(OnConnect));
        }

        private void OnDisconnect(ConnectResponse obj)
        {
            isConnected = false;
            Util.RunCoroutine(Co_CheckConnect(), nameof(Co_CheckConnect));

            DEBUGLOG(nameof(OnDisconnect));
        }

        private void OnError(object obj)
        {
            DEBUGLOG(nameof(OnError));
        }
        #endregion

        #region Send
        public void Send(string _nameSpace, string _eventName, object data = null)
        {
            Send(new WebSocketData(_nameSpace, _eventName, data));
        }

        public void Send(WebSocketData _packet)
        {
            socketManager.Socket.Emit(WebSocketPacket.REQUEST, _packet);
        }
        #endregion

        #region DEBUGLOG
        public void DEBUGLOG(string _method, object _packet) => DEBUGLOG($"{_method} : {Util.Beautify(JsonConvert.SerializeObject(_packet))}");

        public void DEBUGLOG(string str)
        {
            if (isDebug) DEBUG.LOG(str, eColorManager.WebSocket);
        }
        #endregion

        #region IsNull
        /// <summary>
        /// Null체크 및 디버그 로그 출력
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="_obj"></param>
        /// <param name="_methodName"></param>
        /// <returns></returns>
        private bool IsNull<T>(string _methodName, T _obj) where T : class
        {
            if (_obj == null)
            {
                Debug.Log($"{_methodName} is Null !!");
                return true;
            }
            return false;
        }

        private bool IsNull(string _methodName, string _str)
        {
            if (string.IsNullOrEmpty(_str))
            {
                Debug.Log($"{_methodName} is Null !!");
                return true;
            }
            return false;
        }
        #endregion
    }
}

