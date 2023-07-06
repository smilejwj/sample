using Cinemachine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> 채팅 캔버스 컨트롤러 </summary>
public class Canvas_Chat : Canvas_Base
{

    #region 변수

    [Header("패널")]
    public RectTransform _rtPannel;
    bool _isPannelMoving = false;
    Vector2 _endPos;

    [Header("탭")]
    [SerializeField] List<Image> _list_TabBtn = new List<Image>();
    [SerializeField] GameObject _objWhisperTabNewMark;
    [SerializeField] GameObject _objMainUIUnderbarNewMark;
    [SerializeField] Sprite[] _sprTabBtn;

    [Header("귓속말 기능")]
    [SerializeField] GameObject _objWhisperList;
    [SerializeField] GameObject _objWhisperBtn_Origin;
    [SerializeField] RectTransform _rtWhisperTargetArea; // 채팅 대상을 선택하는 범위 오브젝트
    [SerializeField] GameObject _objLetWhisper;
    List<OP_WhisperTarget> _list_WhisperTargetBtn = new List<OP_WhisperTarget>();

    [HideInInspector] public Util.ChatType _chatType { get; set; }

    [Header("로그 기능")]
    public TMP_InputField _inputMassage;
    public RectTransform _rtLogContents;
    public GameObject _objChatLog_Origin;

    List<Transform> _List_LogObject = new List<Transform>();
    int _logIndex;
    string _lastLogNick; // 연속 채팅 유무 체크용

    #endregion

    #region Methods

    // 채팅 Done 처리용 이벤트 등록
    private void ChatOnSubmitAddListener() => _inputMassage.onSubmit.AddListener(delegate { OnBtn_SendChatData(); });

    #endregion

    #region Override 함수

    public override void Init()
    {
        base.Init();

        // 채팅 로그 풀링 : 50개
        for (int i = 0; i < 50; i++)
        {
            var obj = Instantiate(_objChatLog_Origin, _rtLogContents);
            _List_LogObject.Add(obj.transform);
            obj.SetActive(false);
        }

        // 이동 위치 저장
        _endPos = new Vector2(_rtPannel.rect.width, 0);

        // 귓말 대상 선택 버튼 풀링
        InitAllWhisperBtn();

        // 디폴트 방 안
        SetChatTabColor(1);

        _chatType = Util.ChatType.RoomChat;

        // 채팅 Done 처리
        ChatOnSubmitAddListener();
    }

    public override void OpenUI()
    {
        // 이동중이면 취소
        if (_isPannelMoving)
            return;

        // 활성 : 예외- Base에 있는 카메라 투시 거리 조정 필요 없음
        // 메니저에 열린 캔버스 등록
        Managers.UI.RegisterOpendUI(this);

        // 메인 오브젝트 활성
        _MainObj.SetActive(true);

        // 캔버스 렌더 활성 : 최적화
        GetComponent<Canvas>().enabled = true;

        // 화면 시작 위치로
        _rtPannel.anchoredPosition = Vector2.zero;

        // 패널 이동
        OpenMotion().Forget();

        // 캐릭터 인풋 비활성
        InputManager.Instance.IsCharacterMove = false;

        // 사운드
        SoundManager.GetInstance.StartFX_OneShot(Util.sfxType.Page);

        // 데이터 적용
        ResetChatLog().Forget();

    }

    public override void CloseUI()
    {
        // 이동중이면 취소
        if (_isPannelMoving)
            return;

        // 패널 이동 + 오브젝트 비활성
        CloseMotion().Forget();


        // 사운드
        SoundManager.GetInstance.StartFX_OneShot(Util.sfxType.Page);
    }

    public override void ForceCloseUI()
    {
        if (Managers.InteractionList.UserList.ContainsKey(Managers.Data.UserId))
        {
            Managers.UI.RemoveOpendUI(this);
            _MainObj.SetActive(false);
            GetComponent<Canvas>().enabled = false;

            // 카메라 투시거리 확대 : 최적화
            Camera.main.farClipPlane = 200f;
            if (Camera.main.transform.parent != null)
            {
                var Cinemachine = Camera.main.transform.parent.GetComponentInChildren<CinemachineVirtualCamera>();
                if (Cinemachine != null)
                    Cinemachine.m_Lens.FarClipPlane = 200f;

            }
        }
        else
            base.ForceCloseUI();
    }

    #endregion



    #region 열기/닫기 모션 함수

