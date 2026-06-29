namespace FarmaciaPOS.Models
{
    public class VentaItem
    {
        public int ProductoId { get; set; }

        public string Nombre { get; set; } =
            string.Empty;

        public decimal Precio { get; set; }

        public int Cantidad { get; set; }

        // ⭐ AGREGAR ESTO
        public decimal Descuento { get; set; }

        // ⭐ SUBTOTAL
        public decimal Subtotal
        {
            get
            {
                return
                    (Precio * Cantidad)
                    - Descuento;
            }
        }
    }
}