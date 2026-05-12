using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;
using Newtonsoft.Json;
using CardGameServer;
using CardGame;
using SingletonMonoModule;
using CardGameApp;
using System.Collections;
using UnityEngine.SceneManagement;

namespace CardGameClient
{
    #region 网络包
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
        Surrender = 4004
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

    public class UseCardPackage
    {
        public Guid SessionId { get; set; }
        public string ActionId { get; set; }
        public int CardInstanceId { get; set; }
    }

    public class AttackPackage
    {
        public Guid SessionId { get; set; }
        public string ActionId { get; set; }
        public int AttackerInstanceId { get; set; } // 我方随从实例 ID
        public int TargetInstanceId { get; set; } // 目标随从实例 ID，null = 直攻英雄
    }

    public class EndTurnPackage
    {
        public Guid SessionId { get; set; }
        public string ActionId { get; set; }
    }

    public class ActionAck
    {
        public string ActionId { get; set; } // 对应上行包的 ActionId
        public ErrorCode ErrorCode { get; set; }
        public string Reason { get; set; } // 失败原因（调试用）
    }
    public class ForceLeavePackage
    {
        public string Reason { get; set; }
    }

    public class BattleState
    {
        public int MyHP, EnemyHP;
        public int MyCost, EnemyCost;
        public int MyMaxCost, EnemyMaxCost;
        public bool IsMyTurn;
        public string GameResult;

        public List<MiniCardState> MyField;
        public List<MiniCardState> EnemyField;
        public List<HandCardState> MyHand;
        public int EnemyHandCount;
    }

    public class MiniCardState
    {
        public int InstanceId;
        public int CardId;
        public int HP, Attack;
        public int AttackUsed;
    }

    public class HandCardState
    {
        public int InstanceId;
        public int CardId;
    }
    
    public class SurrenderPackage
    {
        public Guid SessionId { get; set; }
        public string ActionId { get; set; }
    }

    public class BattleStartPackage
    {
        public string YourRole { get; set; }
        public List<HandCardState> MyHand { get; set; }
        public int EnemyHand { get; set; }
        public string EnemyName { get; set; }
        public int MyMaxHealth { get; set; }
        public int MyInitialCost { get; set; }
        public int EnemyMaxHealth { get; set; }
        public int EnemyInitialCost { get; set; }
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
    public class RoomPlayerUpdatePackage
    {
        public List<PlayerBaseData> Players { get; set; }
        public string UpdateType { get; set; }  // "Join" / "Leave"
        public string PlayerName { get; set; }  // 谁加入/离开了
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
    #endregion

    public class CardClient : SingletonMono<CardClient>
    {
        private TcpClient _tcpClient;
        private UdpClient _udpClient;
        private NetworkStream _stream;
        private byte[] _buffer = new byte[4096];
        private string _ip = "111.229.131.240";
        private Guid _uuid;
        private int _port = 8888;
        private List<byte> _receivedData = new List<byte>();

        private bool _isQuerying = false;
        public BattleStartPackage _pendingBattleInitInfo;
        public Task _connectTask;


        #region 创建房间事件
        //弃用
        public Action _onRoomCreateOrJoin;

        public Action<BattleState> OnBattleStateReceived;
        public Action<ActionAck> OnActionAckReceived;
        public Action<AuthResultPackage> OnLoginSuccess;
        public Action<AuthResultPackage> OnRegisterSuccess;
        #endregion
        protected override void Awake()
        {
            base.Awake();
            _tcpClient = new TcpClient();
            //不占用端口
            _udpClient = new UdpClient(0);
            _udpClient.Connect(_ip, _port);
            _connectTask = Connect();
            Debug.Log("CardClient 初始化");
        }
        public async Task Connect()
        {
            await _tcpClient.ConnectAsync(_ip, _port);

            _stream = _tcpClient.GetStream();

            Debug.Log("连接服务器成功");

            await SendHello();
            _ = ReceiveLoop();
        }

