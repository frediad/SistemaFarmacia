using System;

namespace FarmaciaPOS.Models
{
    internal class VentaResumenView
    {
        public int Id { get; set; }
        public string Folio { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = string.Empty;
    }
}
