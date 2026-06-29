namespace FarmaciaPOS.Models
{
    public class MovimientoInventarioView
    {
        public string ProductoNombre { get; set; }
            = string.Empty;

        public string TipoMovimiento { get; set; }
            = string.Empty;

        public int Cantidad { get; set; }

        public DateTime Fecha { get; set; }

        public string Motivo { get; set; }
            = string.Empty;
    }
}