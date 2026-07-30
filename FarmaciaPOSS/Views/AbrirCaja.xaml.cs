using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class AbrirCajaWindow : Window
    {
        private bool accionCompletada = false;

        public AbrirCajaWindow()
        {
            InitializeComponent();
            CargarEstadoCaja();
        }

        private void CargarEstadoCaja()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query =
                @"SELECT TOP 1 c.*, u.Nombre AS NombreUsuarioApertura
                  FROM Caja c
                  LEFT JOIN Usuarios u ON c.UsuarioId = u.Id
                  WHERE c.Estado = 'ABIERTA'
                  ORDER BY c.Id DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    DateTime apertura = Convert.ToDateTime(reader["FechaApertura"]);
                    decimal montoInicial = Convert.ToDecimal(reader["MontoInicial"]);
                    string usuarioApertura = reader["NombreUsuarioApertura"]?.ToString() ?? "Desconocido";

                    txtInfoCajaAbierta.Text =
                        $"Abierta por {usuarioApertura} el {apertura:dd/MM/yyyy HH:mm}\n" +
                        $"Monto inicial: {montoInicial:C}";

                    pnlCajaYaAbierta.Visibility = Visibility.Visible;
                    pnlAbrirNueva.Visibility = Visibility.Collapsed;
                }
                else
                {
                    pnlAbrirNueva.Visibility = Visibility.Visible;
                    pnlCajaYaAbierta.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo verificar el estado de la caja: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Ante un error de conexión, permitimos abrir caja de todos modos
                pnlAbrirNueva.Visibility = Visibility.Visible;
                pnlCajaYaAbierta.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtMontoInicial.Text, out decimal monto) || monto < 0)
            {
                MessageBox.Show("Ingresa un monto inicial válido");
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query =
                @"INSERT INTO Caja
                (UsuarioId, FechaApertura, MontoInicial, Estado)
                VALUES
                (@UsuarioId, GETDATE(), @MontoInicial, 'ABIERTA')";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                cmd.Parameters.AddWithValue("@MontoInicial", monto);
                cmd.ExecuteNonQuery();

                accionCompletada = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo abrir la caja: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnContinuar_Click(object sender, RoutedEventArgs e)
        {
            accionCompletada = true;
            DialogResult = true;
        }

        // ✅ Bloquea Alt+F4 / cierre por otro medio: es obligatorio completar una acción
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!accionCompletada)
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }
    }
}