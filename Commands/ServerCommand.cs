using Penguin.Remote.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Remote.Commands
{
    public class ServerCommand<TResponse> : TransmissionPackage where TResponse : ServerResponse, new()
    {
    }
}
