using System.Collections.Generic;

namespace OriginalCircuit.BxlSharp.Types
{
    public class LibAttribute : LibTextItem
    {
        public int Number { get; set; }
        public string Name { get; set; }

        #region Deserialization helpers
        internal KeyValuePair<string, string> Attr
        {
            get => new KeyValuePair<string, string>(Name, Text);
            set => (Name, Text) = (value.Key, value.Value);
        }
        #endregion
    }
}
