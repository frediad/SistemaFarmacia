using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class InventarioWindow : Window
    {
        string connectionString =
            @"Server=.\SQLEXPRESS;
              Database=FarmaciaDB;
              Trusted_Connection=True;
              TrustServerCertificate=True;";

        public InventarioWindow()
        {
            InitializeComponent();

            cbTipo.SelectedIndex = 0;

            CargarProductos();

            CargarMovimientos();
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        private void CargarProductos()
        {
            List<Producto> lista =
                new();

            using SqlConnection conn =
                new SqlConnection(connectionString);

            conn.Open();

            string query =
                "SELECT * FROM Productos WHERE Activo = 1";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Producto
                {
                    Id =
                        Convert.ToInt32(
                            reader["Id"]),

                    Nombre =
                        reader["Nombre"]
                        .ToString()
                });
            }

            cbProductos.ItemsSource =
                lista;

            cbProductos.DisplayMemberPath =
                "Nombre";

            cbProductos.SelectedValuePath =
                "Id";
        }

        // =========================================
        // GUARDAR MOVIMIENTO
        // =========================================

        private void BtnGuardar_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                using SqlConnection conn =
                    new SqlConnection(connectionString);

                conn.Open();

                string tipo =
                    (cbTipo.SelectedItem
                    as ComboBoxItem)?
                    .Content.ToString();

                int cantidad =
                    int.Parse(
                        txtCantidad.Text);

                int productoId =
                    (int)cbProductos.SelectedValue;

                // =====================================
                // INSERTAR MOVIMIENTO
                // =====================================

                string query =
                @"INSERT INTO MovimientoInventarios
                (
                    ProductoId,
                    TipoMovimiento,
                    Cantidad,
                    Motivo,
                    UsuarioId,
                    Fecha
                )
                VALUES
                (
                    @ProductoId,
                    @TipoMovimiento,
                    @Cantidad,
                    @Motivo,
                    @UsuarioId,
                    GETDATE()
                )";

                SqlCommand cmd =
                    new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue(
                    "@ProductoId",
                    productoId);

                cmd.Parameters.AddWithValue(
                    "@TipoMovimiento",
                    tipo);

                cmd.Parameters.AddWithValue(
                    "@Cantidad",
                    cantidad);

                cmd.Parameters.AddWithValue(
                    "@Motivo",
                    txtMotivo.Text);

                cmd.Parameters.AddWithValue(
                    "@UsuarioId",
                    1);

                cmd.ExecuteNonQuery();

                // =====================================
                // ACTUALIZAR STOCK
                // =====================================

                string updateQuery = "";

                if (tipo == "Entrada")
                {
                    updateQuery =
                    @"UPDATE Productos
                      SET Stock = Stock + @Cantidad
                      WHERE Id = @ProductoId";
                }
                else
                {
                    updateQuery =
                    @"UPDATE Productos
                      SET Stock = Stock - @Cantidad
                      WHERE Id = @ProductoId";
                }

                SqlCommand updateCmd =
                    new SqlCommand(
                        updateQuery,
                        conn);

                updateCmd.Parameters.AddWithValue(
                    "@Cantidad",
                    cantidad);

                updateCmd.Parameters.AddWithValue(
                    "@ProductoId",
                    productoId);

                updateCmd.ExecuteNonQuery();

                MessageBox.Show(
                    "Movimiento guardado correctamente");

                txtCantidad.Clear();

                txtMotivo.Clear();

                CargarMovimientos();
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
            List<MovimientoInventarioView>
                lista = new();

            using SqlConnection conn =
                new SqlConnection(connectionString);

            conn.Open();

            string query =
            @"SELECT

                p.Nombre AS ProductoNombre,

                m.TipoMovimiento,

                m.Cantidad,

                m.Fecha,

                m.Motivo

            FROM MovimientoInventarios m

            INNER JOIN Productos p
                ON m.ProductoId = p.Id

            ORDER BY
                m.Fecha DESC";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(
                    new MovimientoInventarioView
                    {
                        ProductoNombre =
                            reader["ProductoNombre"]
                            .ToString(),

                        TipoMovimiento =
                            reader["TipoMovimiento"]
                            .ToString(),

                        Cantidad =
                            Convert.ToInt32(
                                reader["Cantidad"]),

                        Fecha =
                            Convert.ToDateTime(
                                reader["Fecha"]),

                        Motivo =
                            reader["Motivo"]
                            .ToString()
                    });
            }

            dgMovimientos.ItemsSource =
                lista;
        }
    }
}