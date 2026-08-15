using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FarmaciaPOS.Views
{
    public partial class InventarioWindow : Window
    {
        private List<Producto> productos = new();

        // Pedido a proveedor (antes "Compras")
        private ObservableCollection<DetalleCompraItem> itemsCompra = new();

        // Ajuste
        private ObservableCollection<AjusteProductoItem> itemsAjuste = new();

        // Sugerencia de compra
        private ObservableCollection<SugerenciaCompraItem> sugerencias = new();

        // ✅ Recepción de pedidos
        private ObservableCollection<DetalleRecepcionItem> itemsRecepcion = new();
        private int pedidoSeleccionadoId = 0;

        // ✅ Productos por caducar
        private DataView vistaCaducidades;

        public InventarioWindow()
        {
            InitializeComponent();

            cbTipo.SelectedIndex = 0;

            dgItemsCompra.ItemsSource = itemsCompra;
            dgAjuste.ItemsSource = itemsAjuste;
            dgSugerencias.ItemsSource = sugerencias;
            dgDetalleRecepcion.ItemsSource = itemsRecepcion;

            // ⚠️ Constructor blindado — usa MessageBox.Show (no MensajeHelper) porque
            // esta ventana todavía no se ha mostrado en pantalla en este punto; asignarle
            // Owner a MensajeHelper antes de tiempo provoca un crash de WPF.
            try
            {
                CargarProductos();
                CargarProveedores();
                CargarMovimientos();
                CargarAlertasStock();
                CargarPedidosPendientes();
                CargarCaducidades();

                cbProductoKardex.ItemsSource = productos;
                cbProductoKardex.DisplayMemberPath = "Nombre";
                cbProductoKardex.SelectedValuePath = "Id";

                CargarValorizacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al iniciar el módulo de Inventario:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        private void CargarProductos()
        {
            productos.Clear();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT * FROM Productos p
              WHERE p.Activo = 1
              ORDER BY p.Nombre";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(new Producto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString(),
                    CodigoBarras = reader["CodigoBarras"].ToString(),
                    PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                    PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"]),
                    ImagenURL =
                        reader["ImagenURL"] != DBNull.Value
                            ? reader["ImagenURL"].ToString()
                            : "",

                    StockMinimo = reader["StockMinimo"] != DBNull.Value
                        ? Convert.ToInt32(reader["StockMinimo"])
                        : 0,

                    Stock = reader["Stock"] != DBNull.Value
                        ? Convert.ToInt32(reader["Stock"])
                        : 0,
                });
            }

            cbProductos.ItemsSource = productos;
            cbProductos.DisplayMemberPath = "Nombre";
            cbProductos.SelectedValuePath = "Id";

            if (cbProductoKardex != null)
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
        // Este es el único lugar donde el stock aumenta directamente
        // sin pasar por un pedido/recepción — para ajustes rápidos manuales.
        // =========================================

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cbProductos.SelectedItem is not Producto productoSeleccionado)
                {
                    MensajeHelper.Advertencia("Selecciona un producto", "Aviso", this);
                    return;
                }

                string tipo = (cbTipo.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
                {
                    MensajeHelper.Advertencia("Ingresa una cantidad válida", "Aviso", this);
                    return;
                }

                int productoId = productoSeleccionado.Id;

                if (tipo == "Salida" && cantidad > productoSeleccionado.Stock)
                {
                    MensajeHelper.Advertencia(
                        $"No puedes registrar una salida de {cantidad} unidades.\nStock disponible: {productoSeleccionado.Stock}",
                        "Stock insuficiente",
                        this);
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

                MensajeHelper.Exito("Movimiento guardado correctamente", "Listo", this);

                txtCantidad.Clear();
                txtMotivo.Clear();

                CargarProductos();
                CargarMovimientos();
                CargarAlertasStock();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
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
        // PESTAÑA 2 — PEDIR A PROVEEDOR (antes "Compras")
        // Ya NO registra una compra ni toca stock/costo directamente.
        // Solo arma la lista y abre PedirMercanciaWindow.
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

        // ✅ Ya no registra la compra directo: arma el pedido y abre
        // PedirMercanciaWindow, sin tocar stock ni costo.
        private void BtnConfirmarCompra_Click(object sender, RoutedEventArgs e)
        {
            if (cbProveedor.SelectedItem is not Proveedor proveedorSeleccionado)
            {
                MensajeHelper.Advertencia("Selecciona un proveedor", "Aviso", this);
                return;
            }

            if (itemsCompra.Count == 0)
            {
                MensajeHelper.Advertencia("Agrega al menos un producto al pedido", "Aviso", this);
                return;
            }

            foreach (var item in itemsCompra)
            {
                if (item.Cantidad <= 0)
                {
                    MensajeHelper.Advertencia($"\"{item.Nombre}\": la cantidad debe ser mayor a cero", "Aviso", this);
                    return;
                }
            }

            var itemsPedido = itemsCompra.Select(x => new PedidoProveedorItem
            {
                ProductoId = x.ProductoId,
                Nombre = x.Nombre,
                Cantidad = x.Cantidad,
                CostoUnitario = x.CostoUnitario
            }).ToList();

            var ventana = new PedirMercanciaWindow(proveedorSeleccionado, itemsPedido)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true)
            {
                itemsCompra.Clear();
                cbProveedor.SelectedIndex = -1;
                ActualizarTotalCompra();

                // El pedido recién enviado ya quedó guardado en BD como "Enviado"
                CargarPedidosPendientes();
            }
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
                    StockContado = p.Stock
                });
            }
        }

        private void dgAjuste_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => dgAjuste.Items.Refresh()));
        }

        private void BtnAplicarAjustes_Click(object sender, RoutedEventArgs e)
        {
            var itemsConDiferencia = itemsAjuste.Where(x => x.TieneDiferencia).ToList();

            if (itemsConDiferencia.Count == 0)
            {
                MensajeHelper.Info("No hay diferencias que ajustar. Todos los productos coinciden con el sistema.", "Sin cambios", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivoAjuste.Text))
            {
                MensajeHelper.Advertencia("Escribe el motivo del ajuste (ej. \"Conteo físico mensual\")", "Aviso", this);
                return;
            }

            string resumen = string.Join("\n", itemsConDiferencia.Select(x =>
                $"{x.Nombre}: {(x.Diferencia > 0 ? "+" : "")}{x.Diferencia}"));

            bool confirmar = MensajeHelper.Confirmar(
                $"Se aplicarán {itemsConDiferencia.Count} ajuste(s):\n\n{resumen}\n\n" +
                "El stock del sistema se actualizará para reflejar el conteo físico. ¿Confirmar?",
                "Confirmar ajuste de inventario",
                this);

            if (!confirmar)
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

                string queryProducto = "UPDATE Productos SET Stock = @StockContado WHERE Id = @ProductoId";

                SqlCommand cmdProducto = new SqlCommand(queryProducto, conn);
                cmdProducto.Parameters.AddWithValue("@StockContado", item.StockContado);
                cmdProducto.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdProducto.ExecuteNonQuery();
            }

            MensajeHelper.Exito(
                $"Se aplicaron {itemsConDiferencia.Count} ajuste(s) de inventario correctamente.",
                "Éxito",
                this);

            txtMotivoAjuste.Clear();
            itemsAjuste.Clear();

            CargarProductos();
            CargarMovimientos();
            CargarAlertasStock();
        }

        // =========================================
        // PESTAÑA 4 — KARDEX POR PRODUCTO
        // =========================================

        private void BtnVerKardex_Click(object sender, RoutedEventArgs e)
        {
            if (cbProductoKardex.SelectedItem is not Producto producto)
            {
                MensajeHelper.Advertencia("Selecciona un producto", "Aviso", this);
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
        // PESTAÑA 5 — VALORIZACIÓN DE INVENTARIO
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
        // PESTAÑA 6 — SUGERENCIA DE COMPRA
        // =========================================

        private void BtnGenerarSugerencias_Click(object sender, RoutedEventArgs e)
        {
            sugerencias.Clear();

            foreach (var p in productos.Where(p => p.StockMinimo > 0 && p.Stock <= p.StockMinimo))
            {
                int cantidadSugerida = (p.StockMinimo * 2) - p.Stock;
                cantidadSugerida = Math.Max(cantidadSugerida, 1);

                sugerencias.Add(new SugerenciaCompraItem
                {
                    ProductoId = p.Id,
                    Nombre = p.Nombre,
                    Stock = p.Stock,
                    StockMinimo = p.StockMinimo,
                    StockMaximo = p.StockMinimo * 2,
                    CantidadSugerida = cantidadSugerida,
                    CostoUnitario = p.PrecioCompra
                });
            }

            dgSugerencias.ItemsSource = sugerencias;

            ActualizarTotalSugerencias();

            if (sugerencias.Count == 0)
            {
                MensajeHelper.Info("No hay productos por debajo de su stock mínimo en este momento. 🎉", "Todo en orden", this);
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
                MensajeHelper.Advertencia("Selecciona al menos un producto", "Aviso", this);
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

            tabInventario.SelectedIndex = 1;

            MensajeHelper.Exito(
                $"{seleccionados.Count} producto(s) agregado(s) al pedido. Selecciona el proveedor y envía el pedido.",
                "Listo",
                this);
        }

        // =========================================
        // BOTÓN GENERAL DEL HEADER — PEDIDO LIBRE
        // =========================================

        private void BtnPedirMercanciaInventario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new PedirMercanciaWindow
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true)
            {
                CargarPedidosPendientes();
            }
        }

        // =========================================
        // PEDIR POR CORREO DESDE SUGERENCIA DE COMPRA
        // =========================================

        private void BtnPedirSugerenciasPorCorreo_Click(object sender, RoutedEventArgs e)
        {
            var seleccionados = sugerencias.Where(x => x.Seleccionado && x.CantidadSugerida > 0).ToList();

            if (seleccionados.Count == 0)
            {
                MensajeHelper.Advertencia("Selecciona al menos un producto", "Aviso", this);
                return;
            }

            var itemsPedido = seleccionados.Select(x => new PedidoProveedorItem
            {
                ProductoId = x.ProductoId,
                Nombre = x.Nombre,
                Cantidad = x.CantidadSugerida,
                CostoUnitario = x.CostoUnitario
            }).ToList();

            var ventana = new PedirMercanciaWindow(null, itemsPedido)
            {
                Owner = this
            };

            bool? resultado = ventana.ShowDialog();

            if (resultado == true)
            {
                CargarPedidosPendientes();
            }
        }

        // =========================================
        // ✅ PESTAÑA 7 — RECEPCIÓN DE PEDIDOS
        // Aquí sí se registra la compra real: stock, costo promedio,
        // Compras/DetalleCompras y el movimiento de inventario.
        // =========================================

        private void CargarPedidosPendientes()
        {
            try
            {
                List<PedidoProveedorView> lista = new();

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query =
                @"SELECT pp.Id, pr.Nombre AS Proveedor, pp.Fecha, pp.Total, pp.Estado
                  FROM PedidosProveedor pp
                  INNER JOIN Proveedores pr ON pp.ProveedorId = pr.Id
                  WHERE pp.Estado = 'Enviado'
                  ORDER BY pp.Fecha DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new PedidoProveedorView
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Proveedor = reader["Proveedor"].ToString() ?? "",
                        Fecha = Convert.ToDateTime(reader["Fecha"]),
                        Total = Convert.ToDecimal(reader["Total"]),
                        Estado = reader["Estado"].ToString() ?? ""
                    });
                }

                dgPedidosPendientes.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                // ⚠️ MessageBox.Show (no MensajeHelper): este método se llama desde
                // el constructor antes de que la ventana se muestre en pantalla.
                MessageBox.Show(
                    "No se pudieron cargar los pedidos pendientes: " + ex.Message +
                    "\n\n¿Ya creaste las tablas PedidosProveedor y DetallePedidosProveedor en tu base de datos?",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DgPedidosPendientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPedidosPendientes.SelectedItem is not PedidoProveedorView pedido)
                return;

            pedidoSeleccionadoId = pedido.Id;

            txtInfoPedidoSeleccionado.Text =
                $"Pedido a \"{pedido.Proveedor}\" — {pedido.Fecha:dd/MM/yyyy} — Total estimado: {pedido.Total:C}";

            try
            {
                itemsRecepcion.Clear();

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query =
                @"SELECT
                    dp.ProductoId,
                    p.Nombre,
                    dp.Cantidad AS CantidadPedida,
                    dp.CostoUnitario,
                    p.Stock AS StockActual,
                    p.PrecioCompra AS CostoActual
                  FROM DetallePedidoProveedor dp
                  INNER JOIN Productos p ON dp.ProductoId = p.Id
                  WHERE dp.PedidoProveedorId = @PedidoId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PedidoId", pedidoSeleccionadoId);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    itemsRecepcion.Add(new DetalleRecepcionItem
                    {
                        ProductoId = Convert.ToInt32(reader["ProductoId"]),
                        Nombre = reader["Nombre"].ToString() ?? "",
                        CantidadPedida = Convert.ToInt32(reader["CantidadPedida"]),
                        StockActual = Convert.ToInt32(reader["StockActual"]),
                        CostoActual = Convert.ToDecimal(reader["CostoActual"]),
                        CantidadRecibida = Convert.ToInt32(reader["CantidadPedida"]),
                        CostoUnitarioReal = Convert.ToDecimal(reader["CostoUnitario"])
                    });
                }

                ActualizarTotalRecepcion();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo cargar el detalle del pedido: " + ex.Message, "Error", this);
            }
        }

        private void dgDetalleRecepcion_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ActualizarTotalRecepcion));
        }

        private void ActualizarTotalRecepcion()
        {
            decimal total = itemsRecepcion.Sum(x => x.Subtotal);
            txtTotalRecepcion.Text = total.ToString("C");
        }

        private void BtnConfirmarRecepcion_Click(object sender, RoutedEventArgs e)
        {
            if (pedidoSeleccionadoId == 0 || itemsRecepcion.Count == 0)
            {
                MensajeHelper.Advertencia("Selecciona un pedido con productos para recibir", "Aviso", this);
                return;
            }

            var itemsAReceptar = itemsRecepcion.Where(x => x.CantidadRecibida > 0).ToList();

            if (itemsAReceptar.Count == 0)
            {
                MensajeHelper.Advertencia("Indica al menos una cantidad recibida mayor a cero", "Aviso", this);
                return;
            }

            foreach (var item in itemsAReceptar)
            {
                if (item.CostoUnitarioReal <= 0)
                {
                    MensajeHelper.Advertencia($"\"{item.Nombre}\": el costo debe ser mayor a cero", "Aviso", this);
                    return;
                }
            }

            decimal totalRecibido = itemsAReceptar.Sum(x => x.Subtotal);

            bool confirmar = MensajeHelper.Confirmar(
                $"Se registrará la recepción de {itemsAReceptar.Count} producto(s) por {totalRecibido:C}.\n" +
                "Esto aumentará el stock y actualizará el costo promedio de cada producto.\n\n¿Confirmar?",
                "Confirmar recepción",
                this);

            if (!confirmar)
                return;

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            SqlTransaction trans = conn.BeginTransaction();

            try
            {
                string queryCompra =
                @"INSERT INTO Compras
                (ProveedorId, NumeroFactura, Fecha, Total, UsuarioId, MetodoPago)
                SELECT ProveedorId, @NumeroFactura, GETDATE(), @Total, @UsuarioId, MetodoPago
                FROM PedidosProveedor WHERE Id = @PedidoProveedorId;
                SELECT SCOPE_IDENTITY();";

                SqlCommand cmdCompra = new SqlCommand(queryCompra, conn, trans);
                cmdCompra.Parameters.AddWithValue("@NumeroFactura",
                    string.IsNullOrWhiteSpace(txtNumeroFacturaRecepcion.Text) ? (object)DBNull.Value : txtNumeroFacturaRecepcion.Text);
                cmdCompra.Parameters.AddWithValue("@Total", totalRecibido);
                cmdCompra.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                cmdCompra.Parameters.AddWithValue("@PedidoProveedorId", pedidoSeleccionadoId);

                int compraId = Convert.ToInt32(cmdCompra.ExecuteScalar());

                foreach (var item in itemsAReceptar)
                {
                    string queryDetalle =
                    @"INSERT INTO DetalleCompras
                    (CompraId, ProductoId, Cantidad, CostoUnitario)
                    VALUES
                    (@CompraId, @ProductoId, @Cantidad, @CostoUnitario)";

                    SqlCommand cmdDetalle = new SqlCommand(queryDetalle, conn, trans);
                    cmdDetalle.Parameters.AddWithValue("@CompraId", compraId);
                    cmdDetalle.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdDetalle.Parameters.AddWithValue("@Cantidad", item.CantidadRecibida);
                    cmdDetalle.Parameters.AddWithValue("@CostoUnitario", item.CostoUnitarioReal);
                    cmdDetalle.ExecuteNonQuery();

                    decimal costoPromedio = (item.StockActual + item.CantidadRecibida) == 0
                        ? item.CostoUnitarioReal
                        : ((item.StockActual * item.CostoActual) + (item.CantidadRecibida * item.CostoUnitarioReal))
                          / (item.StockActual + item.CantidadRecibida);

                    string queryProducto =
                    @"UPDATE Productos
                      SET Stock = Stock + @Cantidad,
                          PrecioCompra = @CostoPromedio
                      WHERE Id = @ProductoId";

                    SqlCommand cmdProducto = new SqlCommand(queryProducto, conn, trans);
                    cmdProducto.Parameters.AddWithValue("@Cantidad", item.CantidadRecibida);
                    cmdProducto.Parameters.AddWithValue("@CostoPromedio", costoPromedio);
                    cmdProducto.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdProducto.ExecuteNonQuery();

                    string queryMovimiento =
                    @"INSERT INTO MovimientoInventarios
                    (ProductoId, TipoMovimiento, Cantidad, Motivo, UsuarioId, Fecha)
                    VALUES
                    (@ProductoId, 'Entrada', @Cantidad, @Motivo, @UsuarioId, GETDATE())";

                    SqlCommand cmdMov = new SqlCommand(queryMovimiento, conn, trans);
                    cmdMov.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdMov.Parameters.AddWithValue("@Cantidad", item.CantidadRecibida);
                    cmdMov.Parameters.AddWithValue("@Motivo", $"Recepción de pedido #{pedidoSeleccionadoId} — Compra #{compraId}" +
                        (string.IsNullOrWhiteSpace(txtNumeroFacturaRecepcion.Text) ? "" : $" - Factura {txtNumeroFacturaRecepcion.Text}"));
                    cmdMov.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                    cmdMov.ExecuteNonQuery();
                }

                string queryEstado = "UPDATE PedidosProveedor SET Estado = 'Recibido' WHERE Id = @PedidoProveedorId";
                SqlCommand cmdEstado = new SqlCommand(queryEstado, conn, trans);
                cmdEstado.Parameters.AddWithValue("@PedidoProveedorId", pedidoSeleccionadoId);
                cmdEstado.ExecuteNonQuery();

                trans.Commit();

                MensajeHelper.Exito(
                    $"Compra #{compraId} registrada correctamente.\nTotal: {totalRecibido:C}",
                    "Recepción confirmada",
                    this);

                pedidoSeleccionadoId = 0;
                itemsRecepcion.Clear();
                txtNumeroFacturaRecepcion.Clear();
                txtInfoPedidoSeleccionado.Text = "← Selecciona un pedido de la lista";
                ActualizarTotalRecepcion();

                CargarPedidosPendientes();
                CargarProductos();
                CargarMovimientos();
                CargarAlertasStock();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                MensajeHelper.Error("No se pudo registrar la recepción: " + ex.Message, "Error", this);
            }
        }

        // =========================================
        // ✅ PRODUCTOS POR CADUCAR
        // Usa la tabla LotesProductos (FechaCaducidad, NumeroLote, Cantidad)
        // relacionada con Productos por ProductoId.
        // =========================================

        private void CargarCaducidades()
        {
            DataTable dt = ObtenerCaducidades();

            vistaCaducidades = dt.DefaultView;
            dgCaducidades.ItemsSource = vistaCaducidades;

            int caducados = 0;
            int proximos = 0;

            foreach (DataRow fila in dt.Rows)
            {
                string estado = fila["Estado"].ToString();

                if (estado == "CADUCADO")
                    caducados++;
                else if (estado == "PRÓXIMO A CADUCAR")
                    proximos++;
            }

            txtResumenCaducidades.Text =
                (caducados == 0 && proximos == 0)
                    ? "Sin productos caducados ni próximos a caducar ✅"
                    : $"{caducados} caducado(s) · {proximos} próximo(s) a caducar (30 días)";
        }

        private DataTable ObtenerCaducidades()
        {
            DataTable dt = new DataTable();

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query =
                @"SELECT
                    p.CodigoBarras,
                    p.Nombre,
                    ISNULL(l.NumeroLote, '—') AS NumeroLote,
                    l.FechaCaducidad AS Caducidad,
                    CASE
                        WHEN l.FechaCaducidad IS NULL THEN NULL
                        ELSE DATEDIFF(DAY, GETDATE(), l.FechaCaducidad)
                    END AS DiasRestantes,
                    l.Cantidad AS Stock,
                    CASE
                        WHEN l.FechaCaducidad IS NULL
                            THEN 'SIN LOTE REGISTRADO'
                        WHEN DATEDIFF(DAY, GETDATE(), l.FechaCaducidad) < 0
                            THEN 'CADUCADO'
                        WHEN DATEDIFF(DAY, GETDATE(), l.FechaCaducidad) <= 30
                            THEN 'PRÓXIMO A CADUCAR'
                        ELSE 'NO CADUCADO'
                    END AS Estado
                  FROM Productos p
                  LEFT JOIN LotesProductos l ON l.ProductoId = p.Id
                  WHERE p.Activo = 1
                  ORDER BY p.Nombre ASC, l.FechaCaducidad ASC";

                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                da.Fill(dt);
            }
            catch (Exception ex)
            {
                // ⚠️ MessageBox.Show (no MensajeHelper): este método también se llama
                // desde el constructor antes de que la ventana se muestre en pantalla.
                MessageBox.Show(
                    "No se pudieron cargar las caducidades: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return dt;
        }

        private void txtBuscarCaducidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (vistaCaducidades == null)
                return;

            string filtro = txtBuscarCaducidad.Text.Trim().Replace("'", "''");

            vistaCaducidades.RowFilter = string.IsNullOrWhiteSpace(filtro)
                ? ""
                : $"Nombre LIKE '%{filtro}%' OR CodigoBarras LIKE '%{filtro}%'";
        }

        private void BtnActualizarCaducidades_Click(object sender, RoutedEventArgs e)
        {
            CargarCaducidades();
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