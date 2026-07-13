using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FarmaciaPOS.Helpers
{
    public class ConfiguracionPos
    {
        public string ImpresoraTicket { get; set; } = "";
        public bool RespaldoAutomaticoActivo { get; set; } = false;
        public string RespaldoCarpeta { get; set; } = "";
        public int RespaldoIntervaloHoras { get; set; } = 24;
        public DateTime? UltimoRespaldo { get; set; }
        public string ModoConexion { get; set; } = "Local"; // "Local" | "Azure" | "Ambas"
    }

    public static class ConfiguracionPosHelper
    {
        private static readonly string RutaArchivo =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "posconfig.json");

        public static ConfiguracionPos Cargar()
        {
            if (!File.Exists(RutaArchivo))
                return new ConfiguracionPos();

            try
            {
                string json = File.ReadAllText(RutaArchivo);
                return JsonSerializer.Deserialize<ConfiguracionPos>(json) ?? new ConfiguracionPos();
            }
            catch
            {
                return new ConfiguracionPos();
            }
        }

        public static void Guardar(ConfiguracionPos config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RutaArchivo, json);
        }

        // ✅ Además, sincroniza el modo de conexión con appsettings.json,
        // que es lo que realmente usa DatabaseHelper para conectarse.
        public static void ActualizarModoConexionEnAppSettings(string modo)
        {
            string rutaAppSettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (!File.Exists(rutaAppSettings))
                throw new Exception("No se encontró appsettings.json en la carpeta de la aplicación.");

            string json = File.ReadAllText(rutaAppSettings);
            var nodo = JsonNode.Parse(json)!.AsObject();

            nodo["ModoConexion"] = modo;

            File.WriteAllText(
                rutaAppSettings,
                nodo.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}