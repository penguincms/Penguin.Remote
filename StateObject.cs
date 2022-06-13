using Penguin.Remote.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;

namespace Penguin.Remote
{
    internal class StateObject
    {
        public void Append(byte[] data, int length) => this.Data.AddRange(data.Take(length));
        public void Append(byte[] data) => this.Data.AddRange(data);
        public void Append(int length) => this.Append(this.buffer, length);

        public long PackageLength
        {
            get
            {
                int metaSizeLength = SerializationHelper.GetPayloadSizeLength() + typeof(TransmissionMeta).GetProperty(nameof(TransmissionMeta.RemoteCommandKind)).GetCustomAttribute<SerializationData>().Size;

                if (this.packageLength == 0 && this.Data.Count > metaSizeLength)
                {
                    this.packageLength = SerializationHelper.GetPackageLength(this.Data.Take(metaSizeLength).ToArray());

                    Console.WriteLine("Expected Package Length: " + this.packageLength);
                }

                return this.packageLength;
            }
        }

        public bool IsComplete => this.PackageLength != 0 && this.Data.Count >= this.PackageLength;

        private long packageLength;

        // Size of receive buffer.  
        public const int BufferSize = 1_048_576;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public List<byte> Data = new();

        // Client socket.
        public Socket? workSocket = null;
    }
}
