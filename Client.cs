using Penguin.Remote.Commands;
using Penguin.Remote.Responses;
using System.Net;
using System.Threading.Tasks;

namespace Penguin.Remote
{
    public class Client
    {
        public string Host { get; private set; }
        public int Port { get; private set; }

        private readonly IPHostEntry IpHostInfo;

        private readonly IPAddress IpAddress;

        private readonly IPEndPoint RemoteEP;

        public Client(string host, int port, int maximumConnections = 10)
        {
            this.Host = host;
            this.Port = port;

            if (IPAddress.TryParse(host, out IPAddress ipAddress))
            {
                this.IpAddress = ipAddress;
            }
            else
            {
                this.IpHostInfo = Dns.GetHostEntry(this.Host);
                this.IpAddress = this.IpHostInfo.AddressList[0];
            }

            this.RemoteEP = new IPEndPoint(this.IpAddress, port);

            Queue = new AsyncQueue(maximumConnections);
        }

        private AsyncQueue Queue;
        //Def a better way to handle this. Do what you did with the logging worker

        public async Task<TResponse> Send<TResponse>(ServerCommand<TResponse> command) where TResponse : ServerResponse, new()
        {
            Transmission<TResponse> transmission = new(this.IpHostInfo, this.IpAddress, this.RemoteEP);

            TResponse response = await Queue.Execute(async () => await transmission.Send(command));

            return response;
        }
    }
}