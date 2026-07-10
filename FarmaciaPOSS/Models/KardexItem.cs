using System;

namespace FarmaciaPOS.Models
{
    public class KardexItem
    {
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public int Saldo { get; set; }
    }
}