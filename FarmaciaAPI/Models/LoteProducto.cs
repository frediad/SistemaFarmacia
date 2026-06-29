namespace FarmaciaAPI.Models
{
    public class LoteProducto
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }

        public Producto? Producto { get; set; }

        public string NumeroLote { get; set; } = string.Empty;

        public DateTime FechaCaducidad { get; set; }

        public int Cantidad { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}