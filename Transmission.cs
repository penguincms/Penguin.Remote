using Penguin.Remote.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Penguin.Remote.Responses;

namespace Penguin.Remote
{
    class Transmission<TResponse> : AsyncConnector where TResponse : ServerResponse, new()
    {
        private readonly IPEndPoint RemoteEP;


        public Transmission(IPHostEntry ipHostInfo, IPAddress ipAddress, IPEndPoint remoteEP)
        {
            this.IpAddress = ipAddress;
            this.IpHostInfo = ipHostInfo;
            this.RemoteEP = remoteEP;
        }

        private TaskCompletionSource<SocketConnectResult> ConnectDone;

        private readonly TaskCompletionSource SendDone = new();

        private readonly TaskCompletionSource<TResponse> ReceiveDone = new();

        protected void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject
                {
                    workSocket = client
                };

                // Begin receiving the data from the remote device.  
                _ = client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(this.ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task<TResponse> Send(ServerCommand<TResponse> command)
        {

            byte[] data = SerializationHelper.Serialize(command);

            // Create a TCP/IP socket.  
            Socket client;

            SocketConnectResult result;

            do
            {

                this.ConnectDone = new TaskCompletionSource<SocketConnectResult>();

                client = new Socket(this.IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                _ = client.BeginConnect(this.RemoteEP, new AsyncCallback(this.ConnectCallback), client);

                result = await ConnectDone.Task;
            }
            while (result.State == SocketConnectState.Retry);

            // Send test data to the remote device.  
            this.Send(client, data);

            await SendDone.Task;

            // Receive the response from the remote device.  
            this.Receive(client);

            TResponse response = await ReceiveDone.Task;

            // Release the socket.  
            client.Shutdown(SocketShutdown.Both);

            client.Close();

            return response;
        }
        private enum SocketConnectState
        {
            Success,
            Fail,
            Retry
        }
        private class SocketConnectResult
        {
            public SocketConnectState State { get; set; }
            public Exception Exception { get; set; }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                bool retry = false;
                Exception ex = null;

                try
                {
                    // Complete the connection.  
                    client.EndConnect(ar);

                    if (!client.Connected)
                    {
                        retry = true;
                    }

                }
                catch (SocketException sex) when (sex.Message.Contains("did not properly respond after a period of time"))
                {
                    retry = true;
                    ex = sex;
                }
                catch (Exception iex)
                {
                    Debug.WriteLine(iex);
                    Debugger.Break();
                    throw;
                }

                if (retry)
                {
                    ConnectDone.SetResult(new SocketConnectResult()
                    {
                        State = SocketConnectState.Retry,
                        Exception = ex
                    });

                    return;
                }

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                ConnectDone.SetResult(new SocketConnectResult()
                {
                    State = SocketConnectState.Success
                });
            }
            catch (Exception e)
            {
                this.ReturnException(e);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;

                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                state.Append(bytesRead);

                if (state.IsComplete)
                {
                    TResponse response = SerializationHelper.Deserialize<TResponse>(state.Data.ToArray());

                    ReceiveDone.SetResult(response);
                }
                else if (bytesRead > 0)
                {
                    // Get the rest of the data.  
                    _ = client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(this.ReceiveCallback), state);
                }
                else
                {
                    Debugger.Break();
                    // Signal that all bytes have been received.  

                }
            }
            catch (Exception e)
            {
                this.ReturnException(e);
            }
        }

        private void ReturnException(Exception e)
        {
            this.ReceiveDone.SetResult(new TResponse()
            {
                Text = e.Message,
                Success = false
            });

            Console.WriteLine(e.ToString());
        }

        // ManualResetEvent instances signal completion.  


        protected override void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                SendDone.SetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
