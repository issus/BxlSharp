using System.Collections.Generic;

namespace BxlSharp.Types
{
    public class InstPoly : InstItem
    {
        public double Width { get; set; }
        public List<Point> Points { get; } = new List<Point>();

        #region Deserialization helpers
        internal List<Point> PPoint => Points;
        #endregion

    }
}
