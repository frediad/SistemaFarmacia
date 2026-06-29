namespace FarmaciaAPI.Models
{
    public class CompraRequest
    {
        public int ProductoId { get; set; }

        public int Cantidad { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;
    }
}