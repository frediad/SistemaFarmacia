using Microsoft.Data.SqlClient;

namespace FarmaciaPOS.Helpers
{
    public static class UsuarioSetupHelper
    {
        // Verifica si ya existe un usuario Administrador en el sistema.
        // Si no existe ninguno, el login debe mostrar la opción de crear el primero.
        public static bool ExisteAdministrador()
        {
            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT COUNT(*)
            FROM Usuarios U
            INNER JOIN Roles R ON U.RolId = R.Id
            WHERE R.Nombre = 'Administrador'
            AND U.Activo = 1";

            SqlCommand cmd = new SqlCommand(query, conn);

            int total = (int)cmd.ExecuteScalar();
            return total > 0;
        }
    }
}