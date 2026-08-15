namespace FarmaciaPOS.Models
{
    // Representa la fila de la tabla ConfiguracionTicket, con valores por
    // defecto razonables por si la tabla está vacía o algún campo no se llenó.
    public class ConfiguracionTicketData
    {
        public string NombreNegocio { get; set; } = "FarmaClick Yatzil";
        public string RFC { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Correo { get; set; } = "";
        public string MensajeTicket { get; set; } = "¡Gracias por su compra!";
    }
}