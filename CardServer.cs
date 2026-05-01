using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
//暂时利用json进行传输


//建立一个server类，管理session

namespace CardGameServer
{
    //session类，用uuid管理，存储玩家信息，房间信息等
    public class Session : IDisposable
    {
        // 使用 Guid 确保全球唯一，且更难被外部猜测
        public Guid SessionId { get; private set; }
        public TcpClient TcpClient { get; private set; }
        public string PlayerName { get; set; }

        // 玩家当前的 UDP 地址 (支持动态更新)
        public IPEndPoint UdpEndPoint { get; set; }
        public NetworkStream Stream;
        public DateTime LastActiveTime { get; private set; }
        public List<byte> Buffer = new List<byte>();
        private byte[] _receiveBuffer = new byte[4096];
        public bool IsUdpReady => UdpEndPoint != null;

        public Session(TcpClient tcpClient)
        {
            SessionId = Guid.NewGuid();
            Stream = tcpClient.GetStream();
            TcpClient = tcpClient;
            Refresh();
        }

        public void Refresh() => LastActiveTime = DateTime.UtcNow;

        public async Task StartReceive()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine($"等待TCP数据来自: {TcpClient.Client.RemoteEndPoint}");
                    int bytesRead = await Stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length);

                    if (bytesRead <= 0)
                    {
                        Console.WriteLine($"TCP连接关闭: {TcpClient.Client.RemoteEndPoint}");
                        Close();
                        break;
                    }

                    Console.WriteLine($"接收到 {bytesRead} 字节数据");
                    Buffer.AddRange(_receiveBuffer.Take(bytesRead));

