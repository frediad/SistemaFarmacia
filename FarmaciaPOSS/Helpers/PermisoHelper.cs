using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FarmaciaPOS.Helpers
{
    public static class PermisosHelper
    {
        // =========================================
        // VERIFICAR ACCESO A UN MÓDULO
        // =========================================

        public static bool TieneAcceso(string nombreModulo)
        {
            if (Sesion.RolId == 1)
                return true;

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT TieneAcceso
              FROM PermisosUsuario
              WHERE UsuarioId = @UsuarioId
              AND NombreModulo = @NombreModulo";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
            cmd.Parameters.AddWithValue("@NombreModulo", nombreModulo);

            var resultado = cmd.ExecuteScalar();

            if (resultado == null)
                return false;

            return Convert.ToBoolean(resultado);
        }

        // =========================================
        // ✅ NUEVO — CARGAR PERMISOS Y OCULTAR BOTONES
        // =========================================

        public static void AplicarPermisosEnMenu(
            System.Windows.Controls.Button btnVentas,
            System.Windows.Controls.Button btnPedidos,
            System.Windows.Controls.Button btnProductos,
            System.Windows.Controls.Button btnInventario,
            System.Windows.Controls.Button btnReportes,
            System.Windows.Controls.Button btnConfiguracion,
            System.Windows.Controls.Button btnCaja)
        {
            // Administrador ve todo
            if (Sesion.RolId == 1)
                return;

            // Ocultar según permisos reales
            if (!TieneAcceso("Ventas"))
                btnVentas.Visibility = Visibility.Collapsed;

            if (!TieneAcceso("Pedidos"))
                btnPedidos.Visibility = Visibility.Collapsed;

            if (!TieneAcceso("Productos"))
                btnProductos.Visibility = Visibility.Collapsed;

            if (!TieneAcceso("Inventario"))
                btnInventario.Visibility = Visibility.Collapsed;

            if (!TieneAcceso("Reportes"))
                btnReportes.Visibility = Visibility.Collapsed;

            if (!TieneAcceso("Configuración"))
                btnConfiguracion.Visibility = Visibility.Collapsed;

            if (!TieneAcceso("Caja"))
                btnCaja.Visibility = Visibility.Collapsed;
        }

        // =========================================
        // MENSAJE ACCESO DENEGADO (solo para casos especiales)
        // =========================================

        public static void MostrarAccesoDenegado()
        {
            MessageBox.Show(
                "No tienes acceso a este módulo.",
                "Acceso Restringido",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}