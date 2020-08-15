using System;
using System.Collections.Generic;
using System.Linq;

namespace BxlSharp.Types
{
    public class Symbol
    {
        public string Name { get; set; }
        public Point OriginPoint { get; set; }
        public string OriginalName { get; set; }
        public bool Edited { get; set; }
        public List<LibItem> Data { get; } = new List<LibItem>();

        public Symbol(string name)
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
