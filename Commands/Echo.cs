using Penguin.Remote.Responses;

namespace Penguin.Remote.Commands
{
    public class Echo : ServerCommand<EchoResponse>
    {
        public Echo()
        {
        }

        public Echo(string toEcho)
        {
            this.Text = toEcho;
        }
    }
}