/*===========================================================================================
*                                 
*   - 1인 다회 투표 가능
*   - 웹페이지에서 투표를 개설하고 제목, 기간, 후보 등록 및 수정 등 설정하여 해당 데이터를 받아온다
*   
*   - API 호출 -> UI 활성화 및 데이터에 따른 아이템 생성
* 
===========================================================================================*/

#region API 호출부
// 전반적인 콘텐츠 화면에서 투표 기능이 호출되기 때문에
// 타 스크립트(Util.cs)에서 API 호출 및 UI 활성화 로직을 static으로 작성
public static void KTMFVote()
{
    Single.Web.selectVote.GetKTMFVoteInfo((res) =>
    {
        foreach (var item in Util.Enum2List<KTMF_REMAINTIME>())
        {
            // Count Down 코루틴 호출 (투표 종료, 결과 노출 시작, 결과 노출 종료)
            CountDownManager.Instance.SetCountDown(Util.Enum2String(item), res.GetRemain(item));
        }

        SceneLogic.instance.PushPanel<Panel_KTMFVote>().SetData(res); // UI 활성화 및 Responese Data 전달 
    },
    (error) =>
    {
        SceneLogic.instance.GetPanel<Panel_HUD>().viewHudTopRight.SetActiveKTMFVote(false);
    });
}
#endregion

#region UI 세팅부
// 투표 기능 UI 스크립트
public class View_KTMFVote : UIBase
{
    #region 변수
    private List<Item_KTMFPage> pages = new List<Item_KTMFPage>();
    public List<Item_KTMFProfile> profiles = new List<Item_KTMFProfile>();

    private HorizontalScrollSnap scrollSnap;
    private PaginationManager paginationManager;

    private Transform toggleParent;

    private GetKTMFVoteInfoPacketRes data;

    private int VoteItemCount => data.voteItems.Length;
    private float DivNum => VoteItemCount < 26 ? 5f : 10f; // 25개 이하: 5개 분배, 26개 이상: 10개 분배
    #endregion 

    protected override void SetMemberUI()
    {
        #region etc
        scrollSnap = GetChildGObject("go_ScrollView").GetComponent<HorizontalScrollSnap>();
        paginationManager = GetUI<ToggleGroup>("togg_Rig").GetComponent<PaginationManager>();

        toggleParent = paginationManager.transform;
        #endregion
    }

    #region 초기화
    // 데이터 할당 시 호출하는 메서드
    public void SetData(GetKTMFVoteInfoPacketRes _data)
    {
        data = _data;

        if (data == null) return;

        InitData();
        SetUI();
    }

    // 페이지네이션 및 하위 아이템 오브젝트 제거
    private void InitData()
    {
        pages.Clear();
        profiles.Clear();
        Util.ClearProcessQueue("KTMFProflie");

        scrollSnap.RemoveAllChildren(out GameObject[] ChildrenRemoved);
        foreach (var item in ChildrenRemoved)
        {
            Destroy(item);
        }

        paginationManager.gameObject.Children().Destroy();
    }

    private async void SetUI()
    {
        if (VoteItemCount == 0) return;

        int pageCount = Mathf.CeilToInt(VoteItemCount / DivNum);
        for (int i = 0; i < pageCount; i++)
        {
            pages.Add(CreatePage());
            CreateToggle();
        }

        // 투표 항목 개수에 따른 아이템 오브젝트 생성
        List<VoteItem> votes = data.voteItems.OrderBy(x => x.displayNum).ToList();
        for (int i = 0; i < VoteItemCount; i++)
        {
            var profile = CreateProfile();
            profile.SetData(new Item_KTMFProfileData(data.selectVoteInfo.id, IsSelect(votes[i].itemNum), votes[i]));
            // 개수에 따른 페이지 분배
            pages[(int)(i / DivNum)].SetItemParent(profile.gameObject);

            profiles.Add(profile);
        }

        await UniTask.Delay(200);

        SetUpPagination();
    }

    /// <summary>
    /// 페이지네이션 에셋 적용
    /// </summary>
    private void SetUpPagination()
    {
        scrollSnap.DistributePages();
        paginationManager.ResetPaginationChildren();

        paginationManager.GoToScreen(0);
    }
    #endregion

    #region 
    /// <summary>
    /// 선택한 후보 체크 켜기, 데이터 변경
    /// </summary>
    public void SelectItemCheck()
    {
        int count = data.myVote.Length;

        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            int selectNum = data.myVote[i].itemNum;
            Item_KTMFProfile item = profiles.FirstOrDefault(x => x.ItemNum == selectNum);
            if (item != null)
            {
                item.ChangeSelectState(true);
            }
        }
    }

    /// <summary>
    /// 좋아요 데이터 변경
    /// </summary>
    public void LikeItemUpdate(LikeInfo likeInfo)
    {
        Item_KTMFProfile item = profiles.FirstOrDefault(x => x.ItemNum == likeInfo.itemNum);
        if (item != null)
        {
            item.ChangeLikeState(likeInfo);
        }
    }

    /// <summary>
    /// 페이지 아이템 생성 및 페이지네이션 에셋에 등록
    /// </summary>
    private Item_KTMFPage CreatePage()
    {
        var page = Instantiate(Single.Resources.Load<GameObject>(Cons.Path_Prefab_Item + Cons.Item_KTMFPage)).GetComponent<Item_KTMFPage>();
        scrollSnap.AddChild(page.gameObject);

        return page;
    }

    /// <summary>
    /// 프로필 아이템 생성
    /// </summary>
    private Item_KTMFProfile CreateProfile()
    {
        var profile = Instantiate(Single.Resources.Load<GameObject>(Cons.Path_Prefab_Item + Cons.Item_KTMFProfile));

        return profile.GetComponent<Item_KTMFProfile>();
    }

    /// <summary>
    /// 토글 아이템 생성 및 부모 설정
    /// </summary>
    private GameObject CreateToggle()
    {
        var toggle = Instantiate(Single.Resources.Load<GameObject>(Cons.Path_Prefab_UI + Cons.tog_KTMFVote));
        Util.SetParentPosition(toggle, toggleParent);

        return toggle;
    }

    /// <summary>
    /// 자신이 선택한 후보자인지 여부
    /// </summary>
    private bool IsSelect(int itemNum)
    {
        MyVote myVote = data.myVote.FirstOrDefault(x => x.itemNum == itemNum);
        return myVote != null;
    }
    #endregion
}
#endregion