using Penguin.Remote.Attributes;
using Penguin.Remote.Responses;

namespace Penguin.Remote.Commands
{
    public class Pull : ServerCommand<PullResponse>
    {
        [SerializationData(Size = 255)]
        public string RemotePath { get; set; }

        public Pull(string remotePath)
        {
            this.RemotePath = remotePath;
        }

        public Pull()
        {
        }
    }
}