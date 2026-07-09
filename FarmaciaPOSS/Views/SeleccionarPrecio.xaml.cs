using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class SeleccionarPrecioWindow : Window
    {
        public Models.Producto Producto { get; }

        public decimal PrecioSeleccionado { get; private set; }

        public int TipoPrecio { get; private set; } = 1;

        public SeleccionarPrecioWindow(Models.Producto producto)
        {
            InitializeComponent();

            Producto = producto;

            CargarInformacion();
        }

        private void CargarInformacion()
        {
            txtProducto.Text = $"Producto: {Producto.Nombre}";

            lblPrecio1.Text = Producto.PrecioVenta.ToString("C");

            lblPrecio2.Text = Producto.Precio2 > 0
                ? Producto.Precio2.ToString("C")
                : "No disponible";

            lblCantidad2.Text = Producto.CantidadMayoreo2 > 0
                ? $"Desde {Producto.CantidadMayoreo2} piezas"
                : "Sin cantidad mínima configurada";

            lblPrecio3.Text = Producto.Precio3 > 0
                ? Producto.Precio3.ToString("C")
                : "No disponible";

            lblCantidad3.Text = Producto.CantidadMayoreo3 > 0
                ? $"Desde {Producto.CantidadMayoreo3} piezas"
                : "Sin cantidad mínima configurada";

            // Deshabilitar opciones no configuradas
            rb2.IsEnabled = Producto.Precio2 > 0;
            rb3.IsEnabled = Producto.Precio3 > 0;
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            if (rb1.IsChecked == true)
            {
                TipoPrecio = 1;
                PrecioSeleccionado = Producto.PrecioVenta;
            }
            else if (rb2.IsChecked == true)
            {
                TipoPrecio = 2;
                PrecioSeleccionado = Producto.Precio2;
            }
            else
            {
                TipoPrecio = 3;
                PrecioSeleccionado = Producto.Precio3;
            }

            DialogResult = true;
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }       
}