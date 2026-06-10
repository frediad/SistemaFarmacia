namespace FarmaciaAPI.Models
{
    public class Venta
    {
        public int Id { get; set; }

        public string Folio { get; set; } = string.Empty;

        public int? ClienteId { get; set; }

        public Cliente? Cliente { get; set; }

        public int UsuarioId { get; set; }

        public Usuario? Usuario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public decimal Subtotal { get; set; }

        public decimal IVA { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public string Estado { get; set; } = "Completada";
    }
}