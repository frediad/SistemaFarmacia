using System;

namespace FarmaciaPOS.Models
{
    public class Clientes
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public decimal LimiteCredito { get; set; }
        public decimal SaldoActual { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }

        public decimal CreditoDisponible => LimiteCredito - SaldoActual;
    }
}