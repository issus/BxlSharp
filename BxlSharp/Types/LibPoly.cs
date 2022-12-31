using System.Collections.Generic;

namespace OriginalCircuit.BxlSharp.Types
{
    public class LibPoly : LibItem
    {
        public string Property { get; set; }
        public double Width { get; set; }
        public List<Point> Points { get; } = new List<Point>();
    }
}
