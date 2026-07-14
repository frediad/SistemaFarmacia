using System;

namespace FarmaciaPOS.Models
{
    public class AbonoClienteView
    {
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public decimal SaldoDespues { get; set; }
    }
}