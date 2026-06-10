namespace FarmaciaPOS.Models
{
    public class Producto
    {
        public int Id { get; set; }

        public string CodigoBarras { get; set; } = "";

        public string Nombre { get; set; } = "";

        public string Descripcion { get; set; } = "";

        public int CategoriaId { get; set; }

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }

        public int Stock { get; set; }

        public int StockMinimo { get; set; }

        public DateTime? Caducidad { get; set; }

        public string ImagenURL { get; set; } = string.Empty;

        public bool EsMedicamentoControlado { get; set; }

        public bool Activo { get; set; }
    }
}