    /// <summary> 메인 패널 열리는 모션 </summary>
    async UniTaskVoid OpenMotion()
    {
        // 무빙 시작
        _isPannelMoving = true;

        // 오브젝트 활성후, 자동 정렬되는 딜레이
        await UniTask.DelayFrame(10);

        // 무빙
        while (_rtPannel.anchoredPosition.x < _endPos.x)
        {
            _rtPannel.anchoredPosition = Vector2.Lerp(_rtPannel.anchoredPosition, _endPos * 1.02f, Time.deltaTime * 10);

            await UniTask.DelayFrame(1);
        }

        // 정확한 목표 포인트로 이동
        _rtPannel.anchoredPosition = _endPos;

        // 무빙 완료
        _isPannelMoving = false;
    }

    /// <summary> 메인 패널 닫는 모션 </summary>
    async UniTaskVoid CloseMotion()
    {

        // 무빙 시작
        _isPannelMoving = true;

        // 무빙
        var hidePos = new Vector2(-10, 0);
        while (_rtPannel.anchoredPosition.x > 0)
        {
            _rtPannel.anchoredPosition = Vector2.Lerp(_rtPannel.anchoredPosition, hidePos, Time.deltaTime * 10);

            await UniTask.DelayFrame(1);
        }

        // 정확한 목표 포인트로 이동
        _rtPannel.anchoredPosition = Vector2.zero;

        // 무빙 완료
        _isPannelMoving = false;

        // 오브젝트 비활성
        ForceCloseUI();
    }

    #endregion


    #region 채팅 Tab 

    /// <summary> 채팅 타입 탭 </summary>
    public void OnBtn_ChatTab(int index)
    {
        // 탭 버튼 색 변경
        SetChatTabColor(index);

        // 채팅 타입 변경
        _chatType = (Util.ChatType)index;

        // 귓속말 권유 비활성
        _objLetWhisper.SetActive(false);

        // 귓속말 리스트 활성/비활성
        _objWhisperList.SetActive(_chatType == Util.ChatType.WhisperChat);

        // 귓말 리스트 Layout 재정렬
        if (_chatType == Util.ChatType.WhisperChat)
            ReplaceWhisperList().Forget();


        // 데이터 적용
        ResetChatLog().Forget();

        SoundManager.GetInstance.StartFX_OneShot(Util.sfxType.Button);
    }

    /// <summary> 선택된 채팅 그룹 버튼 색 설정 </summary>
    void SetChatTabColor(int index)
    {
        for (int i = 0; i < _list_TabBtn.Count; i++)
        {
            Color TextColor = Util_Palette.Instance.GetColor(Util.Palette.gray40);

            if (i == index)
            {
                // 버튼 리소스 교체 
                _list_TabBtn[i].sprite = _sprTabBtn[1];
                _list_TabBtn[i].color = Color.white;

                // 버튼 인덱스에 따른 색 획득
                switch (index)
                {
                    case 0: TextColor = Util_Palette.Instance.GetColor(Util.Palette.green); break;
                    case 1: TextColor = Util_Palette.Instance.GetColor(Util.Palette.blue); break;
                    case 2: TextColor = Util_Palette.Instance.GetColor(Util.Palette.orange); break;
                }
            }
            else
            {
                // 버튼 리소스 교체
                _list_TabBtn[i].sprite = _sprTabBtn[0];
                _list_TabBtn[i].color = Util_Palette.Instance.GetColor(Util.Palette.gray90);

            }

            // 버튼 텍스트 색 변경
            _list_TabBtn[i].GetComponentInChildren<TextMeshProUGUI>().color = TextColor;

        }
    }


    #endregion


    #region 귓속말 기능

    /// <summary> 귓말 상대 버튼 클릭 처리 </summary> 
    public void OnBtn_WhisperTarget(string userId)
    {
        // 귓속말 대상 교체
        Managers.Chat.SetWhisperTarget(userId);

        // 귓말 상대 버튼 칼라
        SetAllWhisperBtnColor(userId);

        // 채팅 데이터 재적용
        ResetChatLog().Forget();

        SoundManager.GetInstance.StartFX_OneShot(Util.sfxType.Button);
    }

    /// <summary> 귓말 종료 버튼 처리 </summary>
    public void OnBtn_WhisperTargetClose(string userID)
    {
        // 현재 선택된 귓말 상대일 겨우
        if (Managers.Chat._WhisperTargetUserID == userID)
        {
            // 데이터 삭제
            Managers.Chat.DeleteWhisper(userID);

            // 채팅 로그 재적용
            ResetChatLog().Forget();
        }
        else
            // 데이터 삭제
            Managers.Chat.DeleteWhisper(userID);


        SoundManager.GetInstance.StartFX_OneShot(Util.sfxType.Button);
    }

    /// <summary> 새로운 귓말을 시작해보세요! 버튼 클릭 처리 </summary>
    public void OnBtn_LetNewWhisper()
    {
        ForceCloseUI();
        Managers.UI._Canvas.FindCanvas<Canvas_UserList>().OpenUI();
    }

