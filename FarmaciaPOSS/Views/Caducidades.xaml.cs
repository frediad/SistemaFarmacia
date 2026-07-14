using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
                        p.CodigoBarras,
                        p.Nombre,
                        img.RutaImagen AS ImagenURL,
                        p.Caducidad,
                        DATEDIFF(DAY, GETDATE(), p.Caducidad) AS DiasRestantes,
                        p.Stock,
                        CASE
                            WHEN DATEDIFF(DAY, GETDATE(), p.Caducidad) < 0
                                THEN 'CADUCADO'
                            WHEN DATEDIFF(DAY, GETDATE(), p.Caducidad) <= 30
                                THEN 'PRÓXIMO A CADUCAR'
                            ELSE 'NO CADUCADO'
                        END AS Estado
                    FROM Productos p
                    OUTER APPLY (
                        SELECT TOP 1 ip.RutaImagen
                        FROM ImagenesProducto ip
                        WHERE ip.ProductoId = p.Id
                        ORDER BY ip.Orden ASC
                    ) img
                    ORDER BY p.Caducidad ASC";

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

        // =========================================
        // VISOR DE IMAGEN AMPLIADA
        // =========================================

        private void ImagenProducto_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement elemento)
                return;

            if (elemento.DataContext is not DataRowView fila)
                return;

            if (fila["ImagenURL"] == DBNull.Value)
                return;

            string ruta = fila["ImagenURL"].ToString();

            if (string.IsNullOrWhiteSpace(ruta))
                return;

            try
            {
                imgAmpliada.Source = new BitmapImage(new Uri(ruta));
                overlayImagen.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo cargar la imagen:\n" + ex.Message);
            }
        }

        private void overlayImagen_Click(object sender, MouseButtonEventArgs e)
        {
            overlayImagen.Visibility = Visibility.Collapsed;
        }
    }
}