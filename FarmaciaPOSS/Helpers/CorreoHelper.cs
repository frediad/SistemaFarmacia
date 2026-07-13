using System;
using System.Diagnostics;
using System.Web;

namespace FarmaciaPOS.Helpers
{
    public static class CorreoHelper
    {
        // =========================================
        // ✅ ABRIR GMAIL EN EL NAVEGADOR CON EL
        //    CORREO YA REDACTADO
        // =========================================

        public static void AbrirCorreoPedido(
            string destinatario,
            string asunto,
            string cuerpo)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
                throw new Exception("El proveedor no tiene correo registrado.");

            // ✅ Construye la URL de Gmail con los parámetros codificados
            string urlGmail =
                "https://mail.google.com/mail/?view=cm" +
                "&fs=1" +
                $"&to={Uri.EscapeDataString(destinatario)}" +
                $"&su={Uri.EscapeDataString(asunto)}" +
                $"&body={Uri.EscapeDataString(cuerpo)}";

            // ✅ Abre la URL en el navegador predeterminado
            Process.Start(new ProcessStartInfo
            {
                FileName = urlGmail,
                UseShellExecute = true
            });
        }
    }
}