
using System.Reflection.Metadata;
using CardGameServer;

namespace CardGameApp
{

    public class BattleStartPackage
    {
        // 1. 身份识别
        public string MyRole;          // "First" 或 "Last" (决定UI是在上方还是下方)
        public Guid BattleId;          // 这场战斗的唯一ID (用于重连)

        // 2. 对手信息
        public string EnemyName;

        // 3. 初始数值状态
        public int MyMaxHp;
        public int EnemyMaxHp;
        public int MyInitialCost;      // 初始费用 (通常先手1, 后手0或带补偿)

        // 4. 卡组/手牌信息 (核心)
        public List<int> MyHandIds;    // 初始抽到的5张牌的配置ID
        public int MyDeckCount;        // 我方剩余牌库数量
        public int EnemyDeckCount;     // 对方剩余牌库数量
        public int EnemyHandCount;     // 对方手牌数量 (客户端只看背面，所以只给数量)

        // 5. 随机种子 (可选)
        // 如果客户端有一些不影响逻辑的随机动画（如洗牌轨迹），可以统一种子
    }
    public enum RoomStatus
    {
        Waiting,
        InGame,
        Closed
    }

    public class PlayerBaseData
    {
        public string _name { get; set; }
        public string _id { get; set; }
        public List<int> _deck { get; set; } = new List<int>();
    }
    [Serializable]
    public class RoomInfo
    {
        // 必须是 public 且带有 { get; set; }，否则 System.Text.Json 默认不处理
        public string _name { get; set; }
        public int _id { get; set; }

        // 房主信息（建议也用对应的 DTO）
        public PlayerBaseData _host { get; set; }

        // 状态和人数
        public RoomStatus _status { get; set; }
        public int _maxCapacity { get; set; }
        public int _currentCount { get; set; }

        // 如果客户端需要显示玩家列表，才加上这个；如果只是大厅列表，建议不加以节省流量
        public List<PlayerBaseData> _playerList { get; set; } = new List<PlayerBaseData>();
    }

    public class CardRoomInfo
    {
        public int _id { get; set; }
        public string _name { get; set; }
        public string _hostName { get; set; }
        public int _currentCount { get; set; }
        public int _maxCapacity { get; set; }
        public RoomStatus _status { get; set; }
    }
    public class CardRoom
    {
        public string _name { get; set; }
        public int _id { get; set; }
        public PlayerBaseData _host { get; set; }
        public List<PlayerBaseData> _players { get; set; } = new List<PlayerBaseData>();
        private RoomStatus _status { get; set; } = RoomStatus.Waiting;
        public int _maxCapacity { get; set; }
        public int _currentCount => _players.Count;
        public BattleController _battleController { get; set; } = null;
        //暂时不设密码
        public CardRoom(string name, int maxCapacity, PlayerBaseData host)
        {
            _name = name;
            _maxCapacity = maxCapacity;
            _status = RoomStatus.Waiting;
            _host = host;
            _id = Guid.NewGuid().GetHashCode();
            _players.Add(host);
        }

        public bool StartBattle()
        {
            if (_players.Count < 2)
            {
                Console.WriteLine("玩家不足，无法开始战斗");
                return false;
            }
            _status = RoomStatus.InGame;
            _battleController = new BattleController(_players[0], _players[1]);
            _battleController.StartBattle();
            return true;
        }


        public bool JoinRoom(PlayerBaseData player)
        {
            if (IsFull() && _status != RoomStatus.Waiting)
            {
                Console.WriteLine("房间已满，无法加入");
                return false;
            }
            _players.Add(player);
            Console.WriteLine($"{player._name} 加入了房间 {_name}");
            return true;
        }

        public bool LeaveRoom(PlayerBaseData player)
        {
            var id = player._id;
            if (player._id == _host._id)
            {
                Console.WriteLine("房主离开了房间，房间将被关闭");
                CloseRoom(_host);
                return true;
            }
            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i]._id == id)
                {
                    _players.RemoveAt(i);
                    Console.WriteLine($"{player._name} 离开了房间 {_name}");
                    return true;
                }
            }
            Console.WriteLine($"{player._name} 不在房间 {_name} 中");
            return false;
        }

        public bool IsFull()
        {
            return _currentCount >= _maxCapacity;
        }

        public void CloseRoom(PlayerBaseData host)
        {
            if (host._id != _host._id)
            {
                Console.WriteLine("只有房主可以关闭房间");
                return;
            }
            _players.Clear();
            _status = RoomStatus.Closed;
            Console.WriteLine($"房间 {_name} 已关闭");
        }

        public void CloseRoom(int roomId)
        {
            _players.Clear();
            _status = RoomStatus.Closed;
            Console.WriteLine($"房间 {_name} 已关闭");
        }
        public RoomInfo ToInfo()
        {
            RoomInfo info = new RoomInfo();
            info._currentCount = _currentCount;
            info._host = _host;
            info._id = _id;
            info._maxCapacity = _maxCapacity;
            info._name = _name;
            info._playerList = _players;
            info._status = _status;
            return info;
        }
    }


    public class CardRoomManager
    {
        private Dictionary<int, CardRoom> _rooms = new();
        public CardRoom CreateRoom(string name, int maxPlayers, PlayerBaseData host)
        {
            var room = new CardRoom(name, maxPlayers, host);
            _rooms[room._id] = room;

            return room;
        }
        public bool JoinRoom(int roomId, PlayerBaseData player)
        {
            if (!_rooms.TryGetValue(roomId, out var room))
                return false;

            return room.JoinRoom(player);
        }
        public void LeaveRoom(int roomId, PlayerBaseData player)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.LeaveRoom(player);
                if (room._currentCount == 0)
                    _rooms.Remove(roomId);
            }
        }

        public CardRoom GetRoomByPlayerId(string playerId)
        {
            foreach (var room in _rooms.Values)
            {
                if (room._players.Any(p => p._id == playerId))
                    return room;
            }
            return null;
        }

        public CardRoom CloseRoom(int roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.CloseRoom(roomId);
                _rooms.Remove(roomId);
                return room;
            }
            return null;
        }
        public CardRoom GetRoomById(int roomId)
        {
            foreach (var room in _rooms.Values)
            {
                if (room._id == roomId)
                    return room;
            }
            return null;
        }

        public bool IsPlayerInAnyRoom(string playerId)
        {
            // 遍历所有房间，检查玩家列表里是否有这个 ID
            // 如果你的房间很多，建议在 Manager 里维护一个 Dictionary<playerId, roomName> 来快速索引
            return _rooms.Values.Any(r => r._players.Any(p => p._id == playerId));
        }

        //向服务器返回当前所有房间的列表
        public List<CardRoom> GetAllRooms()
        {
            return new List<CardRoom>(_rooms.Values);
        }
    }
}