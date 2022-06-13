using System;
using System.Net;
using System.Net.Sockets;

namespace Penguin.Remote
{
    public abstract class AsyncConnector
    {
        public IPHostEntry IpHostInfo { get; protected set; }
        public IPAddress IpAddress { get; protected set; }

        protected void Send(Socket client, byte[] byteData) => _ = client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(this.SendCallback), client);

        protected abstract void SendCallback(IAsyncResult ar);

    }
}