        public async Task SendHello()
        {
            try
            {
                string pName = PlayerManager.Instance().PlayerName;
                Debug.Log("准备发送 HelloServer 包...");

                // 1. 确保数据不为空

                var pkg = new SessionPackage { _playerName = pName };

                string jsonStr = JsonConvert.SerializeObject(pkg);
                byte[] json = Encoding.UTF8.GetBytes(jsonStr);

                using var net = new NetPackage();
                net.WriteInt((int)SessionMessageID.HelloServer);
                net.WriteInt(json.Length);
                net.WriteBytes(json);


                byte[] data = net.ToArray();
                int actualLength = data.Length;

                // 3. 发送并强制刷新
                await _stream.WriteAsync(data, 0, actualLength);
                await _stream.FlushAsync();

                Debug.Log($"成功发送 HelloServer 包, 真实长度: {actualLength}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送数据时发生异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    int bytesRead = await _stream.ReadAsync(_buffer, 0, _buffer.Length);

                    if (bytesRead == 0)
                    {
                        Debug.Log("服务器断开");
                        break;
                    }

                    _receivedData.AddRange(new ArraySegment<byte>(_buffer, 0, bytesRead));

                    ProcessPackets();
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"接收错误: {ex.Message}");
            }
        }

        private void ProcessPackets()
        {
            while (true)
            {
                if (_receivedData.Count < 8) //至少需要8字节来读取id和length
                {
                    return;
                }

                Debug.Log($"当前Buffer长度: {_receivedData.Count}");
                byte[] bufferArray = _receivedData.ToArray();
                int id = BitConverter.ToInt32(bufferArray, 0);
                int length = BitConverter.ToInt32(bufferArray, 4);
                if (_receivedData.Count < 8 + length) //数据不完整，等待下一次接收
                {
                    return;
                }

                byte[] jsonData = bufferArray.Skip(8).Take(length).ToArray();
                //在数据中移除已经处理的数据
                _receivedData.RemoveRange(0, 8 + length);
                HandlePacket(id, jsonData);
            }
        }

        //处理TCP数据
        private void HandlePacket(int msgId, byte[] data)
        {
            switch ((SessionMessageID)msgId)
            {
                case SessionMessageID.HelloClient:
                    OnHelloClient(data);
                    break;
                case SessionMessageID.CreateRoomResult:
                    OnTcpCreateRoomResult(data);
                    break;
                case SessionMessageID.JoinRoomResult:
                    OnJoinRoomResult(data);
                    break;
                case SessionMessageID.BattleStartResult:
                    OnStartGameResult(data);
                    break;
                case SessionMessageID.BattleStateSync:
                    OnBattleStateSync(data);
                    break;
                case SessionMessageID.BattleActionAck:
                    OnBattleActionAck(data);
                    break;
                case SessionMessageID.RoomPlayerUpdate:
                    OnRoomPlayerUpdate(data);
                    break;
                case SessionMessageID.ForceLeaveRoom:
                    OnForceLeaveRoom(data);
                    break;
                case SessionMessageID.LoginResult:
                    OnLoginResult(data);
                    break;
                case SessionMessageID.RegisterResult:
                    OnRegisterResult(data);
                    break;
            }
        }

