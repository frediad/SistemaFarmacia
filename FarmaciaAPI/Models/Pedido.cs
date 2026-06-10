namespace FarmaciaAPI.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        public string NumeroPedido { get; set; } = string.Empty;

        public int ClienteId { get; set; }

        public Cliente? Cliente { get; set; }

        public DateTime FechaPedido { get; set; } = DateTime.Now;

        public string EstadoPedido { get; set; } = "Pendiente";

        public decimal Total { get; set; }

        public string HoraRecogida { get; set; } = string.Empty;

        public string Observaciones { get; set; } = string.Empty;
    }
}
