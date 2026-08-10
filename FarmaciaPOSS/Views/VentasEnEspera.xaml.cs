using FarmaciaPOS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FarmaciaPOS.Helpers;

namespace FarmaciaPOS.Views
{
    public partial class VentasEnEsperaWindow : Window
    {
        private readonly List<VentaEnEspera> _ventasEnEspera;
        private readonly List<VentaItem> _carritoActual;

        public VentaEnEspera VentaSeleccionada { get; private set; }

        // ✅ Indica si, DESDE esta ventana, se guardó el carrito actual en espera.
        // MainWindow usa esto para saber si debe limpiar su propio carrito.
        public bool VentaActualGuardada { get; private set; } = false;

        public VentasEnEsperaWindow(
            List<VentaEnEspera> ventasEnEspera,
            List<VentaItem> carritoActual = null)
        {
            InitializeComponent();

            _ventasEnEspera = ventasEnEspera;
            _carritoActual = carritoActual ?? new List<VentaItem>();

            ActualizarLista();
            ActualizarBannerVentaActual();
        }

        private void ActualizarLista()
        {
            icVentasEspera.ItemsSource = null;
            icVentasEspera.ItemsSource = _ventasEnEspera;
            txtSinVentas.Visibility =
                _ventasEnEspera.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        // =========================================
        // ✅ BANNER DE LA VENTA ACTUAL (carrito en curso)
        // =========================================

        private void ActualizarBannerVentaActual()
        {
            if (_carritoActual.Count == 0)
            {
                borderVentaActual.Visibility = Visibility.Collapsed;
                return;
            }

            decimal total = _carritoActual.Sum(x => x.Subtotal);

            txtResumenVentaActual.Text =
                $"{_carritoActual.Count} producto(s) — Total: {total:C}";

            borderVentaActual.Visibility = Visibility.Visible;
        }

        private void BtnGuardarVentaActual_Click(object sender, RoutedEventArgs e)
        {
            if (_carritoActual.Count == 0)
                return;

            var ventaEspera = new VentaEnEspera
            {
                Id = (_ventasEnEspera.Count > 0 ? _ventasEnEspera.Max(v => v.Id) : 0) + 1,
                Referencia = $"VE-{DateTime.Now:yyyyMMddHHmmss}",
                Items = _carritoActual.ToList()
            };

            _ventasEnEspera.Add(ventaEspera);

            // Ya se guardó: vaciamos la referencia local para ocultar el banner,
            // y marcamos la bandera para que MainWindow sepa que debe limpiar su carrito.
            _carritoActual.Clear();
            VentaActualGuardada = true;

            ActualizarLista();
            ActualizarBannerVentaActual();

            MensajeHelper.Info(
                $"Venta \"{ventaEspera.Referencia}\" guardada en espera",
                "En espera");
        }

        // =========================================
        // RECUPERAR VENTA
        // =========================================

        private void BtnRecuperar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not VentaEnEspera venta)
                return;

            // ✅ Si el usuario trae productos sin guardar, avisamos antes de perderlos
            if (_carritoActual.Count > 0 && !VentaActualGuardada)
            {
                var confirmar = MessageBox.Show(
                    "Tienes productos en tu venta actual que no has guardado.\n\n" +
                    "Si recuperas esta venta en espera, tu venta actual se perderá.\n\n" +
                    "¿Deseas continuar de todos modos?",
                    "Venta actual sin guardar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmar != MessageBoxResult.Yes)
                    return;
            }

            VentaSeleccionada = venta;
            _ventasEnEspera.Remove(venta);
            DialogResult = true;
        }

        private void BtnEliminarEspera_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is VentaEnEspera venta)
            {
                var confirmar = MessageBox.Show(
                    $"¿Eliminar la venta en espera \"{venta.Referencia}\"?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmar == MessageBoxResult.Yes)
                {
                    _ventasEnEspera.Remove(venta);
                    ActualizarLista();
                }
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = VentaActualGuardada ? true : false;
        }
    }
}