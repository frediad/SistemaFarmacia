using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using FarmaciaPOS.Models;

namespace FarmaciaPOS.Helpers
{
    public static class VentasReporteHelper
    {
        // =========================================
        // ✅ HISTORIAL DE VENTAS (con filtro de fecha)
        // =========================================

        // tipoFiltro: "Dia", "Semana", "Mes", "Año", "Todo"
        public static List<VentaHistorialItem> ObtenerHistorial(string tipoFiltro)
        {
            var lista = new List<VentaHistorialItem>();
           
            (DateTime desde, DateTime hasta) = ObtenerRangoFechas(tipoFiltro);

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT v.Id, v.Folio, v.Fecha, v.ClienteId,
         ISNULL(c.Nombre, 'Público en general') AS Cliente,
         ISNULL(u.Nombre, 'N/D') AS Vendedor,
         v.Subtotal, v.Descuento, v.Total, v.MetodoPago, v.Estado, v.EsCredito
  FROM Ventas v
  LEFT JOIN Clientes c ON v.ClienteId = c.Id
  LEFT JOIN Usuarios u ON v.UsuarioId = u.Id
  WHERE v.Fecha >= @Desde
  ORDER BY v.Fecha DESC";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde);
            cmd.Parameters.AddWithValue("@Hasta", hasta);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new VentaHistorialItem
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Folio = reader["Folio"].ToString() ?? "",
                    Fecha = Convert.ToDateTime(reader["Fecha"]),
                    ClienteId = reader["ClienteId"] != DBNull.Value ? Convert.ToInt32(reader["ClienteId"]) : null,
                    Cliente = reader["Cliente"].ToString() ?? "Público en general",
                    Vendedor = reader["Vendedor"].ToString() ?? "",
                    Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                    Descuento = Convert.ToDecimal(reader["Descuento"]),
                    Total = Convert.ToDecimal(reader["Total"]),
                    MetodoPago = reader["MetodoPago"].ToString() ?? "",
                    Estado = reader["Estado"].ToString() ?? "",
                    EsCredito = Convert.ToBoolean(reader["EsCredito"])
                });
            }

            return lista;
        }

        // ========================================
        // Devuelve un rango de fechas (desde, hasta) según el tipo de filtro solicitado.
        // ========================================
        private static (DateTime desde, DateTime hasta) ObtenerRangoFechas(string tipoFiltro)
        {
            DateTime hoy = DateTime.Now.Date;
            DateTime mañana = hoy.AddDays(1);

            return tipoFiltro switch
            {
                "Dia" => (hoy, mañana),
                "Semana" => (hoy.AddDays(-((int)hoy.DayOfWeek == 0 ? 6 : (int)hoy.DayOfWeek - 1)), mañana),
                "Mes" => (new DateTime(hoy.Year, hoy.Month, 1), mañana),
                "Anio" => (new DateTime(hoy.Year, 1, 1), mañana),
                _ => (new DateTime(2000, 1, 1), mañana) // "Todo"
            };
        }

        // =========================================
        // ✅ DETALLE DE UNA VENTA (para reimprimir el ticket)
        // =========================================

        public static List<VentaItem> ObtenerDetalleVenta(int ventaId)
        {
            var lista = new List<VentaItem>();

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT dv.ProductoId, p.Nombre, dv.Cantidad, dv.PrecioUnitario, dv.Subtotal
              FROM DetalleVentas dv
              INNER JOIN Productos p ON dv.ProductoId = p.Id
              WHERE dv.VentaId = @VentaId";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@VentaId", ventaId);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new VentaItem
                {
                    ProductoId = Convert.ToInt32(reader["ProductoId"]),
                    Nombre = reader["Nombre"].ToString() ?? "",
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    Precio = Convert.ToDecimal(reader["PrecioUnitario"]),
                });
            }

            return lista;
        }

        // ========================================
        // ✅ Devuelve la cabecera de una venta específica
        // ========================================
        public static VentaHistorialItem? ObtenerCabeceraVenta(int ventaId)
        {
            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT v.Id, v.Folio, v.Fecha, v.ClienteId,
                     ISNULL(c.Nombre, 'Público en general') AS Cliente,
                     ISNULL(u.Nombre, 'N/D') AS Vendedor,
                     v.Subtotal, v.Descuento, v.Total, v.MetodoPago, v.Estado, v.EsCredito
              FROM Ventas v
              LEFT JOIN Clientes c ON v.ClienteId = c.Id
              LEFT JOIN Usuarios u ON v.UsuarioId = u.Id
              WHERE v.Id = @VentaId";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@VentaId", ventaId);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new VentaHistorialItem
            {
                Id = Convert.ToInt32(reader["Id"]),
                Folio = reader["Folio"].ToString() ?? "",
                Fecha = Convert.ToDateTime(reader["Fecha"]),
                ClienteId = reader["ClienteId"] != DBNull.Value ? Convert.ToInt32(reader["ClienteId"]) : null,
                Cliente = reader["Cliente"].ToString() ?? "Público en general",
                Vendedor = reader["Vendedor"].ToString() ?? "",
                Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                Descuento = Convert.ToDecimal(reader["Descuento"]),
                Total = Convert.ToDecimal(reader["Total"]),
                MetodoPago = reader["MetodoPago"].ToString() ?? "",
                Estado = reader["Estado"].ToString() ?? "",
                EsCredito = Convert.ToBoolean(reader["EsCredito"])
            };
        }

        // =========================================
        // ✅ ESTADÍSTICAS: VENTAS MENSUALES Y ANUALES
        // =========================================

        public static EstadisticaVentas ObtenerEstadisticaMensual()
        {
            DateTime hoy = DateTime.Now;
            DateTime inicioMesActual = new DateTime(hoy.Year, hoy.Month, 1);
            DateTime inicioMesAnterior = inicioMesActual.AddMonths(-1);

            decimal totalActual = SumarVentasEnRango(inicioMesActual, hoy.AddDays(1).Date);
            // Mismo corte de día que el mes actual, pero un mes atrás — replica el
            // comportamiento "AL 13 ago 2026 vs AL 13 jul 2026" que se ve en SICAR.
            DateTime corteMesAnteriorHasta = inicioMesAnterior.AddDays(hoy.Day - 1).AddDays(1);
            decimal totalAnterior = SumarVentasEnRango(inicioMesAnterior, corteMesAnteriorHasta);

            return new EstadisticaVentas
            {
                EtiquetaActual = $"AL {hoy:dd MMM yyyy}",
                MontoActual = totalActual,
                EtiquetaAnterior = $"AL {corteMesAnteriorHasta.AddDays(-1):dd MMM yyyy}",
                MontoAnterior = totalAnterior
            };
        }

        public static EstadisticaVentas ObtenerEstadisticaAnual()
        {
            DateTime hoy = DateTime.Now;
            DateTime inicioAnioActual = new DateTime(hoy.Year, 1, 1);
            DateTime inicioAnioAnterior = new DateTime(hoy.Year - 1, 1, 1);

            decimal totalActual = SumarVentasEnRango(inicioAnioActual, hoy.AddDays(1).Date);

            // Mismo corte de día/mes que hoy, pero un año atrás.
            DateTime corteAnioAnteriorHasta = new DateTime(hoy.Year - 1, hoy.Month, hoy.Day).AddDays(1);
            decimal totalAnterior = SumarVentasEnRango(inicioAnioAnterior, corteAnioAnteriorHasta);

            return new EstadisticaVentas
            {
                EtiquetaActual = $"AL {hoy:dd MMM yyyy}",
                MontoActual = totalActual,
                EtiquetaAnterior = $"AL {corteAnioAnteriorHasta.AddDays(-1):dd MMM yyyy}",
                MontoAnterior = totalAnterior
            };
        }

        private static decimal SumarVentasEnRango(DateTime desde, DateTime hastaExclusivo)
        {
            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string query =
            @"SELECT ISNULL(SUM(Total), 0)
              FROM Ventas
              WHERE Fecha >= @Desde AND Fecha < @Hasta
              AND Estado = 'Completada'";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Desde", desde);
            cmd.Parameters.AddWithValue("@Hasta", hastaExclusivo);

            object resultado = cmd.ExecuteScalar();
            return resultado != DBNull.Value ? Convert.ToDecimal(resultado) : 0m;
        }
    }
}