using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

/// <summary> 유저 리스트 관리 </summary>
public class Canvas_UserList : Canvas_Base
{

    #region 변수

    [SerializeField] GameObject _op_FriendObject;
    [SerializeField] RectTransform _contentTransform;
    [SerializeField] Image[] _imgTabBtn;
    [SerializeField] GameObject _objRequestNewMark;

    List<OP_FriendObject> _List_OP= new List<OP_FriendObject>();
    PacketFriends[] _List_Friend  ;
    PacketFriends[] _List_Send    ;
    PacketFriends[] _List_Recieve;


    int _ListType = 0; // 0:전체  1:친구  2:요청

    #endregion

    #region Override 함수

    public override void Init()
    {
        base.Init();

        InitPooling();
    }

    public override void OpenUI()
    {
        // 데이터 획득
        UpdataApiData().Forget();

        base.OpenUI();

    }

    #endregion


    #region 탭 항목

    /// <summary> 탭 클릭 처리  </summary>
    public void OnBtn_Tab(int index)
    {
        // 타입 저장
        _ListType = index;

        // 버튼 칼라
        ChangeTabColor();

        // 데이터 세팅
        ResetUserData();

        if (index == 2)
            SetActive_RequestNewMark(false);

        // 사운드 
        SoundManager.GetInstance.StartFX_OneShot(Util.sfxType.Button);
    }

    void ChangeTabColor()
    {
        for (int i = 0; i < _imgTabBtn.Length; i++)
        {
            _imgTabBtn[i].color = i == _ListType ? Util_Palette.Instance.GetColor(Util.Palette.green) : Color.white;
            _imgTabBtn[i].GetComponentInChildren<TextMeshProUGUI>().color = Util_Palette.Instance.GetColor(i == _ListType ? Util.Palette.green : Util.Palette.gray40);
        }
    }

    public async UniTask Check_RequestNew()
    {
        await GetAPI_FriendRecieve();

        if (_List_Recieve.Length > 0)
        {
            SetActive_RequestNewMark(true);
            Managers.UI._Canvas.FindCanvas<Canvas_Main>().SetNewMark_UserList(true);
        }
    }
    public void SetActive_RequestNewMark(bool isOn) => _objRequestNewMark.SetActive(isOn);

    #endregion


    #region 친구 항목 관리

    /// <summary> 초기 풀링 </summary>
    void InitPooling()
    {
        for (int i = 0; i < 10; i++)
            InstantiateFriendOP();
    }

    /// <summary> 친구 항목 생성 </summary>
    void InstantiateFriendOP()
    {
        GameObject go = Instantiate(_op_FriendObject, _contentTransform);
        go.SetActive(false);
        _List_OP.Add(go.GetComponent< OP_FriendObject>());
    }


    /// <summary> 받은 데이터로 친구 목록 세팅 </summary>
    void ResetUserData()
    {
        PacketFriends[] data = null;
        switch (_ListType)
        {
            case 0: // 접속한 유저
                data = UserListToPacketFirends(Managers.AllUsers.GetUsersDic());
                break;

            case 1: // 친구 
                data = _List_Friend;

                break;

            case 2: // 친구 요청 받은 목록
                data = _List_Recieve;

                break;
        }

        // OP 비활성을 위한 인덱스 저장용
        int OffIndex = 0;

        if(data != null && data.Length != 0)
        {

            // 오브젝트 개수 체크
            if (data.Length > _List_OP.Count)
            {
                int needCnt = data.Length - _List_OP.Count + 1;
                for (int i = 0; i < needCnt; i++)
                    InstantiateFriendOP();
            }

            var user = Managers.AllUsers.GetUsersDic();
            for (int i = 0; i < data.Length; i++)
            {
                string userid = _ListType != 2 ? data[i].receiver.userid : data[i].sender.userid;
                string nick = _ListType != 2 ? data[i].receiver.nickname : data[i].sender.nickname;

                // 본인 체크 : 패스
                if (userid == Managers.Data.UserId)
                    continue;

                // 오브젝트 활성
                _List_OP[i].gameObject.SetActive(true);

                // 친구 상태 체크
                int state = 0;
                if (_ListType == 0)
                    state = ContainsApiData(_List_Friend, userid) ? 1 : (ContainsApiData(_List_Send, userid) ? 2 : 0);
                else
                    state = _ListType;


                // 데이터 세팅
                _List_OP[i].GetComponent<OP_FriendObject>().ResetData(
                    userid,
                    nick,
                    _ListType, 
                    user.ContainsKey(userid),
                    state);

            }

            OffIndex = data.Length;
        }


        // 안쓰는 오브젝트 비활성
        for (int i = OffIndex; i < _List_OP.Count; i++)
            _List_OP[i].gameObject.SetActive(false);

    }


