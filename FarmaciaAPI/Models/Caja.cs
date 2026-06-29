using System;

namespace FarmaciaPOS.Models
{
    public class Caja
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public DateTime FechaApertura { get; set; }

        public DateTime? FechaCierre { get; set; }

        public decimal MontoInicial { get; set; }

        public decimal? MontoFinal { get; set; }

        public decimal? TotalVentas { get; set; }

        public decimal? Diferencia { get; set; }

        public string Estado { get; set; } =
            string.Empty;
    }
}