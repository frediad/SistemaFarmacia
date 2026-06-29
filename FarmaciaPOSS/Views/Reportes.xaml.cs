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
        }

        // =========================================
        // BOTONES DE PERIODO
        // =========================================

        private void BtnHoy_Click(object sender, RoutedEventArgs e)
        {
            CargarReporte(DateTime.Today, DateTime.Today, "Hoy");
        }

        private void BtnSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            var inicioSemana = hoy.AddDays(-diff);

            CargarReporte(inicioSemana, hoy, "Esta Semana");
        }

        private void BtnMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            CargarReporte(inicioMes, hoy, "Este Mes");
        }

        private void BtnAnio_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            var inicioAnio = new DateTime(hoy.Year, 1, 1);

            CargarReporte(inicioAnio, hoy, "Este Año");
        }

        // =========================================
        // CARGAR REPORTE PRINCIPAL
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

        // =========================================
        // TOTALES
        // =========================================

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

        // =========================================
        // PRODUCTOS VENDIDOS
        // =========================================

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

        // =========================================
        // GRAFICA POR DIA
        // =========================================

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

            // Convertir a barras visuales
            decimal maxValor = ventasPorDia.Any()
                ? ventasPorDia.Max(v => v.Total)
                : 1;

            if (maxValor == 0) maxValor = 1;

            var barras = ventasPorDia.Select(v => new BarraGrafica
            {
                Etiqueta = v.Fecha.ToString("dd/MM"),
                Valor = v.Total,
                AlturaBarra = (double)(v.Total / maxValor) * 200
            }).ToList();

            icGrafica.ItemsSource = barras;
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
                Productos = (dgProductosVendidos.ItemsSource as List<ReporteVentaItem>) ?? new()
            };

            // Parsear el total y número desde los TextBlock ya calculados
            string totalTexto = txtTotalVentas.Text.Replace("$", "").Replace(",", "");
            decimal.TryParse(totalTexto, out decimal total);
            resumen.TotalVentas = total;

            string numeroTexto = txtNumeroVentas.Text.Split(' ')[0];
            int.TryParse(numeroTexto, out int numero);
            resumen.NumeroVentas = numero;

            return resumen;
        }
    }
}