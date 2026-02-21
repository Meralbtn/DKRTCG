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


namespace UnityServer
{
    internal class CardClient
    {
        private int ClientId { get; set; }
        private NetworkStream Stream { get; set; }
        private byte[] Buffer { get; set; }
        private TcpClient client { get; set; }
        private IPEndPoint endPoint { get; set; }

        public CardClient(int clientId, NetworkStream stream, byte[] buffer, TcpClient tcpClient, IPEndPoint endPoint)
        {
            ClientId = clientId;
            Stream = stream;
            Buffer = buffer;
            client = tcpClient;
            this.endPoint = endPoint;
        }

        

        

    }
}