namespace OriginalCircuit.BxlSharp.Types
{
    public abstract class LibTextItem : LibItem
    {
        public bool IsVisible { get; set; }
        public string Text { get; set; }
        public double Rotate { get; set; }
        public bool IsFlipped { get; set; }
        public TextJustification Justify { get; set; } = TextJustification.Center;
        public string TextStyle { get; set; }

        #region Deserialization helpers
        internal string TextStyleRef
        {
            get => TextStyle;
            set => TextStyle = value;
        }
        #endregion
    }
}
