using System;

namespace FarmaciaPOS.Models
{
    public class PedidoProveedorView
    {
        public int Id { get; set; }
        public string Proveedor { get; set; } = "";
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = "";
    }
}