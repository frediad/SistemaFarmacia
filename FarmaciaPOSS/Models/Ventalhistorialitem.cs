using System;

namespace FarmaciaPOS.Models
{
    // Representa una fila del historial de ventas (tab "Historial de Ventas").
    public class VentaHistorialItem
    {
        public int Id { get; set; }
        public string Folio { get; set; } = "";
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string FechaTexto => Fecha.ToString("dd/MM/yyyy HH:mm");
        public int? ClienteId { get; set; }
        public string Cliente { get; set; } = "Público en general";
        public string Vendedor { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string MetodoPago { get; set; } = "";
        public string Estado { get; set; } = "";
        public bool EsCredito { get; set; }
    }
}