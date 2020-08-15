using System.Collections.Generic;

namespace BxlSharp.Types
{
    public class LayerNumber
    {
        public int Id { get; set; }
        public string LayerName { get; set; }
        public int LayerNum { get; set; }
        public LayerType LayerType { get; set; }
        public int OrderNumber { get; set; }
        public List<InstItem> Data { get; } = new List<InstItem>();
    }
}
