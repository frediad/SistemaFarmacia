
using FarmaciaPOS.Helpers;
using FarmaciaPOS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FarmaciaPOS.Views
{
    public partial class ProductosWindow : Window
    {
        int productoId = 0;
        List<Subcategoria> todasSubcategorias = new();
        List<Producto> listaCompletaProductos = new();

        List<Categoria> categoriasCache = new();
        FiltroCatalogo filtroFiltroActual = new FiltroCatalogo { Tipo = "Todos", Id = 0 };

        List<ImagenProducto> imagenesProductoActual = new();
        int indiceImagenActual = 0;
        const int MAX_IMAGENES = 3;
        List<byte[]> imagenesPendientes = new();

        // IVA fijo del 16%. No es editable desde la interfaz: se usa
        // este valor constante en todos los cálculos de precio final.
        const decimal IVA_FIJO = 16m;

        public ProductosWindow()
        {
            try
            {
                InitializeComponent();

                dgProductos.AlternationCount = 2;

                CargarCategorias();
                CargarProductos();
                CargarTodasSubcategorias();
                CargarCategoriasFiltro();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo abrir la ventana de Productos:\n\n" + ex,
                    "Error al cargar Productos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                throw;
            }
        }

        // =========================================
        // ✅ ABRIR GESTIÓN DE CATEGORÍAS/SUBCATEGORÍAS
        // =========================================

        private void BtnGestionarCategorias_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new GestionCategoriasWindow
            {
                Owner = this
            };

            ventana.ShowDialog();

            if (ventana.HuboCambios)
            {
                CargarCategorias();
                CargarTodasSubcategorias();
                CargarCategoriasFiltro();
                CargarProductos();
            }
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
                    NombreCategoria = categoriasCache
                        .FirstOrDefault(c => c.Id == catId)?.Nombre ?? "Sin categoría"
                });
            }

            listaCompletaProductos = lista;
            AplicarFiltros();
        }

        // =========================================
        // BARRA DE CATEGORÍAS (FILTRO)
        // =========================================

        private void CargarCategoriasFiltro()
        {
            pnlCategoriasFiltro.Children.Clear();

            var btnTodos = new Button
            {
                Content = "🏠 Todos",
                Style = (Style)FindResource(filtroFiltroActual.Tipo == "Todos" ? "BtnCategoriaActiva" : "BtnCategoria"),
                Tag = new FiltroCatalogo { Tipo = "Todos", Id = 0 }
            };
            btnTodos.Click += BtnCategoriaFiltro_Click;
            pnlCategoriasFiltro.Children.Add(btnTodos);

            var subcategoriasPorCategoria = todasSubcategorias
                .GroupBy(s => s.CategoriaId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var categoria in categoriasCache)
            {
                bool esCategoriaActiva =
                    filtroFiltroActual.Tipo == "Categoria" && filtroFiltroActual.Id == categoria.Id;

                var btnCat = new Button
                {
                    Content = categoria.Nombre,
                    Style = (Style)FindResource(esCategoriaActiva ? "BtnCategoriaActiva" : "BtnCategoria"),
                    Tag = new FiltroCatalogo { Tipo = "Categoria", Id = categoria.Id }
                };
                btnCat.Click += BtnCategoriaFiltro_Click;
                pnlCategoriasFiltro.Children.Add(btnCat);

                if (subcategoriasPorCategoria.TryGetValue(categoria.Id, out var subs))
                {
                    foreach (var sub in subs)
                    {
                        bool esSubActiva =
                            filtroFiltroActual.Tipo == "Subcategoria" && filtroFiltroActual.Id == sub.Id;

                        var btnSub = new Button
                        {
                            Content = "" + sub.Nombre,
                            Style = (Style)FindResource(esSubActiva ? "BtnCategoriaActiva" : "BtnCategoria"),
                            Tag = new FiltroCatalogo { Tipo = "Subcategoria", Id = sub.Id }
                        };
                        btnSub.Click += BtnCategoriaFiltro_Click;
                        pnlCategoriasFiltro.Children.Add(btnSub);
                    }
                }
            }
        }

        private void BtnCategoriaFiltro_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var filtro = btn?.Tag as FiltroCatalogo;

            filtroFiltroActual = filtro ?? new FiltroCatalogo { Tipo = "Todos", Id = 0 };

            foreach (Button b in pnlCategoriasFiltro.Children.OfType<Button>())
                b.Style = (Style)FindResource("BtnCategoria");

            btn!.Style = (Style)FindResource("BtnCategoriaActiva");

            AplicarFiltros();
        }

        // =========================================
        // BUSCAR PRODUCTOS
        // =========================================

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBuscar.Text == "Buscar producto...")
                return;

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string texto = (txtBuscar.Text == "Buscar producto...") ? "" : txtBuscar.Text.Trim().ToLower();

            var filtrados = listaCompletaProductos.AsEnumerable();

            if (filtroFiltroActual.Tipo == "Categoria")
                filtrados = filtrados.Where(p => p.CategoriaId == filtroFiltroActual.Id);
            else if (filtroFiltroActual.Tipo == "Subcategoria")
                filtrados = filtrados.Where(p => p.SubcategoriaId == filtroFiltroActual.Id);

            if (!string.IsNullOrWhiteSpace(texto))
                filtrados = filtrados.Where(p =>
                    p.Nombre.ToLower().Contains(texto) ||
                    p.CodigoBarras.ToLower().Contains(texto));

            var resultado = filtrados.ToList();

            dgProductos.ItemsSource = resultado;
            icCatalogoVista.ItemsSource = resultado;
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

            categoriasCache = lista;
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
        // FILTRAR SUBCATEGORIAS (formulario)
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
        // AGREGAR NUEVA CATEGORÍA
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
                MensajeHelper.Advertencia("Ya existe una categoría con ese nombre", "Aviso", this);
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
            CargarCategoriasFiltro();

            cbCategorias.SelectedValue = nuevaCategoriaId;

            MensajeHelper.Exito($"Categoría \"{nombre}\" agregada correctamente", "Listo", this);
        }

        // =========================================
        // AGREGAR NUEVA SUBCATEGORÍA
        // =========================================

        private void BtnNuevaSubcategoria_Click(object sender, RoutedEventArgs e)
        {
            if (cbCategorias.SelectedValue == null)
            {
                MensajeHelper.Advertencia("Primero selecciona una categoría para asignarle la subcategoría", "Aviso", this);
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
                MensajeHelper.Advertencia("Ya existe una subcategoría con ese nombre en esta categoría", "Aviso", this);
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

            MensajeHelper.Exito($"Subcategoría \"{nombre}\" agregada correctamente", "Listo", this);
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

                string query;
                bool esProductoNuevo = productoId == 0;

                if (esProductoNuevo)
                {
                    query =
                    @"INSERT INTO Productos
                    (
                        CodigoBarras, Nombre, Descripcion, CategoriaId, SubcategoriaId,
                        PrecioCompra, PrecioVenta, Precio2, CantidadMayoreo2, Precio3, CantidadMayoreo3,
                        Stock, StockMinimo, ImagenURL, Activo, FechaCreacion
                    )
                    VALUES
                    (
                        @CodigoBarras, @Nombre, @Descripcion, @CategoriaId, @SubcategoriaId,
                        @PrecioCompra, @PrecioVenta, @Precio2, @CantidadMayoreo2, @Precio3, @CantidadMayoreo3,
                        @Stock, @StockMinimo, @ImagenURL, @Activo, GETDATE()
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

                if (esProductoNuevo && imagenesPendientes.Count > 0)
                {
                    foreach (var bytes in imagenesPendientes)
                    {
                        GuardarImagenEnBD(productoId, bytes);
                    }

                    imagenesPendientes.Clear();
                }

                MensajeHelper.Exito("Producto guardado correctamente", "Listo", this);

                CargarLotes();
                CargarProductos();

                CargarImagenesProducto(productoId);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "ERROR", this);
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

                CalcularPrecioVolumen();

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
            chkIncluirIva.IsChecked = true;
            txtPrecioVentaFinal.Text = "0.00";

            txtPrecio2.Text = "0.00";
            txtCantidadMayoreo2.Clear();
            txtPorcentaje2.Clear();

            txtPrecio3.Text = "0.00";
            txtCantidadMayoreo3.Clear();
            txtPorcentaje3.Clear();

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
                    MensajeHelper.Advertencia("Selecciona un producto", "Aviso", this);
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

                MensajeHelper.Exito("Producto eliminado", "Listo", this);

                Limpiar();
                CargarProductos();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "Error", this);
            }
        }

        // =========================================
        // CARGAR IMÁGENES DEL PRODUCTO SELECCIONADO
        // =========================================

        private void CargarImagenesProducto(int idProducto)
        {
            try
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
                    });
                }

                MostrarImagenActual();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar las imágenes: " + ex.Message, "Error", this);
            }
        }

        // =========================================
        // MOSTRAR IMAGEN
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

        // =========================================
        // NAVEGAR IMÁGENES
        // =========================================

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

        // =========================================
        // CARGAR NUEVA IMAGEN
        // =========================================

        private void BtnCargarImagen_Click(object sender, RoutedEventArgs e)
        {
            int totalImagenesActuales = productoId == 0
                ? imagenesPendientes.Count
                : imagenesProductoActual.Count;

            if (totalImagenesActuales >= MAX_IMAGENES)
            {
                MensajeHelper.Advertencia($"Ya tienes el máximo de {MAX_IMAGENES} imágenes para este producto", "Aviso", this);
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
                MensajeHelper.Error($"No se pudo leer el archivo de imagen: {ex.Message}", "Error", this);
                return;
            }

            const int limiteBytes = 5 * 1024 * 1024;

            if (bytes.Length > limiteBytes)
            {
                MensajeHelper.Advertencia("La imagen es demasiado grande. El máximo permitido es 5 MB.", "Aviso", this);
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
                try
                {
                    GuardarImagenEnBD(productoId, bytes);
                    CargarImagenesProducto(productoId);

                    indiceImagenActual = imagenesProductoActual.Count - 1;
                    MostrarImagenActual();
                }
                catch (Exception ex)
                {
                    MensajeHelper.Error("No se pudo guardar la imagen: " + ex.Message, "Error", this);
                }
            }
        }

        private void GuardarImagenEnBD(int idProducto, byte[] imagenBytes)
        {
            try
            {
                using SqlConnection conn =
                    new SqlConnection(DatabaseHelper.ConnectionString);

                conn.Open();

                string queryConteo = "SELECT COUNT(*) FROM ImagenesProducto WHERE ProductoId = @ProductoId";
                SqlCommand cmdConteo = new SqlCommand(queryConteo, conn);
                cmdConteo.Parameters.AddWithValue("@ProductoId", idProducto);
                int siguienteOrden = Convert.ToInt32(cmdConteo.ExecuteScalar()) + 1;

                string query =
                @"INSERT INTO ImagenesProducto (ProductoId, ImagenData, RutaImagen, Orden)
                  VALUES (@ProductoId, @ImagenData, @RutaImagen, @Orden)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProductoId", idProducto);
                cmd.Parameters.Add("@ImagenData", System.Data.SqlDbType.VarBinary, -1).Value = imagenBytes;
                cmd.Parameters.AddWithValue("@RutaImagen", "");
                cmd.Parameters.AddWithValue("@Orden", siguienteOrden);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo guardar la imagen: " + ex.Message, "Error", this);
            }
        }

        // =========================================
        // ELIMINAR IMAGEN
        // =========================================

        private void BtnEliminarImagen_Click(object sender, RoutedEventArgs e)
        {
            if (productoId == 0)
            {
                if (imagenesPendientes.Count == 0)
                    return;

                bool confirmacionPendiente = MensajeHelper.Confirmar(
                    "¿Quitar esta imagen de la selección?",
                    "Confirmar",
                    this);

                if (!confirmacionPendiente)
                    return;

                imagenesPendientes.RemoveAt(indiceImagenActual);
                indiceImagenActual = 0;
                MostrarImagenesPendientes();
                return;
            }

            if (imagenesProductoActual.Count == 0)
                return;

            var imagenAEliminar = imagenesProductoActual[indiceImagenActual];

            bool confirmacion = MensajeHelper.Confirmar(
                "¿Eliminar esta imagen?",
                "Confirmar",
                this);

            if (!confirmacion)
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

        // =========================================
        // ✅ ESCÁNER DE CÓDIGO DE BARRAS
        // =========================================

        private void TxtCodigo_GotFocus(object sender, RoutedEventArgs e)
        {
            // Selecciona todo el texto al entrar al campo, así un nuevo escaneo
            // reemplaza el código anterior en vez de pegarse al final.
            txtCodigo.SelectAll();
        }

        private void TxtCodigo_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            string codigoEscaneado = txtCodigo.Text.Trim();

            if (string.IsNullOrWhiteSpace(codigoEscaneado))
                return;

            // ¿Ese código ya pertenece a un producto existente?
            var productoExistente = listaCompletaProductos
                .FirstOrDefault(p => p.CodigoBarras == codigoEscaneado);

            if (productoExistente != null)
            {
                // Si estamos creando un producto nuevo, avisamos antes de sobreescribir el formulario
                if (productoId == 0 &&
                    (!string.IsNullOrWhiteSpace(txtNombre.Text) || !string.IsNullOrWhiteSpace(txtDescripcion.Text)))
                {
                    bool continuar = MensajeHelper.Confirmar(
                        $"Este código ya pertenece a \"{productoExistente.Nombre}\".\n\n" +
                        "¿Deseas cargar ese producto para editarlo? Se perderán los datos no guardados del formulario actual.",
                        "Código ya registrado",
                        this);

                    if (!continuar)
                        return;
                }

                // Selecciona esa fila en la tabla — dispara dgProductos_SelectionChanged,
                // que ya se encarga de llenar todo el formulario con sus datos.
                dgProductos.SelectedItem = productoExistente;
                dgProductos.ScrollIntoView(productoExistente);

                MensajeHelper.Info(
                    $"Producto encontrado: \"{productoExistente.Nombre}\". Se cargó para editar.",
                    "Producto existente",
                    this);
            }
            else
            {
                // Código nuevo — no existe todavía, así que solo avanzamos el foco
                // para continuar registrando el producto nuevo.
                MensajeHelper.Info(
                    "Código nuevo. Continúa llenando los datos del producto.",
                    "Código disponible",
                    this);

                txtNombre.Focus();
            }
        }

        private void BtnCancelar_Click(
            object sender,
            RoutedEventArgs e)
        {
            Limpiar();
        }

        // =========================================
        // PLACEHOLDER BUSCAR
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
                    MensajeHelper.Advertencia("Primero guarda o selecciona un producto", "Aviso", this);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtNumeroLote.Text))
                {
                    MensajeHelper.Advertencia("Escribe el número de lote", "Aviso", this);
                    return;
                }

                if (!int.TryParse(txtCantidadLote.Text, out int cantidad))
                {
                    MensajeHelper.Advertencia("Cantidad inválida", "Aviso", this);
                    return;
                }

                if (dpCaducidadLote.SelectedDate == null)
                {
                    MensajeHelper.Advertencia("Selecciona la fecha de caducidad", "Aviso", this);
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

                MensajeHelper.Exito("Lote agregado", "Listo", this);

                txtNumeroLote.Clear();
                txtCantidadLote.Clear();
                dpCaducidadLote.SelectedDate = null;

                CargarLotes();
            }
            catch (Exception ex)
            {
                MensajeHelper.Error(ex.Message, "Error", this);
            }
        }

        // =========================================
        // CERRAR VENTANA
        // =========================================

        private void BtnCerrarVentana_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // =========================================
        // Calcula el "Precio final" que se muestra dentro de cada
        // bloque de mayoreo (Precio 2 y Precio 3):
        // 1) Toma el Precio Venta (Precio 1) como base.
        // 2) Si "Incluir IVA" está marcado, le suma el IVA fijo del 16%.
        //    Si no está marcado, usa el precio de venta tal cual.
        // 3) A ese resultado le resta el % de descuento configurado
        //    para ese nivel de mayoreo -> precio unitario.
        // 4) Multiplica ese precio unitario por las piezas indicadas,
        //    para mostrar el total de esa cantidad (no solo 1 pieza).
        // Ejemplo: Precio Venta $100, IVA activado -> $116.00 unitario
        //          20% de descuento -> 116 - 20% de 116 = $92.80 c/u
        //          10 piezas -> 92.80 x 10 = $928.00
        // =========================================
        // =========================================
        // 1) "Precio final" (junto a Precio Venta): es el único lugar
        //    donde se aplica el IVA. Si "Incluir IVA" está marcado,
        //    se le suma el 16% fijo al Precio Venta; si no, se muestra
        //    el mismo Precio Venta sin cambios.
        // 2) "Precios por volumen" (Precio 2 y Precio 3): NUNCA incluyen
        //    IVA. Se calculan directamente sobre el Precio Venta, le
        //    restan su % de descuento y se multiplican por las piezas.
        // Ejemplo: Precio Venta $100, IVA activado -> Precio final $116.00
        //          Precio 2: 20% desc., 10 piezas -> (100 - 20%) x 10 = $800.00
        // =========================================
        private void CalcularPrecioVolumen()
        {
            // Mientras la ventana se está construyendo (InitializeComponent),
            // el checkbox de IVA puede disparar su evento "Checked" antes de
            // que los demás controles (txtPrecio2, txtPrecio3, etc.) existan.
            // Si eso pasa, simplemente no hacemos nada todavía.
            if (txtPrecioVenta == null || txtPrecio2 == null || txtPrecio3 == null ||
                txtCantidadMayoreo2 == null || txtCantidadMayoreo3 == null ||
                txtPorcentaje2 == null || txtPorcentaje3 == null || chkIncluirIva == null ||
                txtPrecioVentaFinal == null || txtLabelPrecioFinal == null)
                return;

            if (!decimal.TryParse(txtPrecioVenta.Text, out decimal precioBase))
            {
                txtPrecioVentaFinal.Text = "0.00";
                txtPrecio2.Text = "0.00";
                txtPrecio3.Text = "0.00";
                return;
            }

            // --- Precio final (con o sin IVA, según el checkbox) ---

            decimal precioFinalVenta = precioBase;

            if (chkIncluirIva.IsChecked == true)
            {
                precioFinalVenta = precioBase + (precioBase * IVA_FIJO / 100);
                txtLabelPrecioFinal.Text = "Precio final (con 16% IVA)";
            }
            else
            {
                txtLabelPrecioFinal.Text = "Precio final (sin IVA)";
            }

            txtPrecioVentaFinal.Text = precioFinalVenta.ToString("0.00");

            // --- Precios por volumen: siempre sin IVA ---

            txtPrecio2.Text = CalcularTotalMayoreo(precioBase, txtPorcentaje2.Text, txtCantidadMayoreo2.Text);
            txtPrecio3.Text = CalcularTotalMayoreo(precioBase, txtPorcentaje3.Text, txtCantidadMayoreo3.Text);
        }

        private void ChkIncluirIva_Changed(object sender, RoutedEventArgs e)
        {
            CalcularPrecioVolumen();
        }

        private string CalcularTotalMayoreo(decimal precioConIva, string textoPorcentaje, string textoPiezas)
        {
            if (!decimal.TryParse(textoPorcentaje, out decimal porcentaje))
                porcentaje = 0;

            // Si no se especifican piezas, se asume 1 (precio unitario).
            if (!decimal.TryParse(textoPiezas, out decimal piezas) || piezas <= 0)
                piezas = 1;

            decimal precioUnitario = precioConIva + (precioConIva * porcentaje / 100);
            decimal total = precioUnitario * piezas;

            return total.ToString("0.00");
        }

        private void txtCantidadMayoreo2_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularPrecioVolumen();
        }

        private void txtCantidadMayoreo3_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularPrecioVolumen();
        }

        private void txtPorcentaje3_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularPrecioVolumen();
        }

        private void txtPrecioVenta_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularPrecioVolumen();
        }

        private void txtPorcentaje2_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularPrecioVolumen();
        }

    }
}
