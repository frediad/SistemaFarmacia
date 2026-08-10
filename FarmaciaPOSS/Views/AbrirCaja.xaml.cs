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
                    int usuarioIdApertura = Convert.ToInt32(reader["UsuarioId"]);
                    string usuarioApertura = reader["NombreUsuarioApertura"]?.ToString() ?? "Desconocido";

                    reader.Close();

                    if (usuarioIdApertura == Sesion.UsuarioId)
                    {
                        // ✅ La caja abierta es del mismo usuario que acaba de iniciar sesión
                        txtInfoCajaAbierta.Text =
                            $"Abierta el {apertura:dd/MM/yyyy HH:mm}\n" +
                            $"Monto inicial: {montoInicial:C}";

                        pnlCajaYaAbierta.Visibility = Visibility.Visible;
                        pnlAbrirNueva.Visibility = Visibility.Collapsed;
                        pnlCajaDeOtroUsuario.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        // ✅ La caja abierta pertenece a OTRO usuario
                        txtInfoCajaOtroUsuario.Text =
                            $"\"{usuarioApertura}\" abrió una caja el {apertura:dd/MM/yyyy HH:mm} " +
                            $"con un monto inicial de {montoInicial:C}.\n\n" +
                            "Puedes abrir tu propia caja para trabajar de forma independiente, " +
                            "o continuar usando esa misma si así lo prefieres.";

                        pnlCajaDeOtroUsuario.Visibility = Visibility.Visible;
                        pnlCajaYaAbierta.Visibility = Visibility.Collapsed;
                        pnlAbrirNueva.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    pnlAbrirNueva.Visibility = Visibility.Visible;
                    pnlCajaYaAbierta.Visibility = Visibility.Collapsed;
                    pnlCajaDeOtroUsuario.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(
                    "No se pudo verificar el estado de la caja: " + ex.Message,
                    "Error");

                pnlAbrirNueva.Visibility = Visibility.Visible;
                pnlCajaYaAbierta.Visibility = Visibility.Collapsed;
                pnlCajaDeOtroUsuario.Visibility = Visibility.Collapsed;
            }
        }

        // ✅ Desde el panel de "caja de otro usuario", pasa al formulario para abrir la propia
        private void BtnAbrirLaMiaPropia_Click(object sender, RoutedEventArgs e)
        {
            pnlCajaDeOtroUsuario.Visibility = Visibility.Collapsed;
            pnlCajaYaAbierta.Visibility = Visibility.Collapsed;
            pnlAbrirNueva.Visibility = Visibility.Visible;

            txtMontoInicial.Focus();
        }

        private void BtnAbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtMontoInicial.Text, out decimal monto) || monto < 0)
            {
                MensajeHelper.Error("Ingresa un monto inicial válido");
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
                (@UsuarioId, @FechaApertura, @MontoInicial, 'ABIERTA')";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UsuarioId", Sesion.UsuarioId);
                cmd.Parameters.AddWithValue("@FechaApertura", DateTime.Now);
                cmd.Parameters.AddWithValue("@MontoInicial", monto);
                cmd.ExecuteNonQuery();

                accionCompletada = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo abrir la caja: " + ex.Message, "Error");
            }
        }

        private void BtnContinuar_Click(object sender, RoutedEventArgs e)
        {
            accionCompletada = true;
            DialogResult = true;
        }

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