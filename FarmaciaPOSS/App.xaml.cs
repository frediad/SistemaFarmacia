using System;
using System.Windows;
using System.Windows.Threading;
using FarmaciaPOS.Helpers;

namespace FarmaciaPOS
{
    public partial class App : Application
    {
        private DispatcherTimer? _timerRespaldo;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            IniciarRespaldoAutomatico();
        }

        private void IniciarRespaldoAutomatico()
        {
            _timerRespaldo = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(30) // revisa cada 30 minutos si toca respaldar
            };

            _timerRespaldo.Tick += (s, e) => RevisarSiTocaRespaldo();
            _timerRespaldo.Start();
        }

        private void RevisarSiTocaRespaldo()
        {
            var config = ConfiguracionPosHelper.Cargar();

            if (!config.RespaldoAutomaticoActivo)
                return;

            if (string.IsNullOrWhiteSpace(config.RespaldoCarpeta))
                return;

            if (DatabaseHelper.ObtenerCadenaConexionOrigenActual() == "Azure")
                return; // no aplica, Azure gestiona sus propios respaldos

            bool tocaRespaldar =
                config.UltimoRespaldo == null ||
                (DateTime.Now - config.UltimoRespaldo.Value).TotalHours >= config.RespaldoIntervaloHoras;

            if (!tocaRespaldar)
                return;

            try
            {
                RespaldoHelper.EjecutarRespaldoLocal(config.RespaldoCarpeta);

                config.UltimoRespaldo = DateTime.Now;
                ConfiguracionPosHelper.Guardar(config);
            }
            catch
            {
                // Falla silenciosa en segundo plano — no interrumpir al usuario
                // mientras trabaja. El próximo intento será en 30 min.
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Ocurrió un error inesperado:\n\n{e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}