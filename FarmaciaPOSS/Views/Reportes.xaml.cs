using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class ReportesWindow : Window
    {
        private DateTime fechaInicioActual;
        private DateTime fechaFinActual;
        private string periodoActual = "Hoy";

        public class BarraGrafica
        {
            public string Etiqueta { get; set; } = string.Empty;
            public double AlturaBarra { get; set; }
            public decimal Valor { get; set; }
        }

        public ReportesWindow()
        {
            InitializeComponent();
            CargarReporte(DateTime.Today, DateTime.Today, "Hoy");
            CargarMasVendidos(DateTime.Today, DateTime.Today);
            CargarGanancias(DateTime.Today, DateTime.Today);
            CargarInventario(DateTime.Today, DateTime.Today);
        }

        // =========================================
        // PESTAÑA VENTAS — BOTONES PERIODO
        // =========================================

        private void BtnHoy_Click(object sender, RoutedEventArgs e)
            => CargarReporte(DateTime.Today, DateTime.Today, "Hoy");

        private void BtnSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarReporte(hoy.AddDays(-diff), hoy, "Esta Semana");
        }

        private void BtnMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarReporte(new DateTime(hoy.Year, hoy.Month, 1), hoy, "Este Mes");
        }

        private void BtnAnio_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarReporte(new DateTime(hoy.Year, 1, 1), hoy, "Este Año");
        }

        // =========================================
        // PESTAÑA MÁS VENDIDOS — BOTONES PERIODO
        // =========================================

        private void BtnMasVendidosHoy_Click(object sender, RoutedEventArgs e)
            => CargarMasVendidos(DateTime.Today, DateTime.Today);

        private void BtnMasVendidosSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarMasVendidos(hoy.AddDays(-diff), hoy);
        }

        private void BtnMasVendidosMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarMasVendidos(new DateTime(hoy.Year, hoy.Month, 1), hoy);
        }

        private void BtnMasVendidosAnio_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarMasVendidos(new DateTime(hoy.Year, 1, 1), hoy);
        }

        // =========================================
        // PESTAÑA GANANCIAS — BOTONES PERIODO
        // =========================================

        private void BtnGananciasHoy_Click(object sender, RoutedEventArgs e)
            => CargarGanancias(DateTime.Today, DateTime.Today);

        private void BtnGananciasSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarGanancias(hoy.AddDays(-diff), hoy);
        }

        private void BtnGananciasMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarGanancias(new DateTime(hoy.Year, hoy.Month, 1), hoy);
        }

        private void BtnGananciasAnio_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarGanancias(new DateTime(hoy.Year, 1, 1), hoy);
        }

        // =========================================
        // PESTAÑA INVENTARIO — BOTONES PERIODO
        // =========================================

        private void BtnInventarioHoy_Click(object sender, RoutedEventArgs e)
            => CargarInventario(DateTime.Today, DateTime.Today);

        private void BtnInventarioSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarInventario(hoy.AddDays(-diff), hoy);
        }

        private void BtnInventarioMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarInventario(new DateTime(hoy.Year, hoy.Month, 1), hoy);
        }

        private void BtnInventarioAnio_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarInventario(new DateTime(hoy.Year, 1, 1), hoy);
        }

        // =========================================
        // VENTAS — LÓGICA EXISTENTE SIN CAMBIOS
        // =========================================

        private void CargarReporte(DateTime desde, DateTime hasta, string etiquetaPeriodo)
        {
            fechaInicioActual = desde;
            fechaFinActual = hasta;
            periodoActual = etiquetaPeriodo;

            txtPeriodoActual.Text = $"Periodo: {etiquetaPeriodo}";

            CargarTotales(desde, hasta);
            CargarProductosVendidos(desde, hasta);
            CargarGrafica(desde, hasta);
        }

        private void CargarTotales(DateTime desde, DateTime hasta)
        {
            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                ISNULL(SUM(Total), 0) AS TotalVentas,
                COUNT(*) AS NumeroVentas
              FROM Ventas
              WHERE Fecha >= @Desde AND Fecha < @Hasta
              AND Estado = 'Completada'";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                decimal total = Convert.ToDecimal(reader["TotalVentas"]);
                int numero = Convert.ToInt32(reader["NumeroVentas"]);

                txtTotalVentas.Text = total.ToString("C");
                txtNumeroVentas.Text = $"{numero} venta(s)";
            }
        }

        private void CargarProductosVendidos(DateTime desde, DateTime hasta)
        {
            List<ReporteVentaItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                p.Nombre AS Producto,
                SUM(dv.Cantidad) AS Cantidad,
                SUM(dv.Subtotal) AS Total
              FROM DetalleVentas dv
              INNER JOIN Ventas v ON dv.VentaId = v.Id
              INNER JOIN Productos p ON dv.ProductoId = p.Id
              WHERE v.Fecha >= @Desde AND v.Fecha < @Hasta
              AND v.Estado = 'Completada'
              GROUP BY p.Nombre
              ORDER BY SUM(dv.Subtotal) DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new ReporteVentaItem
                {
                    Producto = reader["Producto"].ToString() ?? "",
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Total = Convert.ToDecimal(reader["Total"])
                });
            }

            dgProductosVendidos.ItemsSource = lista;
        }

        private void CargarGrafica(DateTime desde, DateTime hasta)
        {
            List<ReporteVentaPorDia> ventasPorDia = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                CAST(Fecha AS DATE) AS Dia,
                SUM(Total) AS Total
              FROM Ventas
              WHERE Fecha >= @Desde AND Fecha < @Hasta
              AND Estado = 'Completada'
              GROUP BY CAST(Fecha AS DATE)
              ORDER BY Dia";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                ventasPorDia.Add(new ReporteVentaPorDia
                {
                    Fecha = Convert.ToDateTime(reader["Dia"]),
                    Total = Convert.ToDecimal(reader["Total"])
                });
            }

            decimal maxValor = ventasPorDia.Any()
                ? ventasPorDia.Max(v => v.Total) : 1;

            if (maxValor == 0) maxValor = 1;

            icGrafica.ItemsSource = ventasPorDia.Select(v => new BarraGrafica
            {
                Etiqueta = v.Fecha.ToString("dd/MM"),
                Valor = v.Total,
                AlturaBarra = (double)(v.Total / maxValor) * 200
            }).ToList();
        }

        // =========================================
        // ✅ MÁS VENDIDOS
        // =========================================

        private void CargarMasVendidos(DateTime desde, DateTime hasta)
        {
            List<ReporteMasVendidoItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT TOP 10
                ROW_NUMBER() OVER (ORDER BY SUM(dv.Cantidad) DESC) AS Posicion,
                p.Nombre AS Producto,
                SUM(dv.Cantidad) AS Cantidad,
                SUM(dv.Subtotal) AS Total
              FROM DetalleVentas dv
              INNER JOIN Ventas v ON dv.VentaId = v.Id
              INNER JOIN Productos p ON dv.ProductoId = p.Id
              WHERE v.Fecha >= @Desde AND v.Fecha < @Hasta
              AND v.Estado = 'Completada'
              GROUP BY p.Nombre
              ORDER BY SUM(dv.Cantidad) DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new ReporteMasVendidoItem
                {
                    Posicion = Convert.ToInt32(reader["Posicion"]),
                    Producto = reader["Producto"].ToString() ?? "",
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Total = Convert.ToDecimal(reader["Total"])
                });
            }

            dgMasVendidos.ItemsSource = lista;

            // Productos sin movimiento en el periodo
            CargarSinMovimiento(desde, hasta);
        }

        private void CargarSinMovimiento(DateTime desde, DateTime hasta)
        {
            List<ProductoStockItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT p.Nombre, p.Stock, p.StockMinimo
              FROM Productos p
              WHERE p.Activo = 1
              AND p.Id NOT IN (
                  SELECT DISTINCT dv.ProductoId
                  FROM DetalleVentas dv
                  INNER JOIN Ventas v ON dv.VentaId = v.Id
                  WHERE v.Fecha >= @Desde AND v.Fecha < @Hasta
                  AND v.Estado = 'Completada'
              )
              ORDER BY p.Nombre";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new ProductoStockItem
                {
                    Nombre = reader["Nombre"].ToString() ?? "",
                    Stock = Convert.ToInt32(reader["Stock"]),
                    StockMinimo = Convert.ToInt32(reader["StockMinimo"])
                });
            }

            dgSinMovimiento.ItemsSource = lista;
        }

        // =========================================
        // ✅ GANANCIAS
        // =========================================

        private void CargarGanancias(DateTime desde, DateTime hasta)
        {
            List<ReporteGananciaItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                p.Nombre AS Producto,
                SUM(dv.Cantidad) AS Cantidad,
                SUM(dv.Subtotal) AS Ingreso,
                SUM(dv.Cantidad * p.PrecioCompra) AS Costo
              FROM DetalleVentas dv
              INNER JOIN Ventas v ON dv.VentaId = v.Id
              INNER JOIN Productos p ON dv.ProductoId = p.Id
              WHERE v.Fecha >= @Desde AND v.Fecha < @Hasta
              AND v.Estado = 'Completada'
              GROUP BY p.Nombre
              ORDER BY (SUM(dv.Subtotal) - SUM(dv.Cantidad * p.PrecioCompra)) DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new ReporteGananciaItem
                {
                    Producto = reader["Producto"].ToString() ?? "",
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Ingreso = Convert.ToDecimal(reader["Ingreso"]),
                    Costo = Convert.ToDecimal(reader["Costo"])
                });
            }

            dgGanancias.ItemsSource = lista;

            decimal totalIngreso = lista.Sum(x => x.Ingreso);
            decimal totalCosto = lista.Sum(x => x.Costo);
            decimal totalGanancia = totalIngreso - totalCosto;

            txtIngresos.Text = totalIngreso.ToString("C");
            txtCostos.Text = totalCosto.ToString("C");
            txtGananciaNeta.Text = totalGanancia.ToString("C");
        }

        // =========================================
        // ✅ INVENTARIO
        // =========================================

        private void CargarInventario(DateTime desde, DateTime hasta)
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
              WHERE m.Fecha >= @Desde AND m.Fecha < @Hasta
              ORDER BY m.Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new MovimientoInventarioView
                {
                    ProductoNombre = reader["ProductoNombre"].ToString() ?? "",
                    TipoMovimiento = reader["TipoMovimiento"].ToString() ?? "",
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                    Motivo = reader["Motivo"].ToString() ?? ""
                });
            }

            dgMovimientosInventario.ItemsSource = lista;

            CargarStockBajo();
        }

        private void CargarStockBajo()
        {
            List<ProductoStockItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT Nombre, Stock, StockMinimo
              FROM Productos
              WHERE Activo = 1
              AND Stock <= StockMinimo
              ORDER BY Stock ASC";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new ProductoStockItem
                {
                    Nombre = reader["Nombre"].ToString() ?? "",
                    Stock = Convert.ToInt32(reader["Stock"]),
                    StockMinimo = Convert.ToInt32(reader["StockMinimo"])
                });
            }

            dgStockBajo.ItemsSource = lista;
        }

        // =========================================
        // EXPORTAR PDF
        // =========================================

        private void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var resumen = ObtenerResumenCompleto();

                string nombreArchivo =
                    $"Reporte_{periodoActual.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                string ruta = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    nombreArchivo);

                ReportePdfGenerator.GenerarReporte(resumen, ruta, periodoActual);

                MessageBox.Show(
                    $"Reporte guardado en:\n{ruta}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al generar PDF: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private ReporteResumen ObtenerResumenCompleto()
        {
            var resumen = new ReporteResumen
            {
                FechaInicio = fechaInicioActual,
                FechaFin = fechaFinActual,
                Productos = (dgProductosVendidos.ItemsSource
                    as List<ReporteVentaItem>) ?? new()
            };

            string totalTexto = txtTotalVentas.Text
                .Replace("$", "").Replace(",", "");
            decimal.TryParse(totalTexto, out decimal total);
            resumen.TotalVentas = total;

            string numeroTexto = txtNumeroVentas.Text.Split(' ')[0];
            int.TryParse(numeroTexto, out int numero);
            resumen.NumeroVentas = numero;

            return resumen;
        }
    }
}