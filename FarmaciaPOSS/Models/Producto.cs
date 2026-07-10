using System;

namespace FarmaciaPOS.Models
{
    public class Producto
    {
        public int Id { get; set; }

        public string CodigoBarras { get; set; } =
            string.Empty;

        public string Nombre { get; set; } =
            string.Empty;

        public string Descripcion { get; set; } = "";

        public int CategoriaId { get; set; }

        public int? SubcategoriaId { get; set; }

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }

        
        public decimal Precio1 => PrecioVenta;

      
        public decimal Precio2 { get; set; }
        public int CantidadMayoreo2 { get; set; }

        public decimal Precio3 { get; set; }
        public int CantidadMayoreo3 { get; set; }

        public decimal ObtenerPrecioPorCantidad(int cantidad)
        {
            if (Precio3 > 0 && CantidadMayoreo3 > 0 && cantidad >= CantidadMayoreo3)
                return Precio3;

            if (Precio2 > 0 && CantidadMayoreo2 > 0 && cantidad >= CantidadMayoreo2)
                return Precio2;

            return Precio1;
        }

        public int Stock { get; set; }

        public int StockMinimo { get; set; }

        public DateTime? Caducidad { get; set; }

        public string ImagenURL { get; set; } = string.Empty;

        public bool Activo { get; set; }
    }
}