    /// <summary> 새로운 귓말 대상 버튼 활성 </summary>
    public void StartNewWhipser(string userid, string nick)
    {
        if (!Managers.Chat._Dic_WhisperChat.ContainsKey(userid))
        {
            // 데이터 생성 
            Managers.Chat.CreateNewWhisperData(userid);

            // 새 귓말 유저 버튼 활성
            ActivateNewWhisperBtn(userid, nick);
        }

        // 해당 귓말 활성
        OnBtn_WhisperTarget(userid);

        // 귓말탭
        OnBtn_ChatTab(2);
    }

    /// <summary> 중앙 하단 바에 New 마크 활성 </summary>
    public void OnNewMark_MainUnderBar() => _objMainUIUnderbarNewMark.SetActive(true);

    /// <summary> 귓속말 탭에 New 마크 활성 </summary>  
    public void OnNewMark_WhisperTab() => _objWhisperTabNewMark.SetActive(true);


    /// <summary> 귓말 대상 버튼 풀링</summary>
    void InitAllWhisperBtn()
    {
        // 풀링
        for (int i = 0; i < 5; i++)
            InstantiateWhisperBtn();
    }

    /// <summary> 귓말 대상 선택하는 버튼 생성용 </summary>
    void InstantiateWhisperBtn()
    {
        // 오브젝트 생성 및 비활성
        var obj = Instantiate(_objWhisperBtn_Origin, _rtWhisperTargetArea);
        obj.SetActive(false);

        // 캔버스 정보 저장
        var op = obj.GetComponent<OP_WhisperTarget>();
        op.SetCanvasChat(this);

        // 리스트 등록
        _list_WhisperTargetBtn.Add(op);
    }

    /// <summary> 새로운 귓말 대상 버튼 활성 </summary>
    public void ActivateNewWhisperBtn(string userid, string nick)
    {
        // op에 유저 아이디 등록
        var op = GetWhisperBtn_Empty();
        op.SetBtn(userid, nick).Forget();
        op.gameObject.SetActive(true);
    }

    /// <summary> 귓말 대상 빈 버튼 획득 </summary>
    OP_WhisperTarget GetWhisperBtn_Empty()
    {
        // 개수 캐싱
        int cnt = _list_WhisperTargetBtn.Count;

        // 순회 및 빈자리 리턴
        for (int i = 0; i < cnt; i++)
        {
            if (string.IsNullOrEmpty(_list_WhisperTargetBtn[i].GetUserID()))
                return _list_WhisperTargetBtn[i];
        }

        // 빈자리 없으면 추가 풀링
        for (int i = 0; i < 5; i++)
            InstantiateWhisperBtn();

        // 추가 풀링 첫번째 항목 리턴
        return _list_WhisperTargetBtn[cnt];
    }

    public OP_WhisperTarget GetWhisperBtn_UserID(string userID) => _list_WhisperTargetBtn.Find((e) => { return e.GetUserID() == userID; });

    /// <summary> 모든 귓말 상대 버튼 칼라 세팅 </summary>
    void SetAllWhisperBtnColor(string selectUserId)
    {
        // 모든 버튼 비선택 상태로
        for (int i = 0; i < _list_WhisperTargetBtn.Count; i++)
            _list_WhisperTargetBtn[i].SetBtnColor(false);

        // 선택된 버튼만 선택 상태로
        GetWhisperBtn_UserID(selectUserId).SetBtnColor(true);
    }

