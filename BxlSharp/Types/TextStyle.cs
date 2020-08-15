namespace BxlSharp.Types
{
    public class TextStyle
    {
        public string Name { get; set; }
        public double FontWidth { get; set; }
        public double FontHeight { get; set; }
        public double? FontCharWidth { get; set; }
        public string FontFamily { get; set; }
        public string FontFace { get; set; }

        public TextStyle(string name)
        {
            Name = name;
        }
    }
}
