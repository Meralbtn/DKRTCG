namespace CardGameServer
{
    // 定义简单的协议头，防止收到杂乱数据
    public enum PacketType : byte
    {
        GetRoomListRequest = 1,
        RoomListResponse = 2
    }
}