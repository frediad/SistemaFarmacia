using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

namespace FarmaciaPOS.Helpers
{
    public static class DatabaseHelper
    {
        private static IConfigurationRoot configuration;

        static DatabaseHelper()
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        // Cache del último modo resuelto en "Ambas", para no probar la conexión
        // en cada consulta (sería muy lento). Se recalcula una vez por sesión de la app,
        // o al llamar ForzarReevaluacion().
        private static string? _cacheConexionResuelta;

        public static void ForzarReevaluacion()
        {
            _cacheConexionResuelta = null;

            configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string ConnectionString
        {
            get
            {
                string modo = configuration["ModoConexion"] ?? MapearBaseDatosActivaAModo();

                switch (modo)
                {
                    case "Azure":
                        return configuration.GetConnectionString("AzureConnection");

                    case "Ambas":
                        if (_cacheConexionResuelta != null)
                            return _cacheConexionResuelta;

                        string local = configuration.GetConnectionString("LocalConnection");
                        string azure = configuration.GetConnectionString("AzureConnection");

                        _cacheConexionResuelta = ProbarConexion(local, timeoutSegundos: 3)
                            ? local
                            : azure;

                        return _cacheConexionResuelta;

                    case "Local":
                    default:
                        return configuration.GetConnectionString("LocalConnection");
                }
            }
        }

        // Compatibilidad con la clave anterior "BaseDatosActiva", por si ModoConexion
        // aún no existe en appsettings.json.
        private static string MapearBaseDatosActivaAModo()
        {
            string baseDatosActiva = configuration["BaseDatosActiva"];

            return baseDatosActiva switch
            {
                "AzureConnection" => "Azure",
                "LocalConnection" => "Local",
                _ => "Local"
            };
        }

        public static string ObtenerModoActual()
        {
            return configuration["ModoConexion"] ?? MapearBaseDatosActivaAModo();
        }

        public static string ObtenerCadenaConexionOrigenActual()
        {
            return ConnectionString == configuration.GetConnectionString("AzureConnection")
                ? "Azure"
                : "Local";
        }

        // ✅ Prueba si una cadena de conexión específica funciona, con timeout corto.
        public static bool ProbarConexion(string connectionString, int timeoutSegundos = 5)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    ConnectTimeout = timeoutSegundos
                };

                using var conn = new SqlConnection(builder.ConnectionString);
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ObtenerCadenaLocal() => configuration.GetConnectionString("LocalConnection");
        public static string ObtenerCadenaAzure() => configuration.GetConnectionString("AzureConnection");
    }
}