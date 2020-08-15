using System.Collections.Generic;

namespace BxlSharp.Types
{
    public class PadStack
    {
        public string Name { get; set; }
        public double HoleDiam { get; set; }
        public bool Surface { get; set; }
        public bool Plated { get; set; }
        public bool NoPaste { get; set; }
        public int StartRange { get; set; }
        public int EndRange { get; set; }
        public bool IsVia { get; set; }
        public List<PadShape> Shapes { get; } = new List<PadShape>();

        public PadStack(string name)
        {
            Name = name;
        }
    }

    public class PadShape
    {
        public PadShapeKind Kind { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int PadType { get; set; }
        public string Layer { get; set; }
    }

    public enum PadShapeKind
    {
        Round,
        Circle = 0,
        Square,
        Oblong,
        Rectangle,
        Polygon,
        Thermal,
        ThermalX
    }
}
