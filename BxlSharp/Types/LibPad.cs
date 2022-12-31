namespace OriginalCircuit.BxlSharp.Types
{
    public class LibPad : LibItem
    {
        public int Number { get; set; }
        public string PinName { get; set; }
        public string PadStyle { get; set; }
        public string OriginalPadStyle { get; set; }
        public bool Mechanical { get; set; }
        public int OriginalPinNumber { get; set; }
        public double Rotate { get; set; }
    }

    public class LibDeletedPad : LibPad
    {

    }
}