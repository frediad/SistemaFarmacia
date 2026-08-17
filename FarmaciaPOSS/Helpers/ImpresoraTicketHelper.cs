using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using Microsoft.Data.SqlClient;
using FarmaciaPOS.Models;

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

        // =========================================
        // ✅ DATOS DEL NEGOCIO (tabla ConfiguracionTicket)
        // =========================================

        private static ConfiguracionTicketData ObtenerConfiguracionTicket()
        {
            var config = new ConfiguracionTicketData();

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query = "SELECT TOP 1 * FROM ConfiguracionTicket";
                SqlCommand cmd = new SqlCommand(query, conn);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    config.NombreNegocio = ValorOTexto(reader, "NombreNegocio", config.NombreNegocio);
                    config.RFC = ValorOTexto(reader, "RFC", config.RFC);
                    config.Direccion = ValorOTexto(reader, "Direccion", config.Direccion);
                    config.Telefono = ValorOTexto(reader, "Telefono", config.Telefono);
                    config.Correo = ValorOTexto(reader, "Correo", config.Correo);
                    config.MensajeTicket = ValorOTexto(reader, "MensajeTicket", config.MensajeTicket);
                }
            }
            catch
            {
                // Valores por defecto si falla la consulta — no debe tronar la impresión.
            }

            return config;
        }

        private static string ValorOTexto(SqlDataReader reader, string columna, string valorPorDefecto)
        {
            object valor = reader[columna];
            string texto = (valor == null || valor == DBNull.Value) ? "" : valor.ToString() ?? "";
            return string.IsNullOrWhiteSpace(texto) ? valorPorDefecto : texto;
        }

        // =========================================
        // ✅ LOGO DEL NEGOCIO (carpeta Images)
        // =========================================

        // Carga el logo desde la carpeta "Images" junto al ejecutable.
        // Devuelve null si el archivo no existe o no se puede leer —
        // así el ticket se imprime igual, solo sin logo, en vez de tronar.
        private static Image? CargarLogo()
        {
            try
            {
                string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "logo-farmacia-yatzil.png");

                if (!File.Exists(ruta))
                    return null;

                // Se carga en memoria (no directo desde el stream del archivo)
                // para no dejar el archivo bloqueado mientras se imprime.
                using var stream = new MemoryStream(File.ReadAllBytes(ruta));
                return Image.FromStream(stream);
            }
            catch
            {
                return null;
            }
        }

        // Dibuja el logo centrado horizontalmente, escalado para que quepa
        // en el ancho del rollo sin deformarse, y devuelve cuánto ocupó en
        // alto (para que el texto de abajo no se le encime).
        private static float DibujarLogo(Graphics g, Image? logo, float anchoUtilPx, float x, float y)
        {
            if (logo == null)
                return 0;

            const float altoMaximoPx = 110f;

            float escala = Math.Min(anchoUtilPx / logo.Width, altoMaximoPx / logo.Height);
            // No agrandamos logos pequeños más allá de su tamaño original,
            // solo los reducimos si no caben — evita verse pixeleados.
            escala = Math.Min(escala, 1f);

            float anchoFinal = logo.Width * escala;
            float altoFinal = logo.Height * escala;
            float xCentrado = x + (anchoUtilPx - anchoFinal) / 2f;

            g.DrawImage(logo, xCentrado, y, anchoFinal, altoFinal);

            return altoFinal;
        }

        // =========================================
        // ✅ CONFIGURACIÓN DE ANCHO DE PAPEL (58mm / 80mm)
        // =========================================

        private static float ConfigurarAnchoPapel(PrintDocument doc, int anchoMM)
        {
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            int anchoCentesimas = (int)Math.Round(anchoMM / 25.4 * 100);
            int altoCentesimas = 30000; // rollo continuo

            doc.DefaultPageSettings.PaperSize =
                new PaperSize("Rollo Térmico", anchoCentesimas, altoCentesimas);

            double anchoImprimibleMM = anchoMM - 3;
            float anchoUtilPx = (float)(anchoImprimibleMM / 25.4 * 100);

            return anchoUtilPx;
        }

        private static (Font titulo, Font normal, Font negrita) ObtenerFuentes(int anchoMM)
        {
            bool esAngosta = anchoMM <= 58;

            float tituloSize = esAngosta ? 10f : 12f;
            float normalSize = esAngosta ? 7.5f : 9f;

            return (
                new Font("Consolas", tituloSize, FontStyle.Bold),
                new Font("Consolas", normalSize),
                new Font("Consolas", normalSize, FontStyle.Bold)
            );
        }

        private static string LineaSeparadora(int anchoMM)
        {
            int guiones = anchoMM <= 58 ? 32 : 46;
            return new string('-', guiones);
        }

        private static float DrawStringMultilinea(Graphics g, string texto, Font font, float x, float y, float anchoUtilPx)
        {
            var rect = new RectangleF(x, y, anchoUtilPx, font.Height * 3);
            g.DrawString(texto, font, Brushes.Black, rect);

            SizeF medida = g.MeasureString(texto, font, (int)anchoUtilPx);
            return medida.Height;
        }

        // =========================================
        // TICKET DE PRUEBA
        // =========================================

        public static void ImprimirTicketPrueba(string nombreImpresora, int anchoMM = 58)
        {
            if (!ImpresoraExiste(nombreImpresora))
                throw new Exception($"La impresora \"{nombreImpresora}\" ya no está disponible en este equipo.");

            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = nombreImpresora;

            if (!doc.PrinterSettings.IsValid)
                throw new Exception($"La impresora \"{nombreImpresora}\" no es válida o no responde.");

            float anchoUtilPx = ConfigurarAnchoPapel(doc, anchoMM);
            var (fontTitulo, font, _) = ObtenerFuentes(anchoMM);
            string separador = LineaSeparadora(anchoMM);
            var negocio = ObtenerConfiguracionTicket();

            using Image? logo = CargarLogo();

            doc.PrintPage += (sender, e) =>
            {
                float x = 6;
                float y = 10;

                y += DibujarLogo(e.Graphics, logo, anchoUtilPx, x, y) + (logo != null ? 6 : 0);

                e.Graphics.DrawString(negocio.NombreNegocio, fontTitulo, Brushes.Black, x, y);
                y += fontTitulo.Height + 6;
                e.Graphics.DrawString("=== TICKET DE PRUEBA ===", font, Brushes.Black, x, y);
                y += font.Height + 4;
                e.Graphics.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", font, Brushes.Black, x, y);
                y += font.Height + 4;
                e.Graphics.DrawString($"Rollo configurado: {anchoMM}mm", font, Brushes.Black, x, y);
                y += font.Height + 4;
                e.Graphics.DrawString(logo != null ? "Logo cargado correctamente ✔" : "⚠ No se encontró el logo (Images/logo-farmacia-yatzil.png)", font, Brushes.Black, x, y);
                y += font.Height + 4;
                e.Graphics.DrawString("Impresora funcionando correctamente ✔", font, Brushes.Black, x, y);
                y += font.Height + 4;
                e.Graphics.DrawString(separador, font, Brushes.Black, x, y);
            };

            doc.Print();
        }

        // =========================================
        // TICKET DE VENTA REAL
        // =========================================

        public static void ImprimirTicketVenta(
            string nombreImpresora,
            string folio,
            string nombreUsuario,
            IEnumerable<VentaItem> items,
            decimal subtotal,
            decimal total,
            decimal pago,
            decimal cambio,
            int anchoMM = 58)
        {
            if (string.IsNullOrWhiteSpace(nombreImpresora))
                throw new Exception("No hay ninguna impresora de tickets configurada. Ve a Configuración para asignar una.");

            if (!ImpresoraExiste(nombreImpresora))
                throw new Exception($"La impresora \"{nombreImpresora}\" ya no está disponible en este equipo.");

            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = nombreImpresora;

            if (!doc.PrinterSettings.IsValid)
                throw new Exception($"La impresora \"{nombreImpresora}\" no es válida o no responde.");

            float anchoUtilPx = ConfigurarAnchoPapel(doc, anchoMM);
            var (fontTitulo, font, fontBold) = ObtenerFuentes(anchoMM);
            string separador = LineaSeparadora(anchoMM);
            var negocio = ObtenerConfiguracionTicket();

            using Image? logo = CargarLogo();

            var listaItems = new List<VentaItem>(items);

            doc.PrintPage += (sender, e) =>
            {
                float x = 6;
                float y = 10;

                // ===== Logo =====
                y += DibujarLogo(e.Graphics, logo, anchoUtilPx, x, y) + (logo != null ? 6 : 0);

                // ===== Encabezado con datos del negocio =====

                y += DrawStringMultilinea(e.Graphics, negocio.NombreNegocio, fontTitulo, x, y, anchoUtilPx) + 2;

                if (!string.IsNullOrWhiteSpace(negocio.RFC))
                {
                    e.Graphics.DrawString($"RFC: {negocio.RFC}", font, Brushes.Black, x, y);
                    y += font.Height + 2;
                }

                if (!string.IsNullOrWhiteSpace(negocio.Direccion))
                {
                    y += DrawStringMultilinea(e.Graphics, negocio.Direccion, font, x, y, anchoUtilPx) + 2;
                }

                if (!string.IsNullOrWhiteSpace(negocio.Telefono))
                {
                    e.Graphics.DrawString($"Tel: {negocio.Telefono}", font, Brushes.Black, x, y);
                    y += font.Height + 2;
                }

                if (!string.IsNullOrWhiteSpace(negocio.Correo))
                {
                    e.Graphics.DrawString(negocio.Correo, font, Brushes.Black, x, y);
                    y += font.Height + 2;
                }

                e.Graphics.DrawString(separador, font, Brushes.Black, x, y);
                y += font.Height + 2;

                // ===== Datos de la venta =====

                e.Graphics.DrawString($"Folio: {folio}", font, Brushes.Black, x, y);
                y += font.Height + 2;
                e.Graphics.DrawString($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", font, Brushes.Black, x, y);
                y += font.Height + 2;
                e.Graphics.DrawString($"Atendió: {nombreUsuario}", font, Brushes.Black, x, y);
                y += font.Height + 2;
                e.Graphics.DrawString(separador, font, Brushes.Black, x, y);
                y += font.Height + 2;

                // ===== Productos =====

                foreach (var item in listaItems)
                {
                    y += DrawStringMultilinea(e.Graphics, item.Nombre, font, x, y, anchoUtilPx);

                    e.Graphics.DrawString(
                        $"  {item.Cantidad} x {item.Precio:C} = {item.Subtotal:C}",
                        font, Brushes.Black, x, y);
                    y += font.Height + 2;
                }

                e.Graphics.DrawString(separador, font, Brushes.Black, x, y);
                y += font.Height + 2;

                // ===== Totales =====

                e.Graphics.DrawString($"Subtotal: {subtotal:C}", font, Brushes.Black, x, y);
                y += font.Height;
                e.Graphics.DrawString($"TOTAL: {total:C}", fontBold, Brushes.Black, x, y);
                y += fontBold.Height + 2;
                e.Graphics.DrawString($"Pago: {pago:C}", font, Brushes.Black, x, y);
                y += font.Height;
                e.Graphics.DrawString($"Cambio: {cambio:C}", font, Brushes.Black, x, y);
                y += font.Height + 4;

                e.Graphics.DrawString(negocio.MensajeTicket, font, Brushes.Black, x, y);
            };

            doc.Print();
        }
    }
}