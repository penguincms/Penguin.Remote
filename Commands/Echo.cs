using Penguin.Remote.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
