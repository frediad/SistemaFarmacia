using Microsoft.Data.SqlClient;
using System;
using System.IO;

namespace FarmaciaPOS.Helpers
{
    public static class RespaldoHelper
    {
        // Solo funciona contra SQL Server LOCAL. Azure SQL no soporta BACKUP DATABASE por T-SQL.
        public static string EjecutarRespaldoLocal(string carpetaDestino)
        {
            if (DatabaseHelper.ObtenerCadenaConexionOrigenActual() == "Azure")
            {
                throw new Exception(
                    "No se puede hacer un respaldo manual mientras estás conectado a Azure SQL.\n\n" +
                    "Azure SQL no permite el comando BACKUP DATABASE. Azure ya genera respaldos automáticos " +
                    "propios (retención de hasta 35 días) que puedes configurar y restaurar desde el Portal de Azure, " +
                    "en la sección \"Copias de seguridad\" de tu base de datos.");
            }

            if (!Directory.Exists(carpetaDestino))
                Directory.CreateDirectory(carpetaDestino);

            string nombreArchivo = $"farmaciaDB_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ObtenerCadenaLocal());
            conn.Open();

            string query = $@"
                BACKUP DATABASE [farmaciaDB]
                TO DISK = @Ruta
                WITH INIT, COMPRESSION, STATS = 10";

            SqlCommand cmd = new SqlCommand(query, conn)
            {
                CommandTimeout = 300 // hasta 5 minutos para bases grandes
            };
            cmd.Parameters.AddWithValue("@Ruta", rutaCompleta);

            cmd.ExecuteNonQuery();

            return rutaCompleta;
        }
    }
}