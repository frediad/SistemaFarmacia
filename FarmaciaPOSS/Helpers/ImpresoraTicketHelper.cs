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

        // Imprime el ticket real de una venta ya registrada en la BD.
        public static void ImprimirTicketVenta(
            string nombreImpresora,
            string folio,
            string nombreUsuario,
            IEnumerable<FarmaciaPOS.Models.VentaItem> items,
            decimal subtotal,
            decimal total,
            decimal pago,
            decimal cambio)
        {
            if (string.IsNullOrWhiteSpace(nombreImpresora))
                throw new Exception("No hay ninguna impresora de tickets configurada. Ve a Configuración para asignar una.");

            if (!ImpresoraExiste(nombreImpresora))
                throw new Exception($"La impresora \"{nombreImpresora}\" ya no está disponible en este equipo.");

            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = nombreImpresora;

            if (!doc.PrinterSettings.IsValid)
                throw new Exception($"La impresora \"{nombreImpresora}\" no es válida o no responde.");

            var listaItems = new List<FarmaciaPOS.Models.VentaItem>(items);

            doc.PrintPage += (sender, e) =>
            {
                var fontTitulo = new Font("Consolas", 12, FontStyle.Bold);
                var font = new Font("Consolas", 9);
                var fontBold = new Font("Consolas", 9, FontStyle.Bold);
                float y = 10;
                float x = 10;

                e.Graphics.DrawString("FarmaClick Yatzil", fontTitulo, Brushes.Black, x, y);
                y += 22;
                e.Graphics.DrawString($"Folio: {folio}", font, Brushes.Black, x, y);
                y += 16;
                e.Graphics.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", font, Brushes.Black, x, y);
                y += 16;
                e.Graphics.DrawString($"Atendió: {nombreUsuario}", font, Brushes.Black, x, y);
                y += 16;
                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, x, y);
                y += 16;

                foreach (var item in listaItems)
                {
                    e.Graphics.DrawString(item.Nombre, font, Brushes.Black, x, y);
                    y += 14;
                    e.Graphics.DrawString(
                        $"  {item.Cantidad} x {item.Precio:C} = {item.Subtotal:C}",
                        font, Brushes.Black, x, y);
                    y += 16;
                }

                e.Graphics.DrawString("--------------------------------", font, Brushes.Black, x, y);
                y += 16;
                e.Graphics.DrawString($"Subtotal: {subtotal:C}", font, Brushes.Black, x, y);
                y += 14;
                e.Graphics.DrawString($"TOTAL: {total:C}", fontBold, Brushes.Black, x, y);
                y += 18;
                e.Graphics.DrawString($"Pago: {pago:C}", font, Brushes.Black, x, y);
                y += 14;
                e.Graphics.DrawString($"Cambio: {cambio:C}", font, Brushes.Black, x, y);
                y += 20;
                e.Graphics.DrawString("¡Gracias por su compra!", font, Brushes.Black, x, y);
            };

            doc.Print();
        }
    }
}