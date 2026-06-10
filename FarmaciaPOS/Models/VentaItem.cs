namespace FarmaciaPOS.Models
{
    public class VentaItem
    {
        public int ProductoId { get; set; }

        public string Nombre { get; set; }
            = string.Empty;

        public decimal Precio { get; set; }

        public int Cantidad { get; set; }

        public decimal Subtotal
        {
            get
            {
                return Precio * Cantidad;
            }
        }
    }
}