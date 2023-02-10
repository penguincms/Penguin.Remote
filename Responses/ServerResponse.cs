using Penguin.Remote.Attributes;
using Penguin.Remote.Commands;
using System;

namespace Penguin.Remote.Responses
{
    public class ServerResponse : TransmissionPackage
    {
        public virtual bool Success { get; set; } = true;

        [DontSerialize]
        public virtual Exception Exception => this.exception != null ? this.exception : !this.Success ? new Exception(this.Text) : null;

        [DontSerialize]
        private readonly Exception exception;
    }
}