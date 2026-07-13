using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;

namespace FarmaciaPOS.Helpers
{
    public static class ImpresoraTicketHelper
    {
        public static List<string> ObtenerImpresorasInstaladas()
        {
            var lista = new List<string>();

            foreach (string nombre in PrinterSettings.InstalledPrinters)
                lista.Add(nombre);

            return lista;
        }

        public static bool ImpresoraExiste(string nombre)
        {
            foreach (string instalada in PrinterSettings.InstalledPrinters)
            {
                if (instalada.Equals(nombre, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        // Envía un ticket de prueba simple a la impresora indicada.
        public static void ImprimirTicketPrueba(string nombreImpresora)
        {
            if (!ImpresoraExiste(nombreImpresora))
                throw new Exception($"La impresora \"{nombreImpresora}\" ya no está disponible en este equipo.");

            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = nombreImpresora;

            if (!doc.PrinterSettings.IsValid)
                throw new Exception($"La impresora \"{nombreImpresora}\" no es válida o no responde.");

            doc.PrintPage += (sender, e) =>
            {
                var font = new Font("Consolas", 10);
                var fontTitulo = new Font("Consolas", 12, FontStyle.Bold);
                float y = 10;

                e.Graphics.DrawString("FarmaClick Yatzil", fontTitulo, Brushes.Black, 10, y);
                y += 25;
                e.Graphics.DrawString("=== TICKET DE PRUEBA ===", font, Brushes.Black, 10, y);
                y += 20;
                e.Graphics.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", font, Brushes.Black, 10, y);
                y += 20;
                e.Graphics.DrawString("Impresora configurada correctamente ✔", font, Brushes.Black, 10, y);
                y += 20;
                e.Graphics.DrawString("------------------------------", font, Brushes.Black, 10, y);
            };

            doc.Print();
        }
    }
}