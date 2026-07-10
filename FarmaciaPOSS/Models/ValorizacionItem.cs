namespace FarmaciaPOS.Models
{
    public class ValorizacionItem
    {
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }

        public decimal ValorCosto => Stock * PrecioCompra;
        public decimal ValorVenta => Stock * PrecioVenta;
        public decimal GananciaPotencial => ValorVenta - ValorCosto;
    }
}