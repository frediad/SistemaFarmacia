namespace FarmaciaAPI.Models
{
    public class Compra
    {
        public int Id { get; set; }

        public int ProveedorId { get; set; }

        public Proveedor? Proveedor { get; set; }

        public int UsuarioId { get; set; }

        public Usuario? Usuario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public decimal Total { get; set; }

        public string Estado { get; set; } = "Completada";
    }
}
