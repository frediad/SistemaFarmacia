using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    public partial class DevolucionesWindow : Window
    {
        private VentaParaDevolucion ventaActual;
        private List<VentaResumenView> todasLasVentas = new();

        public DevolucionesWindow()
        {
            InitializeComponent();
            CargarVentas();
        }

        // =========================================
        // ✅ CARGAR TODAS LAS VENTAS
        // =========================================

        private void CargarVentas()
        {
            todasLasVentas.Clear();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT Id, Folio, Fecha, Total, Estado, MetodoPago
              FROM Ventas
              WHERE Estado IN ('Completada', 'Devuelta')
              ORDER BY Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                todasLasVentas.Add(new VentaResumenView
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Folio = reader["Folio"].ToString() ?? "",
                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                    Total = Convert.ToDecimal(reader["Total"]),
                    Estado = reader["Estado"].ToString() ?? "",
                    MetodoPago = reader["MetodoPago"].ToString() ?? ""
                });
            }

            dgVentas.ItemsSource = todasLasVentas;
        }

        // =========================================
        // ✅ BUSCADOR EN TIEMPO REAL
        // Busca por: ID, Folio, fecha (dd/MM/yyyy)
        // =========================================

        private void TxtBuscarVenta_TextChanged(
            object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscarVenta.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(texto))
            {
                dgVentas.ItemsSource = todasLasVentas;
                return;
            }

            dgVentas.ItemsSource = todasLasVentas
                .Where(v =>
                    v.Id.ToString().Contains(texto) ||
                    v.Folio.ToLower().Contains(texto) ||
                    v.Fecha.ToString("dd/MM/yyyy").Contains(texto) ||
                    v.Total.ToString("C").Contains(texto))
                .ToList();
        }

        // =========================================
        // ✅ SELECCIONAR VENTA DE LA LISTA
        // =========================================

        private void DgVentas_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (dgVentas.SelectedItem is VentaResumenView venta)
            {
                CargarDetalleVenta(venta.Id);
            }
        }

        // =========================================
        // CARGAR DETALLE DE LA VENTA
        // =========================================

        private void CargarDetalleVenta(int ventaId)
        {
            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string queryVenta =
                "SELECT Id, Folio, Fecha, Total FROM Ventas WHERE Id = @Id";

            SqlCommand cmdVenta = new SqlCommand(queryVenta, conn);
            cmdVenta.Parameters.AddWithValue("@Id", ventaId);

            SqlDataReader readerVenta = cmdVenta.ExecuteReader();

            if (!readerVenta.Read())
                return;

            ventaActual = new VentaParaDevolucion
            {
                Id = Convert.ToInt32(readerVenta["Id"]),
                Folio = readerVenta["Folio"].ToString() ?? "",
                Fecha = Convert.ToDateTime(readerVenta["Fecha"]),
                Total = Convert.ToDecimal(readerVenta["Total"])
            };

            readerVenta.Close();

            string queryDetalle =
            @"SELECT
                dv.ProductoId,
                p.Nombre,
                dv.Cantidad AS CantidadVendida,
                dv.PrecioUnitario,
                ISNULL((
                    SELECT SUM(d.Cantidad)
                    FROM Devoluciones d
                    WHERE d.VentaId = dv.VentaId
                    AND d.ProductoId = dv.ProductoId
                ), 0) AS CantidadYaDevuelta
              FROM DetalleVentas dv
              INNER JOIN Productos p ON dv.ProductoId = p.Id
              WHERE dv.VentaId = @VentaId";

            SqlCommand cmdDetalle = new SqlCommand(queryDetalle, conn);
            cmdDetalle.Parameters.AddWithValue("@VentaId", ventaId);

            SqlDataReader readerDetalle = cmdDetalle.ExecuteReader();

            ventaActual.Detalles.Clear();

            while (readerDetalle.Read())
            {
                ventaActual.Detalles.Add(new DetalleVentaDevolucion
                {
                    ProductoId = Convert.ToInt32(readerDetalle["ProductoId"]),
                    Nombre = readerDetalle["Nombre"].ToString() ?? "",
                    CantidadVendida = Convert.ToInt32(readerDetalle["CantidadVendida"]),
                    PrecioUnitario = Convert.ToDecimal(readerDetalle["PrecioUnitario"]),
                    CantidadYaDevuelta = Convert.ToInt32(readerDetalle["CantidadYaDevuelta"]),
                    CantidadADevolver = 0
                });
            }

            dgDetalleVenta.ItemsSource = ventaActual.Detalles;

            txtInfoVenta.Text =
                $"Venta #{ventaActual.Id} — {ventaActual.Folio} — " +
                $"{ventaActual.Fecha:dd/MM/yyyy HH:mm} — Total: {ventaActual.Total:C}";

            ActualizarMontoADevolver();
        }

        // =========================================
        // ✅ CALCULAR MONTO EN TIEMPO REAL
        // =========================================

        private void ActualizarMontoADevolver()
        {
            if (ventaActual == null)
            {
                txtMontoADevolver.Text = "$0.00";
                return;
            }

            decimal total = ventaActual.Detalles
                .Sum(x => x.CantidadADevolver * x.PrecioUnitario);

            txtMontoADevolver.Text = total.ToString("C");
        }

        // =========================================
        // PROCESAR DEVOLUCIÓN
        // =========================================

        private void BtnProcesarDevolucion_Click(
            object sender, RoutedEventArgs e)
        {
            if (ventaActual == null)
            {
                MessageBox.Show("Selecciona una venta de la lista");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivoDevolucion.Text))
            {
                MessageBox.Show("Escribe el motivo de la devolución");
                return;
            }

            var itemsADevolver = ventaActual.Detalles
                .Where(d => d.CantidadADevolver > 0)
                .ToList();

            if (itemsADevolver.Count == 0)
            {
                MessageBox.Show(
                    "Indica cuántas piezas se devuelven de al menos un producto");
                return;
            }

            foreach (var item in itemsADevolver)
            {
                if (item.CantidadADevolver > item.CantidadDisponibleDevolver)
                {
                    MessageBox.Show(
                        $"\"{item.Nombre}\": solo puedes devolver " +
                        $"hasta {item.CantidadDisponibleDevolver} pieza(s)");
                    return;
                }
            }

            decimal totalADevolver =
                itemsADevolver.Sum(x => x.CantidadADevolver * x.PrecioUnitario);

            var confirmar = MessageBox.Show(
                $"Se devolverán {itemsADevolver.Sum(x => x.CantidadADevolver)} " +
                $"pieza(s) por un total de {totalADevolver:C}.\n\n" +
                "✅ Se regresará el stock al inventario\n" +
                "✅ Se registrará la salida de efectivo en caja\n\n" +
                "¿Confirmar devolución?",
                "Confirmar devolución",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
                return;

            try
            {
                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                foreach (var item in itemsADevolver)
                {
                    decimal montoItem =
                        item.CantidadADevolver * item.PrecioUnitario;

                    // 1) Registrar la devolución
                    string queryDevolucion =
                    @"INSERT INTO Devoluciones
                      (VentaId, ProductoId, Cantidad, MontoDevuelto, Motivo, UsuarioId, Fecha)
                      VALUES
                      (@VentaId, @ProductoId, @Cantidad, @MontoDevuelto, @Motivo, @UsuarioId, GETDATE())";

                    SqlCommand cmdDev = new SqlCommand(queryDevolucion, conn);
                    cmdDev.Parameters.AddWithValue("@VentaId", ventaActual.Id);
                    cmdDev.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdDev.Parameters.AddWithValue("@Cantidad", item.CantidadADevolver);
                    cmdDev.Parameters.AddWithValue("@MontoDevuelto", montoItem);
                    cmdDev.Parameters.AddWithValue("@Motivo", txtMotivoDevolucion.Text);
                    cmdDev.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                    cmdDev.ExecuteNonQuery();

                    // 2) Regresar stock
                    string queryStock =
                        "UPDATE Productos SET Stock = Stock + @Cantidad WHERE Id = @ProductoId";

                    SqlCommand cmdStock = new SqlCommand(queryStock, conn);
                    cmdStock.Parameters.AddWithValue("@Cantidad", item.CantidadADevolver);
                    cmdStock.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdStock.ExecuteNonQuery();

                    // 3) Registrar movimiento de inventario
                    string queryMovimiento =
                    @"INSERT INTO MovimientoInventarios
                      (ProductoId, TipoMovimiento, Cantidad, Motivo, UsuarioId, Fecha)
                      VALUES
                      (@ProductoId, 'Entrada', @Cantidad, @Motivo, @UsuarioId, GETDATE())";

                    SqlCommand cmdMov = new SqlCommand(queryMovimiento, conn);
                    cmdMov.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                    cmdMov.Parameters.AddWithValue("@Cantidad", item.CantidadADevolver);
                    cmdMov.Parameters.AddWithValue("@Motivo",
                        $"Devolución venta #{ventaActual.Id} — {txtMotivoDevolucion.Text}");
                    cmdMov.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                    cmdMov.ExecuteNonQuery();
                }

                // 4) Registrar salida en caja
                RegistrarSalidaEnCaja(conn, totalADevolver, ventaActual.Id);

                // 5) Marcar venta como Devuelta si todos los productos fueron devueltos
                bool devolucionTotal = ventaActual.Detalles
                    .All(d => d.CantidadYaDevuelta + d.CantidadADevolver >= d.CantidadVendida);

                if (devolucionTotal)
                {
                    string queryEstado =
                        "UPDATE Ventas SET Estado = 'Devuelta' WHERE Id = @Id";

                    SqlCommand cmdEstado = new SqlCommand(queryEstado, conn);
                    cmdEstado.Parameters.AddWithValue("@Id", ventaActual.Id);
                    cmdEstado.ExecuteNonQuery();
                }

                MessageBox.Show(
                    $"✅ Devolución procesada correctamente\n\nTotal devuelto: {totalADevolver:C}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                txtMotivoDevolucion.Clear();
                txtBuscarVenta.Clear();
                ventaActual = null;
                dgDetalleVenta.ItemsSource = null;
                txtInfoVenta.Text = "← Selecciona una venta de la lista";
                txtMontoADevolver.Text = "$0.00";

                CargarVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al procesar devolución: " + ex.Message,
                    "ERROR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================
        // REGISTRAR SALIDA EN CAJA
        // =========================================

        private void RegistrarSalidaEnCaja(
            SqlConnection conn, decimal monto, int ventaId)
        {
            string queryCaja =
                "SELECT TOP 1 Id FROM Caja WHERE Estado = 'ABIERTA' ORDER BY Id DESC";

            SqlCommand cmdCaja = new SqlCommand(queryCaja, conn);
            var resultado = cmdCaja.ExecuteScalar();

            if (resultado == null)
            {
                MessageBox.Show(
                    "No hay caja abierta — el monto devuelto no se registró en Caja.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            int cajaId = Convert.ToInt32(resultado);

            string queryMovCaja =
            @"INSERT INTO MovimientosCaja
              (CajaId, TipoMovimiento, Monto, Motivo, Fecha)
              VALUES
              (@CajaId, 'SALIDA', @Monto, @Motivo, GETDATE())";

            SqlCommand cmdMovCaja = new SqlCommand(queryMovCaja, conn);
            cmdMovCaja.Parameters.AddWithValue("@CajaId", cajaId);
            cmdMovCaja.Parameters.AddWithValue("@Monto", monto);
            cmdMovCaja.Parameters.AddWithValue("@Motivo",
                $"Devolución venta #{ventaId}");
            cmdMovCaja.ExecuteNonQuery();
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // =========================================
        // ✅ ACTUALIZAR MONTO AL EDITAR CANTIDAD
        // =========================================

        private void txtNumeroVenta_KeyDown(object sender, KeyEventArgs e) { }
        private void BtnBuscarVenta_Click(object sender, RoutedEventArgs e) { }
    }
}