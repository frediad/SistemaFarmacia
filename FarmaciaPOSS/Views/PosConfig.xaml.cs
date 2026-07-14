using FarmaciaPOS.Helpers;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media;

namespace FarmaciaPOS.Views
{
    public partial class PosConfig : Window
    {
        private ConfiguracionPos config;

        public PosConfig()
        {
            InitializeComponent();

            config = ConfiguracionPosHelper.Cargar();

            CargarConfiguracionBD();
            CargarConfiguracionRespaldo();
        }

        // =========================================
        // BASE DE DATOS
        // =========================================

        private void CargarConfiguracionBD()
        {
            string modoActual = DatabaseHelper.ObtenerModoActual();

            rbLocal.IsChecked = modoActual == "Local";
            rbAzure.IsChecked = modoActual == "Azure";
            rbAmbas.IsChecked = modoActual == "Ambas";
        }

        private void BtnProbarLocal_Click(object sender, RoutedEventArgs e)
        {
            bool ok = DatabaseHelper.ProbarConexion(DatabaseHelper.ObtenerCadenaLocal());

            txtEstadoConexion.Text = ok
                ? "✅ Conexión Local exitosa."
                : "❌ No se pudo conectar a la base de datos Local.";

            txtEstadoConexion.Foreground = ok ? Brushes.Green : Brushes.Red;
        }

        private void BtnProbarAzure_Click(object sender, RoutedEventArgs e)
        {
            bool ok = DatabaseHelper.ProbarConexion(DatabaseHelper.ObtenerCadenaAzure());

            txtEstadoConexion.Text = ok
                ? "✅ Conexión Azure exitosa."
                : "❌ No se pudo conectar a Azure SQL. Verifica el firewall del servidor y tus credenciales.";

            txtEstadoConexion.Foreground = ok ? Brushes.Green : Brushes.Red;
        }

        private void BtnGuardarModoConexion_Click(object sender, RoutedEventArgs e)
        {
            string modo =
                rbAzure.IsChecked == true ? "Azure" :
                rbAmbas.IsChecked == true ? "Ambas" :
                "Local";

            try
            {
                ConfiguracionPosHelper.ActualizarModoConexionEnAppSettings(modo);

                config.ModoConexion = modo;
                ConfiguracionPosHelper.Guardar(config);

                DatabaseHelper.ForzarReevaluacion();

                MessageBox.Show(
                    $"Modo de conexión guardado: {modo}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================
        // RESPALDOS
        // =========================================

        private void CargarConfiguracionRespaldo()
        {
            chkRespaldoAutomatico.IsChecked = config.RespaldoAutomaticoActivo;
            txtCarpetaRespaldo.Text = config.RespaldoCarpeta;
            txtIntervaloHoras.Text = config.RespaldoIntervaloHoras.ToString();
        }

        private void BtnElegirCarpeta_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Selecciona la carpeta para guardar los respaldos"
            };

            if (dialog.ShowDialog() == true)
            {
                txtCarpetaRespaldo.Text = dialog.FolderName;
            }
        }

        private void BtnRespaldarAhora_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCarpetaRespaldo.Text))
            {
                txtEstadoRespaldo.Text = "Selecciona primero una carpeta de destino.";
                txtEstadoRespaldo.Foreground = Brushes.OrangeRed;
                return;
            }

            try
            {
                string ruta = RespaldoHelper.EjecutarRespaldoLocal(txtCarpetaRespaldo.Text);

                config.UltimoRespaldo = DateTime.Now;
                ConfiguracionPosHelper.Guardar(config);

                txtEstadoRespaldo.Text = $"✅ Respaldo generado correctamente:\n{ruta}";
                txtEstadoRespaldo.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                txtEstadoRespaldo.Text = $"❌ {ex.Message}";
                txtEstadoRespaldo.Foreground = Brushes.Red;
            }
        }

        private void BtnGuardarRespaldo_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtIntervaloHoras.Text, out int horas) || horas <= 0)
            {
                MessageBox.Show("Ingresa un intervalo de horas válido (mayor a 0)");
                return;
            }

            config.RespaldoAutomaticoActivo = chkRespaldoAutomatico.IsChecked ?? false;
            config.RespaldoCarpeta = txtCarpetaRespaldo.Text;
            config.RespaldoIntervaloHoras = horas;

            ConfiguracionPosHelper.Guardar(config);

            MessageBox.Show(
                "Configuración de respaldo guardada. El respaldo automático se ejecutará mientras el sistema esté abierto, " +
                "según el intervalo indicado.",
                "Éxito",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}