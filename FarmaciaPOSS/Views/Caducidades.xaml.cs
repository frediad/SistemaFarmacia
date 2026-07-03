using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using FarmaciaPOS.Helpers;

namespace FarmaciaPOS.Views
{
    public partial class Caducidades : Window
    {
        private DataView vista;

        public Caducidades()
        {
            InitializeComponent();
            CargarCaducidades();
        }

        private void CargarCaducidades()
        {
            DataTable dt = ObtenerCaducidades();

            vista = dt.DefaultView;
            dgCaducidades.ItemsSource = vista;
        }

        private DataTable ObtenerCaducidades()
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection con =
                    new SqlConnection(DatabaseHelper.ConnectionString))
                {
                    con.Open();

                    string query = @"
                    SELECT
                        CodigoBarras,
                        Nombre,
                        Caducidad,
                        DATEDIFF(DAY, GETDATE(), Caducidad) AS DiasRestantes,
                        Stock,
                        CASE
                            WHEN DATEDIFF(DAY, GETDATE(), Caducidad) <= 7
                                THEN 'URGENTE'
                            ELSE 'PRÓXIMO'
                        END AS Estado
                    FROM Productos
                    ORDER BY Caducidad ASC";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al cargar productos: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            return dt;
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (vista == null)
                return;

            string filtro = txtBuscar.Text.Trim().Replace("'", "''");

            if (string.IsNullOrWhiteSpace(filtro))
            {
                vista.RowFilter = "";
            }
            else
            {
                vista.RowFilter =
                    $"Nombre LIKE '%{filtro}%' OR CodigoBarras LIKE '%{filtro}%'";
            }
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarCaducidades();

            MessageBox.Show(
                "Datos actualizados correctamente.",
                "Actualización",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}