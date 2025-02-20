/*===========================================================================================
*                                 
*   - 백그라운드에서 카운트다운 실행
*   - 설정된 시간을 차감하여 매 초, 종료 시점을 호출해주는 클래스
*   
*   - 카운트다운 시작 메서드 및 편의성을 위해 ChainedMetiod로 Action 등록
*   - 개별 Action 등록 및 제거, 전체 Action 제거, 카운트다운 종료 메서드
*   
*   - MEC(More Effective Coroutines) 에셋 사용 
*   -> MonoBehavior 필요 X, 문자열로 코루틴 객체 관리, 별도 스레드풀에서 실행하여 빠르고 끊김 X
* 
===========================================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;
using System;

public class CountDownManager : Singleton<CountDownManager>
{
    #region 변수
    private Dictionary<string, Action<int>> secondsAct = new Dictionary<string, Action<int>>();
    private Dictionary<string, Action> endAct = new Dictionary<string, Action>();
    private string curKey = null;
    #endregion

    #region 중요 메소드
    public void SetCountDown(string key, int time)
    {
        curKey = key; // ChainedMethod를 위한 Key 캐싱
        Util.RunCoroutine(Co_CountDown(key, time), key); // 코루틴 에셋 내부에서 자동으로 개별 객체를 관리
    }

    private IEnumerator<float> Co_CountDown(string key, int remainTime)
    {
        while (remainTime > 0)
        {
            if (isComeBackApp)
            {
                isComeBackApp = false;
                Debug.Log("앱이 멈췄던 시간(초) : " + calTime);
                remainTime -= calTime;
            }

            // 매 초 호출되는 Action
            if (secondsAct.ContainsKey(key))
            {
                secondsAct[key]?.Invoke(remainTime);
            }

            yield return Timing.WaitForSeconds(1);
            remainTime--;
        }

        // 종료 시 호출되는 Action
        if (endAct.ContainsKey(key))
        {
            endAct[key]?.Invoke();
        }
    }
    #endregion

    #region 액션 등록 및 제거
    /// <summary>
    /// ChainedMethod 초 당 호출 Action 등록
    /// </summary>
    public CountDownManager SetSecondAction(Action<int> action)
    {
        AddSecondAction(curKey, action);
        return this;
    }

    /// <summary>
    /// ChainedMethod 종료 시 호출 Action 등록
    /// </summary>
    public CountDownManager SetEndAction(Action action)
    {
        AddEndAction(curKey, action);
        return this;
    }

    /// <summary>
    /// 초 당 호출 Action 등록
    /// </summary>
    public void AddSecondAction(string key, Action<int> action)
    {
        if (!secondsAct.ContainsKey(key))
        {
            secondsAct.Add(key, default);
        }
        secondsAct[key] += action;
    }

    /// <summary>
    /// 종료 시 호출 Action 등록
    /// </summary>
    public void AddEndAction(string key, Action action)
    {
        if (!endAct.ContainsKey(key))
        {
            endAct.Add(key, default);
        }
        endAct[key] += action;
    }

    /// <summary>
    /// 개별 초 당 호출 Action 제거
    /// </summary>
    public void RemoveSecondAction(string key, Action<int> action)
    {
        if (secondsAct.ContainsKey(key))
        {
            secondsAct[key] -= action;
        }
    }

    /// <summary>
    /// 개별 종료 시 호출 Action 제거
    /// </summary>
    public void RemoveEndAction(string key, Action action)
    {
        if (endAct.ContainsKey(key))
        {
            endAct[key] -= action;
        }
    }

    /// <summary>
    /// 전체 초 당 호출 Action 제거
    /// </summary>
    public void AllRemoveSecondAction(string key)
    {
        if (secondsAct.ContainsKey(key))
        {
            secondsAct[key] = null;
        }
    }

    /// <summary>
    /// 전체 종료 시 호출 Action 제거
    /// </summary>
    public void AllRemoveEndAction(string key)
    {
        if (endAct.ContainsKey(key))
        {
            endAct[key] = null;
        }
    }

    /// <summary>
    /// 카운트 다운 코루틴 종료
    /// </summary>
    public void KillCountDown(string key)
    {
        Util.KillCoroutine(key);
    }
    #endregion

    #region 앱 정지 시 처리
    private bool isComeBackApp = false;
    private int calTime = 0;

    private DateTime pauseTime;

    protected override void OnApplicationPause(bool _isPaused)
    {
        base.OnApplicationPause(_isPaused);

        if (_isPaused)
        {
            calTime = 0;
            pauseTime = DateTime.Now;
        }
        else
        {
            isComeBackApp = true;
            calTime = (int)(DateTime.Now - pauseTime).TotalSeconds;
        }
    }
    #endregion
}