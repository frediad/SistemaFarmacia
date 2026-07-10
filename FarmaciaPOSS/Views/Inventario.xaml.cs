using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FarmaciaPOS.Views
{
    public partial class InventarioWindow : Window
    {
        private List<Producto> productos = new();

        // Compras
        private ObservableCollection<DetalleCompraItem> itemsCompra = new();

        // Ajuste
        private ObservableCollection<AjusteProductoItem> itemsAjuste = new();

        public InventarioWindow()
        {
            InitializeComponent();

            cbTipo.SelectedIndex = 0;

            dgItemsCompra.ItemsSource = itemsCompra;
            dgAjuste.ItemsSource = itemsAjuste;
            dgSugerencias.ItemsSource = sugerencias;

            CargarProductos();
            CargarProveedores();
            CargarMovimientos();
            CargarAlertasStock();

            cbProductoKardex.ItemsSource = productos;
            cbProductoKardex.DisplayMemberPath = "Nombre";
            cbProductoKardex.SelectedValuePath = "Id";

            CargarValorizacion();
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        private void CargarProductos()
        {
            productos.Clear();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query = "SELECT * FROM Productos WHERE Activo = 1 ORDER BY Nombre";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(new Producto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString(),
                    CodigoBarras = reader["CodigoBarras"].ToString(),
                    Stock = Convert.ToInt32(reader["Stock"]),
                    PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                    PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"]),
                    ImagenURL = reader["ImagenURL"].ToString(),
                    StockMinimo = reader["StockMinimo"] != DBNull.Value ? Convert.ToInt32(reader["StockMinimo"]) : 0,
                });
            }

            cbProductos.ItemsSource = productos;
            cbProductos.DisplayMemberPath = "Nombre";
            cbProductos.SelectedValuePath = "Id";

            if(cbProductoKardex != null)
            {
                cbProductoKardex.ItemsSource = productos;

            }

            if (dgValorizacion != null)
            {
                CargarValorizacion();
            }


        }

        private void cbProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Puedes mostrar info adicional del producto seleccionado aquí si lo deseas
        }

        // =========================================
        // PESTAÑA 1 — MOVIMIENTOS (MODO SUMAR)
        // No modifica el costo promedio del producto.
        // =========================================

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbProductos.SelectedItem is not Producto productoSeleccionado)
                {
                    MessageBox.Show("Selecciona un producto");
                    return;
                }

                string tipo = (cbTipo.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
                {
                    MessageBox.Show("Ingresa una cantidad válida", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int productoId = productoSeleccionado.Id;

                if (tipo == "Salida" && cantidad > productoSeleccionado.Stock)
                {
                    MessageBox.Show(
                        $"No puedes registrar una salida de {cantidad} unidades.\nStock disponible: {productoSeleccionado.Stock}",
                        "Stock insuficiente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query =
                @"INSERT INTO MovimientoInventarios
                (ProductoId, TipoMovimiento, Cantidad, Motivo, UsuarioId, Fecha)
                VALUES
                (@ProductoId, @TipoMovimiento, @Cantidad, @Motivo, @UsuarioId, GETDATE())";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                cmd.Parameters.AddWithValue("@TipoMovimiento", tipo);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                cmd.Parameters.AddWithValue("@Motivo", txtMotivo.Text);
                cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                cmd.ExecuteNonQuery();

                string updateQuery = tipo == "Entrada"
                    ? "UPDATE Productos SET Stock = Stock + @Cantidad WHERE Id = @ProductoId"
                    : "UPDATE Productos SET Stock = Stock - @Cantidad WHERE Id = @ProductoId";

                SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@Cantidad", cantidad);
                updateCmd.Parameters.AddWithValue("@ProductoId", productoId);
                updateCmd.ExecuteNonQuery();

                MessageBox.Show("Movimiento guardado correctamente");

                txtCantidad.Clear();
                txtMotivo.Clear();

                CargarProductos();
                CargarMovimientos();
                CargarAlertasStock();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarMovimientos()
        {
            List<MovimientoInventarioView> lista = new();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT
                p.Nombre AS ProductoNombre,
                m.TipoMovimiento,
                m.Cantidad,
                m.Fecha,
                m.Motivo
              FROM MovimientoInventarios m
              INNER JOIN Productos p ON m.ProductoId = p.Id
              ORDER BY m.Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new MovimientoInventarioView
                {
                    ProductoNombre = reader["ProductoNombre"].ToString(),
                    TipoMovimiento = reader["TipoMovimiento"].ToString(),
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                    Motivo = reader["Motivo"].ToString()
                });
            }

            dgMovimientos.ItemsSource = lista;
        }

        // =========================================
        // PESTAÑA 2 — COMPRAS
        // Sí actualiza el costo promedio ponderado.
        // =========================================

        private void CargarProveedores()
        {
            List<Proveedor> lista = new();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query = "SELECT * FROM Proveedores ORDER BY Nombre";
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Proveedor
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString(),
                    Telefono = reader["Telefono"].ToString(),
                    Correo = reader["Correo"].ToString(),
                    Direccion = reader["Direccion"].ToString(),
                    Contacto = reader["Contacto"].ToString()
                });
            }

            cbProveedor.ItemsSource = lista;
        }

        private void BtnBuscarProductoCompra_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new BuscarProductoWindow(productos)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado != true || ventana.ProductoSeleccionado == null)
                return;

            var producto = ventana.ProductoSeleccionado;

            var existente = itemsCompra.FirstOrDefault(x => x.ProductoId == producto.Id);

            if (existente != null)
            {
                existente.Cantidad += 1;
            }
            else
            {
                itemsCompra.Add(new DetalleCompraItem
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    StockActual = producto.Stock,
                    CostoActual = producto.PrecioCompra,
                    Cantidad = 1,
                    CostoUnitario = producto.PrecioCompra
                });
            }

            ActualizarTotalCompra();
        }

        private void BtnQuitarItemCompra_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DetalleCompraItem item)
            {
                itemsCompra.Remove(item);
                ActualizarTotalCompra();
            }
        }

        private void dgItemsCompra_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ActualizarTotalCompra));
        }

        private void ActualizarTotalCompra()
        {
            decimal total = itemsCompra.Sum(x => x.Subtotal);
            txtTotalCompra.Text = total.ToString("C");
        }

        private void BtnConfirmarCompra_Click(object sender, RoutedEventArgs e)
        {
            if (cbProveedor.SelectedValue == null)
            {
                MessageBox.Show("Selecciona un proveedor");
                return;
            }

            if (itemsCompra.Count == 0)
            {
                MessageBox.Show("Agrega al menos un producto a la compra");
                return;
            }

            foreach (var item in itemsCompra)
            {
                if (item.Cantidad <= 0)
                {
                    MessageBox.Show($"\"{item.Nombre}\": la cantidad debe ser mayor a cero");
                    return;
                }

                if (item.CostoUnitario <= 0)
                {
                    MessageBox.Show($"\"{item.Nombre}\": el costo debe ser mayor a cero");
                    return;
                }
            }

            int proveedorId = Convert.ToInt32(cbProveedor.SelectedValue);
            decimal totalCompra = itemsCompra.Sum(x => x.Subtotal);

            var confirmar = MessageBox.Show(
                $"Se registrará una compra por {totalCompra:C} con {itemsCompra.Count} producto(s).\n" +
                "Esto aumentará el stock y actualizará el costo promedio de cada producto.\n\n¿Confirmar?",
                "Confirmar compra",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
                return;

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string queryCompra =
            @"INSERT INTO Compras
            (ProveedorId, NumeroFactura, Fecha, Total, UsuarioId, MetodoPago)
            VALUES
            (@ProveedorId, @NumeroFactura, GETDATE(), @Total, @UsuarioId, @MetodoPago);
            SELECT SCOPE_IDENTITY();";

            SqlCommand cmdCompra = new SqlCommand(queryCompra, conn);
            cmdCompra.Parameters.AddWithValue("@ProveedorId", proveedorId);
            cmdCompra.Parameters.AddWithValue("@NumeroFactura",
                string.IsNullOrWhiteSpace(txtNumeroFactura.Text) ? (object)DBNull.Value : txtNumeroFactura.Text);
            cmdCompra.Parameters.AddWithValue("@Total", totalCompra);
            cmdCompra.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
            cmdCompra.Parameters.AddWithValue("@MetodoPago",
                (cbMetodoPagoCompra.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Transferencia");

            int compraId = Convert.ToInt32(cmdCompra.ExecuteScalar());

            foreach (var item in itemsCompra)
            {
                string queryDetalle =
                @"INSERT INTO DetalleCompras
                (CompraId, ProductoId, Cantidad, CostoUnitario)
                VALUES
                (@CompraId, @ProductoId, @Cantidad, @CostoUnitario)";

                SqlCommand cmdDetalle = new SqlCommand(queryDetalle, conn);
                cmdDetalle.Parameters.AddWithValue("@CompraId", compraId);
                cmdDetalle.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdDetalle.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                cmdDetalle.Parameters.AddWithValue("@CostoUnitario", item.CostoUnitario);
                cmdDetalle.ExecuteNonQuery();

                int stockAnterior = item.StockActual;
                decimal costoAnterior = item.CostoActual;
                int cantidadComprada = item.Cantidad;
                decimal costoNuevo = item.CostoUnitario;

                decimal costoPromedio = (stockAnterior + cantidadComprada) == 0
                    ? costoNuevo
                    : ((stockAnterior * costoAnterior) + (cantidadComprada * costoNuevo)) / (stockAnterior + cantidadComprada);

                string queryProducto =
                @"UPDATE Productos
                  SET Stock = Stock + @Cantidad,
                      PrecioCompra = @CostoPromedio
                  WHERE Id = @ProductoId";

                SqlCommand cmdProducto = new SqlCommand(queryProducto, conn);
                cmdProducto.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                cmdProducto.Parameters.AddWithValue("@CostoPromedio", costoPromedio);
                cmdProducto.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdProducto.ExecuteNonQuery();

                string queryMovimiento =
                @"INSERT INTO MovimientoInventarios
                (ProductoId, TipoMovimiento, Cantidad, Motivo, UsuarioId, Fecha)
                VALUES
                (@ProductoId, 'Entrada', @Cantidad, @Motivo, @UsuarioId, GETDATE())";

                SqlCommand cmdMov = new SqlCommand(queryMovimiento, conn);
                cmdMov.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdMov.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                cmdMov.Parameters.AddWithValue("@Motivo", $"Compra #{compraId}" +
                    (string.IsNullOrWhiteSpace(txtNumeroFactura.Text) ? "" : $" - Factura {txtNumeroFactura.Text}"));
                cmdMov.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                cmdMov.ExecuteNonQuery();
            }

            MessageBox.Show(
                $"Compra #{compraId} registrada correctamente.\nTotal: {totalCompra:C}",
                "Éxito",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            itemsCompra.Clear();
            txtNumeroFactura.Clear();
            cbProveedor.SelectedIndex = -1;
            ActualizarTotalCompra();

            CargarProductos();
            CargarMovimientos();
            CargarAlertasStock();
        }

        // =========================================
        // PESTAÑA 3 — AJUSTE DE INVENTARIO 
        // =========================================

        private void BtnCargarAjuste_Click(object sender, RoutedEventArgs e)
        {
            itemsAjuste.Clear();

            foreach (var p in productos)
            {
                itemsAjuste.Add(new AjusteProductoItem
                {
                    ProductoId = p.Id,
                    Nombre = p.Nombre,
                    StockSistema = p.Stock,
                    StockContado = p.Stock // por defecto igual al sistema, el usuario lo corrige
                });
            }
        }

        private void dgAjuste_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Refresca visualmente los estilos de fila (diferencias) tras editar
            Dispatcher.BeginInvoke(new Action(() => dgAjuste.Items.Refresh()));
        }

        private void BtnAplicarAjustes_Click(object sender, RoutedEventArgs e)
        {
            var itemsConDiferencia = itemsAjuste.Where(x => x.TieneDiferencia).ToList();

            if (itemsConDiferencia.Count == 0)
            {
                MessageBox.Show("No hay diferencias que ajustar. Todos los productos coinciden con el sistema.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivoAjuste.Text))
            {
                MessageBox.Show("Escribe el motivo del ajuste (ej. \"Conteo físico mensual\")");
                return;
            }

            string resumen = string.Join("\n", itemsConDiferencia.Select(x =>
                $"{x.Nombre}: {(x.Diferencia > 0 ? "+" : "")}{x.Diferencia}"));

            var confirmar = MessageBox.Show(
                $"Se aplicarán {itemsConDiferencia.Count} ajuste(s):\n\n{resumen}\n\n" +
                "El stock del sistema se actualizará para reflejar el conteo físico. ¿Confirmar?",
                "Confirmar ajuste de inventario",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
                return;

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            foreach (var item in itemsConDiferencia)
            {
                string tipoMovimiento = item.Diferencia > 0 ? "Entrada" : "Salida";
                int cantidadAbsoluta = Math.Abs(item.Diferencia);

                string queryMovimiento =
                @"INSERT INTO MovimientoInventarios
                (ProductoId, TipoMovimiento, Cantidad, Motivo, UsuarioId, Fecha)
                VALUES
                (@ProductoId, @TipoMovimiento, @Cantidad, @Motivo, @UsuarioId, GETDATE())";

                SqlCommand cmdMov = new SqlCommand(queryMovimiento, conn);
                cmdMov.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdMov.Parameters.AddWithValue("@TipoMovimiento", tipoMovimiento);
                cmdMov.Parameters.AddWithValue("@Cantidad", cantidadAbsoluta);
                cmdMov.Parameters.AddWithValue("@Motivo", $"Ajuste de inventario (conteo físico) - {txtMotivoAjuste.Text}");
                cmdMov.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                cmdMov.ExecuteNonQuery();

                // El nuevo stock queda exactamente igual al contado, sin importar el signo
                string queryProducto = "UPDATE Productos SET Stock = @StockContado WHERE Id = @ProductoId";

                SqlCommand cmdProducto = new SqlCommand(queryProducto, conn);
                cmdProducto.Parameters.AddWithValue("@StockContado", item.StockContado);
                cmdProducto.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdProducto.ExecuteNonQuery();
            }

            MessageBox.Show(
                $"Se aplicaron {itemsConDiferencia.Count} ajuste(s) de inventario correctamente.",
                "Éxito",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            txtMotivoAjuste.Clear();
            itemsAjuste.Clear();

            CargarProductos();
            CargarMovimientos();
            CargarAlertasStock();
        }

        // =========================================
        // ✅ PESTAÑA 4 — KARDEX POR PRODUCTO
        // =========================================

        private void BtnVerKardex_Click(object sender, RoutedEventArgs e)
        {
            if (cbProductoKardex.SelectedItem is not Producto producto)
            {
                MessageBox.Show("Selecciona un producto");
                return;
            }

            List<KardexItem> movimientos = new();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT TipoMovimiento, Cantidad, Motivo, Fecha
      FROM MovimientoInventarios
      WHERE ProductoId = @ProductoId
      ORDER BY Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProductoId", producto.Id);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                movimientos.Add(new KardexItem
                {
                    TipoMovimiento = reader["TipoMovimiento"].ToString(),
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Motivo = reader["Motivo"].ToString(),
                    Fecha = Convert.ToDateTime(reader["Fecha"])
                });
            }

            // Calcula el saldo retrocediendo desde el stock actual
            // (los movimientos vienen ordenados del más reciente al más antiguo)
            int saldoActual = producto.Stock;

            foreach (var mov in movimientos)
            {
                mov.Saldo = saldoActual;

                saldoActual = mov.TipoMovimiento == "Entrada"
                    ? saldoActual - mov.Cantidad
                    : saldoActual + mov.Cantidad;
            }

            dgKardex.ItemsSource = movimientos;
        }

        // =========================================
        // ✅ PESTAÑA 5 — VALORIZACIÓN DE INVENTARIO
        // =========================================

        private void CargarValorizacion()
        {
            var lista = productos.Select(p => new ValorizacionItem
            {
                Nombre = p.Nombre,
                Stock = p.Stock,
                PrecioCompra = p.PrecioCompra,
                PrecioVenta = p.PrecioVenta
            }).ToList();

            dgValorizacion.ItemsSource = lista;

            decimal totalCosto = lista.Sum(x => x.ValorCosto);
            decimal totalVenta = lista.Sum(x => x.ValorVenta);
            decimal gananciaPotencial = totalVenta - totalCosto;

            txtValorCostoTotal.Text = totalCosto.ToString("C");
            txtValorVentaTotal.Text = totalVenta.ToString("C");
            txtGananciaPotencialTotal.Text = gananciaPotencial.ToString("C");
        }

        // =========================================
        // ✅ PESTAÑA 6 — SUGERENCIA DE COMPRA
        // =========================================

        private ObservableCollection<SugerenciaCompraItem> sugerencias = new();

        private void BtnGenerarSugerencias_Click(object sender, RoutedEventArgs e)
        {
            sugerencias.Clear();

            foreach (var p in productos.Where(p => p.StockMinimo > 0 && p.Stock <= p.StockMinimo))
            {
                int cantidadSugerida = p.Stock > p.Stock
                    ? p.Stock - p.Stock
                    : (p.StockMinimo * 2) - p.Stock;

                cantidadSugerida = Math.Max(cantidadSugerida, 1);

                sugerencias.Add(new SugerenciaCompraItem
                {
                    ProductoId = p.Id,
                    Nombre = p.Nombre,
                    Stock = p.Stock,
                    StockMinimo = p.StockMinimo,
                    StockMaximo = p.Stock,
                    CantidadSugerida = cantidadSugerida,
                    CostoUnitario = p.PrecioCompra
                });
            }

            dgSugerencias.ItemsSource = sugerencias;

            ActualizarTotalSugerencias();

            if (sugerencias.Count == 0)
            {
                MessageBox.Show("No hay productos por debajo de su stock mínimo en este momento. 🎉");
            }
        }

        private void dgSugerencias_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ActualizarTotalSugerencias));
        }

        private void ActualizarTotalSugerencias()
        {
            decimal total = sugerencias.Where(x => x.Seleccionado).Sum(x => x.CostoEstimado);
            txtTotalSugerencias.Text = total.ToString("C");
        }

        private void BtnAgregarSugerenciasACompras_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = sugerencias.Where(x => x.Seleccionado && x.CantidadSugerida > 0).ToList();

            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Selecciona al menos un producto");
                return;
            }

            foreach (var item in seleccionados)
            {
                var existente = itemsCompra.FirstOrDefault(x => x.ProductoId == item.ProductoId);

                if (existente != null)
                {
                    existente.Cantidad += item.CantidadSugerida;
                }
                else
                {
                    itemsCompra.Add(new DetalleCompraItem
                    {
                        ProductoId = item.ProductoId,
                        Nombre = item.Nombre,
                        StockActual = item.Stock,
                        CostoActual = item.CostoUnitario,
                        Cantidad = item.CantidadSugerida,
                        CostoUnitario = item.CostoUnitario
                    });
                }
            }

            ActualizarTotalCompra();

            // Cambia automáticamente a la pestaña de Compras (índice 1)
            tabInventario.SelectedIndex = 1;

            MessageBox.Show(
                $"{seleccionados.Count} producto(s) agregado(s) a Compras. Selecciona el proveedor y confirma la compra.",
                "Listo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================
        // ✅ BOTÓN GENERAL DEL HEADER — PEDIDO LIBRE
        // =========================================

        private void BtnPedirMercanciaInventario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new PedirMercanciaWindow
            {
                Owner = this
            };

            ventana.ShowDialog();
        }

        // =========================================
        // ✅ PEDIR POR CORREO DESDE SUGERENCIA DE COMPRA
        // =========================================

        private void BtnPedirSugerenciasPorCorreo_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = sugerencias.Where(x => x.Seleccionado && x.CantidadSugerida > 0).ToList();

            if (seleccionados.Count == 0)
            {
                MessageBox.Show("Selecciona al menos un producto");
                return;
            }

            var itemsPedido = seleccionados.Select(x => new PedidoProveedorItem
            {
                Nombre = x.Nombre,
                Cantidad = x.CantidadSugerida,
                CostoUnitario = x.CostoUnitario
            }).ToList();

            var ventana = new PedirMercanciaWindow(null, itemsPedido)
            {
                Owner = this
            };

            ventana.ShowDialog();
        }

        // =========================================
        // ALERTAS DE STOCK BAJO / AGOTADO
        // =========================================

        private void CargarAlertasStock()
        {
            var alertas = new List<AlertaStockView>();

            foreach (var p in productos.Where(p => p.StockMinimo > 0 || p.Stock == 0))
            {
                if (p.Stock <= 0)
                {
                    alertas.Add(new AlertaStockView
                    {
                        Nombre = p.Nombre,
                        Detalle = "Sin stock disponible",
                        Etiqueta = "AGOTADO",
                        ColorFondo = new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2)),
                        ColorBadge = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26))
                    });
                }
                else if (p.Stock <= p.StockMinimo)
                {
                    alertas.Add(new AlertaStockView
                    {
                        Nombre = p.Nombre,
                        Detalle = $"Stock actual: {p.Stock}  (mínimo: {p.StockMinimo})",
                        Etiqueta = "REABASTECER",
                        ColorFondo = new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)),
                        ColorBadge = new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06))
                    });
                }
            }

            icAlertasStock.ItemsSource = alertas;

            txtResumenAlertas.Text =
                alertas.Count == 0
                    ? "Todo el inventario está en niveles saludables ✅"
                    : $"{alertas.Count} producto(s) requieren atención";
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class AlertaStockView
    {
        public string Nombre { get; set; }
        public string Detalle { get; set; }
        public string Etiqueta { get; set; }
        public Brush ColorFondo { get; set; }
        public Brush ColorBadge { get; set; }
    }
}