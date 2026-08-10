using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using FarmaciaPOS.Views;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        List<Producto> productos = new();

        ObservableCollection<VentaItem> carritoCentral = new();

        private Dictionary<int, int> ventasPorProducto = new();

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public MainWindow()
        {
            InitializeComponent();

                txtUsuarioSesion.Text = Sesion.NombreUsuario;
                txtCargoSesion.Text = Sesion.Rol;

                CargarProductos();
                CargarVentasPorProducto();

                InicializarCarritoCentral();

                IniciarReloj();

                CargarCategoriasCatalogo();
                CargarCatalogo();

                AplicarPermisosEnMenu();
        }
        
        private DispatcherTimer relojTimer;

        private bool cierreConfirmadoPorBoton = false;

        private FiltroCatalogo filtroActivo = new FiltroCatalogo { Tipo = "Todos", Id = 0 };
        private int? productoActualMostradoId = null;

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
                btnReportes,
                btnConfiguracion,
                btnCaja,
                btnDevoluciones,
                btnClientes);
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                CargarProductos();
                CargarVentasPorProducto();
                CargarCategoriasCatalogo();

                AplicarFiltroCatalogo();

                foreach (var item in carritoCentral)
                {
                    var productoActual = productos.FirstOrDefault(p => p.Id == item.ProductoId);
                    if (productoActual != null)
                    {
                        item.Nombre = productoActual.Nombre;
                        item.Precio = productoActual.PrecioVenta;
                        item.Stock = productoActual.Stock;
                    }
                }

                if (productoActualMostradoId.HasValue)
                {
                    var productoMostrado = productos.FirstOrDefault(p => p.Id == productoActualMostradoId.Value);
                    if (productoMostrado != null)
                    {
                        txtNombreProductoActual.Text = productoMostrado.Nombre;
                        CargarImagenProductoActual(productoMostrado.ImagenBytes);
                    }
                }

                AplicarPermisosEnMenu();

                ActualizarCarritoCentral();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(
                    "No se pudo actualizar la información: " + ex.Message,
                    "Error",
                    this);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
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
                @"SELECT p.*,
                (SELECT TOP 1 img.ImagenData
                FROM ImagenesProducto img
                WHERE img.ProductoId = p.Id
                ORDER BY img.Orden) AS PrimeraImagenData
                FROM Productos p
                WHERE p.Activo = 1
                ORDER BY p.Nombre";

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

                        Precio2 =
                             Convert.ToDecimal(
                                 reader["Precio2"]),
                        Precio3 =
                             Convert.ToDecimal(
                                reader["Precio3"]),

                        CantidadMayoreo2 =
                             Convert.ToInt32(
                                 reader["CantidadMayoreo2"]),

                        CantidadMayoreo3 =
                             Convert.ToInt32(
                                 reader["CantidadMayoreo3"]),

                        ImagenBytes = reader["PrimeraImagenData"] != DBNull.Value
                            ? (byte[])reader["PrimeraImagenData"]
                            : null,

                        CategoriaId =
                            reader["CategoriaId"] != DBNull.Value
                            ? Convert.ToInt32(reader["CategoriaId"])
                            : 0,
                    });
                }
        }

        // =========================================
        // ✅ VENTAS POR PRODUCTO (para ordenar el catálogo por más vendidos)
        // =========================================

        private void CargarVentasPorProducto()
        {
            ventasPorProducto.Clear();

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query =
                @"SELECT dv.ProductoId, SUM(dv.Cantidad) AS TotalVendido
                  FROM DetalleVentas dv
                  INNER JOIN Ventas v ON dv.VentaId = v.Id
                  WHERE v.Estado = 'Completada'
                  GROUP BY dv.ProductoId";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ventasPorProducto[Convert.ToInt32(reader["ProductoId"])] =
                        Convert.ToInt32(reader["TotalVendido"]);
                }
            }
            catch { }
        }

        private int VentasDe(Producto p) =>
            ventasPorProducto.TryGetValue(p.Id, out int total) ? total : 0;


        // =========================================
        // ✅ CARGAR CATÁLOGO DE PRODUCTOS (ordenado por más vendidos)
        // =========================================

        private void CargarCatalogo()
        {
            icProductosCatalogo.ItemsSource = productos
                .OrderByDescending(p => VentasDe(p))
                .ThenBy(p => p.Nombre)
                .ToList();
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
                MensajeHelper.Advertencia("Producto no encontrado", "Aviso", this);
                return;
            }

            txtNombreProductoActual.Text = producto.Nombre;
            CargarImagenProductoActual(producto.ImagenBytes);
            productoActualMostradoId = producto.Id;

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

        private void CargarImagenProductoActual(byte[]? imagenBytes)
        {
            if (imagenBytes == null || imagenBytes.Length == 0)
            {
                imgProductoActual.Source = null;
                return;
            }

            try
            {
                using var stream = new System.IO.MemoryStream(imagenBytes);

                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                imgProductoActual.Source = bitmap;
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
                MensajeHelper.Advertencia("Selecciona un producto de la lista", "Aviso", this);
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
                MensajeHelper.Advertencia("Selecciona un producto de la lista", "Aviso", this);
                return;
            }


            var producto = productos.FirstOrDefault(p => p.Id == seleccionado.ProductoId);

            if (producto == null)
            {
                MensajeHelper.Error("No se encontró la información del producto", "Error", this);
                return;
            }

            var ventana = new CantidadWindow(producto)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true)
            {
                seleccionado.Cantidad = ventana.CantidadSeleccionada;
                ActualizarCarritoCentral();
            }
        }
        private void BtnPrecio_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgCarritoCentral.SelectedItem as VentaItem;

            if (seleccionado == null)
            {
                MensajeHelper.Advertencia("Selecciona un producto de la lista", "Aviso", this);
                return;
            }

            var producto = productos.FirstOrDefault(
                p => p.Id == seleccionado.ProductoId);

            if (producto == null)
                return;

            var ventana = new SeleccionarPrecioWindow(producto);
            ventana.Owner = this;

            if (ventana.ShowDialog() == true)
            {
                seleccionado.Precio = ventana.PrecioSeleccionado;

                switch (ventana.TipoPrecio)
                {
                    case 2:
                        if (seleccionado.Cantidad < producto.CantidadMayoreo2
                            && producto.CantidadMayoreo2 > 0)
                        {
                            seleccionado.Cantidad = producto.CantidadMayoreo2;
                        }
                        break;

                    case 3:
                        if (seleccionado.Cantidad < producto.CantidadMayoreo3
                            && producto.CantidadMayoreo3 > 0)
                        {
                            seleccionado.Cantidad = producto.CantidadMayoreo3;
                        }
                        break;

                    default:
                        break;
                }

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
                MensajeHelper.Advertencia("Selecciona un producto de la lista", "Aviso", this);
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
            var ventana = new BuscarProductoWindow(productos)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true && ventana.ProductoSeleccionado != null)
            {
                AgregarProductoDesdeCatalogo(ventana.ProductoSeleccionado);
            }
        }

        private List<VentaEnEspera> ventasEnEspera = new();

        private void BtnEspera_Click(
            object sender,
            RoutedEventArgs e)
        {
            AbrirVentasEnEspera();
        }

        // =========================================
        // FUNCIONES DE ESPERA
        // =========================================

        private void AbrirVentasEnEspera()
        {
            var ventana = new VentasEnEsperaWindow(
                ventasEnEspera,
                carritoCentral.ToList())
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (ventana.VentaActualGuardada)
            {
                carritoCentral.Clear();
                ActualizarCarritoCentral();

                txtNombreProductoActual.Text = "";
                imgProductoActual.Source = null;
                productoActualMostradoId = null;
            }

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

            if (venta.Items.Count > 0)
            {
                var ultimoItem = venta.Items.Last();
                var producto = productos.FirstOrDefault(p => p.Id == ultimoItem.ProductoId);

                if (producto != null)
                {
                    txtNombreProductoActual.Text = producto.Nombre;
                    CargarImagenProductoActual(producto.ImagenBytes);
                    productoActualMostradoId = producto.Id;
                }
            }

            MensajeHelper.Exito($"Venta \"{venta.Referencia}\" recuperada", "Venta recuperada", this);
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
                MensajeHelper.Advertencia("Selecciona un producto para eliminar", "Aviso", this);
                return;
            }

            carritoCentral.Remove(seleccionado);
            ActualizarCarritoCentral();
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
                MensajeHelper.Advertencia("No hay productos en el carrito", "Aviso", this);
                return;
            }

            var ventana = new Cobrar(carritoCentral)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true && ventana.VentaCompletada)
            {
                carritoCentral.Clear();
                ActualizarCarritoCentral();

                txtNombreProductoActual.Text = "";
                imgProductoActual.Source = null;
                productoActualMostradoId = null;

                CargarProductos();
                CargarVentasPorProducto();
                AplicarFiltroCatalogo();
            }
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

            ventana.Show();
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

            ventas.Show();
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
            if (!PermisosHelper.TieneAcceso("Pedidos"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            PedidosWindow pedidos =
                new PedidosWindow();

            pedidos.Show();
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

            caja.Show();
        }

        private void BtnLateralDevoluciones_Click(object sender, RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Devoluciones"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            var ventana = new DevolucionesWindow();
            ventana.Show();
        }

        private void BtnLateralClientes_Click(object sender, RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Clientes"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            var ventana = new ClientesWindow();
            ventana.Show();
        }

        private void BtnSalir_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool confirmar = MensajeHelper.Confirmar(
                "¿Deseas cerrar sesión?",
                "Confirmar",
                this);

            if (confirmar)
            {
                cierreConfirmadoPorBoton = true;

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

                    BtnEspera_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F4:

                    BtnLateralDevoluciones_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F5:

                    BtnPrecio_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F6:

                    BtnCantidad_Click(
                        sender,
                        new RoutedEventArgs());

                    break;

                case Key.F7:
                    BtnEliminar_Click(
                        sender,
                        new RoutedEventArgs());
                    break;

                case Key.F8:
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

            var btnTodos = new Button
            {
                Content = "🏠 Todos",
                Style = (Style)FindResource("BtnCategoriaActiva"),
                Tag = new FiltroCatalogo { Tipo = "Todos", Id = 0 }
            };
            btnTodos.Click += BtnCategoria_Click;
            pnlCategorias.Children.Add(btnTodos);

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            var categorias = new List<(int Id, string Nombre)>();
            string queryCat = "SELECT * FROM Categorias ORDER BY Nombre";
            SqlCommand cmdCat = new SqlCommand(queryCat, conn);
            using (SqlDataReader readerCat = cmdCat.ExecuteReader())
            {
                while (readerCat.Read())
                {
                    categorias.Add((
                        Convert.ToInt32(readerCat["Id"]),
                        readerCat["Nombre"].ToString()));
                }
            }

            var subcategoriasPorCategoria = new Dictionary<int, List<(int Id, string Nombre)>>();
            string querySub = "SELECT * FROM Subcategorias ORDER BY Nombre";
            SqlCommand cmdSub = new SqlCommand(querySub, conn);
            using (SqlDataReader readerSub = cmdSub.ExecuteReader())
            {
                while (readerSub.Read())
                {
                    int categoriaId = Convert.ToInt32(readerSub["CategoriaId"]);
                    int subId = Convert.ToInt32(readerSub["Id"]);
                    string nombreSub = readerSub["Nombre"].ToString();

                    if (!subcategoriasPorCategoria.ContainsKey(categoriaId))
                        subcategoriasPorCategoria[categoriaId] = new List<(int Id, string Nombre)>();

                    subcategoriasPorCategoria[categoriaId].Add((subId, nombreSub));
                }
            }

            foreach (var cat in categorias)
            {
                var btnCat = new Button
                {
                    Content = cat.Nombre,
                    Style = (Style)FindResource("BtnCategoria"),
                    Tag = new FiltroCatalogo { Tipo = "Categoria", Id = cat.Id }
                };
                btnCat.Click += BtnCategoria_Click;
                pnlCategorias.Children.Add(btnCat);

                if (subcategoriasPorCategoria.TryGetValue(cat.Id, out var subs))
                {
                    foreach (var sub in subs)
                    {
                        var btnSub = new Button
                        {
                            Content = "" + sub.Nombre,
                            Style = (Style)FindResource("BtnCategoria"),
                            Tag = new FiltroCatalogo { Tipo = "Subcategoria", Id = sub.Id }
                        };
                        btnSub.Click += BtnCategoria_Click;
                        pnlCategorias.Children.Add(btnSub);
                    }
                }
            }
        }

        private void BtnCategoria_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var filtro = btn?.Tag as FiltroCatalogo;

            filtroActivo = filtro ?? new FiltroCatalogo { Tipo = "Todos", Id = 0 };

            AplicarFiltroCatalogo();
        }


        // ✅ Ahora ordena por más vendidos (descendente), y por nombre como criterio secundario
        private void AplicarFiltroCatalogo()
        {
            foreach (Button b in pnlCategorias.Children.OfType<Button>())
            {
                var tagBtn = b.Tag as FiltroCatalogo;
                bool esActivo =
                    (tagBtn?.Tipo == filtroActivo.Tipo) &&
                    (tagBtn?.Id == filtroActivo.Id);

                b.Style = (Style)FindResource(esActivo ? "BtnCategoriaActiva" : "BtnCategoria");
            }

            IEnumerable<Producto> filtrados = productos;

            if (filtroActivo.Tipo == "Categoria")
            {
                filtrados = productos.Where(p => p.CategoriaId == filtroActivo.Id);
            }
            else if (filtroActivo.Tipo == "Subcategoria")
            {
                filtrados = productos.Where(p => p.SubcategoriaId == filtroActivo.Id);
            }

            icProductosCatalogo.ItemsSource = filtrados
                .OrderByDescending(p => VentasDe(p))
                .ThenBy(p => p.Nombre)
                .ToList();
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
                return;

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

            txtNombreProductoActual.Text = producto.Nombre;
            CargarImagenProductoActual(producto.ImagenBytes);
            productoActualMostradoId = producto.Id;

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

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (cierreConfirmadoPorBoton)
            {
                relojTimer?.Stop();
                return;
            }

            bool confirmar = MensajeHelper.Confirmar(
                "¿Deseas cerrar sesión?",
                "Confirmar",
                this);

            if (confirmar)
            {
                relojTimer?.Stop();

                LoginWindow login = new LoginWindow();
                login.Show();
            }
            else
            {
                e.Cancel = true;
            }
        }



    }
}