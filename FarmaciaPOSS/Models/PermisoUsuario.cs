namespace FarmaciaPOS.Models
{
    public class PermisoUsuario
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }

        public string NombreModulo { get; set; } = string.Empty;

        public bool TieneAcceso { get; set; }
    }
}