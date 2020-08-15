namespace BxlSharp.Types
{
    public class InstTextItem : InstItem
    {
        public bool IsVisible { get; set; }
        public string Text { get; set; }
        public double Rotate { get; set; }
        public bool IsFlipped { get; set; }
        public TextJustification Justify { get; set; } = TextJustification.Center;
        public string TextStyle { get; set; }

        #region Deserialization helpers
        internal bool Visible
        {
            get => IsVisible;
            set => IsVisible = value;
        }

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

        internal string TextStyleRef
        {
            get => TextStyle;
            set => TextStyle = value;
        }
        #endregion
    }
}