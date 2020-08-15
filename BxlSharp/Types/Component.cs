using System;
using System.Collections.Generic;
using System.Linq;

namespace BxlSharp.Types
{
    public class Component
    {
        public string Name { get; set; }
        public string PatternName { get; internal set; }
        public List<string> AlternatePatterns { get; } = new List<string>();
        public string OriginalName { get; set; }
        public string SourceLibrary { get; set; }
        public string RefDesPrefix { get; set; }
        public string Composition { get; set; }
        public bool AltIeee { get; set; }
        public bool AltDeMorgan { get; set; }
        public string RevisionLevel { get; set; }
        public string RevisionNote { get; set; }
        public List<CompPin> Pins { get; } = new List<CompPin>();
        public List<LibItem> Data { get; } = new List<LibItem>();
        public List<RelatedFile> RelatedFiles { get; } = new List<RelatedFile>();
        public List<AttachedSymbol> AttachedSymbols { get; } = new List<AttachedSymbol>();
        public List<PadNum> PinMap { get; } = new List<PadNum>();
        public int NumberOfPins => Pins.Count;
        public int NumParts => Math.Max(Pins.Max(p => p.PartNum), AttachedSymbols.Max(p => p.PartNum));

        public Component(string name)
        {
            Name = name;
        }

        public LibAttribute GetAttribute(string name)
        {
            return Data.OfType<LibAttribute>()
                .FirstOrDefault(a => a.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) == true);
        }
    }

    public class CompPin
    {
        public string Descriptor { get; set; }
        public string Name { get; set; }
        public int PartNum { get; set; }
        public int SymPinNum { get; set; }
        public int GateEq { get; set; }
        public int PinEq { get; set; }
        public PinType PinType { get; set; }
        public string Side { get; set; }
        public int Group { get; set; }
        public string InnerGraphic { get; set; }
        public string OuterGraphic { get; set; }

        public CompPin(string descriptor, string name)
        {
            Descriptor = descriptor;
            Name = name;
        }
    }

    public class RelatedFile
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string Path { get; set; }
    }

    public class AttachedSymbol
    {
        public int PartNum { get; set; }
        public string AltType { get; set; }
        public string SymbolName { get; set; }
    }

    public class PadNum
    {
        public int Number { get; set; }
        public string CompPinRef { get; set; }

        public PadNum(int number)
        {
            Number = number;
        }
    }
}
