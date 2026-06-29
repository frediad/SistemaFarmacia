using FarmaciaPOS.Models;
using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class ProductosWindow : Window
    {
        
        int productoId = 0;

        public ProductosWindow()
        {
            InitializeComponent();

            CargarProductos();
            CargarCategorias();
        }

        // =========================================
        // CARGAR PRODUCTOS
        // =========================================

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

                    Caducidad =
                        reader["Caducidad"]
                        == DBNull.Value
                        ? null
                        : Convert.ToDateTime(
                            reader["Caducidad"]),

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
                        PrecioCompra,
                        PrecioVenta,
                        Stock,
                        StockMinimo,
                        Caducidad,
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
                        @PrecioCompra,
                        @PrecioVenta,
                        @Stock,
                        @StockMinimo,
                        @Caducidad,
                        @ImagenURL,
                        @EsMedicamentoControlado,
                        @Activo,
                        GETDATE()
                    )";
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

                        PrecioCompra =
                            @PrecioCompra,

                        PrecioVenta =
                            @PrecioVenta,

                        Stock =
                            @Stock,

                        StockMinimo =
                            @StockMinimo,

                        Caducidad =
                            @Caducidad,

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
                    "@Caducidad",
                    dpCaducidad.SelectedDate
                    ?? (object)DBNull.Value);

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
                }

                cmd.ExecuteNonQuery();

                MessageBox.Show(
                    "Producto guardado correctamente");

                Limpiar();

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

                dpCaducidad.SelectedDate =
                    producto.Caducidad;

                txtImagenURL.Text =
                    producto.ImagenURL;

                chkControlado.IsChecked =
                    producto.EsMedicamentoControlado;

                chkActivo.IsChecked =
                    producto.Activo;
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

            cbCategorias.SelectedIndex = -1;

            dpCaducidad.SelectedDate =
                null;

            chkControlado.IsChecked =
                false;

            chkActivo.IsChecked =
                true;
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
    }
}