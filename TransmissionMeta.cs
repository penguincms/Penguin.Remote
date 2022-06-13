using Penguin.Remote.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Remote
{
    public class TransmissionMeta
    {
        public virtual long PayloadSize { get; set; }

        [SerializationData(size: 255)]
        public virtual string RemoteCommandKind { get; set; } = string.Empty;
    }
}
