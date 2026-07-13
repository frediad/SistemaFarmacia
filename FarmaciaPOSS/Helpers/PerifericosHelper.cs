using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace FarmaciaPOS.Helpers
{
    public class ResultadoEscaner
    {
        public bool EsEscaner { get; set; }
        public string Mensaje { get; set; } = "";
        public string Detalle { get; set; } = "";
        public Brush Color { get; set; } = Brushes.Black;
    }

    public static class PerifericosHelper
    {
        //==========================================
        // IMPRESORA
        //==========================================

        public static bool ProbarImpresora(
            string nombreImpresora,
            out string mensaje)
        {
            try
            {
                ImpresoraTicketHelper.ImprimirTicketPrueba(nombreImpresora);

                mensaje =
                    $"✅ Ticket enviado correctamente a \"{nombreImpresora}\".";

                return true;
            }
            catch (Exception ex)
            {
                mensaje =
                    $"❌ Error al imprimir:\n{ex.Message}";

                return false;
            }
        }

        //==========================================
        // ESCÁNER
        //==========================================

        public static ResultadoEscaner AnalizarEscaneo(
            string codigo,
            int caracteres,
            DateTime inicio,
            DateTime fin)
        {
            ResultadoEscaner resultado =
                new ResultadoEscaner();

            double tiempo =
                (fin - inicio).TotalMilliseconds;

            double msCaracter =
                tiempo / Math.Max(caracteres, 1);

            bool escaner =
                msCaracter < 25 &&
                caracteres >= 4;

            resultado.EsEscaner = escaner;

            if (escaner)
            {
                resultado.Mensaje =
                    "✅ El escáner funciona correctamente.";

                resultado.Color =
                    Brushes.Green;
            }
            else
            {
                resultado.Mensaje =
                    "⚠️ La lectura parece haber sido escrita manualmente.";

                resultado.Color =
                    Brushes.OrangeRed;
            }

            resultado.Detalle =
                $"Código: {codigo}\n" +
                $"Caracteres: {caracteres}\n" +
                $"Tiempo: {tiempo:F0} ms\n" +
                $"Promedio: {msCaracter:F1} ms/caracter";

            return resultado;
        }
    }
}