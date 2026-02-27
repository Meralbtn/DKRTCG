using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using System.Collections;
using ServerConnectData;
//服务器

namespace UnityServer
{
    internal class CardServer
    {
        private TcpListener tcpListener = null;
        private UdpClient  udpClient = null;
        private bool isRunning = false;
        private Dictionary<int, TcpClientState> tcpClients = new Dictionary<int, TcpClientState>();
        private int nextClientId = 1;
        //增加线程锁，保护tcpClients字典的访问
        private object tcpClientsLock = new object();
        //服务器状态类
        private class TcpClientState
        {
            public int ClientId { get; set; }
            public NetworkStream Stream { get; set; }
            public byte[] Buffer { get; set; }
            public TcpClient client { get; set; }   
            public IPEndPoint endPoint { get; set; }   
        }

        //单例实现
        
        //服务器启动函数
        public void Start(ushort port)
        {
            //启动服务器，监听TCP和UDP端口
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                tcpListener.BeginAcceptTcpClient(OnTcpClientConnected, null);

                udpClient = new UdpClient(port);
                udpClient.BeginReceive(OnUdpDataReceived, null);

                isRunning = true;
                Console.WriteLine($"服务器已启动，监听端口 {port}");

                while (isRunning)
                {
                    //服务器主循环，可以在这里处理其他任务
                    //例如定时清理断开的客户端等
                    var key = Console.ReadKey(true);
                    if(key.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("正在停止服务器...");
                        isRunning = false;
                    }
                    else if(key.Key == ConsoleKey.T)
                    {
                        SendAllTCP();
                    }
                    else if(key.Key == ConsoleKey.U)
                    {
                        SendAllUDP();
                    }
                 
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务器启动失败: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("服务器已停止");
                lock (tcpClientsLock)
                {
                    foreach(var client in tcpClients.Values)
                    {
                        client.Stream.Close();
                        client.client.Close();
                    }
                    tcpClients.Clear();
                }
                udpClient?.Close();
                tcpListener?.Stop();
            }

        }

        //定义链接回调函数
        private void OnTcpClientConnected(IAsyncResult ar)
        {
            if (!isRunning) return;
            try
            {
                TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);

                //这里id并未使用随机生成，而是简单的递增，这在实际应用中可能需要改进以避免冲突
                int clientId;
                lock (tcpClientsLock)
                {
                    clientId = nextClientId++;
                }
                var clientState = new TcpClientState
                {
                    ClientId = clientId,
                    Stream = tcpClient.GetStream(),
                    //size 4096 bytes, 可以根据实际需求调整
                    Buffer = new byte[NetConfig.BufferSize],
                    client = tcpClient,
                    endPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint
                };
                //加锁，字典添加状态
                lock (tcpClientsLock)
                {
                    tcpClients.Add(clientId, clientState);
                }
                Console.WriteLine($"TCP客户端连接: {clientState.endPoint}, 客户端ID: {clientId}");
                //开始读取数据
                clientState.Stream.BeginRead(clientState.Buffer, 0, clientState.Buffer.Length, OnTcpDataReceived, clientState);

                //继续监听下一个连接
                tcpListener.BeginAcceptTcpClient(OnTcpClientConnected, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受TCP客户端连接失败: {ex.Message}");
            }
        }

