using System;
using System.Collections.Generic;

namespace OriginalCircuit.BxlSharp.Types
{
    public class NetInstance
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public List<NetNode> Nodes { get; } = new List<NetNode>();

        public NetInstance(string name)
        {
            Name = name;
        }
    }

    public class NetNode : Tuple<string, string>
    {
        public NetNode(string item1, string item2) : base(item1, item2)
        {

        }
    }
}