using FarmaciaPOS.Helpers;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FarmaciaPOS.Views
{
    public partial class Perifericos : Window
    {
        private ConfiguracionPos config;

        private DateTime primerCaracter;
        private DateTime ultimoCaracter;
        private int contadorCaracteres;

        public Perifericos()
        {
            InitializeComponent();

            config = ConfiguracionPosHelper.Cargar();

            CargarImpresoras();
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

            if (contadorCaracteres == 0)
                return;

            double milisegundosTotales = (ultimoCaracter - primerCaracter).TotalMilliseconds;
            double msPorCaracter = milisegundosTotales / Math.Max(contadorCaracteres, 1);

            borderResultadoEscaner.Visibility = Visibility.Visible;

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

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}