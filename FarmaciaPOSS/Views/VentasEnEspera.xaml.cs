using FarmaciaPOS.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class VentasEnEsperaWindow : Window
    {
        private readonly List<VentaEnEspera> _ventasEnEspera;

        public VentaEnEspera VentaSeleccionada { get; private set; }

        public VentasEnEsperaWindow(List<VentaEnEspera> ventasEnEspera)
        {
            InitializeComponent();
            _ventasEnEspera = ventasEnEspera;
            ActualizarLista();
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

        private void BtnRecuperar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is VentaEnEspera venta)
            {
                VentaSeleccionada = venta;
                _ventasEnEspera.Remove(venta);
                DialogResult = true;
            }
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
            DialogResult = false;
        }
    }
}