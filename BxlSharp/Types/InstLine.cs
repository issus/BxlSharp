namespace BxlSharp.Types
{
    public class InstLine : InstItem
    {
        public Point Point1 { get; set; }
        public double Width { get; set; }

        #region Deserialization helpers
        internal Point Point2
        {
            get => Point1;
            set => Point1 = value;
        }
        #endregion
    }
}
