using Newtonsoft.Json;
using Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

namespace FrameWork.Socket
{
    public partial class WebSocketManager : Singleton<WebSocketManager>
    {
        private void AddListener_MyRoom()
        {
            socketManager.Socket.On<string>(WebSocketPacket.S_MYROOM_GET_ROOMINFO, S_MYROOM_GET_ROOMINFO);
            socketManager.Socket.On<string>(WebSocketPacket.S_MYROOM_START_EDIT, S_MYROOM_START_EDIT);
            socketManager.Socket.On<string>(WebSocketPacket.S_MYROOM_END_EDIT, S_MYROOM_END_EDIT);
            socketManager.Socket.On<string>(WebSocketPacket.S_MYROOM_SHUTDOWN, S_MYROOM_SHUTDOWN);
            socketManager.Socket.On<string>(WebSocketPacket.S_MYROOM_KICK, S_MYROOM_KICK);
        }

        #region C_MYROOM_GET_ROOMINFO => S_MYROOM_GET_ROOMINFO

        public delegate void DEL_MYROOM_GET_ROOMINFO(SOCKET_MYROOM_GET_ROOMINFO_RES _res);
        public event DEL_MYROOM_GET_ROOMINFO MYROOM_GET_ROOMINFO_HENDLER;

        /// <summary>
        /// 마이룸 정보 요청
        /// </summary>
        public void C_MYROOM_GET_ROOMINFO()
        {
            Send(WebSocketPacket.GET_MYROOM, WebSocketPacket.C_MYROOM_GET_ROOMINFO);

            DEBUGLOG(nameof(C_MYROOM_GET_ROOMINFO));
        }

        /// <summary>
        /// 마이룸 정보 받아오기
        /// </summary>
        /// <param name="_packet"></param>
        private void S_MYROOM_GET_ROOMINFO(string _packet)
        {
            if (IsNull(nameof(S_MYROOM_GET_ROOMINFO), _packet)) return;

            var packet = JsonConvert.DeserializeObject<SOCKET_MYROOM_GET_ROOMINFO_RES>(_packet);

            MYROOM_GET_ROOMINFO_HENDLER?.Invoke(packet);

            DEBUGLOG(nameof(S_MYROOM_GET_ROOMINFO), _packet);
        }

        #endregion

        #region C_MYROOM_START_EDIT => S_MYROOM_START_EDIT

        public delegate void DEL_MYROOM_START_EDIT();
        public event DEL_MYROOM_START_EDIT MYROOM_START_EDIT_HENDLER;

        /// <summary>
        /// 마이룸 편집 시작 요청
        /// </summary>
        public void C_MYROOM_START_EDIT()
        {
            Send(WebSocketPacket.GET_MYROOM, WebSocketPacket.C_MYROOM_START_EDIT);

            DEBUGLOG(nameof(C_MYROOM_START_EDIT));
        }

        /// <summary>
        /// 마이룸 편집 시작 결과 받아오기
        /// </summary>
        /// <param name="_packet"></param>
        private void S_MYROOM_START_EDIT(string _packet)
        {
            if (IsNull(nameof(S_MYROOM_GET_ROOMINFO), _packet)) return;
            //??
            MYROOM_START_EDIT_HENDLER?.Invoke();

            DEBUGLOG(nameof(S_MYROOM_START_EDIT));
        }

        #endregion

        #region C_MYROOM_END_EDIT => S_MYROOM_END_EDIT

        public delegate void DEL_MYROOM_END_EDIT(SOCKET_MYROOM_END_EDIT _res);
        public event DEL_MYROOM_END_EDIT MYROOM_END_EDIT_HENDLER;

        /// <summary>
        /// 마이룸 편집 종료 요청
        /// </summary>
        /// <param name="isChanged"></param>
        public void C_MYROOM_END_EDIT(bool _isChanged)
        {
            var packet = new SOCKET_MYROOM_END_EDIT()
            {
                isChanged = _isChanged
            };

            Send(WebSocketPacket.GET_MYROOM, WebSocketPacket.C_MYROOM_END_EDIT, packet);

            DEBUGLOG(nameof(C_MYROOM_END_EDIT), packet);
        }

        /// <summary>
        /// 마이룸 편집 종료 결과 받아오기
        /// </summary>
        /// <param name="_packet"></param>
        private void S_MYROOM_END_EDIT(string _packet)
        {
            if (IsNull(nameof(S_MYROOM_END_EDIT), _packet)) return;

            var packet = JsonConvert.DeserializeObject<SOCKET_MYROOM_END_EDIT>(_packet);

            MYROOM_END_EDIT_HENDLER?.Invoke(packet);

            DEBUGLOG(nameof(S_MYROOM_END_EDIT), packet);
        }

        #endregion

        #region C_MYROOM_SHUTDOWN => S_MYROOM_SHUTDOWN

        public delegate void DEL_MYROOM_SHUTDOWN(SOCKET_MYROOM_SHUTDOWN_RES _res);
        public DEL_MYROOM_SHUTDOWN MYROOM_SHUTDOWN_HANDLER;

        /// <summary>
        /// 마이룸 셧다운 요청
        /// </summary>
        public void C_MYROOM_SHUTDOWN(bool _isShutdown)
        {
            var packet = new
            {
                isShutdown = _isShutdown
            };

            Send(WebSocketPacket.GET_MYROOM, WebSocketPacket.C_MYROOM_SHUTDOWN, packet);

            DEBUGLOG(nameof(C_MYROOM_SHUTDOWN), packet);
        }

        /// <summary>
        /// 마이룸 셧다운 요청 결과 (자신 제외 전원 알림)
        /// </summary>
        private void S_MYROOM_SHUTDOWN(string _packet)
        {
            if (IsNull(nameof(S_MYROOM_SHUTDOWN), _packet)) return;

            var packet = JsonConvert.DeserializeObject<SOCKET_MYROOM_SHUTDOWN_RES>(_packet);

            MYROOM_SHUTDOWN_HANDLER?.Invoke(packet);

            DEBUGLOG(nameof(S_MYROOM_SHUTDOWN), packet);
        }

        #endregion

        #region C_MYROOM_KICK => S_MYROOM_KICK

        public delegate void DEL_MYROOM_KICK(SOCKET_MYROOM_KICK_RES _res);
        public DEL_MYROOM_KICK MYROOM_KICK_HANDLER;

        /// <summary>
        /// 마이룸 특정 사용자 강퇴 요청 (자신)
        /// </summary>
        public void C_MYROOM_KICK(string _clientId)
        {
            var packet = new
            {
                clientId = _clientId
            };

            Send(WebSocketPacket.GET_MYROOM, WebSocketPacket.C_MYROOM_KICK, packet);

            DEBUGLOG(nameof(C_MYROOM_KICK));
        }

        /// <summary>
        /// 마이룸 특정 사용자 강퇴 요청 결과 (타인)
        /// </summary>
        private void S_MYROOM_KICK(string _packet)
        {
            if (IsNull(nameof(S_MYROOM_KICK), _packet)) return;

            var packet = JsonConvert.DeserializeObject<SOCKET_MYROOM_KICK_RES>(_packet);

            MYROOM_KICK_HANDLER?.Invoke(packet);

            DEBUGLOG(nameof(S_MYROOM_KICK));
        }

        #endregion
    }
}


