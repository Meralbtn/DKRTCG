
using System;
using System.Collections.Generic;


namespace CardGameApp
{

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
        private PlayerBaseData _host { get; set; }
        public List<PlayerBaseData> _players { get; set; } = new List<PlayerBaseData>();
        private RoomStatus _status { get; set; } = RoomStatus.Waiting;
        public int _maxCapacity { get; set; }
        public int _currentCount => _players.Count;
       
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
        public CardRoom(string name, PlayerBaseData host,int id)
        {
            _name = name;
            _maxCapacity = 2;
            _status = RoomStatus.Waiting;
            _host = host;
            _id = id;
            _players.Add(host);
        }
        public CardRoom(string name, int id)
        {
            _name = name;
            _maxCapacity = 2;
            _status = RoomStatus.Waiting;
            _id = id;
        }
        public CardRoom()
        {
            
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


        public List<PlayerBaseData> GetPlayerBaseDatas()
        {
            return _players;
        }
    }


    
}