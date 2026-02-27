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
//实现一个CardClient类，包含客户端的基本信息和网络通信功能

namespace UnityServer
{
    internal class CardClient
    {
        private TcpClient tcpClient = null;
        private UdpClient udpClient = null;
        private NetworkStream tcpStream = null;
        private bool running = false;
        private byte[] reBuffer = null;
        
        //需要知道启用端口和IP
        public void Start(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(ip, port);
                tcpStream = tcpClient.GetStream();
                reBuffer = new byte[NetConfig.BufferSize];
                running = true;
                //启动接收线程
                tcpStream.BeginRead(reBuffer, 0, reBuffer.Length, ReceiveTcpData, null);
                udpClient = new UdpClient();
                udpClient.Connect(ip, port);
                udpClient.BeginReceive(ReceiveUdpData, null);
                while (running)
                {
                    //保持客户端运行
                    Task.Delay(100).Wait();
                    var key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.T:
                            //发送TCP数据
                            string tcpMessage = "Hello TCP Server!";
                            SendTcpData();
                            Console.WriteLine("Sent TCP message: " + tcpMessage);
                            break;
                        case ConsoleKey.U:
                            //发送UDP数据
                            string udpMessage = "Hello UDP Server!";
                            SendUdpData();
                            Console.WriteLine("Sent UDP message: " + udpMessage);
                            break;
                        case ConsoleKey.Escape:
                            //退出客户端
                            running = false;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting client: " + ex.Message);
            }
            finally
            {
                udpClient?.Close();
                tcpClient?.Close();
                tcpStream?.Close();
                Console.WriteLine("Client stopped.");
            }
          
        }
        

        private void ReceiveTcpData(IAsyncResult result)
        {
            try
            {
                //获取字节长度
                int bytesRead = tcpStream.EndRead(result);
                if (bytesRead > 0)
                {
                    //获取一个新data存储
                    byte[] data = new byte[bytesRead];
                    for (int i = 0; i < bytesRead; i++)
                    {
                        data[i] = reBuffer[i];
                    }
                    using Packet packet = new Packet(data);
                    int packetId = packet.ReadInt();
                    Console.WriteLine("Received TCP packet with ID: " + packetId);
                    if(packetId == 1)
                    {
                        string message = packet.ReadString();
                        Console.WriteLine("Message from server: " + message);
                    }
                }
                else
                {
                    Console.WriteLine("TCP connection closed by server.");
                    running = false;
                    return;
                }
                //读取后继续接受信息
                reBuffer = new byte[NetConfig.BufferSize];
                tcpStream.BeginRead(reBuffer, 0, reBuffer.Length, ReceiveTcpData, null);
            }
            catch(IOException)
            {
                Console.WriteLine("TCP connection closed by server.");
                running = false;
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("TCP stream has been closed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending TCP data: " + ex.Message);
                running = false;
            }
        }

        private void ReceiveUdpData(IAsyncResult result)
        {
            try
            {
                //udp监听端口内的任何地址
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.EndReceive(result, ref remoteEndPoint);
                using Packet packet = new Packet(data);
                int packetId = packet.ReadInt();
                Console.WriteLine("Received UDP packet with ID: " + packetId);
                if (packetId == 1)
                {
                    string message = packet.ReadString();
                    Console.WriteLine("Message from server: " + message);
                }
                //继续接受UDP数据
                udpClient.BeginReceive(ReceiveUdpData, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending TCP data: " + ex.Message);
            }
        }


        private void SendTcpData()
        {
            try
            {
                string msg = "这是服务器发送的消息"+DateTime.Now.ToString();
                 //转换为字节数组
                 using Packet packet = new Packet();
                packet.WriteInt(1);
                packet.WriteString(msg);
                var data = packet.GetBytesArray();
                tcpStream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending TCP data: " + ex.Message);
            }
        }


        private void SendUdpData()
        {
            try
            {
                string msg = "这是服务器发送的消息"+DateTime.Now.ToString();
                 //转换为字节数组
                 using Packet packet = new Packet();
                packet.WriteInt(1);
                packet.WriteString(msg);
                var data = packet.GetBytesArray();
                udpClient.Send(data, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending UDP data: " + ex.Message);
            }
        }

    }
}