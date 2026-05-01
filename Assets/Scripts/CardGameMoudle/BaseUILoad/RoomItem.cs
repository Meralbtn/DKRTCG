using System;
using System.Collections;
using System.Collections.Generic;
using CardGameApp;
using CardGameClient;
using UnityEngine;

public class RoomItem : MonoBehaviour
{
    public TMPro.TextMeshProUGUI _roomNameText;
    public TMPro.TextMeshProUGUI _roomPlayerCountText;
    public TMPro.TextMeshProUGUI _gameModeText;
    private CardRoomInfo _roomInfo;

    public async void OnJoinClick()
    {
        CardClient.Instance.OnJoinRoomRequest(_roomInfo);
    }
    public void SetRoomInfo(CardRoomInfo room)
    {
        _roomInfo = room;
        _roomNameText.text = room._name;
        _roomPlayerCountText.text = $"{room._currentCount}/2";
        _gameModeText.text ="默认模式";
    }

    public CardRoomInfo GetRoomInfo()
    {
        return _roomInfo;
    }
}
