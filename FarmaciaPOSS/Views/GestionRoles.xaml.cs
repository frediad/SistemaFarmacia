using FarmaciaAPI.Models;
using FarmaciaPOS.Helpers;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace FarmaciaPOS.Views
{
    public partial class GestionRolesWindow : Window
    {
        private List<Rol> roles = new();

        public bool HuboCambios { get; private set; } = false;

        public GestionRolesWindow()
        {
            InitializeComponent();
            CargarRoles();
        }

        private void CargarRoles()
        {
            try
            {
                roles.Clear();

                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string query = "SELECT * FROM Roles ORDER BY Nombre";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    roles.Add(new Rol
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString() ?? "",
                        Descripcion = reader["Descripcion"].ToString() ?? ""
                    });
                }

                icRoles.ItemsSource = null;
                icRoles.ItemsSource = roles;
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudieron cargar los roles: " + ex.Message, "Error", this);
            }
        }

        private void BtnAgregarRol_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreRol.Text.Trim();
            string descripcion = txtDescripcionRol.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MensajeHelper.Advertencia("Escribe el nombre del rol", "Aviso", this);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryExiste = "SELECT COUNT(*) FROM Roles WHERE Nombre = @Nombre";
                SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
                cmdExiste.Parameters.AddWithValue("@Nombre", nombre);

                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                {
                    MensajeHelper.Advertencia("Ya existe un rol con ese nombre", "Aviso", this);
                    return;
                }

                string query =
                @"INSERT INTO Roles (Nombre, Descripcion)
                  VALUES (@Nombre, @Descripcion)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Descripcion", (object)descripcion ?? DBNull.Value);
                cmd.ExecuteNonQuery();

                txtNombreRol.Clear();
                txtDescripcionRol.Clear();
                HuboCambios = true;

                CargarRoles();
                MensajeHelper.Exito($"Rol \"{nombre}\" agregado correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo agregar el rol: " + ex.Message, "Error", this);
            }
        }

        private void BtnEditarRol_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Rol rol)
                return;

            string nuevoNombre = Microsoft.VisualBasic.Interaction.InputBox(
                "Nuevo nombre del rol:",
                "Editar rol",
                rol.Nombre);

            if (string.IsNullOrWhiteSpace(nuevoNombre))
                return;

            string nuevaDescripcion = Microsoft.VisualBasic.Interaction.InputBox(
                "Nueva descripción (opcional):",
                "Editar rol",
                rol.Descripcion ?? "");

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                string queryExiste = "SELECT COUNT(*) FROM Roles WHERE Nombre = @Nombre AND Id <> @Id";
                SqlCommand cmdExiste = new SqlCommand(queryExiste, conn);
                cmdExiste.Parameters.AddWithValue("@Nombre", nuevoNombre.Trim());
                cmdExiste.Parameters.AddWithValue("@Id", rol.Id);

                if (Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0)
                {
                    MensajeHelper.Advertencia("Ya existe otro rol con ese nombre", "Aviso", this);
                    return;
                }

                string query = "UPDATE Roles SET Nombre = @Nombre, Descripcion = @Descripcion WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nuevoNombre.Trim());
                cmd.Parameters.AddWithValue("@Descripcion", nuevaDescripcion?.Trim() ?? "");
                cmd.Parameters.AddWithValue("@Id", rol.Id);
                cmd.ExecuteNonQuery();

                HuboCambios = true;
                CargarRoles();
                MensajeHelper.Exito("Rol actualizado correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo actualizar el rol: " + ex.Message, "Error", this);
            }
        }

        private void BtnEliminarRol_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Rol rol)
                return;

            try
            {
                using SqlConnection conn = new SqlConnection(DatabaseHelper.ConnectionString);
                conn.Open();

                // ✅ Protección: no eliminar si hay usuarios con ese rol
                string queryUsuarios = "SELECT COUNT(*) FROM Usuarios WHERE RolId = @Id";
                SqlCommand cmdUsuarios = new SqlCommand(queryUsuarios, conn);
                cmdUsuarios.Parameters.AddWithValue("@Id", rol.Id);
                int usuariosConEseRol = Convert.ToInt32(cmdUsuarios.ExecuteScalar());

                if (usuariosConEseRol > 0)
                {
                    MensajeHelper.Advertencia(
                        $"No puedes eliminar \"{rol.Nombre}\": hay {usuariosConEseRol} usuario(s) con este rol asignado.",
                        "No se puede eliminar",
                        this);
                    return;
                }

                bool confirmar = MensajeHelper.Confirmar(
                    $"¿Eliminar el rol \"{rol.Nombre}\"?",
                    "Confirmar eliminación",
                    this);

                if (!confirmar)
                    return;

                string query = "DELETE FROM Roles WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", rol.Id);
                cmd.ExecuteNonQuery();

                HuboCambios = true;
                CargarRoles();
                MensajeHelper.Exito("Rol eliminado correctamente", "Listo", this);
            }
            catch (Exception ex)
            {
                MensajeHelper.Error("No se pudo eliminar el rol: " + ex.Message, "Error", this);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = HuboCambios;
        }
    }
}