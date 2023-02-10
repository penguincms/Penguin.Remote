using Penguin.Remote.Attributes;
using Penguin.Remote.Responses;

namespace Penguin.Remote.Commands
{
    public class EnumerateFiles : ServerCommand<EnumerateFilesResponse>
    {
        [SerializationData(size: 255)]
        public string Path { get; set; }

        [SerializationData(size: 255)]
        public string Mask { get; set; } = "*";

        public bool Recursive { get; set; }
    }
}