        private string GetMD5(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
        // 本次登录的 email 和 hash，供登录成功后持久化使用
        private string _pendingEmail;
        private string _pendingHash;

        //登录
        public async Task SendLogin(string email, string password)
        {
            _pendingEmail = email;
            _pendingHash  = GetMD5(password);
            var pkg = new AuthPackage { Email = email, PasswordHash = _pendingHash };
            await SendTcpMessage(SessionMessageID.Login, pkg);
        }

        // 发布版持久登录：直接使用已保存的哈希，不重复哈希
        public async Task SendLoginWithHash(string email, string hash)
        {
            _pendingEmail = email;
            _pendingHash  = hash;
            var pkg = new AuthPackage { Email = email, PasswordHash = hash };
            await SendTcpMessage(SessionMessageID.Login, pkg);
        }
        //注册
        public async Task SendRegister(string userName, string email, string password)
        {
            var pkg = new AuthPackage
            {
                UserName = userName,
                Email = email,
                PasswordHash = GetMD5(password)
            };
            await SendTcpMessage(SessionMessageID.Register, pkg);
        }
        private void OnLoginResult(byte[] data)
        {
            var result = JsonConvert.DeserializeObject<AuthResultPackage>(
                Encoding.UTF8.GetString(data));

            MainDispatcher.Instance.ExecuteOnMainThread(() =>
            {
                if (result.ErrorCode == ErrorCode.Success)
                {
                    // 保存用户信息
                    PlayerManager.Instance().SetPlayerData(result.UserId.ToString());
                    PlayerManager.Instance().PlayerName = result.UserName;
                    Debug.Log($"登录成功，UserId: {result.UserId}");
                    Debug.Log($"当前玩家名称: {PlayerManager.Instance().PlayerName}");
                    PlayerManager.Instance().SetPlayerName(result.UserName);
                    PlayerManager.Instance().SetLoginState();
                    // 持久化登录凭证（发布版自动登录使用）
                    if (!string.IsNullOrEmpty(_pendingEmail) && !string.IsNullOrEmpty(_pendingHash))
                    {
                        PlayerPrefs.SetString("AutoLogin_Email", _pendingEmail);
                        PlayerPrefs.SetString("AutoLogin_Hash",  _pendingHash);
                        PlayerPrefs.Save();
                    }
                    OnLoginSuccess?.Invoke(result);
                    EventCenter.Broadcast(UIEvent.ON_LOGIN_SUCCESS, result);
                }
                else
                {
                    Debug.LogWarning($"登录失败: {result.Tips}");
                    EventCenter.Broadcast(UIEvent.ON_LOGIN_FAILED, result);
                }
            });
        }

        private void OnRegisterResult(byte[] data)
        {
            var result = JsonConvert.DeserializeObject<AuthResultPackage>(
                Encoding.UTF8.GetString(data));

            MainDispatcher.Instance.ExecuteOnMainThread(() =>
            {
                if (result.ErrorCode == ErrorCode.Success)
                {
                    Debug.Log("注册成功");
                    EventCenter.Broadcast(UIEvent.ON_REGISTER_SUCCESS, result);
                }
                else
                {
                    Debug.LogWarning($"注册失败: {result.Tips}");
                    EventCenter.Broadcast(UIEvent.ON_REGISTER_FAILED, result);
                }
            });
        }


        private void OnHelloClient(byte[] data)
        {
            string jsonStr = Encoding.UTF8.GetString(data);
            var pkg = JsonConvert.DeserializeObject<SessionPackage>(jsonStr);
            if (ErrorCode.Success != pkg._errorCode)
            {
                Debug.Log($"服务器返回错误: {pkg._errorCode}");
                return;
            }

            _uuid = pkg._sessionID;
            PlayerManager.Instance().SetPlayerData(_uuid.ToString());
            Debug.Log("服务器返回：");
            Debug.Log($"SessionID: {pkg._sessionID}");
            Debug.Log($"Name: {pkg._playerName}");
            Debug.Log($"Tips: {pkg._tips}");
        }

        //投降逻辑
        public async Task SendSurrender()
        {
            var pkg = new SurrenderPackage
            {
                SessionId = _uuid,
                ActionId = Guid.NewGuid().ToString()
            };
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pkg));
            using var netPkg = new NetPackage();
            netPkg.WriteInt((int)SessionMessageID.Surrender);
            netPkg.WriteInt(data.Length);
            netPkg.WriteBytes(data);
            await SendTcpMessage(SessionMessageID.Surrender, pkg);
        }

        //利用UDP发送获取房间列表请求
        public async Task GetRoomListViaUDP()
        {
            // 1. 防止重复点击
            if (_isQuerying) return;
            _isQuerying = true;

            // 2. 定义统一的超时控制（3秒）
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            try
            {
                // --- 组包逻辑 ---
                using NetPackage package = new NetPackage();
                var sessionMsg = new SessionPackage
                { _sessionID = _uuid, _playerName = PlayerManager.Instance().PlayerName };
                byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sessionMsg));
                package.WriteInt((int)SessionMessageID.GetRoomList);
                package.WriteInt(json.Length);
                package.WriteBytes(json);

