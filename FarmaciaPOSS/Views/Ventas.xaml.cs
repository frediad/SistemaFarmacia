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
using System.Windows.Media;

namespace FarmaciaPOS.Views
{
    public partial class VentasWindow : Window
    {
        private List<Producto> productos = new();
        private ObservableCollection<VentaItem> carrito = new();

        // ✅ Filtro de periodo actualmente activo en el Historial
        private string filtroPeriodoActivo = "Dia";

        public VentasWindow()
        {
            InitializeComponent();

            dgCarrito.ItemsSource = carrito;

            CargarProductos();
            CargarCategoriasCatalogo();
            CargarCatalogo();
            ActualizarTotales();

            // ✅ Historial y estadísticas se cargan cuando el usuario entra a esas
            // pestañas (evita consultas innecesarias si solo va a vender).
            tabPrincipal.SelectionChanged += TabPrincipal_SelectionChanged;
        }

        private bool historialCargadoAlMenosUnaVez = false;
        private bool estadisticasCargadasAlMenosUnaVez = false;

        private void TabPrincipal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabPrincipal.SelectedIndex == 1 && !historialCargadoAlMenosUnaVez)
            {
                CargarHistorial();
                historialCargadoAlMenosUnaVez = true;
            }
            else if (tabPrincipal.SelectedIndex == 2 && !estadisticasCargadasAlMenosUnaVez)
            {
                CargarEstadisticas();
                estadisticasCargadasAlMenosUnaVez = true;
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
                    ImagenBytes =
                        reader["PrimeraImagenData"] != DBNull.Value
                            ? (byte[])reader["PrimeraImagenData"]
                            : null,
                    CategoriaId = reader["CategoriaId"] != DBNull.Value
                        ? Convert.ToInt32(reader["CategoriaId"])
                        : 0,
                });
            }
        }

        // =========================================
        // CATEGORÍAS
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
                            Content = "  " + sub.Nombre,
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

            foreach (Button b in pnlCategorias.Children.OfType<Button>())
            {
                b.Style = (Style)FindResource("BtnCategoria");
            }
            btn!.Style = (Style)FindResource("BtnCategoriaActiva");

            if (filtro == null || filtro.Tipo == "Todos")
                icProductosCatalogo.ItemsSource = productos;
            else if (filtro.Tipo == "Categoria")
                icProductosCatalogo.ItemsSource = productos
                    .Where(p => p.CategoriaId == filtro.Id).ToList();
            else if (filtro.Tipo == "Subcategoria")
                icProductosCatalogo.ItemsSource = productos
                    .Where(p => p.SubcategoriaId == filtro.Id).ToList();
        }

        private void CargarCatalogo()
        {
            icProductosCatalogo.ItemsSource = productos;
        }

        // =========================================
        // CLIC EN TARJETA DE PRODUCTO
        // =========================================

        private void CardProducto_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border &&
                border.DataContext is Producto producto)
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

            var existente =
                carrito.FirstOrDefault(x => x.ProductoId == producto.Id);

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
        // TOTALES
        // =========================================

        private void ActualizarTotales()
        {
            decimal total = carrito.Sum(x => x.Subtotal);
            txtTotal.Text = total.ToString("C");
            txtPago.Text = "$0.00";
            txtCambio.Text = "$0.00";
        }

        // =========================================
        // BUSCADOR DE PRODUCTOS
        // =========================================

        private void txtBuscarProducto_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            string texto = (sender as TextBox)?.Text.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(texto))
            {
                icProductosCatalogo.ItemsSource = productos;
                return;
            }

            icProductosCatalogo.ItemsSource = productos
                .Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.CodigoBarras.ToLower().Contains(texto))
                .ToList();
        }

        // =========================================
        // ACCIONES DEL TICKET
        // =========================================

        private void BtnMasCant_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = dgCarrito.SelectedItem as VentaItem;

            if (seleccionado == null)
            {
                MensajeHelper.Advertencia("Selecciona un producto de la lista", "Aviso", this);
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
                MensajeHelper.Advertencia("Selecciona un producto de la lista", "Aviso", this);
                return;
            }

            var producto =
                productos.FirstOrDefault(p => p.Id == seleccionado.ProductoId);

            if (producto == null)
            {
                MensajeHelper.Error("No se encontró la información del producto", "Error", this);
                return;
            }

            var ventana = new CantidadWindow(producto) { Owner = this };

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
                MensajeHelper.Advertencia("Selecciona un producto de la lista", "Aviso", this);
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
                MensajeHelper.Advertencia("Selecciona un producto para eliminar", "Aviso", this);
                return;
            }

            carrito.Remove(seleccionado);
            ActualizarTotales();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
                return;

            bool confirmar = MensajeHelper.Confirmar(
                "¿Cancelar la venta actual? Se perderán los productos del ticket.",
                "Confirmar",
                this);

            if (confirmar)
            {
                carrito.Clear();
                ActualizarTotales();
            }
        }

        private void BtnGenerarTicket_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MensajeHelper.Advertencia("No hay productos en el ticket", "Aviso", this);
                return;
            }

            MensajeHelper.Info(
                "Función de impresión de ticket pendiente.",
                "Generar Ticket",
                this);
        }

        // =========================================
        // COBRAR
        // =========================================

        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MensajeHelper.Advertencia("No hay productos en el carrito.", "Aviso", this);
                return;
            }

            var ventana = new Cobrar(carrito)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true && ventana.VentaCompletada)
            {
                carrito.Clear();
                ActualizarTotales();
                CargarProductos();

                // ✅ Si el historial ya se había cargado antes, refrescarlo para
                // que la venta recién hecha aparezca sin que el usuario tenga
                // que darle "Actualizar" manualmente.
                if (historialCargadoAlMenosUnaVez)
                    CargarHistorial();

                if (estadisticasCargadasAlMenosUnaVez)
                    CargarEstadisticas();
            }
        }

        // =========================================
        // BUSCAR PRODUCTO
        // =========================================

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

        // =========================================================
        // ✅ TAB 2: HISTORIAL DE VENTAS
        // =========================================================

        private void CargarHistorial()
        {
            try
            {
                var lista = VentasReporteHelper.ObtenerHistorial(filtroPeriodoActivo);
                dgHistorialVentas.ItemsSource = lista;

                decimal totalPeriodo = lista.Sum(v => v.Total);
                txtTotalHistorial.Text = $"Total del periodo: {totalPeriodo:C}  ({lista.Count} venta{(lista.Count == 1 ? "" : "s")})";
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo cargar el historial: " + ex.Message, "Error", this);
            }
        }

        private void BtnFiltroPeriodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag)
                return;

            filtroPeriodoActivo = tag;

            // Resalta el botón activo, regresa los demás a su estilo normal
            foreach (var b in new[] { btnFiltroDia, btnFiltroSemana, btnFiltroMes, btnFiltroAño, btnFiltroTodo })
                b.Style = (Style)FindResource("BtnFiltroPeriodo");

            btn.Style = (Style)FindResource("BtnFiltroPeriodoActivo");

            CargarHistorial();
        }

        private void BtnActualizarHistorial_Click(object sender, RoutedEventArgs e)
        {
            CargarHistorial();
        }

        // ✅ Reimprime el ticket de una venta ya realizada, reconstruyendo el
        // detalle desde DetalleVentas. Como Pago/Cambio no se guardan en BD,
        // se reimprime con Pago = Total y Cambio = $0.00 (venta ya liquidada).
        private void BtnReimprimirTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not VentaHistorialItem venta)
                return;

            try
            {
                var detalle = VentasReporteHelper.ObtenerDetalleVenta(venta.Id);

                if (detalle.Count == 0)
                {
                    MensajeHelper.Advertencia("Esta venta no tiene productos registrados en el detalle.", "Aviso", this);
                    return;
                }

                var config = ConfiguracionPosHelper.Cargar();

                if (string.IsNullOrWhiteSpace(config.ImpresoraTicket))
                {
                    MensajeHelper.Error("No hay ninguna impresora de tickets configurada. Ve a Periféricos para asignar una.", "Error", this);
                    return;
                }

                ImpresoraTicketHelper.ImprimirTicketVenta(
                    config.ImpresoraTicket,
                    venta.Folio,
                    venta.Vendedor,
                    detalle,
                    venta.Subtotal,
                    venta.Total,
                    venta.Total,   // Pago: se reconstruye igual al total (venta ya liquidada)
                    0m,            // Cambio: no se guarda en BD, se reimprime en $0.00
                    config.AnchoTicketMM);

                MensajeHelper.Exito($"Ticket del folio {venta.Folio} reimpreso correctamente.", "Reimpresión", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo reimprimir el ticket: " + ex.Message, "Error", this);
            }
        }

        // =========================================================
        // ✅ TAB 3: ESTADÍSTICAS
        // =========================================================

        private void CargarEstadisticas()
        {
            try
            {
                var mensual = VentasReporteHelper.ObtenerEstadisticaMensual();
                var anual = VentasReporteHelper.ObtenerEstadisticaAnual();

                LlenarTarjetaEstadistica(
                    mensual,
                    txtEtiquetaMesActual, txtMontoMesActual,
                    txtEtiquetaMesAnterior, txtMontoMesAnterior,
                    txtPorcentajeMes, iconoMes, txtDiferenciaMes);

                LlenarTarjetaEstadistica(
                    anual,
                    txtEtiquetaAnioActual, txtMontoAnioActual,
                    txtEtiquetaAnioAnterior, txtMontoAnioAnterior,
                    txtPorcentajeAño, iconoAño, txtDiferenciaAnio);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar las estadísticas: " + ex.Message, "Error", this);
            }
        }

        private void LlenarTarjetaEstadistica(
            EstadisticaVentas datos,
            TextBlock txtEtiquetaActual, TextBlock txtMontoActual,
            TextBlock txtEtiquetaAnterior, TextBlock txtMontoAnterior,
            TextBlock txtPorcentaje, TextBlock icono, TextBlock txtDiferencia)
        {
            txtEtiquetaActual.Text = datos.EtiquetaActual;
            txtMontoActual.Text = datos.MontoActual.ToString("C");

            txtEtiquetaAnterior.Text = datos.EtiquetaAnterior;
            txtMontoAnterior.Text = datos.MontoAnterior.ToString("C");

            txtPorcentaje.Text = $" {Math.Abs(datos.PorcentajeCambio):F2}%";
            txtDiferencia.Text = $"{(datos.EsPositivo ? "+" : "-")} {Math.Abs(datos.Diferencia):C}";

            var colorPositivo = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
            var colorNegativo = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));

            if (datos.EsPositivo)
            {
                txtPorcentaje.Foreground = colorPositivo;
                txtDiferencia.Foreground = colorPositivo;
                icono.Text = " ▲";
                icono.Foreground = colorPositivo;
            }
            else
            {
                txtPorcentaje.Foreground = colorNegativo;
                txtDiferencia.Foreground = colorNegativo;
                icono.Text = " ▼";
                icono.Foreground = colorNegativo;
            }
        }

        // =========================================
        // CERRAR
        // =========================================

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // =========================================
        // ATAJOS DE TECLADO
        // =========================================

        private void VentasWindow_PreviewKeyDown(
            object sender, KeyEventArgs e)
        {
            // ✅ Los atajos de venta solo aplican en la pestaña de Punto de Venta,
            // para no interferir con la navegación en Historial/Estadísticas.
            if (tabPrincipal.SelectedIndex != 0)
                return;

            switch (e.Key)
            {
                case Key.F2:
                    BtnBuscarProducto_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.F5:
                    BtnCantidad_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.F6:
                    BtnEliminar_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;

                case Key.F7:
                    BtnDescuento_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;

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

                case Key.Escape:
                    this.Close();
                    break;
            }
        }
    }
}