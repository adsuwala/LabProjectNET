namespace LabApp.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? PromoPrice { get; set; }
        public int Quantity { get; set; }
        public int AvailableStock { get; set; }

        public decimal EffectivePrice => PromoPrice ?? Price;
        public decimal Total => EffectivePrice * Quantity;
    }
}
