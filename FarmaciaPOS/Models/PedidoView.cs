namespace FarmaciaPOS.Models
{
    public class PedidoView
    {
        public int Id { get; set; }

        public string NumeroPedido { get; set; } = "";

        public string ClienteNombre { get; set; } = "";

        public DateTime FechaPedido { get; set; }

        public decimal Total { get; set; }

        public string EstadoPedido { get; set; } = "";

        public string HoraRecogida { get; set; } = "";

        public string Observaciones { get; set; } = "";
    }
}