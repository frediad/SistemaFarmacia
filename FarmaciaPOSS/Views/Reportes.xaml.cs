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

        private DateTime fechaInicioMasVendidos, fechaFinMasVendidos;
        private DateTime fechaInicioGanancias, fechaFinGanancias;
        private DateTime fechaInicioInventario, fechaFinInventario;
        private DateTime fechaInicioPedidos, fechaFinPedidos;
        private string periodoPedidos = "Hoy";

        // ✅ Nuevo — pestaña de Caja
        private DateTime fechaInicioCaja, fechaFinCaja;
        private string periodoCaja = "Hoy";

        public class BarraGrafica
        {
            public string Etiqueta { get; set; } = string.Empty;
            public double AlturaBarra { get; set; }
            public decimal Valor { get; set; }
        }

        public ReportesWindow()
        {
            InitializeComponent();

            CargarSeguro(() => CargarReporte(DateTime.Today, DateTime.Today, "Hoy"), "Ventas");
            CargarSeguro(() => CargarMasVendidos(DateTime.Today, DateTime.Today), "Más Vendidos");
            CargarSeguro(() => CargarGanancias(DateTime.Today, DateTime.Today), "Ganancias");
            CargarSeguro(() => CargarInventario(DateTime.Today, DateTime.Today), "Inventario");
            CargarSeguro(() => CargarPedidos(DateTime.Today, DateTime.Today, "Hoy"), "Pedidos");
            CargarSeguro(() => CargarCaja(DateTime.Today, DateTime.Today, "Hoy"), "Caja");
        }

        // =========================================
        // CARGA SEGURA
        // =========================================

        private void CargarSeguro(Action accion, string nombreSeccion)
        {
            try
            {
                accion();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(
                    $"No se pudo cargar la sección \"{nombreSeccion}\":\n{ex.Message}\n\n" +
                    "Revisa que la tabla y columnas correspondientes existan en tu base de datos. " +
                    "Las demás pestañas seguirán funcionando normalmente.",
                    "Aviso",
                    this);
            }
        }

        // =========================================
        // ✅ RANGO PERSONALIZADO — aplica según la pestaña activa
        // =========================================

        private void BtnAplicarRango_Click(object sender, RoutedEventArgs e)
        {
            if (dpDesde.SelectedDate == null || dpHasta.SelectedDate == null)
            {
                MensajeHelper.Advertencia("Selecciona ambas fechas (Desde y Hasta)", "Aviso", this);
                return;
            }

            DateTime desde = dpDesde.SelectedDate.Value.Date;
            DateTime hasta = dpHasta.SelectedDate.Value.Date;

            if (hasta < desde)
            {
                MensajeHelper.Advertencia("La fecha 'Hasta' no puede ser anterior a 'Desde'", "Aviso", this);
                return;
            }

            string etiqueta = desde == hasta
                ? desde.ToString("dd-MM-yyyy")
                : $"{desde:dd-MM-yyyy} al {hasta:dd-MM-yyyy}";

            switch (tabReportes.SelectedIndex)
            {
                case 0:
                    CargarSeguro(() => CargarReporte(desde, hasta, etiqueta), "Ventas");
                    break;
                case 1:
                    CargarSeguro(() => CargarMasVendidos(desde, hasta), "Más Vendidos");
                    break;
                case 2:
                    CargarSeguro(() => CargarGanancias(desde, hasta), "Ganancias");
                    break;
                case 3:
                    CargarSeguro(() => CargarInventario(desde, hasta), "Inventario");
                    break;
                case 4:
                    CargarSeguro(() => CargarPedidos(desde, hasta, etiqueta), "Pedidos");
                    break;
                case 5:
                    CargarSeguro(() => CargarCaja(desde, hasta, etiqueta), "Caja");
                    break;
            }

            MensajeHelper.Exito(
                $"Reporte actualizado para el periodo: {etiqueta}",
                "Rango aplicado",
                this);
        }

        // =========================================
        // PESTAÑA VENTAS — BOTONES PERIODO
        // =========================================

        private void BtnHoy_Click(object sender, RoutedEventArgs e)
            => CargarSeguro(() => CargarReporte(DateTime.Today, DateTime.Today, "Hoy"), "Ventas");

        private void BtnSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarSeguro(() => CargarReporte(hoy.AddDays(-diff), hoy, "Esta Semana"), "Ventas");
        }

        private void BtnMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarReporte(new DateTime(hoy.Year, hoy.Month, 1), hoy, "Este Mes"), "Ventas");
        }

        private void BtnAño_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarReporte(new DateTime(hoy.Year, 1, 1), hoy, "Este Año"), "Ventas");
        }

        // =========================================
        // PESTAÑA MÁS VENDIDOS — BOTONES PERIODO
        // =========================================

        private void BtnMasVendidosHoy_Click(object sender, RoutedEventArgs e)
            => CargarSeguro(() => CargarMasVendidos(DateTime.Today, DateTime.Today), "Más Vendidos");

        private void BtnMasVendidosSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarSeguro(() => CargarMasVendidos(hoy.AddDays(-diff), hoy), "Más Vendidos");
        }

        private void BtnMasVendidosMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarMasVendidos(new DateTime(hoy.Year, hoy.Month, 1), hoy), "Más Vendidos");
        }

        private void BtnMasVendidosAño_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarMasVendidos(new DateTime(hoy.Year, 1, 1), hoy), "Más Vendidos");
        }

        // =========================================
        // PESTAÑA GANANCIAS — BOTONES PERIODO
        // =========================================

        private void BtnGananciasHoy_Click(object sender, RoutedEventArgs e)
            => CargarSeguro(() => CargarGanancias(DateTime.Today, DateTime.Today), "Ganancias");

        private void BtnGananciasSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarSeguro(() => CargarGanancias(hoy.AddDays(-diff), hoy), "Ganancias");
        }

        private void BtnGananciasMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarGanancias(new DateTime(hoy.Year, hoy.Month, 1), hoy), "Ganancias");
        }

        private void BtnGananciasAño_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarGanancias(new DateTime(hoy.Year, 1, 1), hoy), "Ganancias");
        }

        // =========================================
        // PESTAÑA INVENTARIO — BOTONES PERIODO
        // =========================================

        private void BtnInventarioHoy_Click(object sender, RoutedEventArgs e)
            => CargarSeguro(() => CargarInventario(DateTime.Today, DateTime.Today), "Inventario");

        private void BtnInventarioSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarSeguro(() => CargarInventario(hoy.AddDays(-diff), hoy), "Inventario");
        }

        private void BtnInventarioMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarInventario(new DateTime(hoy.Year, hoy.Month, 1), hoy), "Inventario");
        }

        private void BtnInventarioAño_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarInventario(new DateTime(hoy.Year, 1, 1), hoy), "Inventario");
        }

        // =========================================
        // PESTAÑA PEDIDOS — BOTONES PERIODO
        // =========================================

        private void BtnPedidosHoy_Click(object sender, RoutedEventArgs e)
            => CargarSeguro(() => CargarPedidos(DateTime.Today, DateTime.Today, "Hoy"), "Pedidos");

        private void BtnPedidosSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarSeguro(() => CargarPedidos(hoy.AddDays(-diff), hoy, "Esta Semana"), "Pedidos");
        }

        private void BtnPedidosMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarPedidos(new DateTime(hoy.Year, hoy.Month, 1), hoy, "Este Mes"), "Pedidos");
        }

        private void BtnPedidosAño_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarPedidos(new DateTime(hoy.Year, 1, 1), hoy, "Este Año"), "Pedidos");
        }

        // =========================================
        // ✅ PESTAÑA CAJA — BOTONES PERIODO
        // =========================================

        private void BtnCajaHoy_Click(object sender, RoutedEventArgs e)
            => CargarSeguro(() => CargarCaja(DateTime.Today, DateTime.Today, "Hoy"), "Caja");

        private void BtnCajaSemana_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
            CargarSeguro(() => CargarCaja(hoy.AddDays(-diff), hoy, "Esta Semana"), "Caja");
        }

        private void BtnCajaMes_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarCaja(new DateTime(hoy.Year, hoy.Month, 1), hoy, "Este Mes"), "Caja");
        }

        private void BtnCajaAño_Click(object sender, RoutedEventArgs e)
        {
            var hoy = DateTime.Today;
            CargarSeguro(() => CargarCaja(new DateTime(hoy.Year, 1, 1), hoy, "Este Año"), "Caja");
        }

        // =========================================
        // VENTAS
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
        SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0) AS Cantidad,
        SUM(dv.Subtotal) - ISNULL(SUM(dev.MontoDevuelto), 0) AS Total
      FROM DetalleVentas dv
      INNER JOIN Ventas v ON dv.VentaId = v.Id
      INNER JOIN Productos p ON dv.ProductoId = p.Id
      LEFT JOIN (
          SELECT VentaId, ProductoId, SUM(Cantidad) AS CantidadDevuelta, SUM(MontoDevuelto) AS MontoDevuelto
          FROM Devoluciones
          GROUP BY VentaId, ProductoId
      ) dev ON dev.VentaId = dv.VentaId AND dev.ProductoId = dv.ProductoId
      WHERE v.Fecha >= @Desde AND v.Fecha < @Hasta
      AND v.Estado = 'Completada'
      GROUP BY p.Nombre
      HAVING SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0) > 0
      ORDER BY (SUM(dv.Subtotal) - ISNULL(SUM(dev.MontoDevuelto), 0)) DESC";

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
        // MÁS VENDIDOS
        // =========================================

        private void CargarMasVendidos(DateTime desde, DateTime hasta)
        {
            fechaInicioMasVendidos = desde;
            fechaFinMasVendidos = hasta;

            List<ReporteMasVendidoItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT TOP 10
        ROW_NUMBER() OVER (ORDER BY (SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0)) DESC) AS Posicion,
        p.Nombre AS Producto,
        SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0) AS Cantidad,
        SUM(dv.Subtotal) - ISNULL(SUM(dev.MontoDevuelto), 0) AS Total
      FROM DetalleVentas dv
      INNER JOIN Ventas v ON dv.VentaId = v.Id
      INNER JOIN Productos p ON dv.ProductoId = p.Id
      LEFT JOIN (
          SELECT VentaId, ProductoId, SUM(Cantidad) AS CantidadDevuelta, SUM(MontoDevuelto) AS MontoDevuelto
          FROM Devoluciones
          GROUP BY VentaId, ProductoId
      ) dev ON dev.VentaId = dv.VentaId AND dev.ProductoId = dv.ProductoId
      WHERE v.Fecha >= @Desde AND v.Fecha < @Hasta
      AND v.Estado = 'Completada'
      GROUP BY p.Nombre
      HAVING SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0) > 0
      ORDER BY (SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0)) DESC";

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
        // GANANCIAS
        // =========================================

        private void CargarGanancias(DateTime desde, DateTime hasta)
        {
            fechaInicioGanancias = desde;
            fechaFinGanancias = hasta;

            List<ReporteGananciaItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT p.Nombre AS Producto,
            SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0) AS Cantidad,
            SUM(dv.Subtotal) - ISNULL(SUM(dev.MontoDevuelto), 0) AS Ingreso,
            (SUM(dv.Cantidad) - ISNULL(SUM(dev.CantidadDevuelta), 0)) * MAX(p.PrecioCompra) AS Costo
            FROM DetalleVentas dv
            INNER JOIN Ventas v ON dv.VentaId = v.Id
            INNER JOIN Productos p ON dv.ProductoId = p.Id
            LEFT JOIN (
            SELECT VentaId, ProductoId, SUM(Cantidad) AS CantidadDevuelta, SUM(MontoDevuelto) AS MontoDevuelto
            FROM Devoluciones
            GROUP BY VentaId, ProductoId
            ) dev ON dev.VentaId = dv.VentaId AND dev.ProductoId = dv.ProductoId
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
        // INVENTARIO
        // =========================================

        private void CargarInventario(DateTime desde, DateTime hasta)
        {
            fechaInicioInventario = desde;
            fechaFinInventario = hasta;

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
        // PEDIDOS
        // =========================================

        private void CargarPedidos(DateTime desde, DateTime hasta, string etiquetaPeriodo)
        {
            fechaInicioPedidos = desde;
            fechaFinPedidos = hasta;
            periodoPedidos = etiquetaPeriodo;

            List<ReportePedidoItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
        p.Id,
        p.NumeroPedido,
        c.Nombre AS Cliente,
        p.EstadoPedido,
        p.Total,
        p.FechaPedido
      FROM Pedidos p
      INNER JOIN Clientes c ON p.ClienteId = c.Id
      WHERE p.FechaPedido >= @Desde AND p.FechaPedido < @Hasta
      ORDER BY p.FechaPedido DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new ReportePedidoItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    NumeroPedido = reader["NumeroPedido"].ToString() ?? "",
                    Cliente = reader["Cliente"].ToString() ?? "",
                    Estado = reader["EstadoPedido"].ToString() ?? "",
                    Total = Convert.ToDecimal(reader["Total"]),
                    Fecha = Convert.ToDateTime(reader["FechaPedido"])
                });
            }

            dgPedidos.ItemsSource = lista;

            txtPedidosPendientes.Text = lista.Count(p => p.Estado == "Pendiente").ToString();
            txtPedidosPreparando.Text = lista.Count(p => p.Estado == "Preparando").ToString();
            txtPedidosEntregados.Text = lista.Count(p => p.Estado == "Entregado").ToString();
            txtPedidosCancelados.Text = lista.Count(p => p.Estado == "Cancelado").ToString();
        }

        // =========================================
        // ✅ CAJA — quién abrió/cerró y montos
        // =========================================

        public class ReporteCajaItem
        {
            public int Id { get; set; }
            public string Usuario { get; set; } = "";
            public DateTime FechaApertura { get; set; }
            public DateTime? FechaCierre { get; set; }
            public decimal MontoInicial { get; set; }
            public decimal MontoFinalEsperado { get; set; }
            public decimal MontoFinalContado { get; set; }
            public decimal Diferencia { get; set; }
            public string Estado { get; set; } = "";
        }

        private void CargarCaja(DateTime desde, DateTime hasta, string etiquetaPeriodo)
        {
            fechaInicioCaja = desde;
            fechaFinCaja = hasta;
            periodoCaja = etiquetaPeriodo;

            txtPeriodoCaja.Text = $"Periodo: {etiquetaPeriodo}";

            List<ReporteCajaItem> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                c.Id,
                ISNULL(u.Nombre, 'Desconocido') AS Usuario,
                c.FechaApertura,
                c.FechaCierre,
                c.MontoInicial,
                c.MontoFinalEsperado,
                c.MontoFinalContado,
                c.Diferencia,
                c.Estado
              FROM Caja c
              LEFT JOIN Usuarios u ON c.UsuarioId = u.Id
              WHERE c.FechaApertura >= @Desde AND c.FechaApertura < @Hasta
              ORDER BY c.FechaApertura DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde.Date);
            cmd.Parameters.AddWithValue("@Hasta", hasta.Date.AddDays(1));

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                decimal? esperado = reader["MontoFinalEsperado"] != DBNull.Value
                    ? Convert.ToDecimal(reader["MontoFinalEsperado"]) : null;
                decimal? contado = reader["MontoFinalContado"] != DBNull.Value
                    ? Convert.ToDecimal(reader["MontoFinalContado"]) : null;
                decimal? diferencia = reader["Diferencia"] != DBNull.Value
                    ? Convert.ToDecimal(reader["Diferencia"]) : null;

                string estadoCrudo = reader["Estado"].ToString() ?? "";

                string estadoTexto =
                    estadoCrudo != "CERRADA" ? "🟢 Abierta" :
                    diferencia == null ? "Sin corte" :
                    diferencia == 0 ? "✅ Cuadre exacto" :
                    diferencia > 0 ? "🟡 Sobrante" :
                                     "🔴 Faltante";

                lista.Add(new ReporteCajaItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Usuario = reader["Usuario"].ToString() ?? "",
                    FechaApertura = Convert.ToDateTime(reader["FechaApertura"]),
                    FechaCierre = reader["FechaCierre"] != DBNull.Value
                        ? Convert.ToDateTime(reader["FechaCierre"]) : (DateTime?)null,
                    MontoInicial = Convert.ToDecimal(reader["MontoInicial"]),
                    MontoFinalEsperado = esperado ?? 0,
                    MontoFinalContado = contado ?? 0,
                    Diferencia = diferencia ?? 0,
                    Estado = estadoTexto
                });
            }

            dgCortesCaja.ItemsSource = lista;

            txtCajaTotalCortes.Text = lista.Count.ToString();
            txtCajaTotalEsperado.Text = lista.Sum(x => x.MontoFinalEsperado).ToString("C");
            txtCajaTotalContado.Text = lista.Sum(x => x.MontoFinalContado).ToString("C");
            txtCajaTotalDiferencia.Text = lista.Sum(x => x.Diferencia).ToString("C");
        }

        // =========================================
        // EXPORTAR
        // =========================================

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (tabReportes.SelectedIndex)
                {
                    case 0:
                        ExportarVentasPDF();
                        break;

                    case 1:
                        ExportarMasVendidosPDF();
                        break;

                    case 2:
                        ExportarGananciasPDF();
                        break;

                    case 3:
                        ExportarInventarioPDF();
                        break;

                    case 4:
                        ExportarPedidosPDF();
                        break;

                    case 5:
                        ExportarCajaPDF();
                        break;
                }
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("Error al exportar: " + ex.Message, "Error", this);
            }
        }

        private string RutaReporte(string subcarpeta, string nombreArchivo)
        {
            string carpetaBase = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string carpetaDestino = System.IO.Path.Combine(carpetaBase, "FarmaClick Yatzil", "Reportes", subcarpeta);

            if (!System.IO.Directory.Exists(carpetaDestino))
            {
                System.IO.Directory.CreateDirectory(carpetaDestino);
            }

            return System.IO.Path.Combine(carpetaDestino, nombreArchivo);
        }
        private string SanitizarNombreArchivo(string texto)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                texto = texto.Replace(c, '-');
            }
            return texto;
        }

        private void MostrarExito(string ruta)
        {
            MensajeHelper.Exito($"Reporte guardado en:\n{ruta}", "Éxito", this);
        }

        private void ExportarVentasPDF()
        {
            var resumen = ObtenerResumenCompleto();

            string periodoSeguro = SanitizarNombreArchivo(periodoActual.Replace(" ", "_"));

            string ruta = RutaReporte("Ventas", $"Reporte_Ventas_{periodoSeguro}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            ReportePdfGenerator.GenerarReporte(resumen, ruta, periodoActual);

            MostrarExito(ruta);
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

        private void ExportarMasVendidosPDF()
        {
            var masVendidos = (dgMasVendidos.ItemsSource as List<ReporteMasVendidoItem>) ?? new();
            var sinMovimiento = (dgSinMovimiento.ItemsSource as List<ProductoStockItem>) ?? new();

            var filas = masVendidos
                .Select(x => new List<string>
                {
                    x.Posicion.ToString(),
                    x.Producto,
                    x.Cantidad.ToString(),
                    x.Total.ToString("C")
                })
                .ToList();

            var filasSinMovimiento = sinMovimiento
                .Select(x => new List<string>
                {
                    x.Nombre,
                    x.Stock.ToString(),
                    x.StockMinimo.ToString()
                })
                .ToList();

            string ruta = RutaReporte("MasVendidos", $"Reporte_MasVendidos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            ReportePdfGenerator.GenerarReporteGenerico(
                tituloReporte: "Productos Más Vendidos",
                periodo: "Personalizado",
                desde: fechaInicioMasVendidos,
                hasta: fechaFinMasVendidos,
                tarjetasResumen: new() { ("Total de productos en Top", masVendidos.Count.ToString()) },
                encabezados: new() { "#", "Producto", "Cantidad", "Total" },
                filas: filas,
                rutaArchivo: ruta,
                tituloTablaSecundaria: "Productos Sin Movimiento",
                encabezadosSecundarios: new() { "Producto", "Stock", "Stock Mínimo" },
                filasSecundarias: filasSinMovimiento
            );

            MostrarExito(ruta);
        }

        private void ExportarGananciasPDF()
        {
            var ganancias = (dgGanancias.ItemsSource as List<ReporteGananciaItem>) ?? new();

            var filas = ganancias
                .Select(x => new List<string>
                {
                    x.Producto,
                    x.Cantidad.ToString(),
                    x.Ingreso.ToString("C"),
                    x.Costo.ToString("C"),
                    x.Ganancia.ToString("C"),
                    x.Margen.ToString("N1") + "%"
                })
                .ToList();

            string ruta = RutaReporte("Ganancias", $"Reporte_Ganancias_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            ReportePdfGenerator.GenerarReporteGenerico(
                tituloReporte: "Reporte de Ganancias",
                periodo: "Personalizado",
                desde: fechaInicioGanancias,
                hasta: fechaFinGanancias,
                tarjetasResumen: new()
                {
                    ("Ingresos", txtIngresos.Text),
                    ("Costos", txtCostos.Text),
                    ("Ganancia Neta", txtGananciaNeta.Text)
                },
                encabezados: new() { "Producto", "Cantidad", "Ingreso", "Costo", "Ganancia", "% Margen" },
                filas: filas,
                rutaArchivo: ruta
            );

            MostrarExito(ruta);
        }

        private void ExportarInventarioPDF()
        {
            var movimientos = (dgMovimientosInventario.ItemsSource as List<MovimientoInventarioView>) ?? new();
            var stockBajo = (dgStockBajo.ItemsSource as List<ProductoStockItem>) ?? new();

            var filas = movimientos
                .Select(x => new List<string>
                {
                    x.ProductoNombre,
                    x.TipoMovimiento,
                    x.Cantidad.ToString(),
                    x.Fecha.ToString("dd/MM/yyyy"),
                    x.Motivo
                })
                .ToList();

            var filasStockBajo = stockBajo
                .Select(x => new List<string>
                {
                    x.Nombre,
                    x.Stock.ToString(),
                    x.StockMinimo.ToString()
                })
                .ToList();

            string ruta = RutaReporte("Inventario", $"Reporte_Inventario_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            ReportePdfGenerator.GenerarReporteGenerico(
                tituloReporte: "Movimientos de Inventario",
                periodo: "Personalizado",
                desde: fechaInicioInventario,
                hasta: fechaFinInventario,
                tarjetasResumen: new() { ("Total de movimientos", movimientos.Count.ToString()) },
                encabezados: new() { "Producto", "Tipo", "Cantidad", "Fecha", "Motivo" },
                filas: filas,
                rutaArchivo: ruta,
                tituloTablaSecundaria: "Alertas de Stock Bajo",
                encabezadosSecundarios: new() { "Producto", "Stock", "Stock Mínimo" },
                filasSecundarias: filasStockBajo
            );

            MostrarExito(ruta);
        }

        private void ExportarPedidosPDF()
        {
            var pedidos = (dgPedidos.ItemsSource as List<ReportePedidoItem>) ?? new();

            var filas = pedidos
                .Select(x => new List<string>
                {
            x.NumeroPedido,
            x.Cliente,
            x.Estado,
            x.Total.ToString("C"),
            x.Fecha.ToString("dd/MM/yyyy hh:mm tt")
                })
                .ToList();

            string ruta = RutaReporte("Pedidos", $"Reporte_Pedidos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            ReportePdfGenerator.GenerarReporteGenerico(
                tituloReporte: "Reporte de Pedidos",
                periodo: periodoPedidos,
                desde: fechaInicioPedidos,
                hasta: fechaFinPedidos,
                tarjetasResumen: new()
                {
            ("Pendientes", txtPedidosPendientes.Text),
            ("Preparando", txtPedidosPreparando.Text),
            ("Entregados", txtPedidosEntregados.Text),
            ("Cancelados", txtPedidosCancelados.Text)
                },
                encabezados: new() { "No. Pedido", "Cliente", "Estado", "Total", "Fecha" },
                filas: filas,
                rutaArchivo: ruta
            );

            MostrarExito(ruta);
        }

        // ✅ Exportar — Caja

        private void ExportarCajaPDF()
        {
            var cortes = (dgCortesCaja.ItemsSource as List<ReporteCajaItem>) ?? new();

            var filas = cortes
                .Select(x => new List<string>
                {
                    x.Usuario,
                    x.FechaApertura.ToString("dd/MM/yyyy HH:mm"),
                    x.FechaCierre?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    x.MontoInicial.ToString("C"),
                    x.MontoFinalEsperado.ToString("C"),
                    x.MontoFinalContado.ToString("C"),
                    x.Diferencia.ToString("C"),
                    x.Estado
                })
                .ToList();

            string ruta = RutaReporte("Caja", $"Reporte_Caja_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

            ReportePdfGenerator.GenerarReporteGenerico(
                tituloReporte: "Reporte de Caja",
                periodo: periodoCaja,
                desde: fechaInicioCaja,
                hasta: fechaFinCaja,
                tarjetasResumen: new()
                {
                    ("Total de cortes", txtCajaTotalCortes.Text),
                    ("Total esperado", txtCajaTotalEsperado.Text),
                    ("Total contado", txtCajaTotalContado.Text),
                    ("Diferencia total", txtCajaTotalDiferencia.Text)
                },
                encabezados: new() { "Usuario", "Apertura", "Cierre", "Monto Inicial", "Esperado", "Contado", "Diferencia", "Estado" },
                filas: filas,
                rutaArchivo: ruta
            );

            MostrarExito(ruta);
        }

        // =========================================
        // CERRAR VENTANA
        // =========================================

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}