using FarmaciaPOS.Models;
using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class ProductosWindow : Window
    {
        int productoId = 0;
        List<Subcategoria> todasSubcategorias = new();

        public ProductosWindow()
        {
            InitializeComponent();

            CargarProductos();
            CargarCategorias();
            CargarTodasSubcategorias();
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

        List<Producto> listaCompletaProductos = new();

        private void CargarProductos()
        {
            List<Producto> lista =
                new List<Producto>();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT *
              FROM Productos
              WHERE Activo = 1
              ORDER BY Nombre";

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

                    CodigoBarras =
                        reader["CodigoBarras"]
                        .ToString(),

                    Nombre =
                        reader["Nombre"]
                        .ToString(),

                    Descripcion =
                        reader["Descripcion"]
                        .ToString(),

                    CategoriaId =
                        Convert.ToInt32(
                            reader["CategoriaId"]),

                    // ✅ NUEVO
                    SubcategoriaId =
                        reader["SubcategoriaId"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["SubcategoriaId"]),

                    PrecioCompra =
                        Convert.ToDecimal(
                            reader["PrecioCompra"]),

                    PrecioVenta =
                        Convert.ToDecimal(
                            reader["PrecioVenta"]),

                    Stock =
                        Convert.ToInt32(
                            reader["Stock"]),

                    StockMinimo =
                        Convert.ToInt32(
                            reader["StockMinimo"]),

                    ImagenURL =
                        reader["ImagenURL"]
                        .ToString(),

                    EsMedicamentoControlado =
                        Convert.ToBoolean(
                            reader["EsMedicamentoControlado"]),

                    Activo =
                        Convert.ToBoolean(
                            reader["Activo"])
                });

            }

            dgProductos.ItemsSource =
                lista;
        }

        // =========================================
        // CARGAR CATEGORIAS
        // =========================================

        private void CargarCategorias()
        {
            List<Categoria> lista =
                new List<Categoria>();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
                "SELECT * FROM Categorias";

            SqlCommand cmd =
                new SqlCommand(query, conn);

            SqlDataReader reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Categoria
                {
                    Id =
                        Convert.ToInt32(
                            reader["Id"]),

                    Nombre =
                        reader["Nombre"]
                        .ToString()
                });
            }

            cbCategorias.ItemsSource =
                lista;

            cbCategorias.DisplayMemberPath =
                "Nombre";

            cbCategorias.SelectedValuePath =
                "Id";
        }

        // =========================================
        // ✅ NUEVO — CARGAR TODAS LAS SUBCATEGORIAS
        // =========================================

        private void CargarTodasSubcategorias()
        {
            todasSubcategorias.Clear();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query = "SELECT * FROM Subcategorias";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                todasSubcategorias.Add(new Subcategoria
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString() ?? "",
                    CategoriaId = Convert.ToInt32(reader["CategoriaId"])
                });
            }
        }

        // =========================================
        // ✅ NUEVO — FILTRAR SUBCATEGORIAS POR CATEGORIA
        // =========================================

        private void CbCategorias_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (cbCategorias.SelectedValue == null)
            {
                cbSubcategorias.ItemsSource = null;
                return;
            }

            int categoriaId = Convert.ToInt32(cbCategorias.SelectedValue);

            var filtradas = todasSubcategorias
                .Where(s => s.CategoriaId == categoriaId)
                .ToList();

            cbSubcategorias.ItemsSource = filtradas;
        }

        // =========================================
        // GUARDAR PRODUCTO
        // =========================================

        private void BtnGuardar_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query = "";

                // INSERTAR

                if (productoId == 0)
                {
                    query =
                    @"INSERT INTO Productos
                    (
                        CodigoBarras,
                        Nombre,
                        Descripcion,
                        CategoriaId,
                        SubcategoriaId,
                        PrecioCompra,
                        PrecioVenta,
                        Stock,
                        StockMinimo,
                        ImagenURL,
                        EsMedicamentoControlado,
                        Activo,
                        FechaCreacion
                    )
                    VALUES
                    (
                        @CodigoBarras,
                        @Nombre,
                        @Descripcion,
                        @CategoriaId,
                        @SubcategoriaId,
                        @PrecioCompra,
                        @PrecioVenta,
                        @Stock,
                        @StockMinimo
                        @ImagenURL,
                        @EsMedicamentoControlado,
                        @Activo,
                        GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();";
                }

                // ACTUALIZAR

                else
                {
                    query =
                    @"UPDATE Productos SET

                        CodigoBarras =
                            @CodigoBarras,

                        Nombre =
                            @Nombre,

                        Descripcion =
                            @Descripcion,

                        CategoriaId =
                            @CategoriaId,

                        SubcategoriaId =
                            @SubcategoriaId,

                        PrecioCompra =
                            @PrecioCompra,

                        PrecioVenta =
                            @PrecioVenta,

                        Stock =
                            @Stock,

                        StockMinimo =
                            @StockMinimo,

                        ImagenURL =
                            @ImagenURL,

                        EsMedicamentoControlado =
                            @EsMedicamentoControlado,

                        Activo =
                            @Activo

                    WHERE Id = @Id";
                }

                SqlCommand cmd =
                    new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue(
                    "@CodigoBarras",
                    txtCodigo.Text);

                cmd.Parameters.AddWithValue(
                    "@Nombre",
                    txtNombre.Text);

                cmd.Parameters.AddWithValue(
                    "@Descripcion",
                    txtDescripcion.Text);

                cmd.Parameters.AddWithValue(
                    "@CategoriaId",
                    cbCategorias.SelectedValue);

                cmd.Parameters.AddWithValue(
                    "@SubcategoriaId",
                    cbSubcategorias.SelectedValue ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue(
                    "@PrecioCompra",
                    decimal.Parse(
                        txtPrecioCompra.Text));

                cmd.Parameters.AddWithValue(
                    "@PrecioVenta",
                    decimal.Parse(
                        txtPrecioVenta.Text));

                cmd.Parameters.AddWithValue(
                    "@Stock",
                    int.Parse(
                        txtStock.Text));

                cmd.Parameters.AddWithValue(
                    "@StockMinimo",
                    int.Parse(
                        txtStockMinimo.Text));

                cmd.Parameters.AddWithValue(
                    "@ImagenURL",
                    txtImagenURL.Text);

                cmd.Parameters.AddWithValue(
                    "@EsMedicamentoControlado",
                    chkControlado.IsChecked
                    ?? false);

                cmd.Parameters.AddWithValue(
                    "@Activo",
                    chkActivo.IsChecked
                    ?? true);

                if (productoId != 0)
                {
                    cmd.Parameters.AddWithValue(
                        "@Id",
                        productoId);

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var resultado = cmd.ExecuteScalar();
                    productoId = Convert.ToInt32(resultado);
                }

                MessageBox.Show(
                    "Producto guardado correctamente");

                CargarLotes();
                CargarProductos();
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
        // SELECCIONAR PRODUCTO
        // =========================================

        private void dgProductos_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (dgProductos.SelectedItem
                is Producto producto)
            {
                productoId =
                    producto.Id;

                txtCodigo.Text =
                    producto.CodigoBarras;

                txtNombre.Text =
                    producto.Nombre;

                txtDescripcion.Text =
                    producto.Descripcion;

                cbCategorias.SelectedValue =
                    producto.CategoriaId;

                // ✅ Forzar carga de subcategorías antes de seleccionar
                CbCategorias_SelectionChanged(this, null);

                cbSubcategorias.SelectedValue =
                    producto.SubcategoriaId;

                txtPrecioCompra.Text =
                    producto.PrecioCompra
                    .ToString();

                txtPrecioVenta.Text =
                    producto.PrecioVenta
                    .ToString();

                txtStock.Text =
                    producto.Stock
                    .ToString();

                txtStockMinimo.Text =
                    producto.StockMinimo
                    .ToString();

                txtImagenURL.Text =
                    producto.ImagenURL;

                CargarImagenPreview(producto.ImagenURL);

                chkControlado.IsChecked =
                    producto.EsMedicamentoControlado;

                chkActivo.IsChecked =
                    producto.Activo;

                // ✅ Cargar lotes del producto seleccionado
                CargarLotes();
            }
        }

        // =========================================
        // NUEVO
        // =========================================

        private void BtnNuevo_Click(
            object sender,
            RoutedEventArgs e)
        {
            Limpiar();
        }

        // =========================================
        // LIMPIAR
        // =========================================

        private void Limpiar()
        {
            productoId = 0;

            txtCodigo.Clear();
            txtNombre.Clear();
            txtDescripcion.Clear();

            txtPrecioCompra.Clear();
            txtPrecioVenta.Clear();

            txtStock.Clear();
            txtStockMinimo.Clear();

            txtImagenURL.Clear();

            imgProductoPreview.Source = null;

            cbCategorias.SelectedIndex = -1;
            cbSubcategorias.ItemsSource = null;

            chkControlado.IsChecked =
                false;

            chkActivo.IsChecked =
                true;

            dgLotes.ItemsSource = null;
            txtNumeroLote.Clear();
            txtCantidadLote.Clear();
            dpCaducidadLote.SelectedDate = null;
        }

        // =========================================
        // ACTUALIZAR
        // =========================================

        private void BtnActualizar_Click(
            object sender,
            RoutedEventArgs e)
        {
            BtnGuardar_Click(sender, e);
        }

        // =========================================
        // ELIMINAR
        // =========================================

        private void BtnEliminar_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (productoId == 0)
                {
                    MessageBox.Show(
                        "Selecciona un producto");

                    return;
                }

                using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query =
                @"UPDATE Productos
          SET Activo = 0
          WHERE Id = @Id";

                SqlCommand cmd =
                    new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue(
                    "@Id",
                    productoId);

                cmd.ExecuteNonQuery();

                MessageBox.Show(
                    "Producto eliminado");

                Limpiar();

                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message);
            }
        }

        // =========================================
        // CARRUSEL DE IMAGEN
        // =========================================

        private void BtnCargarImagen_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                txtImagenURL.Text = dialog.FileName;
                CargarImagenPreview(dialog.FileName);
            }
        }

        private void TxtImagenURL_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            CargarImagenPreview(txtImagenURL.Text);
        }

        private void CargarImagenPreview(string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                imgProductoPreview.Source = null;
                return;
            }

            try
            {
                imgProductoPreview.Source =
                    new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(ruta));
            }
            catch
            {
                imgProductoPreview.Source = null;
            }
        }

        private void BtnGenerarClave_Click(
            object sender,
            RoutedEventArgs e)
        {
            txtCodigo.Text =
                DateTime.Now.Ticks
                .ToString()
                .Substring(0, 12);
        }

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Limpiar();
        }

        // =========================================
        // ✅ NUEVO — TEXTBOX PLACEHOLDER BUSCAR
        // =========================================

        private void TxtBuscar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar producto...")
            {
                txtBuscar.Text = "";
                txtBuscar.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void TxtBuscar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                txtBuscar.Text = "Buscar producto...";
                txtBuscar.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        // =========================================
        // ✅ NUEVO — LOTES
        // =========================================

        private void CargarLotes()
        {
            if (productoId == 0)
            {
                dgLotes.ItemsSource = null;
                return;
            }

            List<LoteProducto> lista = new();

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT * FROM LotesProductos
              WHERE ProductoId = @ProductoId
              ORDER BY FechaCaducidad";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProductoId", productoId);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new LoteProducto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    ProductoId = Convert.ToInt32(reader["ProductoId"]),
                    NumeroLote = reader["NumeroLote"].ToString() ?? "",
                    Cantidad = Convert.ToInt32(reader["Cantidad"]),
                    FechaCaducidad = Convert.ToDateTime(reader["FechaCaducidad"])
                });
            }

            dgLotes.ItemsSource = lista;
        }

        private void BtnAgregarLote_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (productoId == 0)
                {
                    MessageBox.Show(
                        "Primero guarda o selecciona un producto");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtNumeroLote.Text))
                {
                    MessageBox.Show("Escribe el número de lote");
                    return;
                }

                if (!int.TryParse(txtCantidadLote.Text, out int cantidad))
                {
                    MessageBox.Show("Cantidad inválida");
                    return;
                }

                if (dpCaducidadLote.SelectedDate == null)
                {
                    MessageBox.Show("Selecciona la fecha de caducidad");
                    return;
                }

                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query =
                @"INSERT INTO LotesProductos
                  (ProductoId, NumeroLote, FechaCaducidad, Cantidad, FechaRegistro)
                  VALUES
                  (@ProductoId, @NumeroLote, @FechaCaducidad, @Cantidad, GETDATE())";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@ProductoId", productoId);
                cmd.Parameters.AddWithValue("@NumeroLote", txtNumeroLote.Text);
                cmd.Parameters.AddWithValue("@FechaCaducidad", dpCaducidadLote.SelectedDate);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Lote agregado");

                txtNumeroLote.Clear();
                txtCantidadLote.Clear();
                dpCaducidadLote.SelectedDate = null;

                CargarLotes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}