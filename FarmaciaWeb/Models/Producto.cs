namespace FarmaciaWeb.Models
{
    public class Producto
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        public string Descripcion { get; set; } = "";

        public decimal PrecioVenta { get; set; }

        public int Stock { get; set; }

        public string ImagenURL { get; set; } = "";
    }
}
