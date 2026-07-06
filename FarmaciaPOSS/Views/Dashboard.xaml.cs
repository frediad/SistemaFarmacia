using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using FarmaciaPOS.Views;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FarmaciaPOS
{
    public partial class MainWindow : Window
    {
        

        List<Producto> productos =
            new();

        ObservableCollection<VentaItem> carritoCentral = new();

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public MainWindow()
        {
            InitializeComponent();

            txtUsuarioSesion.Text =
               Sesion.NombreUsuario;
           txtRolId.Text =
               Sesion.RolId.ToString();

            CargarProductos();

            InicializarCarritoCentral();

            IniciarReloj();

            CargarCategoriasCatalogo();  
            CargarCatalogo();

            AplicarPermisosEnMenu();



        }

        private DispatcherTimer relojTimer;

        private void IniciarReloj()
        {
            ActualizarFechaHora();

            relojTimer = new DispatcherTimer();
            relojTimer.Interval = TimeSpan.FromSeconds(1);
            relojTimer.Tick += (s, e) => ActualizarFechaHora();
            relojTimer.Start();
        }

        private void ActualizarFechaHora()
        {
            txtFechaHora.Text = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt");
        }

        private void AplicarPermisosEnMenu()
        {
            PermisosHelper.AplicarPermisosEnMenu(
                btnVentas,
                btnPedidos,
                btnProductos,
                btnInventario,
                btnReportes,      // asegúrate que este botón tiene x:Name="btnReportes"
                btnConfiguracion,
                btnCaja);
        }


        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        private void CargarProductos()
        {
            productos.Clear();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

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

                    Stock =
                        Convert.ToInt32(
                            reader["Stock"]),

                    PrecioVenta =
                        Convert.ToDecimal(
                            reader["PrecioVenta"]),

                    ImagenURL =
                        reader["ImagenURL"]
                        .ToString(),

                    CategoriaId =
                        reader["CategoriaId"] != DBNull.Value
                        ? Convert.ToInt32(reader["CategoriaId"])
                        : 0,
                });
            }
        }

        // =========================================
        // INICIALIZAR CARRITO CENTRAL
        // =========================================

        private void InicializarCarritoCentral()
        {
            dgCarritoCentral.ItemsSource = carritoCentral;
            ActualizarCarritoCentral();
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

                AgregarAlCarrito(codigo);

                txtCodigoProducto.Clear();
            }
        }

        // =========================================
        // AGREGAR AL CARRITO
        // =========================================

        private void AgregarAlCarrito(string codigo)
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

            // ✅ Mostrar nombre e imagen del producto escaneado
            txtNombreProductoActual.Text = producto.Nombre;
            CargarImagenProductoActual(producto.ImagenURL);

            var existente =
                carritoCentral.FirstOrDefault(
                    x => x.ProductoId == producto.Id);

            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                carritoCentral.Add(new VentaItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.PrecioVenta,
                    Cantidad = 1,
                    Stock = producto.Stock,
                });
            }

            ActualizarCarritoCentral();
        }

        // =========================================
        // MOSTRAR IMAGEN DEL PRODUCTO
        // =========================================

        private void CargarImagenProductoActual(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                imgProductoActual.Source = null;
                return;
            }

            try
            {
                imgProductoActual.Source =
                    new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(ruta));
            }
            catch
            {
                imgProductoActual.Source = null;
            }
        }

        // =========================================
        // ACTUALIZAR CARRITO CENTRAL
        // =========================================

        private void ActualizarCarritoCentral()
        {
            decimal total =
                carritoCentral.Sum(x => x.Subtotal);

            txtTotalCentral.Text =
                total.ToString("C");

            txtFooterTotal.Text = $"Total: {total:C}";
            txtFooterPago.Text = "Pago: $0.00";
            txtFooterCambio.Text = "Cambio: $0.00";
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

            AgregarAlCarrito(
                resultado.First().CodigoBarras);
        }

        private List<VentaEnEspera> ventasEnEspera = new();
        private int contadorEspera = 0;
        private void BtnEspera_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (carritoCentral.Count == 0)
            {
                // No hay nada que poner en espera → mostrar la lista para recuperar una
                AbrirVentasEnEspera();
                return;
            }

            VentaEnEspera ventaEspera = new VentaEnEspera
            {
                Id = ++contadorEspera,
                Referencia = $"VE-{DateTime.Now:yyyyMMddHHmmss}",
                Items = carritoCentral.ToList()
            };

            ventasEnEspera.Add(ventaEspera);

            carritoCentral.Clear();
            ActualizarCarritoCentral();

            txtNombreProductoActual.Text = "";
            imgProductoActual.Source = null;

            ActualizarBadgeEspera();

            MessageBox.Show(
                $"Venta \"{ventaEspera.Referencia}\" puesta en espera",
                "En espera",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================
        // FUNCIONES DE ESPERA
        // =========================================

        private void AbrirVentasEnEspera()
        {
            var ventana = new VentasEnEsperaWindow(ventasEnEspera)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true && ventana.VentaSeleccionada != null)
                RecuperarVentaEnEspera(ventana.VentaSeleccionada);

            ActualizarBadgeEspera();
        }

        private void RecuperarVentaEnEspera(VentaEnEspera venta)
        {
            carritoCentral.Clear();

            foreach (var item in venta.Items)
                carritoCentral.Add(item);

            ActualizarCarritoCentral();

            MessageBox.Show(
                $"Venta \"{venta.Referencia}\" recuperada",
                "Venta recuperada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ActualizarBadgeEspera()
        {
            if (ventasEnEspera.Count > 0)
            {
                txtBadgeEspera.Text = ventasEnEspera.Count.ToString();
                badgeEspera.Visibility = Visibility.Visible;
            }
            else
            {
                badgeEspera.Visibility = Visibility.Collapsed;
            }
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

            InventarioHelper.DescontarStockPorVenta(carritoCentral, Sesion.UsuarioId);
            BtnCobrarCentral_Click(sender, e);
        }

        // =========================================
        // COBRAR
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

            txtFooterTotal.Text = $"Total: {total:C}";
            txtFooterPago.Text = $"Pago: {pago:C}";
            txtFooterCambio.Text = $"Cambio: {cambio:C}";

            MessageBox.Show(
                $"✅ Venta realizada\n\nTotal:  {total:C}\nPago:   {pago:C}\nCambio: {cambio:C}",
                "Venta exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            carritoCentral.Clear();
            ActualizarCarritoCentral();
        }

        // =========================================
        // NAVEGACIÓN
        // =========================================

        private void BtnProductos_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Productos"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            ProductosWindow ventana =
                new ProductosWindow();

            ventana.ShowDialog();
        }

        private void BtnVentas_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Ventas"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            VentasWindow ventas =
                new VentasWindow();

            ventas.ShowDialog();
        }

        private void BtnInventario_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Inventario"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            InventarioWindow inventario =
                new InventarioWindow();

            inventario.ShowDialog();
        }

        private void BtnReportes_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Reportes"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            ReportesWindow reporte =
                new ReportesWindow();

            reporte.ShowDialog();
        }

        private void BtnPedidos_Click(
            object sender,
            RoutedEventArgs e)
        {
            if(!PermisosHelper.TieneAcceso("Pedidos"))
    {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            PedidosWindow pedidos =
                new PedidosWindow();

            pedidos.ShowDialog();
        }

        private void BtnConfiguracion_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Configuración"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            ConfiguracionWindow configuracion
                = new ConfiguracionWindow();

            configuracion.ShowDialog();

        }

        private void BtnCaja_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Caja"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            CajaWindow caja =
                new CajaWindow();

            caja.ShowDialog();
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

        private void dgCarritoCentral_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void dgCarritoCentral_SelectionChanged_1(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private void DashboardWindow_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            Keyboard.Focus(this);

            switch (e.Key)
            {
                case Key.F2:

                    BtnBuscar_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F3:

                    BtnPrecio_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F5:

                    BtnCantidad_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F6:

                    BtnEliminar_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F7:

                    BtnDescuento_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.Escape:

                    this.Close();

                    break;
            }
        }

        
        // =========================================
        // ✅ CARGAR CATEGORÍAS EN LA BARRA
        // =========================================

        private void CargarCategoriasCatalogo()
        {
            pnlCategorias.Children.Clear();

            // Botón "Todos"
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

            // Resaltar botón activo
            foreach (Button b in pnlCategorias.Children.OfType<Button>())
            {
                b.Style = (Style)FindResource("BtnCategoria");
            }
            btn!.Style = (Style)FindResource("BtnCategoriaActiva");

            // Filtrar productos
            if (categoriaId == 0)
                icProductosCatalogo.ItemsSource = productos;
            else
                icProductosCatalogo.ItemsSource = productos
                    .Where(p => p.CategoriaId == categoriaId)
                    .ToList();
        }

        // =========================================
        // ✅ CARGAR CATÁLOGO DE PRODUCTOS
        // =========================================

        private void CargarCatalogo()
        {
            icProductosCatalogo.ItemsSource = productos;
        }

        // =========================================
        // ✅ AGREGAR PRODUCTO DESDE EL CATÁLOGO (CON CANTIDAD)
        // =========================================

        private void AgregarProductoDesdeCatalogo(Producto producto)
        {
            if (producto == null)
                return;

            var ventana = new CantidadWindow(producto)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado != true)
                return; // El usuario canceló

            int cantidad = ventana.CantidadSeleccionada;

            var existente =
                carritoCentral.FirstOrDefault(
                    x => x.ProductoId == producto.Id);

            if (existente != null)
            {
                existente.Cantidad += cantidad;
            }
            else
            {
                carritoCentral.Add(new VentaItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Precio = producto.PrecioVenta,
                    Cantidad = cantidad,
                    Stock = producto.Stock,
                });
            }

            // ✅ Mostrar nombre e imagen del producto agregado
            txtNombreProductoActual.Text = producto.Nombre;
            CargarImagenProductoActual(producto.ImagenURL);

            ActualizarCarritoCentral();
        }

        // =========================================
        // ✅ CLIC EN TARJETA DE PRODUCTO DEL CATÁLOGO
        // =========================================

        private void CardProducto_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Producto producto)
            {
                AgregarProductoDesdeCatalogo(producto);
            }
        }



    }
}