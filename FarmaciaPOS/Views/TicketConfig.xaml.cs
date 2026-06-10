using Microsoft.Data.SqlClient;
using System;
using System.Windows;

namespace FarmaciaPOS.Views
{
    public partial class TicketConfigWindow : Window
    {
        string connectionString =
            @"Server=.\SQLEXPRESS;
              Database=FarmaciaDB;
              Trusted_Connection=True;
              TrustServerCertificate=True;";

        public TicketConfigWindow()
        {
            InitializeComponent();

            CargarConfiguracion();
        }

        // =====================================
        // CARGAR CONFIG
        // =====================================

        private void CargarConfiguracion()
        {
            try
            {
                using SqlConnection conn =
                    new SqlConnection(connectionString);

                conn.Open();

                string query =
                    "SELECT TOP 1 * FROM ConfiguracionTicket";

                SqlCommand cmd =
                    new SqlCommand(query, conn);

                SqlDataReader reader =
                    cmd.ExecuteReader();

                if (reader.Read())
                {
                    txtNegocio.Text =
                        reader["NombreNegocio"]
                        .ToString();

                    txtRFC.Text =
                        reader["RFC"]
                        .ToString();

                    txtDireccion.Text =
                        reader["Direccion"]
                        .ToString();

                    txtTelefono.Text =
                        reader["Telefono"]
                        .ToString();

                    txtMensaje.Text =
                        reader["MensajeTicket"]
                        .ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "ERROR");
            }
        }

        // =====================================
        // GUARDAR CONFIG
        // =====================================

        private void BtnGuardar_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                using SqlConnection conn =
                    new SqlConnection(connectionString);

                conn.Open();

                string verificar =
                    "SELECT COUNT(*) FROM ConfiguracionTicket";

                SqlCommand verificarCmd =
                    new SqlCommand(verificar, conn);

                int existe =
                    Convert.ToInt32(
                        verificarCmd.ExecuteScalar());

                string query = "";

                if (existe == 0)
                {
                    // INSERTAR

                    query =
                    @"INSERT INTO ConfiguracionTicket
                    (
                        NombreNegocio,
                        RFC,
                        Direccion,
                        Telefono,
                        MensajeTicket
                    )
                    VALUES
                    (
                        @Nombre,
                        @RFC,
                        @Direccion,
                        @Telefono,
                        @Mensaje
                    )";
                }
                else
                {
                    // ACTUALIZAR

                    query =
                    @"UPDATE ConfiguracionTicket
                      SET
                        NombreNegocio = @Nombre,
                        RFC = @RFC,
                        Direccion = @Direccion,
                        Telefono = @Telefono,
                        MensajeTicket = @Mensaje";
                }

                SqlCommand cmd =
                    new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue(
                    "@Nombre",
                    txtNegocio.Text);

                cmd.Parameters.AddWithValue(
                    "@RFC",
                    txtRFC.Text);

                cmd.Parameters.AddWithValue(
                    "@Direccion",
                    txtDireccion.Text);

                cmd.Parameters.AddWithValue(
                    "@Telefono",
                    txtTelefono.Text);

                cmd.Parameters.AddWithValue(
                    "@Mensaje",
                    txtMensaje.Text);

                cmd.ExecuteNonQuery();

                MessageBox.Show(
                    "Configuración guardada correctamente");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "ERROR");
            }
        }
    }
}