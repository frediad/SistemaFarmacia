using Microsoft.Data.SqlClient;
using System;
using System.Windows;

namespace FarmaciaPOS.Helpers
{
    public static class PermisosHelper
    {
        // =========================================
        // ✅ VERIFICAR ACCESO A UN MÓDULO
        // =========================================

        public static bool TieneAcceso(string nombreModulo)
        {
            // Administrador siempre tiene acceso total
            if (Sesion.RolId == 1)
                return true;

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT TieneAcceso
              FROM PermisosUsuario
              WHERE UsuarioId = @UsuarioId
              AND NombreModulo = @NombreModulo";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
            cmd.Parameters.AddWithValue("@NombreModulo", nombreModulo);

            var resultado = cmd.ExecuteScalar();

            if (resultado == null)
                return false;

            return Convert.ToBoolean(resultado);
        }

        // =========================================
        // ✅ MOSTRAR MENSAJE FLOTANTE DE ACCESO DENEGADO
        // =========================================

        public static void MostrarAccesoDenegado()
        {
            MessageBox.Show(
                "No tienes acceso a este módulo.\nDebes ser administrador o solicitar permiso de acceso.",
                "Acceso Restringido",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}