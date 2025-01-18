using CryptoWebRequestSample;
using System;

/*===========================================================================================
*
* Example - 사용 예시 코드
* 
===========================================================================================*/
public class Example
{
    private void Login()
    {
        Single.Web.Login(input_Email.text, input_Password.text,
        (res) =>
        {
            Single.Web.SetLocalPlayerData(res);
        },
        (error) =>
        {
            PushPopup<Popup_Basic>()
                .ChainPopupData(new PopupData(POPUPICON.WARNING, BTN_TYPE.Confirm, null, new MasterLocalData("3009")))
                .ChainPopupAction(new PopupAction(() => input_Password.Clear()));
        });
    }
}

/*===========================================================================================
*
*   Rest API 통신 클래스
*                             
*   WebManager (현재 스크립트) 
*                          
*   - partial을 통해 카테고리 별 API 분류, 각 데이터 요청에 맞는 메서드 작성
* 
===========================================================================================*/

public partial class WebManager : Singleton<WebManager>
{
    /// <summary>
    /// 계정 생성
    /// 아즈메타 계정 생성 시에만 패스워드 필수, 이외에는 필요 없음
    /// </summary>
    public void CreateAccount(string accountToken, string password = null, Action<CreateAccountLoginPacketRes> _res = null, Action<DefaultPacketRes> _error = null)
    {
        var packet = new
        {
            accountToken = ClsCrypto.EncryptByAES(accountToken),
            password = ClsCrypto.EncryptByAES(password),
            regPathType = (int)Single.Web.GetRegPathType(),
        };

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.CreateAccount, packet), _res, _error);
    }

    /// <summary>
    /// 로그인
    /// 아즈메타 계정 로그인 시에만 패스워드 필수, 이외에는 필요 없음
    /// </summary>
    public void Login(string accountToken, string password = null, Action<LoginPacketRes> _res = null, Action<DefaultPacketRes> _error = null)
    {
        var packet = new
        {
            accountToken = ClsCrypto.EncryptByAES(accountToken),
            password = ClsCrypto.EncryptByAES(password),
        };

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.Login, packet), _res, _error);
    }

    /// <summary>
    /// 소셜 로그인 및 계정 생성
    /// </summary>
    public void SocialAccountLogin(Action<SocialAccountLoginPacketRes> _res = null, Action<DefaultPacketRes> _error = null)
    {
        var packet = new SocialAccountLoginPacketReq()
        {
            accountToken = ClsCrypto.EncryptByAES(LocalPlayerData.Method.AccountToken),
            providerType = LocalPlayerData.ProviderType,
            regPathType = (int)Single.Web.GetRegPathType(),
        };

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.SocialAccountLogin, packet), _res, _error);
    }

    /// <summary>
    /// 계정 연동
    /// </summary>
    public void LinkedAccount(string accountToken, string password, int providerType, Action<CurrentAccountPacketRes> _res, Action<DefaultPacketRes> _error = null)
    {
        var packet = new LinkedAccountPacketReq()
        {
            accountToken = ClsCrypto.EncryptByAES(accountToken),
            providerType = providerType,
            password = ClsCrypto.EncryptByAES(password)
        };

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.LinkedAccount, packet), _res, _error);
    }

    /// <summary>
    /// 계정 연동 해제
    /// </summary>
    public void ReleaseLinkedAccount(int providerType, Action<ReleaseLinkedAccountPacketRes> _res, Action<DefaultPacketRes> _error = null)
    {
        SendRequest(new SendRequestData(eHttpVerb.DELETE, AccountServerUrl, WebPacket.ReleaseLinkedAccount(providerType)), _res, _error);
    }

    /// <summary>
    /// 로그인 인증
    /// 로그인 시 얻은 로그인 토큰으로 유효성 검증
    /// </summary>
    public void LoginAuth(Action<LoginAuthPacketRes> _res, Action<DefaultPacketRes> _error = null)
    {
        var packet = new
        {
            loginToken = ClsCrypto.EncryptByAES(LocalPlayerData.LoginToken)
        };

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.LoginAuth, packet, false), _res, _error);
    }

    /// <summary>
    /// 자동 로그인
    /// </summary>
    public void AutoLogin(Action<LoginPacketRes> _res, Action<DefaultPacketRes> _error = null)
    {
        var packet = new
        {
            memberId = ClsCrypto.EncryptByAES(LocalPlayerData.MemberID),
            providerType = LocalPlayerData.ProviderType
        };

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.AutoLogin, packet, false), _res, _error);
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

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.AuthEmail, packet), _res, _error);
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

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.ConfirmEmail, packet), _res, _error);
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

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.ResetPassword, packet), _res, _error);
    }

    /// <summary>
    /// 아즈메타 계정 여부 확인
    /// </summary>
    public void CheckArzmetaAccount(string accountToken, string password, Action<DefaultPacketRes> _res, Action<DefaultPacketRes> _error = null)
    {
        var packet = new
        {
            accountToken = ClsCrypto.EncryptByAES(accountToken),
            password = ClsCrypto.EncryptByAES(password),
        };

        SendRequest(new SendRequestData(eHttpVerb.POST, AccountServerUrl, WebPacket.CheckArzmetaAccount, packet), _res, _error);
    }
}
