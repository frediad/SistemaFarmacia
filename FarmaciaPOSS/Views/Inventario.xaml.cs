using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FarmaciaPOS.Views
{
    public partial class InventarioWindow : Window
    {
        private List<Producto> productos = new();

        public InventarioWindow()
        {
            InitializeComponent();

            cbTipo.SelectedIndex = 0;

            CargarProductos();
            CargarMovimientos();
            CargarAlertasStock();
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                productos.Add(new Producto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString(),
                    Stock = Convert.ToInt32(reader["Stock"]),
                    StockMinimo = reader["StockMinimo"] != DBNull.Value
                        ? Convert.ToInt32(reader["StockMinimo"])
                        : 0,

                });
            }

            cbProductos.ItemsSource = productos;
            cbProductos.DisplayMemberPath = "Nombre";
            cbProductos.SelectedValuePath = "Id";
        }

        // =========================================
        // ✅ MOSTRAR STOCK ACTUAL AL SELECCIONAR
        // =========================================

        private void cbProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbProductos.SelectedItem is Producto producto)
            {
                txtStockActualInfo.Text =
                    $"Stock actual: {producto.Stock}   |   Mínimo: {producto.StockMinimo}   |   Máximo: {producto.Stock}";
            }
            else
            {
                txtStockActualInfo.Text = "";
            }
        }

        // =========================================
        // GUARDAR MOVIMIENTO
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

                string tipo =
                    (cbTipo.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
                {
                    MessageBox.Show(
                        "Ingresa una cantidad válida",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                int productoId = productoSeleccionado.Id;

                // ✅ VALIDACIÓN: no permitir que una salida deje el stock en negativo
                if (tipo == "Salida" && cantidad > productoSeleccionado.Stock)
                {
                    MessageBox.Show(
                        $"No puedes registrar una salida de {cantidad} unidades.\n" +
                        $"Stock disponible: {productoSeleccionado.Stock}",
                        "Stock insuficiente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                // INSERTAR MOVIMIENTO
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

                // ACTUALIZAR STOCK
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
                MessageBox.Show(
                    ex.Message,
                    "ERROR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================
        // CARGAR MOVIMIENTOS
        // =========================================

        private void CargarMovimientos()
        {
            List<MovimientoInventarioView> lista = new();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

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
        // ✅ ALERTAS DE STOCK BAJO / AGOTADO
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
    }

    // =========================================
    // ✅ CLASE AUXILIAR PARA EL PANEL DE ALERTAS
    // =========================================

    public class AlertaStockView
    {
        public string Nombre { get; set; }
        public string Detalle { get; set; }
        public string Etiqueta { get; set; }
        public System.Windows.Media.Brush ColorFondo { get; set; }
        public System.Windows.Media.Brush ColorBadge { get; set; }
    }


}