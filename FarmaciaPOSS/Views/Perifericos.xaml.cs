using FarmaciaPOS.Helpers;
using System;
using System.Collections.Generic;
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
            VerificarEstadoImpresoraSeleccionada();
            VerificarDispositivosConProblemas();
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
            VerificarEstadoImpresoraSeleccionada();
            VerificarDispositivosConProblemas();

            txtEstadoImpresora.Text = "Lista de impresoras actualizada.";
            txtEstadoImpresora.Foreground = Brushes.Gray;
        }

        private void CbImpresoras_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            VerificarEstadoImpresoraSeleccionada();
        }

        // ✅ Consulta si la impresora seleccionada está realmente conectada
        // y lista, o si tiene algún problema (fuera de línea, sin controlador, etc.)
        private void BtnVerificarConexion_Click(object sender, RoutedEventArgs e)
        {
            VerificarEstadoImpresoraSeleccionada();
        }

        private void VerificarEstadoImpresoraSeleccionada()
        {
            if (cbImpresoras.SelectedItem is not string nombreImpresora)
            {
                pnlEstadoConexion.Visibility = Visibility.Collapsed;
                return;
            }

            var estado = DispositivosHelper.ObtenerEstadoImpresora(nombreImpresora);

            pnlEstadoConexion.Visibility = Visibility.Visible;

            if (estado.EnLinea)
            {
                elipseEstadoConexion.Fill = Brushes.MediumSeaGreen;
                txtEstadoConexion.Text = "🟢 " + estado.MensajeEstado;
                txtEstadoConexion.Foreground = new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D));
                btnInstalarControlador.Visibility = Visibility.Collapsed;
            }
            else
            {
                elipseEstadoConexion.Fill = Brushes.OrangeRed;
                txtEstadoConexion.Text = "🔴 " + estado.MensajeEstado;
                txtEstadoConexion.Foreground = Brushes.OrangeRed;
                // Solo ofrecemos instalar controlador si el problema parece serlo
                // (no encontrada / con error), no cuando solo está "fuera de línea"
                // por estar apagada, que es un problema físico, no de software.
                btnInstalarControlador.Visibility = !estado.Encontrada
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
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
                // ✅ Usa el ancho configurado (58mm por defecto — también soporta 80mm)
                ImpresoraTicketHelper.ImprimirTicketPrueba(nombreImpresora, config.AnchoTicketMM);

                config.ImpresoraTicket = nombreImpresora;
                ConfiguracionPosHelper.Guardar(config);

                txtEstadoImpresora.Text = $"✅ Ticket de prueba enviado a \"{nombreImpresora}\" ({config.AnchoTicketMM}mm) y guardada como impresora predeterminada.";
                txtEstadoImpresora.Foreground = Brushes.Green;
            }
            catch (Exception ex)
            {
                txtEstadoImpresora.Text = $"❌ Error al imprimir: {ex.Message}";
                txtEstadoImpresora.Foreground = Brushes.Red;
            }
        }

        // ✅ Abre el asistente nativo de Windows para instalar el controlador
        // de la impresora (elegir fabricante/modelo o buscar por Windows Update).
        private void BtnInstalarControlador_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DispositivosHelper.AbrirAsistenteInstalarImpresora();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "Error");
            }
        }

        // ✅ Abre el panel de Windows para vincular una impresora nueva (USB/red/Bluetooth)
        private void BtnAgregarImpresoraWindows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DispositivosHelper.AbrirConfiguracionImpresorasWindows();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "Error");
            }
        }

        // =========================================
        // ✅ DISPOSITIVOS CON PROBLEMAS DE CONTROLADOR (general: impresoras, USB, HID, puertos)
        // =========================================

        private void VerificarDispositivosConProblemas()
        {
            List<DispositivoConProblema> problemas = DispositivosHelper.ObtenerDispositivosConProblemas();

            if (problemas.Count == 0)
            {
                pnlDispositivosConProblemas.Visibility = Visibility.Collapsed;
                return;
            }

            pnlDispositivosConProblemas.Visibility = Visibility.Visible;
            txtResumenProblemas.Text = problemas.Count == 1
                ? "Se detectó 1 dispositivo con problema de controlador:"
                : $"Se detectaron {problemas.Count} dispositivos con problemas de controlador:";

            lstDispositivosConProblemas.ItemsSource = problemas;
        }

        private void BtnActualizarDispositivos_Click(object sender, RoutedEventArgs e)
        {
            VerificarDispositivosConProblemas();
        }

        // ✅ Abre el Administrador de dispositivos para que el usuario resuelva
        // el problema puntual (clic derecho sobre el dispositivo → Actualizar controlador)
        private void BtnAbrirAdministradorDispositivos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DispositivosHelper.AbrirAdministradorDispositivos();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "Error");
            }
        }

        private void BtnBuscarEnWindowsUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DispositivosHelper.AbrirWindowsUpdate();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "Error");
            }
        }

        // =========================================
        // ✅ ESCÁNER — compatible con lectores 1D y 2D
        // =========================================

        private void txtPruebaEscaner_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPruebaEscaner.Clear();
            borderResultadoEscaner.Visibility = Visibility.Collapsed;
            contadorCaracteres = 0;
        }

        // ✅ PreviewTextInput solo dispara con caracteres reales (nunca con
        // Shift, Ctrl, Alt, flechas, etc.) — funciona igual para lectores 1D
        // (código de barras clásico) y 2D (QR / DataMatrix), sin importar si
        // el código contiene letras, números o símbolos que requieran Shift.
        private void txtPruebaEscaner_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (contadorCaracteres == 0)
                primerCaracter = DateTime.Now;

            contadorCaracteres += e.Text.Length;
            ultimoCaracter = DateTime.Now;
        }

        // ✅ Algunos lectores terminan con Enter, otros con Tab — se aceptan ambos.
        private void txtPruebaEscaner_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Tab)
                return;

            if (contadorCaracteres == 0)
                return;

            e.Handled = true; // evita que Tab mueva el foco a otro control

            double milisegundosTotales = (ultimoCaracter - primerCaracter).TotalMilliseconds;
            double msPorCaracter = milisegundosTotales / Math.Max(contadorCaracteres, 1);

            borderResultadoEscaner.Visibility = Visibility.Visible;

            // ✅ Umbral más flexible: cubre lectores 1D (suelen ser más lentos,
            // ~15-30 ms/carácter) y 2D (suelen ser casi instantáneos, <10 ms/carácter).
            bool pareceEscaner = msPorCaracter < 40 && contadorCaracteres >= 4;

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

            contadorCaracteres = 0;
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}