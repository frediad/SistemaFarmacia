
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FarmaciaPOS.Helpers;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.SqlClient;

namespace FarmaciaPOS.Views
{
    public partial class Graficas : Window
    {
        // Fecha usada como referencia para "día / semana / mes / año".
        // Se actualiza cuando el usuario elige una fecha en el calendario.
        private DateTime fechaSeleccionada = DateTime.Today;

        // Controla qué vista del menú está activa y si los datos de
        // "Ganancias" ya se cargaron alguna vez (carga perezosa).
        private bool vistaGananciasCargada = false;

        public Graficas()
        {
            InitializeComponent();

            txtFechaSeleccionada.Text = fechaSeleccionada.ToString("d 'de' MMMM 'de' yyyy",
                new System.Globalization.CultureInfo("es-MX"));

            MarcarMenuActivo(esGanancias: false);
            CargarTodoElDashboard();
        }

        private void CargarTodoElDashboard()
        {
            CargarTarjetasResumen();
            CargarGraficaDia();
            CargarGraficaMes();
            CargarTopProductos();
            CargarGraficaAnio();
            CargarBajoStock();
            CargarProximosACaducar();
            CargarEstadisticasInferiores();

            // Si el usuario ya había entrado a "Ganancias" y luego cambia la
            // fecha, refrescamos también esa vista para que no quede desfasada.
            if (vistaGananciasCargada)
            {
                CargarTodoElDashboardGanancias();
            }
        }

        private void CargarTodoElDashboardGanancias()
        {
            CargarTarjetasResumenGanancias();
            CargarGraficaGananciaDia();
            CargarGraficaGananciaMes();
            CargarTopProductosGanancia();
            CargarGraficaGananciaAnio();
            CargarMargenesProductos();
        }

        // =========================================
        // TARJETAS KPI (día / semana / mes / año) - VENTAS
        // =========================================

        private void CargarTarjetasResumen()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                // ---------- DÍA ----------
                DateTime diaInicio = fechaSeleccionada.Date;
                DateTime diaFin = diaInicio.AddDays(1);
                decimal ventasDia = ObtenerTotalVentas(cn, diaInicio, diaFin);
                decimal ventasDiaAnterior = ObtenerTotalVentas(cn, diaInicio.AddDays(-1), diaInicio);

                txtVentasDia.Text = ventasDia.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaDia, ventasDia, ventasDiaAnterior, "vs ayer");

                List<double> spDia = new();
                for (int i = 6; i >= 0; i--)
                {
                    DateTime d = diaInicio.AddDays(-i);
                    spDia.Add((double)ObtenerTotalVentas(cn, d, d.AddDays(1)));
                }
                DibujarSparkline(sparkDia, spDia, "#E53935");

                // ---------- SEMANA ----------
                DateTime semanaInicio = diaInicio.AddDays(-6);
                DateTime semanaFin = diaFin;
                decimal ventasSemana = ObtenerTotalVentas(cn, semanaInicio, semanaFin);
                decimal ventasSemanaAnterior = ObtenerTotalVentas(cn, semanaInicio.AddDays(-7), semanaInicio);

