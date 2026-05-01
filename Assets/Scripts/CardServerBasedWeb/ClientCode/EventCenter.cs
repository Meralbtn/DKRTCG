using System;
using System.Collections.Generic;
// 这只是一个存放名字的仓库，不是逻辑类
public static class UIEvent
{
    public const string ON_CREATE_ROOM_SUCCESS = "ON_CREATE_ROOM_SUCCESS";
    public const string ON_CREATE_ROOM_FAILED = "ON_CREATE_ROOM_FAILED";
    public const string ON_PLAYER_JOINED = "ON_PLAYER_JOINED";
    public const string ON_PLAYER_JOINED_USER = "ON_PLAYER_JOINED_USER";
    public const string ON_PLAYER_STATE_GAME = "ON_PLAYER_STATE_GAME";
    public const string ON_ROOM_PLAYER_UPDATE = "ON_ROOM_PLAYER_UPDATE";
    public const string ON_FORCE_LEAVE_ROOM = "ON_FORCE_LEAVE_ROOM";
    public const string ON_LOGIN_SUCCESS = "ON_LOGIN_SUCCESS";
    public const string ON_LOGIN_FAILED = "ON_LOGIN_FAILED";
    public const string ON_REGISTER_SUCCESS = "ON_REGISTER_SUCCESS";
    public const string ON_REGISTER_FAILED = "ON_REGISTER_FAILED";
}
public static class EventCenter
{
    // 使用字典存储事件
    private static Dictionary<string, Action<object>> eventTable = new Dictionary<string, Action<object>>();

    // 订阅事件
    public static void AddListener(string eventName, Action<object> callback)
    {
        if (!eventTable.ContainsKey(eventName))
            eventTable.Add(eventName, null);
        eventTable[eventName] += callback;
    }

    // 取消订阅
    public static void RemoveListener(string eventName, Action<object> callback)
    {
        if (eventTable.ContainsKey(eventName))
            eventTable[eventName] -= callback;
    }

    // 触发事件
    public static void Broadcast(string eventName, object data = null)
    {
        if (eventTable.TryGetValue(eventName, out Action<object> callback))
        {
            callback?.Invoke(data);
        }
    }
}