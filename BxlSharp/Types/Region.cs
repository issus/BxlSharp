namespace BxlSharp.Types
{
    public class Region
    {
        public Point LowerLeft { get; set; }
        public Point UpperRight { get; set; }

        #region Deserialization helpers
        internal Point LL
        {
            get => LowerLeft;
            set => LowerLeft = value;
        }

        internal Point UR
        {
            get => UpperRight;
            set => UpperRight = value;
        }
        #endregion
    }
}
