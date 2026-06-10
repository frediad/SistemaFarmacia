using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using FarmaciaPOS.Views;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FarmaciaPOS
{
    public partial class MainWindow : Window
    {
        string connectionString =
            @"Server=.\SQLEXPRESS;
              Database=FarmaciaDB;
              Trusted_Connection=True;
              TrustServerCertificate=True;";

        List<Producto> productos =
            new();

        // Carrito del POS Rápido (panel derecho)
        List<VentaItem> carritoDashboard =
            new();

        // Carrito del área central de venta
        List<VentaItem> carritoCentral =
            new();

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public MainWindow()
        {
            InitializeComponent();

            CargarProductosDashboard();

            ValidarPermisos();

            InicializarCarritoCentral();
        }

        // =========================================
        // VALIDAR ROLES
        // =========================================

        private void ValidarPermisos()
        {
            if (Sesion.RolId == 1)
                return;

            if (Sesion.RolId == 4)
            {
                btnProductos.Visibility =
                    Visibility.Collapsed;

                btnInventario.Visibility =
                    Visibility.Collapsed;
            }

            if (Sesion.RolId == 5)
            {
                btnVentas.Visibility =
                    Visibility.Collapsed;
            }
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        private void CargarProductosDashboard()
        {
            productos.Clear();

            using SqlConnection conn =
                new SqlConnection(connectionString);

            conn.Open();

            string query =
                "SELECT * FROM Productos WHERE Activo = 1";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(new Producto
                {
                    Id =
                        Convert.ToInt32(
                            reader["Id"]),

                    CodigoBarras =
                        reader["CodigoBarras"]
                        .ToString(),

                    Nombre =
                        reader["Nombre"]
                        .ToString(),

                    PrecioVenta =
                        Convert.ToDecimal(
                            reader["PrecioVenta"])
                });
            }
        }

        // =========================================
        // INICIALIZAR CARRITO CENTRAL
        // =========================================

        private void InicializarCarritoCentral()
        {
            dgCarritoCentral.ItemsSource =
                carritoCentral;

            ActualizarCarritoCentral();
        }

        // =========================================
        // ESCANEAR — POS RÁPIDO
        // =========================================

        private void txtEscaner_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string codigo =
                    txtEscaner.Text.Trim();

                AgregarAlCarrito(
                    codigo,
                    carritoDashboard,
                    esCentral: false);

                txtEscaner.Clear();
            }
        }

        // =========================================
        // ESCANEAR — ÁREA CENTRAL
        // =========================================

        private void txtCodigoProducto_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string codigo =
                    txtCodigoProducto.Text.Trim();

                AgregarAlCarrito(
                    codigo,
                    carritoCentral,
                    esCentral: true);

                txtCodigoProducto.Clear();
            }
        }

        // =========================================
        // LÓGICA COMPARTIDA — AGREGAR AL CARRITO
        // =========================================

        private void AgregarAlCarrito(
            string codigo,
            List<VentaItem> carrito,
            bool esCentral)
        {
            if (string.IsNullOrEmpty(codigo))
                return;

            var producto =
                productos.FirstOrDefault(
                    p => p.CodigoBarras == codigo);

            if (producto == null)
            {
                MessageBox.Show(
                    "Producto no encontrado",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var existente =
                carrito.FirstOrDefault(
                    x => x.ProductoId == producto.Id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                carrito.Add(new VentaItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.PrecioVenta,
                    Cantidad = 1
                });
            }

            if (esCentral)
                ActualizarCarritoCentral();
            else
                ActualizarDashboardPOS();
        }

        // =========================================
        // ACTUALIZAR POS RÁPIDO
        // =========================================

        private void ActualizarDashboardPOS()
        {
            dgCarritoDashboard.ItemsSource = null;
            dgCarritoDashboard.ItemsSource = carritoDashboard;

            decimal total =
                carritoDashboard.Sum(x => x.Subtotal);

            txtTotalDashboard.Text =
                total.ToString("C");
        }

        // =========================================
        // ACTUALIZAR CARRITO CENTRAL
        // =========================================

        private void ActualizarCarritoCentral()
        {
            dgCarritoCentral.ItemsSource = null;
            dgCarritoCentral.ItemsSource = carritoCentral;

            decimal total =
                carritoCentral.Sum(x => x.Subtotal);

            // Total grande
            txtTotalCentral.Text =
                total.ToString("C");

            // Footer
            txtFooterTotal.Text =
                $"Total: {total:C}";

            txtFooterPago.Text =
                "Pago: $0.00";

            txtFooterCambio.Text =
                "Cambio: $0.00";
        }

        // =========================================
        // BOTONES DEL ÁREA CENTRAL
        // =========================================

        private void BtnMasCant_Click(
            object sender,
            RoutedEventArgs e)
        {
            var seleccionado =
                dgCarritoCentral.SelectedItem
                as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show(
                    "Selecciona un producto de la lista");
                return;
            }

            seleccionado.Cantidad++;
            ActualizarCarritoCentral();
        }

        private void BtnCantidad_Click(
            object sender,
            RoutedEventArgs e)
        {
            var seleccionado =
                dgCarritoCentral.SelectedItem
                as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show(
                    "Selecciona un producto de la lista");
                return;
            }

            string input =
                Microsoft.VisualBasic.Interaction
                .InputBox(
                    "Nueva cantidad:",
                    "Cambiar cantidad",
                    seleccionado.Cantidad.ToString());

            if (int.TryParse(input, out int nuevaCant)
                && nuevaCant > 0)
            {
                seleccionado.Cantidad = nuevaCant;
                ActualizarCarritoCentral();
            }
        }

        private void BtnPrecio_Click(
            object sender,
            RoutedEventArgs e)
        {
            var seleccionado =
                dgCarritoCentral.SelectedItem
                as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show(
                    "Selecciona un producto de la lista");
                return;
            }

            string input =
                Microsoft.VisualBasic.Interaction
                .InputBox(
                    "Nuevo precio:",
                    "Cambiar precio",
                    seleccionado.Precio.ToString());

            if (decimal.TryParse(input, out decimal nuevoPrecio)
                && nuevoPrecio > 0)
            {
                seleccionado.Precio = nuevoPrecio;
                ActualizarCarritoCentral();
            }
        }

        private void BtnDescuento_Click(
            object sender,
            RoutedEventArgs e)
        {
            var seleccionado =
                dgCarritoCentral.SelectedItem
                as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show(
                    "Selecciona un producto de la lista");
                return;
            }

            string input =
                Microsoft.VisualBasic.Interaction
                .InputBox(
                    "Descuento en $:",
                    "Aplicar descuento",
                    "0");

            if (decimal.TryParse(input, out decimal descuento)
                && descuento >= 0)
            {
                seleccionado.Descuento = descuento;
                ActualizarCarritoCentral();
            }
        }

        private void BtnBuscar_Click(
            object sender,
            RoutedEventArgs e)
        {
            string input =
                Microsoft.VisualBasic.Interaction
                .InputBox(
                    "Nombre o código del producto:",
                    "Buscar producto");

            if (string.IsNullOrEmpty(input))
                return;

            var resultado =
                productos.Where(
                    p =>
                    p.Nombre.Contains(
                        input,
                        StringComparison.OrdinalIgnoreCase)
                    || p.CodigoBarras.Contains(input))
                .ToList();

            if (resultado.Count == 0)
            {
                MessageBox.Show("No se encontraron productos");
                return;
            }

            // Tomar el primero y agregarlo
            AgregarAlCarrito(
                resultado.First().CodigoBarras,
                carritoCentral,
                esCentral: true);
        }

        private void BtnEliminar_Click(
            object sender,
            RoutedEventArgs e)
        {
            var seleccionado =
                dgCarritoCentral.SelectedItem
                as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show(
                    "Selecciona un producto para eliminar");
                return;
            }

            carritoCentral.Remove(seleccionado);
            ActualizarCarritoCentral();
        }

        private void BtnPagar_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Redirige al flujo de cobro central
            BtnCobrarCentral_Click(sender, e);
        }

        // =========================================
        // COBRAR — ÁREA CENTRAL
        // =========================================

        private void BtnCobrarCentral_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (carritoCentral.Count == 0)
            {
                MessageBox.Show(
                    "No hay productos en el carrito",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            decimal total =
                carritoCentral.Sum(x => x.Subtotal);

            string inputPago =
                Microsoft.VisualBasic.Interaction
                .InputBox(
                    $"Total: {total:C}\nIngresa el monto recibido:",
                    "Cobrar");

            if (!decimal.TryParse(inputPago, out decimal pago)
                || pago < total)
            {
                MessageBox.Show(
                    "Monto insuficiente o inválido");
                return;
            }

            decimal cambio = pago - total;

            // Actualizar footer
            txtFooterTotal.Text = $"Total: {total:C}";
            txtFooterPago.Text = $"Pago: {pago:C}";
            txtFooterCambio.Text = $"Cambio: {cambio:C}";

            MessageBox.Show(
                $"✅ Venta realizada\n\nTotal:  {total:C}\nPago:   {pago:C}\nCambio: {cambio:C}",
                "Venta exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Limpiar carrito
            carritoCentral.Clear();
            ActualizarCarritoCentral();
        }

        // =========================================
        // COBRAR — POS RÁPIDO
        // =========================================

        private void BtnCobrarDashboard_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (carritoDashboard.Count == 0)
            {
                MessageBox.Show("No hay productos");
                return;
            }

            decimal total =
                carritoDashboard.Sum(x => x.Subtotal);

            MessageBox.Show(
                $"Venta realizada\nTotal: {total:C}",
                "POS Rápido",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            carritoDashboard.Clear();
            ActualizarDashboardPOS();
        }

        // =========================================
        // NAVEGACIÓN
        // =========================================

        private void BtnProductos_Click(
            object sender,
            RoutedEventArgs e)
        {
            ProductosWindow ventana =
                new ProductosWindow();

            ventana.ShowDialog();
        }

        private void BtnVentas_Click(
            object sender,
            RoutedEventArgs e)
        {
            VentasWindow ventas =
                new VentasWindow();

            ventas.ShowDialog();
        }

        private void BtnInventario_Click(
            object sender,
            RoutedEventArgs e)
        {
            InventarioWindow inventario =
                new InventarioWindow();

            inventario.ShowDialog();
        }

        private void BtnReportes_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                ReportesWindow reportes =
                    new ReportesWindow();

                reportes.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString());
            }
        }

        private void BtnPedidos_Click(
            object sender,
            RoutedEventArgs e)
        {
            PedidosWindow pedidos =
                new PedidosWindow();

            pedidos.ShowDialog();
        }

        private void BtnConfiguracion_Click(
            object sender,
            RoutedEventArgs e)
        {
            ConfiguracionWindow config =
                new ConfiguracionWindow();

            config.ShowDialog();
        }

        private void BtnSalir_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBoxResult resultado =
                MessageBox.Show(
                    "¿Deseas cerrar sesión?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }

        private void txtEscaner_TextChanged(
            object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
        }

    }
}