                txtVentasSemana.Text = ventasSemana.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaSemana, ventasSemana, ventasSemanaAnterior, "vs semana pasada");

                List<double> spSemana = new();
                for (int i = 7; i >= 0; i--)
                {
                    DateTime ini = semanaInicio.AddDays(-7 * i);
                    spSemana.Add((double)ObtenerTotalVentas(cn, ini, ini.AddDays(7)));
                }
                DibujarSparkline(sparkSemana, spSemana, "#1E88E5");

                // ---------- MES ----------
                DateTime mesInicio = new DateTime(fechaSeleccionada.Year, fechaSeleccionada.Month, 1);
                DateTime mesFin = mesInicio.AddMonths(1);
                decimal ventasMes = ObtenerTotalVentas(cn, mesInicio, mesFin);
                decimal ventasMesAnterior = ObtenerTotalVentas(cn, mesInicio.AddMonths(-1), mesInicio);

                txtVentasMes.Text = ventasMes.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaMes, ventasMes, ventasMesAnterior, "vs mes pasado");

                List<double> spMes = new();
                for (int i = 5; i >= 0; i--)
                {
                    DateTime ini = mesInicio.AddMonths(-i);
                    spMes.Add((double)ObtenerTotalVentas(cn, ini, ini.AddMonths(1)));
                }
                DibujarSparkline(sparkMes, spMes, "#43A047");

                // ---------- AÑO ----------
                DateTime anioInicio = new DateTime(fechaSeleccionada.Year, 1, 1);
                DateTime anioFin = anioInicio.AddYears(1);
                decimal ventasAnio = ObtenerTotalVentas(cn, anioInicio, anioFin);
                decimal ventasAnioAnterior = ObtenerTotalVentas(cn, anioInicio.AddYears(-1), anioInicio);

                txtVentasAnio.Text = ventasAnio.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaAnio, ventasAnio, ventasAnioAnterior, "vs año pasado");

                List<double> spAnio = new();
                for (int i = 5; i >= 0; i--)
                {
                    DateTime ini = anioInicio.AddYears(-i);
                    spAnio.Add((double)ObtenerTotalVentas(cn, ini, ini.AddYears(1)));
                }
                DibujarSparkline(sparkAnio, spAnio, "#8E24AA");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar las tarjetas de resumen:\n\n" + ex.Message);
            }
        }

        private decimal ObtenerTotalVentas(SqlConnection cn, DateTime desde, DateTime hasta)
        {
            string sql = @"SELECT ISNULL(SUM(Total),0) FROM Ventas
                            WHERE Fecha >= @Desde AND Fecha < @Hasta";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Desde", desde);
            cmd.Parameters.AddWithValue("@Hasta", hasta);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        // =========================================
        // TARJETAS KPI (día / semana / mes / año) - GANANCIAS
        // =========================================

        private void CargarTarjetasResumenGanancias()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                // ---------- DÍA ----------
                DateTime diaInicio = fechaSeleccionada.Date;
                DateTime diaFin = diaInicio.AddDays(1);
                decimal gananciaDia = ObtenerTotalGanancia(cn, diaInicio, diaFin);
                decimal gananciaDiaAnterior = ObtenerTotalGanancia(cn, diaInicio.AddDays(-1), diaInicio);

                txtGananciaDia.Text = gananciaDia.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaGananciaDia, gananciaDia, gananciaDiaAnterior, "vs ayer");

                List<double> spDia = new();
                for (int i = 6; i >= 0; i--)
                {
                    DateTime d = diaInicio.AddDays(-i);
                    spDia.Add((double)ObtenerTotalGanancia(cn, d, d.AddDays(1)));
                }
                DibujarSparkline(sparkGananciaDia, spDia, "#FB8C00");

                // ---------- SEMANA ----------
                DateTime semanaInicio = diaInicio.AddDays(-6);
                DateTime semanaFin = diaFin;
                decimal gananciaSemana = ObtenerTotalGanancia(cn, semanaInicio, semanaFin);
                decimal gananciaSemanaAnterior = ObtenerTotalGanancia(cn, semanaInicio.AddDays(-7), semanaInicio);

                txtGananciaSemana.Text = gananciaSemana.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaGananciaSemana, gananciaSemana, gananciaSemanaAnterior, "vs semana pasada");

                List<double> spSemana = new();
                for (int i = 7; i >= 0; i--)
                {
                    DateTime ini = semanaInicio.AddDays(-7 * i);
                    spSemana.Add((double)ObtenerTotalGanancia(cn, ini, ini.AddDays(7)));
                }
                DibujarSparkline(sparkGananciaSemana, spSemana, "#43A047");

                // ---------- MES ----------
                DateTime mesInicio = new DateTime(fechaSeleccionada.Year, fechaSeleccionada.Month, 1);
                DateTime mesFin = mesInicio.AddMonths(1);
                decimal gananciaMes = ObtenerTotalGanancia(cn, mesInicio, mesFin);
                decimal gananciaMesAnterior = ObtenerTotalGanancia(cn, mesInicio.AddMonths(-1), mesInicio);

                txtGananciaMes.Text = gananciaMes.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaGananciaMes, gananciaMes, gananciaMesAnterior, "vs mes pasado");

                List<double> spMes = new();
                for (int i = 5; i >= 0; i--)
                {
                    DateTime ini = mesInicio.AddMonths(-i);
                    spMes.Add((double)ObtenerTotalGanancia(cn, ini, ini.AddMonths(1)));
                }
                DibujarSparkline(sparkGananciaMes, spMes, "#1E88E5");

                // ---------- AÑO ----------
                DateTime anioInicio = new DateTime(fechaSeleccionada.Year, 1, 1);
                DateTime anioFin = anioInicio.AddYears(1);
                decimal gananciaAnio = ObtenerTotalGanancia(cn, anioInicio, anioFin);
                decimal gananciaAnioAnterior = ObtenerTotalGanancia(cn, anioInicio.AddYears(-1), anioInicio);

                txtGananciaAnio.Text = gananciaAnio.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
                ActualizarTendencia(txtTendenciaGananciaAnio, gananciaAnio, gananciaAnioAnterior, "vs año pasado");

                List<double> spAnio = new();
                for (int i = 5; i >= 0; i--)
                {
                    DateTime ini = anioInicio.AddYears(-i);
                    spAnio.Add((double)ObtenerTotalGanancia(cn, ini, ini.AddYears(1)));
                }
                DibujarSparkline(sparkGananciaAnio, spAnio, "#8E24AA");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar las tarjetas de ganancias:\n\n" + ex.Message);
            }
        }

        // Ganancia = SUM( Cantidad * (PrecioUnitario_venta - PrecioCompra_producto) )
        // Nota: usa el PrecioCompra ACTUAL del producto (no un costo histórico por lote),
        // ya que DetalleCompras.CostoUnitario no está vinculado directamente a cada venta.
        private decimal ObtenerTotalGanancia(SqlConnection cn, DateTime desde, DateTime hasta)
        {
            string sql = @"
                SELECT ISNULL(SUM(dv.Cantidad * (dv.PrecioUnitario - p.PrecioCompra)), 0)
                FROM DetalleVentas dv
                INNER JOIN Ventas v ON v.Id = dv.VentaId
                INNER JOIN Productos p ON p.Id = dv.ProductoId
                WHERE v.Fecha >= @Desde AND v.Fecha < @Hasta";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Desde", desde);
            cmd.Parameters.AddWithValue("@Hasta", hasta);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        // =========================================
        // UTILIDADES COMPARTIDAS (tendencia / sparkline)
        // =========================================

        private void ActualizarTendencia(TextBlock txt, decimal actual, decimal anterior, string etiqueta)
        {
            if (anterior == 0)
            {
                txt.Text = actual > 0 ? $"Nuevo {etiqueta}" : $"Sin datos {etiqueta}";
                txt.Foreground = Brushes.Gray;
                return;
            }

            decimal cambio = ((actual - anterior) / anterior) * 100m;
            bool subio = cambio >= 0;

            txt.Text = (subio ? "↗ " : "↘ ") + Math.Abs(cambio).ToString("0.0") + $"% {etiqueta}";
            txt.Foreground = subio
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0xC0, 0x6A))
                : new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
        }

        private void DibujarSparkline(Canvas canvas, List<double> valores, string colorHex)
        {
            canvas.Children.Clear();
            if (valores == null || valores.Count < 2) return;

            double min = valores.Min();
            double max = valores.Max();
            double rango = (max - min) == 0 ? 1 : (max - min);

            double w = canvas.Width;
            double h = canvas.Height;
            double paso = w / (valores.Count - 1);

            PointCollection puntos = new();
            for (int i = 0; i < valores.Count; i++)
            {
                double x = i * paso;
                double y = h - ((valores[i] - min) / rango) * h;
                puntos.Add(new Point(x, y));
            }

            Polyline linea = new Polyline
            {
                Points = puntos,
                Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            canvas.Children.Add(linea);
        }

        // =========================================
        // VENTAS POR DÍA (últimos 7 días)
        // =========================================

        private void CargarGraficaDia()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                DateTime inicio = fechaSeleccionada.Date.AddDays(-6);

                string sql = @"
                    SELECT CAST(Fecha AS DATE) AS Dia, SUM(Total) AS Total
                    FROM Ventas
                    WHERE Fecha >= @Inicio AND Fecha < @Fin
                    GROUP BY CAST(Fecha AS DATE)
                    ORDER BY Dia";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Inicio", inicio);
                cmd.Parameters.AddWithValue("@Fin", fechaSeleccionada.Date.AddDays(1));

                Dictionary<DateTime, double> porDia = new();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        porDia[Convert.ToDateTime(dr["Dia"])] = Convert.ToDouble(dr["Total"]);
                }

                List<double> valores = new();
                List<string> etiquetas = new();
                for (int i = 0; i <= 6; i++)
                {
                    DateTime d = inicio.AddDays(i);
                    valores.Add(porDia.TryGetValue(d, out double v) ? v : 0);
                    etiquetas.Add(d.ToString("d MMM", new System.Globalization.CultureInfo("es-MX")));
                }

                GraficaDia.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = valores,
                        Name = "Ventas ($)",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#3F7EF5"))
                    }
                };
                GraficaDia.XAxes = new Axis[] { new Axis { Labels = etiquetas } };
                GraficaDia.YAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de ventas por día:\n" + ex.Message);
            }
        }

        // =========================================
        // GANANCIA POR DÍA (últimos 7 días)
        // =========================================

        private void CargarGraficaGananciaDia()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                DateTime inicio = fechaSeleccionada.Date.AddDays(-6);

                string sql = @"
                    SELECT CAST(v.Fecha AS DATE) AS Dia,
                           SUM(dv.Cantidad * (dv.PrecioUnitario - p.PrecioCompra)) AS Total
                    FROM Ventas v
                    INNER JOIN DetalleVentas dv ON dv.VentaId = v.Id
                    INNER JOIN Productos p ON p.Id = dv.ProductoId
                    WHERE v.Fecha >= @Inicio AND v.Fecha < @Fin
                    GROUP BY CAST(v.Fecha AS DATE)
                    ORDER BY Dia";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Inicio", inicio);
                cmd.Parameters.AddWithValue("@Fin", fechaSeleccionada.Date.AddDays(1));

                Dictionary<DateTime, double> porDia = new();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        porDia[Convert.ToDateTime(dr["Dia"])] = Convert.ToDouble(dr["Total"]);
                }

                List<double> valores = new();
                List<string> etiquetas = new();
                for (int i = 0; i <= 6; i++)
                {
                    DateTime d = inicio.AddDays(i);
                    valores.Add(porDia.TryGetValue(d, out double v) ? v : 0);
                    etiquetas.Add(d.ToString("d MMM", new System.Globalization.CultureInfo("es-MX")));
                }

                GraficaGananciaDia.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = valores,
                        Name = "Ganancia ($)",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#FB8C00"))
                    }
                };
                GraficaGananciaDia.XAxes = new Axis[] { new Axis { Labels = etiquetas } };
                GraficaGananciaDia.YAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de ganancia por día:\n" + ex.Message);
            }
        }

        // =========================================
        // VENTAS POR MES (este año)
        // =========================================

        private void CargarGraficaMes()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                string sql = @"
                    SELECT MONTH(Fecha) AS Mes, SUM(Total) AS Total
                    FROM Ventas
                    WHERE YEAR(Fecha) = @Anio
                    GROUP BY MONTH(Fecha)
                    ORDER BY Mes";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Anio", fechaSeleccionada.Year);

                double[] valores = new double[12];
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int mes = Convert.ToInt32(dr["Mes"]) - 1;
                        valores[mes] = Convert.ToDouble(dr["Total"]);
                    }
                }

                string[] meses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

                GraficaMes.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = valores,
                        Name = "Ventas ($)",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#2EC06A").WithAlpha(40)),
                        Stroke = new SolidColorPaint(SkiaSharp.SKColor.Parse("#2EC06A"), 3),
                        GeometrySize = 6
                    }
                };
                GraficaMes.XAxes = new Axis[] { new Axis { Labels = meses } };
                GraficaMes.YAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de ventas por mes:\n" + ex.Message);
            }
        }

        // =========================================
        // GANANCIA POR MES (este año)
        // =========================================

        private void CargarGraficaGananciaMes()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                string sql = @"
                    SELECT MONTH(v.Fecha) AS Mes,
                           SUM(dv.Cantidad * (dv.PrecioUnitario - p.PrecioCompra)) AS Total
                    FROM Ventas v
                    INNER JOIN DetalleVentas dv ON dv.VentaId = v.Id
                    INNER JOIN Productos p ON p.Id = dv.ProductoId
                    WHERE YEAR(v.Fecha) = @Anio
                    GROUP BY MONTH(v.Fecha)
                    ORDER BY Mes";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Anio", fechaSeleccionada.Year);

                double[] valores = new double[12];
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        int mes = Convert.ToInt32(dr["Mes"]) - 1;
                        valores[mes] = Convert.ToDouble(dr["Total"]);
                    }
                }

                string[] meses = { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

                GraficaGananciaMes.Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = valores,
                        Name = "Ganancia ($)",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#FB8C00").WithAlpha(40)),
                        Stroke = new SolidColorPaint(SkiaSharp.SKColor.Parse("#FB8C00"), 3),
                        GeometrySize = 6
                    }
                };
                GraficaGananciaMes.XAxes = new Axis[] { new Axis { Labels = meses } };
                GraficaGananciaMes.YAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de ganancia por mes:\n" + ex.Message);
            }
        }

        // =========================================
        // PRODUCTOS MÁS VENDIDOS (top 5)
        // =========================================

        private void CargarTopProductos()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                string sql = @"
                    SELECT TOP 5 p.Nombre AS Nombre, SUM(dv.Cantidad) AS Total
                    FROM DetalleVentas dv
                    INNER JOIN Productos p ON p.Id = dv.ProductoId
                    GROUP BY p.Nombre
                    ORDER BY Total DESC";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                using SqlDataReader dr = cmd.ExecuteReader();

                List<double> valores = new();
                List<string> etiquetas = new();

                while (dr.Read())
                {
                    etiquetas.Add(dr["Nombre"].ToString());
                    valores.Add(Convert.ToDouble(dr["Total"]));
                }

                GraficaTopProductos.Series = new ISeries[]
                {
                    new RowSeries<double>
                    {
                        Values = valores,
                        Name = "Unidades vendidas",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#A08CF5"))
                    }
                };
                GraficaTopProductos.YAxes = new Axis[] { new Axis { Labels = etiquetas } };
                GraficaTopProductos.XAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de productos más vendidos:\n" + ex.Message);
            }
        }

        // =========================================
        // PRODUCTOS QUE MÁS GANANCIA GENERAN (top 5)
        // =========================================

        private void CargarTopProductosGanancia()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                string sql = @"
                    SELECT TOP 5 p.Nombre AS Nombre,
                           SUM(dv.Cantidad * (dv.PrecioUnitario - p.PrecioCompra)) AS Total
                    FROM DetalleVentas dv
                    INNER JOIN Productos p ON p.Id = dv.ProductoId
                    GROUP BY p.Nombre
                    ORDER BY Total DESC";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                using SqlDataReader dr = cmd.ExecuteReader();

                List<double> valores = new();
                List<string> etiquetas = new();

                while (dr.Read())
                {
                    etiquetas.Add(dr["Nombre"].ToString());
                    valores.Add(Convert.ToDouble(dr["Total"]));
                }

                GraficaTopProductosGanancia.Series = new ISeries[]
                {
                    new RowSeries<double>
                    {
                        Values = valores,
                        Name = "Ganancia ($)",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#FB8C00"))
                    }
                };
                GraficaTopProductosGanancia.YAxes = new Axis[] { new Axis { Labels = etiquetas } };
                GraficaTopProductosGanancia.XAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de productos que más ganancia generan:\n" + ex.Message);
            }
        }

        // =========================================
        // VENTAS POR AÑO (últimos 6 años)
        // =========================================

        private void CargarGraficaAnio()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                int anioActual = fechaSeleccionada.Year;
                int anioInicio = anioActual - 5;

                string sql = @"
                    SELECT YEAR(Fecha) AS Anio, SUM(Total) AS Total
                    FROM Ventas
                    WHERE YEAR(Fecha) >= @AnioInicio
                    GROUP BY YEAR(Fecha)
                    ORDER BY Anio";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@AnioInicio", anioInicio);

                Dictionary<int, double> porAnio = new();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        porAnio[Convert.ToInt32(dr["Anio"])] = Convert.ToDouble(dr["Total"]);
                }

                List<double> valores = new();
                List<string> etiquetas = new();
                for (int a = anioInicio; a <= anioActual; a++)
                {
                    valores.Add(porAnio.TryGetValue(a, out double v) ? v : 0);
                    etiquetas.Add(a.ToString());
                }

                GraficaAnio.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = valores,
                        Name = "Ventas ($)",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#7C6CF0"))
                    }
                };
                GraficaAnio.XAxes = new Axis[] { new Axis { Labels = etiquetas } };
                GraficaAnio.YAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de ventas por año:\n" + ex.Message);
            }
        }

        // =========================================
        // GANANCIA POR AÑO (últimos 6 años)
        // =========================================

        private void CargarGraficaGananciaAnio()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                int anioActual = fechaSeleccionada.Year;
                int anioInicio = anioActual - 5;

                string sql = @"
                    SELECT YEAR(v.Fecha) AS Anio,
                           SUM(dv.Cantidad * (dv.PrecioUnitario - p.PrecioCompra)) AS Total
                    FROM Ventas v
                    INNER JOIN DetalleVentas dv ON dv.VentaId = v.Id
                    INNER JOIN Productos p ON p.Id = dv.ProductoId
                    WHERE YEAR(v.Fecha) >= @AnioInicio
                    GROUP BY YEAR(v.Fecha)
                    ORDER BY Anio";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@AnioInicio", anioInicio);

                Dictionary<int, double> porAnio = new();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                        porAnio[Convert.ToInt32(dr["Anio"])] = Convert.ToDouble(dr["Total"]);
                }

                List<double> valores = new();
                List<string> etiquetas = new();
                for (int a = anioInicio; a <= anioActual; a++)
                {
                    valores.Add(porAnio.TryGetValue(a, out double v) ? v : 0);
                    etiquetas.Add(a.ToString());
                }

                GraficaGananciaAnio.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = valores,
                        Name = "Ganancia ($)",
                        Fill = new SolidColorPaint(SkiaSharp.SKColor.Parse("#7C6CF0"))
                    }
                };
                GraficaGananciaAnio.XAxes = new Axis[] { new Axis { Labels = etiquetas } };
                GraficaGananciaAnio.YAxes = new Axis[] { new Axis() };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en gráfica de ganancia por año:\n" + ex.Message);
            }
        }

        // =========================================
        // PRODUCTOS CON BAJO STOCK
        // =========================================

        private class ProductoBajoStock
        {
            public string Nombre { get; set; }
            public string StockTexto { get; set; }
        }

        private void CargarBajoStock()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                // Umbral de "bajo stock": 20 unidades. Ajusta @Umbral si tu regla de negocio es otra.
                string sql = @"
                    SELECT TOP 5 Nombre, Stock
                    FROM Productos
                    WHERE Stock <= @Umbral
                    ORDER BY Stock ASC";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Umbral", 20);
                using SqlDataReader dr = cmd.ExecuteReader();

                List<ProductoBajoStock> lista = new();
                while (dr.Read())
                {
                    lista.Add(new ProductoBajoStock
                    {
                        Nombre = dr["Nombre"].ToString(),
                        StockTexto = Convert.ToInt32(dr["Stock"]) + " unidades"
                    });
                }

                listaBajoStock.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos con bajo stock:\n" + ex.Message);
            }
        }

        // =========================================
        // PRÓXIMOS A CADUCAR
        // =========================================

        private class ProductoCaducar
        {
            public string Nombre { get; set; }
            public string FechaTexto { get; set; }
        }

        private void CargarProximosACaducar()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                // Ventana de 60 días. Ajusta @Dias si tu regla de negocio es otra.
                string sql = @"
                    SELECT TOP 5 p.Nombre AS Nombre, MIN(l.FechaCaducidad) AS FechaCaducidad
                    FROM LotesProductos l
                    INNER JOIN Productos p ON p.Id = l.ProductoId
                    WHERE l.FechaCaducidad BETWEEN GETDATE() AND DATEADD(DAY, @Dias, GETDATE())
                    GROUP BY p.Nombre
                    ORDER BY FechaCaducidad ASC";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@Dias", 60);
                using SqlDataReader dr = cmd.ExecuteReader();

                List<ProductoCaducar> lista = new();
                while (dr.Read())
                {
                    DateTime fecha = Convert.ToDateTime(dr["FechaCaducidad"]);
                    lista.Add(new ProductoCaducar
                    {
                        Nombre = dr["Nombre"].ToString(),
                        FechaTexto = fecha.ToString("dd/MM/yyyy")
                    });
                }

                listaCaducar.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos próximos a caducar:\n" + ex.Message);
            }
        }

        // =========================================
        // MAYOR / MENOR MARGEN DE GANANCIA POR PRODUCTO
        // =========================================

        private class ProductoMargen
        {
            public string Nombre { get; set; }
            public string MargenTexto { get; set; }
        }

        private void CargarMargenesProductos()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                // Margen % = (PrecioVenta - PrecioCompra) / PrecioCompra * 100
                string sqlBase = @"
                    SELECT Nombre,
                           CASE WHEN PrecioCompra > 0
                                THEN ((PrecioVenta - PrecioCompra) / PrecioCompra) * 100
                                ELSE 0 END AS Margen
                    FROM Productos
                    WHERE PrecioCompra > 0";

                // ---------- Mayor margen ----------
                List<ProductoMargen> mayores = new();
                using (SqlCommand cmd = new SqlCommand(sqlBase + " ORDER BY Margen DESC", cn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    int contador = 0;
                    while (dr.Read() && contador < 5)
                    {
                        mayores.Add(new ProductoMargen
                        {
                            Nombre = dr["Nombre"].ToString(),
                            MargenTexto = Convert.ToDouble(dr["Margen"]).ToString("0.0") + "%"
                        });
                        contador++;
                    }
                }
                listaMayorMargen.ItemsSource = mayores;

                // ---------- Menor margen ----------
                List<ProductoMargen> menores = new();
                using (SqlCommand cmd = new SqlCommand(sqlBase + " ORDER BY Margen ASC", cn))
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    int contador = 0;
                    while (dr.Read() && contador < 5)
                    {
                        menores.Add(new ProductoMargen
                        {
                            Nombre = dr["Nombre"].ToString(),
                            MargenTexto = Convert.ToDouble(dr["Margen"]).ToString("0.0") + "%"
                        });
                        contador++;
                    }
                }
                listaMenorMargen.ItemsSource = menores;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar márgenes de productos:\n" + ex.Message);
            }
        }

        // =========================================
        // FRANJA DE ESTADÍSTICAS INFERIORES
        // =========================================

        private void CargarEstadisticasInferiores()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                txtTotalProductos.Text = Convert.ToInt32(
                    Ejecutar(cn, "SELECT COUNT(*) FROM Productos")).ToString("N0");

                txtTotalClientes.Text = Convert.ToInt32(
                    Ejecutar(cn, "SELECT COUNT(*) FROM Clientes")).ToString("N0");

                txtConStock.Text = Convert.ToInt32(
                    Ejecutar(cn, "SELECT COUNT(*) FROM Productos WHERE Stock > 0")).ToString("N0");

                txtSinStock.Text = Convert.ToInt32(
                    Ejecutar(cn, "SELECT COUNT(*) FROM Productos WHERE Stock <= 0")).ToString("N0");

                decimal totalAnio = ObtenerTotalVentas(
                    cn,
                    new DateTime(fechaSeleccionada.Year, 1, 1),
                    new DateTime(fechaSeleccionada.Year, 1, 1).AddYears(1));

                txtTotalVentas.Text = totalAnio.ToString("C2", new System.Globalization.CultureInfo("es-MX"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar las estadísticas inferiores:\n" + ex.Message);
            }
        }

        private object Ejecutar(SqlConnection cn, string sql)
        {
            using SqlCommand cmd = new SqlCommand(sql, cn);
            return cmd.ExecuteScalar();
        }

        // =========================================
        // MENÚ: VENTAS / GANANCIAS
        // =========================================

        private void btnMenuVentas_Click(object sender, RoutedEventArgs e)
        {
            MarcarMenuActivo(esGanancias: false);
        }

        private void btnMenuGanancias_Click(object sender, RoutedEventArgs e)
        {
            MarcarMenuActivo(esGanancias: true);

            // Carga perezosa: solo consultamos ganancias la primera vez que
            // el usuario entra a esta pestaña (o si cambió la fecha, ver CargarTodoElDashboard).
            if (!vistaGananciasCargada)
            {
                CargarTodoElDashboardGanancias();
                vistaGananciasCargada = true;
            }
        }

        private void MarcarMenuActivo(bool esGanancias)
        {
            vistaVentas.Visibility = esGanancias ? Visibility.Collapsed : Visibility.Visible;
            vistaGanancias.Visibility = esGanancias ? Visibility.Visible : Visibility.Collapsed;

            if (esGanancias)
            {
                txtTituloDashboard.Text = "Dashboard de Ganancias";
                txtSubtituloDashboard.Text = "Resumen general de utilidades y márgenes";

                btnMenuGanancias.Background = new SolidColorBrush(Color.FromRgb(0xFB, 0x8C, 0x00));
                btnMenuGanancias.Foreground = Brushes.White;
                btnMenuGanancias.BorderBrush = Brushes.Transparent;

                btnMenuVentas.Background = Brushes.White;
                btnMenuVentas.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#4B5468");
                btnMenuVentas.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#E3E6EE");
            }
            else
            {
                txtTituloDashboard.Text = "Dashboard de Ventas";
                txtSubtituloDashboard.Text = "Resumen general de ventas y productos";

                btnMenuVentas.Background = new SolidColorBrush(Color.FromRgb(0x19, 0x76, 0xD2));
                btnMenuVentas.Foreground = Brushes.White;
                btnMenuVentas.BorderBrush = Brushes.Transparent;

                btnMenuGanancias.Background = Brushes.White;
                btnMenuGanancias.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#4B5468");
                btnMenuGanancias.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#E3E6EE");
            }
        }

        // =========================================
        // EVENTOS
        // =========================================

        private void calFecha_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (calFecha.SelectedDate == null) return;

            fechaSeleccionada = calFecha.SelectedDate.Value;
            txtFechaSeleccionada.Text = fechaSeleccionada.ToString("d 'de' MMMM 'de' yyyy",
                new System.Globalization.CultureInfo("es-MX"));

            CargarTodoElDashboard();
        }

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
