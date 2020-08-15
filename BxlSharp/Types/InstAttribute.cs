using System.Collections.Generic;

namespace BxlSharp.Types
{
    public class InstAttribute : InstTextItem
    {
        public string Name { get; set; }
        public string RefDes { get; set; }
        public int GateNumber { get; set; }

        #region Deserialization helpers
        internal KeyValuePair<string, string> Attribute
        {
            get => new KeyValuePair<string, string>(Name, Text);
            set => (Name, Text) = (value.Key, value.Value);
        }

        internal KeyValuePair<string, string> AttrName
        {
            get => new KeyValuePair<string, string>(Name, Text);
            set => (Name, Text) = (value.Key, value.Value);
        }
        #endregion
    }
}