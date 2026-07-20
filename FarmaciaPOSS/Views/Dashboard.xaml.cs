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

            txtCargoSesion.Text = 
               Sesion.Rol;

            CargarProductos();

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
                CargarCategoriasCatalogo();

                // ✅ Reaplica la categoría/subcategoría que el cajero tenía seleccionada
                AplicarFiltroCatalogo();

                // ✅ Refresca Nombre, Precio y Stock de los productos que ya están en el carrito
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

                // ✅ Refresca el panel "Producto actual" si ese producto cambió
                if (productoActualMostradoId.HasValue)
                {
                    var productoMostrado = productos.FirstOrDefault(p => p.Id == productoActualMostradoId.Value);
                    if (productoMostrado != null)
                    {
                        txtNombreProductoActual.Text = productoMostrado.Nombre;
                        CargarImagenProductoActual(productoMostrado.ImagenBytes);
                    }
                }

                ActualizarCarritoCentral();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo actualizar la información: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            WHERE p.Activo = 1";

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

            
            var producto = productos.FirstOrDefault(p => p.Id == seleccionado.ProductoId);

            if (producto == null)
            {
                MessageBox.Show("No se encontró la información del producto");
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
                MessageBox.Show("Selecciona un producto de la lista");
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

                // ✅ Ajustar cantidad mínima según el tipo de precio elegido
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
                        // Precio 1 — no cambia la cantidad
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

                // Refrescar catálogo y stock tras la venta
                CargarProductos();
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

        private void BtnLateralDevoluciones_Click(object sender, RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Devoluciones"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            var ventana = new DevolucionesWindow();
            ventana.ShowDialog();
        }

        private void BtnLateralClientes_Click(object sender, RoutedEventArgs e)
        {
            if (!PermisosHelper.TieneAcceso("Clientes"))
            {
                PermisosHelper.MostrarAccesoDenegado();
                return;
            }

            var ventana = new ClientesWindow();
            ventana.ShowDialog();
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

            // Botón "Todos"
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

            // 1. Cargar categorías
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

            // 2. Cargar subcategorías, agrupadas por CategoriaId
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

            // 3. Generar botones: cada categoría seguida de sus subcategorías (si tiene)
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

        
        private void AplicarFiltroCatalogo()
        {
            // Resaltar botón activo
            foreach (Button b in pnlCategorias.Children.OfType<Button>())
            {
                var tagBtn = b.Tag as FiltroCatalogo;
                bool esActivo =
                    (tagBtn?.Tipo == filtroActivo.Tipo) &&
                    (tagBtn?.Id == filtroActivo.Id);

                b.Style = (Style)FindResource(esActivo ? "BtnCategoriaActiva" : "BtnCategoria");
            }

            if (filtroActivo.Tipo == "Todos")
            {
                icProductosCatalogo.ItemsSource = productos;
            }
            else if (filtroActivo.Tipo == "Categoria")
            {
                icProductosCatalogo.ItemsSource = productos
                    .Where(p => p.CategoriaId == filtroActivo.Id)
                    .ToList();
            }
            else if (filtroActivo.Tipo == "Subcategoria")
            {
                icProductosCatalogo.ItemsSource = productos
                    .Where(p => p.SubcategoriaId == filtroActivo.Id)
                    .ToList();
            }
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
                // Ya se confirmó desde el botón "Salir" — no preguntar de nuevo
                relojTimer?.Stop();
                return;
            }

            MessageBoxResult resultado =
                MessageBox.Show(
                    "¿Deseas cerrar sesión?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
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