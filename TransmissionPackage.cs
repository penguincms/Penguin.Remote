using Penguin.Remote.Attributes;
using System;

namespace Penguin.Remote.Commands
{
    public class TransmissionPackage : TransmissionMeta
    {
        public override long PayloadSize
        {
            get => this.Payload.Length;
            set => this.Payload = new byte[value];
        }

        [SerializationData(size: 255)]
        public override string RemoteCommandKind => this.GetType().FullName;

        [SerializationData(Order = int.MaxValue)]
        public virtual byte[] Payload { get; set; } = Array.Empty<byte>();

        [DontSerialize]
        public string Text
        {
            get => System.Text.Encoding.UTF8.GetString(this.Payload);
            set => this.Payload = System.Text.Encoding.UTF8.GetBytes(value);
        }
    }
}
