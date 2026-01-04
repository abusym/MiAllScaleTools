using System;

namespace MiAllScaleTools.Models
{
    public sealed class SyncRow
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Barcode { get; set; } = "";
        public decimal Price { get; set; }
        public bool Succeeded { get; set; }
        public string Result { get; set; } = "";
        public DateTime Time { get; set; } = DateTime.Now;
    }
}