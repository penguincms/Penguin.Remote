﻿using Penguin.Remote.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penguin.Remote.Responses
{
    public class EchoResponse : ServerResponse
    {
        public override bool Success => true;
    }
}
