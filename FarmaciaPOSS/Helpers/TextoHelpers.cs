using System.Globalization;
using System.Text;

namespace FarmaciaPOS.Helpers
{
    public static class TextoHelper
    {
        // Quita acentos, pasa a minúsculas y recorta espacios,
        // para que la búsqueda no dependa de cómo esté escrito el texto.
        public static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            string sinAcentos = QuitarAcentos(texto);
            return sinAcentos.ToLowerInvariant().Trim();
        }

        private static string QuitarAcentos(string texto)
        {
            string normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalizado)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // true si "origen" contiene "busqueda", ignorando acentos y mayúsculas
        public static bool Coincide(string origen, string busqueda)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
                return true;

            return Normalizar(origen).Contains(Normalizar(busqueda));
        }
    }
}