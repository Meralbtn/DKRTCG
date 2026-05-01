using System.Collections;
using System.Collections.Generic;
using CardGame;
using TMPro;
using UnityEngine;

public class CardRoomUI : MonoBehaviour
{
    public TextMeshProUGUI _hostName;
    public TextMeshProUGUI _guestName;
    
    public void CreateRoomUI()
    {
        var playersData = PlayerManager.Instance().GetCardRoomInfo().GetPlayerBaseDatas();
        _hostName.text = playersData[0]._name;
        if (playersData.Count == 2)
            _guestName.text = playersData[1]._name;
        else
            _guestName.text = "等待玩家加入...";
        
    }
}
