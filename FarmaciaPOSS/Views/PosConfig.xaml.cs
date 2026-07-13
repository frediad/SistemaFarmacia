using FarmaciaPOS.Helpers;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FarmaciaPOS.Views
{
    public partial class PosConfig : Window
    {
        private ConfiguracionPos config;

        // Variables para medir la velocidad de escritura en la prueba del escáner
        private DateTime primerCaracter;
        private DateTime ultimoCaracter;
        private int contadorCaracteres;

        public PosConfig()
        {
            InitializeComponent();

            config = ConfiguracionPosHelper.Cargar();

            CargarImpresoras();
            CargarConfiguracionBD();
            CargarConfiguracionRespaldo();
        }

        // =========================================
        // IMPRESORA DE TICKETS
        // =========================================

        private void CargarImpresoras()
        {
            cbImpresoras.ItemsSource = ImpresoraTicketHelper.ObtenerImpresorasInstaladas();

            if (!string.IsNullOrWhiteSpace(config.ImpresoraTicket))
                cbImpresoras.SelectedItem = config.ImpresoraTicket;
            else if (cbImpresoras.Items.Count > 0)
                cbImpresoras.SelectedIndex = 0;
        }

        private void BtnActualizarImpresoras_Click(object sender, RoutedEventArgs e)
        {
            CargarImpresoras();
            txtEstadoImpresora.Text = "Lista de impresoras actualizada.";
            txtEstadoImpresora.Foreground = Brushes.Gray;
        }

        private void BtnProbarImpresora_Click(object sender, RoutedEventArgs e)
        {
            if (cbImpresoras.SelectedItem is not string nombreImpresora)
            {
                txtEstadoImpresora.Text = "Selecciona una impresora primero.";
                txtEstadoImpresora.Foreground = Brushes.OrangeRed;
                return;
            }

            try
            {
                ImpresoraTicketHelper.ImprimirTicketPrueba(nombreImpresora);

                config.ImpresoraTicket = nombreImpresora;
                ConfiguracionPosHelper.Guardar(config);

                txtEstadoImpresora.Text = $"✅ Ticket de prueba enviado a \"{nombreImpresora}\" y guardada como impresora predeterminada.";
                txtEstadoImpresora.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                txtEstadoImpresora.Text = $"❌ Error al imprimir: {ex.Message}";
                txtEstadoImpresora.Foreground = Brushes.Red;
            }
        }

        // =========================================
        // ESCÁNER
        // =========================================

        private void txtPruebaEscaner_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPruebaEscaner.Clear();
            borderResultadoEscaner.Visibility = Visibility.Collapsed;
            contadorCaracteres = 0;
        }

        private void txtPruebaEscaner_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (contadorCaracteres == 0)
                primerCaracter = DateTime.Now;

            if (e.Key != Key.Enter)
            {
                contadorCaracteres++;
                ultimoCaracter = DateTime.Now;
                return;
            }

            // Presionó Enter: el escáner (o el usuario) terminó de "escribir" el código
            if (contadorCaracteres == 0)
                return;

            double milisegundosTotales = (ultimoCaracter - primerCaracter).TotalMilliseconds;
            double msPorCaracter = milisegundosTotales / Math.Max(contadorCaracteres, 1);

            borderResultadoEscaner.Visibility = Visibility.Visible;

            // Un humano escribiendo rápido ronda 80-150ms por tecla.
            // Un lector de código de barras suele estar muy por debajo de 20ms por carácter.
            bool pareceEscaner = msPorCaracter < 25 && contadorCaracteres >= 4;

            if (pareceEscaner)
            {
                txtResultadoEscaner.Text = "✅ El escáner está funcionando correctamente";
                txtResultadoEscaner.Foreground = Brushes.Green;
            }
            else
            {
                txtResultadoEscaner.Text = "⚠️ La entrada parece haber sido escrita manualmente, no detectada como escáner";
                txtResultadoEscaner.Foreground = Brushes.OrangeRed;
            }

            txtDetalleEscaner.Text =
                $"Código leído: \"{txtPruebaEscaner.Text}\"  •  {contadorCaracteres} caracteres en {milisegundosTotales:F0} ms " +
                $"({msPorCaracter:F1} ms por carácter)";
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