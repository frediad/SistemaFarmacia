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

            string query =
            @"SELECT p.*,
            (SELECT TOP 1 img.ImagenData
            FROM ImagenesProducto img
            WHERE img.ProductoId = p.Id
            ORDER BY img.Orden) AS PrimeraImagenData
            FROM Productos p
            WHERE p.Activo = 1";

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

                    ImagenBytes = reader["PrimeraImagenData"] != DBNull.Value
                        ? (byte[])reader["PrimeraImagenData"]
                        : null,

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

            // Resaltar botón activo
            foreach (Button b in pnlCategorias.Children.OfType<Button>())
            {
                b.Style = (Style)FindResource("BtnCategoria");
            }
            btn!.Style = (Style)FindResource("BtnCategoriaActiva");

            if (filtro == null || filtro.Tipo == "Todos")
            {
                icProductosCatalogo.ItemsSource = productos;
            }
            else if (filtro.Tipo == "Categoria")
            {
                icProductosCatalogo.ItemsSource = productos
                    .Where(p => p.CategoriaId == filtro.Id)
                    .ToList();
            }
            else if (filtro.Tipo == "Subcategoria")
            {
                icProductosCatalogo.ItemsSource = productos
                    .Where(p => p.SubcategoriaId == filtro.Id)
                    .ToList();
            }
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


        private void ActualizarTotales();

        private void txtBuscarProducto_TextChanged(
            object sender,
            TextChangedEventArgs e)

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

        private void BtnCantidad_Click(
             object sender,
             RoutedEventArgs e)
        {
            var seleccionado =
                dgCarrito.SelectedItem
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
            if (carrito.Count == 0)
            {
                MessageBox.Show(
                    "No hay productos en el carrito.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            decimal total = carrito.Sum(x => x.Subtotal);

            string inputPago =
                Microsoft.VisualBasic.Interaction.InputBox(
                    $"Total: {total:C}\n\nIngrese el monto recibido:",
                    "Cobrar");


            if (!decimal.TryParse(inputPago, out decimal pago))
            {
                MessageBox.Show("Monto inválido.");
                return;

        // =========================================
        // ATAJOS DE TECLADO
        // =========================================

        private void VentasWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                

                case Key.F9:
                    BtnGenerarTicket_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.F10:
                    BtnCobrar_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.F11:
                    BtnCancelar_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;

            }


            if (pago < total)
            {
                MessageBox.Show("El pago es insuficiente.");
                return;
            }

            decimal cambio = pago - total;

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            SqlTransaction trans = conn.BeginTransaction();

            try
            {
                decimal subtotal = carrito.Sum(x => x.Subtotal);
                decimal iva = subtotal * 0.16m;

                string folio =
                    $"VTA-{DateTime.Now:yyyyMMddHHmmss}";

                //----------------------------------
                // INSERTAR VENTA
                //----------------------------------

                string sqlVenta =
                @"INSERT INTO Ventas
                (
                Folio,
                Fecha,
                Subtotal,
                IVA,
                Descuento,
                Total,
                MetodoPago,
                Estado,
                UsuarioId
                )
                VALUES
                (
                @Folio,
                GETDATE(),
                @Subtotal,
                @IVA,
                0,
                @Total,
                'Efectivo',
                'Completada',
                @UsuarioId
                );

                SELECT SCOPE_IDENTITY();";

                SqlCommand cmdVenta =
                    new SqlCommand(sqlVenta, conn, trans);

                cmdVenta.Parameters.AddWithValue("@Folio", folio);
                cmdVenta.Parameters.AddWithValue("@Subtotal", subtotal);
                cmdVenta.Parameters.AddWithValue("@IVA", iva);
                cmdVenta.Parameters.AddWithValue("@Total", subtotal + iva);
                cmdVenta.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);

                int ventaId =
                    Convert.ToInt32(cmdVenta.ExecuteScalar());

                //----------------------------------
                // DETALLE DE VENTA
                //----------------------------------

                foreach (var item in carrito)
                {
                    SqlCommand cmdDetalle =
                        new SqlCommand(
                        @"INSERT INTO DetalleVentas
                (
                    VentaId,
                    ProductoId,
                    Cantidad,
                    PrecioUnitario,
                    Subtotal
                )
                VALUES
                (
                    @VentaId,
                    @ProductoId,
                    @Cantidad,
                    @Precio,
                    @Subtotal
                )",
                        conn,
                        trans);

                    cmdDetalle.Parameters.AddWithValue("@VentaId", ventaId);
                    cmdDetalle.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                    cmdDetalle.Parameters.AddWithValue("@Precio", item.Precio);
                    cmdDetalle.Parameters.AddWithValue("@Subtotal", item.Subtotal);

                    cmdDetalle.ExecuteNonQuery();

                    //----------------------------------
                    // DESCONTAR STOCK
                    //----------------------------------

                    SqlCommand cmdStock =
                        new SqlCommand(
                        @"UPDATE Productos
                  SET Stock = Stock - @Cantidad
                  WHERE Id = @ProductoId",
                        conn,
                        trans);

                    cmdStock.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                    cmdStock.Parameters.AddWithValue("@ProductoId", item.ProductoId);

                    cmdStock.ExecuteNonQuery();
                }

                trans.Commit();

                txtPago.Text = pago.ToString("C");
                txtCambio.Text = cambio.ToString("C");

                MessageBox.Show(
                    $"Venta registrada correctamente.\n\nCambio: {cambio:C}",
                    "Venta",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                carrito.Clear();

                ActualizarTotales();

                CargarProductos();
            }
            catch (Exception ex)
            {
                trans.Rollback();

                MessageBox.Show(ex.Message);
            }
        }

        private void BtnBuscarProducto_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new BuscarProductoWindow(productos)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true && ventana.ProductoSeleccionado != null)
            {
                AgregarProductoAlCarrito(ventana.ProductoSeleccionado);
            }
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
                case Key.F2:
                    BtnBuscarProducto_Click(sender, new RoutedEventArgs());
                    break;

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

        private void VentasWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            VentasWindow_KeyDown(sender, e);

        }







    }
}