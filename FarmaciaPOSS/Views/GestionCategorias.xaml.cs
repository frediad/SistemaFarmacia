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
    public partial class GestionCategoriasWindow : Window
    {
        private List<Categoria> categorias = new();
        private List<Subcategoria> subcategorias = new();
        private Categoria? categoriaSeleccionada = null;

        // ✅ Indica si hubo cambios, para que ProductosWindow sepa que debe refrescar
        public bool HuboCambios { get; private set; } = false;

        public GestionCategoriasWindow()
        {
            InitializeComponent();
            CargarCategorias();
        }

        // =========================================
        // CARGAR CATEGORÍAS
        // =========================================

        private void CargarCategorias()
        {
            try
            {
                categorias.Clear();

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query = "SELECT * FROM Categorias ORDER BY Nombre";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    categorias.Add(new Categoria
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString()
                    });
                }

                icCategorias.ItemsSource = null;
                icCategorias.ItemsSource = categorias;
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar las categorías: " + ex.Message, "Error", this);
            }
        }

        private void BtnAgregarCategoria_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNuevaCategoria.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MensajeHelper.Advertencia("Escribe el nombre de la categoría", "Aviso", this);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryExiste = "SELECT COUNT(*) FROM Categorias WHERE Nombre = @Nombre";
                SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
                cmdExiste.Parameters.AddWithValue("@Nombre", nombre);

                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                {
                    MensajeHelper.Advertencia("Ya existe una categoría con ese nombre", "Aviso", this);
                    return;
                }

                string query =
                @"INSERT INTO Categorias (Nombre)
                  VALUES (@Nombre)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.ExecuteNonQuery();

                txtNuevaCategoria.Clear();
                HuboCambios = true;

                CargarCategorias();
                MensajeHelper.Exito($"Categoría \"{nombre}\" agregada correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo agregar la categoría: " + ex.Message, "Error", this);
            }
        }

        private void BtnEditarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Categoria categoria)
                return;

            string nuevoNombre = Microsoft.VisualBasic.Interaction.InputBox(
                "Nuevo nombre de la categoría:",
                "Editar categoría",
                categoria.Nombre);

            if (string.IsNullOrWhiteSpace(nuevoNombre) || nuevoNombre == categoria.Nombre)
                return;

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryExiste = "SELECT COUNT(*) FROM Categorias WHERE Nombre = @Nombre AND Id <> @Id";
                SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
                cmdExiste.Parameters.AddWithValue("@Nombre", nuevoNombre.Trim());
                cmdExiste.Parameters.AddWithValue("@Id", categoria.Id);

                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                {
                    MensajeHelper.Advertencia("Ya existe otra categoría con ese nombre", "Aviso", this);
                    return;
                }

                string query = "UPDATE Categorias SET Nombre = @Nombre WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nuevoNombre.Trim());
                cmd.Parameters.AddWithValue("@Id", categoria.Id);
                cmd.ExecuteNonQuery();

                HuboCambios = true;
                CargarCategorias();

                // Si estábamos viendo las subcategorías de esta categoría, refresca el título
                if (categoriaSeleccionada != null && categoriaSeleccionada.Id == categoria.Id)
                {
                    categoriaSeleccionada.Nombre = nuevoNombre.Trim();
                    txtTituloSubcategorias.Text = $"Subcategorías de \"{categoriaSeleccionada.Nombre}\"";
                }

                MensajeHelper.Exito("Categoría actualizada correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo actualizar la categoría: " + ex.Message, "Error", this);
            }
        }

        private void BtnEliminarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Categoria categoria)
                return;

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                // ✅ Protección: no eliminar si hay productos usando esta categoría
                string queryProductos = "SELECT COUNT(*) FROM Productos WHERE CategoriaId = @Id";
                SqlCommand cmdProductos = new SqlCommand(queryProductos, conn);
                cmdProductos.Parameters.AddWithValue("@Id", categoria.Id);
                int productosConEsaCategoria = Convert.ToInt32(cmdProductos.ExecuteScalar());

                if (productosConEsaCategoria > 0)
                {
                    MensajeHelper.Advertencia(
                        $"No puedes eliminar \"{categoria.Nombre}\": hay {productosConEsaCategoria} producto(s) asignado(s) a esta categoría.",
                        "No se puede eliminar",
                        this);
                    return;
                }

                // ✅ Protección: no eliminar si tiene subcategorías (borra esas primero)
                string querySubs = "SELECT COUNT(*) FROM Subcategorias WHERE CategoriaId = @Id";
                SqlCommand cmdSubs = new SqlCommand(querySubs, conn);
                cmdSubs.Parameters.AddWithValue("@Id", categoria.Id);
                int subcategoriasExistentes = Convert.ToInt32(cmdSubs.ExecuteScalar());

                if (subcategoriasExistentes > 0)
                {
                    MensajeHelper.Advertencia(
                        $"\"{categoria.Nombre}\" tiene {subcategoriasExistentes} subcategoría(s). Elimínalas primero.",
                        "No se puede eliminar",
                        this);
                    return;
                }

                bool confirmar = MensajeHelper.Confirmar(
                    $"¿Eliminar la categoría \"{categoria.Nombre}\"?",
                    "Confirmar eliminación",
                    this);

                if (!confirmar)
                    return;

                string query = "DELETE FROM Categorias WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", categoria.Id);
                cmd.ExecuteNonQuery();

                HuboCambios = true;

                if (categoriaSeleccionada != null && categoriaSeleccionada.Id == categoria.Id)
                {
                    categoriaSeleccionada = null;
                    subcategorias.Clear();
                    icSubcategorias.ItemsSource = null;
                    txtTituloSubcategorias.Text = "Subcategorías — selecciona una categoría";
                }

                CargarCategorias();
                MensajeHelper.Exito("Categoría eliminada correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo eliminar la categoría: " + ex.Message, "Error", this);
            }
        }

        // =========================================
        // SELECCIONAR CATEGORÍA → CARGAR SUS SUBCATEGORÍAS
        // =========================================

        private void FilaCategoria_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.Tag is not Categoria categoria)
                return;

            categoriaSeleccionada = categoria;
            txtTituloSubcategorias.Text = $"Subcategorías de \"{categoria.Nombre}\"";
            CargarSubcategorias();
        }

        private void CargarSubcategorias()
        {
            if (categoriaSeleccionada == null)
                return;

            try
            {
                subcategorias.Clear();

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query = "SELECT * FROM Subcategorias WHERE CategoriaId = @CategoriaId ORDER BY Nombre";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CategoriaId", categoriaSeleccionada.Id);

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    subcategorias.Add(new Subcategoria
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString() ?? "",
                        CategoriaId = Convert.ToInt32(reader["CategoriaId"])
                    });
                }

                icSubcategorias.ItemsSource = null;
                icSubcategorias.ItemsSource = subcategorias;
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar las subcategorías: " + ex.Message, "Error", this);
            }
        }

        private void BtnAgregarSubcategoria_Click(object sender, RoutedEventArgs e)
        {
            if (categoriaSeleccionada == null)
            {
                MensajeHelper.Advertencia("Primero selecciona una categoría", "Aviso", this);
                return;
            }

            string nombre = txtNuevaSubcategoria.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MensajeHelper.Advertencia("Escribe el nombre de la subcategoría", "Aviso", this);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryExiste = "SELECT COUNT(*) FROM Subcategorias WHERE Nombre = @Nombre AND CategoriaId = @CategoriaId";
                SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
                cmdExiste.Parameters.AddWithValue("@Nombre", nombre);
                cmdExiste.Parameters.AddWithValue("@CategoriaId", categoriaSeleccionada.Id);

                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                {
                    MensajeHelper.Advertencia("Ya existe una subcategoría con ese nombre en esta categoría", "Aviso", this);
                    return;
                }

                string query =
                @"INSERT INTO Subcategorias (Nombre, CategoriaId)
                  VALUES (@Nombre, @CategoriaId)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@CategoriaId", categoriaSeleccionada.Id);
                cmd.ExecuteNonQuery();

                txtNuevaSubcategoria.Clear();
                HuboCambios = true;

                CargarSubcategorias();
                MensajeHelper.Exito($"Subcategoría \"{nombre}\" agregada correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo agregar la subcategoría: " + ex.Message, "Error", this);
            }
        }

        private void BtnEditarSubcategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Subcategoria sub)
                return;

            string nuevoNombre = Microsoft.VisualBasic.Interaction.InputBox(
                "Nuevo nombre de la subcategoría:",
                "Editar subcategoría",
                sub.Nombre);

            if (string.IsNullOrWhiteSpace(nuevoNombre) || nuevoNombre == sub.Nombre)
                return;

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryExiste =
                    "SELECT COUNT(*) FROM Subcategorias WHERE Nombre = @Nombre AND CategoriaId = @CategoriaId AND Id <> @Id";
                SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
                cmdExiste.Parameters.AddWithValue("@Nombre", nuevoNombre.Trim());
                cmdExiste.Parameters.AddWithValue("@CategoriaId", sub.CategoriaId);
                cmdExiste.Parameters.AddWithValue("@Id", sub.Id);

                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                {
                    MensajeHelper.Advertencia("Ya existe otra subcategoría con ese nombre en esta categoría", "Aviso", this);
                    return;
                }

                string query = "UPDATE Subcategorias SET Nombre = @Nombre WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nuevoNombre.Trim());
                cmd.Parameters.AddWithValue("@Id", sub.Id);
                cmd.ExecuteNonQuery();

                HuboCambios = true;
                CargarSubcategorias();
                MensajeHelper.Exito("Subcategoría actualizada correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo actualizar la subcategoría: " + ex.Message, "Error", this);
            }
        }

        private void BtnEliminarSubcategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Subcategoria sub)
                return;

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryProductos = "SELECT COUNT(*) FROM Productos WHERE SubcategoriaId = @Id";
                SqlCommand cmdProductos = new SqlCommand(queryProductos, conn);
                cmdProductos.Parameters.AddWithValue("@Id", sub.Id);
                int productosConEsaSubcategoria = Convert.ToInt32(cmdProductos.ExecuteScalar());

                if (productosConEsaSubcategoria > 0)
                {
                    MensajeHelper.Advertencia(
                        $"No puedes eliminar \"{sub.Nombre}\": hay {productosConEsaSubcategoria} producto(s) asignado(s).",
                        "No se puede eliminar",
                        this);
                    return;
                }

                bool confirmar = MensajeHelper.Confirmar(
                    $"¿Eliminar la subcategoría \"{sub.Nombre}\"?",
                    "Confirmar eliminación",
                    this);

                if (!confirmar)
                    return;

                string query = "DELETE FROM Subcategorias WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", sub.Id);
                cmd.ExecuteNonQuery();

                HuboCambios = true;
                CargarSubcategorias();
                MensajeHelper.Exito("Subcategoría eliminada correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo eliminar la subcategoría: " + ex.Message, "Error", this);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = HuboCambios;
        }
    }
}