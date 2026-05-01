using System.Collections;
using System.Collections.Generic;
using CardGame;
using UnityEngine;

public class RoomListView : MonoBehaviour
{
    public Transform _content;
    public RoomItem _roomItemPrefab;
    private List<RoomItem> _activeRoomItems = new List<RoomItem>();
    private GameObjectPool<RoomItem> _roomItemPool;

    private void Start()
    {
        _roomItemPool = new GameObjectPool<RoomItem>(_roomItemPrefab, 10, _content);
        RefreshRoomList();
    }

    public void RefreshRoomList()
    {
        // 从PlayerManager获取房间列表
        var rooms = PlayerManager.Instance()._roomList;

        // 回收当前显示的房间项
        _roomItemPool.ReturnAll(_activeRoomItems);

        // 创建新的房间项
        foreach (var room in rooms)
        {
            RoomItem item = _roomItemPool.Get();
            item.SetRoomInfo(room);
            _activeRoomItems.Add(item);
        }
    }
}
