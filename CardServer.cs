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
                        lock (tcpClientsLock)
                        {
                            Console.WriteLine($"当前连接的TCP客户端数量: {tcpClients.Count}");
                            foreach(var client in tcpClients.Values)
                            {
                                Console.WriteLine($"客户端ID: {client.ClientId}, 远程地址: {client.endPoint}");
                            }
                        }
                    }
                    else if(key.Key == ConsoleKey.U)
                    {        
                        Console.WriteLine("UDP功能尚未实现");
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
                int clientId = nextClientId++;
                var clientState = new TcpClientState
                {
                    ClientId = clientId,
                    Stream = tcpClient.GetStream(),
                    Buffer = new byte[1024],
                    client = tcpClient,
                    endPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint
                };
                //加锁，字典添加状态
                lock (tcpClientsLock)
                {
                    tcpClients.Add(clientId, clientState);
                }
                Console.WriteLine($"TCP客户端连接: {clientState.endPoint}, 客户端ID: {clientId}");
                //继续监听下一个连接
                tcpListener.BeginAcceptTcpClient(OnTcpClientConnected, null);
                //开始读取数据
                clientState.Stream.BeginRead(clientState.Buffer, 0, clientState.Buffer.Length, OnTcpDataReceived, clientState);
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
                    string receivedText = Encoding.UTF8.GetString(clientState.Buffer, 0, bytesRead);
                    Console.WriteLine($"收到来自客户端 {clientState.ClientId} 的数据: {receivedText}");
                    //继续读取数据
                    clientState.Stream.BeginRead(clientState.Buffer, 0, clientState.Buffer.Length, OnTcpDataReceived, clientState);
                }
                else
                {
                    //客户端断开连接
                    Console.WriteLine($"TCP客户端断开连接: {clientState.endPoint}, 客户端ID: {clientState.ClientId}");
                    lock (tcpClientsLock)
                    {
                        tcpClients.Remove(clientState.ClientId);
                    }
                    clientState.Stream.Close();
                    clientState.client.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理TCP数据失败: {ex.Message}");
                //发生异常，认为客户端断开连接
                Console.WriteLine($"TCP客户端断开连接: {clientState.endPoint}, 客户端ID: {clientState.ClientId}");
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
            //UDP数据接收回调函数，尚未实现
        }
    }    
}
