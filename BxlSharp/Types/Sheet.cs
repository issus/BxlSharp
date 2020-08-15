using System.Collections.Generic;

namespace BxlSharp.Types
{
    public class Sheet
    {
        public string Name { get; set; }
        public int Number { get; set; }
        public bool ShowBorder { get; set; }
        public string BorderName { get; set; }
        public double ScaleFactor { get; set; }
        public Point OffSet { get; set; }
        public List<InstItem> Data { get; } = new List<InstItem>();
    }
}