using System;
using System.Collections.Generic;
using System.Linq;

namespace OriginalCircuit.BxlSharp.Types
{
    public class Pattern
    {
        public string Name { get; set; }
        public Point OriginPoint { get; set; }
        public Point PickPoint { get; set; }
        public Point GluePoint { get; set; }
        public bool PinsRenamed { get; internal set; }
        public List<LibItem> Data { get; } = new List<LibItem>();

        public Pattern(string name)
        {
            Name = name;
        }

        public LibAttribute GetAttribute(string name)
        {
            return Data.OfType<LibAttribute>()
                .FirstOrDefault(a => a.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) == true);
        }
    }
}
