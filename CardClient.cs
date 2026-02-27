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
                udpClient = new UdpClient();
                udpClient.Connect(ip, port);
                reBuffer = new byte[NetConfig.BufferSize];
                running = true;
                Console.WriteLine("Client started and connected to server.");
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
        

        private void ReceiveTcpData()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending TCP data: " + ex.Message);
            }
        }

        private void ReceiveUdpData()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending TCP data: " + ex.Message);
            }
        }


        private void SendTcpData(byte[] data)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending TCP data: " + ex.Message);
            }
        }


        private void SendUdpData(byte[] data, string ip, int port)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending TCP data: " + ex.Message);
            }
        }

    }
}