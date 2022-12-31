namespace OriginalCircuit.BxlSharp.Types
{
    public class LibPin : LibItem
    {
        public int PinNum { get; set; }
        public double PinLength { get; set; }
        public bool IsFlipped { get; set; }
        public bool IsVisible { get; set; }
        public double Rotate { get; set; }
        public double Width { get; set; }
        public PinType PinType { get; set; }
        public DataPinParameter Designator { get; } = new DataPinParameter();
        public DataPinParameter Name { get; } = new DataPinParameter();
    }

    public class DataPinParameter : LibTextItem
    {
    }

    public enum PinType
    {
        None,
        Input,
        Output,
        BiDirectional,
        Tristate,
        OpenCollector,
        OpenEmitter,
        Power,
        Ground,
        Analog,
        Behaviour,
        Any,
        Digital,
        NoConnect,
        Passive
    }
}