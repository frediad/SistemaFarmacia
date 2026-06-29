namespace FarmaciaPOS.Models
{
    public class ReporteVentaItem
    {
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }

    public class ReporteResumen
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal TotalVentas { get; set; }
        public int NumeroVentas { get; set; }
        public List<ReporteVentaItem> Productos { get; set; } = new();
        public List<ReporteVentaPorDia> VentasPorDia { get; set; } = new();
    }

    public class ReporteVentaPorDia
    {
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
    }
}