    async UniTaskVoid ReplaceWhisperList()
    {
        await UniTask.DelayFrame(1);

        // 각 귓말별 버튼 사이즈 재적용
        for (int i = 0; i < _list_WhisperTargetBtn.Count; i++)
            _list_WhisperTargetBtn[i].SetBtnSize().Forget();

        // 귓말 대상 버튼 재정렬 
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rtWhisperTargetArea);

    }

    #endregion


    #region 채팅 입력 함수

    public void OnBtn_SendChatData()
    {
        // 텍스트 입력 유무 확인
        if (string.IsNullOrEmpty(_inputMassage.text))
            return;

        // 비속어 체크 <<<

        // 데이터 전송 
        switch (_chatType)
        {
            case Util.ChatType.AllChat:
                Managers.Socket.Chat.SendAllChat(Managers.Data.UserId, Managers.Data.NickName, _inputMassage.text);
                Managers.Achieve.PostAchieveData("chat");//업적카운트
                break;
            case Util.ChatType.RoomChat:
                Managers.Socket.Chat.SendRoomChat(Managers.Data.UserId, Managers.Data.NickName, _inputMassage.text);
                Managers.Achieve.PostAchieveData("chat");//업적카운트
                break;

            case Util.ChatType.WhisperChat:
                Managers.Socket.Chat.SendUserChat(Managers.Data.UserId, Managers.Data.NickName, Managers.AllUsers.GetPacket(Managers.Chat._WhisperTargetUserID).id, _inputMassage.text);
                Managers.Achieve.PostAchieveData("whisper");//업적카운트
                break;
        }

        // 텍스트 삭제
        _inputMassage.text = "";

        // 인풋 재활성 (다시 작성 가능한 상태로)
        _inputMassage.ActivateInputField();

    }


    #endregion


    #region 채팅 로그 함수

    /// <summary> 데이터를 받아 채팅을 추가하는 함수 </summary>
    public void AddChat(string massage, string nick)
    {
        // 전 채팅 같은 유저인가 체크
        bool same = nick == _lastLogNick;
        _lastLogNick = nick;

        // 채팅 데이터 적용
        Transform obj = _List_LogObject[_logIndex++];

        // 로그 색 획득
        Color col = Util_Palette.Instance.GetColor(Util.Palette.gray90);
        if (nick == Managers.Data.NickName)
        {
            switch (_chatType)
            {
                case Util.ChatType.AllChat: col = Util_Palette.Instance.GetColor(Util.Palette.green); break;
                case Util.ChatType.RoomChat: col = Util_Palette.Instance.GetColor(Util.Palette.blue); break;
                case Util.ChatType.WhisperChat: col = Util_Palette.Instance.GetColor(Util.Palette.orange); break;
            }
        }

        // 채팅 데이터 적용
        obj.GetComponent<OP_ChatLog>().SetChat(massage, nick, same, col).Forget();

        // 오브젝트 최하단
        obj.SetAsLastSibling();

        // 인덱스 카운팅
        if (_logIndex >= _List_LogObject.Count)
            _logIndex = 0;

        // 스크롤 최하단
        _rtLogContents.anchoredPosition = Vector2.zero;

        // 오브젝트 활성
        obj.gameObject.SetActive(true);
    }

    /// <summary> 채팅 로그 재적용 </summary>
    public async UniTaskVoid ResetChatLog()
    {
        await UniTask.NextFrame();

        _lastLogNick = "";
        List<PacketChat> data;

        // 로그 오브젝트 모두 비활성
        AllLogUnactive();


        // 채팅 타겟 구분
        switch (_chatType)
        {
            case Util.ChatType.WhisperChat:
                data = Managers.Chat.GetWhisperChatList();

                // 귓말 대상이 세팅되어있지 않는 경우 <<<
                if (data == null)
                {
                    // 귓말 데이터 유무 확인
                    if (Managers.Chat._Dic_WhisperChat.Count > 0)
                    {
                        // 가장 앞 유저로 대상 지정
                        Managers.Chat.SetWhisperTarget(_list_WhisperTargetBtn[0].GetUserID());
                        data = Managers.Chat.GetWhisperChatList();
                    }
                    else
                    {
                        // 새로운 귓말 권유 표시
                        _objLetWhisper.SetActive(true);
                        return;
                    }
                }

                break;

            case Util.ChatType.RoomChat:
                data = Managers.Chat._List_RoomChat;
                break;

            // 전체 채팅 정보 
            default:
                data = Managers.Chat._List_AllChat;
                break;
        }


        // 프레임 딜레이로 인한 순간포착 안보이게 하기 위한 장치
        _rtLogContents.GetComponent<CanvasGroup>().alpha = 0;

        // 데이터 재적용
        RefreshAllChatLog(data);

        // 안보이기 장치 해제
        await UniTask.NextFrame();
        var _cg = _rtLogContents.GetComponent<CanvasGroup>();
        while (_cg.alpha < 1)
        {
            _cg.alpha = Mathf.Lerp(_cg.alpha, 1.5f, 5 * Time.deltaTime);
            await UniTask.NextFrame();
        }

    }

    /// <summary> 받은 데이터로 Log 작성 </summary>
    void RefreshAllChatLog(List<PacketChat> data)
    {
        if (data == null)
            return;

        int startIndex = 0;

        // 데이터 개수 체크 : 로그 갯수가 50개라 뒤에서 50개만 적용
        if (data.Count > 50)
            startIndex = data.Count - 50;

        //  텍스트 적용
        for (int i = startIndex; i < data.Count; i++)
            AddChat(data[i].msg, data[i].name);

    }

    /// <summary> 모든 로그 비활성 </summary>
    void AllLogUnactive()
    {
        for (int i = 0; i < _List_LogObject.Count; i++)
            _List_LogObject[i].gameObject.SetActive(false);
    }

    #endregion



}
