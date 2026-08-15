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
        // BANNER DE LA VENTA ACTUAL (carrito en curso)
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

            _carritoActual.Clear();
            VentaActualGuardada = true;

            ActualizarLista();
            ActualizarBannerVentaActual();

            MensajeHelper.Exito(
                $"Venta \"{ventaEspera.Referencia}\" guardada en espera",
                "En espera",
                this);
        }

        // =========================================
        // RECUPERAR VENTA
        // =========================================

        private void BtnRecuperar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not VentaEnEspera venta)
                return;

            if (_carritoActual.Count > 0 && !VentaActualGuardada)
            {
                bool confirmar = MensajeHelper.Confirmar(
                    "Tienes productos en tu venta actual que no has guardado.\n\n" +
                    "Si recuperas esta venta en espera, tu venta actual se perderá.\n\n" +
                    "¿Deseas continuar de todos modos?",
                    "Venta actual sin guardar",
                    this);

                if (!confirmar)
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
                bool confirmar = MensajeHelper.Confirmar(
                    $"¿Eliminar la venta en espera \"{venta.Referencia}\"?",
                    "Confirmar",
                    this);

                if (confirmar)
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