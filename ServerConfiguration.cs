using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Remote
{
    public class ServerConfiguration
    {
        public bool IsConfigured { get; set; }
        public int Port { get; set; }
        public string Key { get; set; }
        public string Host { get; set; }
    }
}
