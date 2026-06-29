using System;

namespace FarmaciaPOS.Models
{
    public class MovimientoCaja
    {
        public int Id { get; set; }

        public int CajaId { get; set; }

        public string TipoMovimiento { get; set; } =
            string.Empty;

        public decimal Monto { get; set; }

        public string Motivo { get; set; } =
            string.Empty;

        public DateTime Fecha { get; set; }
    }
}