                    ProcessTcpBuffer();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"接收异常: {e.Message}");
                Close();
            }
        }
        public void ProcessTcpBuffer()
        {
            while (true)
            {
                if (Buffer.Count < 8) //至少需要8字节来读取id和length
                {
                    return;
                }
                Console.WriteLine($"当前Buffer长度: {Buffer.Count}");
                byte[] bufferArray = Buffer.ToArray();
                int id = BitConverter.ToInt32(bufferArray, 0);
                int length = BitConverter.ToInt32(bufferArray, 4);
                if (Buffer.Count < 8 + length) //数据不完整，等待下一次接收
                {
                    return;
                }
                byte[] jsonData = bufferArray.Skip(8).Take(length).ToArray();
                //在数据中移除已经处理的数据
                Buffer.RemoveRange(0, 8 + length);
                using NetPackage package = new NetPackage();
                package.WriteInt(id);
                package.WriteInt(length);
                package.WriteBytes(jsonData);
                Console.WriteLine($"处理TCP数据: id={id}, length={length}, json={Encoding.UTF8.GetString(jsonData)}");
                byte[] data = package.ToArray();
                NetPackage readPkg = new NetPackage(data);
                CardLogic.instance.AddTcpRequest(this, readPkg);
            }
        }


        public void SendTcpData(NetPackage package)
        {
            try
            {
                byte[] packet = package.ToArray();
                Stream.Write(packet, 0, packet.Length);
                Console.WriteLine($"发送TCP数据到: {TcpClient.Client.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送TCP数据时发生错误: {ex.Message}");
                Dispose();
            }
        }


        public bool IsTimeout(int timeoutSeconds)
            => (DateTime.UtcNow - LastActiveTime).TotalSeconds > timeoutSeconds;

        // 统一销毁资源
        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            TcpClient?.Close();
            TcpClient?.Dispose();
        }
    }

    public class SessionManager
    {
        // 主索引：通过 UUID 找 Session (逻辑唯一)
        private readonly ConcurrentDictionary<Guid, Session> _sessionsById = new();

        // 辅助索引：通过 IPEndPoint 字符串找 ID (网络映射)
        private readonly ConcurrentDictionary<string, Guid> _endpointToIdMap = new();

        /// <summary>
        /// 创建新会话
        /// </summary>
        public Session Create(TcpClient tcpClient)
        {
            var session = new Session(tcpClient);
            _sessionsById.TryAdd(session.SessionId, session);
            return session;
        }

        /// <summary>
        /// 核心：通过 UUID 获取 Session，并根据当前包的来源更新网络地址（解决切网问题）
        /// </summary>
        public Session GetAndBind(Guid sessionId, IPEndPoint currentEndPoint)
        {
            if (_sessionsById.TryGetValue(sessionId, out var session))
            {
                // 如果当前包的地址与记录不符，说明发生了“漫游/切网”
                if (session.UdpEndPoint == null || !session.UdpEndPoint.Equals(currentEndPoint))
                {
                    // 清理旧映射，建立新映射
                    if (session.UdpEndPoint != null)
                        _endpointToIdMap.TryRemove(session.UdpEndPoint.ToString(), out _);

                    session.UdpEndPoint = currentEndPoint;
                    _endpointToIdMap.TryAdd(currentEndPoint.ToString(), sessionId);
                }

                session.Refresh();
                return session;
            }
            return null;
        }

        public Session GetById(Guid sessionId)
        {
            _sessionsById.TryGetValue(sessionId, out var session);
            return session;
        }

        /// <summary>
        /// 移除并释放会话
        /// </summary>
        public void Remove(Guid sessionId)
        {
            if (_sessionsById.TryRemove(sessionId, out var session))
            {
                if (session.UdpEndPoint != null)
                    _endpointToIdMap.TryRemove(session.UdpEndPoint.ToString(), out _);

                session.Dispose(); // 记得释放 Socket 资源
            }
        }

        /// <summary>
        /// 扫描并清理超时会话
        /// </summary>
        public void CleanupTimeouts(int timeoutSeconds)
        {
            var timeoutIds = _sessionsById.Values
                .Where(s => s.IsTimeout(timeoutSeconds))
                .Select(s => s.SessionId)
                .ToList();

            foreach (var id in timeoutIds)
            {
                Remove(id);
                Console.WriteLine($"[Session] 清理超时会话: {id}");
            }
        }
    }

    public class CardServer
    {
        //创建Udp服务器来监听客户端请求
        private UdpClient _udpServer;
        private TcpListener _tcpServer;
        private SessionManager _sessionManager = new SessionManager();
        private int _serverPort = 8888;
        private bool _initialized = false;


        public CardServer()
        {
            _udpServer = new UdpClient(_serverPort);
            _tcpServer = new TcpListener(IPAddress.Any, _serverPort);
            CardLogic.instance.OnUdpSendData += SendUdpData;
            CardLogic.instance.OnTcpSendData = (guid, pkg) =>
            {
                 var session = _sessionManager.GetById(guid);
                 session?.SendTcpData(pkg);
            };
        }

        public void Open()
        {
            if (_initialized)
                return;
            _initialized = true;
            _tcpServer.Start();
            _tcpServer.BeginAcceptTcpClient(OnTcpClientAccepted, null);
            _udpServer.BeginReceive(OnUdpDataReceived, null);
            Console.WriteLine($"网关服务器已启动，监听端口: {_serverPort}...");
        }

        #region TCP连接处理
        //我们将协议分为int id + int length + json序列化的数据包，id用来区分不同的请求类型，length用来确定数据长度，json序列化的数据用来传输具体的请求内容
        //处理粘包的函数

        private void OnTcpClientAccepted(IAsyncResult ar)
        {
            try
            {
                var tcpClient = _tcpServer.EndAcceptTcpClient(ar);
                Console.WriteLine($"新TCP连接: {tcpClient.Client.RemoteEndPoint}");
                var session = _sessionManager.Create(tcpClient);
                session.StartReceive(); // 开始接收数据
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受TCP连接时发生错误: {ex.Message}");
            }
            finally
            {
                // 继续接受下一个 TCP 连接
                _tcpServer.BeginAcceptTcpClient(OnTcpClientAccepted, null);
            }
        }


        #endregion


        #region UDP数据处理
        public void OnUdpDataReceived(IAsyncResult ar)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _udpServer.EndReceive(ar, ref remoteEndPoint);
                Console.WriteLine($"收到UDP数据来自: {remoteEndPoint}");

                // 解析数据包，假设前16字节是SessionId
                NetPackage package = new NetPackage(data);
                //需要修改
                if (remoteEndPoint == null)
                {
                    Console.WriteLine("无法解析UDP数据包: 无法获取远程端点");
                    return;
                }
                CardLogic.instance.AddUdpRequest(remoteEndPoint, package);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理UDP数据时发生错误: {ex.Message}");
            }
            finally
            {
                // 继续监听下一个 UDP 数据包
                _udpServer.BeginReceive(OnUdpDataReceived, null);
            }
        }

        public void SendUdpData(IPEndPoint endPoint, NetPackage package)
        {
            try
            {
                _udpServer.Send(package.ToArray(), package.ToArray().Length, endPoint);
                Console.WriteLine($"发送UDP数据到: {endPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送UDP数据时发生错误: {ex.Message}");
            }
        }

        #endregion


    }
}