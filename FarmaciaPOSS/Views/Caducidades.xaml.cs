using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
                             ISNULL(l.NumeroLote, '—') AS NumeroLote,
                             l.FechaCaducidad AS Caducidad,
                             CASE
                                WHEN l.FechaCaducidad IS NULL THEN NULL
                                ELSE DATEDIFF(DAY, GETDATE(), l.FechaCaducidad)
                             END AS DiasRestantes,
                             l.Cantidad AS Stock,
                             CASE
                                WHEN l.FechaCaducidad IS NULL
                                    THEN 'SIN LOTE REGISTRADO'
                                WHEN DATEDIFF(DAY, GETDATE(), l.FechaCaducidad) < 0
                                    THEN 'CADUCADO'
                                WHEN DATEDIFF(DAY, GETDATE(), l.FechaCaducidad) <= 30
                                    THEN 'PRÓXIMO A CADUCAR'
                                ELSE 'NO CADUCADO'
                             END AS Estado,
                             (SELECT TOP 1 img.ImagenData
                             FROM ImagenesProducto img
                             WHERE img.ProductoId = p.Id
                             ORDER BY img.Orden) AS ImagenData
                             FROM Productos p
                             LEFT JOIN LotesProductos l ON l.ProductoId = p.Id
                             WHERE p.Activo = 1
                             ORDER BY p.Nombre ASC, l.FechaCaducidad ASC";

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
            try
            {
                if (sender is not FrameworkElement elemento)
                    return;

                if (elemento.DataContext is not DataRowView fila)
                    return;

                if (fila["ImagenData"] == DBNull.Value)
                    return;

                byte[] bytes = (byte[])fila["ImagenData"];

                var bitmap = BytesABitmap(bytes);

                if (bitmap == null)
                    return;

                imgAmpliada.Source = bitmap;
                overlayImagen.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo mostrar la imagen: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private BitmapImage BytesABitmap(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            try
            {
                using var stream = new System.IO.MemoryStream(bytes);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void overlayImagen_Click(object sender, MouseButtonEventArgs e)
        {
            overlayImagen.Visibility = Visibility.Collapsed;
        }
    }
}