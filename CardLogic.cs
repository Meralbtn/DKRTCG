using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using CardGameApp;
namespace CardGameServer
{

    public enum SessionMessageID
    {

        HelloServer = 1,
        GetRoomList = 2,
        CreateRoom = 3,
        JoinRoom = 4,
        LeaveRoom = 5,
        BattleStart = 6,


        PlayCard = 7,
        Attack = 8,
        EndTurn = 9,
        Register = 10,
        Login = 11,
        RegisterResult = 1008,
        LoginResult = 1009,

        HelloClient = 1001,
        RoomList = 1002,
        CreateRoomResult = 1003,
        JoinRoomResult = 1004,
        LeaveRoomResult = 1005,
        RoomPlayerUpdate = 1006,
        ForceLeaveRoom = 1007,
        BattleStartResult = 2001,


        BattleStateSync = 2002,
        BattleActionAck = 2003,
    }
    public enum ErrorCode
    {
        Success = 0,
        UnknownError = 1,
        InvalidRequest = 2,
        RoomFull = 3,
        RoomNotFound = 4,
        AlreadyInRoom = 5,
        NotInRoom = 6,
        LoginFailed = 7
    }
    public class AuthPackage
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
    }

    public class AuthResultPackage
    {
        public ErrorCode ErrorCode { get; set; }
        public string Tips { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
    }

    public class SessionPackage
    {
        public Guid _sessionID { get; set; }
        public string _playerName { get; set; }
        public string _tips { get; set; }
        public int _roomID { get; set; }
        public string _roomName { get; set; }
        public int _maxPlayers { get; set; }
        public List<int> _playerDeck { get; set; } = null;
        public ErrorCode _errorCode { get; set; }
    }

    public class JoinRoomResponse
    {
        public ErrorCode ErrorCode { get; set; }
        public string Tips { get; set; }

        // 房间静态信息
        public string RoomName { get; set; }
        public int RoomId { get; set; }
        public int MaxPlayers { get; set; }

        // 房间动态信息：当前房间里的所有玩家列表
        public List<PlayerBaseData> ExistingPlayers { get; set; }
    }
    public class UseInfo
    {
        public long userId { get; set; }
        public string userName { get; set; }
    }
    public class NetEvent
    {
        public Session Session;
        public IPEndPoint EndPoint;
        public NetPackage Package;
        public bool IsUdp;
    }

    public class ForceLeavePackage
    {
        public string Reason { get; set; }
    }
    public class RoomPlayerUpdatePackage
    {
        public List<PlayerBaseData> Players { get; set; }
        public string UpdateType { get; set; }  // "Join" / "Leave"
        public string PlayerName { get; set; }  // 谁加入/离开了
    }


    public class CardLogic
    {
        public static CardLogic instance = new CardLogic();
        public static CardLogic Instance => instance;
        //虚拟房间
        private CardRoomManager _roomManager = new CardRoomManager();

        public Action<IPEndPoint, NetPackage> OnUdpSendData;
        private bool _running = false;
        private CardLogic()
        {
            _running = true;
            LoadPacketHandlers();
            Task.Run(Work);
        }

        //定义处理包体的办法
        public delegate void PacketHandler(Session session, byte[] data);
        public delegate void UdpPacketHandler(IPEndPoint point, byte[] data);
        //根据包头分发到不同的处理函数
        private Dictionary<SessionMessageID, PacketHandler> _packetHandlers = new Dictionary<SessionMessageID, PacketHandler>();
        private Dictionary<SessionMessageID, UdpPacketHandler> _udpPacketHandlers = new Dictionary<SessionMessageID, UdpPacketHandler>();

        //定义委托优化Session
        public Action<Guid, NetPackage> OnTcpSendData;


        //记录Server端的信息
        private ConcurrentQueue<NetEvent> _eventQueue = new ConcurrentQueue<NetEvent>();
        object locker = new();

        #region 事件处理

        //TCP链接服务器
        public void OnSessionConnected(Session session, byte[] data)
        {
            try
            {
                //直接用json获取SessionPackage
                var sessionPackage = JsonConvert.DeserializeObject<SessionPackage>(Encoding.UTF8.GetString(data));
                session.PlayerName = sessionPackage._playerName;
                Console.WriteLine($"玩家 {session.PlayerName} 已连接，SessionID: {session.SessionId}");
                //回传链接成功消息
                var responsePackage = new SessionPackage();
                responsePackage._sessionID = session.SessionId;
                responsePackage._playerName = session.PlayerName;
                responsePackage._tips = "欢迎来到卡牌游戏服务器！";
                responsePackage._errorCode = ErrorCode.Success;
                var responseData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responsePackage));
                using var netPackage = new NetPackage();
                netPackage.WriteInt(1001);
                netPackage.WriteInt(responseData.Length);
                netPackage.WriteBytes(responseData);
                session.SendTcpData(netPackage);
            }
            catch (Exception ex)
            {
                var responsePackage = new SessionPackage();
                responsePackage._sessionID = session.SessionId;
                responsePackage._playerName = session.PlayerName;
                responsePackage._tips = "欢迎来到卡牌游戏服务器！";
                responsePackage._errorCode = ErrorCode.LoginFailed;
                var responseData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responsePackage));
                using var netPackage = new NetPackage();
                netPackage.WriteInt(1001);
                netPackage.WriteInt(responseData.Length);
                netPackage.WriteBytes(responseData);
                session.SendTcpData(netPackage);
                Console.WriteLine($"处理连接事件时发生错误: {ex.Message}");
            }
        }

        public void OnCreateRoom(Session session, byte[] data)
        {
            try
            {
                var package = JsonConvert.DeserializeObject<SessionPackage>(Encoding.UTF8.GetString(data));
                if (package == null)
                {
                    throw new Exception("房间名称不能为空");
                }


                if (package._roomName == null)
                {
                    throw new Exception("房间名称不能为空");
                }
                Console.WriteLine($"房间");
                PlayerBaseData playerBase = new PlayerBaseData();
                playerBase._id = package._sessionID.ToString();
                playerBase._name = package._playerName;
                playerBase._deck = package._playerDeck;
                if (_roomManager.IsPlayerInAnyRoom(playerBase._id))
                {
                    throw new Exception("您已经在其他房间中了");
                }
                var room = _roomManager.CreateRoom(package._roomName, package._maxPlayers, playerBase);
                Console.WriteLine($"房间 {room._name} 创建成功，房主: {playerBase._name},房间ID: {room._id}");
                //回传创建房间结果
                var responsePackage = new SessionPackage();
                responsePackage._sessionID = session.SessionId;
                responsePackage._playerName = session.PlayerName;
                responsePackage._roomID = room._id;
                responsePackage._roomName = package._roomName;
                responsePackage._tips = $"房间 {room._name} 创建成功！";
                responsePackage._errorCode = ErrorCode.Success;
                var responseData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responsePackage));
                var netPackage = new NetPackage();
                netPackage.WriteInt(1003);
                netPackage.WriteInt(responseData.Length);
                netPackage.WriteBytes(responseData);
                session.SendTcpData(netPackage);
            }
            catch (Exception ex)
            {
                //回传错误创建
                var responsePackage = new SessionPackage();
                responsePackage._sessionID = session.SessionId;
                responsePackage._playerName = session.PlayerName;
                responsePackage._tips = $"房间创建失败: {ex.Message}";
                responsePackage._errorCode = ErrorCode.UnknownError;
                var responseData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(responsePackage));
                using var netPackage = new NetPackage();
                netPackage.WriteInt(1003);
                netPackage.WriteInt(responseData.Length);
                netPackage.WriteBytes(responseData);
                session.SendTcpData(netPackage);
                Console.WriteLine($"处理创建房间事件时发生错误: {ex.Message}");
            }
        }
        public void OnLeaveRoom(Session session, byte[] data)
        {
            try
            {
                var package = JsonConvert.DeserializeObject<SessionPackage>(
                    Encoding.UTF8.GetString(data));

                var room = _roomManager.GetRoomById(package._roomID);
                if (room == null) return;

                var player = room._players
                    .FirstOrDefault(p => p._id == session.SessionId.ToString());
                if (player == null) return;

                bool wasHost = player._id == room._host._id;

                // 房主退出，先通知其他人强制退出
                if (wasHost && room._currentCount > 1)
                {
                    var otherPlayers = room._players
                        .Where(p => p._id != player._id)
                        .ToList();

                    var forceData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                        new ForceLeavePackage { Reason = "HostLeft" }));

                    foreach (var p in otherPlayers)
                    {
                        using var netPkg = new NetPackage();
                        netPkg.WriteInt((int)SessionMessageID.ForceLeaveRoom);
                        netPkg.WriteInt(forceData.Length);
                        netPkg.WriteBytes(forceData);
                        SendToPlayer(p, netPkg);
                    }
                }

                // 关闭房间
                _roomManager.LeaveRoom(package._roomID, player);

                // 回传给离开者
                var ackData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
                    new SessionPackage { _errorCode = ErrorCode.Success }));
                using var ackPkg = new NetPackage();
                ackPkg.WriteInt((int)SessionMessageID.LeaveRoomResult);
                ackPkg.WriteInt(ackData.Length);
                ackPkg.WriteBytes(ackData);
                session.SendTcpData(ackPkg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"离开房间错误: {ex.Message}");
            }
        }

        public void OnJoinRoom(Session session, byte[] data)
        {
            try
            {
                try
                {
                    var package = JsonConvert.DeserializeObject<SessionPackage>(
                        Encoding.UTF8.GetString(data));

                    PlayerBaseData playerBase = new PlayerBaseData
                    {
                        _id = session.SessionId.ToString(),
                        _name = session.PlayerName,
                        _deck = package._playerDeck
                    };

                    var room = _roomManager.GetRoomById(package._roomID);
                    if (room == null)
                    { SendJoinRoomResult(session, ErrorCode.RoomNotFound, null, "房间不存在"); return; }

                    if (room._currentCount >= room._maxCapacity)
                    { SendJoinRoomResult(session, ErrorCode.RoomFull, null, "房间已满"); return; }

                    _roomManager.JoinRoom(package._roomID, playerBase);

                    // 回传给加入者
                    SendJoinRoomResult(session, ErrorCode.Success, room._players,
                        $"成功加入房间 {room._name}", room._name, room._id);

                    // 广播给房间内其他玩家（通知有人加入）
                    BroadcastRoomUpdate(room, playerBase._name, "Join");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"加入房间错误: {ex.Message}");
                    SendJoinRoomResult(session, ErrorCode.UnknownError, null, ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理加入房间事件时发生错误: {ex.Message}");
                SendJoinRoomResult(session, ErrorCode.UnknownError, null, $"加入失败: {ex.Message}");
            }
        }


        private void BroadcastRoomUpdate(CardRoom room, string playerName, string updateType)
        {
            var updatePkg = new RoomPlayerUpdatePackage
            {
                Players = room._players,
                UpdateType = updateType,
                PlayerName = playerName
            };

            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(updatePkg));

            foreach (var p in room._players)
            {
                using var netPkg = new NetPackage();
                netPkg.WriteInt((int)SessionMessageID.RoomPlayerUpdate);
                netPkg.WriteInt(data.Length);
                netPkg.WriteBytes(data);
                SendToPlayer(p, netPkg);
            }
        }
        private void SendJoinRoomResult(Session session, ErrorCode errorCode, List<PlayerBaseData> existingPlayers, string tips, string roomName = "", int roomId = 0)
        {
            var response = new JoinRoomResponse
            {
                ErrorCode = errorCode,
                Tips = tips,
                RoomName = roomName,
                RoomId = roomId,
                MaxPlayers = 2,
                ExistingPlayers = existingPlayers ?? new List<PlayerBaseData>() // 这里可以根据实际情况填充
            };

            var responseData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            using var netPackage = new NetPackage();
            netPackage.WriteInt((int)SessionMessageID.JoinRoomResult); // ID: 1004
            netPackage.WriteInt(responseData.Length);
            netPackage.WriteBytes(responseData);
            session.SendTcpData(netPackage);
        }

        private void OnUseCard(Session session, byte[] data)
        {
            var req = JsonConvert.DeserializeObject<UseCardPackage>(Encoding.UTF8.GetString(data));
            var battle = GetBattleBySession(session);
            if (battle == null) { Console.WriteLine("找不到对应战斗"); return; }
            battle.HandleUseCard(GetPlayerBySession(session, battle), req);
        }

        private void OnAttack(Session session, byte[] data)
        {
            var req = JsonConvert.DeserializeObject<AttackPackage>(Encoding.UTF8.GetString(data));
            var battle = GetBattleBySession(session);
            if (battle == null) { Console.WriteLine("找不到对应战斗"); return; }
            battle.HandleAttack(GetPlayerBySession(session, battle), req);
        }

        private void OnEndTurn(Session session, byte[] data)
        {
            var req = JsonConvert.DeserializeObject<EndTurnPackage>(Encoding.UTF8.GetString(data));
            var battle = GetBattleBySession(session);
            if (battle == null) { Console.WriteLine("找不到对应战斗"); return; }
            battle.HandleEndTurn(GetPlayerBySession(session, battle), req);
        }

        private BattleController GetBattleBySession(Session session)
        {
            var room = _roomManager.GetAllRooms()
                .FirstOrDefault(r => r._players
                    .Any(p => p._id == session.SessionId.ToString()));
            return room?._battleController;
        }

        private PlayerBaseData GetPlayerBySession(Session session, BattleController battle)
        {
            var id = session.SessionId.ToString();
            return battle.Player1._id == id ? battle.Player1 : battle.Player2;
        }
        //UDP查询房间
        public void OnUdpQueryRooms(IPEndPoint point, byte[] data)
        {
            try
            {
                Console.WriteLine($"处理UDP查询房间事件: {point}");
                var rooms = _roomManager.GetAllRooms();
                List<CardRoomInfo> roomInfos = new List<CardRoomInfo>();
                foreach (var room in rooms)
                {
                    CardRoomInfo info = new CardRoomInfo
                    {
                        _id = room._id,
                        _name = room._name,
                        _currentCount = room._currentCount,
                        _maxCapacity = room._maxCapacity,
                    };
                    roomInfos.Add(info);
                }
                var responseData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(roomInfos));
                using var netPackage = new NetPackage();
                netPackage.WriteInt(1002);
                netPackage.WriteInt(responseData.Length);
                netPackage.WriteBytes(responseData);
                OnUdpSendData?.Invoke(point, netPackage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理UDP查询房间事件时发生错误: {ex.Message}");
            }
        }

        public void OnBattleStart(Session session, byte[] data)
        {
            try
            {
                var package = JsonConvert.DeserializeObject<SessionPackage>(Encoding.UTF8.GetString(data));
                if (package == null)
                {
                    throw new Exception("数据包无效");
                }
                var room = _roomManager.GetRoomById(package._roomID);
                if (room == null)
                {
                    throw new Exception("房间不存在");
                }
                if (room._players[0]._id != package._sessionID.ToString())
                {
                    throw new Exception("只有房主可以开始战斗");
                }
                room.StartBattle();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理战斗开始事件时发生错误: {ex.Message}");
            }
        }




        public void SendToPlayer(PlayerBaseData player, NetPackage pkg)
        {
            Guid.TryParse(player._id, out var playerId);
            OnTcpSendData?.Invoke(playerId, pkg);
        }

        public void OnRegister(Session session, byte[] data)
        {
            var pkg = JsonConvert.DeserializeObject<AuthPackage>(
                Encoding.UTF8.GetString(data));

            bool success = DatabaseManager.Instance.Register(
                pkg.UserName, pkg.Email, pkg.PasswordHash);

            var result = new AuthResultPackage
            {
                ErrorCode = success ? ErrorCode.Success : ErrorCode.UnknownError,
                Tips = success ? "注册成功" : "该邮箱已被注册"
            };

            SendAuthResult(session, SessionMessageID.RegisterResult, result);
        }

        public void OnLogin(Session session, byte[] data)
        {
            var pkg = JsonConvert.DeserializeObject<AuthPackage>(
                Encoding.UTF8.GetString(data));

            UseInfo userId = DatabaseManager.Instance.Login(pkg.Email, pkg.PasswordHash);

            var result = new AuthResultPackage
            {
                ErrorCode = userId != null ? ErrorCode.Success : ErrorCode.LoginFailed,
                UserId = userId?.userId ?? 0,
                UserName = userId?.userName ?? string.Empty,
                Tips = userId != null ? "登录成功" : "邮箱或密码错误"
            };

            SendAuthResult(session, SessionMessageID.LoginResult, result);
        }

        private void SendAuthResult(Session session, SessionMessageID msgId,AuthResultPackage result)
        {
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
            using var netPkg = new NetPackage();
            netPkg.WriteInt((int)msgId);
            netPkg.WriteInt(data.Length);
            netPkg.WriteBytes(data);
            session.SendTcpData(netPkg);
        }


        #endregion

        #region 基础逻辑

        //加载处理字典
        public void LoadPacketHandlers()
        {
            _packetHandlers[SessionMessageID.HelloServer] = OnSessionConnected;
            _packetHandlers[SessionMessageID.CreateRoom] = OnCreateRoom;
            _packetHandlers[SessionMessageID.JoinRoom] = OnJoinRoom;
            _packetHandlers[SessionMessageID.LeaveRoom] = OnLeaveRoom;
            _packetHandlers[SessionMessageID.BattleStart] = OnBattleStart;
            _udpPacketHandlers[SessionMessageID.GetRoomList] = OnUdpQueryRooms;
            _packetHandlers[SessionMessageID.PlayCard] = OnUseCard;
            _packetHandlers[SessionMessageID.Attack] = OnAttack;
            _packetHandlers[SessionMessageID.EndTurn] = OnEndTurn;
            _packetHandlers[SessionMessageID.Register] = OnRegister;
            _packetHandlers[SessionMessageID.Login] = OnLogin;
        }

        public void Work()
        {
            //session同时被logic和manage访问，所以需要锁
            while (_running)
            {
                NetEvent item;

                lock (locker)
                {
                    while (_eventQueue.Count == 0 && _running)
                    {
                        Monitor.Wait(locker);
                    }

                    if (!_running)
                        break;

                    _eventQueue.TryDequeue(out item);
                }
                try
                {
                    if (item == null)
                        continue;
                    if (item.IsUdp)
                    {
                        Console.WriteLine($"从事件队列中取出UDP数据: {item.EndPoint}");
                        int id = item.Package.ReadInt();
                        int length = item.Package.ReadInt();
                        byte[] data = item.Package.ReadBytes(length);

                        if (_udpPacketHandlers.TryGetValue((SessionMessageID)id, out var handler))
                        {
                            handler(item.EndPoint, data);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"从事件队列中取出数据: {item.Session.SessionId}");
                        int id = item.Package.ReadInt();
                        int length = item.Package.ReadInt();
                        byte[] data = item.Package.ReadBytes(length);

                        if (_packetHandlers.TryGetValue((SessionMessageID)id, out var handler))
                        {
                            handler(item.Session, data);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            Console.WriteLine("Worker线程退出");
        }



        public void AddTcpRequest(Session session, NetPackage netPackage)
        {
            try
            {
                Console.WriteLine($"添加TCP请求到事件队列: SessionId={session.SessionId}, PackageLength={netPackage.ToArray().Length}");
                lock (locker)
                {
                    _eventQueue.Enqueue(new NetEvent
                    {
                        Session = session,
                        Package = netPackage,
                        EndPoint = null,
                        IsUdp = false
                    });
                    Monitor.Pulse(locker);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理TCP请求时发生错误: {ex.Message}");
            }
        }

        public void AddUdpRequest(IPEndPoint endPoint, NetPackage netPackage)
        {
            try
            {
                Console.WriteLine($"添加UDP请求: EndPoint={endPoint}, PackageLength={netPackage.ToArray().Length}");
                lock (locker)
                {
                    _eventQueue.Enqueue(new NetEvent
                    {
                        Session = null,
                        Package = netPackage,
                        EndPoint = endPoint,
                        IsUdp = true
                    });
                    Monitor.Pulse(locker);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理UDP请求时发生错误: {ex.Message}");
            }
        }

        public void Stop()
        {
            lock (locker)
            {
                _running = false;
                Monitor.PulseAll(locker);
            }
        }


        #endregion

    }
}