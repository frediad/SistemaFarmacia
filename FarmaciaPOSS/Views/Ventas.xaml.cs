using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    public partial class VentasWindow : Window
    {
        private List<Producto> productos = new();
        private ObservableCollection<VentaItem> carrito = new();

        public VentasWindow()
        {
            InitializeComponent();

            dgCarrito.ItemsSource = carrito;

            CargarProductos();
            CargarCategoriasCatalogo();
            CargarCatalogo();
            ActualizarTotales();
        }

        // =========================================
        // ✅ CARGAR PRODUCTOS
        // =========================================

        private void CargarProductos()
        {
            productos.Clear();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query = "SELECT * FROM Productos WHERE Activo = 1";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(new Producto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    CodigoBarras = reader["CodigoBarras"].ToString(),
                    Nombre = reader["Nombre"].ToString(),
                    Stock = Convert.ToInt32(reader["Stock"]),
                    PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"]),
                    ImagenURL = reader["ImagenURL"].ToString(),
                    CategoriaId = reader["CategoriaId"] != DBNull.Value
                        ? Convert.ToInt32(reader["CategoriaId"])
                        : 0,
                });
            }
        }

        // =========================================
        // ✅ CATEGORÍAS
        // =========================================

        private void CargarCategoriasCatalogo()
        {
            pnlCategorias.Children.Clear();

            var btnTodos = new Button
            {
                Content = "🏠 Todos",
                Style = (Style)FindResource("BtnCategoriaActiva"),
                Tag = 0
            };
            btnTodos.Click += BtnCategoria_Click;
            pnlCategorias.Children.Add(btnTodos);

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query = "SELECT * FROM Categorias ORDER BY Nombre";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var btn = new Button
                {
                    Content = reader["Nombre"].ToString(),
                    Style = (Style)FindResource("BtnCategoria"),
                    Tag = Convert.ToInt32(reader["Id"])
                };
                btn.Click += BtnCategoria_Click;
                pnlCategorias.Children.Add(btn);
            }
        }

        private void BtnCategoria_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            int categoriaId = Convert.ToInt32(btn?.Tag ?? 0);

            foreach (Button b in pnlCategorias.Children.OfType<Button>())
                b.Style = (Style)FindResource("BtnCategoria");

            btn!.Style = (Style)FindResource("BtnCategoriaActiva");

            icProductosCatalogo.ItemsSource =
                categoriaId == 0
                    ? productos
                    : productos.Where(p => p.CategoriaId == categoriaId).ToList();
        }

        private void CargarCatalogo()
        {
            icProductosCatalogo.ItemsSource = productos;
        }

        // =========================================
        // ✅ CLIC EN TARJETA DE PRODUCTO
        // =========================================

        private void CardProducto_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Producto producto)
            {
                AgregarProductoAlCarrito(producto);
            }
        }

        private void AgregarProductoAlCarrito(Producto producto)
        {
            var ventana = new CantidadWindow(producto)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado != true)
                return;

            int cantidad = ventana.CantidadSeleccionada;

            var existente = carrito.FirstOrDefault(x => x.ProductoId == producto.Id);

            if (existente != null)
            {
                existente.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new VentaItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.PrecioVenta,
                    Cantidad = cantidad,
                    Stock = producto.Stock,
                });
            }

            ActualizarTotales();
        }

        // =========================================
        // ✅ TOTALES
        // =========================================

        private void ActualizarTotales()
        {
            decimal total = carrito.Sum(x => x.Subtotal);

            txtTotal.Text = total.ToString("C");
            txtPago.Text = "$0.00";
            txtCambio.Text = "$0.00";
        }

        // =========================================
        // ✅ ACCIONES DEL TICKET
        // =========================================

        private void BtnMasCant_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgCarrito.SelectedItem as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show("Selecciona un producto de la lista");
                return;
            }

            seleccionado.Cantidad++;
            ActualizarTotales();
        }

        private void BtnCantidad_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgCarrito.SelectedItem as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show("Selecciona un producto de la lista");
                return;
            }

            string input =
                Microsoft.VisualBasic.Interaction.InputBox(
                    "Nueva cantidad:",
                    "Cambiar cantidad",
                    seleccionado.Cantidad.ToString());

            if (int.TryParse(input, out int nuevaCant) && nuevaCant > 0)
            {
                seleccionado.Cantidad = nuevaCant;
                ActualizarTotales();
            }
        }

        private void BtnDescuento_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgCarrito.SelectedItem as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show("Selecciona un producto de la lista");
                return;
            }

            string input =
                Microsoft.VisualBasic.Interaction.InputBox(
                    "Descuento en $:",
                    "Aplicar descuento",
                    "0");

            if (decimal.TryParse(input, out decimal descuento) && descuento >= 0)
            {
                seleccionado.Descuento = descuento;
                ActualizarTotales();
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgCarrito.SelectedItem as VentaItem;

            if (seleccionado == null)
            {
                MessageBox.Show("Selecciona un producto para eliminar");
                return;
            }

            carrito.Remove(seleccionado);
            ActualizarTotales();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
                return;

            var confirmar = MessageBox.Show(
                "¿Cancelar la venta actual? Se perderán los productos del ticket.",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar == MessageBoxResult.Yes)
            {
                carrito.Clear();
                ActualizarTotales();
            }
        }

        private void BtnGenerarTicket_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MessageBox.Show("No hay productos en el ticket");
                return;
            }

            // Aquí puedes conectar tu lógica real de impresión/generación de ticket
            MessageBox.Show(
                "Función de impresión de ticket pendiente de conectar con la impresora.",
                "Generar Ticket",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================
        // ✅ COBRAR
        // =========================================

        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {

            // ✅ Descontar stock y registrar movimientos de salida
            InventarioHelper.DescontarStockPorVenta(carrito, Sesion.UsuarioId);

            if (carrito.Count == 0)
            {
                MessageBox.Show(
                    "No hay productos en el carrito",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            decimal total = carrito.Sum(x => x.Subtotal);

            string inputPago =
                Microsoft.VisualBasic.Interaction.InputBox(
                    $"Total: {total:C}\nIngresa el monto recibido:",
                    "Cobrar");

            if (!decimal.TryParse(inputPago, out decimal pago) || pago < total)
            {
                MessageBox.Show("Monto insuficiente o inválido");
                return;
            }

            decimal cambio = pago - total;

            txtPago.Text = pago.ToString("C");
            txtCambio.Text = cambio.ToString("C");

            MessageBox.Show(
                $"✅ Venta realizada\n\nTotal:  {total:C}\nPago:   {pago:C}\nCambio: {cambio:C}",
                "Venta exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            carrito.Clear();
            ActualizarTotales();
        }

        // =========================================
        // ✅ NAVEGACIÓN / ATAJOS
        // =========================================

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void VentasWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    BtnCantidad_Click(sender, new RoutedEventArgs());
                    break;

                case Key.F6:
                    BtnEliminar_Click(sender, new RoutedEventArgs());
                    break;

                case Key.F7:
                    BtnDescuento_Click(sender, new RoutedEventArgs());
                    break;

                case Key.Escape:
                    this.Close();
                    break;
            }
        }
    }
}