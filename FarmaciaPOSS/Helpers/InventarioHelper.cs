using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace FarmaciaPOS.Helpers
{
    public static class InventarioHelper
    {
        // Descuenta el stock y registra el movimiento de salida
        // para cada producto vendido en una venta ya cobrada.
        public static void DescontarStockPorVenta(
            IEnumerable<VentaItem> itemsVendidos,
            int usuarioId = 1)
        {
            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            foreach (var item in itemsVendidos)
            {
                // Registrar movimiento de salida
                string queryMovimiento =
                @"INSERT INTO MovimientoInventarios
                (ProductoId, TipoMovimiento, Cantidad, Motivo, UsuarioId, Fecha)
                VALUES
                (@ProductoId, 'Salida', @Cantidad, @Motivo, @UsuarioId, GETDATE())";

                using SqlCommand cmdMov = new SqlCommand(queryMovimiento, conn);
                cmdMov.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdMov.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                cmdMov.Parameters.AddWithValue("@Motivo", "Venta en POS");
                cmdMov.Parameters.AddWithValue("@UsuarioId", usuarioId);
                cmdMov.ExecuteNonQuery();

                // Descontar stock
                string queryStock =
                @"UPDATE Productos
                  SET Stock = Stock - @Cantidad
                  WHERE Id = @ProductoId";

                using SqlCommand cmdStock = new SqlCommand(queryStock, conn);
                cmdStock.Parameters.AddWithValue("@Cantidad", item.Cantidad);
                cmdStock.Parameters.AddWithValue("@ProductoId", item.ProductoId);
                cmdStock.ExecuteNonQuery();
            }
        }
    }
}