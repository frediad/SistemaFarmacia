
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;

namespace FarmaciaPOS.Views
{
    public partial class InformacionNegocio : Window
    {
        public InformacionNegocio()
        {
            InitializeComponent();
            CargarDatosNegocio();
        }

        private void CargarDatosNegocio()
        {
            try
            {
                using SqlConnection cn = new SqlConnection(DatabaseHelper.ConnectionString);
                cn.Open();

                // Se asume una sola fila de configuración (TOP 1).
                string sql = "SELECT TOP 1 * FROM ConfiguracionTicket";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                using SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    txtNombre.Text = ValorOGuion(ObtenerValorSeguro(dr, "NombreNegocio"));
                    txtRFC.Text = ValorOGuion(ObtenerValorSeguro(dr, "RFC"));
                    txtDireccion.Text = ValorOGuion(ObtenerValorSeguro(dr, "Direccion"));
                    txtTelefono.Text = ValorOGuion(ObtenerValorSeguro(dr, "Telefono"));
                    txtCorreo.Text = ValorOGuion(ObtenerValorSeguro(dr, "Correo"));
                }
                else
                {
                    txtSinDatos.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar información del negocio:\n" + ex.Message);
                txtSinDatos.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Devuelve "—" si el valor es nulo o una cadena vacía/solo espacios.
        /// </summary>
        private string ValorOGuion(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "—" : valor;
        }

        /// <summary>
        /// Lee una columna por nombre solo si existe en el resultado, evitando
        /// que truene si el nombre exacto de la columna difiere un poco.
        /// </summary>
        private string ObtenerValorSeguro(SqlDataReader dr, string columna)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (string.Equals(dr.GetName(i), columna, StringComparison.OrdinalIgnoreCase))
                {
                    return dr.IsDBNull(i) ? null : dr.GetValue(i).ToString();
                }
            }
            return null;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir el enlace:\n" + ex.Message);
            }

            e.Handled = true;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}