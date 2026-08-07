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
            // ✅ + del teclado numérico o del teclado normal (Shift + =)
            if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                IncrementarCantidad();
                e.Handled = true;
                return;
            }

            // ✅ - del teclado numérico o del teclado normal
            if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                DecrementarCantidad();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
                BtnAceptar_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.Escape)
                BtnCancelar_Click(sender, new RoutedEventArgs());
        }

        // incrementa la cantidad respetando el stock disponible
        private void IncrementarCantidad()
        {
            int cantidadActual = ObtenerCantidadActual();

            if (cantidadActual >= _producto.Stock)
            {
                // Ya está en el máximo disponible; no sigue subiendo
                return;
            }

            cantidadActual++;
            ActualizarCampoCantidad(cantidadActual);
        }

        //  disminuye la cantidad sin bajar de 1
        private void DecrementarCantidad()
        {
            int cantidadActual = ObtenerCantidadActual();

            if (cantidadActual <= 1)
                return;

            cantidadActual--;
            ActualizarCampoCantidad(cantidadActual);
        }

        private int ObtenerCantidadActual()
        {
            return int.TryParse(txtCantidad.Text, out int valor) && valor > 0 ? valor : 1;
        }

        private void ActualizarCampoCantidad(int nuevaCantidad)
        {
            txtCantidad.Text = nuevaCantidad.ToString();
            txtCantidad.CaretIndex = txtCantidad.Text.Length; // cursor al final
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