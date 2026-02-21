using System;
namespace ServerConnectData
{
    enum ConnectionType
    {
        ServerError = 1001,
        ServerConnected = 1002,
        LogicUser = 1003,
        LogicUserError = 1004,
        LogicUserConnected = 1005,
        LogicUserDisconnected = 1006,
    }
}