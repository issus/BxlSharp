namespace OriginalCircuit.BxlSharp.Types
{
    public class InstSymbol : InstItem
    {
        public double Rotate { get; set; }
        public string SymbolName { get; set; }
        public string RefDes { get; set; }
        public int GateNumber { get; set; }
        public bool IsFlipped { get; set; }

        #region Deserialization helpers
        internal double Rotated
        {
            get => Rotate;
            set => Rotate = value;
        }

        internal bool Flipped
        {
            get => IsFlipped;
            set => IsFlipped = value;
        }
        #endregion
    }
}
