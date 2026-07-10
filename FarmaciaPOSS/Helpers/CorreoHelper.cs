using System;
using System.Diagnostics;

namespace FarmaciaPOS.Helpers
{
    public static class CorreoHelper
    {
        // Abre el cliente de correo predeterminado (Outlook, Gmail vía navegador si está configurado, etc.)
        // con el mensaje ya redactado, para que el usuario lo revise y presione Enviar.
        public static void AbrirCorreoPedido(string destinatario, string asunto, string cuerpo)
        {
            if (string.IsNullOrWhiteSpace(destinatario))
                throw new Exception("El proveedor no tiene un correo electrónico registrado.");

            try
            {
                string cuerpoNormalizado = cuerpo.Replace("\r\n", "\n").Replace("\n", "\r\n");

                string mailto =
                    $"mailto:{Uri.EscapeDataString(destinatario)}" +
                    $"?subject={Uri.EscapeDataString(asunto)}" +
                    $"&body={Uri.EscapeDataString(cuerpoNormalizado)}";

                var psi = new ProcessStartInfo(mailto)
                {
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "No se pudo abrir el cliente de correo. Verifica que tengas una aplicación de correo " +
                    "predeterminada configurada en Windows (Outlook, etc.).", ex);
            }
        }
    }
}