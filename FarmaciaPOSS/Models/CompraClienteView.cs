using System;

namespace FarmaciaPOS.Models
{
    public class CompraClienteView
    {
        public int VentaId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string TipoPago { get; set; } = string.Empty; // "Contado" / "Crédito"
    }
}