                // --- 发送请求 ---
                await _udpClient.SendAsync(package.ToArray(), package.Length);
                Debug.Log("已发送请求，等待响应...");

                // --- 核心修复：将 Token 传给 ReceiveAsync ---
                // 注意：WaitAsync 是 .NET 6+ 的便捷写法，Unity 环境下建议用 Task.Run 配合 Token
                var receiveTask = _udpClient.ReceiveAsync();
                var completedTask = await Task.WhenAny(receiveTask, Task.Delay(3000, cts.Token));

                if (completedTask == receiveTask)
                {
                    var result = await receiveTask; // 真正拿到数据
                    ProcessRoomListData(result.Buffer); // 抽离出的解析逻辑
                    Debug.Log("UDP响应已成功处理");
                }
                else
                {
                    Debug.LogWarning("UDP 请求超时（服务器未响应）");
                }
            }
            catch (OperationCanceledException)
            {
                /* 正常超时，无需报错 */
            }
            catch (Exception ex)
            {
                Debug.LogError($"UDP 异常: {ex.Message}");
            }
            finally
            {
                _isQuerying = false;
            }
        }

        // 将解析逻辑独立出来，代码更整洁
        private void ProcessRoomListData(byte[] data)
        {
            if (data.Length < 8) return;

            int msgId = BitConverter.ToInt32(data, 0);
            int length = BitConverter.ToInt32(data, 4);

            if ((SessionMessageID)msgId == SessionMessageID.RoomList)
            {
                byte[] jsonData = data.Skip(8).Take(length).ToArray();
                string jsonStr = Encoding.UTF8.GetString(jsonData);
                var rooms = JsonConvert.DeserializeObject<List<CardRoomInfo>>(jsonStr);
                PlayerManager.Instance()._roomList.Clear();
                PlayerManager.Instance()._roomList = rooms;
                Debug.Log($"成功获取房间列表，数量: {PlayerManager.Instance()._roomList.Count}");
            }
        }


        #region 对战房间逻辑TCP

        public async Task TcpCreateRoomRequest(string roomName, int maxPlayers)
        {
            var sessionMsg = new SessionPackage { _sessionID = _uuid };
            sessionMsg._playerName = PlayerManager.Instance().PlayerName;
            sessionMsg._roomName = roomName;
            sessionMsg._playerDeck = PlayerManager.Instance().GetBattleDeck().DeckToNetList();
            Debug.Log(roomName);
            sessionMsg._maxPlayers = maxPlayers;
            string jsonStr = JsonConvert.SerializeObject(sessionMsg);
            byte[] json = Encoding.UTF8.GetBytes(jsonStr);
            using var package = new NetPackage();
            package.WriteInt((int)SessionMessageID.CreateRoom);
            package.WriteInt(json.Length);
            package.WriteBytes(json);
            try
            {
                await _stream.WriteAsync(package.ToArray(), 0, package.Length);
                await _stream.FlushAsync();
                Debug.Log("已发送创建房间请求");
            }
            catch (Exception ex)
            {
                Debug.LogError($"发送创建房间请求失败: {ex.Message}");
            }
        }

