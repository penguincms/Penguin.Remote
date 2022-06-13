using Penguin.Remote.Attributes;
using Penguin.Remote.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Remote.Commands
{
    public class Push : ServerCommand<PushResponse>
    {
        [SerializationData(Size = 255)]
        public string RemotePath { get; set; }

        public bool Overwrite { get; set; }
        public Push(string localPath, string remotePath, bool overwrite = false)
        {
            this.Payload = File.ReadAllBytes(localPath);

            this.RemotePath = remotePath;

            this.Overwrite = overwrite;
        }

        public Push()
        {

        }
    }
}
