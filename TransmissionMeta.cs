using Penguin.Remote.Attributes;

namespace Penguin.Remote
{
    public class TransmissionMeta
    {
        public virtual long PayloadSize { get; set; }

        [SerializationData(size: 255)]
        public virtual string RemoteCommandKind { get; set; } = string.Empty;
    }
}