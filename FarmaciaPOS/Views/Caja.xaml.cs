using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class CajaWindow : Window
    {
       

        int cajaActualId = 0;

        public CajaWindow()
        {
            InitializeComponent();

            CargarCajaAbierta();
            CargarMovimientos();
        }

        // =====================================
        // CARGAR CAJA ABIERTA
        // =====================================

        private void CargarCajaAbierta()
        {
            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT TOP 1 *
              FROM Caja
              WHERE Estado = 'ABIERTA'
              ORDER BY Id DESC";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            if (reader.Read())
            {
                cajaActualId =
                    Convert.ToInt32(
                        reader["Id"]);

                txtMontoInicial.Text =
                    reader["MontoInicial"]
                    .ToString();

                txtMontoInicial.IsEnabled =
                    false;

                btnAbrirCaja.IsEnabled =
                    false;
            }
        }

        // =====================================
        // ABRIR CAJA
        // =====================================

        private void BtnAbrirCaja_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!decimal.TryParse(
                txtMontoInicial.Text,
                out decimal monto))
            {
                MessageBox.Show(
                    "Monto inválido");

                return;
            }

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"INSERT INTO Caja
            (
                UsuarioId,
                FechaApertura,
                MontoInicial,
                Estado
            )
            VALUES
            (
                @UsuarioId,
                GETDATE(),
                @MontoInicial,
                'ABIERTA'
            )";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@UsuarioId",
                Sesion.UsuarioId);

            cmd.Parameters.AddWithValue(
                "@MontoInicial",
                monto);

            cmd.ExecuteNonQuery();

            MessageBox.Show(
                "Caja abierta correctamente");

            CargarCajaAbierta();
        }

        // =====================================
        // REGISTRAR MOVIMIENTO
        // =====================================

        private void BtnMovimiento_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (cajaActualId == 0)
            {
                MessageBox.Show(
                    "No hay caja abierta");

                return;
            }

            if (!decimal.TryParse(
                txtMontoMovimiento.Text,
                out decimal monto))
            {
                MessageBox.Show(
                    "Monto inválido");

                return;
            }

            ComboBoxItem item =
                cbTipoMovimiento.SelectedItem
                as ComboBoxItem;

            if (item == null)
            {
                MessageBox.Show(
                    "Selecciona tipo");

                return;
            }

            string tipo =
                item.Content.ToString();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"INSERT INTO MovimientosCaja
            (
                CajaId,
                TipoMovimiento,
                Monto,
                Motivo
            )
            VALUES
            (
                @CajaId,
                @TipoMovimiento,
                @Monto,
                @Motivo
            )";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@CajaId",
                cajaActualId);

            cmd.Parameters.AddWithValue(
                "@TipoMovimiento",
                tipo);

            cmd.Parameters.AddWithValue(
                "@Monto",
                monto);

            cmd.Parameters.AddWithValue(
                "@Motivo",
                txtMotivo.Text);

            cmd.ExecuteNonQuery();

            MessageBox.Show(
                "Movimiento registrado");

            txtMontoMovimiento.Clear();
            txtMotivo.Clear();

            CargarMovimientos();
        }

        // =====================================
        // CARGAR MOVIMIENTOS
        // =====================================

        private void CargarMovimientos()
        {
            List<dynamic> lista =
                new();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT
                TipoMovimiento,
                Monto,
                Motivo,
                Fecha
              FROM MovimientosCaja
              ORDER BY Fecha DESC";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new
                {
                    Tipo =
                        reader["TipoMovimiento"],

                    Monto =
                        Convert.ToDecimal(
                            reader["Monto"])
                        .ToString("C"),

                    Motivo =
                        reader["Motivo"],

                    Fecha =
                        Convert.ToDateTime(
                            reader["Fecha"])
                        .ToString("dd/MM/yyyy HH:mm")
                });
            }

            dgMovimientos.ItemsSource =
                lista;
        }

        // =====================================
        // CERRAR CAJA
        // =====================================

        private void BtnCerrarCaja_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (cajaActualId == 0)
            {
                MessageBox.Show(
                    "No hay caja abierta");

                return;
            }

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"UPDATE Caja
              SET
                FechaCierre = GETDATE(),
                Estado = 'CERRADA'
              WHERE Id = @Id";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue(
                "@Id",
                cajaActualId);

            cmd.ExecuteNonQuery();

            MessageBox.Show(
                "Caja cerrada correctamente");

            Close();
        }
    }
}