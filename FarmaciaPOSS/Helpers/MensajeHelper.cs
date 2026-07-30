using System.Windows;

namespace FarmaciaPOS.Helpers
{
    public static class MensajeHelper
    {
        public static void Info(string mensaje, string titulo = "Aviso", Window? owner = null)
        {
            Mostrar(mensaje, titulo, Views.TipoMensaje.Informacion, false, owner);
        }

        public static void Exito(string mensaje, string titulo = "Éxito", Window? owner = null)
        {
            Mostrar(mensaje, titulo, Views.TipoMensaje.Exito, false, owner);
        }

        public static void Advertencia(string mensaje, string titulo = "Aviso", Window? owner = null)
        {
            Mostrar(mensaje, titulo, Views.TipoMensaje.Advertencia, false, owner);
        }

        public static void Error(string mensaje, string titulo = "Error", Window? owner = null)
        {
            Mostrar(mensaje, titulo, Views.TipoMensaje.Error, false, owner);
        }

        public static bool Confirmar(string mensaje, string titulo = "Confirmar", Window? owner = null)
        {
            return Mostrar(mensaje, titulo, Views.TipoMensaje.Pregunta, true, owner);
        }

        private static bool Mostrar(string mensaje, string titulo, Views.TipoMensaje tipo, bool esConfirmacion, Window? owner)
        {
            var ventana = new Views.MensajeWindow();
            ventana.Configurar(mensaje, titulo, tipo, esConfirmacion);

            if (owner != null)
                ventana.Owner = owner;

            ventana.ShowDialog();

            return ventana.Resultado;
        }
    }
}