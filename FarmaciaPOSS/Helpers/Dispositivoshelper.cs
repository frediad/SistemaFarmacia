using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace FarmaciaPOS.Helpers
{
    // =========================================
    // Resultado de verificar una impresora puntual
    // =========================================
    public class EstadoImpresora
    {
        public bool Encontrada { get; set; }
        public bool EnLinea { get; set; }
        public bool TieneError { get; set; }
        public string MensajeEstado { get; set; } = "";
    }

    // =========================================
    // Un dispositivo Plug and Play con problema de controlador
    // =========================================
    public class DispositivoConProblema
    {
        public string Nombre { get; set; } = "";
        public string Clase { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string DescripcionError { get; set; } = "";
    }

    public static class DispositivosHelper
    {
        // =========================================
        // ✅ VERIFICAR ESTADO DE UNA IMPRESORA (conectada / fuera de línea / error)
        // =========================================

        public static EstadoImpresora ObtenerEstadoImpresora(string nombreImpresora)
        {
            var resultado = new EstadoImpresora();

            if (string.IsNullOrWhiteSpace(nombreImpresora))
            {
                resultado.Encontrada = false;
                resultado.MensajeEstado = "No hay impresora seleccionada.";
                return resultado;
            }

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Printer WHERE Name = '{nombreImpresora.Replace("'", "''")}'");

                foreach (ManagementObject impresora in searcher.Get())
                {
                    resultado.Encontrada = true;

                    bool workOffline = Convert.ToBoolean(impresora["WorkOffline"] ?? false);
                    uint estadoPrinter = Convert.ToUInt32(impresora["PrinterStatus"] ?? 0u);
                    string? extendedStatus = impresora["ExtendedDetectedErrorState"]?.ToString();

                    // PrinterStatus: 3 = Idle (lista), 4 = Printing, otros valores suelen indicar problema
                    resultado.EnLinea = !workOffline && (estadoPrinter == 3 || estadoPrinter == 4);
                    resultado.TieneError = workOffline || (estadoPrinter != 3 && estadoPrinter != 4);

                    resultado.MensajeEstado = resultado.EnLinea
                        ? "Conectada y lista para imprimir."
                        : workOffline
                            ? "La impresora está configurada como \"Fuera de línea\". Revisa el cable/USB o la conexión de red."
                            : $"La impresora reporta un estado inusual (código {estadoPrinter}). Puede estar apagada, sin papel, o con un problema de controlador.";

                    return resultado;
                }

                // No se encontró en WMI: probablemente el driver ni siquiera se instaló
                resultado.Encontrada = false;
                resultado.TieneError = true;
                resultado.MensajeEstado = "No se encontró información de esta impresora en el sistema. Es posible que el controlador no esté instalado.";
            }
            catch (Exception ex)
            {
                resultado.Encontrada = false;
                resultado.TieneError = true;
                resultado.MensajeEstado = $"No se pudo consultar el estado: {ex.Message}";
            }

            return resultado;
        }

        // =========================================
        // ✅ DISPOSITIVOS CON PROBLEMA DE CONTROLADOR (impresoras, USB, puertos, HID)
        // =========================================

        // Busca dispositivos Plug and Play relevantes para un POS (impresoras,
        // lectores de código de barras, cajones de dinero por USB/serial) que
        // Windows marca con un código de error de administrador de dispositivos
        // (ConfigManagerErrorCode distinto de 0 = "controlador con problema").
        public static List<DispositivoConProblema> ObtenerDispositivosConProblemas()
        {
            var lista = new List<DispositivoConProblema>();

            // Clases PnP relevantes para periféricos de punto de venta.
            // (Se filtra así para no saturar al usuario con dispositivos
            // internos del equipo que no le interesan, como tarjetas de red.)
            var clasesRelevantes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Printer", "USB", "Ports", "HIDClass", "USBDevice"
            };

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, PNPClass, DeviceID, ConfigManagerErrorCode " +
                    "FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0");

                foreach (ManagementObject dispositivo in searcher.Get())
                {
                    string clase = dispositivo["PNPClass"]?.ToString() ?? "";

                    if (!clasesRelevantes.Contains(clase))
                        continue;

                    uint codigoError = Convert.ToUInt32(dispositivo["ConfigManagerErrorCode"] ?? 0u);

                    lista.Add(new DispositivoConProblema
                    {
                        Nombre = dispositivo["Name"]?.ToString() ?? "Dispositivo desconocido",
                        Clase = clase,
                        DeviceId = dispositivo["DeviceID"]?.ToString() ?? "",
                        DescripcionError = DescribirCodigoError(codigoError)
                    });
                }
            }
            catch
            {
                // Si WMI falla (permisos, versión de Windows, etc.) simplemente
                // no mostramos la lista — no es crítico para el resto del módulo.
            }

            return lista;
        }

        // Traduce los códigos más comunes de "Configuration Manager Error Code"
        // de Windows a un mensaje entendible para el usuario final.
        private static string DescribirCodigoError(uint codigo)
        {
            return codigo switch
            {
                1 => "El dispositivo no está configurado correctamente.",
                10 => "El dispositivo no pudo iniciarse.",
                18 => "Es necesario reinstalar los controladores de este dispositivo.",
                22 => "El dispositivo está deshabilitado.",
                28 => "Los controladores de este dispositivo no están instalados.",
                31 => "Windows no pudo cargar los controladores para este dispositivo.",
                39 => "El controlador puede estar dañado o faltante.",
                _ => "Este dispositivo tiene un problema de controlador (Windows lo reporta con errores)."
            };
        }

        // =========================================
        // ✅ ABRIR HERRAMIENTAS NATIVAS DE WINDOWS PARA INSTALAR/ACTUALIZAR CONTROLADORES
        // =========================================

        // Windows no permite que apps de terceros instalen controladores de forma
        // silenciosa (por seguridad), pero sí podemos abrir directamente el
        // asistente correcto para que el usuario lo haga en un par de clics.

        // Asistente nativo de Windows para agregar/instalar una impresora
        // (permite elegir fabricante/modelo o buscar por Windows Update).
        public static void AbrirAsistenteInstalarImpresora()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = "printui.dll,PrintUIEntry /il",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception("No se pudo abrir el asistente de instalación de impresoras: " + ex.Message);
            }
        }

        // Administrador de dispositivos de Windows, para que el usuario ubique
        // el dispositivo con problema y elija "Actualizar controlador" manualmente.
        public static void AbrirAdministradorDispositivos()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "devmgmt.msc",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception("No se pudo abrir el Administrador de dispositivos: " + ex.Message);
            }
        }

        // Panel de "Impresoras y escáneres" de Windows, útil para que el usuario
        // vincule un dispositivo nuevo (USB, Bluetooth o red) desde cero.
        public static void AbrirConfiguracionImpresorasWindows()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:printers",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception("No se pudo abrir la configuración de impresoras de Windows: " + ex.Message);
            }
        }

        // Windows Update, donde suelen aparecer controladores opcionales para
        // impresoras y periféricos comunes cuando Windows los detecta.
        public static void AbrirWindowsUpdate()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:windowsupdate",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception("No se pudo abrir Windows Update: " + ex.Message);
            }
        }
    }
}