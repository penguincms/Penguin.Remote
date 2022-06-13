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
