using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class TicketConfigWindow : Window
    {


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
                    new SqlConnection(DatabaseHelper.ConnectionString);

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

                    txtCorreo.Text =
                       reader["Correo"]
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
                    new SqlConnection(DatabaseHelper.ConnectionString);

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
                MensajeTicket,
                Correo
            )
            VALUES
            (
                @Nombre,
                @RFC,
                @Direccion,
                @Telefono,
                @Mensaje,
                @Correo

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
                    Correo = @Correo,
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

                cmd.Parameters.AddWithValue(
                    "@Correo",
                    txtCorreo.Text);

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

        // =========================================
        // ✅ ACTUALIZAR VISTA PREVIA EN TIEMPO REAL
        // =========================================

        private void Campo_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarPreview();
        }

        private void ActualizarPreview()
        {
            if (previewNegocio == null) return;

            previewNegocio.Text = string.IsNullOrWhiteSpace(txtNegocio.Text)
                ? "Nombre del Negocio"
                : txtNegocio.Text;

            previewRFC.Text = string.IsNullOrWhiteSpace(txtRFC.Text)
                ? "RFC: —"
                : $"RFC: {txtRFC.Text}";

            previewDireccion.Text = string.IsNullOrWhiteSpace(txtDireccion.Text)
                ? "Dirección"
                : txtDireccion.Text;

            previewTelefono.Text = string.IsNullOrWhiteSpace(txtTelefono.Text)
                ? "Tel: —"
                : $"Tel: {txtTelefono.Text}";

            previewCorreo.Text = string.IsNullOrWhiteSpace(txtCorreo.Text)
                ? "Correo"
                : txtCorreo.Text;

            previewMensaje.Text = string.IsNullOrWhiteSpace(txtMensaje.Text)
                ? "¡Gracias por su compra!"
                : txtMensaje.Text;
        }

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}