        private void OnTcpDataReceived(IAsyncResult ar)
        {
            var clientState = (TcpClientState)ar.AsyncState;
            try
            {
                int bytesRead = clientState.Stream.EndRead(ar);
        
                if (bytesRead > 0)
                {
                    //处理接收到的数据
                    byte[] data = new byte[bytesRead];
                    for(int i = 0; i < bytesRead; i++)
                    {
                        data[i] = clientState.Buffer[i];
                    }
                    using (Packet packet = new Packet(data))
                    {
                        int id = packet.ReadInt();
                        if(id ==1)
                        {
                            string msg = packet.ReadString();
                            Console.WriteLine($"收到消息: {msg}");
                        }
                    }
                    //获取完数据后，重置buffer
                    clientState.Buffer = new byte[NetConfig.BufferSize];
                    string receivedText = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"收到来自客户端 {clientState.ClientId} 的数据: {receivedText}");
                    //继续读取数据
                    clientState.Stream.BeginRead(clientState.Buffer, 0, clientState.Buffer.Length, OnTcpDataReceived, clientState);
                }
                else
                {
                    //客户端断开连接
                    Console.WriteLine($"TCP客户端断开连接: {clientState.endPoint}, 客户端ID: {clientState.ClientId}");
                    RemoveTcpClient(clientState.ClientId);
                }
            }
            catch (ObjectDisposedException)
            {
              
                lock (tcpClientsLock)
                {
                    tcpClients.Remove(clientState.ClientId);
                }
                clientState.Stream.Close();
                clientState.client.Close();
            }
            catch(IOException)
            {
                Console.WriteLine($"处理TCP数据失败: IO异常");
                //从回调的会话状态来获取ID，认为客户端断开连接，清理资源
                Console.WriteLine($"TCP客户端断开连接: {clientState.endPoint}, 客户端ID: {clientState.ClientId}");
                RemoveTcpClient(clientState.ClientId);
            }
            catch (Exception ex)
                {
                //发生异常，认为客户端断开连接
                    Console.WriteLine($"TCP客户端断开连接: {clientState.endPoint}, 客户端ID: {clientState.ClientId}" + $"，异常: {ex.Message}"); ;
                lock (tcpClientsLock)
                {
                    tcpClients.Remove(clientState.ClientId);
                }
                clientState.Stream.Close();
                clientState.client.Close();
            }
        }
        private void OnUdpDataReceived(IAsyncResult ar)
        {
            //UDP数据接收回调函数

            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.EndReceive(ar, ref remoteEndPoint);

                using Packet packet = new Packet(data);
                
                int id = packet.ReadInt();
                if(id == 1)
                {
                    string msg = packet.ReadString();
                    Console.WriteLine($"收到UDP消息: {msg} 来自 {remoteEndPoint}");
                }

                //这里简单的通过远程端点来匹配TCP客户端，实际应用中可能需要更复杂的逻辑来关联UDP数据和TCP客户端
                foreach (var client in tcpClients.Values)
                {
                    if (client.endPoint!=null)
                    {
                        continue;
                    }
                    IPEndPoint tcpEndPoint = (IPEndPoint)client.client.Client.RemoteEndPoint;
                    if (tcpEndPoint.Address.Equals(remoteEndPoint.Address))
                    {
                        client.endPoint = remoteEndPoint;
                        break;
                    }
                }
                
                //继续监听UDP数据
                udpClient.BeginReceive(OnUdpDataReceived, null);
            }
            catch (ObjectDisposedException)
            {
                //UDP客户端已关闭，不需要处理
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理UDP数据失败: {ex.Message}");
                if(isRunning)
                {
                    udpClient.BeginReceive(OnUdpDataReceived, null);
                }
            }
        }

        //完成写事件,分为部分写和全部写
        private void SendAllTCP()
        {
            string msg = "这是服务器发送的消息"+DateTime.Now.ToString();
            //转换为字节数组
            using Packet packet = new Packet();
            packet.WriteInt(1);
            packet.WriteString(msg);
            var data = packet.GetBytesArray();
            foreach (var client in tcpClients.Values)
            {
                client.Stream.Write(data, 0, data.Length);
            }
        }

         private void SendAllUDP()
        {
            string msg = "这是服务器发送的消息"+DateTime.Now.ToString();
            //转换为字节数组
            using Packet packet = new Packet();
            packet.WriteInt(1);
            packet.WriteString(msg);
            var data = packet.GetBytesArray();
            foreach (var client in tcpClients.Values)
            {
                if(client.endPoint!=null)
                {
                    udpClient.Send(data, data.Length, client.endPoint);
                }
            }
        }


        //当出现错误，断开链接时，清理TcpClient
        private void RemoveTcpClient(int clientId)
        {
            lock (tcpClientsLock)
            {
                //找到id对应的客户端状态，关闭连接并从字典中移除
                if (tcpClients.TryGetValue(clientId, out var clientState))
                {
                    Console.WriteLine($"清理TCP客户端: {clientState.endPoint}, 客户端ID: {clientId}");
                    clientState.Stream.Close();
                    clientState.client.Close();
                    tcpClients.Remove(clientId);
                }
            }
        }
    }    
}
