namespace FarmaciaAPI.Models
{
    public class Producto
    {
        public int Id { get; set; }

        public string CodigoBarras { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }
        public decimal Precio2 { get; set; }
        public decimal Precio3 { get; set; }

        public int CantidadMayoreo2 { get; set; }

        public int CantidadMayoreo3 { get; set; }

        public int Stock { get; set; }

        public int StockMinimo { get; set; }

        public int? SubCategoriaId { get; set; }

        public DateTime? Caducidad { get; set; }

        public string ImagenURL { get; set; } = string.Empty;


        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}