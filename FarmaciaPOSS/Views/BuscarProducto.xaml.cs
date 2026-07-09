using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    public partial class BuscarProductoWindow : Window
    {
        private readonly List<Producto> _productos;

        public Producto ProductoSeleccionado { get; private set; }

        public BuscarProductoWindow(List<Producto> productos)
        {
            InitializeComponent();

            _productos = productos;

            lstResultados.ItemsSource = _productos;

            Loaded += (s, e) => txtBuscar.Focus();
        }

        private void txtBuscar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text;

            var resultados = string.IsNullOrWhiteSpace(texto)
                ? _productos
                : _productos.Where(p =>
                    TextoHelper.Coincide(p.Nombre, texto) ||
                    TextoHelper.Coincide(p.CodigoBarras, texto))
                  .ToList();

            lstResultados.ItemsSource = resultados;

            txtSinResultados.Visibility =
                resultados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void lstResultados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SeleccionarActual();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SeleccionarActual();
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
            else if (e.Key == Key.Down && lstResultados.Items.Count > 0)
            {
                lstResultados.Focus();
                lstResultados.SelectedIndex = 0;
            }
        }

        private void SeleccionarActual()
        {
            var seleccionado = lstResultados.SelectedItem as Producto;

            // Si no hay nada seleccionado con el mouse/teclado, toma el primer resultado visible
            seleccionado ??= (lstResultados.ItemsSource as List<Producto>)?.FirstOrDefault();

            if (seleccionado == null)
                return;

            ProductoSeleccionado = seleccionado;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}