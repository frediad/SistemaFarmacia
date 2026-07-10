using System;


namespace FarmaciaPOS.Models
{
    internal class ReportePedidoItem
    {
        public int Id { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string NumeroPedido { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime Fecha { get; set; }

    }
}
