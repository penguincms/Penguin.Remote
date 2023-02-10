using System;

namespace Penguin.Remote.Attributes
{
    public class SerializationData : Attribute
    {
        public int Order { get; set; }

        public int Size { get; set; }

        public SerializationData(int order = 0, int size = 0)
        {
            this.Order = order;
            this.Size = size;
        }
    }
}