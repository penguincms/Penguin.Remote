using Penguin.Remote.Responses;

namespace Penguin.Remote.Commands
{
    public class ServerCommand<TResponse> : TransmissionPackage where TResponse : ServerResponse, new()
    {
    }
}