namespace FarmaciaPOS.Models
{
    public class DetallePedidoView
    {
        public string NombreProducto { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal Precio { get; set; }

        public decimal Subtotal { get; set; }
    }
}