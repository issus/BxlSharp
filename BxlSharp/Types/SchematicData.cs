using System.Collections.Generic;

namespace OriginalCircuit.BxlSharp.Types
{
    public class SchematicData
    {
        public string Units { get; set; } = "mil";
        public Workspace Workspace { get; } = new Workspace();
        public List<KeyValuePair<string, string>> Attributes { get; } = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<int, string>> Sheets { get; } = new List<KeyValuePair<int, string>>();
    }

    public class Workspace : Region
    {
        public double Grid { get; set; }
    }
}