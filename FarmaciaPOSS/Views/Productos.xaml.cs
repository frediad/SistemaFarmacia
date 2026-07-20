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

        // ✅ NUEVO — categorías y filtro de la barra
        List<Categoria> categoriasCache = new();
        int categoriaFiltroActual = 0; // 0 = "Todos"

        // Carrusel de imágenes
        List<ImagenProducto> imagenesProductoActual = new();
        int indiceImagenActual = 0;
        const int MAX_IMAGENES = 3;
        List<byte[]> imagenesPendientes = new();

        public ProductosWindow()
        {
            InitializeComponent();

            dgProductos.AlternationCount = 2; // ✅ habilita filas alternadas

            CargarCategorias();        // ✅ primero, para mapear nombres
            CargarProductos();
            CargarTodasSubcategorias();
            CargarCategoriasFiltro();  // ✅ construye la barra de categorías
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
            @"SELECT p.*,
            (SELECT TOP 1 img.ImagenData
             FROM ImagenesProducto img
             WHERE img.ProductoId = p.Id
             ORDER BY img.Orden) AS PrimeraImagenData
              FROM Productos p
              WHERE p.Activo = 1
              ORDER BY p.Nombre";

            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int catId = Convert.ToInt32(reader["CategoriaId"]);

                lista.Add(new Producto
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    CodigoBarras = reader["CodigoBarras"].ToString(),
                    Nombre = reader["Nombre"].ToString(),
                    Descripcion = reader["Descripcion"].ToString(),
                    CategoriaId = catId,
                    SubcategoriaId = reader["SubcategoriaId"] == DBNull.Value ? null : Convert.ToInt32(reader["SubcategoriaId"]),
                    PrecioCompra = Convert.ToDecimal(reader["PrecioCompra"]),
                    PrecioVenta = Convert.ToDecimal(reader["PrecioVenta"]),
                    Precio2 = Convert.ToDecimal(reader["Precio2"]),
                    CantidadMayoreo2 = Convert.ToInt32(reader["CantidadMayoreo2"]),
                    Precio3 = Convert.ToDecimal(reader["Precio3"]),
                    CantidadMayoreo3 = Convert.ToInt32(reader["CantidadMayoreo3"]),
                    Stock = Convert.ToInt32(reader["Stock"]),
                    StockMinimo = Convert.ToInt32(reader["StockMinimo"]),
                    ImagenBytes = reader["PrimeraImagenData"] != DBNull.Value
                        ? (byte[])reader["PrimeraImagenData"]
                        : null,
                    Activo = Convert.ToBoolean(reader["Activo"]),


                    // ✅ nuevo — nombre de categoría legible para la tabla
                    NombreCategoria = categoriasCache
                        .FirstOrDefault(c => c.Id == catId)?.Nombre ?? "Sin categoría"

                   

 
 

                });
            }


            listaCompletaProductos = lista;
            AplicarFiltros(); // ✅ respeta el filtro de categoría/búsqueda activo al recargar
        }

        // =========================================
        // ✅ BARRA DE CATEGORÍAS (FILTRO)
        // =========================================

        private void CargarCategoriasFiltro()
        {
            pnlCategoriasFiltro.Children.Clear();

            var btnTodos = new Button
            {
                Content = "🏠 Todos",
                Style = (Style)FindResource("BtnCategoriaActiva"),
                Tag = 0
            };
            btnTodos.Click += BtnCategoriaFiltro_Click;
            pnlCategoriasFiltro.Children.Add(btnTodos);

            foreach (var categoria in categoriasCache)
            {
                var btn = new Button
                {
                    Content = categoria.Nombre,
                    Style = (Style)FindResource("BtnCategoria"),
                    Tag = categoria.Id
                };
                btn.Click += BtnCategoriaFiltro_Click;
                pnlCategoriasFiltro.Children.Add(btn);
            }
        }

        private void BtnCategoriaFiltro_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            categoriaFiltroActual = Convert.ToInt32(btn?.Tag ?? 0);

            foreach (Button b in pnlCategoriasFiltro.Children.OfType<Button>())
                b.Style = (Style)FindResource("BtnCategoria");

            btn!.Style = (Style)FindResource("BtnCategoriaActiva");

            AplicarFiltros();
        }

        // =========================================
        // BUSCAR PRODUCTOS (+ filtro de categoría)
        // =========================================

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar producto...")
                return;

            AplicarFiltros();
        }

        // ✅ NUEVO — combina texto de búsqueda + categoría seleccionada
        private void AplicarFiltros()
        {
            string texto = (txtBuscar.Text == "Buscar producto...") ? "" : txtBuscar.Text.Trim().ToLower();

            var filtrados = listaCompletaProductos.AsEnumerable();

            if (categoriaFiltroActual != 0)
                filtrados = filtrados.Where(p => p.CategoriaId == categoriaFiltroActual);

            if (!string.IsNullOrWhiteSpace(texto))
                filtrados = filtrados.Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.CodigoBarras.ToLower().Contains(texto));

            var resultado = filtrados.ToList();

            dgProductos.ItemsSource = resultado;
            icCatalogoVista.ItemsSource = resultado;

            dgProductos.ItemsSource = lista;

            listaCompletaProductos = lista;

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
                return;
            }

            dgProductos.ItemsSource = listaCompletaProductos
                .Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.CodigoBarras.ToLower().Contains(texto))
                .ToList();
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

            string query = "SELECT * FROM Categorias ORDER BY Nombre";

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

            categoriasCache = lista; // ✅ nuevo
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
        // FILTRAR SUBCATEGORIAS POR CATEGORIA (en el formulario)
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
        // ✅ AGREGAR NUEVA CATEGORÍA
        // =========================================

        private void BtnNuevaCategoria_Click(object sender, RoutedEventArgs e)
        {
            string nombre =
                Microsoft.VisualBasic.Interaction.InputBox(
                    "Nombre de la nueva categoría:",
                    "Agregar categoría",
                    "");

            if (string.IsNullOrWhiteSpace(nombre))
                return;

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string queryExiste = "SELECT COUNT(*) FROM Categorias WHERE Nombre = @Nombre";
            SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
            cmdExiste.Parameters.AddWithValue("@Nombre", nombre.Trim());

            int existentes = Convert.ToInt32(cmdExiste.ExecuteScalar());

            if (existentes > 0)
            {
                MessageBox.Show("Ya existe una categoría con ese nombre");
                return;
            }

            string query =
            @"INSERT INTO Categorias (Nombre)
              VALUES (@Nombre);
              SELECT SCOPE_IDENTITY();";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());

            int nuevaCategoriaId = Convert.ToInt32(cmd.ExecuteScalar());

            CargarCategorias();
            CargarCategoriasFiltro(); // ✅ refresca también la barra de filtro

            cbCategorias.SelectedValue = nuevaCategoriaId;

            MessageBox.Show($"Categoría \"{nombre}\" agregada correctamente");
        }

        // =========================================
        // ✅ AGREGAR NUEVA SUBCATEGORÍA
        // =========================================

        private void BtnNuevaSubcategoria_Click(object sender, RoutedEventArgs e)
        {
            if (cbCategorias.SelectedValue == null)
            {
                MessageBox.Show("Primero selecciona una categoría para asignarle la subcategoría");
                return;
            }

            int categoriaId = Convert.ToInt32(cbCategorias.SelectedValue);
            string nombreCategoria = (cbCategorias.SelectedItem as Categoria)?.Nombre ?? "";

            string nombre =
                Microsoft.VisualBasic.Interaction.InputBox(
                    $"Nombre de la nueva subcategoría para \"{nombreCategoria}\":",
                    "Agregar subcategoría",
                    "");

            if (string.IsNullOrWhiteSpace(nombre))
                return;

            using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
            conn.Open();

            string queryExiste = "SELECT COUNT(*) FROM Subcategorias WHERE Nombre = @Nombre AND CategoriaId = @CategoriaId";
            SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
            cmdExiste.Parameters.AddWithValue("@Nombre", nombre.Trim());
            cmdExiste.Parameters.AddWithValue("@CategoriaId", categoriaId);

            int existentes = Convert.ToInt32(cmdExiste.ExecuteScalar());

            if (existentes > 0)
            {
                MessageBox.Show("Ya existe una subcategoría con ese nombre en esta categoría");
                return;
            }

            string query =
            @"INSERT INTO Subcategorias (Nombre, CategoriaId)
              VALUES (@Nombre, @CategoriaId);
              SELECT SCOPE_IDENTITY();";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Nombre", nombre.Trim());
            cmd.Parameters.AddWithValue("@CategoriaId", categoriaId);

            int nuevaSubcategoriaId = Convert.ToInt32(cmd.ExecuteScalar());

            CargarTodasSubcategorias();
            CbCategorias_SelectionChanged(this, null);

            cbSubcategorias.SelectedValue = nuevaSubcategoriaId;

            MessageBox.Show($"Subcategoría \"{nombre}\" agregada correctamente");
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

                if (productoId == 0)
                {
                    query =
                    @"INSERT INTO Productos
                    (
                        CodigoBarras, Nombre, Descripcion, CategoriaId, SubcategoriaId,
                        PrecioCompra, PrecioVenta, Precio2, CantidadMayoreo2, Precio3, CantidadMayoreo3,
                        Stock, StockMinimo, ImagenURL,Activo, FechaCreacion
                    )
                    VALUES
                    (

                        @CodigoBarras, @Nombre, @Descripcion, @CategoriaId, @SubcategoriaId,
                        @PrecioCompra, @PrecioVenta, @Precio2, @CantidadMayoreo2, @Precio3, @CantidadMayoreo3,
                        @Stock, @StockMinimo, @ImagenURL, @Activo, GETDATE()

                        @CodigoBarras,
                        @Nombre,
                        @Descripcion,
                        @CategoriaId,
                        @SubcategoriaId,
                        @PrecioCompra,
                        @PrecioVenta,
                        @Stock,
                        @StockMinimo,
                        @ImagenURL,
                        @Activo,
                        GETDATE()

                    );
                    SELECT SCOPE_IDENTITY();";
                }
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
                     "");

               

                cmd.Parameters.AddWithValue(
                    "@Activo",
                    chkActivo.IsChecked
                    ?? true);

                if (productoId != 0)

                {
                    cmd.Parameters.AddWithValue("@Id", productoId);
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    var resultado = cmd.ExecuteScalar();
                    productoId = Convert.ToInt32(resultado);
                }

                if (esProductoNuevo && imagenesPendientes.Count > 0)
                {
                    foreach (var bytes in imagenesPendientes)
                    {
                        GuardarImagenEnBD(productoId, bytes);
                    }

                    imagenesPendientes.Clear();
                }

                MessageBox.Show("Producto guardado correctamente");

                CargarLotes();
                CargarProductos();

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

                imagenesPendientes.Clear();

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

                txtStockMinimo.Text =
                    producto.StockMinimo
                    .ToString();

         

                chkActivo.IsChecked =
                    producto.Activo;

                // ✅ Cargar lotes del producto seleccionado
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


            imagenesPendientes.Clear();

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


        List<ImagenProducto> imagenesProductoActual = new();
        int indiceImagenActual = 0;
        const int MAX_IMAGENES = 3;

        // =========================================
        // ✅ CARGAR IMÁGENES DEL PRODUCTO SELECCIONADO
        // =========================================

        private void CargarImagenesProducto(int idProducto)
        {
            imagenesProductoActual.Clear();
            indiceImagenActual = 0;


            if (idProducto == 0)
            {
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

                    RutaImagen = reader["RutaImagen"]?.ToString() ?? "",
                    Orden = Convert.ToInt32(reader["Orden"]),
                    ImagenData = reader["ImagenData"] != DBNull.Value
                        ? (byte[])reader["ImagenData"]
                        : null

                    RutaImagen = reader["RutaImagen"].ToString() ?? "",
                    Orden = Convert.ToInt32(reader["Orden"])

                });
            }

            MostrarImagenActual();
        }

        // =========================================
        // ✅ MOSTRAR LA IMAGEN SEGÚN EL ÍNDICE ACTUAL
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


            imgProductoPreview.Source = BytesABitmap(imagen.ImagenData);

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

        private void MostrarImagenesPendientes()
        {
            if (imagenesPendientes.Count == 0)
            {
                imgProductoPreview.Source = null;
                txtIndicadorImagen.Text = "0 / 0";
                return;
            }

            if (indiceImagenActual < 0 || indiceImagenActual >= imagenesPendientes.Count)
                indiceImagenActual = imagenesPendientes.Count - 1;

            imgProductoPreview.Source = BytesABitmap(imagenesPendientes[indiceImagenActual]);

            txtIndicadorImagen.Text =
                $"{indiceImagenActual + 1} / {imagenesPendientes.Count}  (sin guardar)";
        }

        private System.Windows.Media.ImageSource? BytesABitmap(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            try
            {
                using var stream = new System.IO.MemoryStream(bytes);

                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
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

        private void BtnImagenAnterior_Click(object sender, RoutedEventArgs e)
        {
            if (productoId == 0)
            {
                if (imagenesPendientes.Count == 0)
                    return;

                indiceImagenActual =
                    (indiceImagenActual - 1 + imagenesPendientes.Count) % imagenesPendientes.Count;

                MostrarImagenesPendientes();
                return;
            }


        // =========================================
        // ✅ NAVEGAR ENTRE IMÁGENES
        // =========================================

        private void BtnImagenAnterior_Click(object sender, RoutedEventArgs e)
        {

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
                if (imagenesPendientes.Count == 0)
                    return;

                indiceImagenActual = (indiceImagenActual + 1) % imagenesPendientes.Count;

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


        private void BtnCargarImagen_Click(object sender, RoutedEventArgs e)
        {
            int totalImagenesActuales = productoId == 0
                ? imagenesPendientes.Count
                : imagenesProductoActual.Count;

            if (totalImagenesActuales >= MAX_IMAGENES)

        // =========================================
        // ✅ CARGAR NUEVA IMAGEN (máximo 3)
        // =========================================

        private void BtnCargarImagen_Click(object sender, RoutedEventArgs e)
        {
            if (productoId == 0)
            {
                MessageBox.Show("Primero guarda el producto antes de agregar imágenes");
                return;
            }

            if (imagenesProductoActual.Count >= MAX_IMAGENES)

            {
                MessageBox.Show($"Ya tienes el máximo de {MAX_IMAGENES} imágenes para este producto");
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp"
            };


            if (dialog.ShowDialog() != true)
                return;

            byte[] bytes;

            try
            {
                bytes = System.IO.File.ReadAllBytes(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo leer el archivo de imagen: {ex.Message}");
                return;
            }

            const int limiteBytes = 5 * 1024 * 1024;

            if (bytes.Length > limiteBytes)
            {
                MessageBox.Show("La imagen es demasiado grande. El máximo permitido es 5 MB.");
                return;
            }

            if (productoId == 0)
            {
                imagenesPendientes.Add(bytes);
                indiceImagenActual = imagenesPendientes.Count - 1;
                MostrarImagenesPendientes();
            }
            else
            {
                GuardarImagenEnBD(productoId, bytes);
                CargarImagenesProducto(productoId);

                indiceImagenActual = imagenesProductoActual.Count - 1;
                MostrarImagenActual();
            }
        }

        private void GuardarImagenEnBD(int idProducto, byte[] imagenBytes)
        {
            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            string queryConteo = "SELECT COUNT(*) FROM ImagenesProducto WHERE ProductoId = @ProductoId";
            SqlCommand cmdConteo = new SqlCommand(queryConteo, conn);
            cmdConteo.Parameters.AddWithValue("@ProductoId", idProducto);
            int siguienteOrden = Convert.ToInt32(cmdConteo.ExecuteScalar()) + 1;

            string query =
            @"INSERT INTO ImagenesProducto (ProductoId, ImagenData, Orden)
              VALUES (@ProductoId, @ImagenData, @Orden)";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ProductoId", idProducto);
            cmd.Parameters.Add("@ImagenData", System.Data.SqlDbType.VarBinary, -1).Value = imagenBytes;
            cmd.Parameters.AddWithValue("@Orden", siguienteOrden);

            cmd.ExecuteNonQuery();
        }

        private void BtnEliminarImagen_Click(object sender, RoutedEventArgs e)
        {
            if (productoId == 0)
            {
                if (imagenesPendientes.Count == 0)
                    return;

                var confirmacionPendiente = MessageBox.Show(
                    "¿Quitar esta imagen de la selección?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmacionPendiente != MessageBoxResult.Yes)
                    return;

                imagenesPendientes.RemoveAt(indiceImagenActual);
                indiceImagenActual = 0;
                MostrarImagenesPendientes();
                return;
            }

            if (imagenesProductoActual.Count == 0)
                return;


            if (dialog.ShowDialog() == true)
            {
                GuardarImagenEnBD(productoId, dialog.FileName);
                CargarImagenesProducto(productoId);

                // Mover al índice de la imagen recién agregada
                indiceImagenActual = imagenesProductoActual.Count - 1;
                MostrarImagenActual();
            }
        }

        private void GuardarImagenEnBD(int idProducto, string ruta)
        {
            using SqlConnection conn =
                new SqlConnection(DatabaseHelper.ConnectionString);

            conn.Open();

            int siguienteOrden = imagenesProductoActual.Count + 1;

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
        // ✅ ELIMINAR IMAGEN ACTUAL
        // =========================================

        private void BtnEliminarImagen_Click(object sender, RoutedEventArgs e)
        {
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