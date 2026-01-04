namespace MiAllScaleTools.Domain
{
    public sealed class Good
    {
        public string Name { get; set; } = "";
        public string Barcode { get; set; } = "";
        public decimal Price { get; set; }

        public override string ToString()
        {
            return $"名称：{Name}, 条码：{Barcode}，价格：{Price}";
        }
    }
}