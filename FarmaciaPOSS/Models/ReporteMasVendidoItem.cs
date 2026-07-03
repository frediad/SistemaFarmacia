namespace FarmaciaPOS.Models
{
    public class ReporteMasVendidoItem
    {
        public int Posicion { get; set; }
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }
}