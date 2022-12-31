namespace OriginalCircuit.BxlSharp.Types
{
    public class InstCopperpour : InstPoly
    {
        public string PourType { get; set; }
        public double PourSpacing { get; set; }
        public bool UseDesignRules { get; set; }
        public string ThermalType { get; set; }
        public double ThermalWidth { get; set; }
        public int ThermalSpokes { get; set; }
    }
}
