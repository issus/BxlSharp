using System.Collections.Generic;

namespace OriginalCircuit.BxlSharp.Types
{
    public class ComponentInstance
    {
        public string Designator { get; set; }
        public string CompName { get; set; }
        public Point Point { get; set; }
        public double Rotate { get; set; }
        public string PatternRef { get; set; }
        public List<InstAttribute> Attributes { get; } = new List<InstAttribute>();

        public ComponentInstance(string designator)
        {
            Designator = designator;
        }
    }
}
