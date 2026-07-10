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
        List<Producto> listaCompletaProductos = new();

        // =========================================
        // CARRUSEL DE IMÁGENES
        // =========================================

        List<ImagenProducto> imagenesProductoActual = new();
        int indiceImagenActual = 0;
        const int MAX_IMAGENES = 3;

        // ✅ NUEVO — imágenes seleccionadas antes de guardar el producto (aún no existen en BD)
        List<string> rutasImagenesPendientes = new();

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

        private void CargarProductos()
        {
            List<Producto> lista = new List<Producto>();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT *
              FROM Productos
              WHERE Activo = 1
              ORDER BY Nombre";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Producto
                {
                    Id = Convert.ToInt32(reader["Id"]),

                    CodigoBarras = reader["CodigoBarras"].ToString(),

                    Nombre = reader["Nombre"].ToString(),

                    Descripcion = reader["Descripcion"].ToString(),

                    CategoriaId = Convert.ToInt32(reader["CategoriaId"]),

                    SubcategoriaId = reader["SubcategoriaId"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(reader["SubcategoriaId"]),

                    PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),

                    PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"]),

                    Precio2 = Convert.ToDecimal(reader["Precio2"]),

                    CantidadMayoreo2 = Convert.ToInt32(reader["CantidadMayoreo2"]),

                    Precio3 = Convert.ToDecimal(reader["Precio3"]),

                    CantidadMayoreo3 = Convert.ToInt32(reader["CantidadMayoreo3"]),

                    Stock = Convert.ToInt32(reader["Stock"]),

                    StockMinimo = Convert.ToInt32(reader["StockMinimo"]),

                    ImagenURL = reader["ImagenURL"].ToString(),

                    Activo = Convert.ToBoolean(reader["Activo"])
                });
            }

            dgProductos.ItemsSource = lista;
            listaCompletaProductos = lista;

            icCatalogoVista.ItemsSource = listaCompletaProductos;
        }

        // =========================================
        // BUSCAR PRODUCTOS
        // =========================================

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar producto...")
                return;

            string texto = txtBuscar.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(texto))
            {
                dgProductos.ItemsSource = listaCompletaProductos;
                icCatalogoVista.ItemsSource = listaCompletaProductos;
                return;
            }

            var filtrados = listaCompletaProductos
                .Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.CodigoBarras.ToLower().Contains(texto))
                .ToList();

            dgProductos.ItemsSource = filtrados;
            icCatalogoVista.ItemsSource = filtrados;
        }

        // =========================================
        // CARGAR CATEGORIAS
        // =========================================

        private void CargarCategorias()
        {
            List<Categoria> lista = new List<Categoria>();

            using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query = "SELECT * FROM Categorias";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Categoria
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Nombre = reader["Nombre"].ToString()
                });
            }

            cbCategorias.ItemsSource = lista;
            cbCategorias.DisplayMemberPath = "Nombre";
            cbCategorias.SelectedValuePath = "Id";
        }

        // =========================================
        // CARGAR TODAS LAS SUBCATEGORIAS
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
        // FILTRAR SUBCATEGORIAS POR CATEGORIA
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
                        Precio2,
                        CantidadMayoreo2,
                        Precio3,
                        CantidadMayoreo3,
                        Stock,
                        StockMinimo,
                        ImagenURL,
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
                        @Precio2,
                        @CantidadMayoreo2,
                        @Precio3,
                        @CantidadMayoreo3,
                        @Stock,
                        @StockMinimo,
                        @ImagenURL,
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

                        CodigoBarras = @CodigoBarras,
                        Nombre = @Nombre,
                        Descripcion = @Descripcion,
                        CategoriaId = @CategoriaId,
                        SubcategoriaId = @SubcategoriaId,
                        PrecioCompra = @PrecioCompra,
                        PrecioVenta = @PrecioVenta,
                        Precio2 = @Precio2,
                        CantidadMayoreo2 = @CantidadMayoreo2,
                        Precio3 = @Precio3,
                        CantidadMayoreo3 = @CantidadMayoreo3,
                        Stock = @Stock,
                        StockMinimo = @StockMinimo,
                        ImagenURL = @ImagenURL,
                        Activo = @Activo

                    WHERE Id = @Id";
                }

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@CodigoBarras", txtCodigo.Text);

                cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);

                cmd.Parameters.AddWithValue("@Descripcion", txtDescripcion.Text);

                cmd.Parameters.AddWithValue("@CategoriaId", cbCategorias.SelectedValue);

                cmd.Parameters.AddWithValue("@SubcategoriaId", cbSubcategorias.SelectedValue ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@PrecioCompra", decimal.Parse(txtPrecioCompra.Text));

                cmd.Parameters.AddWithValue("@PrecioVenta", decimal.Parse(txtPrecioVenta.Text));

                cmd.Parameters.AddWithValue("@Precio2",
                    decimal.TryParse(txtPrecio2.Text, out decimal precio2) ? precio2 : 0);

                cmd.Parameters.AddWithValue("@CantidadMayoreo2",
                    int.TryParse(txtCantidadMayoreo2.Text, out int cant2) ? cant2 : 0);

                cmd.Parameters.AddWithValue("@Precio3",
                    decimal.TryParse(txtPrecio3.Text, out decimal precio3) ? precio3 : 0);

                cmd.Parameters.AddWithValue("@CantidadMayoreo3",
                    int.TryParse(txtCantidadMayoreo3.Text, out int cant3) ? cant3 : 0);

                cmd.Parameters.AddWithValue("@Stock", int.Parse(txtStock.Text));

                cmd.Parameters.AddWithValue("@StockMinimo", int.Parse(txtStockMinimo.Text));

                cmd.Parameters.AddWithValue("@ImagenURL", "");

                cmd.Parameters.AddWithValue("@Activo", chkActivo.IsChecked ?? true);

                bool esProductoNuevo = productoId == 0;

                if (!esProductoNuevo)
                {
                    cmd.Parameters.AddWithValue("@Id", productoId);

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var resultado = cmd.ExecuteScalar();
                    productoId = Convert.ToInt32(resultado);
                }

                // ✅ NUEVO — Si el producto era nuevo, sube las imágenes que quedaron pendientes
                if (esProductoNuevo && rutasImagenesPendientes.Count > 0)
                {
                    foreach (var ruta in rutasImagenesPendientes)
                    {
                        GuardarImagenEnBD(productoId, ruta);
                    }

                    rutasImagenesPendientes.Clear();
                }

                MessageBox.Show("Producto guardado correctamente");

                CargarLotes();
                CargarProductos();

                // ✅ NUEVO — refresca el carrusel para mostrar las imágenes ya guardadas en BD
                CargarImagenesProducto(productoId);
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
            if (dgProductos.SelectedItem is Producto producto)
            {
                productoId = producto.Id;

                // ✅ Al seleccionar un producto ya existente, se descarta cualquier
                // imagen pendiente que hubiera quedado de un producto nuevo sin guardar
                rutasImagenesPendientes.Clear();

                txtCodigo.Text = producto.CodigoBarras;

                txtNombre.Text = producto.Nombre;

                txtDescripcion.Text = producto.Descripcion;

                cbCategorias.SelectedValue = producto.CategoriaId;

                CbCategorias_SelectionChanged(this, null);

                cbSubcategorias.SelectedValue = producto.SubcategoriaId;

                txtPrecioCompra.Text = producto.PrecioCompra.ToString();

                txtPrecioVenta.Text = producto.PrecioVenta.ToString();

                txtPrecio2.Text = producto.Precio2 > 0 ? producto.Precio2.ToString() : "";

                txtCantidadMayoreo2.Text = producto.CantidadMayoreo2 > 0 ? producto.CantidadMayoreo2.ToString() : "";

                txtPrecio3.Text = producto.Precio3 > 0 ? producto.Precio3.ToString() : "";

                txtCantidadMayoreo3.Text = producto.CantidadMayoreo3 > 0 ? producto.CantidadMayoreo3.ToString() : "";

                txtStock.Text = producto.Stock.ToString();

                txtStockMinimo.Text = producto.StockMinimo.ToString();

                chkActivo.IsChecked = producto.Activo;

                CargarLotes();

                CargarImagenesProducto(producto.Id);
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

            txtPrecio2.Clear();
            txtCantidadMayoreo2.Clear();
            txtPrecio3.Clear();
            txtCantidadMayoreo3.Clear();

            txtStock.Clear();
            txtStockMinimo.Clear();

            imgProductoPreview.Source = null;

            cbCategorias.SelectedIndex = -1;
            cbSubcategorias.ItemsSource = null;

            chkActivo.IsChecked = true;

            dgLotes.ItemsSource = null;
            txtNumeroLote.Clear();
            txtCantidadLote.Clear();
            dpCaducidadLote.SelectedDate = null;

            imagenesProductoActual.Clear();
            indiceImagenActual = 0;
            imgProductoPreview.Source = null;
            txtIndicadorImagen.Text = "0 / 0";
            rutasImagenesPendientes.Clear();
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
                    MessageBox.Show("Selecciona un producto");
                    return;
                }

                using SqlConnection conn =
                 new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string query =
                @"UPDATE Productos
                  SET Activo = 0
                  WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@Id", productoId);

                cmd.ExecuteNonQuery();

                MessageBox.Show("Producto eliminado");

                Limpiar();

                CargarProductos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // =========================================
        // CARGAR IMÁGENES DEL PRODUCTO SELECCIONADO
        // =========================================

        private void CargarImagenesProducto(int idProducto)
        {
            imagenesProductoActual.Clear();
            indiceImagenActual = 0;

            if (idProducto == 0)
            {
                // ✅ Producto nuevo sin guardar: mostrar imágenes pendientes en vez de consultar BD
                MostrarImagenesPendientes();
                return;
            }

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query =
            @"SELECT * FROM ImagenesProducto
              WHERE ProductoId = @ProductoId
              ORDER BY Orden";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProductoId", idProducto);

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                imagenesProductoActual.Add(new ImagenProducto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    ProductoId = Convert.ToInt32(reader["ProductoId"]),
                    RutaImagen = reader["RutaImagen"].ToString() ?? "",
                    Orden = Convert.ToInt32(reader["Orden"])
                });
            }

            MostrarImagenActual();
        }

        // =========================================
        // MOSTRAR LA IMAGEN SEGÚN EL ÍNDICE ACTUAL (producto ya guardado)
        // =========================================

        private void MostrarImagenActual()
        {
            if (imagenesProductoActual.Count == 0)
            {
                imgProductoPreview.Source = null;
                txtIndicadorImagen.Text = "0 / 0";
                return;
            }

            var imagen = imagenesProductoActual[indiceImagenActual];

            try
            {
                imgProductoPreview.Source =
                    new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(imagen.RutaImagen));
            }
            catch
            {
                imgProductoPreview.Source = null;
            }

            txtIndicadorImagen.Text =
                $"{indiceImagenActual + 1} / {imagenesProductoActual.Count}";
        }

        // =========================================
        // ✅ NUEVO — MOSTRAR IMÁGENES PENDIENTES (producto aún no guardado)
        // =========================================

        private void MostrarImagenesPendientes()
        {
            if (rutasImagenesPendientes.Count == 0)
            {
                imgProductoPreview.Source = null;
                txtIndicadorImagen.Text = "0 / 0";
                return;
            }

            if (indiceImagenActual < 0 || indiceImagenActual >= rutasImagenesPendientes.Count)
                indiceImagenActual = rutasImagenesPendientes.Count - 1;

            try
            {
                imgProductoPreview.Source =
                    new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(rutasImagenesPendientes[indiceImagenActual]));
            }
            catch
            {
                imgProductoPreview.Source = null;
            }

            txtIndicadorImagen.Text =
                $"{indiceImagenActual + 1} / {rutasImagenesPendientes.Count}  (sin guardar)";
        }

        // =========================================
        // NAVEGAR ENTRE IMÁGENES
        // =========================================

        private void BtnImagenAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (productoId == 0)
            {
                if (rutasImagenesPendientes.Count == 0)
                    return;

                indiceImagenActual =
                    (indiceImagenActual - 1 + rutasImagenesPendientes.Count) % rutasImagenesPendientes.Count;

                MostrarImagenesPendientes();
                return;
            }

            if (imagenesProductoActual.Count == 0)
                return;

            indiceImagenActual--;

            if (indiceImagenActual < 0)
                indiceImagenActual = imagenesProductoActual.Count - 1;

            MostrarImagenActual();
        }

        private void BtnImagenSiguiente_Click(object sender, RoutedEventArgs e)
        {
            if (productoId == 0)
            {
                if (rutasImagenesPendientes.Count == 0)
                    return;

                indiceImagenActual =
                    (indiceImagenActual + 1) % rutasImagenesPendientes.Count;

                MostrarImagenesPendientes();
                return;
            }

            if (imagenesProductoActual.Count == 0)
                return;

            indiceImagenActual++;

            if (indiceImagenActual >= imagenesProductoActual.Count)
                indiceImagenActual = 0;

            MostrarImagenActual();
        }

        // =========================================
        // ✅ CARGAR NUEVA IMAGEN (máximo 3, con o sin producto guardado)
        // =========================================

        private void BtnCargarImagen_Click(object sender, RoutedEventArgs e)
        {
            int totalImagenesActuales = productoId == 0
                ? rutasImagenesPendientes.Count
                : imagenesProductoActual.Count;

            if (totalImagenesActuales >= MAX_IMAGENES)
            {
                MessageBox.Show($"Ya tienes el máximo de {MAX_IMAGENES} imágenes para este producto");
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.webp"
            };

            if (dialog.ShowDialog() != true)
                return;

            if (productoId == 0)
            {
                // ✅ Producto aún no guardado: la imagen se queda en memoria por ahora
                rutasImagenesPendientes.Add(dialog.FileName);
                indiceImagenActual = rutasImagenesPendientes.Count - 1;
                MostrarImagenesPendientes();
            }
            else
            {
                // Producto ya existe: se guarda directo en la base de datos
                GuardarImagenEnBD(productoId, dialog.FileName);
                CargarImagenesProducto(productoId);

                indiceImagenActual = imagenesProductoActual.Count - 1;
                MostrarImagenActual();
            }
        }

        private void GuardarImagenEnBD(int idProducto, string ruta)
        {
            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            // Calcula el siguiente orden basándose en lo que ya existe en BD
            int siguienteOrden = 1;

            string queryConteo = "SELECT COUNT(*) FROM ImagenesProducto WHERE ProductoId = @ProductoId";
            SqlCommand cmdConteo = new SqlCommand(queryConteo, conn);
            cmdConteo.Parameters.AddWithValue("@ProductoId", idProducto);
            siguienteOrden = Convert.ToInt32(cmdConteo.ExecuteScalar()) + 1;

            string query =
            @"INSERT INTO ImagenesProducto (ProductoId, RutaImagen, Orden)
              VALUES (@ProductoId, @RutaImagen, @Orden)";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProductoId", idProducto);
            cmd.Parameters.AddWithValue("@RutaImagen", ruta);
            cmd.Parameters.AddWithValue("@Orden", siguienteOrden);

            cmd.ExecuteNonQuery();
        }

        // =========================================
        // ✅ ELIMINAR IMAGEN ACTUAL (pendiente o ya guardada)
        // =========================================

        private void BtnEliminarImagen_Click(object sender, RoutedEventArgs e)
        {
            if (productoId == 0)
            {
                if (rutasImagenesPendientes.Count == 0)
                    return;

                var confirmacionPendiente = MessageBox.Show(
                    "¿Quitar esta imagen de la selección?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmacionPendiente != MessageBoxResult.Yes)
                    return;

                rutasImagenesPendientes.RemoveAt(indiceImagenActual);
                indiceImagenActual = 0;
                MostrarImagenesPendientes();
                return;
            }

            if (imagenesProductoActual.Count == 0)
                return;

            var imagenAEliminar = imagenesProductoActual[indiceImagenActual];

            var confirmacion = MessageBox.Show(
                "¿Eliminar esta imagen?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmacion != MessageBoxResult.Yes)
                return;

            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string query = "DELETE FROM ImagenesProducto WHERE Id = @Id";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", imagenAEliminar.Id);

            cmd.ExecuteNonQuery();

            CargarImagenesProducto(productoId);
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
        // TEXTBOX PLACEHOLDER BUSCAR
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
        // LOTES
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
                    MessageBox.Show("Primero guarda o selecciona un producto");
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

        // =========================================
        // CERRAR VENTANA
        // =========================================

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}