    /// <summary> 가공 PacketClient -> PacketFriends </summary>
    PacketFriends[] UserListToPacketFirends(Dictionary<string, PacketClient> data)
    {
        if (data == null)
            return null;

        // 메모리 할당 : 본인 데이터는 빼고
        PacketFriends[] result = new PacketFriends[data.Count -1 ];

        // 순회 체크용 자신의 Userid 캐싱
        string me = Managers.Data.UserId;

        // 순회 및 데이터 대입
        int index = 0;
        foreach (var i in data)
        {
            if (i.Key == me)
                continue;

            result[index++] = new PacketFriends(i.Key , i.Value.character.nickname);
        }

        return result;
    }

    /// <summary> 유저와의 관계 확인용 함수 </summary>
    bool ContainsApiData(PacketFriends[] data, string userid)
    {
        if (data == null)
            return false;

        for (int i = 0; i < data.Length; i++)
            if (data[i].sender.userid == userid || data[i].receiver.userid == userid)
                return true;

        return false;
    }

    public bool GetIsFriend(string userid) => ContainsApiData(_List_Friend, userid);

    #endregion


    #region 통신 함수


    async UniTask UpdataApiData()
    {

        // 서버 대기중 표시 + 항목 미표시
        _contentTransform.gameObject.SetActive(false);
        Managers.UI._Popup.FindPopup<Popup_ServerWaiting>().OpenUI();

        await GetAPI_FriendData();
        await GetAPI_FriendSend();
        await GetAPI_FriendRecieve();

        // 항목 재적용
        ResetUserData();

        // 서버 대기중 제거 + 항목 표시
        Managers.UI._Popup.FindPopup<Popup_ServerWaiting>().ForceCloseUI();
        _contentTransform.gameObject.SetActive(true);

    }
    async UniTask GetAPI_FriendData()
    {
        try
        {
            Friends[] data = await Managers.API.Friend.GetFriendList(Managers.Data.UserId);

            // 순회돌며 데이터 가공
            _List_Friend = new PacketFriends[data.Length];
            for (int i = 0; i < data.Length; i++)
                _List_Friend[i] = new PacketFriends(data[i].friend.userid, data[i].friend.nickname);
        }
        catch
        {
            //Debug.LogError("친구 정보 API 오류");
        }

    }
    async UniTask GetAPI_FriendSend()
    {
        try
        {
            _List_Send = await Managers.API.Friend.FriendLookUp(Managers.Data.UserId);

        }
        catch
        {
            //Debug.LogError("친구 요청 보낸 정보 API 오류\n");
        }

    }
    async UniTask GetAPI_FriendRecieve()
    {
        try
        {
            _List_Recieve = await Managers.API.Friend.FriendREceptionLookUp(Managers.Data.UserId);

        }
        catch
        {
            //Debug.LogError("친구 요청 수신 정보 API 오류");
        }

    }


    /// <summary> 친구 해제 </summary>
    public async UniTask DeleteAPI_FriendRemove(string friendUserid)
    {
        if( await Managers.API.Friend.DeleteFriend(Managers.Data.UserId, friendUserid))
            UpdataApiData().Forget();
    }



    #endregion
}
