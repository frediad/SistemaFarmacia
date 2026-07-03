namespace FarmaciaPOS.Models
{
    public class ProductoStockItem
    {
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
        public int StockMinimo { get; set; }
    }
}