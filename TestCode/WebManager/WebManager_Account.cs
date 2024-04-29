using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FrameWork.Network
{
    public partial class WebManager : Singleton<WebManager>
    {
        /// <summary>
        /// AccessToken 만료 시 재발급
        /// </summary>
        public void AccessToken(Action action = null, Action<DefaultPacketRes> _error = null)
        {
            var data = new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.AccessToken)
                .SetAuthHeader(AuthHeader.Refresh)
                .SetIsDim(false);

            Single.Web.SendRequest<ReciveTokenRes>(data, (res) => LocalPlayerData.SetToken(res, action), _error);
        }

        /// <summary>
        /// 계정 생성
        /// 아즈메타 계정 생성 시에만 패스워드 필수, 이외에는 필요 없음
        /// </summary>
        public void CreateAccount(string _email, string _password = null, Action<ReciveTokenRes> _res = null, Action<DefaultPacketRes> _error = null)
        {
            var packet = new
            {
                email = _email,
                password = _password,
                regPathType = Single.Web.GetRegPathType(),
            };

            var data = new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.CreateAccount, packet)
                .SetAuthHeader(AuthHeader.None);

            Single.Web.SendRequest(data, _res, _error);
        }

        /// <summary>
        /// 로그인
        /// </summary>
        public void Login(string _accountToken, string _password = null, Action<ReciveTokenRes> _res = null, Action<DefaultPacketRes> _error = null)
        {
            byte[] bytesToEncode = Encoding.UTF8.GetBytes($"{_accountToken}:{_password}");
            string encodedText = Convert.ToBase64String(bytesToEncode);
            string basicAuth = $"Basic {encodedText}";

            var data = new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.Login)
                .SetAuthHeader(AuthHeader.None)
                .SetCustomHeader("authorization", basicAuth);

            Single.Web.SendRequest(data, _res, _error);
        }

        /// <summary>
        /// 자동 로그인
        /// </summary>
        public void AutoLogin(Action<ReciveTokenRes> _res, Action<DefaultPacketRes> _error = null)
        {
            var data = new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.AutoLogin)
                .SetAuthHeader(AuthHeader.Refresh);

            Single.Web.SendRequest(data, _res, _error);
        }

        /// <summary>
        /// 이메일 인증번호 받기
        /// </summary>
        public void AuthEmail(string email, Action<CheckEmailPacketRes> _res, Action<DefaultPacketRes> _error = null)
        {
            var packet = new CheckEmailPacketReq()
            {
                email = email
            };

            var data = new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.AuthEmail, packet)
                .SetAuthHeader(AuthHeader.None);

            Single.Web.SendRequest(data, _res, _error);
        }

        /// <summary>
        /// 이메일 인증 확인
        /// </summary>
        public void ConfirmEmail(string email, int authCode, Action<AuthEmailPacketRes> _res, Action<DefaultPacketRes> _error = null)
        {
            var packet = new AuthEmailPacketReq()
            {
                email = email,
                authCode = authCode
            };

            var data = new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.ConfirmEmail, packet)
                .SetAuthHeader(AuthHeader.None);

            Single.Web.SendRequest(data, _res, _error);
        }

        /// <summary>
        /// 패스워드 재설정
        /// </summary>
        public void ResetPassword(string email, Action<DefaultPacketRes> _res, Action<DefaultPacketRes> _error = null)
        {
            var packet = new CheckEmailPacketReq()
            {
                email = email
            };

            var data = new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.ResetPassword, packet)
                .SetAuthHeader(AuthHeader.None);

            Single.Web.SendRequest(data, _res, _error);
        }
    }
}