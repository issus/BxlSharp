namespace OriginalCircuit.BxlSharp.Types
{
    public class Layer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LayerType LayerType { get; set; }
        public string BoardLayerType { get; set; }
        public int LayerOrder { get; set; }
    }

    public enum LayerType
    {
        NonSignal,
        Signal,
        Plane
    }
}