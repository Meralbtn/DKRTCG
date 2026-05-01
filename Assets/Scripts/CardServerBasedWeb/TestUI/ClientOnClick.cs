using UnityEngine;
using CardGameClient;
using System.Threading.Tasks;
using CardGame;
using System;
using CardGameApp;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using System.Collections.Generic;
public class ClientOnClick : MonoBehaviour
{
    public RoomListView _roomListView;
    public GameObject _roomEditPanel;
    public RoomTipUI _roomTipUI;
    public GameObject _menuUI;
    public GameObject _roomContentUI;


    private void Awake()
    {
        Debug.Log($"[Init] {gameObject.name} 上的脚本加载了，ID: {this.GetInstanceID()}");
    }

    private void Start()
    {
        CardClient.Instance._onRoomCreateOrJoin += OnClientGetRoom;
        EventCenter.AddListener(UIEvent.ON_CREATE_ROOM_SUCCESS, OnRoomCreateSuccess);
        EventCenter.AddListener(UIEvent.ON_PLAYER_STATE_GAME, StartGame);
        EventCenter.AddListener(UIEvent.ON_ROOM_PLAYER_UPDATE, OnPlayerUpdate);
        EventCenter.AddListener(UIEvent.ON_FORCE_LEAVE_ROOM, OnForceLeave);
    }
    public async void OnClick()
    {

        try
        {
            _ = CardClient.Instance.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }


    public void RoomSceneOpen()
    {
        SceneManager.LoadScene("RoomList");
    }

    public void ReturnMainSc()
    {
        SceneManager.LoadScene("MainMenu");
    }



    public void OnCreateRoom()
    {
        _roomTipUI.FreshUI();
        _roomEditPanel.SetActive(true);
    }

    public void OnCancelCreateRoom()
    {
        _roomEditPanel.SetActive(false);
    }

    private bool _isProcessing = false; // 防抖标志位

    public async void OnConfirmCreateRoom()
    {
        if (_isProcessing) return; // 如果正在请求，直接拦截

        _isProcessing = true;
        try
        {
            var roomName = _roomTipUI?._inputText?.text; // 安全访问

            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning("房间名称不能为空");
                _isProcessing = false;
                return;
            }

            // 发送请求
            await CardClient.Instance.TcpCreateRoomRequest(roomName, 2);
        }
        catch (Exception ex)
        {
            Debug.LogError($"创建房间请求异常: {ex.Message}");
        }
        finally
        {
            // 只有没被销毁时才重置标志位
            if (this != null) _isProcessing = false;
        }
    }

    public void OnRoomCreateSuccess(object data)
    {
        _menuUI.SetActive(false);
        _roomEditPanel.SetActive(false);
        _roomContentUI.SetActive(true);
        _roomContentUI.GetComponent<CardRoomUI>().CreateRoomUI();
    }



    public async void GetRoomList()
    {
        try
        {
            await CardClient.Instance.GetRoomListViaUDP();
            _roomListView.RefreshRoomList();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }


    //加载进入房间UI
    public void OnClientGetRoom()
    {
        _roomEditPanel.SetActive(false);
        _menuUI.SetActive(false);
        _roomContentUI.SetActive(true);
        _roomContentUI.GetComponent<CardRoomUI>().CreateRoomUI();
    }

    public async void OnGameStart()
    {
        try
        {
            Debug.Log("start con");
            await CardClient.Instance.TcpStartGameRequest(PlayerManager.Instance().GetPlayerData());
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    public void StartGame(object obj)
    {
        CardClient.Instance.StartLoadBattleScene();
    }

    private void OnPlayerUpdate(object obj)
    {
        var pkg = obj as RoomPlayerUpdatePackage;
        if (pkg == null) return;
        RefreshPlayerList(pkg.Players);
    }
    private void RefreshPlayerList(List<PlayerBaseData> players)
    {
        var roomUI = _roomContentUI.GetComponent<CardRoomUI>();
        if (roomUI != null)
        {
            roomUI.CreateRoomUI(); // 重新创建UI以刷新玩家列表
        }
    }

    public void OnClickLeaveRoom()
    {
        _ = CardClient.Instance.SendLeaveRoom();
        _menuUI.SetActive(true);
        _roomContentUI.SetActive(false);
    }
    private void OnForceLeave(object obj)
    {
        _menuUI.SetActive(true);
        _roomContentUI.SetActive(false);
    }
    //确保脚本摧毁时，静态变量不指向空引用
    private void OnDisable()
    {
        EventCenter.RemoveListener(UIEvent.ON_CREATE_ROOM_SUCCESS, OnRoomCreateSuccess);
        EventCenter.RemoveListener(UIEvent.ON_ROOM_PLAYER_UPDATE, OnPlayerUpdate);
        EventCenter.RemoveListener(UIEvent.ON_PLAYER_STATE_GAME, StartGame);
        EventCenter.RemoveListener(UIEvent.ON_FORCE_LEAVE_ROOM, OnForceLeave);
    }
}