        public async void OnJoinRoomRequest(CardRoomInfo _roomInfo)
        {
            try
            {
                //向服务器发送请求
                var sessionMsg = new SessionPackage { _sessionID = _uuid };
                sessionMsg._playerName = PlayerManager.Instance().PlayerName;
                sessionMsg._playerDeck = PlayerManager.Instance().GetBattleDeck().DeckToNetList();
                sessionMsg._roomName = _roomInfo._name;
                sessionMsg._roomID = _roomInfo._id;
                string jsonStr = JsonConvert.SerializeObject(sessionMsg);
                byte[] json = Encoding.UTF8.GetBytes(jsonStr);
                using var package = new NetPackage();
                package.WriteInt((int)SessionMessageID.JoinRoom);
                package.WriteInt(json.Length);
                package.WriteBytes(json);
                await _stream.WriteAsync(package.ToArray(), 0, package.Length);
                await _stream.FlushAsync();
                Debug.Log("已发送创建房间请求");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void OnRoomPlayerUpdate(byte[] data)
        {
            var pkg = JsonConvert.DeserializeObject<RoomPlayerUpdatePackage>(
                Encoding.UTF8.GetString(data));

            MainDispatcher.Instance.ExecuteOnMainThread(() =>
            {
                // 更新本地房间玩家列表
                PlayerManager.Instance().UpdateRoomPlayers(pkg.Players);

                // 广播 UI 事件
                EventCenter.Broadcast(UIEvent.ON_ROOM_PLAYER_UPDATE, pkg);

                if (pkg.UpdateType == "Join")
                    Debug.Log($"{pkg.PlayerName} 加入了房间");
                else
                    Debug.Log($"{pkg.PlayerName} 离开了房间");
            });
        }

        public async Task SendLeaveRoom()
        {
            var sessionMsg = new SessionPackage
            {
                _sessionID = _uuid,
                _roomID = PlayerManager.Instance().GetCardRoomInfo()._id
            };
            await SendTcpMessage(SessionMessageID.LeaveRoom, sessionMsg);
        }

        public void OnJoinRoomResult(byte[] data)
        {
            try
            {
                string jsonStr = Encoding.UTF8.GetString(data);
                var pkg = JsonConvert.DeserializeObject<JoinRoomResponse>(jsonStr);
                if (pkg == null)
                {
                    Debug.Log("pkg==null");
                }
                if (ErrorCode.Success != pkg.ErrorCode)
                {
                    Debug.Log($"服务器返回错误: {pkg.ErrorCode}");
                    return;
                }
                Debug.Log($"服务器房间ID: {pkg.ErrorCode}");
                MainDispatcher.Instance.ExecuteOnMainThread(() =>
                    {
                        PlayerManager.Instance().JoinRoom(pkg.ExistingPlayers, pkg.RoomName, pkg.RoomId);
                        EventCenter.Broadcast(UIEvent.ON_CREATE_ROOM_SUCCESS, null);
                    }
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void OnTcpCreateRoomResult(byte[] data)
        {
            Debug.Log("处理房间信息");
            string jsonStr = Encoding.UTF8.GetString(data);
            var pkg = JsonConvert.DeserializeObject<SessionPackage>(jsonStr);
            if (pkg == null)
            {
                Debug.Log("pkg==null");
            }

            if (ErrorCode.Success != pkg._errorCode)
            {
                Debug.Log($"服务器返回错误: {pkg._errorCode}");
                //unity UI显示创建房间失败
                return;
            }
            //unity UI进入房间
            Debug.Log(pkg._roomID);
            MainDispatcher.Instance.ExecuteOnMainThread(() =>
                {
                    PlayerManager.Instance().CreateRoomHost(pkg._roomName, pkg._roomID);
                    EventCenter.Broadcast(UIEvent.ON_CREATE_ROOM_SUCCESS, null);
                }
                );

            Debug.Log("房间创建成功");
        }
        #endregion
        private async Task SendTcpMessage<T>(SessionMessageID msgId, T payload)
        {
            try
            {
                byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
                using var package = new NetPackage();
                package.WriteInt((int)msgId);
                package.WriteInt(json.Length);
                package.WriteBytes(json);
                await _stream.WriteAsync(package.ToArray(), 0, package.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardClient] 发送 {msgId} 失败: {ex.Message}");
            }
        }

        #region 对战逻辑TCP
        //开始对战请求
        public async Task TcpStartGameRequest(PlayerBaseData _player)
        {
            try
            {
                //创建开始游戏请求
                var sessionMsg = new SessionPackage { _sessionID = _uuid };
                sessionMsg._roomID = PlayerManager.Instance().GetCardRoomInfo()._id;
                byte[] json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sessionMsg));
                using var package = new NetPackage();
                package.WriteInt((int)SessionMessageID.BattleStart);
                package.WriteInt(json.Length);
                package.WriteBytes(json);
                await _stream.WriteAsync(package.ToArray(), 0, package.Length);
                await _stream.FlushAsync();
                Debug.Log("已发送请求，等待响应...");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void OnStartGameResult(byte[] data)
        {
            try
            {
                string jsonStr = Encoding.UTF8.GetString(data);
                var battleInitInfo = JsonConvert.DeserializeObject<BattleStartPackage>(jsonStr);

                Debug.Log($"OnStartGameResult 收到数据，JSON: {jsonStr}");

                if (battleInitInfo == null)
                {
                    Debug.LogError("battleInitInfo 反序列化结果为 null");
                    return;
                }

                Debug.Log($"反序列化成功，MyHand数量: {battleInitInfo.MyHand?.Count}");

                MainDispatcher.Instance.ExecuteOnMainThread(() =>
                {
                    _pendingBattleInitInfo = battleInitInfo;
                    Debug.Log($"已赋值 _pendingBattleInitInfo，MyHand数量: {_pendingBattleInitInfo.MyHand?.Count}");

                    EventCenter.Broadcast(UIEvent.ON_PLAYER_STATE_GAME);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        //使用牌
        public async Task SendPlayCard(int cardInstanceId)
        {
            var playCardPkg = new UseCardPackage
            {
                SessionId = _uuid,
                ActionId = Guid.NewGuid().ToString(),
                CardInstanceId = cardInstanceId
            };
            await SendTcpMessage(SessionMessageID.PlayCard, playCardPkg);
        }
        //攻击对象
        public async Task SendAttack(int attackerInstanceId, int targetInstanceId)
        {
            var attackPkg = new AttackPackage
            {
                SessionId = _uuid,
                ActionId = Guid.NewGuid().ToString(),
                AttackerInstanceId = attackerInstanceId,
                TargetInstanceId = targetInstanceId
            };
            await SendTcpMessage(SessionMessageID.Attack, attackPkg);
        }

        //结束回合请求
        public async Task SendEndTurn()
        {
            var endTurn = new EndTurnPackage
            {
                SessionId = _uuid,
                ActionId = Guid.NewGuid().ToString()
            };
            await SendTcpMessage(SessionMessageID.EndTurn, endTurn);
        }
        //接收服务器广播的战场状态
        private void OnBattleStateSync(byte[] data)
        {
            var state = JsonConvert.DeserializeObject<BattleState>(
            Encoding.UTF8.GetString(data));

            // 切回主线程再通知 BattleManager（你已有 MainDispatcher）
            MainDispatcher.Instance.ExecuteOnMainThread(() =>
                OnBattleStateReceived?.Invoke(state));
        }

        //接收服务器对操作的确认
        private void OnBattleActionAck(byte[] data)
        {
            var ack = JsonConvert.DeserializeObject<ActionAck>(
                Encoding.UTF8.GetString(data));
            MainDispatcher.Instance.ExecuteOnMainThread(() =>
                OnActionAckReceived?.Invoke(ack));
        }

        private void OnForceLeaveRoom(byte[] data)
        {
            var pkg = JsonConvert.DeserializeObject<ForceLeavePackage>(
                Encoding.UTF8.GetString(data));

            MainDispatcher.Instance.ExecuteOnMainThread(() =>
            {
                Debug.Log($"被强制退出房间，原因: {pkg.Reason}");

                // 清理本地房间数据
                PlayerManager.Instance().LeaveRoom();

                // 返回大厅
                EventCenter.Broadcast(UIEvent.ON_FORCE_LEAVE_ROOM);
            });
        }

        public void StartLoadBattleScene()
        {
            StartCoroutine(LoadBattleScene());
        }

        private IEnumerator LoadBattleScene()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync("BattleS");
            // 等场景真正加载完
            yield return op;
            if (_pendingBattleInitInfo != null)
            {
                BattleManager.Instance.GameStart(_pendingBattleInitInfo);
                _pendingBattleInitInfo = null;
            }
            else
            {
                Debug.LogError("场景加载完但 _pendingBattleInitInfo 是 null");
            }
        }

        #endregion
    }
}