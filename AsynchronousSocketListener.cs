using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Penguin.Remote
{
    public class AsynchronousSocketListener : AsyncConnector
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener(int port, string localAddress = null)
        {
            this.Port = port;
            this.LocalAddress = localAddress;
        }

        public int Port { get; private set; }

        public string LocalAddress { get; private set; }

        public IPEndPoint LocalEndPoint { get; private set; }
        public void Start()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  

            if (this.LocalAddress is null)
            {
                this.IpHostInfo = Dns.GetHostEntry(Dns.GetHostName());

                this.IpAddress = this.IpHostInfo.AddressList[0];
            }
            else
            {
                this.IpAddress = IPAddress.Parse(this.LocalAddress);
            }

            this.LocalEndPoint = new IPEndPoint(this.IpAddress, this.Port);

            // Create a TCP/IP socket.  
            Socket listener = new(this.IpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(this.LocalEndPoint);

                listener.Listen(int.MaxValue);

                // Set the event to nonsignaled state.  
                _ = allDone.Reset();

                while (true)
                {
                    try
                    {
                        // Start an asynchronous socket to listen for connections.  
                        Console.WriteLine("Waiting for a connection...");
                        
                        _ = listener.BeginAccept(
                            new AsyncCallback(this.AcceptCallback),
                            listener);

                        // Wait until a connection is made before continuing.  
                        _ = allDone.WaitOne();

                    } catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    } finally
                    {
                        // Set the event to nonsignaled state.  
                        _ = allDone.Reset();
                    }


                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            _ = Console.Read();

        }

        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            _ = allDone.Set();

            // Get the socket that handles the client request.  
            Socket? listener = (Socket)ar.AsyncState;

            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new()
            {
                workSocket = handler
            };

            _ = handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(this.ReadCallback), state);
        }

        protected virtual void ReadCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the handler socket  
                // from the asynchronous state object.  
                StateObject? state = (StateObject)ar.AsyncState;

                Socket handler = state.workSocket;

                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.Append(bytesRead);

                    if (state.IsComplete)
                    {
                        Console.WriteLine("Read {0} bytes from socket.", state.Data.Count);

                        byte[] response = Functions.Execute(state.Data.ToArray());

                        this.Send(handler, response);
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        _ = handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(this.ReadCallback), state);
                    }
                }
            } catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket? handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);

                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}
