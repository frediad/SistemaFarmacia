using FarmaciaPOS.Models;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    public partial class CantidadWindow : Window
    {
        public int CantidadSeleccionada { get; private set; } = 1;

        private readonly Producto _producto;

        public CantidadWindow(Producto producto)
        {
            InitializeComponent();

            _producto = producto;

            txtNombreProducto.Text = producto.Nombre;
            txtStockDisponible.Text = $"Stock disponible: {producto.Stock}";

            Loaded += (s, e) =>
            {
                txtCantidad.Focus();
                txtCantidad.SelectAll();
            };
        }

        // Solo permite dígitos en el campo de cantidad
        private void txtCantidad_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void txtCantidad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnAceptar_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.Escape)
                BtnCancelar_Click(sender, new RoutedEventArgs());
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show(
                    "Ingresa una cantidad válida",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (cantidad > _producto.Stock)
            {
                MessageBox.Show(
                    $"No hay stock suficiente. Disponible: {_producto.Stock}",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            CantidadSeleccionada = cantidad;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}