namespace FarmaciaPOS.Helpers
{
    public static class Sesion
    {
        public static int UsuarioId { get; set; }

        public static string NombreUsuario { get; set; } =
            string.Empty;

        public static int RolId { get; set; }
    }
}